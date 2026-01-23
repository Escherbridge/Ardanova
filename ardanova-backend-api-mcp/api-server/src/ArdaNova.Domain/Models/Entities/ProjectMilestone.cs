namespace ArdaNova.Domain.Models.Entities;

public class ProjectMilestone
{
    public Guid Id { get; private set; }
    public Guid ProjectId { get; private set; }
    public string Title { get; private set; } = null!;
    public string? Description { get; private set; }
    public DateTime TargetDate { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public bool IsCompleted { get; private set; }
    public DateTime CreatedAt { get; private set; }

    // Navigation property
    public Project Project { get; private set; } = null!;

    private ProjectMilestone() { }

    public static ProjectMilestone Create(
        Guid projectId,
        string title,
        DateTime targetDate,
        string? description = null)
    {
        return new ProjectMilestone
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            Title = title,
            Description = description,
            TargetDate = targetDate,
            IsCompleted = false,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Update(string title, string? description, DateTime targetDate)
    {
        Title = title;
        Description = description;
        TargetDate = targetDate;
    }

    public void Complete()
    {
        IsCompleted = true;
        CompletedAt = DateTime.UtcNow;
    }

    public void Reopen()
    {
        IsCompleted = false;
        CompletedAt = null;
    }
}
