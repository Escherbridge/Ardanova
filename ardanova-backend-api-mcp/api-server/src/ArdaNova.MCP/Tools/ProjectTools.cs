namespace ArdaNova.MCP.Tools;

using System.ComponentModel;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Interfaces;
using ArdaNova.Domain.Models.Enums;
using ModelContextProtocol.Server;

[McpServerToolType]
public class ProjectTools
{
    private readonly IProjectService _projectService;

    public ProjectTools(IProjectService projectService)
    {
        _projectService = projectService;
    }

    [McpServerTool(Name = "project_get_by_id")]
    [Description("Retrieves a project by its unique identifier")]
    public async Task<ProjectDto?> GetProjectById(
        [Description("The unique identifier of the project")] Guid id,
        CancellationToken ct = default)
    {
        var result = await _projectService.GetByIdAsync(id, ct);
        return result.IsSuccess ? result.Value : null;
    }

    [McpServerTool(Name = "project_get_by_slug")]
    [Description("Retrieves a project by its URL slug")]
    public async Task<ProjectDto?> GetProjectBySlug(
        [Description("The URL-friendly slug of the project")] string slug,
        CancellationToken ct = default)
    {
        var result = await _projectService.GetBySlugAsync(slug, ct);
        return result.IsSuccess ? result.Value : null;
    }

    [McpServerTool(Name = "project_get_all")]
    [Description("Retrieves all projects")]
    public async Task<IReadOnlyList<ProjectDto>?> GetAllProjects(CancellationToken ct = default)
    {
        var result = await _projectService.GetAllAsync(ct);
        return result.IsSuccess ? result.Value : null;
    }

    [McpServerTool(Name = "project_get_paged")]
    [Description("Retrieves projects with pagination")]
    public async Task<object?> GetPagedProjects(
        [Description("Page number (1-based)")] int page = 1,
        [Description("Number of items per page")] int pageSize = 10,
        CancellationToken ct = default)
    {
        var result = await _projectService.GetPagedAsync(page, pageSize, ct);
        return result.IsSuccess ? result.Value : null;
    }

    [McpServerTool(Name = "project_get_by_user")]
    [Description("Retrieves all projects for a specific user")]
    public async Task<IReadOnlyList<ProjectDto>?> GetProjectsByUser(
        [Description("The unique identifier of the user")] Guid userId,
        CancellationToken ct = default)
    {
        var result = await _projectService.GetByUserIdAsync(userId, ct);
        return result.IsSuccess ? result.Value : null;
    }

    [McpServerTool(Name = "project_get_by_category")]
    [Description("Retrieves projects by their category")]
    public async Task<IReadOnlyList<ProjectDto>?> GetProjectsByCategory(
        [Description("The project category")] string category,
        CancellationToken ct = default)
    {
        if (!Enum.TryParse<ProjectCategory>(category, true, out var projectCategory))
            return null;
        var result = await _projectService.GetByCategory(projectCategory, ct);
        return result.IsSuccess ? result.Value : null;
    }

    [McpServerTool(Name = "project_get_featured")]
    [Description("Retrieves all featured projects")]
    public async Task<IReadOnlyList<ProjectDto>?> GetFeaturedProjects(CancellationToken ct = default)
    {
        var result = await _projectService.GetFeaturedAsync(ct);
        return result.IsSuccess ? result.Value : null;
    }

}
