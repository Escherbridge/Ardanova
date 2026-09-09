namespace ArdaNova.Application.Common.Security;

using System;
using System.Text;

/// <summary>
/// The owner segment a storage key carries, minted and checked in ONE place.
/// </summary>
/// <remarks>
/// <para>
/// The attachment registration route decides whether a caller may claim a bucket path by
/// reading this segment out of the key. That is only a proof if the minting side and the
/// checking side agree on its exact shape, and if a key WITHOUT one is refused: the check
/// used to skip every segment that did not begin with the prefix and then answer true, so a
/// path with no owner segment at all belonged to everybody. It also compared the first eight
/// characters of an id, which is a naming convention, not an identity.
/// </para>
/// <para>
/// So the segment now carries the WHOLE actor id, sanitised to characters a key may hold, and
/// <see cref="PathBelongsTo"/> requires a segment that matches - never merely the absence of
/// one that does not.
/// </para>
/// </remarks>
public static class UserStoragePrefix
{
    /// <summary>What every owner segment starts with.</summary>
    public const string SegmentPrefix = "user_";

    /// <summary>The owner segment for one actor, or null when the id cannot produce one.</summary>
    public static string? SegmentFor(string? userId)
    {
        var sanitized = Sanitize(userId);
        return sanitized is null ? null : SegmentPrefix + sanitized;
    }

    /// <summary>
    /// Whether a stored key is stamped for this actor. Requires a matching owner segment to be
    /// PRESENT: a key with no owner segment belongs to nobody, so it is refused.
    /// </summary>
    public static bool PathBelongsTo(string? bucketPath, string? actorId)
    {
        if (string.IsNullOrEmpty(bucketPath))
            return false;

        var expected = SegmentFor(actorId);
        if (expected is null)
            return false;

        var claimed = false;
        foreach (var segment in bucketPath.Split(
            new[] { '/', '\\' },
            StringSplitOptions.RemoveEmptyEntries))
        {
            if (!segment.StartsWith(SegmentPrefix, StringComparison.Ordinal))
                continue;

            if (!string.Equals(segment, expected, StringComparison.Ordinal))
                return false;

            claimed = true;
        }

        return claimed;
    }

    /// <summary>
    /// The id as a single key segment: letters, digits, dash and underscore survive, everything
    /// else becomes a dash. Null when nothing usable is left, so a blank actor can never mint or
    /// match a segment.
    /// </summary>
    private static string? Sanitize(string? userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return null;

        var builder = new StringBuilder(userId.Length);
        foreach (var character in userId.Trim())
        {
            builder.Append(
                character < 128 && (char.IsLetterOrDigit(character) || character is '-' or '_')
                    ? character
                    : '-');
        }

        var sanitized = builder.ToString().Trim('-');
        return sanitized.Length == 0 ? null : sanitized;
    }
}
