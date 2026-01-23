namespace ArdaNova.Domain.Models.Entities;

public class BusinessAnalytics
{
    public Guid Id { get; private set; }
    public Guid BusinessId { get; private set; }
    public DateTime Date { get; private set; }
    public decimal Revenue { get; private set; }
    public decimal Expenses { get; private set; }
    public decimal Profit { get; private set; }
    public int SalesCount { get; private set; }
    public int NewCustomers { get; private set; }
    public DateTime CreatedAt { get; private set; }

    // Navigation property
    public Business Business { get; private set; } = null!;

    private BusinessAnalytics() { }

    public static BusinessAnalytics Create(
        Guid businessId,
        DateTime date,
        decimal revenue,
        decimal expenses,
        int salesCount,
        int newCustomers)
    {
        return new BusinessAnalytics
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            Date = date.Date,
            Revenue = revenue,
            Expenses = expenses,
            Profit = revenue - expenses,
            SalesCount = salesCount,
            NewCustomers = newCustomers,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Update(decimal revenue, decimal expenses, int salesCount, int newCustomers)
    {
        Revenue = revenue;
        Expenses = expenses;
        Profit = revenue - expenses;
        SalesCount = salesCount;
        NewCustomers = newCustomers;
    }
}
