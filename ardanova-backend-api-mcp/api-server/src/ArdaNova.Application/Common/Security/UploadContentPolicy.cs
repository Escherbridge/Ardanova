namespace ArdaNova.Application.Common.Security;

using System.Text.RegularExpressions;

/// <summary>Bounds on what a signed storage URL may carry. See Application/Common/AGENTS.md §Upload content policy.</summary>
public static partial class UploadContentPolicy
{
    public const int MinimumDownloadMinutes = 1;
    public const int MaximumDownloadMinutes = 60;

    /// <summary>Types a browser executes when opened from the bucket's origin.</summary>
    private static readonly HashSet<string> ActiveContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "text/html", "application/xhtml+xml", "image/svg+xml", "text/xml", "application/xml",
        "text/javascript", "application/javascript", "application/ecmascript", "text/ecmascript",
        "application/x-javascript", "application/wasm", "multipart/x-mixed-replace",
    };

    /// <summary>A well-formed type/subtype that a browser will not run as script or markup.</summary>
    public static bool IsAllowedContentType(string? contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType) || contentType.Length > 127)
            return false;
        var essence = contentType.Split(';', 2)[0].Trim();
        return MediaType().IsMatch(essence) && !ActiveContentTypes.Contains(essence);
    }

    public static int ClampDownloadMinutes(int requested)
        => Math.Clamp(requested, MinimumDownloadMinutes, MaximumDownloadMinutes);

    /// <summary>A filename safe to quote inside Content-Disposition: no quotes, backslashes or control characters.</summary>
    public static string SafeAttachmentName(string fileName)
    {
        var cleaned = new string(fileName.Where(c => !char.IsControl(c) && c is not ('"' or '\\')).ToArray()).Trim();
        return cleaned.Length == 0 ? "download" : cleaned;
    }

    [GeneratedRegex(@"^[A-Za-z0-9][A-Za-z0-9!#$&^_.+-]{0,63}/[A-Za-z0-9][A-Za-z0-9!#$&^_.+-]{0,63}$")]
    private static partial Regex MediaType();
}
