namespace ArdaNova.Domain.Models.Entities;

using System.Text.Json.Serialization;
using ArdaNova.Domain.Models.Enums;

public class Invoice
{
    public Guid Id { get; private set; }
    public Guid BusinessId { get; private set; }
    public Guid CustomerId { get; private set; }
    public string InvoiceNumber { get; private set; } = null!;
    public decimal Amount { get; private set; }
    public decimal? Tax { get; private set; }
    public decimal? Discount { get; private set; }
    public decimal Total { get; private set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public InvoiceStatus Status { get; private set; }

    public DateTime DueDate { get; private set; }
    public DateTime? PaidAt { get; private set; }
    public string? Notes { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public Guid UserId { get; private set; }

    // Navigation properties
    public Business Business { get; private set; } = null!;
    public Customer Customer { get; private set; } = null!;
    public User User { get; private set; } = null!;

    private Invoice() { }

    public static Invoice Create(
        Guid businessId,
        Guid customerId,
        Guid userId,
        string invoiceNumber,
        decimal amount,
        DateTime dueDate,
        decimal? tax = null,
        decimal? discount = null,
        string? notes = null)
    {
        var total = amount + (tax ?? 0) - (discount ?? 0);
        return new Invoice
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            CustomerId = customerId,
            UserId = userId,
            InvoiceNumber = invoiceNumber,
            Amount = amount,
            Tax = tax,
            Discount = discount,
            Total = total,
            Status = InvoiceStatus.DRAFT,
            DueDate = dueDate,
            Notes = notes,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public void Update(decimal amount, decimal? tax, decimal? discount, DateTime dueDate, string? notes)
    {
        Amount = amount;
        Tax = tax;
        Discount = discount;
        Total = amount + (tax ?? 0) - (discount ?? 0);
        DueDate = dueDate;
        Notes = notes;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Send()
    {
        Status = InvoiceStatus.SENT;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkPaid()
    {
        Status = InvoiceStatus.PAID;
        PaidAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkOverdue()
    {
        Status = InvoiceStatus.OVERDUE;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Cancel()
    {
        Status = InvoiceStatus.CANCELLED;
        UpdatedAt = DateTime.UtcNow;
    }
}
