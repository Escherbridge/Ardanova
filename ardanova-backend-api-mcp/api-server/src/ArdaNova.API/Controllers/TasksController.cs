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
[Route("api/[controller]")]
public class TasksController : ControllerBase
{
    private readonly ITaskService _taskService;
    private readonly IProjectService _projectService;
    private readonly IProjectMilestoneService _milestoneService;
    private readonly IEpicService _epicService;
    private readonly ISprintService _sprintService;
    private readonly IFeatureService _featureService;
    private readonly IProductBacklogItemService _pbiService;
    private readonly IHierarchyAuthorizationService _authorization;

    public TasksController(
        ITaskService taskService,
        IProjectService projectService,
        IProjectMilestoneService milestoneService,
        IEpicService epicService,
        ISprintService sprintService,
        IFeatureService featureService,
        IProductBacklogItemService pbiService,
        IHierarchyAuthorizationService authorization)
    {
        _taskService = taskService;
        _projectService = projectService;
        _milestoneService = milestoneService;
        _epicService = epicService;
        _sprintService = sprintService;
        _featureService = featureService;
        _pbiService = pbiService;
        _authorization = authorization;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var result = await _taskService.GetAllAsync(ct);
        return ToActionResult(result);
    }

    [HttpGet("paged")]
    public async Task<IActionResult> GetPaged([FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken ct = default)
    {
        var result = await _taskService.GetPagedAsync(page, pageSize, ct);
        return ToActionResult(result);
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search(
        [FromQuery] string? searchTerm,
        [FromQuery] TaskStatus? status,
        [FromQuery] TaskPriority? priority,
        [FromQuery] TaskType? taskType,
        [FromQuery] string? projectId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken ct = default)
    {
        var result = await _taskService.SearchAsync(searchTerm, status, priority, taskType, projectId, page, pageSize, ct);
        return ToActionResult(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id, CancellationToken ct)
    {
        var result = await _taskService.GetByIdAsync(id, ct);
        return ToActionResult(result);
    }

    [HttpGet("me")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> GetMine(CancellationToken ct)
    {
        var result = await _taskService.GetByUserIdAsync(ActorId, ct);
        return ToActionResult(result);
    }

    [HttpGet("project/{projectId}")]
    public async Task<IActionResult> GetByProjectId(string projectId, CancellationToken ct)
    {
        var result = await _taskService.GetByProjectIdAsync(projectId, ct);
        return ToActionResult(result);
    }

    [HttpGet("pbi/{pbiId}")]
    public async Task<IActionResult> GetByPbiId(string pbiId, CancellationToken ct)
    {
        var result = await _taskService.GetByPbiIdAsync(pbiId, ct);
        return ToActionResult(result);
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> Create([FromBody] CreateTaskDto dto, CancellationToken ct)
    {
        var parents = await ValidateParentAssociationsAsync(
            dto.ProjectId,
            dto.PbiId,
            dto.FeatureId,
            dto.SprintId,
            dto.EpicId,
            dto.MilestoneId,
            ct);
        if (!parents.IsValid)
            return BadRequest(new { error = "Hierarchy parents must belong to one project and one ancestry chain" });

        var canManage = await CanManageProjectAsync(dto.ProjectId, ct);
        var hasParent = dto.PbiId is not null || dto.FeatureId is not null ||
                        dto.SprintId is not null || dto.EpicId is not null ||
                        dto.MilestoneId is not null;
        if (!canManage && (!hasParent || !parents.ActorAssigned))
            return Forbid();
        if ((dto.AssignedToId is not null || dto.GuildId is not null || dto.EquityReward.HasValue) &&
            !canManage)
            return Forbid();
        if (dto.AssignedToId is not null &&
            !await _authorization.IsProjectMemberAsync(dto.AssignedToId, dto.ProjectId, ct))
            return BadRequest(new { error = "Assignee must be a member of the project" });
        if (dto.GuildId is not null && !await IsAssignedGuildAsync(dto.ProjectId, dto.GuildId, ct))
            return BadRequest(new { error = "Guild is not assigned to the project" });

        var result = await _taskService.CreateAsync(dto, ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value)
            : ToActionResult(result);
    }

    [HttpPut("{id}")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateTaskDto dto, CancellationToken ct)
    {
        var task = await GetVerifiedTaskAsync(id, ct);
        if (task is null)
            return Forbid();
        var structuralChange = dto.EquityReward.HasValue;
        var mayMutate = structuralChange
            ? await CanManageProjectAsync(task.ProjectId, ct)
            : await CanWorkOnTaskAsync(task, ct);
        if (!mayMutate)
            return Forbid();
        var result = await _taskService.UpdateAsync(id, dto, ct);
        return ToActionResult(result);
    }

    [HttpPatch("{id}/status")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> UpdateStatus(string id, [FromBody] UpdateTaskStatusDto dto, CancellationToken ct)
    {
        var task = await GetVerifiedTaskAsync(id, ct);
        if (task is null || !await CanWorkOnTaskAsync(task, ct))
            return Forbid();

        var result = await _taskService.UpdateStatusAsync(id, dto.Status, ct);
        return ToActionResult(result);
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> Delete(string id, CancellationToken ct)
    {
        var task = await GetVerifiedTaskAsync(id, ct);
        if (task is null || !await CanManageProjectAsync(task.ProjectId, ct))
            return Forbid();

        var result = await _taskService.DeleteAsync(id, ct);
        return result.IsSuccess ? NoContent() : ToActionResult(result);
    }

    private string ActorId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    private bool IsAdmin => User.IsInRole(UserRole.ADMIN.ToString());

    private Task<bool> CanManageProjectAsync(string projectId, CancellationToken ct)
        => IsAdmin
            ? Task.FromResult(true)
            : _authorization.CanManageProjectAsync(ActorId, projectId, ct);

    private Task<bool> CanWorkOnTaskAsync(TaskDto task, CancellationToken ct)
        => IsAdmin
            ? Task.FromResult(true)
            : _authorization.CanWorkOnItemAsync(
                ActorId,
                task.ProjectId,
                task.AssignedToId,
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

    private async Task<TaskDto?> GetVerifiedTaskAsync(string id, CancellationToken ct)
    {
        var task = await _taskService.GetByIdAsync(id, ct);
        if (!task.IsSuccess)
            return null;

        var parents = await ValidateParentAssociationsAsync(
            task.Value!.ProjectId,
            task.Value.PbiId,
            task.Value.FeatureId,
            task.Value.SprintId,
            task.Value.EpicId,
            task.Value.MilestoneId,
            ct);
        return parents.IsValid ? task.Value : null;
    }

    private async Task<(bool IsValid, bool ActorAssigned)> ValidateParentAssociationsAsync(
        string projectId,
        string? pbiId,
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

        if (pbiId is not null)
        {
            var pbi = await _pbiService.GetByIdAsync(pbiId, ct);
            if (!pbi.IsSuccess ||
                !string.Equals(pbi.Value!.ProjectId, projectId, StringComparison.Ordinal))
                return (false, false);
            actorAssigned |= string.Equals(pbi.Value.AssigneeId, ActorId, StringComparison.Ordinal);
            if (featureId is not null &&
                !string.Equals(pbi.Value.FeatureId, featureId, StringComparison.Ordinal))
                return (false, false);
            if (sprintId is not null &&
                pbi.Value.SprintId is null &&
                pbi.Value.FeatureId is null)
                return (false, false);
            if (sprintId is not null && pbi.Value.SprintId is not null &&
                !string.Equals(pbi.Value.SprintId, sprintId, StringComparison.Ordinal))
                return (false, false);
            if (epicId is not null &&
                pbi.Value.EpicId is null &&
                pbi.Value.SprintId is null &&
                pbi.Value.FeatureId is null)
                return (false, false);
            if (epicId is not null && pbi.Value.EpicId is not null &&
                !string.Equals(pbi.Value.EpicId, epicId, StringComparison.Ordinal))
                return (false, false);
            if (milestoneId is not null &&
                pbi.Value.MilestoneId is null &&
                pbi.Value.EpicId is null &&
                pbi.Value.SprintId is null &&
                pbi.Value.FeatureId is null)
                return (false, false);
            if (milestoneId is not null && pbi.Value.MilestoneId is not null &&
                !string.Equals(pbi.Value.MilestoneId, milestoneId, StringComparison.Ordinal))
                return (false, false);
            featureId ??= pbi.Value.FeatureId;
            sprintId ??= pbi.Value.SprintId;
            epicId ??= pbi.Value.EpicId;
            milestoneId ??= pbi.Value.MilestoneId;
        }

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
