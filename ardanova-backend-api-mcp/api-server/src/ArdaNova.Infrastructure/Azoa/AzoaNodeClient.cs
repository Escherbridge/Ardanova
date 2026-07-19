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

            // Map an error envelope (or a non-2xx without a parseable body) into
            // the right Result type — preserving fail-closed KYC semantics.
            if (envelope is { IsError: true } || !response.IsSuccessStatusCode)
            {
                var message = envelope?.Message
                    ?? $"AZOA node returned {(int)response.StatusCode} {response.ReasonPhrase}.";
                return MapError<T>(response.StatusCode, message);
            }

            if (envelope is null)
                return Result<T>.Failure("AZOA node returned an empty/unparseable response.");

            if (envelope.Result is null)
                return Result<T>.Failure(envelope.Message ?? "AZOA node returned no result payload.");

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

    private static Result<T> MapError<T>(HttpStatusCode status, string message)
    {
        // Fail-closed KYC gate: the node returns a KYC_FORBIDDEN:-prefixed 403 on
        // a value seam when the actor is not KYC-approved (§6, §11.4). Surface it
        // as Forbidden with the message intact so the controller can translate it.
        if (message.StartsWith(KycForbiddenPrefix, StringComparison.Ordinal))
            return Result<T>.Forbidden(message);

        return status switch
        {
            HttpStatusCode.NotFound => Result<T>.NotFound(message),
            HttpStatusCode.Forbidden => Result<T>.Forbidden(message),
            HttpStatusCode.Unauthorized => Result<T>.Unauthorized(message),
            HttpStatusCode.Conflict => Result<T>.Conflict(message),
            (HttpStatusCode)429 => Result<T>.Failure($"Rate limited by AZOA node: {message}"),
            HttpStatusCode.BadRequest => Result<T>.BadRequest(message),
            _ => Result<T>.Failure(message),
        };
    }
}
