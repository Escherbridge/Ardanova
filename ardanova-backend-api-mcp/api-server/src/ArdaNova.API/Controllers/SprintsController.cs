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
public class SprintsController : ControllerBase
{
    private readonly ISprintService _sprintService;
    private readonly IEpicService _epicService;
    private readonly IHierarchyAuthorizationService _authorization;

    public SprintsController(
        ISprintService sprintService,
        IEpicService epicService,
        IHierarchyAuthorizationService authorization)
    {
        _sprintService = sprintService;
        _epicService = epicService;
        _authorization = authorization;
    }

    // ===== EPIC-BASED ROUTES =====
    [HttpGet("api/epics/{epicId}/sprints")]
    public async Task<IActionResult> GetByEpicId(string epicId, CancellationToken ct)
    {
        var result = await _sprintService.GetByEpicIdAsync(epicId, ct);
        return ToActionResult(result);
    }

    [HttpGet("api/epics/{epicId}/sprints/active")]
    public async Task<IActionResult> GetActiveSprint(string epicId, CancellationToken ct)
    {
        var result = await _sprintService.GetActiveByEpicIdAsync(epicId, ct);
        return ToActionResult(result);
    }

    // ===== SPRINT-BASED ROUTES =====
    [HttpGet("api/sprints/{id}")]
    public async Task<IActionResult> GetById(string id, CancellationToken ct)
    {
        var result = await _sprintService.GetByIdAsync(id, ct);
        return ToActionResult(result);
    }

    [HttpPost("api/sprints")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> Create([FromBody] CreateSprintDto dto, CancellationToken ct)
    {
        var epic = await _epicService.GetByIdAsync(dto.EpicId, ct);
        if (!epic.IsSuccess)
            return ToActionResult(epic);
        if (!await HasValidEpicAncestryAsync(epic.Value!, ct))
            return BadRequest(new { error = "Epic ancestry could not be verified" });
        if (!await CanWorkOnItemAsync(epic.Value!.ProjectId, epic.Value.AssigneeId, ct))
            return Forbid();
        if ((dto.AssigneeId is not null || dto.EquityBudget.HasValue) &&
            !await CanManageProjectAsync(epic.Value.ProjectId, ct))
            return Forbid();
        if (dto.AssigneeId is not null)
        {
            if (!await _authorization.IsProjectMemberAsync(dto.AssigneeId, epic.Value.ProjectId, ct))
                return BadRequest(new { error = "Assignee must be a member of the project" });
        }

        var result = await _sprintService.CreateAsync(
            dto with { ProjectId = epic.Value.ProjectId },
            ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value)
            : ToActionResult(result);
    }

    [HttpPut("api/sprints/{id}")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateSprintDto dto, CancellationToken ct)
    {
        var sprint = await _sprintService.GetByIdAsync(id, ct);
        if (!sprint.IsSuccess)
            return ToActionResult(sprint);
        if (!await HasValidSprintAncestryAsync(sprint.Value!, ct))
            return Forbid();
        var structuralChange = dto.EquityBudget.HasValue;
        var mayMutate = structuralChange
            ? await CanManageProjectAsync(sprint.Value!.ProjectId, ct)
            : await CanWorkOnItemAsync(sprint.Value!.ProjectId, sprint.Value.AssigneeId, ct);
        if (!mayMutate)
            return Forbid();
        var result = await _sprintService.UpdateAsync(id, dto, ct);
        return ToActionResult(result);
    }

    [HttpDelete("api/sprints/{id}")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> Delete(string id, CancellationToken ct)
    {
        var sprint = await _sprintService.GetByIdAsync(id, ct);
        if (!sprint.IsSuccess)
            return ToActionResult(sprint);
        if (!await HasValidSprintAncestryAsync(sprint.Value!, ct))
            return Forbid();
        if (!await CanManageProjectAsync(sprint.Value!.ProjectId, ct))
            return Forbid();

        var result = await _sprintService.DeleteAsync(id, ct);
        return result.IsSuccess ? NoContent() : ToActionResult(result);
    }

    [HttpPost("api/sprints/{id}/start")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> Start(string id, CancellationToken ct)
    {
        if (!await MayChangeStatusAsync(id, ct))
            return Forbid();
        var result = await _sprintService.StartAsync(id, ct);
        return ToActionResult(result);
    }

    [HttpPost("api/sprints/{id}/complete")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> Complete(string id, CancellationToken ct)
    {
        if (!await MayChangeStatusAsync(id, ct))
            return Forbid();
        var result = await _sprintService.CompleteAsync(id, ct);
        return ToActionResult(result);
    }

    [HttpPost("api/sprints/{id}/cancel")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> Cancel(string id, CancellationToken ct)
    {
        if (!await MayChangeStatusAsync(id, ct))
            return Forbid();
        var result = await _sprintService.UpdateStatusAsync(id, SprintStatus.CANCELLED, ct);
        return ToActionResult(result);
    }

    [HttpPut("api/sprints/{id}/status")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> UpdateStatus(string id, [FromBody] UpdateSprintStatusDto dto, CancellationToken ct)
    {
        if (!await MayChangeStatusAsync(id, ct))
            return Forbid();
        var result = await _sprintService.UpdateStatusAsync(id, dto.Status, ct);
        return ToActionResult(result);
    }

    private string ActorId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    private bool IsAdmin => User.IsInRole(UserRole.ADMIN.ToString());

    private Task<bool> CanManageProjectAsync(string projectId, CancellationToken ct)
        => IsAdmin
            ? Task.FromResult(true)
            : _authorization.CanManageProjectAsync(ActorId, projectId, ct);

    private Task<bool> CanWorkOnItemAsync(
        string projectId,
        string? assigneeId,
        CancellationToken ct)
        => IsAdmin
            ? Task.FromResult(true)
            : _authorization.CanWorkOnItemAsync(ActorId, projectId, assigneeId, ct);

    private async Task<bool> MayChangeStatusAsync(string id, CancellationToken ct)
    {
        var sprint = await _sprintService.GetByIdAsync(id, ct);
        return sprint.IsSuccess &&
               await HasValidSprintAncestryAsync(sprint.Value!, ct) &&
               await CanWorkOnItemAsync(
                   sprint.Value!.ProjectId,
                   sprint.Value.AssigneeId,
                   ct);
    }

    private async Task<bool> HasValidEpicAncestryAsync(
        EpicDto epic,
        CancellationToken ct)
        => string.Equals(
            await _authorization.ResolveCommentTargetProjectAsync(
                CommentTargetType.EPIC,
                epic.Id,
                ct),
            epic.ProjectId,
            StringComparison.Ordinal);

    private async Task<bool> HasValidSprintAncestryAsync(
        SprintDto sprint,
        CancellationToken ct)
        => string.Equals(
            await _authorization.ResolveCommentTargetProjectAsync(
                CommentTargetType.SPRINT,
                sprint.Id,
                ct),
            sprint.ProjectId,
            StringComparison.Ordinal);

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

public record UpdateSprintStatusDto
{
    public required SprintStatus Status { get; init; }
}
