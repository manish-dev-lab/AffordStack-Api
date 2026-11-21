using System.Threading.Channels;
using SoftwareServices.Application.Interfaces;

namespace SoftwareServices.Infrastructure.Services;

public class BackgroundEmailQueue : IBackgroundEmailQueue
{
    private readonly Channel<QueuedEmailMessage> _queue;

    public BackgroundEmailQueue(int capacity = 10000)
    {
        var options = new BoundedChannelOptions(capacity)
        {
            FullMode = BoundedChannelFullMode.Wait
        };
        _queue = Channel.CreateBounded<QueuedEmailMessage>(options);
    }

    public ValueTask QueueEmailAsync(QueuedEmailMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);
        return _queue.Writer.WriteAsync(message);
    }

    public ValueTask<QueuedEmailMessage> DequeueAsync(CancellationToken cancellationToken)
    {
        return _queue.Reader.ReadAsync(cancellationToken);
    }
}
