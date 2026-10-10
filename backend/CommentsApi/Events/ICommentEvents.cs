namespace CommentsApi.Events;

public interface ICommentEvents
{
    /// <summary>
    /// Happens after a new comment was saved in the database.
    /// </summary>
    event EventHandler<CommentCreatedEventArgs>? CommentCreated;

    void RaiseCommentCreated(int commentId, int? parentId);
}
