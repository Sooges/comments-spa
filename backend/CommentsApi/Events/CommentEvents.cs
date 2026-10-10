namespace CommentsApi.Events;

public class CommentEvents : ICommentEvents
{
    private readonly ILogger<CommentEvents> _logger;

    public CommentEvents(ILogger<CommentEvents> logger)
    {
        _logger = logger;
    }

    public event EventHandler<CommentCreatedEventArgs>? CommentCreated;

    public void RaiseCommentCreated(int commentId, int? parentId)
    {
        var handlers = CommentCreated;
        if (handlers == null)
        {
            return;
        }

        var args = new CommentCreatedEventArgs { CommentId = commentId, ParentId = parentId };

        // the comment is already saved, so a failing subscriber must not break the request
        foreach (var handler in handlers.GetInvocationList().Cast<EventHandler<CommentCreatedEventArgs>>())
        {
            try
            {
                handler(this, args);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "A subscriber of CommentCreated failed for the comment {CommentId}", commentId);
            }
        }
    }
}
