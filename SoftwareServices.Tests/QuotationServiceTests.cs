using Microsoft.EntityFrameworkCore;
using Moq;
using SoftwareServices.Application.DTOs.Quotation;
using SoftwareServices.Application.Interfaces;
using SoftwareServices.Application.Services;
using SoftwareServices.Domain.Entities;
using SoftwareServices.Domain.Enums;
using SoftwareServices.Infrastructure.Data;
using Xunit;

namespace SoftwareServices.Tests;

public class QuotationServiceTests
{
    private ApplicationDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task CreateQuotation_CalculatesTaxAndTotal_Accurately()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var mockEmail = new Mock<IEmailService>();
        var mockPdf = new Mock<IPdfQuotationGenerator>();
        var service = new QuotationService(context, mockEmail.Object, mockPdf.Object);

        var customer = new Customer { FullName = "Alice", Email = "alice@example.com", PhoneNumber = "123" };
        context.Customers.Add(customer);
        await context.SaveChangesAsync();

        var enquiry = new Enquiry
        {
            ReferenceNumber = "ENQ-20261118-0002",
            CustomerId = customer.Id,
            ProjectTitle = "E-Commerce App",
            ProjectDescription = "Desc",
            RequiredTechnology = "React & .NET",
            EstimatedBudget = "100000"
        };
        context.Enquiries.Add(enquiry);
        await context.SaveChangesAsync();

        var request = new CreateQuotationRequest
        {
            EnquiryId = enquiry.Id,
            ServiceDescription = "E-Commerce Web Portal Development",
            Deliverables = "Admin dashboard, storefront, payment gateway integration",
            DevelopmentCost = 100000m,
            AdditionalCost = 10000m,
            TaxRate = 18m, // 18% of 110,000 = 19,800
            AdvancePaymentAmount = 45000m,
            ExpectedDeliveryDate = DateTime.UtcNow.AddDays(30),
            ValidityDays = 15,
            TermsAndConditions = "Standard terms",
            Items = new List<QuotationItemDto>
            {
                new() { Title = "Storefront UI", Description = "React frontend", Quantity = 1, UnitPrice = 50000m },
                new() { Title = "Backend Web API", Description = "ASP.NET Core REST API", Quantity = 1, UnitPrice = 50000m }
            }
        };

        // Act
        var result = await service.CreateQuotationAsync(request, "AdminUser");

        // Assert
        Assert.NotNull(result);
        Assert.StartsWith("QT-", result.QuotationNumber);
        Assert.Equal(110000m, result.DevelopmentCost + result.AdditionalCost);
        Assert.Equal(19800m, result.TaxAmount);
        Assert.Equal(129800m, result.TotalAmount);
        Assert.Equal(45000m, result.AdvancePaymentAmount);
        Assert.Equal(84800m, result.RemainingPaymentAmount);
        Assert.Equal(QuotationStatus.Draft, result.Status);
    }

    [Fact]
    public async Task CustomerAcceptQuotation_UpdatesStatus_AndInitiatesProject()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var mockEmail = new Mock<IEmailService>();
        var mockPdf = new Mock<IPdfQuotationGenerator>();
        var service = new QuotationService(context, mockEmail.Object, mockPdf.Object);

        var customer = new Customer { FullName = "Charlie", Email = "charlie@example.com", PhoneNumber = "123" };
        context.Customers.Add(customer);
        await context.SaveChangesAsync();

        var enquiry = new Enquiry
        {
            ReferenceNumber = "ENQ-20261118-0003",
            CustomerId = customer.Id,
            ProjectTitle = "Inventory ERP",
            ProjectDescription = "Desc",
            RequiredTechnology = "C# & PostgreSQL",
            EstimatedBudget = "200000"
        };
        context.Enquiries.Add(enquiry);
        await context.SaveChangesAsync();

        var token = Guid.NewGuid().ToString("N");
        var quotation = new Quotation
        {
            QuotationNumber = "QT-20261118-0001",
            EnquiryId = enquiry.Id,
            CustomerId = customer.Id,
            Customer = customer,
            ServiceDescription = "ERP Setup",
            Deliverables = "Modules A, B, C",
            DevelopmentCost = 150000m,
            TotalAmount = 150000m,
            AdvancePaymentAmount = 60000m,
            RemainingPaymentAmount = 90000m,
            ExpectedDeliveryDate = DateTime.UtcNow.AddDays(45),
            ValidUntil = DateTime.UtcNow.AddDays(15),
            Status = QuotationStatus.Sent,
            SecureToken = token
        };
        context.Quotations.Add(quotation);
        await context.SaveChangesAsync();

        // Act
        var accepted = await service.CustomerAcceptQuotationAsync(token, new QuotationDecisionRequest
        {
            Remarks = "Looks good, approved to start!"
        });

        // Assert
        Assert.True(accepted);
        var refreshedQuote = await context.Quotations.Include(q => q.Project).FirstOrDefaultAsync(q => q.Id == quotation.Id);
        Assert.NotNull(refreshedQuote);
        Assert.Equal(QuotationStatus.Accepted, refreshedQuote.Status);
        Assert.NotNull(refreshedQuote.Project);
        Assert.Equal(ProjectStatus.InProgress, refreshedQuote.Project.Status);
        Assert.Equal(customer.Id, refreshedQuote.Project.CustomerId);

        mockEmail.Verify(e => e.SendQuotationAcceptedNotificationAsync(It.IsAny<Quotation>(), It.IsAny<Customer>()), Times.Once);
    }
}
