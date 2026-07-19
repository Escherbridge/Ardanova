namespace ArdaNova.API.Controllers;

using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class DelegatedVotesController : ControllerBase
{
    private readonly IDelegatedVoteService _delegatedVoteService;

    public DelegatedVotesController(IDelegatedVoteService delegatedVoteService)
    {
        _delegatedVoteService = delegatedVoteService;
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id, CancellationToken ct)
    {
        var result = await _delegatedVoteService.GetByIdAsync(id, ct);
        return ToActionResult(result);
    }

    [HttpGet("delegator/{delegatorId}")]
    public async Task<IActionResult> GetByDelegatorId(string delegatorId, CancellationToken ct)
    {
        var result = await _delegatedVoteService.GetByDelegatorIdAsync(delegatorId, ct);
        return ToActionResult(result);
    }

    [HttpGet("delegatee/{delegateeId}")]
    public async Task<IActionResult> GetByDelegateeId(string delegateeId, CancellationToken ct)
    {
        var result = await _delegatedVoteService.GetByDelegateeIdAsync(delegateeId, ct);
        return ToActionResult(result);
    }

    [HttpGet("project/{projectId}")]
    public async Task<IActionResult> GetByProjectId(string projectId, CancellationToken ct)
    {
        var result = await _delegatedVoteService.GetByProjectIdAsync(projectId, ct);
        return ToActionResult(result);
    }

    [HttpGet("project/{projectId}/active")]
    public async Task<IActionResult> GetActiveByProjectId(string projectId, CancellationToken ct)
    {
        var result = await _delegatedVoteService.GetActiveByProjectIdAsync(projectId, ct);
        return ToActionResult(result);
    }

    [HttpGet("delegatee/{delegateeId}/project/{projectId}/power")]
    public async Task<IActionResult> GetTotalDelegatedPower(string delegateeId, string projectId, CancellationToken ct)
    {
        var result = await _delegatedVoteService.GetTotalDelegatedPowerAsync(delegateeId, projectId, ct);
        return ToActionResult(result);
    }

    [HttpPost]
    public IActionResult Create([FromBody] CreateDelegatedVoteDto dto, CancellationToken ct)
    {
        _ = dto;
        _ = ct;
        return MutationUnavailable();
    }

    [HttpPut("{id}")]
    public IActionResult Update(string id, [FromBody] UpdateDelegatedVoteDto dto, CancellationToken ct)
    {
        _ = id;
        _ = dto;
        _ = ct;
        return MutationUnavailable();
    }

    [HttpPost("{id}/revoke")]
    public IActionResult Revoke(string id, CancellationToken ct)
    {
        _ = id;
        _ = ct;
        return MutationUnavailable();
    }

    private IActionResult MutationUnavailable()
        => Problem(
            statusCode: StatusCodes.Status501NotImplemented,
            title: "Vote delegation mutations are unavailable",
            detail: "The authenticated delegator, project authority, and delegated share must be verified atomically before this mutation surface can be enabled.");

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
