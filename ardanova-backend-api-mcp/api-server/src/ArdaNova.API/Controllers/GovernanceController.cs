namespace ArdaNova.API.Controllers;

using System.Security.Claims;
using ArdaNova.API.Middleware;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Implementations;
using ArdaNova.Application.Services.Interfaces;
using ArdaNova.Domain.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

/// <summary>Cross-project governance surface; every mutation is bound to the authenticated actor and authorised against the proposal's project.</summary>
[ApiController]
[Route("api/[controller]")]
public class GovernanceController : ControllerBase
{
    private readonly IGovernanceService _governanceService;
    private readonly IHierarchyAuthorizationService _hierarchyAuthorization;

    public GovernanceController(
        IGovernanceService governanceService,
        IHierarchyAuthorizationService hierarchyAuthorization)
    {
        _governanceService = governanceService;
        _hierarchyAuthorization = hierarchyAuthorization;
    }

    // Proposal endpoints

    [HttpGet("proposals")]
    public async Task<IActionResult> GetAllProposals(CancellationToken ct)
    {
        var result = await _governanceService.GetAllVisibleProposalsAsync(Viewer, ct);
        return ToActionResult(result);
    }

    [HttpGet("proposals/paged")]
    public async Task<IActionResult> GetProposalsPaged([FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken ct = default)
    {
        var result = await _governanceService.GetVisibleProposalsPagedAsync(Viewer, page, pageSize, ct);
        return ToActionResult(result);
    }

    [HttpGet("proposals/search")]
    public async Task<IActionResult> SearchProposals(
        [FromQuery] string? searchTerm,
        [FromQuery] ProposalType? type,
        [FromQuery] ProposalStatus? status,
        [FromQuery] string? projectId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken ct = default)
    {
        var result = await _governanceService.SearchVisibleProposalsAsync(Viewer, searchTerm, type, status, projectId, page, pageSize, ct);
        return ToActionResult(result);
    }

    [HttpGet("proposals/{id}")]
    public async Task<IActionResult> GetProposalById(string id, CancellationToken ct)
    {
        var result = await _governanceService.GetVisibleProposalByIdAsync(Viewer, id, ct);
        return ToActionResult(result);
    }

    [HttpGet("proposals/active")]
    public async Task<IActionResult> GetActiveProposals(CancellationToken ct)
    {
        var result = await _governanceService.GetVisibleActiveProposalsAsync(Viewer, ct);
        return ToActionResult(result);
    }

    [HttpGet("proposals/proposer/{proposerId}")]
    public async Task<IActionResult> GetByProposerId(string proposerId, CancellationToken ct)
    {
        var result = await _governanceService.GetVisibleByProposerIdAsync(Viewer, proposerId, ct);
        return ToActionResult(result);
    }

    [HttpPost("proposals")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> CreateProposal([FromBody] CreateProposalDto dto, CancellationToken ct)
    {
        if (!IsAdmin && !await _hierarchyAuthorization.IsProjectMemberAsync(ActorId, dto.ProjectId, ct))
            return NotAuthorized("Only members of this project can raise a proposal on it");

        var result = await _governanceService.CreateProposalAsync(dto with { CreatorId = ActorId }, ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetProposalById), new { id = result.Value!.Id }, result.Value)
            : ToActionResult(result);
    }

    [HttpPut("proposals/{id}")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> UpdateProposal(string id, [FromBody] UpdateProposalDto dto, CancellationToken ct)
    {
        var denied = await DenyUnlessMayActOnProposalAsync(id, ct);
        if (denied is not null)
            return denied;

        var result = await _governanceService.UpdateProposalAsync(id, dto, ct);
        return ToActionResult(result);
    }

    [HttpDelete("proposals/{id}")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> DeleteProposal(string id, CancellationToken ct)
    {
        var denied = await DenyUnlessMayActOnProposalAsync(id, ct);
        if (denied is not null)
            return denied;

        var result = await _governanceService.DeleteProposalAsync(id, ct);
        return result.IsSuccess ? NoContent() : ToActionResult(result);
    }

    // Voting endpoints

    /// <summary>The ballot names the signed-in member; a voter id or weight in the body is discarded.</summary>
    [HttpPost("proposals/{id}/vote")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> CastVote(string id, [FromBody] CastVoteDto dto, CancellationToken ct)
    {
        var result = await _governanceService.CastVoteAsync(
            id,
            dto with { VoterId = ActorId, Weight = null },
            ct);
        return ToActionResult(result);
    }

    [HttpGet("proposals/{id}/votes")]
    public async Task<IActionResult> GetVotes(string id, CancellationToken ct)
    {
        if (await ProposalNotVisibleAsync(id, ct) is { } hidden)
            return hidden;

        var result = await _governanceService.GetVotesAsync(id, ct);
        return ToActionResult(result);
    }

    [HttpGet("proposals/{id}/my-vote")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> GetMyVote(string id, [FromQuery] string userId, CancellationToken ct)
    {
        if (!IsAdmin && !string.Equals(userId, ActorId, StringComparison.Ordinal))
            return NotAuthorized("You can only read your own vote");

        var result = await _governanceService.GetUserVoteAsync(id, userId, ct);
        return ToActionResult(result);
    }

    [HttpGet("proposals/{id}/summary")]
    public async Task<IActionResult> GetVoteSummary(string id, CancellationToken ct)
    {
        if (await ProposalNotVisibleAsync(id, ct) is { } hidden)
            return hidden;

        var result = await _governanceService.GetVoteSummaryAsync(id, ct);
        return ToActionResult(result);
    }

    // Proposal lifecycle endpoints

    [HttpPatch("proposals/{id}/execute")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> ExecuteProposal(string id, CancellationToken ct)
    {
        var denied = await DenyUnlessMayActOnProposalAsync(id, ct, creatorMayAct: false);
        if (denied is not null)
            return denied;

        var result = await _governanceService.ExecuteProposalAsync(id, ct);
        return ToActionResult(result);
    }

    /// <summary>Closing ends the voting window now and records the outcome; withdrawing (cancel) throws the proposal away and is refused once anyone has voted.</summary>
    [HttpPatch("proposals/{id}/close")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> CloseProposal(string id, CancellationToken ct)
    {
        var denied = await DenyUnlessMayActOnProposalAsync(id, ct);
        if (denied is not null)
            return denied;

        var result = await _governanceService.UpdateProposalAsync(
            id,
            new UpdateProposalDto { VotingEnd = DateTime.UtcNow },
            ct);
        return ToActionResult(result);
    }

    [HttpPatch("proposals/{id}/cancel")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> CancelProposal(string id, CancellationToken ct)
    {
        var denied = await DenyUnlessMayActOnProposalAsync(id, ct);
        if (denied is not null)
            return denied;

        var result = await _governanceService.CancelProposalAsync(id, ct);
        return ToActionResult(result);
    }

    [HttpPatch("proposals/{id}/publish")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> PublishProposal(string id, CancellationToken ct)
    {
        var denied = await DenyUnlessMayActOnProposalAsync(id, ct);
        if (denied is not null)
            return denied;

        var result = await _governanceService.PublishProposalAsync(id, ct);
        return ToActionResult(result);
    }

    // Proposal comment endpoints

    [HttpGet("proposals/{id}/comments")]
    public async Task<IActionResult> GetProposalComments(string id, CancellationToken ct)
    {
        if (await ProposalNotVisibleAsync(id, ct) is { } hidden)
            return hidden;

        var result = await _governanceService.GetProposalCommentsAsync(id, ct);
        return ToActionResult(result);
    }

    [HttpPost("proposals/{id}/comments")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> CreateProposalComment(string id, [FromBody] CreateProposalCommentDto dto, CancellationToken ct)
    {
        var result = await _governanceService.CreateProposalCommentAsync(
            dto with { ProposalId = id, UserId = ActorId },
            ct);
        return ToActionResult(result);
    }

    [HttpPut("proposals/{id}/comments/{commentId}")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> UpdateProposalComment(
        string id,
        string commentId,
        [FromBody] UpdateProposalCommentDto dto,
        CancellationToken ct)
    {
        var result = await _governanceService.UpdateProposalCommentAsync(id, commentId, ActorId, dto, ct);
        return ToActionResult(result);
    }

    [HttpDelete("proposals/{id}/comments/{commentId}")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> DeleteProposalComment(string id, string commentId, CancellationToken ct)
    {
        var result = await _governanceService.DeleteProposalCommentAsync(id, commentId, ActorId, ct);
        return result.IsSuccess ? NoContent() : ToActionResult(result);
    }

    private string ActorId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    private bool IsAdmin => User.IsInRole(UserRole.ADMIN.ToString());

    /// <summary>Only a verified actor assertion names a viewer; every other read is the public's.</summary>
    private ProjectViewer Viewer => ActorAssertionMiddleware.TryGetActorId(User, out var actorId)
        ? new ProjectViewer(actorId, IsAdmin)
        : ProjectViewer.Anonymous;

    /// <summary>NotFound when this viewer may not see the proposal, so a private proposal's ballots and discussion are as hidden as the proposal itself.</summary>
    private async Task<IActionResult?> ProposalNotVisibleAsync(string proposalId, CancellationToken ct)
    {
        var proposal = await _governanceService.GetVisibleProposalByIdAsync(Viewer, proposalId, ct);
        return proposal.IsSuccess ? null : ToActionResult(proposal);
    }

    /// <summary>A proposal's lifecycle belongs to the people who run its project, plus — where it is their own — the member who raised it. Returns the refusal to send, or null when the actor may act.</summary>
    private async Task<IActionResult?> DenyUnlessMayActOnProposalAsync(
        string proposalId,
        CancellationToken ct,
        bool creatorMayAct = true)
    {
        var proposal = await _governanceService.GetProposalByIdAsync(proposalId, ct);
        if (!proposal.IsSuccess)
            return ToActionResult(proposal);

        if (IsAdmin)
            return null;

        if (creatorMayAct && string.Equals(proposal.Value!.CreatorId, ActorId, StringComparison.Ordinal))
            return null;

        if (await _hierarchyAuthorization.CanManageProjectAsync(ActorId, proposal.Value!.ProjectId, ct))
            return null;

        return NotAuthorized("Only the people who run this project can take that action");
    }

    /// <summary>A refusal the reader can act on: 403 with the reason in the body, rather than an empty Forbid.</summary>
    private IActionResult NotAuthorized(string reason)
        => StatusCode(StatusCodes.Status403Forbidden, new { error = reason });

    private IActionResult ToActionResult<T>(Result<T> result)
    {
        if (result.IsSuccess)
            return Ok(result.Value);

        return result.Type switch
        {
            ResultType.NotFound => NotFound(new { error = result.Error }),
            ResultType.ValidationError => BadRequest(new { error = result.Error }),
            ResultType.Unauthorized => Unauthorized(new { error = result.Error }),
            ResultType.Forbidden => NotAuthorized(result.Error ?? "Not permitted"),
            _ => BadRequest(new { error = result.Error })
        };
    }
}
