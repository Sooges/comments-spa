namespace CommentsApi.Queue;

public interface INotificationQueue
{
    /// <summary>
    /// Puts a notification into the queue, it does not wait until the notification is sent.
    /// </summary>
    void Enqueue(CommentNotification notification);

    /// <summary>
    /// Takes the notifications one by one, waits when the queue is empty.
    /// </summary>
    IAsyncEnumerable<CommentNotification> ReadAllAsync(CancellationToken cancellationToken);
}
