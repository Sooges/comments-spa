namespace CommentsApi.Services;

public class AttachmentResult
{
    public bool IsValid { get; private init; }
    public PreparedAttachment? Attachment { get; private init; }
    public string? Error { get; private init; }

    public static AttachmentResult Success(PreparedAttachment attachment)
    {
        return new AttachmentResult { IsValid = true, Attachment = attachment };
    }

    public static AttachmentResult Fail(string error)
    {
        return new AttachmentResult { IsValid = false, Error = error };
    }
}
