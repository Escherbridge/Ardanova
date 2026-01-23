namespace ArdaNova.Domain.Models.Entities;

using System.Text.Json.Serialization;
using ArdaNova.Domain.Models.Enums;

public class Sale
{
    public Guid Id { get; private set; }
    public Guid BusinessId { get; private set; }
    public Guid? CustomerId { get; private set; }
    public decimal Total { get; private set; }
    public decimal? Tax { get; private set; }
    public decimal? Discount { get; private set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public PaymentMethod PaymentMethod { get; private set; }

    public string? Notes { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public Guid UserId { get; private set; }

    // Navigation properties
    public Business Business { get; private set; } = null!;
    public Customer? Customer { get; private set; }
    public User User { get; private set; } = null!;
    public ICollection<SaleItem> Items { get; private set; } = new List<SaleItem>();

    private Sale() { }

    public static Sale Create(
        Guid businessId,
        Guid userId,
        decimal total,
        PaymentMethod paymentMethod,
        Guid? customerId = null,
        decimal? tax = null,
        decimal? discount = null,
        string? notes = null)
    {
        return new Sale
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            UserId = userId,
            CustomerId = customerId,
            Total = total,
            Tax = tax,
            Discount = discount,
            PaymentMethod = paymentMethod,
            Notes = notes,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void RecalculateTotal(decimal itemsTotal)
    {
        Total = itemsTotal + (Tax ?? 0) - (Discount ?? 0);
    }
}
