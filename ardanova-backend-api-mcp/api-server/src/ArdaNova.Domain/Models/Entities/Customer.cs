namespace ArdaNova.Domain.Models.Entities;

public class Customer
{
    public Guid Id { get; private set; }
    public Guid BusinessId { get; private set; }
    public string Name { get; private set; } = null!;
    public string? Email { get; private set; }
    public string? Phone { get; private set; }
    public string? Address { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public Guid UserId { get; private set; }

    // Navigation properties
    public Business Business { get; private set; } = null!;
    public User User { get; private set; } = null!;

    private Customer() { }

    public static Customer Create(
        Guid businessId,
        Guid userId,
        string name,
        string? email = null,
        string? phone = null,
        string? address = null)
    {
        return new Customer
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            UserId = userId,
            Name = name,
            Email = email,
            Phone = phone,
            Address = address,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public void Update(string name, string? email, string? phone, string? address)
    {
        Name = name;
        Email = email;
        Phone = phone;
        Address = address;
        UpdatedAt = DateTime.UtcNow;
    }
}
