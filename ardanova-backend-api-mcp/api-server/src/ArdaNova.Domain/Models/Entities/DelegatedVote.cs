namespace ArdaNova.Domain.Models.Entities;

public class DelegatedVote
{
    public Guid Id { get; private set; }
    public Guid ProjectId { get; private set; }
    public Guid DelegatorId { get; private set; }
    public Guid DelegateeId { get; private set; }
    public Guid TokenId { get; private set; }
    public decimal Amount { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? ExpiresAt { get; private set; }
    public DateTime? RevokedAt { get; private set; }

    // Navigation
    public Project Project { get; private set; } = null!;
    public User Delegator { get; private set; } = null!;
    public User Delegatee { get; private set; } = null!;

    private DelegatedVote() { }

    public static DelegatedVote Create(
        Guid projectId,
        Guid delegatorId,
        Guid delegateeId,
        Guid tokenId,
        decimal amount,
        DateTime? expiresAt = null)
    {
        return new DelegatedVote
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            DelegatorId = delegatorId,
            DelegateeId = delegateeId,
            TokenId = tokenId,
            Amount = amount,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = expiresAt
        };
    }

    public void Revoke()
    {
        IsActive = false;
        RevokedAt = DateTime.UtcNow;
    }

    public void UpdateAmount(decimal newAmount)
    {
        Amount = newAmount;
    }

    public void Extend(DateTime newExpiresAt)
    {
        ExpiresAt = newExpiresAt;
    }

    public bool IsExpired() => ExpiresAt.HasValue && ExpiresAt.Value < DateTime.UtcNow;
}
