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

    /// <summary>The node's own not-found discriminator (<c>AzoaErrorCodes.NotFound</c>).</summary>
    internal const string NodeNotFoundCode = "NOT_FOUND";

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

    /// <summary>Posts to a value route; <see cref="AzoaNodeRoutePolicy.IsValueRoutePath"/> decides which.</summary>
    /// <remarks>
    /// The allowed set is the fungible-mint door, that mint's consent lifecycle steps, and the
    /// reserve-partition declaration door. All stand on the same authority - the value key's
    /// <c>token:create:sign</c> creates a project's stake, sees that creation through, and then
    /// carves the named budgets that stake is issued out of - so a caller that can create a
    /// stake can finish it and declare its allowances and nothing else. The predicate below is
    /// what actually refuses, and it is the one to read.
    /// </remarks>
    public Task<Result<T>> PostAsync<T>(string path, object? body, CancellationToken ct = default)
        => PostValueAsync<T>(path, body, idempotencyKey: null, ct);

    /// <inheritdoc/>
    public Task<Result<T>> PostAsync<T>(string path, object? body, string idempotencyKey, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
            return Task.FromResult(Result<T>.ValidationError("A stable Idempotency-Key is required for a keyed value call."));

        return PostValueAsync<T>(path, body, idempotencyKey, ct);
    }

    private Task<Result<T>> PostValueAsync<T>(string path, object? body, string? idempotencyKey, CancellationToken ct)
    {
        var msg = new HttpRequestMessage(HttpMethod.Post, path)
        {
            Content = body is null ? null : JsonContent.Create(body),
        };
        // The header NftController.ReadIdempotencyKey reads; the node refuses a blank one on fungible-mint.
        if (idempotencyKey is not null)
            msg.Headers.Add("Idempotency-Key", idempotencyKey);

        return SendValueAsync<T>(
            msg,
            AzoaNodeRoutePolicy.IsValueRoutePath(msg.RequestUri?.OriginalString),
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
                "The AZOA value credential is restricted to allocation, fungible-mint, "
                + "fungible-creation and reserve-partition endpoints."));
        }

        return SendAsync<T>(_http, _logger, msg, ct);
    }

    internal static async Task<Result<T>> SendAsync<T>(
        HttpClient http,
        ILogger logger,
        HttpRequestMessage msg,
        CancellationToken ct,
        bool notFoundNeedsNodeCode = false)
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

                // A proxy page or an unrouted path also answers 404; only the node's own envelope
                // is an absence the caller may act on (see AGENTS.md, "Settlement gateway").
                if (notFoundNeedsNodeCode
                    && response.StatusCode == HttpStatusCode.NotFound
                    && !string.Equals(envelope?.Code, NodeNotFoundCode, StringComparison.Ordinal))
                {
                    return Result<T>.Failure("AZOA answered 404 without its not-found envelope; the outcome is unknown.");
                }

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
            // 422 is the node's DECLINE: it read the request, said no, and created nothing
            // (AZOA REQUEST_DECLINED). It maps to a declined result type on purpose - a caller
            // holding a seller's units may release them on this and only on this. 409 above
            // stays AMBIGUOUS: the movement may already have run, so its units stay locked.
            HttpStatusCode.UnprocessableEntity =>
                Result<T>.BadRequest("AZOA node declined the request; nothing was moved."),
            (HttpStatusCode)429 => Result<T>.Failure("AZOA node rate limit exceeded."),
            HttpStatusCode.BadRequest => Result<T>.BadRequest("AZOA node rejected the request."),
            _ => Result<T>.Failure("AZOA node request failed."),
        };
    }
}

/// <summary>
/// The route families L2 opened on the AZOA node, and the shape each one accepts.
/// </summary>
/// <remarks>
/// <para>This is the second half of <c>AzoaNodePathPolicy</c> (declared beside
/// <see cref="AzoaCustodialNodeClient"/>, which owns the custody, allocation, fungible-mint and
/// quest families). It is a separate type only because a static class cannot be reopened; the
/// two are ONE policy, and every predicate here starts from
/// <see cref="AzoaNodePathPolicy.IsSafeApiPath"/> - so absolute destinations, query and fragment
/// suffixes, dot segments and encoded delimiters are refused before any shape rule below is
/// asked.</para>
/// <para><b>A predicate is not a gate.</b> Nothing here refuses anything by itself: each typed
/// client passes the predicate for ITS OWN route family to its send method, and that is where a
/// request is actually stopped. The client, the credential it carries, and the family it may
/// reach: <see cref="AzoaNodeClient.PostAsync{T}"/> (value key) -> <see cref="IsValueRoutePath"/>;
/// <see cref="AzoaProjectTokenNodeGateway"/> (custody key) -> <see cref="IsTreasuryResolvePath"/>;
/// <see cref="AzoaCredentialIssuanceClient"/> (issuance key) -> <see cref="IsCredentialIssuePath"/>;
/// the tenant-consent revoke gateway (custody key) -> <see cref="IsProvisionedConsentPath"/>.</para>
/// </remarks>
internal static class AzoaNodeRoutePolicy
{
    /// <summary>Tenant-scoped project treasury resolve; needs <c>tenant:provision</c>.</summary>
    private const string TreasuryResolvePath = "/api/projects/treasury/resolve";

    /// <summary>Implied-provisioning consent revoke; needs <c>tenant:provision</c>.</summary>
    private const string ProvisionedConsentPath = "/api/tenant/consent/provisioned";

    private const string ReservePrefix = "/api/reserve/";
    private const string PartitionsSuffix = "/partitions";
    private const string CredentialIssuePrefix = "/api/credential/issue/";
    private const string FungibleCreationPrefix = "/api/fungible-creation/";

    /// <summary>The consent-lifecycle steps of FungibleCreationController a fungible-mint needs; replace/cancel/attachment stay out.</summary>
    private static readonly string[] FungibleCreationSteps =
    [
        AzoaFungibleCreationSteps.Quote,
        AzoaFungibleCreationSteps.AcceptQuote,
        AzoaFungibleCreationSteps.Continue,
        AzoaFungibleCreationSteps.Refresh,
    ];

    /// <summary>Longest addressable id this policy will carry in a path segment.</summary>
    private const int MaximumSegmentLength = 128;

    /// <summary>
    /// Everything the VALUE credential may post: create a stake, see that creation through its
    /// consent lifecycle, then declare the named budgets that stake is issued out of. Nothing else.
    /// </summary>
    public static bool IsValueRoutePath(string? path)
        => AzoaNodePathPolicy.IsFungibleMintPath(path)
            || IsFungibleCreationStepPath(path)
            || IsReservePartitionsPath(path);

    /// <summary>Exactly <c>POST /api/fungible-creation/{operationId}/{quote|accept-quote|continue|refresh}</c>.</summary>
    public static bool IsFungibleCreationStepPath(string? path)
    {
        if (!AzoaNodePathPolicy.IsSafeApiPath(path)
            || !path!.StartsWith(FungibleCreationPrefix, StringComparison.Ordinal))
        {
            return false;
        }

        var segments = path[FungibleCreationPrefix.Length..].Split('/');
        return segments.Length == 2
            && IsAddressableSegment(segments[0])
            && FungibleCreationSteps.Contains(segments[1], StringComparer.Ordinal);
    }

    /// <summary>
    /// Builds a lifecycle step path for a node-issued operation id, or null when the id is not one
    /// addressable segment. Refused rather than escaped: an id needing escaping is not the node's.
    /// </summary>
    public static string? FungibleCreationStepPath(string? operationId, string step)
    {
        if (!IsAddressableSegment(operationId))
            return null;

        var path = $"{FungibleCreationPrefix}{operationId}/{step}";
        return IsFungibleCreationStepPath(path) ? path : null;
    }

    /// <summary>Exactly <c>POST /api/projects/treasury/resolve</c>.</summary>
    public static bool IsTreasuryResolvePath(string? path)
        => AzoaNodePathPolicy.IsSafeApiPath(path)
            && string.Equals(path, TreasuryResolvePath, StringComparison.Ordinal);

    /// <summary>Exactly <c>POST /api/reserve/{assetId}/partitions</c>, for one addressable asset.</summary>
    public static bool IsReservePartitionsPath(string? path)
    {
        // The length rule is not decoration: "/api/reserve/partitions" satisfies both the prefix
        // and the suffix while leaving no asset id between them, and slicing it would throw
        // rather than refuse.
        if (!AzoaNodePathPolicy.IsSafeApiPath(path)
            || path!.Length <= ReservePrefix.Length + PartitionsSuffix.Length
            || !path.StartsWith(ReservePrefix, StringComparison.Ordinal)
            || !path.EndsWith(PartitionsSuffix, StringComparison.Ordinal))
        {
            return false;
        }

        var assetId = path[ReservePrefix.Length..^PartitionsSuffix.Length];
        return IsAddressableSegment(assetId);
    }

    /// <summary>Exactly <c>POST /api/credential/issue/{recipientAvatarId}</c>, recipient-keyed.</summary>
    public static bool IsCredentialIssuePath(string? path)
        => AzoaNodePathPolicy.IsSafeApiPath(path)
            && path!.StartsWith(CredentialIssuePrefix, StringComparison.Ordinal)
            && Guid.TryParseExact(path[CredentialIssuePrefix.Length..], "D", out _);

    /// <summary>
    /// Exactly <c>DELETE /api/tenant/consent/provisioned</c>. The node addresses the grant by an
    /// <c>externalUserId</c> QUERY parameter, and <see cref="AzoaNodePathPolicy.IsSafeApiPath"/>
    /// refuses a '?' outright - so a caller asks this of the BARE path and appends its own
    /// already-escaped query to the request afterwards, exactly as
    /// <c>AzoaCashOutClient.ListAsync</c> does with its limit.
    /// </summary>
    public static bool IsProvisionedConsentPath(string? path)
        => AzoaNodePathPolicy.IsSafeApiPath(path)
            && string.Equals(path, ProvisionedConsentPath, StringComparison.Ordinal);

    /// <summary>
    /// One path segment that names a thing: letters, digits, '-' and '_' and nothing else. A dot
    /// is excluded outright rather than left to the dot-segment rule, so no spelling of a
    /// relative segment reaches the shape checks above.
    /// </summary>
    private static bool IsAddressableSegment(string? segment)
    {
        if (string.IsNullOrEmpty(segment) || segment.Length > MaximumSegmentLength)
            return false;

        foreach (var character in segment)
        {
            if (!char.IsAsciiLetterOrDigit(character) && character is not ('-' or '_'))
                return false;
        }

        return true;
    }
}
