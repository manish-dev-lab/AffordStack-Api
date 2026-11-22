using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using AffordStack.Application.Interfaces;

namespace AffordStack.Infrastructure.Services;

public class BackgroundEmailWorker : BackgroundService
{
    private readonly IBackgroundEmailQueue _queue;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<BackgroundEmailWorker> _logger;

    public BackgroundEmailWorker(
        IBackgroundEmailQueue queue,
        IServiceProvider serviceProvider,
        ILogger<BackgroundEmailWorker> logger)
    {
        _queue = queue;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Background Email Dispatcher Worker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var message = await _queue.DequeueAsync(stoppingToken);

                // Create a fresh scope to execute the email send and DB log safely
                using var scope = _serviceProvider.CreateScope();
                var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

                await emailService.SendEmailDirectAsync(
                    message.RecipientEmail,
                    message.RecipientName,
                    message.Subject,
                    message.BodyHtml,
                    message.AttachmentBytes,
                    message.AttachmentName);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error processing queued email in BackgroundEmailWorker.");
            }
        }
    }
}
