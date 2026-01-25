namespace ArdaNova.Application.DTOs;

using ArdaNova.Domain.Models.Enums;

// TokenSwap DTOs
public record TokenSwapDto
{
    public string Id { get; init; }
    public string UserId { get; init; }
    public string FromTokenId { get; init; }
    public string ToTokenId { get; init; }
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
    public required string UserId { get; init; }
    public required string FromTokenId { get; init; }
    public required string ToTokenId { get; init; }
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
    public string Id { get; init; }
    public string Token1Id { get; init; }
    public string Token2Id { get; init; }
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
    public required string Token1Id { get; init; }
    public required string Token2Id { get; init; }
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
    public string Id { get; init; }
    public string PoolId { get; init; }
    public string UserId { get; init; }
    public decimal Shares { get; init; }
    public decimal Token1In { get; init; }
    public decimal Token2In { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}

public record CreateLiquidityProviderDto
{
    public required string PoolId { get; init; }
    public required string UserId { get; init; }
    public required decimal Shares { get; init; }
    public required decimal Token1In { get; init; }
    public required decimal Token2In { get; init; }
}
