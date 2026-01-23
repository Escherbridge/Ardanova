namespace ArdaNova.Domain.Models.Entities;

public class ProjectComment
{
    public Guid Id { get; private set; }
    public Guid ProjectId { get; private set; }
    public Guid UserId { get; private set; }
    public string Content { get; private set; } = null!;
    public Guid? ParentId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    // Navigation properties
    public Project Project { get; private set; } = null!;
    public User User { get; private set; } = null!;
    public ProjectComment? Parent { get; private set; }
    public ICollection<ProjectComment> Replies { get; private set; } = new List<ProjectComment>();

    private ProjectComment() { }

    public static ProjectComment Create(
        Guid projectId,
        Guid userId,
        string content,
        Guid? parentId = null)
    {
        return new ProjectComment
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            UserId = userId,
            Content = content,
            ParentId = parentId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public void UpdateContent(string content)
    {
        Content = content;
        UpdatedAt = DateTime.UtcNow;
    }
}
