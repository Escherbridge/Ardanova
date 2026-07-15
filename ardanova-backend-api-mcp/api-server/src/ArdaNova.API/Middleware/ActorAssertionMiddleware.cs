using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ArdaNova.Application.Common.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

namespace ArdaNova.API.Middleware;

/// <summary>Verifies and consumes single-use actor assertions from the trusted Next.js BFF.</summary>
public sealed class ActorAssertionMiddleware
{
    public const string HeaderName = "X-Ardanova-Actor";
    public const string ClaimType = "ardanova:actor-assertion";
    public const string AuthenticationType = "ActorAssertion";
    private const string ExpectedIssuer = "ardanova-next-bff";
    private const string ExpectedAudience = "ardanova-api";
    private const int MaximumLifetimeSeconds = 120;
    private const int ClockSkewSeconds = 30;
    private const int MaximumBodyBytes = 1024 * 1024;
    private const int MaximumHeaderValueLength = 512;
    private readonly RequestDelegate _next;
    private readonly IConfiguration _configuration;
    private readonly IActorAssertionReplayLedger _replayLedger;
    private readonly ILogger<ActorAssertionMiddleware> _logger;

    public ActorAssertionMiddleware(
        RequestDelegate next,
        IConfiguration configuration,
        IActorAssertionReplayLedger replayLedger,
        ILogger<ActorAssertionMiddleware> logger)
    {
        _next = next;
        _configuration = configuration;
        _replayLedger = replayLedger;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Headers.TryGetValue(HeaderName, out var supplied))
        {
            await _next(context);
            return;
        }

        var key = GetSigningKey();
        if (string.IsNullOrWhiteSpace(key))
        {
            _logger.LogError("Actor assertion was supplied but no signing key is configured.");
            await RejectAsync(context, StatusCodes.Status500InternalServerError, "Actor assertion authentication is not configured.");
            return;
        }

        if (supplied.Count != 1 || string.IsNullOrWhiteSpace(supplied[0])
            || !TryGetNormalizedHeader(context.Request.Headers, "Content-Type", out var contentType)
            || !TryGetOptionalHeader(context.Request.Headers, "X-Idempotency-Key", out var idempotencyKey)
            || contentType.StartsWith("multipart/", StringComparison.Ordinal))
        {
            await RejectAsync(context, StatusCodes.Status401Unauthorized, "Invalid actor assertion.");
            return;
        }

        var body = await ReadBodyDigestAsync(context.Request, context.RequestAborted);
        if (body.IsTooLarge)
        {
            await RejectAsync(context, StatusCodes.Status413PayloadTooLarge, "Actor-signed request body is too large.");
            return;
        }

        if (!TryValidate(supplied[0]!, key, context.Request, contentType, idempotencyKey, body.Sha256, out var actor))
        {
            _logger.LogWarning("Rejected invalid actor assertion for {Method} {Target}", context.Request.Method, RequestTarget(context.Request));
            await RejectAsync(context, StatusCodes.Status401Unauthorized, "Invalid actor assertion.");
            return;
        }

        var replayClaim = await _replayLedger.TryConsumeAsync(
            new ActorAssertionReplayEntry(
                actor.Jti,
                DateTimeOffset.FromUnixTimeSeconds(actor.ExpiresAt).UtcDateTime,
                DateTime.UtcNow,
                actor.Subject,
                actor.RequestTarget,
                actor.BodySha256),
            context.RequestAborted);
        if (replayClaim != ActorAssertionReplayClaim.Consumed)
        {
            _logger.LogWarning("Rejected replayed actor assertion for {Subject} {Target}", actor.Subject, actor.RequestTarget);
            await RejectAsync(context, StatusCodes.Status401Unauthorized, "Invalid actor assertion.");
            return;
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, actor.Subject),
            new("sub", actor.Subject),
            new(ClaimType, "v2")
        };
        if (!string.IsNullOrWhiteSpace(actor.Role))
        {
            claims.Add(new Claim("ardanova:role", actor.Role));
            claims.Add(new Claim(ClaimTypes.Role, actor.Role));
        }

        context.User.AddIdentity(new ClaimsIdentity(claims, AuthenticationType));
        await _next(context);
    }

    private string? GetSigningKey()
    {
        var environmentKey = Environment.GetEnvironmentVariable("ACTOR_ASSERTION_HMAC_KEY");
        var signingKey = string.IsNullOrWhiteSpace(environmentKey)
            ? _configuration["ActorAssertion:HmacKey"]
            : environmentKey;
        return string.IsNullOrWhiteSpace(signingKey) || Encoding.UTF8.GetByteCount(signingKey) < 32
            ? null
            : signingKey;
    }

    private static async Task<BodyDigest> ReadBodyDigestAsync(HttpRequest request, CancellationToken ct)
    {
        if (request.ContentLength > MaximumBodyBytes)
            return BodyDigest.TooLarge;

        request.EnableBuffering(bufferThreshold: 64 * 1024);
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        var buffer = new byte[16 * 1024];
        var total = 0;
        int read;
        while ((read = await request.Body.ReadAsync(buffer.AsMemory(0, buffer.Length), ct)) != 0)
        {
            total += read;
            if (total > MaximumBodyBytes)
            {
                request.Body.Position = 0;
                return BodyDigest.TooLarge;
            }
            hash.AppendData(buffer, 0, read);
        }

        request.Body.Position = 0;
        return new BodyDigest(false, Convert.ToHexString(hash.GetHashAndReset()).ToLowerInvariant());
    }

    private static bool TryValidate(
        string supplied,
        string key,
        HttpRequest request,
        string contentType,
        string? idempotencyKey,
        string bodySha256,
        out ActorAssertionPayload actor)
    {
        actor = default!;
        var segments = supplied.Split('.', StringSplitOptions.None);
        if (segments.Length != 2 || string.IsNullOrWhiteSpace(segments[0]) || string.IsNullOrWhiteSpace(segments[1]))
            return false;

        byte[] payloadBytes;
        byte[] signature;
        try
        {
            payloadBytes = FromBase64Url(segments[0]);
            signature = FromBase64Url(segments[1]);
        }
        catch (FormatException)
        {
            return false;
        }

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key));
        var expectedSignature = hmac.ComputeHash(Encoding.ASCII.GetBytes(segments[0]));
        if (signature.Length != expectedSignature.Length || !CryptographicOperations.FixedTimeEquals(signature, expectedSignature))
            return false;

        try
        {
            actor = JsonSerializer.Deserialize<ActorAssertionPayload>(payloadBytes, SerializerOptions)!;
        }
        catch (JsonException)
        {
            return false;
        }

        if (actor is null || actor.Version != 2
            || !string.Equals(actor.Issuer, ExpectedIssuer, StringComparison.Ordinal)
            || !string.Equals(actor.Audience, ExpectedAudience, StringComparison.Ordinal)
            || string.IsNullOrWhiteSpace(actor.Subject) || actor.Subject.Length > 200
            || actor.Role?.Length > 100
            || !string.Equals(actor.Method, request.Method, StringComparison.Ordinal)
            || !string.Equals(actor.RequestTarget, RequestTarget(request), StringComparison.Ordinal)
            || !string.Equals(actor.ContentType, contentType, StringComparison.Ordinal)
            || !string.Equals(actor.IdempotencyKey, idempotencyKey, StringComparison.Ordinal)
            || !string.Equals(actor.BodySha256, bodySha256, StringComparison.Ordinal)
            || !IsCanonicalGuid(actor.Jti))
        {
            return false;
        }

        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        return actor.IssuedAt <= now + ClockSkewSeconds
            && actor.ExpiresAt >= now - ClockSkewSeconds
            && actor.ExpiresAt > actor.IssuedAt
            && actor.ExpiresAt - actor.IssuedAt <= MaximumLifetimeSeconds;
    }

    private static bool TryGetNormalizedHeader(IHeaderDictionary headers, string headerName, out string normalized)
    {
        normalized = string.Empty;
        if (!headers.TryGetValue(headerName, out var values))
            return true;
        if (values.Count != 1 || values[0] is null || values[0]!.Length > MaximumHeaderValueLength)
            return false;

        normalized = string.Join(';', values[0]!
            .Split(';', StringSplitOptions.None)
            .Select(value => value.Trim().ToLowerInvariant()));
        return true;
    }

    private static bool TryGetOptionalHeader(IHeaderDictionary headers, string headerName, out string? value)
    {
        value = null;
        if (!headers.TryGetValue(headerName, out StringValues values))
            return true;
        if (values.Count != 1 || values[0] is null || values[0]!.Length > MaximumHeaderValueLength)
            return false;

        value = string.IsNullOrEmpty(values[0]) ? null : values[0];
        return true;
    }

    private static string RequestTarget(HttpRequest request)
        => string.Concat(request.PathBase.Value, request.Path.Value, request.QueryString.Value);

    private static bool IsCanonicalGuid(string? value)
        => Guid.TryParseExact(value, "D", out var parsed)
            && string.Equals(value, parsed.ToString("D"), StringComparison.Ordinal);

    private static async Task RejectAsync(HttpContext context, int statusCode, string error)
    {
        context.Response.StatusCode = statusCode;
        await context.Response.WriteAsJsonAsync(new { error });
    }

    private static byte[] FromBase64Url(string value)
    {
        var padded = value.Replace('-', '+').Replace('_', '/');
        padded = padded.PadRight(padded.Length + (4 - padded.Length % 4) % 4, '=');
        return Convert.FromBase64String(padded);
    }

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private sealed record ActorAssertionPayload(
        int Version,
        string Issuer,
        string Audience,
        string Subject,
        string? Role,
        string Method,
        string RequestTarget,
        string ContentType,
        string BodySha256,
        string? IdempotencyKey,
        string Jti,
        long IssuedAt,
        long ExpiresAt);

    private sealed record BodyDigest(bool IsTooLarge, string Sha256)
    {
        public static BodyDigest TooLarge { get; } = new(true, string.Empty);
    }
}

public static class ActorAssertionMiddlewareExtensions
{
    public static IApplicationBuilder UseActorAssertions(this IApplicationBuilder builder)
        => builder.UseMiddleware<ActorAssertionMiddleware>();
}
