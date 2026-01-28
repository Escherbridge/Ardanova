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
    private readonly ISprintItemService _sprintItemService;

    public SprintsController(ISprintService sprintService, ISprintItemService sprintItemService)
    {
        _sprintService = sprintService;
        _sprintItemService = sprintItemService;
    }

    // ===== PROJECT-BASED ROUTES =====
    [HttpGet("api/projects/{projectId}/sprints")]
    public async Task<IActionResult> GetByProjectId(string projectId, CancellationToken ct)
    {
        var result = await _sprintService.GetByProjectIdAsync(projectId, ct);
        return ToActionResult(result);
    }

    [HttpGet("api/projects/{projectId}/sprints/active")]
    public async Task<IActionResult> GetActiveSprint(string projectId, CancellationToken ct)
    {
        var result = await _sprintService.GetActiveByProjectIdAsync(projectId, ct);
        return ToActionResult(result);
    }

    // ===== SPRINT-BASED ROUTES =====
    [HttpGet("api/sprints/{id}")]
    public async Task<IActionResult> GetById(string id, CancellationToken ct)
    {
        var result = await _sprintService.GetByIdAsync(id, ct);
        return ToActionResult(result);
    }

    [HttpGet("api/sprints/{sprintId}/items")]
    public async Task<IActionResult> GetItems(string sprintId, CancellationToken ct)
    {
        var result = await _sprintItemService.GetBySprintIdAsync(sprintId, ct);
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

    [HttpPut("api/sprints/{sprintId}/items/reorder")]
    public async Task<IActionResult> ReorderItems(string sprintId, [FromBody] ReorderSprintItemsDto dto, CancellationToken ct)
    {
        var result = await _sprintItemService.ReorderAsync(sprintId, dto.ItemIds, ct);
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

public record UpdateSprintStatusDto
{
    public required SprintStatus Status { get; init; }
}

public record ReorderSprintItemsDto
{
    public required IReadOnlyList<string> ItemIds { get; init; }
}
