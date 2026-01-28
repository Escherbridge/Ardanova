namespace ArdaNova.API.Controllers;

using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Interfaces;
using ArdaNova.Domain.Models.Enums;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api")]
public class BacklogItemsController : ControllerBase
{
    private readonly IBacklogItemService _backlogItemService;

    public BacklogItemsController(IBacklogItemService backlogItemService)
    {
        _backlogItemService = backlogItemService;
    }

    [HttpGet("backlog-items/{id}")]
    public async Task<IActionResult> GetById(string id, CancellationToken ct)
    {
        var result = await _backlogItemService.GetByIdAsync(id, ct);
        return ToActionResult(result);
    }

    [HttpGet("projects/{projectId}/backlog-items")]
    public async Task<IActionResult> GetByProjectId(string projectId, CancellationToken ct)
    {
        var result = await _backlogItemService.GetByProjectIdAsync(projectId, ct);
        return ToActionResult(result);
    }

    [HttpPost("backlog-items")]
    public async Task<IActionResult> Create([FromBody] CreateBacklogItemDto dto, CancellationToken ct)
    {
        var result = await _backlogItemService.CreateAsync(dto, ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value)
            : ToActionResult(result);
    }

    [HttpPut("backlog-items/{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateBacklogItemDto dto, CancellationToken ct)
    {
        var result = await _backlogItemService.UpdateAsync(id, dto, ct);
        return ToActionResult(result);
    }

    [HttpDelete("backlog-items/{id}")]
    public async Task<IActionResult> Delete(string id, CancellationToken ct)
    {
        var result = await _backlogItemService.DeleteAsync(id, ct);
        return result.IsSuccess ? NoContent() : ToActionResult(result);
    }

    [HttpPut("backlog-items/{id}/assign")]
    public async Task<IActionResult> Assign(string id, [FromBody] AssignBacklogItemDto dto, CancellationToken ct)
    {
        var result = await _backlogItemService.AssignAsync(id, dto.UserId, ct);
        return ToActionResult(result);
    }

    [HttpPut("backlog-items/{id}/status")]
    public async Task<IActionResult> UpdateStatus(string id, [FromBody] UpdateBacklogStatusDto dto, CancellationToken ct)
    {
        var result = await _backlogItemService.UpdateStatusAsync(id, dto.Status, ct);
        return ToActionResult(result);
    }

    [HttpPut("product-backlog-items/{pbiId}/backlog-items/reorder")]
    public async Task<IActionResult> ReorderItems(string pbiId, [FromBody] ReorderBacklogItemsDto dto, CancellationToken ct)
    {
        var result = await _backlogItemService.ReorderAsync(pbiId, dto.ItemIds, ct);
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

public record AssignBacklogItemDto
{
    public string? UserId { get; init; }
}

public record UpdateBacklogStatusDto
{
    public BacklogStatus Status { get; init; }
}

public record ReorderBacklogItemsDto
{
    public required IReadOnlyList<string> ItemIds { get; init; }
}
