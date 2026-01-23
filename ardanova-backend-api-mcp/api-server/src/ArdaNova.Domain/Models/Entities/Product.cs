namespace ArdaNova.Domain.Models.Entities;

public class Product
{
    public Guid Id { get; private set; }
    public Guid BusinessId { get; private set; }
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public string? Sku { get; private set; }
    public decimal Price { get; private set; }
    public decimal? Cost { get; private set; }
    public string? Category { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public Guid UserId { get; private set; }

    // Navigation properties
    public Business Business { get; private set; } = null!;
    public User User { get; private set; } = null!;
    public InventoryItem? InventoryItem { get; private set; }
    public ICollection<SaleItem> SaleItems { get; private set; } = new List<SaleItem>();

    private Product() { }

    public static Product Create(
        Guid businessId,
        Guid userId,
        string name,
        decimal price,
        string? description = null,
        string? sku = null,
        decimal? cost = null,
        string? category = null)
    {
        return new Product
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            UserId = userId,
            Name = name,
            Price = price,
            Description = description,
            Sku = sku,
            Cost = cost,
            Category = category,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public void Update(string name, string? description, string? sku, decimal price, decimal? cost, string? category)
    {
        Name = name;
        Description = description;
        Sku = sku;
        Price = price;
        Cost = cost;
        Category = category;
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
}
