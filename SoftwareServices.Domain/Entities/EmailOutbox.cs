using SoftwareServices.Domain.Common;
using SoftwareServices.Domain.Enums;

namespace SoftwareServices.Domain.Entities;

public class EmailOutbox : BaseEntity
{
    public string RecipientEmail { get; set; } = string.Empty;
    public string RecipientName { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string BodyHtml { get; set; } = string.Empty;
    public bool HasAttachment { get; set; } = false;
    public string? AttachmentPath { get; set; }
    public string? AttachmentName { get; set; }

    public EmailOutboxStatus Status { get; set; } = EmailOutboxStatus.Pending;
    public int RetryCount { get; set; } = 0;
    public int MaxRetries { get; set; } = 3;
    public string? LastError { get; set; }
    public DateTime? SentAt { get; set; }
}
