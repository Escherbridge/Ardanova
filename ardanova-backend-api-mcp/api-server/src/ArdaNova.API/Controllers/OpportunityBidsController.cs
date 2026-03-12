namespace ArdaNova.API.Controllers;

using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/opportunity-bids")]
public class OpportunityBidsController : ControllerBase
{
    private readonly IOpportunityBidService _opportunityBidService;

    public OpportunityBidsController(IOpportunityBidService opportunityBidService)
    {
        _opportunityBidService = opportunityBidService;
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id, CancellationToken ct)
    {
        var result = await _opportunityBidService.GetByIdAsync(id, ct);
        return ToActionResult(result);
    }

    // Absolute route to override base route and match GET /api/opportunities/{opportunityId}/bids
    [HttpGet("/api/opportunities/{opportunityId}/bids")]
    public async Task<IActionResult> GetByOpportunityId(string opportunityId, CancellationToken ct)
    {
        var result = await _opportunityBidService.GetByOpportunityIdAsync(opportunityId, ct);
        return ToActionResult(result);
    }

    [HttpGet("bidder/{bidderId}")]
    public async Task<IActionResult> GetByBidderId(string bidderId, CancellationToken ct)
    {
        var result = await _opportunityBidService.GetByBidderIdAsync(bidderId, ct);
        return ToActionResult(result);
    }

    [HttpGet("guild/{guildId}")]
    public async Task<IActionResult> GetByGuildId(string guildId, CancellationToken ct)
    {
        var result = await _opportunityBidService.GetByGuildIdAsync(guildId, ct);
        return ToActionResult(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateOpportunityBidDto dto, CancellationToken ct)
    {
        var result = await _opportunityBidService.CreateAsync(dto, ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value)
            : ToActionResult(result);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateOpportunityBidDto dto, CancellationToken ct)
    {
        var result = await _opportunityBidService.UpdateAsync(id, dto, ct);
        return ToActionResult(result);
    }

    [HttpPost("{id}/accept")]
    public async Task<IActionResult> Accept(string id, CancellationToken ct)
    {
        var result = await _opportunityBidService.AcceptAsync(id, ct);
        return ToActionResult(result);
    }

    [HttpPost("{id}/reject")]
    public async Task<IActionResult> Reject(string id, CancellationToken ct)
    {
        var result = await _opportunityBidService.RejectAsync(id, ct);
        return ToActionResult(result);
    }

    [HttpPost("{id}/withdraw")]
    public async Task<IActionResult> Withdraw(string id, CancellationToken ct)
    {
        var result = await _opportunityBidService.WithdrawAsync(id, ct);
        return ToActionResult(result);
    }

    [HttpPost("{id}/complete")]
    public async Task<IActionResult> Complete(string id, CancellationToken ct)
    {
        var result = await _opportunityBidService.CompleteAsync(id, ct);
        return ToActionResult(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id, CancellationToken ct)
    {
        var result = await _opportunityBidService.DeleteAsync(id, ct);
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
}
