namespace ArdaNova.Application.DTOs;

using ArdaNova.Domain.Models.Enums;

// ==================== Swap Preview ====================

public record SwapPreviewDto(
    int SourceTokenAmount,
    string SourceUnitName,
    decimal SourceUsdValue,
    decimal ArdaAmount,
    int TargetTokenAmount,
    string TargetUnitName,
    decimal TargetUsdValue,
    decimal SourceTokenRate,
    decimal TargetTokenRate,
    decimal ArdaRate
);

// ==================== Swap Request ====================

public record SwapRequestDto
{
    public required string SourceConfigId { get; init; }
    public required string TargetConfigId { get; init; }
    public required int SourceTokenAmount { get; init; }
}

// ==================== Swap Result ====================

public record SwapResultDto
{
    public string Id { get; init; } = null!;
    public string UserId { get; init; } = null!;
    public string SourceConfigId { get; init; } = null!;
    public string TargetConfigId { get; init; } = null!;
    public int SourceTokenAmount { get; init; }
    public string SourceUnitName { get; init; } = null!;
    public decimal SourceUsdValue { get; init; }
    public decimal ArdaAmount { get; init; }
    public int TargetTokenAmount { get; init; }
    public string TargetUnitName { get; init; } = null!;
    public decimal TargetUsdValue { get; init; }
    public SwapStatus Status { get; init; }
    public DateTime CreatedAt { get; init; }
}

// ==================== Swap History ====================

public record SwapHistoryDto
{
    public string Id { get; init; } = null!;
    public string SourceUnitName { get; init; } = null!;
    public int SourceTokenAmount { get; init; }
    public string TargetUnitName { get; init; } = null!;
    public int TargetTokenAmount { get; init; }
    public decimal SourceUsdValue { get; init; }
    public decimal TargetUsdValue { get; init; }
    public SwapStatus Status { get; init; }
    public DateTime CreatedAt { get; init; }
}
