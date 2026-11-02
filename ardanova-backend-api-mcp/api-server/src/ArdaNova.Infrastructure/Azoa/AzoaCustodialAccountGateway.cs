namespace ArdaNova.Infrastructure.Azoa;

using System.Text.Json.Serialization;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

/// <summary>Tenant-scoped transport for AZOA-owned identity, custody, and KYC.</summary>
public sealed class AzoaCustodialAccountGateway
    : IAzoaCustodialAccountGateway, IAzoaConsentRevocationGateway, IAzoaAccountHandoffGateway
{
    /// <summary>The node's offboard route for the implied provisioning grant; a constant, never caller-supplied.</summary>
    /// <remarks>Allowed by <c>AzoaNodeRoutePolicy.IsProvisionedConsentPath</c>, which names this gateway as its consumer.</remarks>
    private const string ProvisionedConsentPath = "/api/tenant/consent/provisioned";

    private readonly IAzoaCustodialNodeClient _node;
    private readonly IHttpClientFactory? _httpClientFactory;
    private readonly ILogger _logger;

    // The transport arguments are optional so the pre-existing single-argument composition in
    // AzoaCustodialAccountGatewayTests still builds; the container fills all three, because both
    // IHttpClientFactory (AddHttpClient) and ILogger<T> (AddLogging) are registered. A gateway
    // built without them can read and provision but CANNOT withdraw - see ResolveCustodyTransport.
    public AzoaCustodialAccountGateway(
        IAzoaCustodialNodeClient node,
        IHttpClientFactory? httpClientFactory = null,
        ILogger<AzoaCustodialAccountGateway>? logger = null)
    {
        _node = node;
        _httpClientFactory = httpClientFactory;
        _logger = logger ?? (ILogger)NullLogger<AzoaCustodialAccountGateway>.Instance;
    }

    public async Task<Result<AzoaCustodialAccountCapabilities>> GetCapabilitiesAsync(
        CancellationToken ct = default)
    {
        var result = await _node.GetAsync<CapabilitiesWire>(
            "/api/tenant/custodial-accounts/capabilities",
            ct);
        if (result.IsFailure)
            return MapFailure<CapabilitiesWire, AzoaCustodialAccountCapabilities>(result);

        var value = result.Value!;
        return Result<AzoaCustodialAccountCapabilities>.Success(new(
            value.Enabled,
            value.WalletChain ?? string.Empty,
            value.CustodyMode ?? string.Empty,
            value.CustodyAvailable,
            value.BlockchainProviderAvailable,
            value.KycProvider ?? string.Empty,
            value.KycAvailable,
            value.HostedVerification,
            value.AcceptsDocumentReferences,
            value.IdentityReady,
            value.KycReady,
            value.WalletProvisioningReady,
            value.Ready,
            value.DevelopmentSimulation,
            value.UnavailableReason));
    }

    public async Task<Result<AzoaCustodialAccountStatus>> EnsureAsync(
        AzoaCustodialAccountBinding binding,
        CancellationToken ct = default)
    {
        var result = await _node.PutAsync<StatusWire>(
            AccountPath(binding.ArdaNovaUserId),
            body: null,
            binding.IdempotencyKey,
            ct);
        return MapStatus(result, binding);
    }

    public async Task<Result<AzoaCustodialAccountStatus>> GetStatusAsync(
        AzoaCustodialAccountBinding binding,
        CancellationToken ct = default)
    {
        var result = await _node.GetAsync<StatusWire>(AccountPath(binding.ArdaNovaUserId), ct);
        return MapStatus(result, binding);
    }

    public async Task<Result<AzoaKycSession>> BeginKycAsync(
        AzoaCustodialAccountBinding binding,
        CancellationToken ct = default)
    {
        var result = await _node.PostAsync<KycSessionWire>(
            $"{AccountPath(binding.ArdaNovaUserId)}/kyc/session",
            body: null,
            binding.KycSessionIdempotencyKey,
            ct);
        if (result.IsFailure)
            return MapFailure<KycSessionWire, AzoaKycSession>(result);

        var value = result.Value!;
        return Result<AzoaKycSession>.Success(new(
            value.Provider ?? string.Empty,
            value.HostedVerification,
            value.AcceptsDocumentReferences,
            value.VerificationUrl,
            value.ExpiresAt,
            value.Instructions,
            value.DevelopmentSimulation));
    }

    /// <summary>Hands one person's standing provisioning permission back to them at the node.</summary>
    public bool CanRevokeConsent => ResolveCustodyTransport() is not null;

    public async Task<Result<AzoaConsentRevocation>> RevokeProvisionedConsentAsync(
        string ardaNovaUserId,
        CancellationToken ct = default)
    {
        // The subject is the authenticated actor and nothing else. Refusing a blank one HERE,
        // where the grant is actually ended, means no present or future caller can withdraw
        // "somebody's" permission by omitting an identity.
        var externalUserId = ardaNovaUserId?.Trim();
        if (string.IsNullOrEmpty(externalUserId))
        {
            return Result<AzoaConsentRevocation>.ValidationError(
                "A person must be identified before their permission can be withdrawn.");
        }

        // The gate for this route family, exactly as AzoaCashOutClient.ListAsync does it: the
        // predicate runs on the BARE path (IsSafeApiPath refuses a '?' outright) and the only
        // thing appended afterwards is an EscapeDataString'd identity, which cannot carry '?',
        // '#', '&' or '/'. So the custody credential cannot be aimed anywhere but this route.
        if (!AzoaNodeRoutePolicy.IsProvisionedConsentPath(ProvisionedConsentPath))
        {
            // A configuration fault, never the person's KYB: the code keeps MoneyRefusal from
            // reading this Forbidden as the custodial route family's KYB gate.
            return Result<AzoaConsentRevocation>.Forbidden(
                "The AZOA custody credential is restricted to tenant custody endpoints.")
                .WithCode(UpstreamFailureCodes.RouteNotAllowed);
        }

        var transport = ResolveCustodyTransport();
        if (transport is null)
        {
            _logger.LogError(
                "AZOA custody transport is unconfigured; a permission withdrawal could not be sent.");
            return Result<AzoaConsentRevocation>.Failure(
                "We could not reach the operator to hand your permission back. Nothing was changed.")
                .WithCode(UpstreamFailureCodes.TransportNotConfigured);
        }

        var request = new HttpRequestMessage(
            HttpMethod.Delete,
            $"{ProvisionedConsentPath}?externalUserId={Uri.EscapeDataString(externalUserId)}");

        var result = await AzoaNodeClient.SendAsync<int>(transport, _logger, request, ct);
        if (result.IsFailure)
            return MapFailure<int, AzoaConsentRevocation>(result);

        // The node answers with how many live grants it ended. Zero is a SUCCESS: the permission
        // is not in force, which is exactly what the person asked for - it was simply already
        // withdrawn (or never issued). Only the timestamp distinguishes the two.
        var withdrawn = result.Value;
        return Result<AzoaConsentRevocation>.Success(new AzoaConsentRevocation(
            Revoked: true,
            RevokedAt: withdrawn > 0 ? DateTimeOffset.UtcNow : (DateTimeOffset?)null,
            PermissionsWithdrawn: withdrawn));
    }

    public async Task<Result<AzoaConsentRevocation>> RevokeClaimedConsentAsync(string userId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(userId)) return Result<AzoaConsentRevocation>.ValidationError("An account is required.");
        var path = $"{AccountPath(userId)}/consent";
        var transport = ResolveCustodyTransport();
        if (transport is null || !AzoaNodePathPolicy.IsCustodialPath(path))
            return Result<AzoaConsentRevocation>.Conflict("Account permission could not be withdrawn.");
        var result = await AzoaNodeClient.SendAsync<RelinquishWire>(transport, _logger,
            new HttpRequestMessage(HttpMethod.Delete, path), ct);
        return result.IsSuccess && result.Value is { } value
            ? Result<AzoaConsentRevocation>.Success(new(true, value.RevokedCount > 0 ? value.RelinquishedAt : null, value.RevokedCount))
            : MapFailure<RelinquishWire, AzoaConsentRevocation>(result);
    }

    // The custody credential lives on the typed client registered as
    // AddHttpClient<IAzoaCustodialNodeClient, AzoaCustodialNodeClient> (Infrastructure
    // DependencyInjection.cs:110). IHttpClientFactory names that registration after the SERVICE
    // type; the implementation name is tried as a fallback, so both plausible conventions resolve
    // (ConsentRevokeChainTests exercises each name on its own). Returning null rather than a bare
    // HttpClient keeps an unconfigured transport fail-closed instead of posting into the void.
    private HttpClient? ResolveCustodyTransport()
    {
        if (_httpClientFactory is null)
            return null;

        var client = _httpClientFactory.CreateClient(nameof(IAzoaCustodialNodeClient));
        if (client.BaseAddress is null)
            client = _httpClientFactory.CreateClient(nameof(AzoaCustodialNodeClient));

        return client.BaseAddress is null ? null : client;
    }

    public async Task<Result<AccountHandoff>> BeginHandoffAsync(string externalSubject, string email,
        string returnUrl, string? requestId, CancellationToken ct)
    {
        var path = $"{AccountPath(externalSubject)}/handoff";
        if (requestId is not null)
        {
            if (!Guid.TryParse(requestId, out _)) return Result<AccountHandoff>.ValidationError("Invalid account request.");
            path += $"/{Uri.EscapeDataString(requestId)}/resend";
        }
        return ValidateHandoff(await _node.PostAsync<AccountHandoff>(path,
            new { deliveryEmail = email, returnUrl }, "handoff:" + (requestId ?? externalSubject), ct), requestId);
    }

    public async Task<Result<AccountHandoff>> ReadHandoffAsync(string externalSubject, string requestId, CancellationToken ct)
    {
        if (!Guid.TryParse(requestId, out _)) return Result<AccountHandoff>.ValidationError("Invalid account request.");
        return ValidateHandoff(await _node.GetAsync<AccountHandoff>(
            $"{AccountPath(externalSubject)}/handoff/{Uri.EscapeDataString(requestId)}", ct), requestId);
    }

    private static Result<AccountHandoff> ValidateHandoff(Result<AccountHandoff> result, string? requestId)
    {
        if (result.IsFailure) return result;
        var value = result.Value;
        return value is not null && Guid.TryParse(value.RequestId, out _)
            && Guid.TryParse(value.TargetUserId, out _) && (requestId is null || value.RequestId == requestId)
            && value.Ownership is "custodial" or "claimed"
            && value.Status is "pending" or "approving" or "approved" or "declined"
            && value.Delivery == "operator_stub"
            ? result : Result<AccountHandoff>.Conflict("The account request could not be confirmed.");
    }

    private static Result<AzoaCustodialAccountStatus> MapStatus(Result<StatusWire> result, AzoaCustodialAccountBinding binding)
    {
        if (result.IsFailure)
            return MapFailure<StatusWire, AzoaCustodialAccountStatus>(result);

        var value = result.Value!;
        if (value.Ownership is not (null or "custodial" or "claimed")
            || value.Consent is not (null or "granted" or "none"))
            return Result<AzoaCustodialAccountStatus>.Conflict("The account permission state was not recognized.");

        var claimed = value.Ownership == "claimed";
        if (claimed && value.Consent is not ("granted" or "none"))
            return Result<AzoaCustodialAccountStatus>.Conflict("The claimed account permission was missing.");
        var scopes = value.ConsentScopes ?? [];
        var minimal = claimed && !scopes.Contains("kyc:read", StringComparer.Ordinal);
        if (minimal)
        {
            if (value.Ready || value.KycStatus is not (null or "Unknown")
                || value.TenantId is not null || value.ExternalSubject is not null || value.AvatarId is not null
                || value.WalletId is not null || value.WalletAddress is not null)
                return Result<AzoaCustodialAccountStatus>.Conflict("The restricted account status was inconsistent.");
            return Result<AzoaCustodialAccountStatus>.Success(new(
                binding.TenantId, binding.ArdaNovaUserId, null, null, null, AzoaKycStatus.Unknown,
                false, false, false, false, "Your permission is needed to read this account.",
                Ownership: "claimed", Consent: value.Consent ?? "none", ConsentScopes: scopes,
                ConsentExpiresAt: value.ConsentExpiresAt, IsMinimal: true));
        }
        var externalSubject = value.ExternalSubject ?? value.ArdaNovaUserId;
        if (!TryParseKycStatus(value.KycStatus, out var status)
            || string.IsNullOrWhiteSpace(value.TenantId)
            || string.IsNullOrWhiteSpace(externalSubject))
        {
            return Result<AzoaCustodialAccountStatus>.Conflict(
                "AZOA returned an invalid custodial account binding or KYC state.");
        }

        return Result<AzoaCustodialAccountStatus>.Success(new(
            value.TenantId,
            externalSubject,
            value.AvatarId,
            value.WalletId,
            value.WalletAddress,
            status,
            value.IdentityReady,
            value.KycReady,
            value.WalletReady,
            value.Ready,
            value.UnavailableReason,
            value.ReviewDueAt,
            value.Ownership ?? "custodial", value.Consent ?? "granted", scopes, value.ConsentExpiresAt));
    }

    private static bool TryParseKycStatus(string? value, out AzoaKycStatus status)
        => Enum.TryParse(value, ignoreCase: false, out status)
            && Enum.IsDefined(status);

    private static string AccountPath(string externalSubject)
        => $"/api/tenant/custodial-accounts/{Uri.EscapeDataString(externalSubject)}";

    private static Result<TOut> MapFailure<TIn, TOut>(Result<TIn> source)
    {
        var error = source.Error ?? "AZOA custodial account operation failed.";
        Result<TOut> mapped = source.Type switch
        {
            ResultType.NotFound => Result<TOut>.NotFound(error),
            ResultType.Forbidden => Result<TOut>.Forbidden(error),
            ResultType.Unauthorized => Result<TOut>.Unauthorized(error),
            ResultType.Conflict => Result<TOut>.Conflict(error),
            ResultType.ValidationError => Result<TOut>.ValidationError(error),
            ResultType.BadRequest => Result<TOut>.BadRequest(error),
            _ => Result<TOut>.Failure(error),
        };
        return mapped.WithCode(source.Code);
    }

    private sealed record RelinquishWire(int RevokedCount, DateTimeOffset RelinquishedAt);

    private sealed record CapabilitiesWire
    {
        public bool Enabled { get; init; }
        public string? WalletChain { get; init; }
        public string? CustodyMode { get; init; }
        public bool CustodyAvailable { get; init; }
        public bool BlockchainProviderAvailable { get; init; }
        public string? KycProvider { get; init; }
        public bool KycAvailable { get; init; }
        public bool HostedVerification { get; init; }
        public bool AcceptsDocumentReferences { get; init; }
        public bool DevelopmentSimulation { get; init; }
        public bool IdentityReady { get; init; }
        public bool KycReady { get; init; }
        public bool WalletProvisioningReady { get; init; }
        public bool Ready { get; init; }
        public string? UnavailableReason { get; init; }
    }

    private sealed record StatusWire
    {
        public string? Ownership { get; init; }
        public string? Consent { get; init; }
        public string[]? ConsentScopes { get; init; }
        public DateTimeOffset? ConsentExpiresAt { get; init; }
        public string? TenantId { get; init; }
        public string? ExternalSubject { get; init; }
        [JsonPropertyName("ardanovaUserId")]
        public string? ArdaNovaUserId { get; init; }
        // AZOA renamed this wire field avatarId -> userId (D28). Pinned
        // explicitly: without it, System.Text.Json's case-insensitive match
        // silently yields null instead of throwing, which reads downstream as
        // "identity not ready" and fail-closes every value action.
        [JsonPropertyName("userId")]
        public string? AvatarId { get; init; }
        public string? WalletId { get; init; }
        public string? WalletAddress { get; init; }
        public string? KycStatus { get; init; }
        public bool IdentityReady { get; init; }
        public bool KycReady { get; init; }
        public bool WalletReady { get; init; }
        public bool Ready { get; init; }
        public string? UnavailableReason { get; init; }

        /// <summary>Null unless KycStatus is UnderReview; when the operator's decision is due.</summary>
        public DateTimeOffset? ReviewDueAt { get; init; }
    }

    private sealed record KycSessionWire
    {
        public string? Provider { get; init; }
        public bool HostedVerification { get; init; }
        public bool AcceptsDocumentReferences { get; init; }
        public bool DevelopmentSimulation { get; init; }
        public string? VerificationUrl { get; init; }
        public DateTime? ExpiresAt { get; init; }
        public string? Instructions { get; init; }
    }

}

/// <summary>What withdrawing the standing provisioning permission actually did at the node.</summary>
/// <param name="Revoked">The permission is no longer in force - true even when it already was not.</param>
/// <param name="RevokedAt">When THIS call ended it; null when there was nothing left to end.</param>
/// <param name="PermissionsWithdrawn">How many live grants this call ended; zero is a success, not a failure.</param>
public sealed record AzoaConsentRevocation(
    bool Revoked,
    DateTimeOffset? RevokedAt,
    int PermissionsWithdrawn);

/// <summary>
/// The withdrawal half of the custody boundary: a person's standing right to take back the
/// permission that provisioning implied. It is a separate port on purpose - only the ENABLED
/// custody gateway implements it, so with custody switched off the answer is an honest refusal
/// rather than a silent success that would leave a live grant standing at the node.
/// </summary>
public interface IAzoaConsentRevocationGateway
{
    bool CanRevokeConsent => true;

    Task<Result<AzoaConsentRevocation>> RevokeProvisionedConsentAsync(
        string ardaNovaUserId,
        CancellationToken ct = default);

    Task<Result<AzoaConsentRevocation>> RevokeClaimedConsentAsync(string userId, CancellationToken ct = default)
        => Task.FromResult(Result<AzoaConsentRevocation>.Conflict("Account permission could not be withdrawn."));
}
