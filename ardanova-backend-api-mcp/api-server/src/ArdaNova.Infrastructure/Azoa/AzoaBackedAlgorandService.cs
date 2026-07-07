namespace ArdaNova.Infrastructure.Azoa;

using System.Text.Json;
using System.Text.Json.Serialization;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

/// <summary>
/// AZOA-backed implementation of <see cref="IAlgorandService"/> (provider-adapter track).
///
/// The <see cref="IAlgorandService"/> interface is the seam: every existing
/// consumer (CredentialUtilityService, tokenomics, etc.) keeps calling the same
/// surface unchanged, while value moves are routed to the shared/managed AZOA
/// node instead of the legacy custodial-mnemonic signer (<c>AlgorandService</c>).
/// Selection is feature-flagged by <c>Algorand:Provider</c> (Legacy | Azoa |
/// Simulated); see <c>Infrastructure.DependencyInjection</c>.
///
/// ── The address/avatar impedance mismatch (the documented gap) ───────────────
/// The legacy <see cref="IAlgorandService"/> is ADDRESS-centric: callers pass
/// raw Algorand <c>recipientAddress</c> strings and string/ulong asset ids.
/// AZOA's allocation door is AVATAR-centric: it operates on an <c>avatarId</c>
/// (a Guid the node maps to a custodied wallet). This adapter cannot resolve an
/// arbitrary Algorand address back to an avatar — that mapping lives in the
/// allocation domain (the treasury-reward-to-azoa-allocation track), which holds
/// the avatarId at the point the economic event is decided.
///
/// Therefore the methods split into three honest buckets:
///   1. CLEAN MAP   — CreateFungibleASAAsync (direct fungible mint, no avatar
///                    needed) and BuildARC19MetadataAsync (pure, no network).
///   2. DOCUMENTED GAP — address-keyed mint/transfer/read/ownership calls return
///                    a clear <see cref="Result{T}.Failure"/> instructing the
///                    caller to route through the avatar-aware allocation service.
///                    We do NOT fake an avatar resolution.
///   3. DEFERRED    — BurnASAAsync / ClawbackASAAsync (soulbound revoke) are an
///                    AZOA H2 follow-up and return a clear deferred failure.
///
/// Amounts pass through to the node as opaque strings; this adapter computes no
/// economics (contract §1, §3).
/// </summary>
public sealed class AzoaBackedAlgorandService : IAlgorandService
{
    /// <summary>
    /// Direct fungible-mint endpoint on the AZOA node. Unlike the allocation door
    /// this is not avatar-scoped — it creates a fungible asset under the tenant,
    /// which maps cleanly onto the legacy <c>CreateFungibleASAAsync</c> surface.
    /// </summary>
    private const string FungibleMintPath = "/api/nft/fungible-mint";

    private const string AvatarGapMessage =
        "AZOA-backed path is avatar-centric and cannot resolve an Algorand address to an avatar. " +
        "Route this address-keyed call through the allocation service " +
        "(treasury-reward-to-azoa-allocation track), which carries the avatarId for the economic event.";

    private const string DeferredMessage =
        "BurnASA/Clawback deferred to AZOA H2 (soulbound revoke is a follow-up).";

    private readonly IAzoaNodeClient _node;
    private readonly AzoaSettings _settings;
    private readonly ILogger<AzoaBackedAlgorandService> _logger;

    public AzoaBackedAlgorandService(
        IAzoaNodeClient node,
        IOptions<AzoaSettings> settings,
        ILogger<AzoaBackedAlgorandService> logger)
    {
        _node = node;
        _settings = settings.Value;
        _logger = logger;
    }

    // ========================================================================
    // BuildARC19MetadataAsync — CLEAN MAP (pure / no network)
    //
    // Byte-identical to the legacy AlgorandService.BuildARC19MetadataAsync body.
    // We deliberately replicate the metadata-shape logic rather than depend on
    // the legacy service (injecting it would defeat the seam and re-introduce the
    // custodial dependency). Any change here MUST be mirrored in the legacy impl
    // so the on-chain ARC-19 document stays stable across providers.
    // ========================================================================

    /// <inheritdoc/>
    public Task<Result<string>> BuildARC19MetadataAsync(
        CredentialMetadataInput credential,
        CancellationToken ct = default)
    {
        try
        {
            // NOTE: scopeLabel is retained to mirror the legacy body exactly
            // (it is computed but not emitted there either). Do not "clean it up"
            // independently of the legacy impl or the two will drift.
            var scopeLabel = credential.Scope.Equals("GUILD", StringComparison.OrdinalIgnoreCase)
                ? "guild"
                : "project";

            var metadata = new
            {
                standard = "arc19",
                name = "ArdaNova Membership Credential",
                description = $"Soulbound governance credential for {credential.ScopeName}",
                properties = new
                {
                    credentialId = credential.CredentialId,
                    scope = credential.Scope,
                    scopeId = credential.ScopeId,
                    scopeName = credential.ScopeName,
                    userId = credential.UserId,
                    tier = credential.Tier,
                    grantedVia = credential.GrantedVia,
                    grantedAt = credential.GrantedAt.ToString("o"),
                    isTransferable = false,
                    platform = "ArdaNova",
                    version = "1.0"
                }
            };

            var json = JsonSerializer.Serialize(metadata, new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            return Task.FromResult(Result<string>.Success(json));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to build ARC-19 metadata for credential {CredentialId}", credential.CredentialId);
            return Task.FromResult(Result<string>.Failure($"Failed to build ARC-19 metadata: {ex.Message}"));
        }
    }

    // ========================================================================
    // CreateFungibleASAAsync — CLEAN MAP (POST /api/nft/fungible-mint)
    //
    // This is the one value-move that maps cleanly without an avatar: the tenant
    // mints a new fungible asset. We post the already-decided supply as an opaque
    // string (the node derives no economics) and surface the node's asset id +
    // operation id back through the legacy FungibleAsaCreateResult shape.
    // ========================================================================

    /// <inheritdoc/>
    public async Task<Result<FungibleAsaCreateResult>> CreateFungibleASAAsync(
        string name,
        string unitName,
        ulong totalSupply,
        CancellationToken ct = default)
    {
        _logger.LogInformation(
            "AZOA fungible-mint: {Name} ({UnitName}), supply {TotalSupply}",
            name, unitName, totalSupply);

        var request = new AzoaFungibleMintRequest
        {
            ChainType = string.IsNullOrWhiteSpace(_settings.ChainType) ? "Algorand" : _settings.ChainType,
            Name = name,
            UnitName = unitName,
            // Opaque string — AZOA does not recompute supply (§1, §3).
            TotalSupply = totalSupply.ToString(),
        };

        var result = await _node.PostAsync<AzoaFungibleMintResult>(FungibleMintPath, request, ct);
        if (result.IsFailure)
        {
            // Preserve the node's failure type (Forbidden/NotFound/etc.) verbatim
            // so KYC fail-closed and rate-limit semantics survive the adapter.
            return MapFailure<AzoaFungibleMintResult, FungibleAsaCreateResult>(result);
        }

        var payload = result.Value!;
        if (string.IsNullOrWhiteSpace(payload.AssetId))
            return Result<FungibleAsaCreateResult>.Failure("AZOA fungible-mint returned no asset id.");

        return Result<FungibleAsaCreateResult>.Success(new FungibleAsaCreateResult
        {
            AssetId = payload.AssetId,
            // The node reports the chain transaction hash when live; in Simulated
            // mode it returns a deterministic operation id. Either is a valid
            // opaque reference for the legacy TxHash field.
            TxHash = !string.IsNullOrWhiteSpace(payload.TxHash)
                ? payload.TxHash
                : payload.OperationId,
        });
    }

    // ========================================================================
    // MintSoulboundASAAsync — DOCUMENTED GAP
    //
    // A soulbound mint to a specific recipient is an avatar-scoped value move on
    // AZOA (allocation kind=Mint against an avatarId). The legacy surface only
    // gives us a recipientAddress, which the adapter cannot map to an avatar.
    // Routed callers should use the allocation service instead. We return a clear
    // Failure — never throw, never fabricate an avatar.
    // ========================================================================

    /// <inheritdoc/>
    public Task<Result<SoulboundAsaMintResult>> MintSoulboundASAAsync(
        string recipientAddress,
        CredentialMetadataInput metadata,
        CancellationToken ct = default)
    {
        _logger.LogWarning(
            "MintSoulboundASAAsync called on AZOA-backed adapter with address {RecipientAddress} for credential {CredentialId}; address->avatar mapping is unavailable here.",
            recipientAddress, metadata.CredentialId);
        return Task.FromResult(Result<SoulboundAsaMintResult>.Failure(AvatarGapMessage));
    }

    // ========================================================================
    // TransferASAAsync — DOCUMENTED GAP
    //
    // AZOA transfer (allocation kind=Transfer) targets an avatarId, not an
    // Algorand address. Same gap as mint.
    // ========================================================================

    /// <inheritdoc/>
    public Task<Result<string>> TransferASAAsync(
        string assetId,
        string recipientAddress,
        ulong amount,
        CancellationToken ct = default)
    {
        _logger.LogWarning(
            "TransferASAAsync called on AZOA-backed adapter with address {RecipientAddress} (asset {AssetId}); address->avatar mapping is unavailable here.",
            recipientAddress, assetId);
        return Task.FromResult(Result<string>.Failure(AvatarGapMessage));
    }

    // ========================================================================
    // GetASAInfoAsync — DOCUMENTED GAP
    //
    // Chain is the source of truth; AZOA stores no balance and exposes reads via
    // avatar wallet/portfolio (GET /api/wallet/{id}/portfolio), keyed by wallet,
    // not by a bare asset id. There is no avatar/wallet context in this call, so
    // the asset-info read cannot be satisfied through the AZOA-backed path.
    // ========================================================================

    /// <inheritdoc/>
    public Task<Result<AsaInfoDto>> GetASAInfoAsync(string assetId, CancellationToken ct = default)
    {
        _logger.LogWarning(
            "GetASAInfoAsync called on AZOA-backed adapter for asset {AssetId}; AZOA exposes reads via avatar wallet/portfolio, not bare asset id.",
            assetId);
        return Task.FromResult(Result<AsaInfoDto>.Failure(AvatarGapMessage));
    }

    // ========================================================================
    // GetASABalanceAsync — DOCUMENTED GAP
    //
    // Same as GetASAInfoAsync: balance is read from the avatar's wallet portfolio
    // (GET /api/wallet/{id}/portfolio), which requires an avatar/wallet context
    // this address-keyed call does not provide.
    // ========================================================================

    /// <inheritdoc/>
    public Task<Result<ulong>> GetASABalanceAsync(
        string assetId,
        string address,
        CancellationToken ct = default)
    {
        _logger.LogWarning(
            "GetASABalanceAsync called on AZOA-backed adapter for asset {AssetId} address {Address}; AZOA reads balance via avatar wallet portfolio.",
            assetId, address);
        return Task.FromResult(Result<ulong>.Failure(AvatarGapMessage));
    }

    // ========================================================================
    // VerifyOwnershipAsync — DOCUMENTED GAP
    //
    // Ownership is derived from the avatar's wallet portfolio. Without an
    // avatar/wallet context we cannot answer for a bare Algorand address.
    // ========================================================================

    /// <inheritdoc/>
    public Task<Result<bool>> VerifyOwnershipAsync(
        string assetId,
        string address,
        CancellationToken ct = default)
    {
        _logger.LogWarning(
            "VerifyOwnershipAsync called on AZOA-backed adapter for asset {AssetId} address {Address}; AZOA derives ownership from avatar wallet portfolio.",
            assetId, address);
        return Task.FromResult(Result<bool>.Failure(AvatarGapMessage));
    }

    // ========================================================================
    // BurnASAAsync — DEFERRED (AZOA H2)
    // ========================================================================

    /// <inheritdoc/>
    public Task<Result<string>> BurnASAAsync(string assetId, CancellationToken ct = default)
    {
        _logger.LogWarning("BurnASAAsync is deferred on the AZOA-backed adapter (asset {AssetId}).", assetId);
        return Task.FromResult(Result<string>.Failure(DeferredMessage));
    }

    // ========================================================================
    // ClawbackASAAsync — DEFERRED (AZOA H2)
    // ========================================================================

    /// <inheritdoc/>
    public Task<Result<string>> ClawbackASAAsync(
        string assetId,
        string fromAddress,
        ulong amount,
        CancellationToken ct = default)
    {
        _logger.LogWarning(
            "ClawbackASAAsync is deferred on the AZOA-backed adapter (asset {AssetId} from {FromAddress}).",
            assetId, fromAddress);
        return Task.FromResult(Result<string>.Failure(DeferredMessage));
    }

    // ========================================================================
    // Helpers
    // ========================================================================

    /// <summary>
    /// Re-projects a failed <c>Result&lt;TIn&gt;</c> onto <c>Result&lt;TOut&gt;</c>
    /// while preserving the original <see cref="ResultType"/> (Forbidden,
    /// NotFound, Conflict, …) and message — so the node's fail-closed KYC and
    /// rate-limit semantics survive the adapter boundary.
    /// </summary>
    private static Result<TOut> MapFailure<TIn, TOut>(Result<TIn> source)
    {
        var error = source.Error ?? "AZOA node call failed.";
        return source.Type switch
        {
            ResultType.NotFound => Result<TOut>.NotFound(error),
            ResultType.Forbidden => Result<TOut>.Forbidden(error),
            ResultType.Unauthorized => Result<TOut>.Unauthorized(error),
            ResultType.Conflict => Result<TOut>.Conflict(error),
            ResultType.ValidationError => Result<TOut>.ValidationError(error),
            ResultType.BadRequest => Result<TOut>.BadRequest(error),
            _ => Result<TOut>.Failure(error),
        };
    }
}

// ── Fungible-mint wire models (local to the adapter) ──────────────────────────
// The contract names POST /api/nft/fungible-mint as the direct fungible-mint
// endpoint but pins no DTO for it; these mirror the conventions of the existing
// AZOA wire models (camelCase JSON, opaque string amounts). Kept internal to the
// adapter to avoid widening the shared Azoa wire surface for a single call.

/// <summary>Request body for <c>POST /api/nft/fungible-mint</c>.</summary>
public sealed class AzoaFungibleMintRequest
{
    [JsonPropertyName("chainType")] public string ChainType { get; set; } = string.Empty;
    [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;
    [JsonPropertyName("unitName")] public string UnitName { get; set; } = string.Empty;

    /// <summary>Already-decided total supply as an opaque string (§1, §3).</summary>
    [JsonPropertyName("totalSupply")] public string TotalSupply { get; set; } = string.Empty;
}

/// <summary>Result payload from <c>POST /api/nft/fungible-mint</c>.</summary>
public sealed class AzoaFungibleMintResult
{
    [JsonPropertyName("assetId")] public string AssetId { get; set; } = string.Empty;

    /// <summary>Chain transaction hash when live; empty in Simulated mode.</summary>
    [JsonPropertyName("txHash")] public string? TxHash { get; set; }

    /// <summary>Deterministic operation id; used as the TxHash fallback in Simulated mode.</summary>
    [JsonPropertyName("operationId")] public string OperationId { get; set; } = string.Empty;
}
