namespace ArdaNova.Domain.Models.Entities;

public class LiquidityPool
{
    public Guid Id { get; private set; }
    public Guid Token1Id { get; private set; }
    public Guid Token2Id { get; private set; }
    public decimal Reserve1 { get; private set; }
    public decimal Reserve2 { get; private set; }
    public decimal TotalShares { get; private set; }
    public decimal FeePercent { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    // Navigation
    public ICollection<LiquidityProvider> Providers { get; private set; } = new List<LiquidityProvider>();

    private LiquidityPool() { }

    public static LiquidityPool Create(
        Guid token1Id,
        Guid token2Id,
        decimal feePercent = 0.003m)
    {
        return new LiquidityPool
        {
            Id = Guid.NewGuid(),
            Token1Id = token1Id,
            Token2Id = token2Id,
            Reserve1 = 0,
            Reserve2 = 0,
            TotalShares = 0,
            FeePercent = feePercent,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public void AddLiquidity(decimal amount1, decimal amount2, decimal shares)
    {
        Reserve1 += amount1;
        Reserve2 += amount2;
        TotalShares += shares;
        UpdatedAt = DateTime.UtcNow;
    }

    public void RemoveLiquidity(decimal amount1, decimal amount2, decimal shares)
    {
        Reserve1 -= amount1;
        Reserve2 -= amount2;
        TotalShares -= shares;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateReserves(decimal reserve1, decimal reserve2)
    {
        Reserve1 = reserve1;
        Reserve2 = reserve2;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public decimal GetExchangeRate() => Reserve1 > 0 ? Reserve2 / Reserve1 : 0;
}
