namespace ArdaNova.API.Controllers;

using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Interfaces;
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

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await _delegatedVoteService.GetByIdAsync(id, ct);
        return ToActionResult(result);
    }

    [HttpGet("delegator/{delegatorId:guid}")]
    public async Task<IActionResult> GetByDelegatorId(Guid delegatorId, CancellationToken ct)
    {
        var result = await _delegatedVoteService.GetByDelegatorIdAsync(delegatorId, ct);
        return ToActionResult(result);
    }

    [HttpGet("delegatee/{delegateeId:guid}")]
    public async Task<IActionResult> GetByDelegateeId(Guid delegateeId, CancellationToken ct)
    {
        var result = await _delegatedVoteService.GetByDelegateeIdAsync(delegateeId, ct);
        return ToActionResult(result);
    }

    [HttpGet("project/{projectId:guid}")]
    public async Task<IActionResult> GetByProjectId(Guid projectId, CancellationToken ct)
    {
        var result = await _delegatedVoteService.GetByProjectIdAsync(projectId, ct);
        return ToActionResult(result);
    }

    [HttpGet("project/{projectId:guid}/active")]
    public async Task<IActionResult> GetActiveByProjectId(Guid projectId, CancellationToken ct)
    {
        var result = await _delegatedVoteService.GetActiveByProjectIdAsync(projectId, ct);
        return ToActionResult(result);
    }

    [HttpGet("delegatee/{delegateeId:guid}/project/{projectId:guid}/power")]
    public async Task<IActionResult> GetTotalDelegatedPower(Guid delegateeId, Guid projectId, CancellationToken ct)
    {
        var result = await _delegatedVoteService.GetTotalDelegatedPowerAsync(delegateeId, projectId, ct);
        return ToActionResult(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateDelegatedVoteDto dto, CancellationToken ct)
    {
        var result = await _delegatedVoteService.CreateAsync(dto, ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value)
            : ToActionResult(result);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateDelegatedVoteDto dto, CancellationToken ct)
    {
        var result = await _delegatedVoteService.UpdateAsync(id, dto, ct);
        return ToActionResult(result);
    }

    [HttpPost("{id:guid}/revoke")]
    public async Task<IActionResult> Revoke(Guid id, CancellationToken ct)
    {
        var result = await _delegatedVoteService.RevokeAsync(id, ct);
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
