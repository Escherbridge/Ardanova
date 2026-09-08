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
public class OpportunitiesController : ControllerBase
{
    private readonly IOpportunityService _opportunityService;
    private readonly IHierarchyAuthorizationService _hierarchyAuthorization;
    private readonly IGuildService _guildService;
    private readonly IGuildMemberService _guildMemberService;
    private readonly IAzoaValueEligibilityService _valueEligibility;

    public OpportunitiesController(
        IOpportunityService opportunityService,
        IHierarchyAuthorizationService hierarchyAuthorization,
        IGuildService guildService,
        IGuildMemberService guildMemberService,
        IAzoaValueEligibilityService valueEligibility)
    {
        _opportunityService = opportunityService;
        _hierarchyAuthorization = hierarchyAuthorization;
        _guildService = guildService;
        _guildMemberService = guildMemberService;
        _valueEligibility = valueEligibility;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var result = await _opportunityService.GetAllAsync(ct);
        return ToActionResult(result);
    }

    [HttpGet("paged")]
    public async Task<IActionResult> GetPaged([FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken ct = default)
    {
        var result = await _opportunityService.GetPagedAsync(page, pageSize, ct);
        return ToActionResult(result);
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search(
        [FromQuery] string? searchTerm,
        [FromQuery] OpportunityType? type,
        [FromQuery] OpportunityStatus? status,
        [FromQuery] ExperienceLevel? experienceLevel,
        [FromQuery] string? skills,
        [FromQuery] string? sourceType,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken ct = default)
    {
        var result = await _opportunityService.SearchAsync(searchTerm, type, status, experienceLevel, skills, sourceType, page, pageSize, ct);
        return ToActionResult(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id, CancellationToken ct)
    {
        var result = await _opportunityService.GetByIdAsync(id, ct);
        return ToActionResult(result);
    }

    [HttpGet("slug/{slug}")]
    public async Task<IActionResult> GetBySlug(string slug, CancellationToken ct)
    {
        var result = await _opportunityService.GetBySlugAsync(slug, ct);
        return ToActionResult(result);
    }

    [HttpGet("poster/{posterId}")]
    public async Task<IActionResult> GetByPosterId(string posterId, CancellationToken ct)
    {
        var result = await _opportunityService.GetByPosterIdAsync(posterId, ct);
        return ToActionResult(result);
    }

    [HttpGet("guild/{guildId}")]
    public async Task<IActionResult> GetByGuildId(string guildId, CancellationToken ct)
    {
        var result = await _opportunityService.GetByGuildIdAsync(guildId, ct);
        return ToActionResult(result);
    }

    [HttpGet("project/{projectId}")]
    public async Task<IActionResult> GetByProjectId(string projectId, CancellationToken ct)
    {
        var result = await _opportunityService.GetByProjectIdAsync(projectId, ct);
        return ToActionResult(result);
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> Create([FromBody] CreateOpportunityDto dto, CancellationToken ct)
    {
        var hasProject = !string.IsNullOrWhiteSpace(dto.ProjectId);
        var hasGuild = !string.IsNullOrWhiteSpace(dto.GuildId);
        if (hasProject == hasGuild)
        {
            return BadRequest(new
            {
                error = "An opportunity must be associated with exactly one project or guild."
            });
        }

        var canCreate = hasProject
            ? await CanManageProjectAsync(dto.ProjectId!, ct)
            : await CanManageGuildAsync(dto.GuildId!, ct);
        if (!canCreate)
            return Forbid();

        var readinessFailure = await RequireValueListingAccountSetupAsync(
            dto.Compensation,
            dto.EquityPercent,
            "publish a paid or project-unit opportunity",
            ct);
        if (readinessFailure is not null)
            return readinessFailure;

        var result = await _opportunityService.CreateAsync(dto with { PosterId = ActorId }, ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value)
            : ToActionResult(result);
    }

    [HttpPut("{id}")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateOpportunityDto dto, CancellationToken ct)
    {
        var opportunity = await GetManagedOpportunityAsync(id, ct);
        if (!opportunity.IsSuccess)
            return ToActionResult(opportunity);

        var currentOpportunity = opportunity.Value!;
        var readinessFailure = await RequireValueListingAccountSetupAsync(
            dto.Compensation ?? currentOpportunity.Compensation,
            currentOpportunity.EquityPercent,
            "update a paid or project-unit opportunity",
            ct);
        if (readinessFailure is not null)
            return readinessFailure;

        var result = await _opportunityService.UpdateAsync(id, dto, ct);
        return ToActionResult(result);
    }

    [HttpPatch("{id}/close")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> Close(string id, CancellationToken ct)
    {
        var opportunity = await GetManagedOpportunityAsync(id, ct);
        if (!opportunity.IsSuccess)
            return ToActionResult(opportunity);

        var result = await _opportunityService.CloseAsync(id, ct);
        return ToActionResult(result);
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> Delete(string id, CancellationToken ct)
    {
        var opportunity = await GetManagedOpportunityAsync(id, ct);
        if (!opportunity.IsSuccess)
            return ToActionResult(opportunity);

        var result = await _opportunityService.DeleteAsync(id, ct);
        return result.IsSuccess ? NoContent() : ToActionResult(result);
    }

    [HttpPost("{id}/apply")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> Apply(string id, [FromBody] ApplyToOpportunityDto dto, CancellationToken ct)
    {
        var opportunity = await _opportunityService.GetByIdAsync(id, ct);
        if (!opportunity.IsSuccess)
            return ToActionResult(opportunity);
        if (string.Equals(opportunity.Value!.PosterId, ActorId, StringComparison.Ordinal))
            return BadRequest(new { error = "You cannot apply to your own opportunity." });

        var result = await _opportunityService.ApplyAsync(
            id,
            dto with { ApplicantId = ActorId },
            ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id }, result.Value)
            : ToActionResult(result);
    }

    [HttpGet("{id}/applications")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> GetApplications(string id, CancellationToken ct)
    {
        var opportunity = await GetManagedOpportunityAsync(id, ct);
        if (!opportunity.IsSuccess)
            return ToActionResult(opportunity);

        var result = await _opportunityService.GetApplicationsAsync(id, ct);
        return ToActionResult(result);
    }

    [HttpPatch("applications/{applicationId}/status")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> UpdateApplicationStatus(string applicationId, [FromBody] UpdateApplicationStatusDto dto, CancellationToken ct)
    {
        var application = await _opportunityService.GetApplicationByIdAsync(applicationId, ct);
        if (!application.IsSuccess)
            return ToActionResult(application);

        var opportunity = await GetManagedOpportunityAsync(application.Value!.OpportunityId, ct);
        if (!opportunity.IsSuccess)
            return ToActionResult(opportunity);

        var result = await _opportunityService.UpdateApplicationStatusAsync(applicationId, dto, ct);
        return ToActionResult(result);
    }

    // ===== Updates =====

    [HttpGet("{opportunityId}/updates")]
    public async Task<IActionResult> GetUpdates(string opportunityId, CancellationToken ct)
    {
        var result = await _opportunityService.GetUpdatesAsync(opportunityId, ct);
        return ToActionResult(result);
    }

    [HttpPost("{opportunityId}/updates")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> CreateUpdate(string opportunityId, [FromBody] CreateOpportunityUpdateDto dto, CancellationToken ct)
    {
        var opportunity = await GetManagedOpportunityAsync(opportunityId, ct);
        if (!opportunity.IsSuccess)
            return ToActionResult(opportunity);

        var dtoWithId = dto with { OpportunityId = opportunityId, UserId = ActorId };
        var result = await _opportunityService.CreateUpdateAsync(dtoWithId, ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = opportunityId }, result.Value)
            : ToActionResult(result);
    }

    [HttpDelete("updates/{updateId}")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> DeleteUpdate(string updateId, CancellationToken ct)
    {
        var update = await _opportunityService.GetUpdateByIdAsync(updateId, ct);
        if (!update.IsSuccess)
            return ToActionResult(update);

        var opportunity = await GetManagedOpportunityAsync(update.Value!.OpportunityId, ct);
        if (!opportunity.IsSuccess)
            return ToActionResult(opportunity);

        var result = await _opportunityService.DeleteUpdateAsync(updateId, ct);
        return result.IsSuccess ? NoContent() : ToActionResult(result);
    }

    // ===== Comments =====

    [HttpGet("{opportunityId}/comments")]
    public async Task<IActionResult> GetComments(string opportunityId, CancellationToken ct)
    {
        var result = await _opportunityService.GetCommentsAsync(opportunityId, ct);
        return ToActionResult(result);
    }

    [HttpPost("{opportunityId}/comments")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> AddComment(string opportunityId, [FromBody] CreateOpportunityCommentDto dto, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(dto.ParentId))
        {
            var parent = await _opportunityService.GetCommentByIdAsync(dto.ParentId, ct);
            if (!parent.IsSuccess ||
                !string.Equals(parent.Value!.OpportunityId, opportunityId, StringComparison.Ordinal))
            {
                return BadRequest(new { error = "Parent comment must belong to this opportunity." });
            }
        }

        var dtoWithId = dto with { OpportunityId = opportunityId, UserId = ActorId };
        var result = await _opportunityService.AddCommentAsync(dtoWithId, ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = opportunityId }, result.Value)
            : ToActionResult(result);
    }

    [HttpDelete("comments/{commentId}")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> DeleteComment(string commentId, CancellationToken ct)
    {
        var comment = await _opportunityService.GetCommentByIdAsync(commentId, ct);
        if (!comment.IsSuccess)
            return ToActionResult(comment);

        if (!string.Equals(comment.Value!.UserId, ActorId, StringComparison.Ordinal))
        {
            var opportunity = await GetManagedOpportunityAsync(comment.Value.OpportunityId, ct);
            if (!opportunity.IsSuccess)
                return ToActionResult(opportunity);
        }

        var result = await _opportunityService.DeleteCommentAsync(commentId, ct);
        return result.IsSuccess ? NoContent() : ToActionResult(result);
    }

    private string ActorId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    private bool IsAdmin => User.IsInRole(UserRole.ADMIN.ToString());

    private Task<bool> CanManageProjectAsync(string projectId, CancellationToken ct)
        => IsAdmin
            ? Task.FromResult(true)
            : _hierarchyAuthorization.CanManageProjectAsync(ActorId, projectId, ct);

    private async Task<bool> CanManageGuildAsync(string guildId, CancellationToken ct)
    {
        if (IsAdmin)
            return true;

        var guild = await _guildService.GetByIdAsync(guildId, ct);
        if (!guild.IsSuccess)
            return false;
        if (string.Equals(guild.Value!.OwnerId, ActorId, StringComparison.Ordinal))
            return true;

        var members = await _guildMemberService.GetByGuildIdAsync(guildId, ct);
        return members.IsSuccess && members.Value!.Any(member =>
            string.Equals(member.UserId, ActorId, StringComparison.Ordinal) &&
            member.Role is GuildMemberRole.OWNER or GuildMemberRole.ADMIN or GuildMemberRole.MANAGER);
    }

    private async Task<Result<OpportunityDto>> GetManagedOpportunityAsync(
        string opportunityId,
        CancellationToken ct)
    {
        var opportunity = await _opportunityService.GetByIdAsync(opportunityId, ct);
        if (!opportunity.IsSuccess)
            return opportunity;

        if (IsAdmin)
            return opportunity;

        var hasProject = !string.IsNullOrWhiteSpace(opportunity.Value!.ProjectId);
        var hasGuild = !string.IsNullOrWhiteSpace(opportunity.Value.GuildId);
        var canManage = hasProject == hasGuild
            ? !hasProject && string.Equals(opportunity.Value.PosterId, ActorId, StringComparison.Ordinal)
            : hasProject
                ? await CanManageProjectAsync(opportunity.Value.ProjectId!, ct)
                : await CanManageGuildAsync(opportunity.Value.GuildId!, ct);

        return canManage
            ? opportunity
            : Result<OpportunityDto>.Forbidden("You do not manage this opportunity.");
    }

    private async Task<IActionResult?> RequireValueListingAccountSetupAsync(
        decimal? compensation,
        decimal? equityPercent,
        string action,
        CancellationToken ct)
    {
        if (!(compensation is > 0m || equityPercent is > 0m))
            return null;

        var readiness = await _valueEligibility.RequireReadyAsync(ActorId, action, ct);
        return readiness.IsSuccess ? null : ValueActionForbidden(readiness.Error);
    }

    private IActionResult ValueActionForbidden(string? error)
        => StatusCode(403, new
        {
            error = error ?? "Complete account setup before publishing or updating paid or project-unit opportunities."
        });

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
