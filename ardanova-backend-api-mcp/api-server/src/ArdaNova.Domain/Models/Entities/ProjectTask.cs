namespace ArdaNova.Domain.Models.Entities;

using System.Text.Json.Serialization;
using ArdaNova.Domain.Models.Enums;
using TaskStatus = ArdaNova.Domain.Models.Enums.TaskStatus;

public class ProjectTask
{
    public Guid Id { get; private set; }
    public Guid ProjectId { get; private set; }
    public string Title { get; private set; } = null!;
    public string? Description { get; private set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public TaskStatus Status { get; private set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public TaskPriority Priority { get; private set; }

    public int? EstimatedHours { get; private set; }
    public int? ActualHours { get; private set; }
    public DateTime? DueDate { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public Guid? AssignedToId { get; private set; }

    // Navigation properties
    public Project Project { get; private set; } = null!;
    public User? AssignedTo { get; private set; }
    public ICollection<ProjectTaskDependency> Dependencies { get; private set; } = new List<ProjectTaskDependency>();
    public ICollection<ProjectTaskDependency> Dependents { get; private set; } = new List<ProjectTaskDependency>();
    public TaskEscrow? Escrow { get; private set; }

    private ProjectTask() { }

    public static ProjectTask Create(
        Guid projectId,
        string title,
        string? description = null,
        TaskPriority priority = TaskPriority.MEDIUM,
        int? estimatedHours = null,
        DateTime? dueDate = null,
        Guid? assignedToId = null)
    {
        return new ProjectTask
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            Title = title,
            Description = description,
            Status = TaskStatus.TODO,
            Priority = priority,
            EstimatedHours = estimatedHours,
            DueDate = dueDate,
            AssignedToId = assignedToId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public void Update(string title, string? description, TaskPriority priority, int? estimatedHours, DateTime? dueDate)
    {
        Title = title;
        Description = description;
        Priority = priority;
        EstimatedHours = estimatedHours;
        DueDate = dueDate;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AssignTo(Guid? userId)
    {
        AssignedToId = userId;
        UpdatedAt = DateTime.UtcNow;
    }

    public void StartProgress()
    {
        Status = TaskStatus.IN_PROGRESS;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SubmitForReview()
    {
        Status = TaskStatus.REVIEW;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Complete(int? actualHours = null)
    {
        Status = TaskStatus.COMPLETED;
        ActualHours = actualHours;
        CompletedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Block()
    {
        Status = TaskStatus.BLOCKED;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Reopen()
    {
        Status = TaskStatus.TODO;
        CompletedAt = null;
        UpdatedAt = DateTime.UtcNow;
    }
}
