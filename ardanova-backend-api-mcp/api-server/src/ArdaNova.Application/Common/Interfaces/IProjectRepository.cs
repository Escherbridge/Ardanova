namespace ArdaNova.Application.Common.Interfaces;

using ArdaNova.Application.Common.Results;
using ArdaNova.Domain.Models.Entities;
using ArdaNova.Domain.Models.Enums;

public interface IProjectRepository : IRepository<Project>
{
    /// <summary>
    /// Search projects with multiple filters
    /// </summary>
    Task<PagedResult<Project>> SearchAsync(
        string? searchTerm,
        ProjectStatus? status,
        ProjectCategory? category,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get project with user data included
    /// </summary>
    Task<Project?> GetWithUserAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get project by slug with user data included
    /// </summary>
    Task<Project?> GetBySlugWithUserAsync(string slug, CancellationToken cancellationToken = default);
}
