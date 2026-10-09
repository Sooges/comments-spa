namespace CommentsApi.Services;

public interface IHtmlTextProcessor
{
    /// <summary>
    /// Checks the comment text and turns it into safe HTML.
    /// Only a, code, i and strong tags are allowed and they must be closed correctly.
    /// </summary>
    HtmlTextResult Process(string text);
}
