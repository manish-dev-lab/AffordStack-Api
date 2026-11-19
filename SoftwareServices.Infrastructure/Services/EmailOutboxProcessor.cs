using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SoftwareServices.Application.Interfaces;
using SoftwareServices.Domain.Enums;

namespace SoftwareServices.Infrastructure.Services;

public class EmailOutboxProcessor : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<EmailOutboxProcessor> _logger;

    public EmailOutboxProcessor(IServiceProvider serviceProvider, ILogger<EmailOutboxProcessor> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Email Outbox Processor started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
                var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

                var pendingEmails = await context.EmailOutbox
                    .Where(e => e.Status == EmailOutboxStatus.Pending && e.RetryCount < e.MaxRetries)
                    .OrderBy(e => e.CreatedAt)
                    .Take(10)
                    .ToListAsync(stoppingToken);

                foreach (var email in pendingEmails)
                {
                    try
                    {
                        email.Status = EmailOutboxStatus.Processing;
                        email.RetryCount++;
                        await context.SaveChangesAsync(stoppingToken);

                        byte[]? attBytes = null;
                        if (email.HasAttachment && !string.IsNullOrWhiteSpace(email.AttachmentPath) && File.Exists(email.AttachmentPath))
                        {
                            attBytes = await File.ReadAllBytesAsync(email.AttachmentPath, stoppingToken);
                        }

                        var sent = await emailService.SendEmailDirectAsync(
                            email.RecipientEmail,
                            email.RecipientName,
                            email.Subject,
                            email.BodyHtml,
                            attBytes,
                            email.AttachmentName);

                        if (sent)
                        {
                            email.Status = EmailOutboxStatus.Sent;
                            email.SentAt = DateTime.UtcNow;
                        }
                        else
                        {
                            email.Status = email.RetryCount >= email.MaxRetries ? EmailOutboxStatus.Failed : EmailOutboxStatus.Pending;
                        }

                        await context.SaveChangesAsync(stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing outbox email id {Id}", email.Id);
                        email.LastError = ex.Message;
                        email.Status = email.RetryCount >= email.MaxRetries ? EmailOutboxStatus.Failed : EmailOutboxStatus.Pending;
                        await context.SaveChangesAsync(stoppingToken);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in EmailOutboxProcessor loop");
            }

            await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
        }
    }
}
