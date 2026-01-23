namespace ArdaNova.Domain.Models.Entities;

using System.Text.Json.Serialization;
using ArdaNova.Domain.Models.Enums;

public class Referral
{
    public Guid Id { get; private set; }
    public Guid ReferrerId { get; private set; }
    public Guid ReferredId { get; private set; }
    public string? ReferralCode { get; private set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ReferralStatus Status { get; private set; }

    public bool RewardClaimed { get; private set; }
    public int? XpRewarded { get; private set; }
    public decimal? TokenRewarded { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }

    // Navigation
    public User Referrer { get; private set; } = null!;
    public User Referred { get; private set; } = null!;

    private Referral() { }

    public static Referral Create(
        Guid referrerId,
        Guid referredId,
        string? referralCode = null)
    {
        return new Referral
        {
            Id = Guid.NewGuid(),
            ReferrerId = referrerId,
            ReferredId = referredId,
            ReferralCode = referralCode,
            Status = ReferralStatus.PENDING,
            RewardClaimed = false,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Complete()
    {
        Status = ReferralStatus.COMPLETED;
        CompletedAt = DateTime.UtcNow;
    }

    public void Expire()
    {
        Status = ReferralStatus.EXPIRED;
    }

    public void Cancel()
    {
        Status = ReferralStatus.CANCELLED;
    }

    public void ClaimReward(int xpAmount, decimal? tokenAmount = null)
    {
        if (Status != ReferralStatus.COMPLETED)
            throw new InvalidOperationException("Cannot claim reward for incomplete referral");

        RewardClaimed = true;
        XpRewarded = xpAmount;
        TokenRewarded = tokenAmount;
    }
}
