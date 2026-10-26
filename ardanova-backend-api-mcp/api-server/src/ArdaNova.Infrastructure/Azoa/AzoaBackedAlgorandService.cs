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
/// Allocation-door amounts pass through to the node as opaque strings (this
/// adapter computes no economics, contract §1, §3); the fungible-mint door is
/// the one exception — its Total/Decimals are real typed numbers, because
/// minting has no prior balance for the node to preserve opaquely.
/// </summary>
public sealed class AzoaBackedAlgorandService : IAlgorandService
{
    /// <summary>
    /// Direct fungible-mint endpoint on the AZOA node. Unlike the allocation door
    /// this is not avatar-scoped — it creates a fungible asset under the tenant,
    /// which maps cleanly onto the legacy <c>CreateFungibleASAAsync</c> surface.
    /// </summary>
    private const string FungibleMintPath = "/api/nft/fungible-mint";

    /// <summary>
    /// Asset scale (decimal places) for AZOA-backed fungible mints. The shared
    /// <see cref="IAlgorandService.CreateFungibleASAAsync"/> surface carries no
    /// decimals parameter of its own, so this mirrors the legacy
    /// <c>AlgorandService.CreateFungibleASAAsync</c> hardcoded convention
    /// ("standard 6 decimals for fungible tokens") to keep the two providers'
    /// asset scale in agreement.
    /// </summary>
    private const int FungibleMintAssetScale = 6;

    private const string AvatarGapMessage =
        "AZOA-backed path is avatar-centric and cannot resolve an Algorand address to an avatar. " +
        "Route this address-keyed call through the allocation service " +
        "(treasury-reward-to-azoa-allocation track), which carries the avatarId for the economic event.";

    private const string DeferredMessage =
        "BurnASA/Clawback deferred to AZOA H2 (soulbound revoke is a follow-up).";

    private readonly IAzoaNodeClient _node;
    private readonly AzoaSettings _settings;
    private readonly ILogger<AzoaBackedAlgorandService> _logger;
    private readonly AzoaFungibleCreationBudget _creationBudget;

    public AzoaBackedAlgorandService(
        IAzoaNodeClient node,
        IOptions<AzoaSettings> settings,
        ILogger<AzoaBackedAlgorandService> logger,
        AzoaFungibleCreationBudget? creationBudget = null)
    {
        _node = node;
        _settings = settings.Value;
        _logger = logger;
        _creationBudget = creationBudget ?? AzoaFungibleCreationBudget.Default;
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
            return Task.FromResult(Result<string>.Failure("Failed to build ARC-19 metadata."));
        }
    }

    // ========================================================================
    // CreateFungibleASAAsync — CLEAN MAP (POST /api/nft/fungible-mint)
    //
    // This is the one value-move that maps cleanly without an avatar: the tenant
    // mints a new fungible asset. We post the already-decided supply as a real
    // Total/Decimals pair (the node derives no economics from it, but it IS a
    // typed number, not an opaque string), drive the node's consent lifecycle to
    // a terminal view, and surface that view's asset id back through the legacy
    // FungibleAsaCreateResult shape. See AGENTS.md §Fungible-mint consent lifecycle.
    // ========================================================================

    /// <inheritdoc/>
    public async Task<Result<FungibleAsaCreateResult>> CreateFungibleASAAsync(
        string name,
        string unitName,
        ulong totalSupply,
        string idempotencyKey,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
            return Result<FungibleAsaCreateResult>.ValidationError(
                "A stable idempotency key is required to create a fungible asset.");

        _logger.LogInformation(
            "AZOA fungible-mint: {Name} ({UnitName}), supply {TotalSupply}",
            name, unitName, totalSupply);

        var request = new AzoaFungibleMintRequest
        {
            ChainType = string.IsNullOrWhiteSpace(_settings.ChainType) ? "Algorand" : _settings.ChainType,
            Name = name,
            UnitName = unitName,
            // A real ulong, NOT an opaque string: AZOA's FungibleMintRequest.Total is
            // the tenant-decided supply itself (no economics for the node to derive),
            // unlike the allocation door's opaque-string amount. Sending it as a JSON
            // string here previously deserialized to 0 on AZOA's side and failed every
            // mint with "zero total" (see AZOA repo Models/Requests/FungibleMintRequest.cs).
            Total = totalSupply,
            Decimals = FungibleMintAssetScale,
        };

        var result = await _node.PostAsync<AzoaFungibleMintResult>(FungibleMintPath, request, idempotencyKey, ct);
        if (result.IsFailure)
        {
            // Preserve the node's failure type (Forbidden/NotFound/etc.) verbatim
            // so KYC fail-closed and rate-limit semantics survive the adapter.
            return MapFailure<AzoaFungibleMintResult, FungibleAsaCreateResult>(result);
        }

        if (result.Value!.Operation is not { } admitted)
            return Result<FungibleAsaCreateResult>.Failure("AZOA fungible-mint returned no creation operation.");

        var completed = await DriveCreationAsync(admitted, ct);
        if (completed.IsFailure)
            return MapFailure<AzoaFungibleCreationView, FungibleAsaCreateResult>(completed);

        return Result<FungibleAsaCreateResult>.Success(new FungibleAsaCreateResult
        {
            AssetId = completed.Value!.AssetId!,
            // The terminal view's own creation transaction, or empty when it names none —
            // never derived from AssetId/WalletId (same "no faking" rule as the gaps below).
            TxHash = completed.Value.TransactionId ?? string.Empty,
            // The operation's own chain, not the one requested: a Simulated node redirects it.
            ChainType = string.IsNullOrWhiteSpace(completed.Value.ChainType) ? null : completed.Value.ChainType.Trim(),
        });
    }

    /// <summary>
    /// Walks one fungible creation from wherever the node says it stands to a terminal view:
    /// quote → accept-quote → continue, then refresh until Completed, within the creation budget.
    /// </summary>
    private async Task<Result<AzoaFungibleCreationView>> DriveCreationAsync(
        AzoaFungibleCreationView admitted, CancellationToken ct)
    {
        var clock = System.Diagnostics.Stopwatch.StartNew();
        var view = admitted;
        string? previousStep = null;

        while (true)
        {
            if (view.State == AzoaFungibleCreationStates.Completed)
            {
                return string.IsNullOrWhiteSpace(view.AssetId)
                    ? Result<AzoaFungibleCreationView>.Failure("AZOA fungible creation completed without an asset id.")
                    : Result<AzoaFungibleCreationView>.Success(view);
            }

            if (view.State is AzoaFungibleCreationStates.Cancelled or AzoaFungibleCreationStates.FailedNoEffect)
            {
                return Result<AzoaFungibleCreationView>.Failure(
                    $"The AZOA node ended this stake's creation as {view.State}; no asset was created.");
            }

            if (clock.Elapsed >= _creationBudget.Total)
            {
                _logger.LogWarning(
                    "AZOA fungible creation {OperationId} still {State} (v{Version}) after {Budget}",
                    view.OperationId, view.State, view.Version, _creationBudget.Total);
                return Result<AzoaFungibleCreationView>.Failure(
                    $"The stake is still being created on the AZOA node (last state: {view.State}). "
                    + "Retrying setup resumes the same creation; it will not create a second stake.");
            }

            var (step, body) = NextCreationStep(view);
            // Back off whenever the node did not move us on, so a stalled step polls rather than spins.
            if (step == AzoaFungibleCreationSteps.Refresh || step == previousStep)
                await Task.Delay(_creationBudget.PollDelay, ct);
            previousStep = step;

            var path = AzoaNodeRoutePolicy.FungibleCreationStepPath(view.OperationId, step);
            if (path is null)
                return Result<AzoaFungibleCreationView>.Failure("AZOA fungible-mint returned an unaddressable operation id.");

            var next = await _node.PostAsync<AzoaFungibleCreationView>(path, body, ct);
            if (next.IsFailure)
                return next;

            view = next.Value!;
        }
    }

    /// <summary>The lifecycle step a view in this state is waiting on, and its body.</summary>
    private static (string Step, object? Body) NextCreationStep(AzoaFungibleCreationView view)
        => view.State switch
        {
            AzoaFungibleCreationStates.AwaitingConsent when view.Quote is null
                => (AzoaFungibleCreationSteps.Quote, null),
            AzoaFungibleCreationStates.AwaitingConsent
                => (AzoaFungibleCreationSteps.AcceptQuote,
                    new AzoaFungibleQuoteAcceptance { Version = view.Version, QuoteId = view.Quote!.QuoteId }),
            AzoaFungibleCreationStates.Admitted or AzoaFungibleCreationStates.Preparing or AzoaFungibleCreationStates.Prepared
                => (AzoaFungibleCreationSteps.Continue,
                    new AzoaFungibleContinuationRequest
                    {
                        RequestId = ContinuationRequestId(view.OperationId, view.Version),
                        Version = view.Version,
                    }),
            _ => (AzoaFungibleCreationSteps.Refresh, null),
        };

    /// <summary>Same operation + version → same continuation id, so a retried continue is one authorization.</summary>
    private static Guid ContinuationRequestId(string operationId, long version)
    {
        var digest = System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes($"ardanova-fungible-continue:{operationId}:{version}"));
        return new Guid(digest.AsSpan(0, 16));
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
// Mirror AZOA's actual DTOs byte-for-byte (Models/Requests/FungibleMintRequest.cs
// and Models/Responses/FungibleTokenResult.cs in the AZOA repo) — this endpoint,
// unlike the allocation door, carries a REAL typed Total/Decimals pair, not an
// opaque string amount, because minting a brand-new asset has no prior economics
// for the node to preserve; the tenant-decided supply IS the number. Kept internal
// to the adapter to avoid widening the shared Azoa wire surface for a single call.
// See tests/Azoa/FungibleMintContractTests.cs for the pinned wire-shape assertions.

/// <summary>Request body for <c>POST /api/nft/fungible-mint</c>.</summary>
public sealed class AzoaFungibleMintRequest
{
    [JsonPropertyName("chainType")] public string ChainType { get; set; } = string.Empty;
    [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;
    [JsonPropertyName("unitName")] public string UnitName { get; set; } = string.Empty;

    /// <summary>
    /// Total supply in base units. A real number — AZOA's <c>FungibleMintRequest.Total</c>
    /// is <c>ulong</c>, not an opaque string; serializing it as a string here
    /// deserializes to 0 on AZOA's side and the mint fails with "zero total".
    /// </summary>
    [JsonPropertyName("total")] public ulong Total { get; set; }

    /// <summary>Decimal places (asset scale). Mirrors AZOA's <c>FungibleMintRequest.Decimals</c>.</summary>
    [JsonPropertyName("decimals")] public int Decimals { get; set; }
}

/// <summary>
/// Result payload from <c>POST /api/nft/fungible-mint</c>, mirroring AZOA's
/// <c>FungibleTokenResult</c> exactly. The chain references live on <see cref="Operation"/>,
/// the durable creation the node admitted; the top-level <see cref="AssetId"/> stays empty
/// until that creation is confirmed.
/// </summary>
public sealed class AzoaFungibleMintResult
{
    /// <summary>The admitted creation (AZOA <c>FungibleCreationView</c>); null only from a node predating the lifecycle.</summary>
    [JsonPropertyName("operation")] public AzoaFungibleCreationView? Operation { get; set; }

    [JsonPropertyName("userId")] public Guid UserId { get; set; }
    [JsonPropertyName("walletId")] public Guid WalletId { get; set; }
    [JsonPropertyName("walletAddress")] public string WalletAddress { get; set; } = string.Empty;
    [JsonPropertyName("walletProvisioned")] public bool WalletProvisioned { get; set; }
    [JsonPropertyName("assetId")] public string AssetId { get; set; } = string.Empty;

    /// <summary>True when the idempotency key replayed an existing mint — no second asset was created.</summary>
    [JsonPropertyName("replayed")] public bool Replayed { get; set; }
}

// ── Fungible-creation lifecycle wire models ───────────────────────────────────
// Mirror AZOA's Models/Blockchain/FungibleCreationCommand.cs (FungibleCreationView),
// FungibleCreation.cs (FungibleCreationQuote), FungibleQuoteAcceptance.cs and
// FungibleContinuationRequest.cs. Only the fields this adapter reads are bound.

/// <summary>AZOA <c>FungibleCreationView</c>: one durable creation's state, as every lifecycle step answers it.</summary>
public sealed class AzoaFungibleCreationView
{
    [JsonPropertyName("operationId")] public string OperationId { get; set; } = string.Empty;
    [JsonPropertyName("version")] public long Version { get; set; }
    [JsonPropertyName("state")] public string State { get; set; } = string.Empty;

    /// <summary>The chain the node actually created on (Blockchain:Mode=Simulated stamps "Simulated").</summary>
    [JsonPropertyName("chainType")] public string? ChainType { get; set; }

    /// <summary>Set by the node only once the state is Completed or AttachmentPending.</summary>
    [JsonPropertyName("assetId")] public string? AssetId { get; set; }
    [JsonPropertyName("transactionId")] public string? TransactionId { get; set; }
    [JsonPropertyName("quote")] public AzoaFungibleCreationQuote? Quote { get; set; }
}

/// <summary>AZOA <c>FungibleCreationQuote</c>, reduced to the id acceptance must echo.</summary>
public sealed class AzoaFungibleCreationQuote
{
    [JsonPropertyName("quoteId")] public string QuoteId { get; set; } = string.Empty;
}

/// <summary>Body of <c>POST /api/fungible-creation/{id}/accept-quote</c> (AZOA <c>FungibleQuoteAcceptance</c>).</summary>
public sealed class AzoaFungibleQuoteAcceptance
{
    [JsonPropertyName("version")] public long Version { get; set; }
    [JsonPropertyName("quoteId")] public string QuoteId { get; set; } = string.Empty;
}

/// <summary>Body of <c>POST /api/fungible-creation/{id}/continue</c> (AZOA <c>FungibleContinuationRequest</c>).</summary>
public sealed class AzoaFungibleContinuationRequest
{
    [JsonPropertyName("requestId")] public Guid RequestId { get; set; }
    [JsonPropertyName("version")] public long Version { get; set; }
}

/// <summary>The <c>FungibleCreationView.State</c> values the adapter branches on.</summary>
public static class AzoaFungibleCreationStates
{
    public const string AwaitingConsent = "AwaitingConsent";
    public const string Admitted = "Admitted";
    public const string Preparing = "Preparing";
    public const string Prepared = "Prepared";
    public const string Submitted = "Submitted";
    public const string Completed = "Completed";
    public const string Cancelled = "Cancelled";
    public const string FailedNoEffect = "FailedNoEffect";
}

/// <summary>The lifecycle step segments of <c>FungibleCreationController</c> the adapter drives.</summary>
internal static class AzoaFungibleCreationSteps
{
    public const string Quote = "quote";
    public const string AcceptQuote = "accept-quote";
    public const string Continue = "continue";
    public const string Refresh = "refresh";
}

/// <summary>How long one provisioning call may wait for a creation to finish, and how often it polls.</summary>
public sealed record AzoaFungibleCreationBudget(TimeSpan Total, TimeSpan PollDelay)
{
    public static AzoaFungibleCreationBudget Default { get; } = new(TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(1));
}
