using System.Text;
using System.Text.RegularExpressions;

namespace CommentsApi.Services;

public class HtmlTextProcessor : IHtmlTextProcessor
{
    private const int MaxTagLengthInMessage = 40;
    private static readonly TimeSpan RegexTimeout = TimeSpan.FromSeconds(1);
    private const RegexOptions Options = RegexOptions.IgnoreCase | RegexOptions.CultureInvariant;

    // everything that looks like a tag: <...>
    private static readonly Regex AnyTag = new(@"<[^<>]*>", Options, RegexTimeout);

    // </a>, </code>, </i>, </strong>
    private static readonly Regex ClosingTag = new(@"^</\s*(a|code|i|strong)\s*>$", Options, RegexTimeout);

    // <code>, <i>, <strong> (no attributes allowed)
    private static readonly Regex SimpleOpeningTag = new(@"^<\s*(code|i|strong)\s*>$", Options, RegexTimeout);

    // <a href="..." title="...">, the attributes are captured in the first group
    private static readonly Regex LinkOpeningTag =
        new(@"^<\s*a((?:\s+(?:href|title)\s*=\s*""[^""]*"")*)\s*>$", Options, RegexTimeout);

    private static readonly Regex LinkAttribute =
        new(@"(href|title)\s*=\s*""([^""]*)""", Options, RegexTimeout);

    public HtmlTextResult Process(string text)
    {
        var html = new StringBuilder();
        var openTags = new Stack<string>();
        var position = 0;

        foreach (Match tag in AnyTag.Matches(text))
        {
            // plain text before the tag
            var textError = AppendText(html, text.Substring(position, tag.Index - position));
            if (textError != null)
            {
                return HtmlTextResult.Fail(textError);
            }

            position = tag.Index + tag.Length;

            var tagError = AppendTag(html, openTags, tag.Value);
            if (tagError != null)
            {
                return HtmlTextResult.Fail(tagError);
            }
        }

        var lastTextError = AppendText(html, text.Substring(position));
        if (lastTextError != null)
        {
            return HtmlTextResult.Fail(lastTextError);
        }

        if (openTags.Count > 0)
        {
            return HtmlTextResult.Fail($"Тег <{openTags.Peek()}> не закрыт.");
        }

        return HtmlTextResult.Success(html.ToString());
    }

    private static string? AppendText(StringBuilder html, string text)
    {
        // a "<" that is not a part of an allowed tag
        if (text.Contains('<'))
        {
            return "Символ \"<\" можно использовать только в разрешённых тегах: a, code, i, strong.";
        }

        html.Append(Encode(text));
        return null;
    }

    private static string? AppendTag(StringBuilder html, Stack<string> openTags, string tag)
    {
        var closing = ClosingTag.Match(tag);
        if (closing.Success)
        {
            var name = closing.Groups[1].Value.ToLowerInvariant();
            if (openTags.Count == 0 || openTags.Peek() != name)
            {
                return $"Тег </{name}> закрыт неверно: проверьте порядок и вложенность тегов.";
            }

            openTags.Pop();
            html.Append($"</{name}>");
            return null;
        }

        var simple = SimpleOpeningTag.Match(tag);
        if (simple.Success)
        {
            var name = simple.Groups[1].Value.ToLowerInvariant();
            openTags.Push(name);
            html.Append($"<{name}>");
            return null;
        }

        var link = LinkOpeningTag.Match(tag);
        if (link.Success)
        {
            return AppendLink(html, openTags, link.Groups[1].Value);
        }

        return $"Тег {Shorten(tag)} не разрешён. Можно использовать только " +
               "<a href=\"\" title=\"\">, <code>, <i> и <strong>.";
    }

    private static string? AppendLink(StringBuilder html, Stack<string> openTags, string attributes)
    {
        if (openTags.Contains("a"))
        {
            return "Ссылки нельзя вкладывать друг в друга.";
        }

        string? href = null;
        string? title = null;

        foreach (Match attribute in LinkAttribute.Matches(attributes))
        {
            var name = attribute.Groups[1].Value.ToLowerInvariant();
            var value = attribute.Groups[2].Value;

            if (name == "href")
            {
                if (href != null)
                {
                    return "Атрибут href указан дважды.";
                }

                href = value;
            }
            else
            {
                if (title != null)
                {
                    return "Атрибут title указан дважды.";
                }

                title = value;
            }
        }

        // only absolute http(s) links, so javascript: and similar are not possible
        if (string.IsNullOrWhiteSpace(href)
            || !Uri.TryCreate(href.Trim(), UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            return "У ссылки <a> должен быть корректный адрес в атрибуте href (http:// или https://).";
        }

        openTags.Push("a");

        html.Append($"<a href=\"{Encode(uri.AbsoluteUri)}\"");
        if (!string.IsNullOrWhiteSpace(title))
        {
            html.Append($" title=\"{Encode(title.Trim())}\"");
        }

        html.Append(" rel=\"nofollow noopener noreferrer\" target=\"_blank\">");
        return null;
    }

    private static string Encode(string value)
    {
        return value
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;")
            .Replace("'", "&#39;");
    }

    private static string Shorten(string tag)
    {
        return tag.Length <= MaxTagLengthInMessage ? tag : tag[..MaxTagLengthInMessage] + "...>";
    }
}
