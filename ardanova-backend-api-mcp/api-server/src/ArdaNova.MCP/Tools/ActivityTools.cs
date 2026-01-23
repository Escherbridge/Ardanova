namespace ArdaNova.MCP.Tools;

using System.ComponentModel;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Interfaces;
using ArdaNova.Domain.Models.Enums;
using ModelContextProtocol.Server;

[McpServerToolType]
public class ActivityTools
{
    private readonly IActivityService _activityService;

    public ActivityTools(IActivityService activityService)
    {
        _activityService = activityService;
    }

    [McpServerTool(Name = "activity_get_by_id")]
    [Description("Retrieves an activity by its unique identifier")]
    public async Task<ActivityDto?> GetActivityById(
        [Description("The activity ID")] Guid id,
        CancellationToken ct = default)
    {
        var result = await _activityService.GetByIdAsync(id, ct);
        return result.IsSuccess ? result.Value : null;
    }

    [McpServerTool(Name = "activity_get_by_user_id")]
    [Description("Retrieves all activities for a user")]
    public async Task<IReadOnlyList<ActivityDto>?> GetActivitiesByUserId(
        [Description("The user ID")] Guid userId,
        CancellationToken ct = default)
    {
        var result = await _activityService.GetByUserIdAsync(userId, ct);
        return result.IsSuccess ? result.Value : null;
    }

    [McpServerTool(Name = "activity_get_by_project_id")]
    [Description("Retrieves all activities for a project")]
    public async Task<IReadOnlyList<ActivityDto>?> GetActivitiesByProjectId(
        [Description("The project ID")] Guid projectId,
        CancellationToken ct = default)
    {
        var result = await _activityService.GetByProjectIdAsync(projectId, ct);
        return result.IsSuccess ? result.Value : null;
    }

    [McpServerTool(Name = "activity_create")]
    [Description("Creates a new activity record")]
    public async Task<ActivityDto?> CreateActivity(
        [Description("The user who performed the action")] Guid userId,
        [Description("The type of activity")] ActivityType type,
        [Description("The entity type (e.g., 'Project', 'Task')")] string entityType,
        [Description("The entity ID")] string entityId,
        [Description("Description of the action")] string action,
        [Description("Optional project ID")] Guid? projectId = null,
        CancellationToken ct = default)
    {
        var dto = new CreateActivityDto
        {
            UserId = userId,
            Type = type,
            EntityType = entityType,
            EntityId = entityId,
            Action = action,
            ProjectId = projectId
        };
        var result = await _activityService.CreateAsync(dto, ct);
        return result.IsSuccess ? result.Value : null;
    }
}
