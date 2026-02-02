namespace ArdaNova.API.Controllers;

using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Interfaces;
using ArdaNova.Domain.Models.Enums;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/epics")]
public class EpicsController : ControllerBase
{
    private readonly IEpicService _epicService;

    public EpicsController(IEpicService epicService)
    {
        _epicService = epicService;
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id, CancellationToken ct)
    {
        var result = await _epicService.GetByIdAsync(id, ct);
        return ToActionResult(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateEpicDto dto, CancellationToken ct)
    {
        var result = await _epicService.CreateAsync(dto, ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value)
            : ToActionResult(result);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateEpicDto dto, CancellationToken ct)
    {
        var result = await _epicService.UpdateAsync(id, dto, ct);
        return ToActionResult(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id, CancellationToken ct)
    {
        var result = await _epicService.DeleteAsync(id, ct);
        return result.IsSuccess ? NoContent() : ToActionResult(result);
    }

    [HttpPut("{id}/assign")]
    public async Task<IActionResult> Assign(string id, [FromBody] AssignEpicDto dto, CancellationToken ct)
    {
        var result = await _epicService.AssignAsync(id, dto.UserId, ct);
        return ToActionResult(result);
    }

    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdateStatus(string id, [FromBody] UpdateEpicStatusDto dto, CancellationToken ct)
    {
        var result = await _epicService.UpdateStatusAsync(id, dto.Status, ct);
        return ToActionResult(result);
    }

    [HttpPut("{id}/priority")]
    public async Task<IActionResult> UpdatePriority(string id, [FromBody] UpdateEpicPriorityDto dto, CancellationToken ct)
    {
        var result = await _epicService.UpdatePriorityAsync(id, dto.Priority, ct);
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

// Helper DTOs for endpoints
public record AssignEpicDto
{
    public string? UserId { get; init; }
}

public record UpdateEpicStatusDto
{
    public EpicStatus Status { get; init; }
}

// Nested controller for project epics
[ApiController]
[Route("api/projects/{projectId}/epics")]
public class ProjectEpicsController : ControllerBase
{
    private readonly IEpicService _epicService;

    public ProjectEpicsController(IEpicService epicService)
    {
        _epicService = epicService;
    }

    [HttpGet]
    public async Task<IActionResult> GetByProjectId(string projectId, CancellationToken ct)
    {
        var result = await _epicService.GetByProjectIdAsync(projectId, ct);
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

// Nested controller for milestone epics
[ApiController]
[Route("api/milestones/{milestoneId}/epics")]
public class MilestoneEpicsController : ControllerBase
{
    private readonly IEpicService _epicService;

    public MilestoneEpicsController(IEpicService epicService)
    {
        _epicService = epicService;
    }

    [HttpGet]
    public async Task<IActionResult> GetByMilestoneId(string milestoneId, CancellationToken ct)
    {
        var result = await _epicService.GetByMilestoneIdAsync(milestoneId, ct);
        return ToActionResult(result);
    }

    [HttpPut("reorder")]
    public async Task<IActionResult> ReorderEpics(string milestoneId, [FromBody] ReorderEpicsDto dto, CancellationToken ct)
    {
        var result = await _epicService.ReorderAsync(milestoneId, dto.EpicIds, ct);
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
