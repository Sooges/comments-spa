namespace CommentsApi.Events;

public class CommentCreatedEventArgs : EventArgs
{
    public int CommentId { get; init; }

    // null if it is a top-level comment
    public int? ParentId { get; init; }
}
