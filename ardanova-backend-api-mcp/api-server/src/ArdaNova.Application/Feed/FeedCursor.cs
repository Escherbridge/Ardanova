namespace ArdaNova.Application.Feed;

using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>A keyset position in the feed's (createdAt desc, id desc) order.</summary>
public sealed record FeedKey(DateTime CreatedAt, string Id);

/// <summary>How a scope orders its cards: ranked windows (home, tag) or plain recency (one project, one guild).</summary>
public enum FeedPagingMode
{
    Ranked = 0,
    Recency = 1,
}

/// <summary>
/// Where an infinite scroll stands: the pinned <see cref="AsOf"/>, the open window [Floor, Top) in keyset order,
/// and fingerprints of the window's cards already served. Opaque to clients and unsigned on purpose: every field
/// can only narrow or skip, because each page re-applies visibility; see Feed/AGENTS.md §Paging.
/// </summary>
/// <param name="Top">Ranked: exclusive upper bound of the open window (null = from <see cref="AsOf"/>).
/// Recency: the last card served; the next page starts strictly below it.</param>
/// <param name="Floor">Ranked: inclusive lower bound of the open window; null = no window open yet. Recency: unused.</param>
/// <param name="Exhausted">Nothing visible was left below <see cref="Floor"/> when it was last looked for.</param>
/// <param name="Mode">Bound to the scope's mode, so a cursor from one scope is refused on another.</param>
public sealed record FeedCursor(
    DateTime AsOf,
    string Scope,
    FeedKey? Top,
    FeedKey? Floor,
    IReadOnlySet<ulong> Served,
    bool Exhausted,
    FeedPagingMode Mode = FeedPagingMode.Ranked)
{
    public const int Version = 1;

    /// <summary>The most served fingerprints a cursor may carry (the window cap plus a page of skips).</summary>
    public const int MaxServed = 256;

    private const int FingerprintBytes = 6;
    private const int MaxIdLength = 64;
    private const int MaxScopeLength = 128;

    /// <summary>A 48-bit SHA-256 prefix of a post id: 6 bytes in the cursor instead of 36, collision odds ~1e-10 per window.</summary>
    /// <remarks>A collision can only make a card look served, i.e. skip it; it can never reveal one.</remarks>
    public static ulong Fingerprint(string postId)
    {
        Span<byte> hash = stackalloc byte[32];
        SHA256.HashData(Encoding.UTF8.GetBytes(postId), hash);
        Span<byte> padded = stackalloc byte[8];
        hash[..FingerprintBytes].CopyTo(padded);
        return BinaryPrimitives.ReadUInt64LittleEndian(padded);
    }

    public string Encode()
    {
        var served = new byte[Served.Count * FingerprintBytes];
        var offset = 0;
        Span<byte> buffer = stackalloc byte[8];
        foreach (var fingerprint in Served.Order())
        {
            BinaryPrimitives.WriteUInt64LittleEndian(buffer, fingerprint);
            buffer[..FingerprintBytes].CopyTo(served.AsSpan(offset));
            offset += FingerprintBytes;
        }

        var wire = new Wire
        {
            V = Version,
            A = AsOf.Ticks,
            S = Scope,
            T = Top is { } top ? new WireKey { C = top.CreatedAt.Ticks, I = top.Id } : null,
            F = Floor is { } floor ? new WireKey { C = floor.CreatedAt.Ticks, I = floor.Id } : null,
            X = served.Length == 0 ? null : Convert.ToBase64String(served),
            E = Exhausted ? true : null,
            M = (int)Mode,
        };
        return Base64UrlEncode(JsonSerializer.SerializeToUtf8Bytes(wire, WireOptions));
    }

    /// <summary>Parses a cursor this API issued; anything malformed, oversized or out of range is refused.</summary>
    public static bool TryDecode(string? text, out FeedCursor? cursor)
    {
        cursor = null;
        if (string.IsNullOrWhiteSpace(text) || text.Length > 4096)
            return false;

        try
        {
            var wire = JsonSerializer.Deserialize<Wire>(Base64UrlDecode(text), WireOptions);
            if (wire is null || wire.V != Version || !IsTicks(wire.A)
                || string.IsNullOrEmpty(wire.S) || wire.S.Length > MaxScopeLength
                || !Enum.IsDefined(typeof(FeedPagingMode), wire.M))
                return false;

            if (!TryKey(wire.T, out var top) || !TryKey(wire.F, out var floor))
                return false;

            var served = new HashSet<ulong>();
            if (wire.X is { Length: > 0 } packed)
            {
                var bytes = Convert.FromBase64String(packed);
                if (bytes.Length % FingerprintBytes != 0 || bytes.Length / FingerprintBytes > MaxServed)
                    return false;
                Span<byte> buffer = stackalloc byte[8];
                for (var offset = 0; offset < bytes.Length; offset += FingerprintBytes)
                {
                    buffer.Clear();
                    bytes.AsSpan(offset, FingerprintBytes).CopyTo(buffer);
                    served.Add(BinaryPrimitives.ReadUInt64LittleEndian(buffer));
                }
            }

            cursor = new FeedCursor(Utc(wire.A), wire.S, top, floor, served, wire.E == true, (FeedPagingMode)wire.M);
            return true;
        }
        catch (Exception malformed) when (malformed is FormatException or JsonException or ArgumentException)
        {
            return false;
        }
    }

    private static bool TryKey(WireKey? wire, out FeedKey? key)
    {
        key = null;
        if (wire is null)
            return true;
        if (!IsTicks(wire.C) || string.IsNullOrEmpty(wire.I) || wire.I.Length > MaxIdLength)
            return false;
        key = new FeedKey(Utc(wire.C), wire.I);
        return true;
    }

    private static bool IsTicks(long ticks) => ticks >= DateTime.MinValue.Ticks && ticks <= DateTime.MaxValue.Ticks;

    private static DateTime Utc(long ticks) => new(ticks, DateTimeKind.Utc);

    private static string Base64UrlEncode(byte[] bytes)
        => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static byte[] Base64UrlDecode(string text)
    {
        var padded = text.Replace('-', '+').Replace('_', '/');
        padded += (padded.Length % 4) switch { 2 => "==", 3 => "=", 0 => "", _ => throw new FormatException() };
        return Convert.FromBase64String(padded);
    }

    private static readonly JsonSerializerOptions WireOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private sealed class Wire
    {
        [JsonPropertyName("v")] public int V { get; set; }
        [JsonPropertyName("a")] public long A { get; set; }
        [JsonPropertyName("s")] public string? S { get; set; }
        [JsonPropertyName("t")] public WireKey? T { get; set; }
        [JsonPropertyName("f")] public WireKey? F { get; set; }
        [JsonPropertyName("x")] public string? X { get; set; }
        [JsonPropertyName("e")] public bool? E { get; set; }
        [JsonPropertyName("m")] public int M { get; set; }
    }

    private sealed class WireKey
    {
        [JsonPropertyName("c")] public long C { get; set; }
        [JsonPropertyName("i")] public string? I { get; set; }
    }
}
