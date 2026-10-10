using CommentsApi.Services;

namespace CommentsApi.Events;

// Listens to the comment events. It is created when the application starts (see Program.cs).
public class CommentEventSubscriber
{
    private readonly ICommentCache _cache;

    public CommentEventSubscriber(ICommentEvents events, ICommentCache cache)
    {
        _cache = cache;

        events.CommentCreated += OnCommentCreated;
    }

    private void OnCommentCreated(object? sender, CommentCreatedEventArgs e)
    {
        // the cached pages do not have the new comment, so they are removed at once
        _cache.Clear();
    }
}
