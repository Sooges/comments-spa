namespace CommentsApi.Dtos;

public class CaptchaResponse
{
    public string Id { get; set; } = string.Empty;

    // PNG picture as a data URI, can be used as the src of an img tag
    public string Image { get; set; } = string.Empty;
}
