namespace ArdaNova.Domain.Models.Entities;

using System.Text.Json.Serialization;
using ArdaNova.Domain.Models.Enums;

public class Business
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public string? Industry { get; private set; }
    public string? Address { get; private set; }
    public string? Phone { get; private set; }
    public string? Email { get; private set; }
    public string? Website { get; private set; }
    public string? Logo { get; private set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public SubscriptionPlan Plan { get; private set; }

    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public Guid OwnerId { get; private set; }

    // Navigation properties
    public User Owner { get; private set; } = null!;
    public ICollection<Customer> Customers { get; private set; } = new List<Customer>();
    public ICollection<Product> Products { get; private set; } = new List<Product>();
    public ICollection<BusinessAnalytics> Analytics { get; private set; } = new List<BusinessAnalytics>();

    private Business() { }

    public static Business Create(
        Guid ownerId,
        string name,
        string? description = null,
        string? industry = null)
    {
        return new Business
        {
            Id = Guid.NewGuid(),
            OwnerId = ownerId,
            Name = name,
            Description = description,
            Industry = industry,
            Plan = SubscriptionPlan.FREE,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public void Update(string name, string? description, string? industry, string? address, string? phone, string? email, string? website)
    {
        Name = name;
        Description = description;
        Industry = industry;
        Address = address;
        Phone = phone;
        Email = email;
        Website = website;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetLogo(string? logo)
    {
        Logo = logo;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpgradePlan(SubscriptionPlan plan)
    {
        Plan = plan;
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
