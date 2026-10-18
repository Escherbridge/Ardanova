namespace ArdaNova.API.Controllers;

using System.Security.Claims;
using ArdaNova.API.Middleware;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Interfaces;
using ArdaNova.Domain.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/opportunity-bids")]
public class OpportunityBidsController : ControllerBase
{
    private readonly IOpportunityBidService _opportunityBidService;
    private readonly IOpportunityService _opportunityService;
    private readonly IHierarchyAuthorizationService _hierarchyAuthorization;
    private readonly ITaskCommerceService _taskCommerceService;

    public OpportunityBidsController(
        IOpportunityBidService opportunityBidService,
        IOpportunityService opportunityService,
        IHierarchyAuthorizationService hierarchyAuthorization,
        ITaskCommerceService taskCommerceService)
    {
        _opportunityBidService = opportunityBidService;
        _opportunityService = opportunityService;
        _hierarchyAuthorization = hierarchyAuthorization;
        _taskCommerceService = taskCommerceService;
    }

    /// <summary>A bid is readable by its bidder and the opportunity's managers; anyone else gets 404.</summary>
    [HttpGet("{id}")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> GetById(string id, CancellationToken ct)
    {
        var bid = await _opportunityBidService.GetByIdAsync(id, ct);
        if (!bid.IsSuccess)
            return ToActionResult(bid);

        var readable = string.Equals(bid.Value!.BidderId, ActorId, StringComparison.Ordinal)
            || await ActorManagesOpportunityAsync(bid.Value.OpportunityId, ct);
        return readable ? Ok(bid.Value) : BidNotFound(id);
    }

    /// <summary>The bid list (proposals and prices) is for the opportunity's managers only; see Controllers/AGENTS.md §Bid privacy.</summary>
    [HttpGet("/api/opportunities/{opportunityId}/bids")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> GetByOpportunityId(string opportunityId, CancellationToken ct)
    {
        if (!await ActorManagesOpportunityAsync(opportunityId, ct))
            return NotFound(new { error = $"Opportunity with id {opportunityId} not found" });

        return ToActionResult(await _opportunityBidService.GetByOpportunityIdAsync(opportunityId, ct));
    }

    [HttpGet("me")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> GetMine(CancellationToken ct)
    {
        return ToActionResult(await _opportunityBidService.GetByBidderIdAsync(ActorId, ct));
    }

    /// <summary>A guild's bids, narrowed to the ones the signed actor submitted (admins see all).</summary>
    [HttpGet("guild/{guildId}")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> GetByGuildId(string guildId, CancellationToken ct)
    {
        var bids = await _opportunityBidService.GetByGuildIdAsync(guildId, ct);
        if (!bids.IsSuccess || IsAdmin)
            return ToActionResult(bids);

        IReadOnlyList<OpportunityBidDto> own = bids.Value!
            .Where(bid => string.Equals(bid.BidderId, ActorId, StringComparison.Ordinal))
            .ToList();
        return Ok(own);
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> Create([FromBody] CreateOpportunityBidRequest dto, CancellationToken ct)
    {
        var result = await _opportunityBidService.CreateAsync(new CreateOpportunityBidDto
        {
            OpportunityId = dto.OpportunityId,
            BidderId = ActorId,
            GuildId = dto.GuildId,
            ProposedAmount = dto.ProposedAmount,
            GuildAwardSplit = dto.GuildAwardSplit,
            Proposal = dto.Proposal,
            EstimatedHours = dto.EstimatedHours,
            Timeline = dto.Timeline,
            Deliverables = dto.Deliverables
        }, ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value)
            : ToActionResult(result);
    }

    [HttpPut("{id}")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateOpportunityBidDto dto, CancellationToken ct)
    {
        if (!await ActorOwnsBidAsync(id, ct))
            return Forbid();

        return ToActionResult(await _opportunityBidService.UpdateAsync(id, dto, ct));
    }

    [HttpPost("{id}/accept")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> Accept(string id, CancellationToken ct)
    {
        return ToActionResult(await _taskCommerceService.AcceptBidAsync(id, ActorId, ct));
    }

    [HttpPost("{id}/reject")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> Reject(string id, CancellationToken ct)
    {
        if (!await ActorOwnsOpportunityAsync(id, ct))
            return Forbid();

        return ToActionResult(await _opportunityBidService.RejectAsync(id, ct));
    }

    [HttpPost("{id}/withdraw")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> Withdraw(string id, CancellationToken ct)
    {
        if (!await ActorOwnsBidAsync(id, ct))
            return Forbid();

        return ToActionResult(await _opportunityBidService.WithdrawAsync(id, ct));
    }

    [HttpPost("{id}/complete")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> Complete(string id, CancellationToken ct)
    {
        if (!await ActorOwnsOpportunityAsync(id, ct))
            return Forbid();

        return ToActionResult(await _opportunityBidService.CompleteAsync(id, ct));
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> Delete(string id, CancellationToken ct)
    {
        if (!await ActorOwnsBidAsync(id, ct))
            return Forbid();

        var result = await _opportunityBidService.DeleteAsync(id, ct);
        return result.IsSuccess ? NoContent() : ToActionResult(result);
    }

    private string ActorId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    private bool IsAdmin => User.IsInRole(UserRole.ADMIN.ToString());

    private NotFoundObjectResult BidNotFound(string id)
        => NotFound(new { error = $"Opportunity bid with id {id} not found" });

    /// <summary>Same rule as OpportunitiesController: project managers for a project role, else the poster.</summary>
    private async Task<bool> ActorManagesOpportunityAsync(string opportunityId, CancellationToken ct)
    {
        var opportunity = await _opportunityService.GetByIdAsync(opportunityId, ct);
        if (!opportunity.IsSuccess)
            return false;
        if (IsAdmin)
            return true;

        return string.IsNullOrWhiteSpace(opportunity.Value!.ProjectId)
            ? string.Equals(opportunity.Value.PosterId, ActorId, StringComparison.Ordinal)
            : await _hierarchyAuthorization.CanManageProjectAsync(ActorId, opportunity.Value.ProjectId, ct);
    }

    private async Task<bool> ActorOwnsBidAsync(string id, CancellationToken ct)
    {
        var bid = await _opportunityBidService.GetByIdAsync(id, ct);
        return bid.IsSuccess && string.Equals(bid.Value!.BidderId, ActorId, StringComparison.Ordinal);
    }

    private async Task<bool> ActorOwnsOpportunityAsync(string bidId, CancellationToken ct)
    {
        var bid = await _opportunityBidService.GetByIdAsync(bidId, ct);
        return bid.IsSuccess && await ActorManagesOpportunityAsync(bid.Value!.OpportunityId, ct);
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
            ResultType.Forbidden => StatusCode(StatusCodes.Status403Forbidden, new { error = result.Error }),
            ResultType.Conflict => Conflict(new { error = result.Error }),
            _ => BadRequest(new { error = result.Error })
        };
    }

    public sealed record CreateOpportunityBidRequest(
        string OpportunityId,
        string? GuildId,
        decimal? ProposedAmount,
        decimal? GuildAwardSplit,
        string Proposal,
        int? EstimatedHours,
        string? Timeline,
        string? Deliverables);
}
