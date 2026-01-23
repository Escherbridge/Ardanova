namespace ArdaNova.Domain.Models.Entities;

public class ProjectUpdate
{
    public Guid Id { get; private set; }
    public Guid ProjectId { get; private set; }
    public Guid UserId { get; private set; }
    public string Title { get; private set; } = null!;
    public string Content { get; private set; } = null!;
    public string? Images { get; private set; }
    public DateTime CreatedAt { get; private set; }

    // Navigation properties
    public Project Project { get; private set; } = null!;
    public User User { get; private set; } = null!;

    private ProjectUpdate() { }

    public static ProjectUpdate Create(
        Guid projectId,
        Guid userId,
        string title,
        string content,
        string? images = null)
    {
        return new ProjectUpdate
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            UserId = userId,
            Title = title,
            Content = content,
            Images = images,
            CreatedAt = DateTime.UtcNow
        };
    }
}
