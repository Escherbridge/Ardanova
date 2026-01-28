namespace ArdaNova.API.Controllers;

using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Interfaces;
using ArdaNova.Domain.Models.Enums;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api")]
public class ProductBacklogItemsController : ControllerBase
{
    private readonly IProductBacklogItemService _pbiService;

    public ProductBacklogItemsController(IProductBacklogItemService pbiService)
    {
        _pbiService = pbiService;
    }

    [HttpGet("epics/{epicId}/product-backlog-items")]
    public async Task<IActionResult> GetByEpicId(string epicId, CancellationToken ct)
    {
        var result = await _pbiService.GetByProjectIdAsync(epicId, ct);
        return ToActionResult(result);
    }

    [HttpGet("product-backlog-items/{id}")]
    public async Task<IActionResult> GetById(string id, CancellationToken ct)
    {
        var result = await _pbiService.GetByIdAsync(id, ct);
        return ToActionResult(result);
    }

    [HttpGet("projects/{projectId}/product-backlog-items")]
    public async Task<IActionResult> GetByProjectId(string projectId, CancellationToken ct)
    {
        var result = await _pbiService.GetByProjectIdAsync(projectId, ct);
        return ToActionResult(result);
    }

    [HttpPost("product-backlog-items")]
    public async Task<IActionResult> Create([FromBody] CreateProductBacklogItemDto dto, CancellationToken ct)
    {
        var result = await _pbiService.CreateAsync(dto, ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value)
            : ToActionResult(result);
    }

    [HttpPut("product-backlog-items/{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateProductBacklogItemDto dto, CancellationToken ct)
    {
        var result = await _pbiService.UpdateAsync(id, dto, ct);
        return ToActionResult(result);
    }

    [HttpDelete("product-backlog-items/{id}")]
    public async Task<IActionResult> Delete(string id, CancellationToken ct)
    {
        var result = await _pbiService.DeleteAsync(id, ct);
        return result.IsSuccess ? NoContent() : ToActionResult(result);
    }

    [HttpPut("product-backlog-items/{id}/assign")]
    public async Task<IActionResult> Assign(string id, [FromBody] AssignDto dto, CancellationToken ct)
    {
        var result = await _pbiService.AssignAsync(id, dto.UserId, ct);
        return ToActionResult(result);
    }

    [HttpPut("product-backlog-items/{id}/status")]
    public async Task<IActionResult> UpdateStatus(string id, [FromBody] UpdatePBIStatusDto dto, CancellationToken ct)
    {
        var result = await _pbiService.UpdateStatusAsync(id, dto.Status, ct);
        return ToActionResult(result);
    }

    [HttpPut("product-backlog-items/{id}/priority")]
    public async Task<IActionResult> UpdatePriority(string id, [FromBody] UpdatePriorityDto dto, CancellationToken ct)
    {
        var updateDto = new UpdateProductBacklogItemDto { Priority = dto.Priority };
        var result = await _pbiService.UpdateAsync(id, updateDto, ct);
        return ToActionResult(result);
    }

    [HttpPut("epics/{epicId}/product-backlog-items/reorder")]
    public async Task<IActionResult> ReorderPbis(string epicId, [FromBody] ReorderDto dto, CancellationToken ct)
    {
        var result = await _pbiService.ReorderAsync(epicId, dto.ItemIds, ct);
        return result.IsSuccess ? NoContent() : ToActionResult(result);
    }

    [HttpGet("product-backlog-items/{pbiId}/backlog-items")]
    public async Task<IActionResult> GetBacklogItems(string pbiId, CancellationToken ct)
    {
        var result = await _pbiService.GetByIdAsync(pbiId, ct);
        if (!result.IsSuccess)
            return ToActionResult(result);

        return Ok(result.Value!.BacklogItems ?? new List<BacklogItemDto>());
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

public record AssignDto
{
    public string? UserId { get; init; }
}

public record UpdatePBIStatusDto
{
    public PBIStatus Status { get; init; }
}

public record UpdatePriorityDto
{
    public TaskPriority Priority { get; init; }
}

public record ReorderDto
{
    public required IReadOnlyList<string> ItemIds { get; init; }
}
