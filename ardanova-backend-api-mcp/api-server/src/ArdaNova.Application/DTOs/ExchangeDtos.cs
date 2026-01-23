namespace ArdaNova.Application.DTOs;

using ArdaNova.Domain.Models.Enums;

// TokenSwap DTOs
public record TokenSwapDto
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public Guid FromTokenId { get; init; }
    public Guid ToTokenId { get; init; }
    public decimal FromAmount { get; init; }
    public decimal ToAmount { get; init; }
    public decimal ExchangeRate { get; init; }
    public decimal Fee { get; init; }
    public string? TxHash { get; init; }
    public SwapStatus Status { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? CompletedAt { get; init; }
}

public record CreateTokenSwapDto
{
    public required Guid UserId { get; init; }
    public required Guid FromTokenId { get; init; }
    public required Guid ToTokenId { get; init; }
    public required decimal FromAmount { get; init; }
    public required decimal ToAmount { get; init; }
    public required decimal ExchangeRate { get; init; }
    public decimal Fee { get; init; } = 0;
}

public record CompleteSwapDto
{
    public string? TxHash { get; init; }
}

// LiquidityPool DTOs
public record LiquidityPoolDto
{
    public Guid Id { get; init; }
    public Guid Token1Id { get; init; }
    public Guid Token2Id { get; init; }
    public decimal Reserve1 { get; init; }
    public decimal Reserve2 { get; init; }
    public decimal TotalShares { get; init; }
    public decimal FeePercent { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}

public record CreateLiquidityPoolDto
{
    public required Guid Token1Id { get; init; }
    public required Guid Token2Id { get; init; }
    public decimal FeePercent { get; init; } = 0.003m;
}

public record AddLiquidityDto
{
    public required decimal Amount1 { get; init; }
    public required decimal Amount2 { get; init; }
    public required decimal Shares { get; init; }
}

public record RemoveLiquidityDto
{
    public required decimal Amount1 { get; init; }
    public required decimal Amount2 { get; init; }
    public required decimal Shares { get; init; }
}

// LiquidityProvider DTOs
public record LiquidityProviderDto
{
    public Guid Id { get; init; }
    public Guid PoolId { get; init; }
    public Guid UserId { get; init; }
    public decimal Shares { get; init; }
    public decimal Token1In { get; init; }
    public decimal Token2In { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}

public record CreateLiquidityProviderDto
{
    public required Guid PoolId { get; init; }
    public required Guid UserId { get; init; }
    public required decimal Shares { get; init; }
    public required decimal Token1In { get; init; }
    public required decimal Token2In { get; init; }
}
