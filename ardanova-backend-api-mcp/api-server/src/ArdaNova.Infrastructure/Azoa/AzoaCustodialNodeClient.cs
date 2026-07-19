namespace ArdaNova.Infrastructure.Azoa;

using System.Net.Http.Json;
using ArdaNova.Application.Common.Results;
using Microsoft.Extensions.Logging;

/// <summary>Typed transport constrained to tenant custody and KYC routes.</summary>
public sealed class AzoaCustodialNodeClient : IAzoaCustodialNodeClient
{
    private readonly HttpClient _http;
    private readonly ILogger<AzoaCustodialNodeClient> _logger;

    public AzoaCustodialNodeClient(
        HttpClient http,
        ILogger<AzoaCustodialNodeClient> logger)
    {
        _http = http;
        _logger = logger;
    }

    public Task<Result<T>> GetAsync<T>(string path, CancellationToken ct = default)
        => SendAsync<T>(new HttpRequestMessage(HttpMethod.Get, path), ct);

    public Task<Result<T>> PostAsync<T>(
        string path,
        object? body,
        string idempotencyKey,
        CancellationToken ct = default)
        => SendIdempotentAsync<T>(HttpMethod.Post, path, body, idempotencyKey, ct);

    public Task<Result<T>> PutAsync<T>(
        string path,
        object? body,
        string idempotencyKey,
        CancellationToken ct = default)
        => SendIdempotentAsync<T>(HttpMethod.Put, path, body, idempotencyKey, ct);

    private Task<Result<T>> SendIdempotentAsync<T>(
        HttpMethod method,
        string path,
        object? body,
        string idempotencyKey,
        CancellationToken ct)
    {
        if (idempotencyKey.Length is < 16 or > 200
            || idempotencyKey.Any(char.IsWhiteSpace))
        {
            return Task.FromResult(Result<T>.ValidationError(
                "A 16-200 character whitespace-free Idempotency-Key is required."));
        }

        var message = new HttpRequestMessage(method, path)
        {
            Content = body is null ? null : JsonContent.Create(body),
        };
        message.Headers.Add("Idempotency-Key", idempotencyKey);
        return SendAsync<T>(message, ct);
    }

    private Task<Result<T>> SendAsync<T>(HttpRequestMessage message, CancellationToken ct)
    {
        if (!AzoaNodePathPolicy.IsCustodialPath(message.RequestUri?.OriginalString))
        {
            message.Dispose();
            return Task.FromResult(Result<T>.Forbidden(
                "The AZOA custody credential is restricted to custodial-account endpoints."));
        }

        return AzoaNodeClient.SendAsync<T>(_http, _logger, message, ct);
    }
}

internal static class AzoaNodePathPolicy
{
    private const string CustodialPrefix = "/api/tenant/custodial-accounts";
    private const string AllocationPrefix = "/api/allocation/";
    private const string FungibleMintPath = "/api/nft/fungible-mint";
    private const string QuestPrefix = "/api/quest";

    public static bool IsSafeApiPath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)
            || !path.StartsWith("/api/", StringComparison.Ordinal)
            || path.StartsWith("//", StringComparison.Ordinal)
            || path.Contains('\\')
            || path.Contains('?')
            || path.Contains('#')
            || path.Contains("%2f", StringComparison.OrdinalIgnoreCase)
            || path.Contains("%5c", StringComparison.OrdinalIgnoreCase)
            || path.Contains("%2e", StringComparison.OrdinalIgnoreCase)
            || path.Contains("%25", StringComparison.OrdinalIgnoreCase)
            || path.Any(character => char.IsControl(character) || char.IsWhiteSpace(character))
            || !Uri.TryCreate(path, UriKind.Relative, out _))
        {
            return false;
        }

        string decoded;
        try
        {
            decoded = Uri.UnescapeDataString(path);
        }
        catch (UriFormatException)
        {
            return false;
        }

        return !decoded.Contains('\\')
            && !decoded.Split('/').Any(segment => segment is "." or "..");
    }

    public static bool IsCustodialPath(string? path)
    {
        if (!IsSafeApiPath(path))
            return false;

        var safePath = path!;
        if (!IsCustodialNamespace(safePath))
            return false;

        return safePath.Length == CustodialPrefix.Length
            || safePath[CustodialPrefix.Length] is '/' or '?';
    }

    public static bool IsCustodialNamespace(string? path)
        => IsSafeApiPath(path)
            && path!.StartsWith(CustodialPrefix, StringComparison.OrdinalIgnoreCase);

    public static bool IsAllocationPath(string? path)
        => IsSafeApiPath(path)
            && path!.StartsWith(AllocationPrefix, StringComparison.Ordinal)
            && Guid.TryParseExact(path[AllocationPrefix.Length..], "D", out _);

    public static bool IsFungibleMintPath(string? path)
        => IsSafeApiPath(path)
            && string.Equals(path, FungibleMintPath, StringComparison.Ordinal);

    public static bool IsQuestPath(string? path)
        => IsSafeApiPath(path)
            && (string.Equals(path, QuestPrefix, StringComparison.Ordinal)
                || path!.StartsWith($"{QuestPrefix}/", StringComparison.Ordinal));
}
