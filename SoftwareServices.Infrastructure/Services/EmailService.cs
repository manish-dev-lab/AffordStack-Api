using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;
using SoftwareServices.Application.Interfaces;
using SoftwareServices.Domain.Entities;
using SoftwareServices.Domain.Enums;

namespace SoftwareServices.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;
    private readonly IApplicationDbContext _context;

    public EmailService(
        IConfiguration configuration,
        ILogger<EmailService> logger,
        IApplicationDbContext context)
    {
        _configuration = configuration;
        _logger = logger;
        _context = context;
    }

    public async Task QueueEmailAsync(string recipientEmail, string recipientName, string subject, string bodyHtml, string? attachmentPath = null, string? attachmentName = null)
    {
        var outboxItem = new EmailOutbox
        {
            RecipientEmail = recipientEmail,
            RecipientName = recipientName,
            Subject = subject,
            BodyHtml = bodyHtml,
            HasAttachment = !string.IsNullOrWhiteSpace(attachmentPath),
            AttachmentPath = attachmentPath,
            AttachmentName = attachmentName,
            Status = EmailOutboxStatus.Pending
        };

        _context.EmailOutbox.Add(outboxItem);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> SendEmailDirectAsync(string recipientEmail, string recipientName, string subject, string bodyHtml, byte[]? attachmentBytes = null, string? attachmentName = null)
    {
        var host = _configuration["Smtp:Host"];
        var port = int.TryParse(_configuration["Smtp:Port"], out var p) ? p : 587;
        var username = _configuration["Smtp:Username"];
        var password = _configuration["Smtp:Password"];
        var fromEmail = _configuration["Smtp:FromEmail"] ?? "noreply@example.com";
        var fromName = _configuration["Smtp:FromName"] ?? "Software Services";

        // If host is dummy/unconfigured, log and return gracefully
        if (string.IsNullOrWhiteSpace(host) || host == "example.smtp.com" || string.IsNullOrWhiteSpace(username) || username == "your-smtp-username")
        {
            _logger.LogInformation("SMTP unconfigured or set to placeholder. Email to {Recipient} simulated successfully. Subject: {Subject}", recipientEmail, subject);
            
            _context.EmailLogs.Add(new EmailLog
            {
                RecipientEmail = recipientEmail,
                Subject = subject,
                EventType = "SimulatedSend",
                IsSuccess = true,
                ErrorMessage = "SMTP credentials are placeholder; logged without external network dispatch."
            });
            await _context.SaveChangesAsync();
            return true;
        }

        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(fromName, fromEmail));
            message.To.Add(new MailboxAddress(recipientName, recipientEmail));
            message.Subject = subject;

            var builder = new BodyBuilder { HtmlBody = bodyHtml };

            if (attachmentBytes != null && attachmentBytes.Length > 0 && !string.IsNullOrWhiteSpace(attachmentName))
            {
                builder.Attachments.Add(attachmentName, attachmentBytes);
            }

            message.Body = builder.ToMessageBody();

            using var client = new SmtpClient();
            await client.ConnectAsync(host, port, SecureSocketOptions.Auto);
            if (!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password))
            {
                await client.AuthenticateAsync(username, password);
            }
            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            _context.EmailLogs.Add(new EmailLog
            {
                RecipientEmail = recipientEmail,
                Subject = subject,
                EventType = "SmtpSend",
                IsSuccess = true
            });
            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to dispatch email to {Recipient}. Subject: {Subject}", recipientEmail, subject);
            _context.EmailLogs.Add(new EmailLog
            {
                RecipientEmail = recipientEmail,
                Subject = subject,
                EventType = "SmtpError",
                IsSuccess = false,
                ErrorMessage = ex.Message
            });
            await _context.SaveChangesAsync();
            return false;
        }
    }

    private async Task<(string Name, string Email, string Phone, string Address)> GetCompanyDetailsAsync()
    {
        var settings = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToDictionaryAsync(
            Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.AsNoTracking(_context.WebsiteSettings),
            s => s.SettingKey,
            s => s.SettingValue);

        var name = settings.GetValueOrDefault("Company:Name")
            ?? _configuration["Company:Name"]
            ?? "Soft Solutions";

        var email = settings.GetValueOrDefault("Company:Email")
            ?? _configuration["Company:Email"]
            ?? "manishkiran8464@gmail.com";

        var phone = settings.GetValueOrDefault("Company:Phone")
            ?? _configuration["Company:Phone"]
            ?? "+91 8464992957";

        var address = settings.GetValueOrDefault("Company:Address")
            ?? _configuration["Company:Address"]
            ?? "Rc puram Hyd";

        return (name, email, phone, address);
    }

    public async Task SendEnquiryAcknowledgementAsync(Enquiry enquiry, Customer customer)
    {
        var (businessName, businessEmail, businessPhone, _) = await GetCompanyDetailsAsync();

        var body = $@"
<div style=""font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif; max-width: 600px; margin: 0 auto; background: #ffffff; border: 1px solid #e2e8f0; border-radius: 8px; overflow: hidden;"">
  <div style=""background: #0a1128; padding: 24px; color: #ffffff; text-align: center;"">
    <h2 style=""margin: 0; font-size: 22px; color: #ffffff;"">{businessName}</h2>
    <p style=""margin: 6px 0 0 0; color: #94a3b8; font-size: 14px;"">Software Development &amp; Technical Services</p>
  </div>
  <div style=""padding: 28px 24px; color: #334155; line-height: 1.6;"">
    <p style=""font-size: 16px; margin-top: 0;"">Hello <strong>{customer.FullName}</strong>,</p>
    <p>Thank you for reaching out to us. We have received your project enquiry and quotation request.</p>
    
    <div style=""background: #f8fafc; border-left: 4px solid #2563eb; padding: 16px; margin: 20px 0; border-radius: 4px;"">
      <p style=""margin: 0 0 8px 0;""><strong>Reference Number:</strong> <span style=""color: #2563eb; font-weight: 600;"">{enquiry.ReferenceNumber}</span></p>
      <p style=""margin: 0 0 8px 0;""><strong>Project Title:</strong> {enquiry.ProjectTitle}</p>
      <p style=""margin: 0 0 8px 0;""><strong>Required Technology:</strong> {enquiry.RequiredTechnology}</p>
      <p style=""margin: 0;""><strong>Estimated Budget:</strong> {enquiry.EstimatedBudget}</p>
    </div>

    <p>Our technical team is reviewing your requirements. We will prepare an architectural breakdown and detailed quotation for you shortly.</p>
    <p>If you have any supplementary documentation or source code samples to share, reply directly to this email referencing your enquiry number.</p>
  </div>
  <div style=""background: #f1f5f9; padding: 18px 24px; text-align: center; font-size: 13px; color: #64748b; border-top: 1px solid #e2e8f0;"">
    <p style=""margin: 0 0 4px 0;"">Need immediate assistance? Contact us at <a href=""mailto:{businessEmail}"" style=""color: #2563eb;"">{businessEmail}</a> or {businessPhone}</p>
    <p style=""margin: 0;"">&copy; {DateTime.UtcNow.Year} {businessName}. All rights reserved.</p>
  </div>
</div>";

        await SendEmailDirectAsync(customer.Email, customer.FullName, $"Enquiry Received [{enquiry.ReferenceNumber}] - {businessName}", body);
    }

    public async Task SendAdminEnquiryNotificationAsync(Enquiry enquiry, Customer customer)
    {
        var (businessName, businessEmail, _, _) = await GetCompanyDetailsAsync();
        var adminEmail = _configuration["Smtp:AdminNotificationEmail"] ?? businessEmail;

        var body = $@"
<div style=""font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; background: #ffffff; border: 1px solid #e2e8f0; border-radius: 8px; overflow: hidden;"">
  <div style=""background: #1e3a8a; padding: 20px; color: #ffffff;"">
    <h3 style=""margin: 0;"">New Enquiry Alert [{enquiry.ReferenceNumber}]</h3>
  </div>
  <div style=""padding: 24px; color: #1e293b;"">
    <p>A new quotation enquiry has been submitted through the portal.</p>
    <table style=""width: 100%; border-collapse: collapse; margin-top: 12px;"">
      <tr><td style=""padding: 8px; border-bottom: 1px solid #e2e8f0; font-weight: bold;"">Reference No:</td><td style=""padding: 8px; border-bottom: 1px solid #e2e8f0;"">{enquiry.ReferenceNumber}</td></tr>
      <tr><td style=""padding: 8px; border-bottom: 1px solid #e2e8f0; font-weight: bold;"">Customer:</td><td style=""padding: 8px; border-bottom: 1px solid #e2e8f0;"">{customer.FullName} ({customer.Email})</td></tr>
      <tr><td style=""padding: 8px; border-bottom: 1px solid #e2e8f0; font-weight: bold;"">Phone:</td><td style=""padding: 8px; border-bottom: 1px solid #e2e8f0;"">{customer.PhoneNumber}</td></tr>
      <tr><td style=""padding: 8px; border-bottom: 1px solid #e2e8f0; font-weight: bold;"">Company:</td><td style=""padding: 8px; border-bottom: 1px solid #e2e8f0;"">{customer.CompanyName ?? "N/A"}</td></tr>
      <tr><td style=""padding: 8px; border-bottom: 1px solid #e2e8f0; font-weight: bold;"">Project Title:</td><td style=""padding: 8px; border-bottom: 1px solid #e2e8f0;"">{enquiry.ProjectTitle}</td></tr>
      <tr><td style=""padding: 8px; border-bottom: 1px solid #e2e8f0; font-weight: bold;"">Technology:</td><td style=""padding: 8px; border-bottom: 1px solid #e2e8f0;"">{enquiry.RequiredTechnology}</td></tr>
      <tr><td style=""padding: 8px; border-bottom: 1px solid #e2e8f0; font-weight: bold;"">Budget:</td><td style=""padding: 8px; border-bottom: 1px solid #e2e8f0;"">{enquiry.EstimatedBudget}</td></tr>
      <tr><td style=""padding: 8px; border-bottom: 1px solid #e2e8f0; font-weight: bold;"">Description:</td><td style=""padding: 8px; border-bottom: 1px solid #e2e8f0;"">{enquiry.ProjectDescription}</td></tr>
    </table>
    <p style=""margin-top: 20px;""><a href=""/admin/enquiries/{enquiry.Id}"" style=""background: #2563eb; color: #ffffff; padding: 10px 18px; text-decoration: none; border-radius: 4px; display: inline-block;"">View in Admin Dashboard</a></p>
  </div>
</div>";

        await SendEmailDirectAsync(adminEmail, "Admin Portal", $"New Enquiry: {enquiry.ProjectTitle} [{enquiry.ReferenceNumber}]", body);
    }

    public async Task SendQuotationEmailAsync(Quotation quotation, Customer customer, string quoteLink, byte[]? pdfBytes = null)
    {
        var (businessName, _, _, _) = await GetCompanyDetailsAsync();

        var body = $@"
<div style=""font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif; max-width: 600px; margin: 0 auto; background: #ffffff; border: 1px solid #e2e8f0; border-radius: 8px; overflow: hidden;"">
  <div style=""background: #0a1128; padding: 24px; color: #ffffff; text-align: center;"">
    <h2 style=""margin: 0; color: #ffffff;"">{businessName}</h2>
    <p style=""margin: 6px 0 0 0; color: #94a3b8; font-size: 14px;"">Official Quotation Proposal</p>
  </div>
  <div style=""padding: 28px 24px; color: #334155; line-height: 1.6;"">
    <p style=""font-size: 16px; margin-top: 0;"">Dear <strong>{customer.FullName}</strong>,</p>
    <p>We are pleased to present our official quotation for your project.</p>
    
    <div style=""background: #f8fafc; border: 1px solid #e2e8f0; padding: 18px; margin: 20px 0; border-radius: 6px;"">
      <div style=""font-size: 18px; font-weight: 700; color: #0f172a; margin-bottom: 8px;"">Quotation: {quotation.QuotationNumber}</div>
      <p style=""margin: 0 0 6px 0;""><strong>Total Investment:</strong> ₹{quotation.TotalAmount:N2}</p>
      <p style=""margin: 0 0 6px 0;""><strong>Advance Required:</strong> ₹{quotation.AdvancePaymentAmount:N2}</p>
      <p style=""margin: 0 0 6px 0;""><strong>Expected Delivery:</strong> {quotation.ExpectedDeliveryDate:dd MMM yyyy}</p>
      <p style=""margin: 0;""><strong>Offer Valid Until:</strong> {quotation.ValidUntil:dd MMM yyyy}</p>
    </div>

    <p>You can review full details, deliverables, milestones, and accept or decline this quotation directly via our secure portal below:</p>
    
    <p style=""text-align: center; margin: 28px 0;"">
      <a href=""{quoteLink}"" style=""background: #2563eb; color: #ffffff; padding: 12px 28px; text-decoration: none; border-radius: 6px; font-weight: 600; display: inline-block; font-size: 15px;"">Review Quotation Proposal</a>
    </p>

    <p style=""font-size: 13px; color: #64748b;"">A PDF copy of this quotation is also attached for your records.</p>
  </div>
  <div style=""background: #f1f5f9; padding: 16px 24px; text-align: center; font-size: 13px; color: #64748b; border-top: 1px solid #e2e8f0;"">
    <p style=""margin: 0;"">&copy; {DateTime.UtcNow.Year} {businessName}. All rights reserved.</p>
  </div>
</div>";

        var attName = $"Quotation_{quotation.QuotationNumber}.pdf";
        await SendEmailDirectAsync(customer.Email, customer.FullName, $"Quotation Proposal [{quotation.QuotationNumber}] - {businessName}", body, pdfBytes, attName);
    }

    public async Task SendQuotationAcceptedNotificationAsync(Quotation quotation, Customer customer)
    {
        var (_, businessEmail, _, _) = await GetCompanyDetailsAsync();
        var adminEmail = _configuration["Smtp:AdminNotificationEmail"] ?? businessEmail;
        var body = $@"
<div style=""font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #e2e8f0; border-radius: 8px;"">
  <h2 style=""color: #16a34a; margin-top: 0;"">Quotation Accepted!</h2>
  <p>Customer <strong>{customer.FullName}</strong> ({customer.Email}) has accepted Quotation <strong>{quotation.QuotationNumber}</strong>.</p>
  <p><strong>Total Amount:</strong> ₹{quotation.TotalAmount:N2}</p>
  <p><strong>Advance Amount:</strong> ₹{quotation.AdvancePaymentAmount:N2}</p>
  <p><strong>Customer Remarks:</strong> {quotation.ResponseRemarks ?? "None"}</p>
  <p>A linked project record has been automatically initiated.</p>
</div>";

        await SendEmailDirectAsync(adminEmail, "Admin", $"Quotation ACCEPTED: {quotation.QuotationNumber}", body);
    }

    public async Task SendQuotationRejectedNotificationAsync(Quotation quotation, Customer customer)
    {
        var (_, businessEmail, _, _) = await GetCompanyDetailsAsync();
        var adminEmail = _configuration["Smtp:AdminNotificationEmail"] ?? businessEmail;
        var body = $@"
<div style=""font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #e2e8f0; border-radius: 8px;"">
  <h2 style=""color: #dc2626; margin-top: 0;"">Quotation Declined</h2>
  <p>Customer <strong>{customer.FullName}</strong> ({customer.Email}) has declined Quotation <strong>{quotation.QuotationNumber}</strong>.</p>
  <p><strong>Customer Remarks:</strong> {quotation.ResponseRemarks ?? "None provided"}</p>
</div>";

        await SendEmailDirectAsync(adminEmail, "Admin", $"Quotation DECLINED: {quotation.QuotationNumber}", body);
    }
}
