namespace ArdaNova.Domain.Models.Entities;

using System.Text.Json.Serialization;
using ArdaNova.Domain.Models.Enums;

public class MarketingCampaign
{
    public Guid Id { get; private set; }
    public Guid BusinessId { get; private set; }
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public string Platform { get; private set; } = null!;
    public string Content { get; private set; } = null!;
    public string? MediaUrls { get; private set; }
    public DateTime? ScheduledAt { get; private set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public CampaignStatus Status { get; private set; }

    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public Guid UserId { get; private set; }

    // Navigation properties
    public Business Business { get; private set; } = null!;
    public User User { get; private set; } = null!;

    private MarketingCampaign() { }

    public static MarketingCampaign Create(
        Guid businessId,
        Guid userId,
        string name,
        string platform,
        string content,
        string? description = null,
        string? mediaUrls = null,
        DateTime? scheduledAt = null)
    {
        return new MarketingCampaign
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            UserId = userId,
            Name = name,
            Description = description,
            Platform = platform,
            Content = content,
            MediaUrls = mediaUrls,
            ScheduledAt = scheduledAt,
            Status = CampaignStatus.DRAFT,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public void Update(string name, string? description, string platform, string content, string? mediaUrls)
    {
        Name = name;
        Description = description;
        Platform = platform;
        Content = content;
        MediaUrls = mediaUrls;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Schedule(DateTime scheduledAt)
    {
        ScheduledAt = scheduledAt;
        Status = CampaignStatus.SCHEDULED;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Activate()
    {
        Status = CampaignStatus.ACTIVE;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Complete()
    {
        Status = CampaignStatus.COMPLETED;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Cancel()
    {
        Status = CampaignStatus.CANCELLED;
        UpdatedAt = DateTime.UtcNow;
    }
}
