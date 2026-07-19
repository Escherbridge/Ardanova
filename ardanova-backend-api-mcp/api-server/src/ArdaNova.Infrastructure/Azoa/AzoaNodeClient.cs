namespace ArdaNova.Infrastructure.Azoa;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ArdaNova.Application.Common.Results;
using Microsoft.Extensions.Logging;

/// <summary>
/// Typed HttpClient transport to the AZOA node. Registered via
/// <c>AddHttpClient&lt;IAzoaNodeClient, AzoaNodeClient&gt;</c>; the base address +
/// <c>X-Api-Key</c> default header are configured at registration time
/// (see <c>Infrastructure.DependencyInjection</c>).
/// </summary>
public sealed class AzoaNodeClient : IAzoaNodeClient
{
    /// <summary>Fail-closed KYC prefix the node stamps on a forbidden value move (§6).</summary>
    private const string KycForbiddenPrefix = "KYC_FORBIDDEN:";
    private const string KycForbiddenMessage = "KYC_FORBIDDEN: AZOA approval is required.";

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly HttpClient _http;
    private readonly ILogger<AzoaNodeClient> _logger;

    public AzoaNodeClient(
        HttpClient http,
        ILogger<AzoaNodeClient> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<Result<AzoaAllocationResult>> AllocateAsync(
        Guid avatarId,
        AzoaAllocationRequest request,
        string idempotencyKey,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
            return Result<AzoaAllocationResult>.ValidationError(
                "A stable Idempotency-Key is required for every allocation (contract §6/§7).");

        using var msg = new HttpRequestMessage(
            HttpMethod.Post, $"/api/allocation/{avatarId}")
        {
            Content = JsonContent.Create(request),
        };
        // Stable per-event key — the node dedupes a redelivered trigger so the
        // asset moves exactly once.
        msg.Headers.Add("Idempotency-Key", idempotencyKey);

        return await SendValueAsync<AzoaAllocationResult>(
            msg,
            AzoaNodePathPolicy.IsAllocationPath(msg.RequestUri?.OriginalString),
            ct);
    }

    public Task<Result<T>> PostAsync<T>(string path, object? body, CancellationToken ct = default)
    {
        var msg = new HttpRequestMessage(HttpMethod.Post, path)
        {
            Content = body is null ? null : JsonContent.Create(body),
        };
        return SendValueAsync<T>(
            msg,
            AzoaNodePathPolicy.IsFungibleMintPath(msg.RequestUri?.OriginalString),
            ct);
    }

    private Task<Result<T>> SendValueAsync<T>(
        HttpRequestMessage msg,
        bool pathAllowed,
        CancellationToken ct)
    {
        if (!pathAllowed)
        {
            msg.Dispose();
            return Task.FromResult(Result<T>.Forbidden(
                "The AZOA value credential is restricted to allocation and fungible-mint endpoints."));
        }

        return SendAsync<T>(_http, _logger, msg, ct);
    }

    internal static async Task<Result<T>> SendAsync<T>(
        HttpClient http,
        ILogger logger,
        HttpRequestMessage msg,
        CancellationToken ct)
    {
        try
        {
            using var response = await http.SendAsync(msg, ct);
            var raw = await response.Content.ReadAsStringAsync(ct);

            AzoaResultEnvelope<T>? envelope = null;
            if (!string.IsNullOrWhiteSpace(raw))
            {
                try { envelope = JsonSerializer.Deserialize<AzoaResultEnvelope<T>>(raw, JsonOpts); }
                catch (JsonException) { /* fall through to status-based mapping */ }
            }

            // Provider details stay behind the transport boundary; see AGENTS.md.
            if (envelope is { IsError: true } || !response.IsSuccessStatusCode)
            {
                logger.LogWarning(
                    "AZOA node rejected {Method} {Path} with status {StatusCode}",
                    msg.Method,
                    msg.RequestUri,
                    (int)response.StatusCode);
                return MapError<T>(response.StatusCode, envelope?.Message);
            }

            if (envelope is null)
                return Result<T>.Failure("AZOA node returned an empty/unparseable response.");

            if (envelope.Result is null)
            {
                logger.LogWarning(
                    "AZOA node returned no result payload for {Method} {Path}",
                    msg.Method,
                    msg.RequestUri);
                return Result<T>.Failure("AZOA node returned no result payload.");
            }

            return Result<T>.Success(envelope.Result);
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            logger.LogError("AZOA node call timed out: {Method} {Path}", msg.Method, msg.RequestUri);
            return Result<T>.Failure("AZOA node call timed out.");
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "AZOA node call failed: {Method} {Path}", msg.Method, msg.RequestUri);
            return Result<T>.Failure("AZOA node call failed. Retry after checking node availability.");
        }
        catch (InvalidOperationException ex)
        {
            logger.LogError(ex, "AZOA node transport is not configured: {Method} {Path}", msg.Method, msg.RequestUri);
            return Result<T>.Failure("AZOA node transport is not configured.");
        }
        finally
        {
            msg.Dispose();
        }
    }

    private static Result<T> MapError<T>(HttpStatusCode status, string? providerMessage)
    {
        // Preserve only the public KYC signal, never the provider detail.
        if (status == HttpStatusCode.Forbidden
            && providerMessage?.StartsWith(KycForbiddenPrefix, StringComparison.Ordinal) == true)
        {
            return Result<T>.Forbidden(KycForbiddenMessage);
        }

        return status switch
        {
            HttpStatusCode.NotFound => Result<T>.NotFound("AZOA resource was not found."),
            HttpStatusCode.Forbidden => Result<T>.Forbidden("AZOA node denied the request."),
            HttpStatusCode.Unauthorized => Result<T>.Unauthorized("AZOA node authentication failed."),
            HttpStatusCode.Conflict => Result<T>.Conflict("AZOA request conflicted with existing state."),
            (HttpStatusCode)429 => Result<T>.Failure("AZOA node rate limit exceeded."),
            HttpStatusCode.BadRequest => Result<T>.BadRequest("AZOA node rejected the request."),
            _ => Result<T>.Failure("AZOA node request failed."),
        };
    }
}
