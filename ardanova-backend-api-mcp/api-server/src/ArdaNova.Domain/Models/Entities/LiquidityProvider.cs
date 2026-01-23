namespace ArdaNova.Domain.Models.Entities;

public class LiquidityProvider
{
    public Guid Id { get; private set; }
    public Guid PoolId { get; private set; }
    public Guid UserId { get; private set; }
    public decimal Shares { get; private set; }
    public decimal Token1In { get; private set; }
    public decimal Token2In { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    // Navigation
    public LiquidityPool Pool { get; private set; } = null!;
    public User User { get; private set; } = null!;

    private LiquidityProvider() { }

    public static LiquidityProvider Create(
        Guid poolId,
        Guid userId,
        decimal shares,
        decimal token1In,
        decimal token2In)
    {
        return new LiquidityProvider
        {
            Id = Guid.NewGuid(),
            PoolId = poolId,
            UserId = userId,
            Shares = shares,
            Token1In = token1In,
            Token2In = token2In,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public void AddLiquidity(decimal additionalShares, decimal token1, decimal token2)
    {
        Shares += additionalShares;
        Token1In += token1;
        Token2In += token2;
        UpdatedAt = DateTime.UtcNow;
    }

    public void RemoveLiquidity(decimal sharesToRemove, decimal token1, decimal token2)
    {
        Shares -= sharesToRemove;
        Token1In -= token1;
        Token2In -= token2;
        UpdatedAt = DateTime.UtcNow;
    }
}
