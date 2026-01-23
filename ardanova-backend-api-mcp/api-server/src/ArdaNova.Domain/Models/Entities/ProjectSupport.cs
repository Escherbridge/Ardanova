namespace ArdaNova.Domain.Models.Entities;

using System.Text.Json.Serialization;
using ArdaNova.Domain.Models.Enums;

public class ProjectSupport
{
    public Guid Id { get; private set; }
    public Guid ProjectId { get; private set; }
    public Guid UserId { get; private set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public SupportType SupportType { get; private set; }

    public decimal? MonthlyAmount { get; private set; }
    public string? Message { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    // Navigation properties
    public Project Project { get; private set; } = null!;
    public User User { get; private set; } = null!;

    private ProjectSupport() { }

    public static ProjectSupport Create(
        Guid projectId,
        Guid userId,
        SupportType supportType,
        decimal? monthlyAmount = null,
        string? message = null)
    {
        return new ProjectSupport
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            UserId = userId,
            SupportType = supportType,
            MonthlyAmount = monthlyAmount,
            Message = message,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public void UpdateAmount(decimal? monthlyAmount)
    {
        MonthlyAmount = monthlyAmount;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateMessage(string? message)
    {
        Message = message;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Reactivate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }
}
