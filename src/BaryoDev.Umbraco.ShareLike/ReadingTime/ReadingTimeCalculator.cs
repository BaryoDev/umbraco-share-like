using System.Text;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Models.PublishedContent;

namespace BaryoDev.Umbraco.ShareLike.ReadingTime;

public interface IReadingTimeCalculator
{
    /// <summary>Whole minutes, at least 1. Null when the property is missing or empty.</summary>
    int? Minutes(IPublishedContent content, string? propertyAlias = null);

    string? Format(IPublishedContent content, string? propertyAlias = null);
}

public sealed class ReadingTimeCalculator(IOptionsMonitor<ShareLikeOptions> options) : IReadingTimeCalculator
{
    public int? Minutes(IPublishedContent content, string? propertyAlias = null)
    {
        var o = options.CurrentValue;
        var value = content.GetProperty(propertyAlias ?? o.ReadingTimeProperty)?.GetValue();
        return value is null ? null : MinutesFor(value.ToString(), o.WordsPerMinute);
    }

    public string? Format(IPublishedContent content, string? propertyAlias = null)
    {
        var minutes = Minutes(content, propertyAlias);
        return minutes is null ? null : string.Format(options.CurrentValue.ReadingTimeFormat, minutes);
    }

    public static int? MinutesFor(string? html, int wordsPerMinute)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return null;
        }

        var text = System.Net.WebUtility.HtmlDecode(StripTags(html));
        var words = text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;
        if (words == 0)
        {
            return null;
        }

        return Math.Max(1, (int)Math.Ceiling(words / (double)Math.Max(1, wordsPerMinute)));
    }

    // One pass. A '<' with no '>' after it cannot start a tag, and neither can any later '<'.
    private static string StripTags(string html)
    {
        var sb = new StringBuilder(html.Length);
        var pos = 0;
        while (pos < html.Length)
        {
            var open = html.IndexOf('<', pos);
            if (open < 0)
            {
                break;
            }

            var close = html.IndexOf('>', open + 1);
            if (close < 0)
            {
                break;
            }

            sb.Append(html, pos, open - pos).Append(' ');
            pos = close + 1;
        }

        return sb.Append(html, pos, html.Length - pos).ToString();
    }
}
