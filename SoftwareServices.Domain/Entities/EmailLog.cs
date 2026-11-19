using SoftwareServices.Domain.Common;

namespace SoftwareServices.Domain.Entities;

public class EmailLog : BaseEntity
{
    public string RecipientEmail { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty; // e.g. "EnquiryAcknowledgement", "AdminNotification", "QuotationSent"
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
}
