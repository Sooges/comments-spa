using CommentsApi.Queue;
using CommentsApi.Services;

namespace CommentsApi.Events;

// Listens to the comment events. It is created when the application starts (see Program.cs).
public class CommentEventSubscriber
{
    private readonly ICommentCache _cache;
    private readonly INotificationQueue _queue;

    public CommentEventSubscriber(ICommentEvents events, ICommentCache cache, INotificationQueue queue)
    {
        _cache = cache;
        _queue = queue;

        events.CommentCreated += OnCommentCreated;
    }

    private void OnCommentCreated(object? sender, CommentCreatedEventArgs e)
    {
        // the cached pages do not have the new comment, so they are removed at once
        _cache.Clear();

        // the other users are told about the comment later, by the background worker,
        // so the request of the author does not wait for it
        _queue.Enqueue(new CommentNotification { CommentId = e.CommentId, ParentId = e.ParentId });
    }
}
