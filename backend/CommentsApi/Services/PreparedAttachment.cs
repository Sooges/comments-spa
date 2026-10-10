using CommentsApi.Models;

namespace CommentsApi.Services;

/// <summary>
/// An uploaded file that has been checked (and resized if it is a big image),
/// it is ready to be saved.
/// </summary>
public class PreparedAttachment
{
    public AttachmentKind Kind { get; init; }
    public string OriginalName { get; init; } = string.Empty;
    public string ContentType { get; init; } = string.Empty;

    // with the dot, for example ".png"
    public string Extension { get; init; } = string.Empty;

    public byte[] Content { get; init; } = Array.Empty<byte>();

    public int? Width { get; init; }
    public int? Height { get; init; }
}
