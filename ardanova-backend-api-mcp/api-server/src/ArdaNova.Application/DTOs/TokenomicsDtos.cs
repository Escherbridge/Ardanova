namespace ArdaNova.Application.DTOs;

using System.ComponentModel.DataAnnotations;
using ArdaNova.Domain.Models.Enums;

// ==================== Project Token Config ====================

public record ProjectTokenConfigDto
{
    public string Id { get; init; } = null!;
    public string ProjectId { get; init; } = null!;
    public string? AssetId { get; init; }
    public string AssetName { get; init; } = null!;
    public string UnitName { get; init; } = null!;
    public int? AssetScale { get; init; }
    public int TotalSupply { get; init; }
    public int AllocatedSupply { get; init; }
    public int DistributedSupply { get; init; }
    public int ReservedSupply { get; init; }
    public string? MintTxHash { get; init; }
    public ProjectTokenStatus Status { get; init; }
    public double FundingGoal { get; init; }
    public double FundingRaised { get; init; }
    public ProjectGateStatus GateStatus { get; init; }
    public DateTime? Gate1ClearedAt { get; init; }
    public DateTime? Gate2ClearedAt { get; init; }
    public DateTime? FailedAt { get; init; }
    public int ContributorSupply { get; init; }
    public int InvestorSupply { get; init; }
    public int FounderSupply { get; init; }
    public int BurnedSupply { get; init; }
    public string? SuccessCriteria { get; init; }
    public string? SuccessVerifiedBy { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }

    /// <summary>Computed: totalSupply - contributorSupply - investorSupply - founderSupply - burnedSupply</summary>
    public int AvailableSupply => TotalSupply - ContributorSupply - InvestorSupply - FounderSupply - BurnedSupply;
}

public record ProjectTokenMetadataDto
{
    public string Id { get; init; } = null!;
    public string AssetName { get; init; } = null!;
    public string UnitName { get; init; } = null!;
}

public record ProjectTokenMetadataBatchRequestDto
{
    public IReadOnlyList<string> Ids { get; init; } = [];
}

public record ProjectTokenMetadataBatchDto
{
    public IReadOnlyList<ProjectTokenMetadataDto> Items { get; init; } = [];
    public IReadOnlyList<string> MissingIds { get; init; } = [];
}

public record CreateProjectTokenConfigDto
{
    public required string ProjectId { get; init; }
    public required string AssetName { get; init; }
    public required string UnitName { get; init; }
    [Required]
    [Range(0, 18)]
    public required int? AssetScale { get; init; }
    public required int TotalSupply { get; init; }
    public required double FundingGoal { get; init; }

    /// <summary>
    /// The unit <see cref="FundingGoal"/> is denominated in. REQUIRED, and refused unless it is
    /// the unit the funding columns are kept in: this door writes straight into the column the
    /// opening price divides, and a default would let an omitted unit stand in for a declared one.
    /// </summary>
    public required string FundingGoalUnit { get; init; }

    public double ReservedPercentage { get; init; }
    public string? SuccessCriteria { get; init; }
}

// ==================== Token Allocation ====================

public record TokenAllocationDto
{
    public string Id { get; init; } = null!;
    public string ProjectTokenConfigId { get; init; } = null!;
    public string? PbiId { get; init; }
    public string? RecipientUserId { get; init; }
    public double EquityPercentage { get; init; }
    public int TokenAmount { get; init; }
    public AllocationStatus Status { get; init; }
    public TokenHolderClass HolderClass { get; init; }
    public bool IsLiquid { get; init; }
    public DateTime? DistributedAt { get; init; }
    public string? DistributionTxHash { get; init; }
    public DateTime? BurnedAt { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}

public record CreateTokenAllocationDto
{
    public string? PbiId { get; init; }
    public required double EquityPercentage { get; init; }
}

public record CreateInvestorAllocationDto
{
    public required string UserId { get; init; }
    public required double UsdAmount { get; init; }
    public required int TokenAmount { get; init; }
}

public record CreateFounderAllocationDto
{
    public required string UserId { get; init; }
    public required double EquityPercentage { get; init; }
}

// ==================== Token Balance ====================

/// <summary>One holding, in the asset's BASE units, stamped with where the figure came from.</summary>
public record TokenBalanceDto
{
    public string Id { get; init; } = null!;
    public string UserId { get; init; } = null!;
    public string? ProjectTokenConfigId { get; init; }
    public bool IsPlatformToken { get; init; }
    public TokenHolderClass? HolderClass { get; init; }
    public bool IsLiquid { get; init; }

    /// <summary>Units in the asset's base units. Fixed scale, never narrowed to int.</summary>
    public long Balance { get; init; }

    /// <summary>Base units committed elsewhere and therefore unavailable to move.</summary>
    public long LockedBalance { get; init; }

    /// <summary>The on-chain asset these units count; null when the row is internal bookkeeping.</summary>
    public string? AssetId { get; init; }

    /// <summary>Custodial address the units were observed at.</summary>
    public string? WalletAddress { get; init; }

    /// <summary>When a chain read produced this figure. Null means it was never observed on chain.</summary>
    public DateTime? ObservedAt { get; init; }

    public DateTime UpdatedAt { get; init; }

    /// <summary>True only when a chain read against a named asset produced this figure.</summary>
    public bool IsChainObserved => ObservedAt.HasValue && !string.IsNullOrWhiteSpace(AssetId);

    /// <summary>Base units free to move (balance - lockedBalance)</summary>
    public long AvailableBalance => Balance - LockedBalance;
}

/// <summary>
/// Everything one person holds. Valuation figures are null - not zero - whenever no chain-backed
/// rate produced them, so an unavailable read can never be rendered as an empty portfolio.
/// </summary>
public record UserPortfolioDto
{
    public string UserId { get; init; } = null!;
    public IReadOnlyList<TokenBalanceDto> Holdings { get; init; } = [];

    /// <summary>The person's platform unit, read from their custodial wallet on the node.</summary>
    public TokenBalanceDto? ArdaBalance { get; init; }

    /// <summary>Why the platform-unit balance is absent. Null when it was read successfully.</summary>
    public string? ArdaBalanceUnavailableReason { get; init; }

    public decimal? TotalLiquidValueUsd { get; init; }
    public decimal? TotalLockedValueUsd { get; init; }
    public decimal? TotalPortfolioValueUsd { get; init; }

    /// <summary>Why the valuation figures are absent. Null when they were computed.</summary>
    public string? ValuationUnavailableReason { get; init; }
}

// ==================== Payout Request ====================

public record PayoutRequestDto
{
    public string Id { get; init; } = null!;
    public string UserId { get; init; } = null!;
    public string? SourceProjectTokenConfigId { get; init; }
    public int SourceTokenAmount { get; init; }
    public int? ArdaTokenAmount { get; init; }
    public double? UsdAmount { get; init; }
    public PayoutStatus Status { get; init; }
    public TokenHolderClass HolderClass { get; init; }
    public ProjectGateStatus GateStatusAtRequest { get; init; }
    public string? ConversionTxHash { get; init; }
    public string? PayoutTxHash { get; init; }
    public string? StripePayoutId { get; init; }
    public string? FailureReason { get; init; }
    public DateTime RequestedAt { get; init; }
    public DateTime? ProcessedAt { get; init; }
    public DateTime? CompletedAt { get; init; }
}

public record CreatePayoutRequestDto
{
    public required string SourceProjectTokenConfigId { get; init; }
    public required int SourceTokenAmount { get; init; }
    public required TokenHolderClass HolderClass { get; init; }
}

// ==================== Project Gate ====================

public record ProjectGateStatusDto
{
    public string ProjectTokenConfigId { get; init; } = null!;
    public ProjectGateStatus GateStatus { get; init; }
    public double FundingGoal { get; init; }
    public double FundingRaised { get; init; }
    public double FundingProgress => FundingGoal > 0 ? FundingRaised / FundingGoal : 0;
    public DateTime? Gate1ClearedAt { get; init; }
    public DateTime? Gate2ClearedAt { get; init; }
    public DateTime? FailedAt { get; init; }
}

public record GateTransitionResultDto
{
    public bool Transitioned { get; init; }
    public ProjectGateStatus PreviousStatus { get; init; }
    public ProjectGateStatus NewStatus { get; init; }

    /// <summary>Base units unlocked. Long because a holding is a long; a gate must not narrow one.</summary>
    public long TokensUnlocked { get; init; }

    /// <summary>Base units burned.</summary>
    public long TokensBurned { get; init; }

    public double TrustProtectionPaid { get; init; }
}

// ==================== Treasury ====================

/// <summary>
/// The treasury as a POLICY VIEW over one balance, plus whether a chain read backed it.
/// </summary>
/// <remarks>
/// The three bucket balances are NULL unless a chain read produced them, so an unavailable read
/// can never be rendered as "$0.00". The percentages beside them are real configuration and are
/// always present.
/// </remarks>
public record TreasuryStatusDto
{
    public string Id { get; init; } = null!;
    public long ArdaTotalSupply { get; init; }
    public long ArdaCirculatingSupply { get; init; }
    public string? ArdaAssetId { get; init; }

    /// <summary>Policy share of the observed balance. Null when no chain read backed this status.</summary>
    public decimal? IndexFundBalance { get; init; }
    public decimal? LiquidReserveBalance { get; init; }
    public decimal? OperationsBalance { get; init; }

    /// <summary>True only when a chain read produced the balances above.</summary>
    public bool IsChainBacked { get; init; }

    /// <summary>The observed balance in minor units - the one number the split is a view of.</summary>
    public long? ChainBalanceMinorUnits { get; init; }

    /// <summary>Address of the node-side treasury account the balance was read from.</summary>
    public string? TreasuryAddress { get; init; }

    /// <summary>When the chain read happened.</summary>
    public DateTimeOffset? BalanceObservedAt { get; init; }

    /// <summary>Why no balance is reported. Null when one is.</summary>
    public string? BalanceUnavailableReason { get; init; }

    public decimal? TotalTreasury => IsChainBacked
        ? (IndexFundBalance ?? 0m) + (LiquidReserveBalance ?? 0m) + (OperationsBalance ?? 0m)
        : null;

    public decimal? ArdaValueUsd => TotalTreasury is { } total && ArdaCirculatingSupply > 0
        ? total / ArdaCirculatingSupply
        : null;

    public double IndexFundAllocationPct { get; init; }
    public double LiquidReserveAllocationPct { get; init; }
    public double OperationsAllocationPct { get; init; }
    public double IndexFundAnnualReturn { get; init; }
    public double PlatformProfitSharePct { get; init; }
    public double TrustProtectionRate { get; init; }
    public double TotalInflows { get; init; }
    public double TotalPayouts { get; init; }
    public double TotalRebalanceTransfers { get; init; }
    public DateTime? LastRebalanceAt { get; init; }
    public DateTime? LastReconciliationAt { get; init; }
}

/// <summary>
/// A treasury RECEIPT: what ArdaNova recorded about a move, and the node operation it attests to.
/// </summary>
/// <remarks>
/// <see cref="AmountMinorUnits"/> is the exact figure; <see cref="Amount"/> is its display
/// projection and is never read back into money math. A row with no <see cref="AzoaOperationId"/>
/// records ArdaNova's own policy split, not a value move on the node - which is what
/// <see cref="IsChainReceipt"/> says out loud.
/// </remarks>
public record PlatformTreasuryTransactionDto
{
    public string Id { get; init; } = null!;
    public PlatformTreasuryTransactionType Type { get; init; }

    /// <summary>Display projection of <see cref="AmountMinorUnits"/>.</summary>
    public double Amount { get; init; }

    /// <summary>The exact amount, in minor units (cents).</summary>
    public long AmountMinorUnits { get; init; }

    public string? FromBucket { get; init; }
    public string? ToBucket { get; init; }
    public string? RelatedProjectId { get; init; }
    public string? RelatedPayoutRequestId { get; init; }
    public string? Description { get; init; }

    /// <summary>Display projection of <see cref="BalanceAfterMinorUnits"/>.</summary>
    public double BalanceAfter { get; init; }

    /// <summary>The policy-view balance after this receipt, in minor units. Null when only the node knows it.</summary>
    public long? BalanceAfterMinorUnits { get; init; }

    /// <summary>Durable node operation this receipt is for.</summary>
    public string? AzoaOperationId { get; init; }

    /// <summary>The stable key the node deduped the move on; the reconcile handle.</summary>
    public string? AzoaIdempotencyKey { get; init; }

    /// <summary>Opaque node receipt reference, safe to log and echo back.</summary>
    public string? AzoaReceiptReference { get; init; }

    /// <summary>The on-chain asset the move was counted in.</summary>
    public string? ChainAssetId { get; init; }

    /// <summary>Address of the project's node-side treasury this receipt credits.</summary>
    public string? ProjectTreasuryAddress { get; init; }

    /// <summary>True only when this row points at a node operation that can be reconciled.</summary>
    public bool IsChainReceipt => !string.IsNullOrWhiteSpace(AzoaOperationId);

    public DateTime CreatedAt { get; init; }
}

// ==================== Project Investment ====================

/// <summary>
/// One sponsorship, as anyone who can see the project's investors may see it.
/// </summary>
/// <remarks>
/// The payment-provider id is NOT here. It identifies a real charge on a real card and belongs
/// only to the sponsor and to operators; the investors surface is readable by people who are
/// neither. Any admin surface that needs it reads the <c>ProjectInvestment</c> row directly.
/// </remarks>
public record ProjectInvestmentDto
{
    public string Id { get; init; } = null!;
    public string ProjectTokenConfigId { get; init; } = null!;
    public string UserId { get; init; } = null!;
    public decimal UsdAmount { get; init; }
    public long TokenAmount { get; init; }
    public DateTime InvestedAt { get; init; }
    public bool ProtectionEligible { get; init; }
    public bool ProtectionPaidOut { get; init; }
    public decimal? ProtectionAmount { get; init; }
    public DateTime? ProtectionPaidAt { get; init; }
}

// ==================== Exchange ====================

/// <summary>
/// What a holder gets for their project units, priced in ARDA in ONE hop.
/// </summary>
/// <remarks>
/// The exact fields are the contract: <see cref="ArdaPerProjectUnit"/>,
/// <see cref="ArdaAmountExact"/> and <see cref="ArdaMinorUnits"/> are decimal/long and carry no
/// rounding. The four <c>double</c> members below them are the pre-W5.3 display surface that
/// <c>PayoutService</c> still types that way; they are written once from the exact figures and
/// are never read back into money math. Wave 8 removes them with PayoutService.
/// </remarks>
public record ConversionPreviewDto
{
    public int SourceTokenAmount { get; init; }

    /// <summary>The declared one-hop rate: ARDA per whole project unit.</summary>
    public decimal ArdaPerProjectUnit { get; init; }

    /// <summary>ARDA the holder receives, exact.</summary>
    public decimal ArdaAmountExact { get; init; }

    /// <summary>ARDA the holder receives, in minor units, floored. The figure a value move uses.</summary>
    public long ArdaMinorUnits { get; init; }

    /// <summary>D16 reference only: what one whole project unit was worth in dollars at this rate.</summary>
    public decimal ProjectUnitValueUsdExact { get; init; }

    /// <summary>D16 reference only: what one ARDA was worth in dollars at this rate.</summary>
    public decimal ArdaValueUsdExact { get; init; }

    /// <summary>Where the rate came from, as recorded on the node's receipt.</summary>
    public string? RateSource { get; init; }

    /// <summary>When ArdaNova observed this rate.</summary>
    public DateTimeOffset? RateDeclaredAt { get; init; }

    // -- Pre-W5.3 display surface (Wave 8 removes these with PayoutService) --

    public double ProjectTokenValueUsd { get; init; }
    public double ArdaValueUsd { get; init; }
    public double UsdValue { get; init; }

    /// <summary>Whole ARDA, floored. Use <see cref="ArdaMinorUnits"/> for anything that moves value.</summary>
    public long ArdaAmount { get; init; }
}
