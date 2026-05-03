namespace ArdaNova.Application.DTOs;

using ArdaNova.Domain.Models.Enums;

// ==================== Project Token Config ====================

public record ProjectTokenConfigDto
{
    public string Id { get; init; } = null!;
    public string ProjectId { get; init; } = null!;
    public string? AssetId { get; init; }
    public string AssetName { get; init; } = null!;
    public string UnitName { get; init; } = null!;
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

public record CreateProjectTokenConfigDto
{
    public required string ProjectId { get; init; }
    public required string AssetName { get; init; }
    public required string UnitName { get; init; }
    public required int TotalSupply { get; init; }
    public required double FundingGoal { get; init; }
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

public record TokenBalanceDto
{
    public string Id { get; init; } = null!;
    public string UserId { get; init; } = null!;
    public string? ProjectTokenConfigId { get; init; }
    public bool IsPlatformToken { get; init; }
    public TokenHolderClass? HolderClass { get; init; }
    public bool IsLiquid { get; init; }
    public int Balance { get; init; }
    public int LockedBalance { get; init; }
    public DateTime UpdatedAt { get; init; }

    /// <summary>Balance available for payout (balance - lockedBalance)</summary>
    public int AvailableBalance => Balance - LockedBalance;
}

public record UserPortfolioDto
{
    public string UserId { get; init; } = null!;
    public IReadOnlyList<TokenBalanceDto> Holdings { get; init; } = [];
    public TokenBalanceDto? ArdaBalance { get; init; }
    public double TotalLiquidValueUsd { get; init; }
    public double TotalLockedValueUsd { get; init; }
    public double TotalPortfolioValueUsd { get; init; }
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
    public int TokensUnlocked { get; init; }
    public int TokensBurned { get; init; }
    public double TrustProtectionPaid { get; init; }
}

// ==================== Treasury ====================

public record TreasuryStatusDto
{
    public string Id { get; init; } = null!;
    public long ArdaTotalSupply { get; init; }
    public long ArdaCirculatingSupply { get; init; }
    public string? ArdaAssetId { get; init; }
    public double IndexFundBalance { get; init; }
    public double LiquidReserveBalance { get; init; }
    public double OperationsBalance { get; init; }
    public double TotalTreasury => IndexFundBalance + LiquidReserveBalance + OperationsBalance;
    public double ArdaValueUsd => ArdaCirculatingSupply > 0 ? TotalTreasury / ArdaCirculatingSupply : 0;
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

public record PlatformTreasuryTransactionDto
{
    public string Id { get; init; } = null!;
    public PlatformTreasuryTransactionType Type { get; init; }
    public double Amount { get; init; }
    public string? FromBucket { get; init; }
    public string? ToBucket { get; init; }
    public string? RelatedProjectId { get; init; }
    public string? RelatedPayoutRequestId { get; init; }
    public string? Description { get; init; }
    public double BalanceAfter { get; init; }
    public DateTime CreatedAt { get; init; }
}

// ==================== Project Investment ====================

public record ProjectInvestmentDto
{
    public string Id { get; init; } = null!;
    public string ProjectTokenConfigId { get; init; } = null!;
    public string UserId { get; init; } = null!;
    public double UsdAmount { get; init; }
    public int TokenAmount { get; init; }
    public string? StripePaymentIntentId { get; init; }
    public DateTime InvestedAt { get; init; }
    public bool ProtectionEligible { get; init; }
    public bool ProtectionPaidOut { get; init; }
    public double? ProtectionAmount { get; init; }
    public DateTime? ProtectionPaidAt { get; init; }
}

// ==================== Exchange ====================

public record ConversionPreviewDto
{
    public double ProjectTokenValueUsd { get; init; }
    public double ArdaValueUsd { get; init; }
    public int SourceTokenAmount { get; init; }
    public double UsdValue { get; init; }
    public long ArdaAmount { get; init; }
}
