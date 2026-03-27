namespace ArdaNova.API.Controllers;

using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Interfaces;
using ArdaNova.Domain.Models.Enums;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class GovernanceController : ControllerBase
{
    private readonly IGovernanceService _governanceService;

    public GovernanceController(IGovernanceService governanceService)
    {
        _governanceService = governanceService;
    }

    // Proposal endpoints

    [HttpGet("proposals")]
    public async Task<IActionResult> GetAllProposals(CancellationToken ct)
    {
        var result = await _governanceService.GetAllProposalsAsync(ct);
        return ToActionResult(result);
    }

    [HttpGet("proposals/paged")]
    public async Task<IActionResult> GetProposalsPaged([FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken ct = default)
    {
        var result = await _governanceService.GetProposalsPagedAsync(page, pageSize, ct);
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
        var result = await _governanceService.SearchProposalsAsync(searchTerm, type, status, projectId, page, pageSize, ct);
        return ToActionResult(result);
    }

    [HttpGet("proposals/{id}")]
    public async Task<IActionResult> GetProposalById(string id, CancellationToken ct)
    {
        var result = await _governanceService.GetProposalByIdAsync(id, ct);
        return ToActionResult(result);
    }

    [HttpGet("proposals/active")]
    public async Task<IActionResult> GetActiveProposals(CancellationToken ct)
    {
        var result = await _governanceService.GetActiveProposalsAsync(ct);
        return ToActionResult(result);
    }

    [HttpGet("proposals/proposer/{proposerId}")]
    public async Task<IActionResult> GetByProposerId(string proposerId, CancellationToken ct)
    {
        var result = await _governanceService.GetByProposerIdAsync(proposerId, ct);
        return ToActionResult(result);
    }

    [HttpPost("proposals")]
    public async Task<IActionResult> CreateProposal([FromBody] CreateProposalDto dto, CancellationToken ct)
    {
        var result = await _governanceService.CreateProposalAsync(dto, ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetProposalById), new { id = result.Value!.Id }, result.Value)
            : ToActionResult(result);
    }

    [HttpPut("proposals/{id}")]
    public async Task<IActionResult> UpdateProposal(string id, [FromBody] UpdateProposalDto dto, CancellationToken ct)
    {
        var result = await _governanceService.UpdateProposalAsync(id, dto, ct);
        return ToActionResult(result);
    }

    [HttpDelete("proposals/{id}")]
    public async Task<IActionResult> DeleteProposal(string id, CancellationToken ct)
    {
        var result = await _governanceService.DeleteProposalAsync(id, ct);
        return result.IsSuccess ? NoContent() : ToActionResult(result);
    }

    // Voting endpoints

    [HttpPost("proposals/{id}/vote")]
    public async Task<IActionResult> CastVote(string id, [FromBody] CastVoteDto dto, CancellationToken ct)
    {
        var result = await _governanceService.CastVoteAsync(id, dto, ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetProposalById), new { id }, result.Value)
            : ToActionResult(result);
    }

    [HttpGet("proposals/{id}/votes")]
    public async Task<IActionResult> GetVotes(string id, CancellationToken ct)
    {
        var result = await _governanceService.GetVotesAsync(id, ct);
        return ToActionResult(result);
    }

    [HttpGet("proposals/{id}/my-vote")]
    public async Task<IActionResult> GetMyVote(string id, [FromQuery] string userId, CancellationToken ct)
    {
        var result = await _governanceService.GetUserVoteAsync(id, userId, ct);
        return ToActionResult(result);
    }

    [HttpGet("proposals/{id}/summary")]
    public async Task<IActionResult> GetVoteSummary(string id, CancellationToken ct)
    {
        var result = await _governanceService.GetVoteSummaryAsync(id, ct);
        return ToActionResult(result);
    }

    // Proposal lifecycle endpoints

    [HttpPatch("proposals/{id}/execute")]
    public async Task<IActionResult> ExecuteProposal(string id, CancellationToken ct)
    {
        var result = await _governanceService.ExecuteProposalAsync(id, ct);
        return ToActionResult(result);
    }

    [HttpPatch("proposals/{id}/cancel")]
    public async Task<IActionResult> CancelProposal(string id, CancellationToken ct)
    {
        var result = await _governanceService.CancelProposalAsync(id, ct);
        return ToActionResult(result);
    }

    [HttpPatch("proposals/{id}/publish")]
    public async Task<IActionResult> PublishProposal(string id, CancellationToken ct)
    {
        var result = await _governanceService.PublishProposalAsync(id, ct);
        return ToActionResult(result);
    }

    // Proposal comment endpoints

    [HttpGet("proposals/{id}/comments")]
    public async Task<IActionResult> GetProposalComments(string id, CancellationToken ct)
    {
        var result = await _governanceService.GetProposalCommentsAsync(id, ct);
        return ToActionResult(result);
    }

    [HttpPost("proposals/{id}/comments")]
    public async Task<IActionResult> CreateProposalComment(string id, [FromBody] CreateProposalCommentDto dto, CancellationToken ct)
    {
        var dtoWithProposal = dto with { ProposalId = id };
        var result = await _governanceService.CreateProposalCommentAsync(dtoWithProposal, ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetProposalById), new { id }, result.Value)
            : ToActionResult(result);
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
