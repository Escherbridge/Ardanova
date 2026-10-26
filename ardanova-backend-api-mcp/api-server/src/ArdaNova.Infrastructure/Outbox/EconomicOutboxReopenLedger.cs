namespace ArdaNova.Infrastructure.Outbox;

using System.Globalization;
using System.Text.RegularExpressions;

/// <summary>Carries an outbox row's no-effect reopen count as a trailer on its operator-only failure detail.</summary>
/// <remarks>A schema-free stand-in for a dedicated column; see <c>AGENTS.md</c>, "No-effect reopen".</remarks>
internal static partial class EconomicOutboxReopenLedger
{
    private const string TrailerPrefix = "[noEffectReopens=";

    /// <summary>The reopen count a row's failure detail carries, or zero.</summary>
    public static int Read(string? failureDetail)
    {
        if (string.IsNullOrEmpty(failureDetail))
            return 0;

        var match = Trailer().Match(failureDetail);
        return match.Success
            && int.TryParse(match.Groups[1].Value, NumberStyles.None, CultureInfo.InvariantCulture, out var count)
            ? count
            : 0;
    }

    /// <summary>The detail to persist: the new text plus the trailer whenever the count is non-zero.</summary>
    public static string? Stamp(string? failureDetail, int reopenCount)
    {
        var text = failureDetail is null ? null : Trailer().Replace(failureDetail, string.Empty).TrimEnd();
        if (reopenCount <= 0)
            return text;

        var trailer = TrailerPrefix + reopenCount.ToString(CultureInfo.InvariantCulture) + "]";
        return string.IsNullOrEmpty(text) ? trailer : text + " " + trailer;
    }

    [GeneratedRegex(@"\s*\[noEffectReopens=(\d{1,9})\]\s*$", RegexOptions.CultureInvariant)]
    private static partial Regex Trailer();
}
