namespace CommentsApi.Models;

public class Comment
{
    public int Id { get; set; }

    // null for top-level comments, otherwise the comment this one replies to
    public int? ParentId { get; set; }
    public Comment? Parent { get; set; }
    public List<Comment> Replies { get; set; } = new();

    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? HomePage { get; set; }

    public string Text { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    // an image or a text file, a comment has not more than one
    public Attachment? Attachment { get; set; }

    // data that helps to identify the client
    public string IpAddress { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;
}
