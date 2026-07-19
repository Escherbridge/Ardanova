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
[Route("api/features")]
public class FeaturesController : ControllerBase
{
    private readonly IFeatureService _featureService;
    private readonly ISprintService _sprintService;
    private readonly IHierarchyAuthorizationService _authorization;

    public FeaturesController(
        IFeatureService featureService,
        ISprintService sprintService,
        IHierarchyAuthorizationService authorization)
    {
        _featureService = featureService;
        _sprintService = sprintService;
        _authorization = authorization;
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id, CancellationToken ct)
    {
        var result = await _featureService.GetByIdAsync(id, ct);
        return ToActionResult(result);
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> Create([FromBody] CreateFeatureDto dto, CancellationToken ct)
    {
        var sprint = await _sprintService.GetByIdAsync(dto.SprintId, ct);
        if (!sprint.IsSuccess)
            return ToActionResult(sprint);
        if (!await HasValidSprintAncestryAsync(sprint.Value!, ct))
            return BadRequest(new { error = "Sprint ancestry could not be verified" });
        if (!await CanWorkOnItemAsync(sprint.Value!.ProjectId, sprint.Value.AssigneeId, ct))
            return Forbid();
        if (dto.Order.HasValue && !await CanManageProjectAsync(sprint.Value.ProjectId, ct))
            return Forbid();

        var result = await _featureService.CreateAsync(
            dto with { ProjectId = sprint.Value.ProjectId },
            ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value)
            : ToActionResult(result);
    }

    [HttpPut("{id}")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateFeatureDto dto, CancellationToken ct)
    {
        var feature = await _featureService.GetByIdAsync(id, ct);
        if (!feature.IsSuccess)
            return ToActionResult(feature);
        if (!await HasValidFeatureAncestryAsync(feature.Value!, ct))
            return Forbid();
        var structuralChange = dto.Order.HasValue;
        var mayMutate = structuralChange
            ? await CanManageProjectAsync(feature.Value!.ProjectId, ct)
            : await CanWorkOnItemAsync(feature.Value!.ProjectId, feature.Value.AssigneeId, ct);
        if (!mayMutate)
            return Forbid();
        var result = await _featureService.UpdateAsync(id, dto, ct);
        return ToActionResult(result);
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> Delete(string id, CancellationToken ct)
    {
        var feature = await _featureService.GetByIdAsync(id, ct);
        if (!feature.IsSuccess)
            return ToActionResult(feature);
        if (!await HasValidFeatureAncestryAsync(feature.Value!, ct))
            return Forbid();
        if (!await CanManageProjectAsync(feature.Value!.ProjectId, ct))
            return Forbid();

        var result = await _featureService.DeleteAsync(id, ct);
        return result.IsSuccess ? NoContent() : ToActionResult(result);
    }

    [HttpPut("{id}/assign")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> Assign(string id, [FromBody] AssignFeatureDto dto, CancellationToken ct)
    {
        var feature = await _featureService.GetByIdAsync(id, ct);
        if (!feature.IsSuccess)
            return ToActionResult(feature);
        if (!await HasValidFeatureAncestryAsync(feature.Value!, ct))
            return Forbid();
        if (!await CanManageProjectAsync(feature.Value!.ProjectId, ct))
            return Forbid();
        if (dto.UserId is not null &&
            !await _authorization.IsProjectMemberAsync(dto.UserId, feature.Value.ProjectId, ct))
            return BadRequest(new { error = "Assignee must be a member of the project" });

        var result = await _featureService.AssignAsync(id, dto.UserId, ct);
        return ToActionResult(result);
    }

    [HttpPut("{id}/status")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> UpdateStatus(string id, [FromBody] UpdateFeatureStatusDto dto, CancellationToken ct)
    {
        var feature = await _featureService.GetByIdAsync(id, ct);
        if (!feature.IsSuccess)
            return ToActionResult(feature);
        if (!await HasValidFeatureAncestryAsync(feature.Value!, ct))
            return Forbid();
        if (!await CanWorkOnItemAsync(feature.Value!.ProjectId, feature.Value.AssigneeId, ct))
            return Forbid();

        var result = await _featureService.UpdateStatusAsync(id, dto.Status, ct);
        return ToActionResult(result);
    }

    [HttpPut("{id}/priority")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> UpdatePriority(string id, [FromBody] UpdateFeaturePriorityDto dto, CancellationToken ct)
    {
        var feature = await _featureService.GetByIdAsync(id, ct);
        if (!feature.IsSuccess)
            return ToActionResult(feature);
        if (!await HasValidFeatureAncestryAsync(feature.Value!, ct))
            return Forbid();
        if (!await CanWorkOnItemAsync(feature.Value!.ProjectId, feature.Value.AssigneeId, ct))
            return Forbid();

        var result = await _featureService.UpdatePriorityAsync(id, dto.Priority, ct);
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

    private async Task<bool> HasValidFeatureAncestryAsync(
        FeatureDto feature,
        CancellationToken ct)
        => string.Equals(
            await _authorization.ResolveCommentTargetProjectAsync(
                CommentTargetType.FEATURE,
                feature.Id,
                ct),
            feature.ProjectId,
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
public record AssignFeatureDto
{
    public string? UserId { get; init; }
}

public record UpdateFeatureStatusDto
{
    public FeatureStatus Status { get; init; }
}

public record UpdateFeaturePriorityDto
{
    public Priority Priority { get; init; }
}

// Nested controller for sprint features
[ApiController]
[Route("api/sprints/{sprintId}/features")]
public class SprintFeaturesController : ControllerBase
{
    private readonly IFeatureService _featureService;

    public SprintFeaturesController(IFeatureService featureService)
    {
        _featureService = featureService;
    }

    [HttpGet]
    public async Task<IActionResult> GetBySprintId(string sprintId, CancellationToken ct)
    {
        var result = await _featureService.GetBySprintIdAsync(sprintId, ct);
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
