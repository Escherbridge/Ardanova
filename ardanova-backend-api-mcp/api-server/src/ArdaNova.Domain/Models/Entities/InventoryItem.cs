namespace ArdaNova.Domain.Models.Entities;

public class InventoryItem
{
    public Guid Id { get; private set; }
    public Guid BusinessId { get; private set; }
    public Guid ProductId { get; private set; }
    public int CurrentStock { get; private set; }
    public int MinStock { get; private set; }
    public int? MaxStock { get; private set; }
    public int? ReorderPoint { get; private set; }
    public DateTime? LastRestocked { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public Guid UserId { get; private set; }

    // Navigation properties
    public Business Business { get; private set; } = null!;
    public Product Product { get; private set; } = null!;
    public User User { get; private set; } = null!;

    private InventoryItem() { }

    public static InventoryItem Create(
        Guid businessId,
        Guid productId,
        Guid userId,
        int currentStock = 0,
        int minStock = 0,
        int? maxStock = null,
        int? reorderPoint = null)
    {
        return new InventoryItem
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            ProductId = productId,
            UserId = userId,
            CurrentStock = currentStock,
            MinStock = minStock,
            MaxStock = maxStock,
            ReorderPoint = reorderPoint,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public void UpdateStock(int quantity)
    {
        CurrentStock = quantity;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddStock(int quantity)
    {
        CurrentStock += quantity;
        LastRestocked = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void RemoveStock(int quantity)
    {
        CurrentStock = Math.Max(0, CurrentStock - quantity);
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetStockLevels(int minStock, int? maxStock, int? reorderPoint)
    {
        MinStock = minStock;
        MaxStock = maxStock;
        ReorderPoint = reorderPoint;
        UpdatedAt = DateTime.UtcNow;
    }

    public bool NeedsReorder() => ReorderPoint.HasValue && CurrentStock <= ReorderPoint.Value;

    public bool IsLowStock() => CurrentStock <= MinStock;
}
