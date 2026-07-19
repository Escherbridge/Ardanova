namespace ArdaNova.API.Controllers;

using System.Security.Claims;
using ArdaNova.API.Middleware;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Interfaces;
using ArdaNova.Domain.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api")]
public class ProductBacklogItemsController : ControllerBase
{
    private readonly IProductBacklogItemService _pbiService;
    private readonly IProjectService _projectService;
    private readonly IProjectMilestoneService _milestoneService;
    private readonly IEpicService _epicService;
    private readonly ISprintService _sprintService;
    private readonly IFeatureService _featureService;
    private readonly IHierarchyAuthorizationService _authorization;

    public ProductBacklogItemsController(
        IProductBacklogItemService pbiService,
        IProjectService projectService,
        IProjectMilestoneService milestoneService,
        IEpicService epicService,
        ISprintService sprintService,
        IFeatureService featureService,
        IHierarchyAuthorizationService authorization)
    {
        _pbiService = pbiService;
        _projectService = projectService;
        _milestoneService = milestoneService;
        _epicService = epicService;
        _sprintService = sprintService;
        _featureService = featureService;
        _authorization = authorization;
    }

    [HttpGet("features/{featureId}/product-backlog-items")]
    public async Task<IActionResult> GetByFeatureId(string featureId, CancellationToken ct)
    {
        var result = await _pbiService.GetByFeatureIdAsync(featureId, ct);
        return ToActionResult(result);
    }

    [HttpGet("product-backlog-items/{id}")]
    public async Task<IActionResult> GetById(string id, CancellationToken ct)
    {
        var result = await _pbiService.GetByIdAsync(id, ct);
        return ToActionResult(result);
    }

    [HttpGet("projects/{projectId}/product-backlog-items")]
    public async Task<IActionResult> GetByProjectId(string projectId, CancellationToken ct)
    {
        var result = await _pbiService.GetByProjectIdAsync(projectId, ct);
        return ToActionResult(result);
    }

    [HttpPost("product-backlog-items")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> Create([FromBody] CreateProductBacklogItemDto dto, CancellationToken ct)
    {
        var parents = await ValidateParentAssociationsAsync(
            dto.ProjectId,
            dto.FeatureId,
            dto.SprintId,
            dto.EpicId,
            dto.MilestoneId,
            ct);
        if (!parents.IsValid)
            return BadRequest(new { error = "Hierarchy parents must belong to one project and one ancestry chain" });

        var canManage = await CanManageProjectAsync(dto.ProjectId, ct);
        var hasParent = dto.FeatureId is not null || dto.SprintId is not null ||
                        dto.EpicId is not null || dto.MilestoneId is not null;
        if (!canManage && (!hasParent || !parents.ActorAssigned))
            return Forbid();
        if ((dto.AssigneeId is not null || dto.GuildId is not null) && !canManage)
            return Forbid();
        if (dto.AssigneeId is not null &&
            !await _authorization.IsProjectMemberAsync(dto.AssigneeId, dto.ProjectId, ct))
            return BadRequest(new { error = "Assignee must be a member of the project" });
        if (dto.GuildId is not null && !await IsAssignedGuildAsync(dto.ProjectId, dto.GuildId, ct))
            return BadRequest(new { error = "Guild is not assigned to the project" });

        var result = await _pbiService.CreateAsync(dto, ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value)
            : ToActionResult(result);
    }

    [HttpPut("product-backlog-items/{id}")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateProductBacklogItemDto dto, CancellationToken ct)
    {
        var item = await GetVerifiedItemAsync(id, ct);
        if (item is null)
            return Forbid();
        var structuralChange = dto.GuildId is not null;
        var mayMutate = structuralChange
            ? await CanManageProjectAsync(item.ProjectId, ct)
            : await CanWorkOnItemAsync(item, ct);
        if (!mayMutate)
            return Forbid();
        if (dto.GuildId is not null && !await IsAssignedGuildAsync(item.ProjectId, dto.GuildId, ct))
            return BadRequest(new { error = "Guild is not assigned to the project" });

        var result = await _pbiService.UpdateAsync(id, dto, ct);
        return ToActionResult(result);
    }

    [HttpDelete("product-backlog-items/{id}")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> Delete(string id, CancellationToken ct)
    {
        var item = await GetVerifiedItemAsync(id, ct);
        if (item is null || !await CanManageProjectAsync(item.ProjectId, ct))
            return Forbid();

        var result = await _pbiService.DeleteAsync(id, ct);
        return result.IsSuccess ? NoContent() : ToActionResult(result);
    }

    [HttpPut("product-backlog-items/{id}/assign")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> Assign(string id, [FromBody] AssignDto dto, CancellationToken ct)
    {
        var item = await GetVerifiedItemAsync(id, ct);
        if (item is null || !await CanManageProjectAsync(item.ProjectId, ct))
            return Forbid();
        if (dto.UserId is not null &&
            !await _authorization.IsProjectMemberAsync(dto.UserId, item.ProjectId, ct))
            return BadRequest(new { error = "Assignee must be a member of the project" });

        var result = await _pbiService.AssignAsync(id, dto.UserId, ct);
        return ToActionResult(result);
    }

    [HttpPut("product-backlog-items/{id}/status")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> UpdateStatus(string id, [FromBody] UpdatePBIStatusDto dto, CancellationToken ct)
    {
        var item = await GetVerifiedItemAsync(id, ct);
        if (item is null || !await CanWorkOnItemAsync(item, ct))
            return Forbid();

        var result = await _pbiService.UpdateStatusAsync(id, dto.Status, ct);
        return ToActionResult(result);
    }

    [HttpPut("product-backlog-items/{id}/priority")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> UpdatePriority(string id, [FromBody] UpdatePriorityDto dto, CancellationToken ct)
    {
        var item = await GetVerifiedItemAsync(id, ct);
        if (item is null || !await CanWorkOnItemAsync(item, ct))
            return Forbid();

        var updateDto = new UpdateProductBacklogItemDto { Priority = dto.Priority };
        var result = await _pbiService.UpdateAsync(id, updateDto, ct);
        return ToActionResult(result);
    }

    [HttpPut("features/{featureId}/product-backlog-items/reorder")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> ReorderPbis(string featureId, [FromBody] ReorderDto dto, CancellationToken ct)
    {
        var feature = await _featureService.GetByIdAsync(featureId, ct);
        if (!feature.IsSuccess || !await CanManageProjectAsync(feature.Value!.ProjectId, ct))
            return Forbid();

        foreach (var itemId in dto.ItemIds.Distinct(StringComparer.Ordinal))
        {
            var item = await GetVerifiedItemAsync(itemId, ct);
            if (item is null ||
                !string.Equals(item.ProjectId, feature.Value.ProjectId, StringComparison.Ordinal) ||
                !string.Equals(item.FeatureId, featureId, StringComparison.Ordinal))
                return BadRequest(new { error = "Every backlog item must belong to the routed feature and project" });
        }

        var result = await _pbiService.ReorderAsync(featureId, dto.ItemIds, ct);
        return result.IsSuccess ? NoContent() : ToActionResult(result);
    }

    private string ActorId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    private bool IsAdmin => User.IsInRole(UserRole.ADMIN.ToString());

    private Task<bool> CanManageProjectAsync(string projectId, CancellationToken ct)
        => IsAdmin
            ? Task.FromResult(true)
            : _authorization.CanManageProjectAsync(ActorId, projectId, ct);

    private Task<bool> CanWorkOnItemAsync(ProductBacklogItemDto item, CancellationToken ct)
        => IsAdmin
            ? Task.FromResult(true)
            : _authorization.CanWorkOnItemAsync(
                ActorId,
                item.ProjectId,
                item.AssigneeId,
                ct);

    private async Task<bool> IsAssignedGuildAsync(
        string projectId,
        string guildId,
        CancellationToken ct)
    {
        var project = await _projectService.GetByIdAsync(projectId, ct);
        return project.IsSuccess &&
               string.Equals(project.Value!.AssignedGuildId, guildId, StringComparison.Ordinal);
    }

    private async Task<ProductBacklogItemDto?> GetVerifiedItemAsync(
        string id,
        CancellationToken ct)
    {
        var item = await _pbiService.GetByIdAsync(id, ct);
        if (!item.IsSuccess)
            return null;

        var parents = await ValidateParentAssociationsAsync(
            item.Value!.ProjectId,
            item.Value.FeatureId,
            item.Value.SprintId,
            item.Value.EpicId,
            item.Value.MilestoneId,
            ct);
        return parents.IsValid ? item.Value : null;
    }

    private async Task<(bool IsValid, bool ActorAssigned)> ValidateParentAssociationsAsync(
        string projectId,
        string? featureId,
        string? sprintId,
        string? epicId,
        string? milestoneId,
        CancellationToken ct)
    {
        var actorAssigned = false;
        var project = await _projectService.GetByIdAsync(projectId, ct);
        if (!project.IsSuccess ||
            !string.Equals(project.Value!.Id, projectId, StringComparison.Ordinal))
            return (false, false);

        if (featureId is not null)
        {
            var feature = await _featureService.GetByIdAsync(featureId, ct);
            if (!feature.IsSuccess ||
                !string.Equals(feature.Value!.ProjectId, projectId, StringComparison.Ordinal))
                return (false, false);
            actorAssigned |= string.Equals(feature.Value.AssigneeId, ActorId, StringComparison.Ordinal);
            if (sprintId is not null &&
                !string.Equals(feature.Value.SprintId, sprintId, StringComparison.Ordinal))
                return (false, false);
            sprintId ??= feature.Value.SprintId;
        }

        if (sprintId is not null)
        {
            var sprint = await _sprintService.GetByIdAsync(sprintId, ct);
            if (!sprint.IsSuccess ||
                !string.Equals(sprint.Value!.ProjectId, projectId, StringComparison.Ordinal))
                return (false, false);
            actorAssigned |= string.Equals(sprint.Value.AssigneeId, ActorId, StringComparison.Ordinal);
            if (epicId is not null &&
                !string.Equals(sprint.Value.EpicId, epicId, StringComparison.Ordinal))
                return (false, false);
            epicId ??= sprint.Value.EpicId;
        }

        if (epicId is not null)
        {
            var epic = await _epicService.GetByIdAsync(epicId, ct);
            if (!epic.IsSuccess ||
                !string.Equals(epic.Value!.ProjectId, projectId, StringComparison.Ordinal))
                return (false, false);
            actorAssigned |= string.Equals(epic.Value.AssigneeId, ActorId, StringComparison.Ordinal);
            if (milestoneId is not null &&
                !string.Equals(epic.Value.MilestoneId, milestoneId, StringComparison.Ordinal))
                return (false, false);
            milestoneId ??= epic.Value.MilestoneId;
        }

        if (milestoneId is not null)
        {
            var milestone = await _milestoneService.GetByIdAsync(milestoneId, ct);
            if (!milestone.IsSuccess ||
                !string.Equals(milestone.Value!.ProjectId, projectId, StringComparison.Ordinal))
                return (false, false);
        }

        return (true, actorAssigned);
    }

    private IActionResult ToActionResult<T>(Result<T> result)
    {
        if (result.IsSuccess)
            return Ok(result.Value);

        return result.Type switch
        {
            ResultType.NotFound => NotFound(new { error = result.Error }),
            ResultType.ValidationError => BadRequest(new { error = result.Error }),
            ResultType.Unauthorized => Unauthorized(new { error = result.Error }),
            ResultType.Forbidden => Forbid(),
            _ => BadRequest(new { error = result.Error })
        };
    }
}

public record AssignDto
{
    public string? UserId { get; init; }
}

public record UpdatePBIStatusDto
{
    public PBIStatus Status { get; init; }
}

public record UpdatePriorityDto
{
    public Priority Priority { get; init; }
}

public record ReorderDto
{
    public required IReadOnlyList<string> ItemIds { get; init; }
}
