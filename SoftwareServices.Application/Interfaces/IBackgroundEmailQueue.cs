namespace SoftwareServices.Application.Interfaces;

public record QueuedEmailMessage(
    string RecipientEmail,
    string RecipientName,
    string Subject,
    string BodyHtml,
    byte[]? AttachmentBytes = null,
    string? AttachmentName = null
);

public interface IBackgroundEmailQueue
{
    ValueTask QueueEmailAsync(QueuedEmailMessage message);
    ValueTask<QueuedEmailMessage> DequeueAsync(CancellationToken cancellationToken);
}
