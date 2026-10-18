namespace ArdaNova.Application.Services.Implementations;

using System.Buffers.Binary;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

/// <summary>Stateless session ownership: the state a member carries is HMAC-bound to (actor, session, expiry). See Infrastructure/Ogai/AGENTS.md §Session tickets.</summary>
public sealed class NovaSessionTickets
{
    /// <summary>Stays inside the upstream's 2 h session lifetime, with margin for the time the first turn took.</summary>
    public static readonly TimeSpan SessionLifetime = TimeSpan.FromHours(2) - TimeSpan.FromMinutes(5);

    public const int MinKeyBytes = 32;

    /// <summary>Largest upstream state carried; the client twin is OGAI_STATE_MAX_CHARS (src/lib/nova-state-limits.ts).</summary>
    public const int MaxUpstreamStateLength = 512;

    /// <summary>"nv1." + 10-digit unix expiry + "." + 64 hex HMAC + ".": the fixed part of every ticket.</summary>
    public const int EnvelopeLength = 4 + 10 + 1 + 64 + 1;

    /// <summary>Longest ticket a member can legitimately present; the client twin is NOVA_STATE_MAX_CHARS.</summary>
    public const int MaxTicketLength = EnvelopeLength + MaxUpstreamStateLength;
    private const string Prefix = "nv1";
    private const string Domain = "ardanova/nova-session-ticket/v1";

    private readonly byte[] _key;
    private readonly TimeProvider _time;

    public NovaSessionTickets(byte[] key, TimeProvider time)
    {
        if (key.Length < MinKeyBytes)
            throw new ArgumentException($"A Nova session key needs at least {MinKeyBytes} bytes.", nameof(key));
        _key = key;
        _time = time;
    }

    /// <summary>When a session started now stops being continuable.</summary>
    public DateTimeOffset NewExpiry() => _time.GetUtcNow() + SessionLifetime;

    /// <summary>Wraps the upstream's state so only this actor can continue this session until expiresAt.</summary>
    public string Issue(string actorId, string sessionId, string upstreamState, DateTimeOffset expiresAt)
    {
        var expires = expiresAt.ToUnixTimeSeconds();
        return string.Join('.', Prefix, expires.ToString(CultureInfo.InvariantCulture), Mac(actorId, sessionId, expires, upstreamState), upstreamState);
    }

    /// <summary>The upstream state and expiry inside a ticket, or false when it is not this actor's live ticket for this session.</summary>
    public bool TryOpen(string? ticket, string actorId, string sessionId, out string upstreamState, out DateTimeOffset expiresAt)
    {
        upstreamState = string.Empty;
        expiresAt = default;
        if (ticket is null || ticket.Length > MaxTicketLength)
            return false;
        var parts = ticket.Split('.', 4);
        if (parts.Length != 4 || parts[0] != Prefix
            || !long.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out var expires))
            return false;

        var expected = Encoding.ASCII.GetBytes(Mac(actorId, sessionId, expires, parts[3]));
        if (!CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(parts[2]), expected))
            return false;

        expiresAt = DateTimeOffset.FromUnixTimeSeconds(expires);
        if (expiresAt <= _time.GetUtcNow())
            return false;
        upstreamState = parts[3];
        return true;
    }

    /// <summary>HMAC-SHA256 over length-prefixed fields, so no two field lists collide.</summary>
    private string Mac(string actorId, string sessionId, long expires, string upstreamState)
    {
        using var mac = new HMACSHA256(_key);
        var buffer = new List<byte>();
        foreach (var field in new[] { Domain, actorId, sessionId, expires.ToString(CultureInfo.InvariantCulture), upstreamState })
        {
            var bytes = Encoding.UTF8.GetBytes(field);
            var length = new byte[4];
            BinaryPrimitives.WriteInt32BigEndian(length, bytes.Length);
            buffer.AddRange(length);
            buffer.AddRange(bytes);
        }
        return Convert.ToHexString(mac.ComputeHash(buffer.ToArray()));
    }

    /// <summary>Derives the ticket key from a configured secret (HKDF, domain-separated), or a random per-process key when none is set.</summary>
    public static byte[] DeriveKey(string? secret)
        => string.IsNullOrWhiteSpace(secret) || Encoding.UTF8.GetByteCount(secret) < MinKeyBytes
            ? RandomNumberGenerator.GetBytes(MinKeyBytes)
            : HKDF.DeriveKey(HashAlgorithmName.SHA256, Encoding.UTF8.GetBytes(secret), MinKeyBytes, info: Encoding.UTF8.GetBytes(Domain));
}
