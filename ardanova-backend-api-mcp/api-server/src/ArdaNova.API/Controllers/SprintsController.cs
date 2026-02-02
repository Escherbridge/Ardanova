namespace ArdaNova.API.Controllers;

using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Interfaces;
using ArdaNova.Domain.Models.Enums;
using Microsoft.AspNetCore.Mvc;

[ApiController]
public class SprintsController : ControllerBase
{
    private readonly ISprintService _sprintService;

    public SprintsController(ISprintService sprintService)
    {
        _sprintService = sprintService;
    }

    // ===== EPIC-BASED ROUTES =====
    [HttpGet("api/epics/{epicId}/sprints")]
    public async Task<IActionResult> GetByEpicId(string epicId, CancellationToken ct)
    {
        var result = await _sprintService.GetByEpicIdAsync(epicId, ct);
        return ToActionResult(result);
    }

    [HttpGet("api/epics/{epicId}/sprints/active")]
    public async Task<IActionResult> GetActiveSprint(string epicId, CancellationToken ct)
    {
        var result = await _sprintService.GetActiveByEpicIdAsync(epicId, ct);
        return ToActionResult(result);
    }

    // ===== SPRINT-BASED ROUTES =====
    [HttpGet("api/sprints/{id}")]
    public async Task<IActionResult> GetById(string id, CancellationToken ct)
    {
        var result = await _sprintService.GetByIdAsync(id, ct);
        return ToActionResult(result);
    }

    [HttpPost("api/sprints")]
    public async Task<IActionResult> Create([FromBody] CreateSprintDto dto, CancellationToken ct)
    {
        var result = await _sprintService.CreateAsync(dto, ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value)
            : ToActionResult(result);
    }

    [HttpPut("api/sprints/{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateSprintDto dto, CancellationToken ct)
    {
        var result = await _sprintService.UpdateAsync(id, dto, ct);
        return ToActionResult(result);
    }

    [HttpDelete("api/sprints/{id}")]
    public async Task<IActionResult> Delete(string id, CancellationToken ct)
    {
        var result = await _sprintService.DeleteAsync(id, ct);
        return result.IsSuccess ? NoContent() : ToActionResult(result);
    }

    [HttpPost("api/sprints/{id}/start")]
    public async Task<IActionResult> Start(string id, CancellationToken ct)
    {
        var result = await _sprintService.StartAsync(id, ct);
        return ToActionResult(result);
    }

    [HttpPost("api/sprints/{id}/complete")]
    public async Task<IActionResult> Complete(string id, CancellationToken ct)
    {
        var result = await _sprintService.CompleteAsync(id, ct);
        return ToActionResult(result);
    }

    [HttpPost("api/sprints/{id}/cancel")]
    public async Task<IActionResult> Cancel(string id, CancellationToken ct)
    {
        var result = await _sprintService.UpdateStatusAsync(id, SprintStatus.CANCELLED, ct);
        return ToActionResult(result);
    }

    [HttpPut("api/sprints/{id}/status")]
    public async Task<IActionResult> UpdateStatus(string id, [FromBody] UpdateSprintStatusDto dto, CancellationToken ct)
    {
        var result = await _sprintService.UpdateStatusAsync(id, dto.Status, ct);
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

public record UpdateSprintStatusDto
{
    public required SprintStatus Status { get; init; }
}
