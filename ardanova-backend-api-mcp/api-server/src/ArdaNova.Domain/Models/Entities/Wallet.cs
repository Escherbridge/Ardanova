namespace ArdaNova.Domain.Models.Entities;

using System.Text.Json.Serialization;
using ArdaNova.Domain.Models.Enums;

public class Wallet
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string Address { get; private set; } = null!;

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public WalletProvider Provider { get; private set; }

    public string? Label { get; private set; }
    public bool IsVerified { get; private set; }
    public bool IsPrimary { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    // Navigation
    public User User { get; private set; } = null!;

    private Wallet() { }

    public static Wallet Create(
        Guid userId,
        string address,
        WalletProvider provider,
        string? label = null,
        bool isPrimary = false)
    {
        return new Wallet
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Address = address,
            Provider = provider,
            Label = label,
            IsVerified = false,
            IsPrimary = isPrimary,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public void Update(string? label, bool? isPrimary)
    {
        if (label is not null) Label = label;
        if (isPrimary.HasValue) IsPrimary = isPrimary.Value;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Verify()
    {
        IsVerified = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetPrimary(bool isPrimary)
    {
        IsPrimary = isPrimary;
        UpdatedAt = DateTime.UtcNow;
    }
}
