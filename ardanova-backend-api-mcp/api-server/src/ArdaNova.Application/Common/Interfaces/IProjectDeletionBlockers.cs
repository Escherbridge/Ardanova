namespace ArdaNova.Application.Common.Interfaces;

/// <summary>Which dependent tables still hold rows whose foreign key stops a project being deleted. See Services/Implementations/AGENTS.md §Project delete.</summary>
public interface IProjectDeletionBlockers
{
    /// <summary>The entity names (for example <c>Epic</c>, <c>ProjectFollow</c>) with at least one row referencing the project, sorted.</summary>
    Task<IReadOnlyList<string>> FindAsync(string projectId, CancellationToken ct = default);
}
