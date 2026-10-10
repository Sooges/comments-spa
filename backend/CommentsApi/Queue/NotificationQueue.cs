using System.Threading.Channels;

namespace CommentsApi.Queue;

// The queue lives in the memory of the application, it is enough for this task.
public class NotificationQueue : INotificationQueue
{
    private readonly Channel<CommentNotification> _channel = Channel.CreateUnbounded<CommentNotification>();

    public void Enqueue(CommentNotification notification)
    {
        _channel.Writer.TryWrite(notification);
    }

    public IAsyncEnumerable<CommentNotification> ReadAllAsync(CancellationToken cancellationToken)
    {
        return _channel.Reader.ReadAllAsync(cancellationToken);
    }
}
