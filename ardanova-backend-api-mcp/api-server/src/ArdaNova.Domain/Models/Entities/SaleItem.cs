namespace ArdaNova.Domain.Models.Entities;

public class SaleItem
{
    public Guid Id { get; private set; }
    public Guid SaleId { get; private set; }
    public Guid ProductId { get; private set; }
    public int Quantity { get; private set; }
    public decimal Price { get; private set; }
    public decimal Total { get; private set; }

    // Navigation properties
    public Sale Sale { get; private set; } = null!;
    public Product Product { get; private set; } = null!;

    private SaleItem() { }

    public static SaleItem Create(
        Guid saleId,
        Guid productId,
        int quantity,
        decimal price)
    {
        return new SaleItem
        {
            Id = Guid.NewGuid(),
            SaleId = saleId,
            ProductId = productId,
            Quantity = quantity,
            Price = price,
            Total = quantity * price
        };
    }

    public void UpdateQuantity(int quantity)
    {
        Quantity = quantity;
        Total = Quantity * Price;
    }
}
