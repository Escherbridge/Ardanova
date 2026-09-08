namespace ArdaNova.Infrastructure.Azoa;

using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.Services.Implementations;
using ArdaNova.Application.Services.Interfaces;
using Microsoft.Extensions.Logging;

/// <summary>What an operator actually configured for the node allowed to settle sponsorships.</summary>
/// <remarks>
/// Computed once in <c>Infrastructure.DependencyInjection</c> from the same settings the AZOA block
/// already validates, so the readiness gate reads configuration facts rather than a hard-coded
/// <c>true</c>. <see cref="OperatorAttestationExpiresAt"/> comes from <c>Azoa:SettlementAttestationExpiresAt</c>
/// and is absent by default - an attestation nobody renewed is not a current attestation.
/// </remarks>
public sealed record AzoaSettlementGatewayCapability(
    string SelectedNodeId,
    bool TransportConfigured,
    DateTimeOffset? OperatorAttestationExpiresAt);

/// <summary>Which leg of an exchange the settlement row's own amount refers to.</summary>
public enum AzoaSettlementLeg
{
    /// <summary>The platform-unit leg: what the sponsor gave up.</summary>
    Platform,

    /// <summary>The project-unit leg: what the beneficiary received.</summary>
    ProjectUnit,
}

/// <summary>
/// Settles ArdaNova's immutable economic decisions as AZOA exchange operations.
/// </summary>
/// <remarks>
/// One node call per settlement, carrying token base units only: the exchange never learns a fiat
/// amount, and a settlement whose asset code IS a currency is refused here rather than sent as
/// "USD" (contract §1/§3, gotcha 12). Everything the exchange needs beyond the settlement row -
/// the project, its asset, the reserve partition, both legs and the declared rate - travels in the
/// settlement's immutable terms snapshot, which the sponsorship intent locked at authorization
/// time; unreadable terms are a refusal, never a guess.
/// <para>
/// Reconciliation reads <c>GET /api/exchange/receipt/{idempotencyKey}</c>. The allocation receipt
/// route cannot resolve an exchange operation id, so the durable idempotency key is the only handle
/// that survives an ambiguous dispatch.
/// </para>
/// <para>
/// Amount accounting is a control, not a log line: gross is what the payer side gave up, net is
/// what the beneficiary side received, and the difference is the fee the node kept. A receipt that
/// lands MORE than ArdaNova authorised is not a bonus - it is unreconciled, and it stops here.
/// </para>
/// </remarks>
public sealed class AzoaSettlementGateway : IAzoaSettlementGateway, ISelectedNodeSettlementCapability
{
    /// <summary>Currency codes an exchange settlement may never carry; the node moves token units.</summary>
    private static readonly HashSet<string> FiatAssetCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        "USD", "EUR", "GBP", "CAD", "AUD", "NZD", "CHF", "JPY", "SEK", "NOK", "DKK",
    };

    private readonly IAzoaExchangeNode _exchange;
    private readonly IAzoaValueEligibilityService _valueEligibility;
    private readonly AzoaSettlementGatewayCapability _capability;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<AzoaSettlementGateway> _logger;

    public AzoaSettlementGateway(
        IAzoaExchangeNode exchange,
        IAzoaValueEligibilityService valueEligibility,
        AzoaSettlementGatewayCapability capability,
        TimeProvider timeProvider,
        ILogger<AzoaSettlementGateway> logger)
    {
        _exchange = exchange;
        _valueEligibility = valueEligibility;
        _capability = capability;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    /// <inheritdoc/>
    public string SelectedNodeId => _capability.SelectedNodeId;

    /// <inheritdoc/>
    public bool CanDispatch => IsTransportUsable;

    /// <inheritdoc/>
    public bool CanReconcile => IsTransportUsable;

    /// <inheritdoc/>
    /// <remarks>Expiry-bounded on purpose: a stale attestation closes funding checkout by itself.</remarks>
    public bool HasCurrentOperatorAttestation
        => _capability.OperatorAttestationExpiresAt is { } expiresAt
            && expiresAt > _timeProvider.GetUtcNow();

    /// <inheritdoc/>
    /// <remarks>
    /// True for this implementation because it settles the complete contract: the durable
    /// idempotency key, canonical base units at a fixed scale, and the immutable terms snapshot.
    /// </remarks>
    public bool SupportsCanonicalFundingSettlement => true;

    private bool IsTransportUsable
        => _capability.TransportConfigured && !string.IsNullOrWhiteSpace(_capability.SelectedNodeId);

    /// <inheritdoc/>
    public async Task<AzoaSettlementGatewayResult> DispatchAsync(
        AzoaSettlementRequest request,
        CancellationToken ct = default)
    {
        var refusal = RefuseUnusableCapability("dispatch");
        if (refusal is not null)
            return refusal;

        if (IsFiatAssetCode(request.AssetCode))
        {
            _logger.LogError(
                "Settlement {SettlementId} carries the currency asset code {AssetCode}; the exchange settles token units only.",
                request.SettlementId,
                request.AssetCode);
            return AzoaSettlementGatewayResult.Retry(
                "AZOA_SETTLEMENT_FIAT_ASSET_REFUSED",
                "This settlement is denominated in a currency. The exchange settles token units, so it was not sent.");
        }

        if (!AzoaSettlementTerms.TryParse(request.TermsSnapshot, out var terms, out var termsError))
        {
            _logger.LogError(
                "Settlement {SettlementId} has unusable exchange terms: {TermsError}",
                request.SettlementId,
                termsError);
            return AzoaSettlementGatewayResult.Retry(
                "AZOA_SETTLEMENT_TERMS_UNREADABLE",
                "The settlement's locked exchange terms could not be read, so nothing was sent to the node.");
        }

        var authorisedBaseUnits = terms!.SettlementLeg == AzoaSettlementLeg.Platform
            ? terms.PlatformAmountBaseUnits
            : terms.ProjectUnitAmountBaseUnits;
        if (!string.Equals(authorisedBaseUnits, request.Amount.BaseUnits, StringComparison.Ordinal))
        {
            _logger.LogError(
                "Settlement {SettlementId} authorises {AuthorisedBaseUnits} base units but its terms declare "
                + "{TermsBaseUnits} on the {SettlementLeg} leg.",
                request.SettlementId,
                request.Amount.BaseUnits,
                authorisedBaseUnits,
                terms.SettlementLeg);
            return AzoaSettlementGatewayResult.Retry(
                "AZOA_SETTLEMENT_TERMS_AMOUNT_MISMATCH",
                "The settlement amount and its locked terms disagree, so nothing was sent to the node.");
        }

        var readiness = await _valueEligibility.RequireReadyAsync(
            request.BeneficiaryUserId,
            "receive settled project value",
            ct);
        if (readiness.IsFailure)
        {
            return AzoaSettlementGatewayResult.Retry(
                readiness.Type == ResultType.Conflict
                    ? "AZOA_ELIGIBILITY_UNCONFIRMED"
                    : "AZOA_ACCOUNT_NOT_READY",
                "The recipient's account setup could not be confirmed, so nothing was settled.");
        }

        if (!Guid.TryParse(readiness.Value!.AvatarId, out var avatarId) || avatarId == Guid.Empty)
        {
            _logger.LogError(
                "Settlement {SettlementId} resolved an unusable avatar reference for its beneficiary.",
                request.SettlementId);
            return AzoaSettlementGatewayResult.Retry(
                "AZOA_SETTLEMENT_AVATAR_UNRESOLVED",
                "The recipient has no usable node account reference, so nothing was settled.");
        }

        var command = terms.ToExchangeCommand();
        var exchange = await _exchange.ExchangeAsync(avatarId, command, request.IdempotencyKey, ct);
        return ToGatewayResult(request, terms, exchange, "dispatch");
    }

    /// <inheritdoc/>
    public async Task<AzoaSettlementGatewayResult> ReconcileAsync(
        AzoaSettlementRequest request,
        CancellationToken ct = default)
    {
        var refusal = RefuseUnusableCapability("reconciliation");
        if (refusal is not null)
            return refusal;

        var receipt = await _exchange.GetReceiptAsync(request.IdempotencyKey, ct);
        if (receipt.Type == ResultType.NotFound)
        {
            // A definitive "no operation under this key" is the only safe re-dispatch signal there
            // is: the key itself keeps a second attempt from creating a second economic effect.
            _logger.LogWarning(
                "Settlement {SettlementId} has no operation on the node under its idempotency key; it returns to dispatch.",
                request.SettlementId);
            return AzoaSettlementGatewayResult.Retry(
                EconomicSettlementFailureCodes.ExchangeNeverLanded,
                "The node holds no operation for this settlement, so it may be dispatched again.");
        }

        // Terms are optional here: an operation that already exists is reconciled on the node's
        // word, and the terms only decide which leg the amounts are checked against.
        AzoaSettlementTerms.TryParse(request.TermsSnapshot, out var terms, out _);
        return ToGatewayResult(request, terms, receipt, "reconciliation");
    }

    /// <summary>Refuses before any node call when the operator has not configured a usable capability.</summary>
    private AzoaSettlementGatewayResult? RefuseUnusableCapability(string operation)
    {
        if (!IsTransportUsable)
        {
            return AzoaSettlementGatewayResult.Retry(
                "AZOA_SETTLEMENT_TRANSPORT_UNCONFIGURED",
                $"AZOA settlement {operation} needs a selected node id and a configured value transport.");
        }

        if (!HasCurrentOperatorAttestation)
        {
            return AzoaSettlementGatewayResult.Retry(
                "AZOA_SETTLEMENT_ATTESTATION_EXPIRED",
                $"AZOA settlement {operation} needs a current operator attestation (Azoa:SettlementAttestationExpiresAt).");
        }

        return null;
    }

    /// <summary>Projects a node exchange result onto the outbox's three typed outcomes.</summary>
    private AzoaSettlementGatewayResult ToGatewayResult(
        AzoaSettlementRequest request,
        AzoaSettlementTerms? terms,
        Result<ArdaNovaExchangeReceipt> exchange,
        string operation)
    {
        if (exchange.IsFailure)
            return ToFailureResult(request, exchange, operation);

        var receipt = exchange.Value!;
        var failureCode = SafeFailureCode(receipt.FailureCode);
        if (receipt.IsTerminal && failureCode is not null)
        {
            _logger.LogWarning(
                "Settlement {SettlementId} was refused by the node during {Operation} with {FailureCode}.",
                request.SettlementId,
                operation,
                failureCode);
            return AzoaSettlementGatewayResult.Retry(
                "AZOA_EXCHANGE_REJECTED",
                "The node refused this exchange and moved no value.");
        }

        var operationId = receipt.OperationId ?? Guid.Empty;
        if (operationId == Guid.Empty
            || !receipt.IsTerminal
            || receipt.RequiresReconciliation)
        {
            return AzoaSettlementGatewayResult.Unknown(
                "AZOA_SETTLEMENT_AWAITING_NODE",
                "The node has not finished this exchange, so its economic effect is not yet durable.");
        }

        if (!TryAccountForAmounts(request, terms, receipt, out var gross, out var fee, out var net))
        {
            _logger.LogCritical(
                "Settlement {SettlementId} came back from the node with amounts that exceed what ArdaNova "
                + "authorised; it is held for reconciliation instead of being recorded.",
                request.SettlementId);
            return AzoaSettlementGatewayResult.Unknown(
                "AZOA_SETTLEMENT_AMOUNT_UNRECONCILED",
                "The node reported an amount ArdaNova did not authorise; an operator must reconcile it.");
        }

        if (gross is null || net is null)
        {
            _logger.LogWarning(
                "Settlement {SettlementId} was accepted without a comparable node amount, so no fee could be recorded.",
                request.SettlementId);
        }

        var persisted = new EconomicSettlementReceipt(
            OperationId: operationId.ToString("D"),
            ReceiptReference: SafeReference(receipt.ReceiptReference),
            State: SafeFailureCode(receipt.State) ?? "Confirmed",
            Replayed: receipt.Replayed,
            GrossBaseUnits: gross,
            FeeBaseUnits: fee,
            NetBaseUnits: net,
            Scale: request.Amount.Scale,
            SettlementReference: SafeReference(receipt.SettlementReference),
            HoldReference: SafeReference(receipt.HoldReference),
            HoldReleasesAt: receipt.HoldReleasesAt);

        return AzoaSettlementGatewayResult.Accepted(
            operationId.ToString("D"),
            persisted.ToJson(),
            receipt.Replayed);
    }

    /// <summary>
    /// Maps a transport failure. The generic failure is AMBIGUOUS on purpose: the node client
    /// converts timeouts and socket faults into it, and a timed-out exchange may still have landed.
    /// </summary>
    private AzoaSettlementGatewayResult ToFailureResult(
        AzoaSettlementRequest request,
        Result<ArdaNovaExchangeReceipt> exchange,
        string operation)
    {
        _logger.LogWarning(
            "Settlement {SettlementId} {Operation} returned {ResultType} from the node.",
            request.SettlementId,
            operation,
            exchange.Type);

        return exchange.Type switch
        {
            ResultType.Forbidden => AzoaSettlementGatewayResult.Retry(
                "AZOA_SETTLEMENT_FORBIDDEN",
                "The node refused this settlement for the recipient, so no value moved."),
            ResultType.Unauthorized => AzoaSettlementGatewayResult.Retry(
                "AZOA_SETTLEMENT_UNAUTHORIZED",
                "The node rejected the settlement credential, so no value moved."),
            ResultType.ValidationError or ResultType.BadRequest => AzoaSettlementGatewayResult.Retry(
                "AZOA_SETTLEMENT_REJECTED",
                "The node rejected this settlement request, so no value moved."),
            ResultType.NotFound => AzoaSettlementGatewayResult.Retry(
                "AZOA_SETTLEMENT_ROUTE_UNAVAILABLE",
                "The node exposes no exchange for this settlement, so no value moved."),
            ResultType.Conflict => AzoaSettlementGatewayResult.Unknown(
                "AZOA_SETTLEMENT_CONFLICT",
                "The node reported a conflicting state for this idempotency key; it must be reconciled."),
            _ => AzoaSettlementGatewayResult.Unknown(
                "AZOA_TRANSPORT_UNCERTAIN",
                "The node returned no durable outcome; this settlement must be reconciled before another dispatch."),
        };
    }

    /// <summary>
    /// Derives gross, fee and net for the settlement's own leg. Returns false only when the node
    /// moved MORE than ArdaNova authorised, which is a refusal rather than a rounding difference.
    /// </summary>
    private static bool TryAccountForAmounts(
        AzoaSettlementRequest request,
        AzoaSettlementTerms? terms,
        ArdaNovaExchangeReceipt receipt,
        out string? gross,
        out string? fee,
        out string? net)
    {
        gross = null;
        fee = null;
        net = null;

        var leg = terms?.SettlementLeg ?? AzoaSettlementLeg.ProjectUnit;
        var settledText = leg == AzoaSettlementLeg.Platform
            ? receipt.AmountSpentBaseUnits
            : receipt.AmountReceivedBaseUnits;

        if (terms is null
            || !BigInteger.TryParse(request.Amount.BaseUnits, out var authorised)
            || !BigInteger.TryParse(settledText, out var settled))
        {
            // Nothing comparable came back; the caller records the operation and warns rather than
            // inventing a fee.
            return true;
        }

        // Platform leg: the node may have SPENT more than authorised, and the surplus is the fee.
        // Project-unit leg: the node may have DELIVERED less than authorised, and the shortfall is.
        var grossValue = leg == AzoaSettlementLeg.Platform ? settled : authorised;
        var netValue = leg == AzoaSettlementLeg.Platform ? authorised : settled;
        var feeValue = grossValue - netValue;
        if (feeValue < BigInteger.Zero)
            return false;

        gross = grossValue.ToString();
        net = netValue.ToString();
        fee = feeValue.ToString();
        return true;
    }

    private static bool IsFiatAssetCode(string? assetCode)
        => !string.IsNullOrWhiteSpace(assetCode) && FiatAssetCodes.Contains(assetCode.Trim());

    /// <summary>Keeps a provider-controlled code to a stable alphabet before it is persisted or logged.</summary>
    private static string? SafeFailureCode(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var trimmed = value.Trim();
        if (trimmed.Length > 64)
            trimmed = trimmed[..64];
        foreach (var character in trimmed)
        {
            if (!char.IsAsciiLetterOrDigit(character) && character is not ('_' or '-'))
                return "UNREADABLE";
        }

        return trimmed;
    }

    /// <summary>Keeps a provider-controlled reference to a stable alphabet before it is persisted.</summary>
    private static string? SafeReference(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var trimmed = value.Trim();
        if (trimmed.Length > 128)
            trimmed = trimmed[..128];
        foreach (var character in trimmed)
        {
            if (!char.IsAsciiLetterOrDigit(character) && character is not ('_' or '-' or ':' or '.'))
                return null;
        }

        return trimmed;
    }
}

/// <summary>
/// The exchange terms a sponsorship intent locked into its settlement, read back at dispatch time.
/// </summary>
/// <remarks>
/// The settlement row carries one amount and one asset code; an exchange needs both legs, the
/// project, its asset, the reserve partition and the declared rate. Those are written once, when
/// the person authorises the sponsorship, and are immutable from then on - which is exactly why
/// this parser refuses rather than defaults: a missing field means the snapshot is not the one the
/// person agreed to.
/// </remarks>
public sealed class AzoaSettlementTerms
{
    private static readonly JsonSerializerOptions ParseOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private AzoaSettlementTerms(
        AzoaSettlementLeg settlementLeg,
        string chainType,
        bool isBuy,
        string projectReference,
        string projectAssetId,
        string reservePartition,
        string platformAmountBaseUnits,
        string projectUnitAmountBaseUnits,
        long platformAmount,
        long projectUnitAmount,
        DeclaredProjectUnitRate rate,
        DateTimeOffset? releaseNotBefore,
        string? purpose)
    {
        SettlementLeg = settlementLeg;
        ChainType = chainType;
        IsBuy = isBuy;
        ProjectReference = projectReference;
        ProjectAssetId = projectAssetId;
        ReservePartition = reservePartition;
        PlatformAmountBaseUnits = platformAmountBaseUnits;
        ProjectUnitAmountBaseUnits = projectUnitAmountBaseUnits;
        PlatformAmount = platformAmount;
        ProjectUnitAmount = projectUnitAmount;
        Rate = rate;
        ReleaseNotBefore = releaseNotBefore;
        Purpose = purpose;
    }

    public AzoaSettlementLeg SettlementLeg { get; }
    public string ChainType { get; }
    public bool IsBuy { get; }
    public string ProjectReference { get; }
    public string ProjectAssetId { get; }
    public string ReservePartition { get; }
    public string PlatformAmountBaseUnits { get; }
    public string ProjectUnitAmountBaseUnits { get; }
    public long PlatformAmount { get; }
    public long ProjectUnitAmount { get; }
    public DeclaredProjectUnitRate Rate { get; }
    public DateTimeOffset? ReleaseNotBefore { get; }
    public string? Purpose { get; }

    /// <summary>Builds the node command. Never a preview: a settlement dispatch always moves value.</summary>
    public ArdaNovaExchangeCommand ToExchangeCommand()
        => new(
            ChainType: ChainType,
            IsBuy: IsBuy,
            ProjectReference: ProjectReference,
            ProjectAssetId: ProjectAssetId,
            PlatformAmountBaseUnits: PlatformAmount,
            ProjectUnitAmountBaseUnits: ProjectUnitAmount,
            ReservePartition: ReservePartition,
            Rate: Rate,
            ReleaseNotBefore: ReleaseNotBefore,
            Purpose: Purpose,
            PreviewOnly: false);

    /// <summary>Parses a settlement's terms snapshot, naming the first thing that made it unusable.</summary>
    public static bool TryParse(string? snapshot, out AzoaSettlementTerms? terms, out string? error)
    {
        terms = null;
        error = null;

        if (string.IsNullOrWhiteSpace(snapshot))
        {
            error = "the settlement carries no terms snapshot";
            return false;
        }

        AzoaSettlementTermsWire? wire;
        try
        {
            wire = JsonSerializer.Deserialize<AzoaSettlementTermsWire>(snapshot, ParseOptions);
        }
        catch (JsonException)
        {
            error = "the terms snapshot is not readable JSON";
            return false;
        }

        if (wire is null)
        {
            error = "the terms snapshot is empty";
            return false;
        }

        if (wire.PreviewOnly)
        {
            error = "the terms snapshot is a preview and may never be settled";
            return false;
        }

        AzoaSettlementLeg leg;
        if (string.Equals(wire.SettlementLeg, "platform", StringComparison.OrdinalIgnoreCase))
            leg = AzoaSettlementLeg.Platform;
        else if (string.Equals(wire.SettlementLeg, "projectUnit", StringComparison.OrdinalIgnoreCase))
            leg = AzoaSettlementLeg.ProjectUnit;
        else
        {
            error = "settlementLeg must be 'platform' or 'projectUnit'";
            return false;
        }

        if (string.IsNullOrWhiteSpace(wire.ChainType))
        {
            error = "chainType is required";
            return false;
        }

        if (string.IsNullOrWhiteSpace(wire.ProjectReference)
            || string.IsNullOrWhiteSpace(wire.ProjectAssetId)
            || string.IsNullOrWhiteSpace(wire.ReservePartition))
        {
            error = "projectReference, projectAssetId and reservePartition are all required";
            return false;
        }

        if (!TryPositiveBaseUnits(wire.PlatformAmountBaseUnits, out var platformAmount))
        {
            error = "platformAmountBaseUnits must be a positive whole number of base units";
            return false;
        }

        if (!TryPositiveBaseUnits(wire.ProjectUnitAmountBaseUnits, out var projectUnitAmount))
        {
            error = "projectUnitAmountBaseUnits must be a positive whole number of base units";
            return false;
        }

        var rateWire = wire.DeclaredRate;
        if (rateWire is null
            || string.IsNullOrWhiteSpace(rateWire.ArdaPerWholeProjectUnit)
            || string.IsNullOrWhiteSpace(rateWire.UsdMajorUnitsPerWholeProjectUnit)
            || string.IsNullOrWhiteSpace(rateWire.Source)
            || rateWire.DeclaredAt is null)
        {
            error = "declaredRate needs a rate, its USD reference, a source and a declaration time";
            return false;
        }

        if (rateWire.ProjectUnitDecimals is < 0 or > 18)
        {
            error = "declaredRate.projectUnitDecimals must be between 0 and 18";
            return false;
        }

        terms = new AzoaSettlementTerms(
            leg,
            wire.ChainType!.Trim(),
            wire.IsBuy ?? true,
            wire.ProjectReference!.Trim(),
            wire.ProjectAssetId!.Trim(),
            wire.ReservePartition!.Trim(),
            wire.PlatformAmountBaseUnits!.Trim(),
            wire.ProjectUnitAmountBaseUnits!.Trim(),
            platformAmount,
            projectUnitAmount,
            new DeclaredProjectUnitRate(
                ProjectTokenConfigId: rateWire.ProjectTokenConfigId ?? string.Empty,
                ArdaPerWholeProjectUnit: rateWire.ArdaPerWholeProjectUnit!.Trim(),
                UsdMajorUnitsPerWholeProjectUnit: rateWire.UsdMajorUnitsPerWholeProjectUnit!.Trim(),
                ProjectUnitDecimals: rateWire.ProjectUnitDecimals,
                Source: rateWire.Source!.Trim(),
                DeclaredAt: rateWire.DeclaredAt.Value,
                Version: rateWire.Version),
            wire.ReleaseNotBefore,
            string.IsNullOrWhiteSpace(wire.Purpose) ? null : wire.Purpose!.Trim());
        return true;
    }

    private static bool TryPositiveBaseUnits(string? value, out long amount)
    {
        amount = 0;
        return !string.IsNullOrWhiteSpace(value)
            && long.TryParse(value.Trim(), out amount)
            && amount > 0;
    }
}

/// <summary>Wire shape of the terms snapshot the sponsorship intent locks onto its settlement.</summary>
internal sealed class AzoaSettlementTermsWire
{
    [JsonPropertyName("settlementLeg")] public string? SettlementLeg { get; set; }
    [JsonPropertyName("chainType")] public string? ChainType { get; set; }
    [JsonPropertyName("isBuy")] public bool? IsBuy { get; set; }
    [JsonPropertyName("projectReference")] public string? ProjectReference { get; set; }
    [JsonPropertyName("projectAssetId")] public string? ProjectAssetId { get; set; }
    [JsonPropertyName("reservePartition")] public string? ReservePartition { get; set; }
    [JsonPropertyName("platformAmountBaseUnits")] public string? PlatformAmountBaseUnits { get; set; }
    [JsonPropertyName("projectUnitAmountBaseUnits")] public string? ProjectUnitAmountBaseUnits { get; set; }
    [JsonPropertyName("releaseNotBefore")] public DateTimeOffset? ReleaseNotBefore { get; set; }
    [JsonPropertyName("purpose")] public string? Purpose { get; set; }
    [JsonPropertyName("previewOnly")] public bool PreviewOnly { get; set; }
    [JsonPropertyName("declaredRate")] public AzoaSettlementRateWire? DeclaredRate { get; set; }
}

/// <summary>Wire shape of the rate ArdaNova declared when the person authorised the sponsorship.</summary>
internal sealed class AzoaSettlementRateWire
{
    [JsonPropertyName("projectTokenConfigId")] public string? ProjectTokenConfigId { get; set; }
    [JsonPropertyName("ardaPerWholeProjectUnit")] public string? ArdaPerWholeProjectUnit { get; set; }
    [JsonPropertyName("usdMajorUnitsPerWholeProjectUnit")] public string? UsdMajorUnitsPerWholeProjectUnit { get; set; }
    [JsonPropertyName("projectUnitDecimals")] public int ProjectUnitDecimals { get; set; }
    [JsonPropertyName("source")] public string? Source { get; set; }
    [JsonPropertyName("declaredAt")] public DateTimeOffset? DeclaredAt { get; set; }
    [JsonPropertyName("version")] public string? Version { get; set; }
}
