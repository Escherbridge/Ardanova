namespace ArdaNova.Domain.Models.Entities;

using System.Text.Json.Serialization;
using ArdaNova.Domain.Models.Enums;

public class ProjectApplication
{
    public Guid Id { get; private set; }
    public Guid ProjectId { get; private set; }
    public Guid UserId { get; private set; }
    public string RoleTitle { get; private set; } = null!;
    public string Message { get; private set; } = null!;
    public string? Skills { get; private set; }
    public string? Experience { get; private set; }
    public string? Availability { get; private set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ApplicationStatus Status { get; private set; }

    public DateTime AppliedAt { get; private set; }
    public DateTime? ReviewedAt { get; private set; }
    public string? ReviewMessage { get; private set; }

    // Navigation properties
    public Project Project { get; private set; } = null!;
    public User User { get; private set; } = null!;

    private ProjectApplication() { }

    public static ProjectApplication Create(
        Guid projectId,
        Guid userId,
        string roleTitle,
        string message,
        string? skills = null,
        string? experience = null,
        string? availability = null)
    {
        return new ProjectApplication
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            UserId = userId,
            RoleTitle = roleTitle,
            Message = message,
            Skills = skills,
            Experience = experience,
            Availability = availability,
            Status = ApplicationStatus.PENDING,
            AppliedAt = DateTime.UtcNow
        };
    }

    public void Accept(string? reviewMessage = null)
    {
        Status = ApplicationStatus.ACCEPTED;
        ReviewMessage = reviewMessage;
        ReviewedAt = DateTime.UtcNow;
    }

    public void Reject(string? reviewMessage = null)
    {
        Status = ApplicationStatus.REJECTED;
        ReviewMessage = reviewMessage;
        ReviewedAt = DateTime.UtcNow;
    }

    public void Withdraw()
    {
        Status = ApplicationStatus.WITHDRAWN;
    }
}
