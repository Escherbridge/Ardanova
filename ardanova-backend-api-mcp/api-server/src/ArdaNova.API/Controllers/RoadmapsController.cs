namespace ArdaNova.API.Controllers;

using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Interfaces;
using ArdaNova.Domain.Models.Enums;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class RoadmapsController : ControllerBase
{
    private readonly IRoadmapService _roadmapService;
    private readonly IRoadmapPhaseService _roadmapPhaseService;

    public RoadmapsController(IRoadmapService roadmapService, IRoadmapPhaseService roadmapPhaseService)
    {
        _roadmapService = roadmapService;
        _roadmapPhaseService = roadmapPhaseService;
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id, CancellationToken ct)
    {
        var result = await _roadmapService.GetByIdAsync(id, ct);
        return ToActionResult(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateRoadmapDto dto, CancellationToken ct)
    {
        var result = await _roadmapService.CreateAsync(dto, ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value)
            : ToActionResult(result);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateRoadmapDto dto, CancellationToken ct)
    {
        var result = await _roadmapService.UpdateAsync(id, dto, ct);
        return ToActionResult(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id, CancellationToken ct)
    {
        var result = await _roadmapService.DeleteAsync(id, ct);
        return result.IsSuccess ? NoContent() : ToActionResult(result);
    }

    [HttpPut("{id}/assign")]
    public async Task<IActionResult> Assign(string id, [FromBody] AssignRoadmapDto dto, CancellationToken ct)
    {
        var result = await _roadmapService.AssignAsync(id, dto.UserId, ct);
        return ToActionResult(result);
    }

    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdateStatus(string id, [FromBody] UpdateRoadmapStatusDto dto, CancellationToken ct)
    {
        var result = await _roadmapService.UpdateStatusAsync(id, dto.Status, ct);
        return ToActionResult(result);
    }

    [HttpGet("{roadmapId}/phases")]
    public async Task<IActionResult> GetPhases(string roadmapId, CancellationToken ct)
    {
        var result = await _roadmapPhaseService.GetByRoadmapIdAsync(roadmapId, ct);
        return ToActionResult(result);
    }

    [HttpPut("{roadmapId}/phases/reorder")]
    public async Task<IActionResult> ReorderPhases(string roadmapId, [FromBody] ReorderPhasesDto dto, CancellationToken ct)
    {
        var result = await _roadmapPhaseService.ReorderAsync(roadmapId, dto.PhaseIds, ct);
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

// Additional route for /api/projects/{projectId}/roadmap
[ApiController]
[Route("api/projects")]
public class ProjectRoadmapsController : ControllerBase
{
    private readonly IRoadmapService _roadmapService;

    public ProjectRoadmapsController(IRoadmapService roadmapService)
    {
        _roadmapService = roadmapService;
    }

    [HttpGet("{projectId}/roadmap")]
    public async Task<IActionResult> GetByProjectId(string projectId, CancellationToken ct)
    {
        var result = await _roadmapService.GetByProjectIdAsync(projectId, ct);
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
public record AssignRoadmapDto
{
    public string? UserId { get; init; }
}

public record UpdateRoadmapStatusDto
{
    public RoadmapStatus Status { get; init; }
}

public record ReorderPhasesDto
{
    public required IReadOnlyList<string> PhaseIds { get; init; }
}
