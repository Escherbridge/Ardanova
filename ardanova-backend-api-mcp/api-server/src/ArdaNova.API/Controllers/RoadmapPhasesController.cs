namespace ArdaNova.API.Controllers;

using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Interfaces;
using ArdaNova.Domain.Models.Enums;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/roadmap-phases")]
public class RoadmapPhasesController : ControllerBase
{
    private readonly IRoadmapPhaseService _roadmapPhaseService;

    public RoadmapPhasesController(IRoadmapPhaseService roadmapPhaseService)
    {
        _roadmapPhaseService = roadmapPhaseService;
    }

    [HttpGet("{phaseId}")]
    public async Task<IActionResult> GetById(string phaseId, CancellationToken ct)
    {
        var result = await _roadmapPhaseService.GetByIdAsync(phaseId, ct);
        return ToActionResult(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateRoadmapPhaseDto dto, CancellationToken ct)
    {
        var result = await _roadmapPhaseService.CreateAsync(dto, ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { phaseId = result.Value!.Id }, result.Value)
            : ToActionResult(result);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateRoadmapPhaseDto dto, CancellationToken ct)
    {
        var result = await _roadmapPhaseService.UpdateAsync(id, dto, ct);
        return ToActionResult(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id, CancellationToken ct)
    {
        var result = await _roadmapPhaseService.DeleteAsync(id, ct);
        return result.IsSuccess ? NoContent() : ToActionResult(result);
    }

    [HttpPut("{id}/assign")]
    public async Task<IActionResult> Assign(string id, [FromBody] AssignPhaseDto dto, CancellationToken ct)
    {
        var result = await _roadmapPhaseService.AssignAsync(id, dto.UserId, ct);
        return ToActionResult(result);
    }

    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdateStatus(string id, [FromBody] UpdatePhaseStatusDto dto, CancellationToken ct)
    {
        var result = await _roadmapPhaseService.UpdateStatusAsync(id, dto.Status, ct);
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

// DTOs for request bodies
public record AssignPhaseDto
{
    public string? UserId { get; init; }
}

public record UpdatePhaseStatusDto
{
    public PhaseStatus Status { get; init; }
}
