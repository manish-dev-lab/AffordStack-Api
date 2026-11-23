using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using AffordStack.Application.DTOs.Payment;
using AffordStack.Application.Interfaces;
using AffordStack.Application.Services;
using AffordStack.Domain.Entities;
using AffordStack.Domain.Enums;
using AffordStack.Infrastructure.Data;
using Xunit;

namespace AffordStack.Tests;

public class PaymentServiceTests
{
    private ApplicationDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task AdvancePaymentVerification_AcceptsQuotation_ProvisionsProject_AndRecordsPayment()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var mockCashfree = new Mock<ICashfreeService>();
        var mockEmail = new Mock<IEmailService>();
        var mockLogger = new Mock<ILogger<PaymentService>>();

        var service = new PaymentService(context, mockCashfree.Object, mockEmail.Object, mockLogger.Object);

        var customer = new Customer
        {
            FullName = "Manish Test",
            Email = "manish@affordstack.com",
            PhoneNumber = "9876543210"
        };
        context.Customers.Add(customer);
        await context.SaveChangesAsync();

        var enquiry = new Enquiry
        {
            ReferenceNumber = "ENQ-20261123-0001",
            CustomerId = customer.Id,
            Customer = customer,
            ProjectTitle = "Custom Web Development",
            ProjectDescription = "Requirements for web portal",
            RequiredTechnology = "React & .NET",
            EstimatedBudget = "50000"
        };
        context.Enquiries.Add(enquiry);
        await context.SaveChangesAsync();

        var quotation = new Quotation
        {
            QuotationNumber = "QT-20261123-0001",
            SecureToken = "SECURE_TOKEN_XYZ_123",
            EnquiryId = enquiry.Id,
            Enquiry = enquiry,
            CustomerId = customer.Id,
            Customer = customer,
            ServiceDescription = "Custom Web Development",
            Deliverables = "Module A, Module B",
            DevelopmentCost = 50000m,
            TotalAmount = 50000m,
            AdvancePaymentAmount = 15000m,
            RemainingPaymentAmount = 35000m,
            Status = QuotationStatus.Sent,
            IsAdvancePaid = false,
            ExpectedDeliveryDate = DateTime.UtcNow.AddDays(30),
            ValidUntil = DateTime.UtcNow.AddDays(14)
        };
        context.Quotations.Add(quotation);
        await context.SaveChangesAsync();

        var quoteIdFromDb = quotation.Id;
        var orderId = $"CF_Q_{quoteIdFromDb}_123456789";

        mockCashfree.Setup(c => c.GetOrderDetailsAsync(orderId))
            .ReturnsAsync(new PaymentVerificationResponse
            {
                Success = true,
                OrderId = orderId,
                PaymentId = "CF_PAY_9999",
                Status = "PAID",
                Amount = 15000m,
                PaidAt = DateTime.UtcNow,
                Message = "Payment successful."
            });

        // Act
        var result = await service.VerifyAndCompletePaymentAsync(orderId);

        // Assert
        Assert.True(quoteIdFromDb > 0, $"quoteIdFromDb was {quoteIdFromDb}");
        Assert.Equal("Advance payment received successfully. Project kickoff initiated!", result.Message);

        var refreshedQuote = await context.Quotations
            .Include(q => q.Project)
                .ThenInclude(p => p!.Milestones)
            .FirstOrDefaultAsync(q => q.Id == quoteIdFromDb);

        Assert.NotNull(refreshedQuote);
        Assert.True(refreshedQuote.IsAdvancePaid);
        Assert.Equal(QuotationStatus.Accepted, refreshedQuote.Status);

        // Verify Project auto-provisioned
        Assert.NotNull(refreshedQuote.Project);
        Assert.Equal(ProjectStatus.InProgress, refreshedQuote.Project.Status);
        Assert.NotEmpty(refreshedQuote.Project.Milestones);

        // Verify single default delivery milestone created for remaining balance
        Assert.Single(refreshedQuote.Project.Milestones);
        var defaultMilestone = refreshedQuote.Project.Milestones.First();
        Assert.NotNull(defaultMilestone);
        Assert.Contains("Delivery", defaultMilestone.Title);
        Assert.Equal(35000m, defaultMilestone.Amount); // 50000 total - 15000 advance = 35000 remaining
        Assert.False(defaultMilestone.IsPaid);

        // Verify Payment Record created in database
        var paymentRecord = await context.Payments
            .FirstOrDefaultAsync(p => p.CashfreeOrderId == orderId);

        Assert.NotNull(paymentRecord);
        Assert.Equal(customer.Id, paymentRecord.CustomerId);
        Assert.Equal(15000m, paymentRecord.Amount);
        Assert.Equal(PaymentType.Advance, paymentRecord.PaymentType);
        Assert.Equal(PaymentStatus.Received, paymentRecord.Status);
        Assert.Equal("CF_PAY_9999", paymentRecord.CashfreePaymentId);
    }

    [Fact]
    public async Task MilestonePaymentVerification_MarksMilestonePaid_AndRecordsPayment()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var mockCashfree = new Mock<ICashfreeService>();
        var mockEmail = new Mock<IEmailService>();
        var mockLogger = new Mock<ILogger<PaymentService>>();

        var service = new PaymentService(context, mockCashfree.Object, mockEmail.Object, mockLogger.Object);

        var customer = new Customer
        {
            FullName = "Manish Test",
            Email = "manish@affordstack.com",
            PhoneNumber = "9876543210"
        };
        context.Customers.Add(customer);

        var project = new Project
        {
            ProjectNumber = "PRJ-20261123-0001",
            Customer = customer,
            CustomerId = customer.Id,
            ProjectTitle = "E-Commerce Project",
            Description = "Full stack app",
            AgreedAmount = 50000m,
            Status = ProjectStatus.InProgress
        };
        context.Projects.Add(project);

        var milestone = new ProjectMilestone
        {
            Project = project,
            ProjectId = project.Id,
            Title = "Sprint 1: Authentication & Core APIs",
            Amount = 17500m,
            IsCompleted = true, // Marked completed by developer
            CompletedAt = DateTime.UtcNow,
            IsPaid = false,
            DueDate = DateTime.UtcNow.AddDays(7)
        };
        context.ProjectMilestones.Add(milestone);
        await context.SaveChangesAsync();

        var orderId = $"CF_M_{milestone.Id}_987654321";
        milestone.PaymentReference = orderId;
        await context.SaveChangesAsync();

        mockCashfree.Setup(c => c.GetOrderDetailsAsync(orderId))
            .ReturnsAsync(new PaymentVerificationResponse
            {
                Success = true,
                OrderId = orderId,
                PaymentId = "CF_PAY_MILESTONE_8888",
                Status = "PAID",
                Amount = 17500m,
                PaidAt = DateTime.UtcNow,
                Message = "Milestone payment successful."
            });

        // Act
        var result = await service.VerifyAndCompletePaymentAsync(orderId);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("PAID", result.Status);

        // Verify Milestone marked as Paid
        var refreshedMilestone = await context.ProjectMilestones
            .FirstOrDefaultAsync(m => m.Id == milestone.Id);

        Assert.NotNull(refreshedMilestone);
        Assert.True(refreshedMilestone.IsPaid);
        Assert.NotNull(refreshedMilestone.PaidAt);

        // Verify Payment Record exists
        var payments = await service.GetCustomerPaymentsAsync(customer.Id);
        Assert.Single(payments);
        Assert.Equal(17500m, payments[0].Amount);
        Assert.Equal(PaymentType.Milestone, payments[0].PaymentType);
        Assert.Equal(orderId, payments[0].CashfreeOrderId);
        Assert.Equal("CF_PAY_MILESTONE_8888", payments[0].CashfreePaymentId);
    }
}
