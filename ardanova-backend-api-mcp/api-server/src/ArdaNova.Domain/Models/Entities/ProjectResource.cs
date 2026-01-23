namespace ArdaNova.Domain.Models.Entities;

public class ProjectResource
{
    public Guid Id { get; private set; }
    public Guid ProjectId { get; private set; }
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public int Quantity { get; private set; }
    public decimal? EstimatedCost { get; private set; }
    public bool IsRequired { get; private set; }
    public bool IsObtained { get; private set; }
    public DateTime CreatedAt { get; private set; }

    // Navigation property
    public Project Project { get; private set; } = null!;

    private ProjectResource() { }

    public static ProjectResource Create(
        Guid projectId,
        string name,
        string? description = null,
        int quantity = 1,
        decimal? estimatedCost = null,
        bool isRequired = true)
    {
        return new ProjectResource
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            Name = name,
            Description = description,
            Quantity = quantity,
            EstimatedCost = estimatedCost,
            IsRequired = isRequired,
            IsObtained = false,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Update(string name, string? description, int quantity, decimal? estimatedCost, bool isRequired)
    {
        Name = name;
        Description = description;
        Quantity = quantity;
        EstimatedCost = estimatedCost;
        IsRequired = isRequired;
    }

    public void MarkObtained()
    {
        IsObtained = true;
    }

    public void MarkNotObtained()
    {
        IsObtained = false;
    }
}
