using Microsoft.EntityFrameworkCore;
using Moq;
using SoftwareServices.Application.DTOs.Enquiry;
using SoftwareServices.Application.Interfaces;
using SoftwareServices.Application.Services;
using SoftwareServices.Domain.Entities;
using SoftwareServices.Domain.Enums;
using SoftwareServices.Infrastructure.Data;
using Xunit;

namespace SoftwareServices.Tests;

public class EnquiryServiceTests
{
    private ApplicationDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task SubmitEnquiry_CreatesCustomer_AndGeneratesReferenceNumber()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var mockEmail = new Mock<IEmailService>();
        var service = new EnquiryService(context, mockEmail.Object);

        var request = new CreateEnquiryRequest
        {
            FullName = "Jane Doe",
            Email = "jane@example.com",
            PhoneNumber = "+919876543210",
            CompanyName = "Acme Corp",
            ProjectTitle = "Hospital Management System",
            ProjectDescription = "Needs a complete hospital management system with patient portal.",
            RequiredTechnology = "ASP.NET Core & React",
            EstimatedBudget = "₹1,50,000 - ₹2,00,000"
        };

        // Act
        var result = await service.SubmitEnquiryAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.StartsWith("ENQ-", result.ReferenceNumber);

        var customer = await context.Customers.FirstOrDefaultAsync(c => c.Email == "jane@example.com");
        Assert.NotNull(customer);
        Assert.Equal("Jane Doe", customer.FullName);

        var enquiry = await context.Enquiries.FirstOrDefaultAsync(e => e.ReferenceNumber == result.ReferenceNumber);
        Assert.NotNull(enquiry);
        Assert.Equal(EnquiryStatus.New, enquiry.Status);
        Assert.Equal(customer.Id, enquiry.CustomerId);

        // Verify emails were requested
        mockEmail.Verify(e => e.SendEnquiryAcknowledgementAsync(It.IsAny<Enquiry>(), It.IsAny<Customer>()), Times.Once);
        mockEmail.Verify(e => e.SendAdminEnquiryNotificationAsync(It.IsAny<Enquiry>(), It.IsAny<Customer>()), Times.Once);
    }

    [Fact]
    public async Task UpdateStatus_UpdatesStatus_AndAddsHistoryNote()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var mockEmail = new Mock<IEmailService>();
        var service = new EnquiryService(context, mockEmail.Object);

        var customer = new Customer { FullName = "Bob Smith", Email = "bob@example.com", PhoneNumber = "1234567890" };
        context.Customers.Add(customer);
        await context.SaveChangesAsync();

        var enquiry = new Enquiry
        {
            ReferenceNumber = "ENQ-20261118-0001",
            CustomerId = customer.Id,
            ProjectTitle = "Test Project",
            ProjectDescription = "Test description",
            RequiredTechnology = ".NET",
            EstimatedBudget = "50000",
            Status = EnquiryStatus.New
        };
        context.Enquiries.Add(enquiry);
        await context.SaveChangesAsync();

        // Act
        var updated = await service.UpdateStatusAsync(enquiry.Id, new UpdateEnquiryStatusRequest
        {
            Status = EnquiryStatus.Reviewed,
            Note = "Initial requirement check completed."
        }, adminUserId: 1);

        // Assert
        Assert.True(updated);
        var refreshed = await context.Enquiries.Include(e => e.Notes).FirstOrDefaultAsync(e => e.Id == enquiry.Id);
        Assert.NotNull(refreshed);
        Assert.Equal(EnquiryStatus.Reviewed, refreshed.Status);
        Assert.Single(refreshed.Notes);
    }
}
