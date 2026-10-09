namespace CommentsApi.Services;

public class HtmlTextResult
{
    public bool IsValid { get; private init; }

    /// <summary>
    /// Safe HTML built from the comment text. Empty if the text is not valid.
    /// </summary>
    public string Html { get; private init; } = string.Empty;

    public string? Error { get; private init; }

    public static HtmlTextResult Success(string html)
    {
        return new HtmlTextResult { IsValid = true, Html = html };
    }

    public static HtmlTextResult Fail(string error)
    {
        return new HtmlTextResult { IsValid = false, Error = error };
    }
}
