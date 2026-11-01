namespace ArdaNova.Application.Feed;

using System.Text;
using System.Text.RegularExpressions;

/// <summary>#tag extraction and normalisation for post copy; see Feed/AGENTS.md §Hashtags.</summary>
public static partial class Hashtags
{
    public const int MaxPerPost = 10;

    /// <summary>
    /// A '#' that starts a token - not mid-word, not a URL fragment ("/#anchor"), not an HTML entity ("&amp;#39;"),
    /// not a second '#' - then 2-40 tag characters and nothing more of them. "# Title" (a markdown heading) never
    /// matches because a space is not a tag character.
    /// </summary>
    /// <remarks>\p{M} joins the owner's [\p{L}\p{N}_-] so scripts whose letters carry combining marks (Devanagari,
    /// Thai, ...) are not cut mid-word; NFKC has already composed every mark that has a precomposed form.</remarks>
    [GeneratedRegex(
        @"(?<![\p{L}\p{N}\p{M}_/&#-])#([\p{L}\p{N}\p{M}_-]{2,40})(?![\p{L}\p{N}\p{M}_-])",
        RegexOptions.CultureInvariant)]
    private static partial Regex TagInCopy();

    [GeneratedRegex(@"^[\p{L}\p{N}\p{M}_-]{2,40}$", RegexOptions.CultureInvariant)]
    private static partial Regex WholeTag();

    /// <summary>The tags in the given copy: lowercased, NFKC, deduped, first-seen order, at most <see cref="MaxPerPost"/>.</summary>
    public static string[] Extract(params string?[] copy)
    {
        var text = string.Join('\n', copy.Where(part => !string.IsNullOrEmpty(part)));
        if (text.Length == 0)
            return [];

        var tags = new List<string>(MaxPerPost);
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (Match match in TagInCopy().Matches(text.Normalize(NormalizationForm.FormKC)))
        {
            if (Canonical(match.Groups[1].Value) is { } tag && seen.Add(tag))
            {
                tags.Add(tag);
                if (tags.Count == MaxPerPost)
                    break;
            }
        }

        return tags.ToArray();
    }

    /// <summary>One tag as a caller typed it ("#Carpentry", "carpentry"), in stored form; null when it is not a tag.</summary>
    public static string? Normalize(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return null;

        var text = input.Normalize(NormalizationForm.FormKC).Trim();
        if (text.StartsWith('#'))
            text = text[1..];
        return WholeTag().IsMatch(text) ? Canonical(text) : null;
    }

    /// <summary>Lowercase, with dashes and underscores trimmed off the ends ("#roofing-" is "roofing").</summary>
    private static string? Canonical(string raw)
    {
        var trimmed = raw.Trim('-', '_').ToLowerInvariant();
        return trimmed.Length >= 2 ? trimmed : null;
    }
}
