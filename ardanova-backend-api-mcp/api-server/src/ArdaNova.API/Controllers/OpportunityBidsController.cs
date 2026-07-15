namespace ArdaNova.API.Controllers;

using System.Security.Claims;
using ArdaNova.API.Middleware;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/opportunity-bids")]
public class OpportunityBidsController : ControllerBase
{
    private readonly IOpportunityBidService _opportunityBidService;
    private readonly IOpportunityService _opportunityService;
    private readonly IProjectService _projectService;
    private readonly ITaskCommerceService _taskCommerceService;

    public OpportunityBidsController(
        IOpportunityBidService opportunityBidService,
        IOpportunityService opportunityService,
        IProjectService projectService,
        ITaskCommerceService taskCommerceService)
    {
        _opportunityBidService = opportunityBidService;
        _opportunityService = opportunityService;
        _projectService = projectService;
        _taskCommerceService = taskCommerceService;
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id, CancellationToken ct)
    {
        return ToActionResult(await _opportunityBidService.GetByIdAsync(id, ct));
    }

    [HttpGet("/api/opportunities/{opportunityId}/bids")]
    public async Task<IActionResult> GetByOpportunityId(string opportunityId, CancellationToken ct)
    {
        return ToActionResult(await _opportunityBidService.GetByOpportunityIdAsync(opportunityId, ct));
    }

    [HttpGet("me")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> GetMine(CancellationToken ct)
    {
        return ToActionResult(await _opportunityBidService.GetByBidderIdAsync(ActorId, ct));
    }

    [HttpGet("guild/{guildId}")]
    public async Task<IActionResult> GetByGuildId(string guildId, CancellationToken ct)
    {
        return ToActionResult(await _opportunityBidService.GetByGuildIdAsync(guildId, ct));
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

    private async Task<bool> ActorOwnsBidAsync(string id, CancellationToken ct)
    {
        var bid = await _opportunityBidService.GetByIdAsync(id, ct);
        return bid.IsSuccess && string.Equals(bid.Value!.BidderId, ActorId, StringComparison.Ordinal);
    }

    private async Task<bool> ActorOwnsOpportunityAsync(string bidId, CancellationToken ct)
    {
        var bid = await _opportunityBidService.GetByIdAsync(bidId, ct);
        if (!bid.IsSuccess)
            return false;

        var opportunity = await _opportunityService.GetByIdAsync(bid.Value!.OpportunityId, ct);
        if (!opportunity.IsSuccess)
            return false;

        if (string.IsNullOrWhiteSpace(opportunity.Value!.ProjectId))
            return string.Equals(opportunity.Value.PosterId, ActorId, StringComparison.Ordinal);

        var project = await _projectService.GetByIdAsync(opportunity.Value.ProjectId, ct);
        return project.IsSuccess && string.Equals(project.Value!.CreatedById, ActorId, StringComparison.Ordinal);
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
            ResultType.Conflict => Conflict(new { error = result.Error }),
            _ => BadRequest(new { error = result.Error })
        };
    }

    public sealed record CreateOpportunityBidRequest(
        string OpportunityId,
        string? GuildId,
        decimal? ProposedAmount,
        string Proposal,
        int? EstimatedHours,
        string? Timeline,
        string? Deliverables);
}
