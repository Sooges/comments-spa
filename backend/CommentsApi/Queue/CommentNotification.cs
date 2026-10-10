namespace CommentsApi.Queue;

// A message for the other users: "a new comment appeared".
public class CommentNotification
{
    public int CommentId { get; init; }
    public int? ParentId { get; init; }
}
