namespace CommentsApi.Dtos;

public class CommentDto
{
    public int Id { get; set; }
    public int? ParentId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? HomePage { get; set; }
    public string Text { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public AttachmentDto? Attachment { get; set; }
    public List<CommentDto> Replies { get; set; } = new();
}
