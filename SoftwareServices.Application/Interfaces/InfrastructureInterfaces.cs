using SoftwareServices.Domain.Entities;

namespace SoftwareServices.Application.Interfaces;

public interface IEmailService
{
    Task QueueEmailAsync(string recipientEmail, string recipientName, string subject, string bodyHtml, string? attachmentPath = null, string? attachmentName = null);
    Task<bool> SendEmailDirectAsync(string recipientEmail, string recipientName, string subject, string bodyHtml, byte[]? attachmentBytes = null, string? attachmentName = null);
    Task SendEnquiryAcknowledgementAsync(Enquiry enquiry, Customer customer);
    Task SendAdminEnquiryNotificationAsync(Enquiry enquiry, Customer customer);
    Task SendQuotationEmailAsync(Quotation quotation, Customer customer, string quoteLink, byte[]? pdfBytes = null);
    Task SendQuotationAcceptedNotificationAsync(Quotation quotation, Customer customer);
    Task SendQuotationRejectedNotificationAsync(Quotation quotation, Customer customer);
}

public interface IJwtTokenService
{
    string GenerateToken(AdminUser user, int timeoutMinutes = 0);
    string GenerateUserToken(User user, int timeoutMinutes = 0);
}

public interface IPdfQuotationGenerator
{
    byte[] GenerateQuotationPdf(Quotation quotation, Customer customer, IEnumerable<QuotationItem> items, Dictionary<string, string>? companySettings = null);
}
