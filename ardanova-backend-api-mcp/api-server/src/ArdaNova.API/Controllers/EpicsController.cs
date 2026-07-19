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
[Route("api/epics")]
public class EpicsController : ControllerBase
{
    private readonly IEpicService _epicService;
    private readonly IProjectMilestoneService _milestoneService;
    private readonly IHierarchyAuthorizationService _authorization;

    public EpicsController(
        IEpicService epicService,
        IProjectMilestoneService milestoneService,
        IHierarchyAuthorizationService authorization)
    {
        _epicService = epicService;
        _milestoneService = milestoneService;
        _authorization = authorization;
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id, CancellationToken ct)
    {
        var result = await _epicService.GetByIdAsync(id, ct);
        return ToActionResult(result);
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> Create([FromBody] CreateEpicDto dto, CancellationToken ct)
    {
        var milestone = await _milestoneService.GetByIdAsync(dto.MilestoneId, ct);
        if (!milestone.IsSuccess)
            return ToActionResult(milestone);
        if (!await HasValidMilestoneAncestryAsync(milestone.Value!, ct))
            return BadRequest(new { error = "Milestone ancestry could not be verified" });
        if (!await CanManageProjectAsync(milestone.Value!.ProjectId, ct))
            return Forbid();
        if (dto.AssigneeId is not null &&
            !await _authorization.IsProjectMemberAsync(dto.AssigneeId, milestone.Value.ProjectId, ct))
            return BadRequest(new { error = "Assignee must be a member of the project" });

        var result = await _epicService.CreateAsync(
            dto with { ProjectId = milestone.Value.ProjectId },
            ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value)
            : ToActionResult(result);
    }

    [HttpPut("{id}")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateEpicDto dto, CancellationToken ct)
    {
        var epic = await _epicService.GetByIdAsync(id, ct);
        if (!epic.IsSuccess)
            return ToActionResult(epic);
        if (!await HasValidEpicAncestryAsync(epic.Value!, ct))
            return Forbid();
        var structuralChange = dto.EquityBudget.HasValue;
        var mayMutate = structuralChange
            ? await CanManageProjectAsync(epic.Value!.ProjectId, ct)
            : await CanWorkOnItemAsync(epic.Value!, ct);
        if (!mayMutate)
            return Forbid();
        var result = await _epicService.UpdateAsync(id, dto, ct);
        return ToActionResult(result);
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> Delete(string id, CancellationToken ct)
    {
        var epic = await _epicService.GetByIdAsync(id, ct);
        if (!epic.IsSuccess)
            return ToActionResult(epic);
        if (!await HasValidEpicAncestryAsync(epic.Value!, ct))
            return Forbid();
        if (!await CanManageProjectAsync(epic.Value!.ProjectId, ct))
            return Forbid();

        var result = await _epicService.DeleteAsync(id, ct);
        return result.IsSuccess ? NoContent() : ToActionResult(result);
    }

    [HttpPut("{id}/assign")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> Assign(string id, [FromBody] AssignEpicDto dto, CancellationToken ct)
    {
        var epic = await _epicService.GetByIdAsync(id, ct);
        if (!epic.IsSuccess)
            return ToActionResult(epic);
        if (!await HasValidEpicAncestryAsync(epic.Value!, ct))
            return Forbid();
        if (!await CanManageProjectAsync(epic.Value!.ProjectId, ct))
            return Forbid();
        if (dto.UserId is not null &&
            !await _authorization.IsProjectMemberAsync(dto.UserId, epic.Value.ProjectId, ct))
            return BadRequest(new { error = "Assignee must be a member of the project" });

        var result = await _epicService.AssignAsync(id, dto.UserId, ct);
        return ToActionResult(result);
    }

    [HttpPut("{id}/status")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> UpdateStatus(string id, [FromBody] UpdateEpicStatusDto dto, CancellationToken ct)
    {
        var epic = await _epicService.GetByIdAsync(id, ct);
        if (!epic.IsSuccess)
            return ToActionResult(epic);
        if (!await HasValidEpicAncestryAsync(epic.Value!, ct))
            return Forbid();
        if (!await CanWorkOnItemAsync(epic.Value!, ct))
            return Forbid();

        var result = await _epicService.UpdateStatusAsync(id, dto.Status, ct);
        return ToActionResult(result);
    }

    [HttpPut("{id}/priority")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> UpdatePriority(string id, [FromBody] UpdateEpicPriorityDto dto, CancellationToken ct)
    {
        var epic = await _epicService.GetByIdAsync(id, ct);
        if (!epic.IsSuccess)
            return ToActionResult(epic);
        if (!await HasValidEpicAncestryAsync(epic.Value!, ct))
            return Forbid();
        if (!await CanWorkOnItemAsync(epic.Value!, ct))
            return Forbid();

        var result = await _epicService.UpdatePriorityAsync(id, dto.Priority, ct);
        return ToActionResult(result);
    }

    private string ActorId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    private bool IsAdmin => User.IsInRole(UserRole.ADMIN.ToString());

    private Task<bool> CanManageProjectAsync(string projectId, CancellationToken ct)
        => IsAdmin
            ? Task.FromResult(true)
            : _authorization.CanManageProjectAsync(ActorId, projectId, ct);

    private Task<bool> CanWorkOnItemAsync(EpicDto epic, CancellationToken ct)
        => IsAdmin
            ? Task.FromResult(true)
            : _authorization.CanWorkOnItemAsync(
                ActorId,
                epic.ProjectId,
                epic.AssigneeId,
                ct);

    private async Task<bool> HasValidMilestoneAncestryAsync(
        ProjectMilestoneDto milestone,
        CancellationToken ct)
        => string.Equals(
            await _authorization.ResolveCommentTargetProjectAsync(
                CommentTargetType.MILESTONE,
                milestone.Id,
                ct),
            milestone.ProjectId,
            StringComparison.Ordinal);

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

// Helper DTOs for endpoints
public record AssignEpicDto
{
    public string? UserId { get; init; }
}

public record UpdateEpicStatusDto
{
    public EpicStatus Status { get; init; }
}

// Nested controller for project epics
[ApiController]
[Route("api/projects/{projectId}/epics")]
public class ProjectEpicsController : ControllerBase
{
    private readonly IEpicService _epicService;

    public ProjectEpicsController(IEpicService epicService)
    {
        _epicService = epicService;
    }

    [HttpGet]
    public async Task<IActionResult> GetByProjectId(string projectId, CancellationToken ct)
    {
        var result = await _epicService.GetByProjectIdAsync(projectId, ct);
        return ToActionResult(result);
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

// Nested controller for milestone epics
[ApiController]
[Route("api/milestones/{milestoneId}/epics")]
public class MilestoneEpicsController : ControllerBase
{
    private readonly IEpicService _epicService;
    private readonly IProjectMilestoneService _milestoneService;
    private readonly IHierarchyAuthorizationService _authorization;

    public MilestoneEpicsController(
        IEpicService epicService,
        IProjectMilestoneService milestoneService,
        IHierarchyAuthorizationService authorization)
    {
        _epicService = epicService;
        _milestoneService = milestoneService;
        _authorization = authorization;
    }

    [HttpGet]
    public async Task<IActionResult> GetByMilestoneId(string milestoneId, CancellationToken ct)
    {
        var result = await _epicService.GetByMilestoneIdAsync(milestoneId, ct);
        return ToActionResult(result);
    }

    [HttpPut("reorder")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> ReorderEpics(string milestoneId, [FromBody] ReorderEpicsDto dto, CancellationToken ct)
    {
        var milestone = await _milestoneService.GetByIdAsync(milestoneId, ct);
        if (!milestone.IsSuccess)
            return ToActionResult(milestone);
        if (!string.Equals(
                await _authorization.ResolveCommentTargetProjectAsync(
                    CommentTargetType.MILESTONE,
                    milestoneId,
                    ct),
                milestone.Value!.ProjectId,
                StringComparison.Ordinal))
            return BadRequest(new { error = "Milestone ancestry could not be verified" });
        if (!IsAdmin && !await _authorization.CanManageProjectAsync(ActorId, milestone.Value!.ProjectId, ct))
            return Forbid();

        foreach (var epicId in dto.EpicIds.Distinct(StringComparer.Ordinal))
        {
            var epic = await _epicService.GetByIdAsync(epicId, ct);
            if (!epic.IsSuccess ||
                !string.Equals(epic.Value!.ProjectId, milestone.Value!.ProjectId, StringComparison.Ordinal) ||
                !string.Equals(epic.Value.MilestoneId, milestoneId, StringComparison.Ordinal) ||
                !string.Equals(
                    await _authorization.ResolveCommentTargetProjectAsync(
                        CommentTargetType.EPIC,
                        epicId,
                        ct),
                    milestone.Value.ProjectId,
                    StringComparison.Ordinal))
                return BadRequest(new { error = "Every epic must belong to the routed milestone and project" });
        }

        var result = await _epicService.ReorderAsync(milestoneId, dto.EpicIds, ct);
        return result.IsSuccess ? NoContent() : ToActionResult(result);
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

    private string ActorId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    private bool IsAdmin => User.IsInRole(UserRole.ADMIN.ToString());
}
