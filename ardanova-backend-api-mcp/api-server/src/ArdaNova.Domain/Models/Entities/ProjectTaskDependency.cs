namespace ArdaNova.Domain.Models.Entities;

public class ProjectTaskDependency
{
    public Guid Id { get; private set; }
    public Guid TaskId { get; private set; }
    public Guid DependsOnId { get; private set; }

    // Navigation properties
    public ProjectTask Task { get; private set; } = null!;
    public ProjectTask DependsOn { get; private set; } = null!;

    private ProjectTaskDependency() { }

    public static ProjectTaskDependency Create(Guid taskId, Guid dependsOnId)
    {
        return new ProjectTaskDependency
        {
            Id = Guid.NewGuid(),
            TaskId = taskId,
            DependsOnId = dependsOnId
        };
    }
}
