namespace CommentsApi.Services;

public interface IAttachmentProcessor
{
    /// <summary>
    /// Checks an uploaded file. Allowed files are images (JPG, GIF, PNG), which are made smaller
    /// if they are bigger than 320x240, and text files (TXT) up to 100 KB.
    /// </summary>
    Task<AttachmentResult> ProcessAsync(IFormFile file);
}
