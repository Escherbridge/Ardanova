namespace ArdaNova.MCP.Tools;

using System.ComponentModel;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Implementations;
using ModelContextProtocol.Server;

/// <summary>Project reads for MCP callers, answered through the viewer-scoped surface.</summary>
/// <remarks>
/// No MCP transport carries a signed actor, so every read here answers as <see cref="ProjectViewer.Anonymous"/>.
/// The unscoped <c>IProjectService.GetByIdAsync</c>/<c>GetBySlugAsync</c> lookups exist for authorization to read
/// through and deliberately do not hide drafts; this seam must never call them.
/// </remarks>
[McpServerToolType]
public class ProjectTools
{
    private static readonly ProjectViewer Viewer = ProjectViewer.Anonymous;

    private readonly IProjectVisibility _projects;

    public ProjectTools(IProjectVisibility projects)
    {
        _projects = projects;
    }

    [McpServerTool(Name = "project_get_by_id")]
    [Description("Retrieves a published project by its unique identifier")]
    public async Task<ProjectDto?> GetProjectById(
        [Description("The unique identifier of the project")] string id,
        CancellationToken ct = default)
    {
        var result = await _projects.GetVisibleByIdAsync(Viewer, id, ct);
        return result.IsSuccess ? result.Value : null;
    }

    [McpServerTool(Name = "project_get_by_slug")]
    [Description("Retrieves a published project by its URL slug")]
    public async Task<ProjectDto?> GetProjectBySlug(
        [Description("The URL-friendly slug of the project")] string slug,
        CancellationToken ct = default)
    {
        var result = await _projects.GetVisibleBySlugAsync(Viewer, slug, ct);
        return result.IsSuccess ? result.Value : null;
    }

    [McpServerTool(Name = "project_get_all")]
    [Description("Retrieves all published projects")]
    public async Task<IReadOnlyList<ProjectDto>?> GetAllProjects(CancellationToken ct = default)
    {
        var result = await _projects.GetAllVisibleAsync(Viewer, ct);
        return result.IsSuccess ? result.Value : null;
    }

    [McpServerTool(Name = "project_get_paged")]
    [Description("Retrieves published projects with pagination")]
    public async Task<object?> GetPagedProjects(
        [Description("Page number (1-based)")] int page = 1,
        [Description("Number of items per page")] int pageSize = 10,
        CancellationToken ct = default)
    {
        var result = await _projects.GetPagedVisibleAsync(Viewer, page, pageSize, ct);
        return result.IsSuccess ? result.Value : null;
    }

    [McpServerTool(Name = "project_get_by_user")]
    [Description("Retrieves the published projects of a specific user")]
    public async Task<IReadOnlyList<ProjectDto>?> GetProjectsByUser(
        [Description("The unique identifier of the user")] string userId,
        CancellationToken ct = default)
    {
        var result = await _projects.GetVisibleByUserIdAsync(Viewer, userId, ct);
        return result.IsSuccess ? result.Value : null;
    }

    [McpServerTool(Name = "project_get_by_category")]
    [Description("Retrieves published projects that match a given category")]
    public async Task<IReadOnlyList<ProjectDto>?> GetProjectsByCategory(
        [Description("The project category to search for")] string category,
        CancellationToken ct = default)
    {
        var result = await _projects.GetVisibleByCategoryAsync(Viewer, category, ct);
        return result.IsSuccess ? result.Value : null;
    }

    [McpServerTool(Name = "project_get_featured")]
    [Description("Retrieves all featured published projects")]
    public async Task<IReadOnlyList<ProjectDto>?> GetFeaturedProjects(CancellationToken ct = default)
    {
        var result = await _projects.GetFeaturedVisibleAsync(Viewer, ct);
        return result.IsSuccess ? result.Value : null;
    }

}
