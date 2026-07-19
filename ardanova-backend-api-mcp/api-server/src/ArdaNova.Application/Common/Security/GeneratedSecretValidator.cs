namespace ArdaNova.Application.Common.Security;

using System.Diagnostics.CodeAnalysis;
using System.Text;

public static class GeneratedSecretValidator
{
    public const int MinimumUtf8Bytes = 32;

    private static readonly string[] UnsafeMarkers =
    [
        "replace-with",
        "your-api-key",
        "your-admin-api-key",
        "your-actor-assertion",
        "your-auth-secret",
        "placeholder",
        "change-me",
        "changeme",
        "not-a-secret",
    ];

    private static readonly HashSet<string> PublishedExamples = new(StringComparer.OrdinalIgnoreCase)
    {
        "tenant-provision-wallet-manage-kyc-only-key",
        "nft-mint-only-key",
        "dapp-develop-only-key",
    };

    public static bool IsValid(
        [NotNullWhen(true)] string? secret,
        int minimumUtf8Bytes = MinimumUtf8Bytes)
    {
        if (minimumUtf8Bytes < 1 || string.IsNullOrWhiteSpace(secret))
            return false;

        var candidate = secret.Trim();
        if (candidate.Length >= 2
            && ((candidate[0] == '"' && candidate[^1] == '"')
                || (candidate[0] == '\'' && candidate[^1] == '\'')))
        {
            candidate = candidate[1..^1].Trim();
        }

        if (Encoding.UTF8.GetByteCount(candidate) < minimumUtf8Bytes)
            return false;

        if (PublishedExamples.Contains(candidate))
            return false;

        return !UnsafeMarkers.Any(marker =>
            candidate.Contains(marker, StringComparison.OrdinalIgnoreCase));
    }
}
