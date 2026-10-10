namespace CommentsApi.Dtos;

public class AttachmentDto
{
    public int Id { get; set; }

    // "image" or "text"
    public string Kind { get; set; } = string.Empty;

    public string FileName { get; set; } = string.Empty;
    public long Size { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
}
