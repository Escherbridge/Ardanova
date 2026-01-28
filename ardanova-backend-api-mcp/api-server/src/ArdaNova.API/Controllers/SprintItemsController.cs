namespace ArdaNova.API.Controllers;

using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/sprint-items")]
public class SprintItemsController : ControllerBase
{
    private readonly ISprintItemService _sprintItemService;
    private readonly ITaskService _taskService;

    public SprintItemsController(ISprintItemService sprintItemService, ITaskService taskService)
    {
        _sprintItemService = sprintItemService;
        _taskService = taskService;
    }

    [HttpGet("{itemId}")]
    public async Task<IActionResult> GetById(string itemId, CancellationToken ct)
    {
        var result = await _sprintItemService.GetByIdAsync(itemId, ct);
        return ToActionResult(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] AddSprintItemDto dto, CancellationToken ct)
    {
        var result = await _sprintItemService.AddTaskToSprintAsync(dto, ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { itemId = result.Value!.Id }, result.Value)
            : ToActionResult(result);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateSprintItemDto dto, CancellationToken ct)
    {
        // Sprint items don't have a direct update method in the service interface
        // Updates should be done on the task itself or by removing/re-adding
        return BadRequest(new { error = "To update a sprint item, update the associated task or remove and re-add the item" });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id, CancellationToken ct)
    {
        var result = await _sprintItemService.RemoveTaskFromSprintAsync(id, ct);
        return result.IsSuccess ? NoContent() : ToActionResult(result);
    }

    [HttpPut("{id}/assign")]
    public async Task<IActionResult> Assign(string id, [FromBody] AssignSprintItemDto dto, CancellationToken ct)
    {
        // Get the sprint item to find the associated task
        var itemResult = await _sprintItemService.GetByIdAsync(id, ct);
        if (!itemResult.IsSuccess)
            return ToActionResult(itemResult);

        // Assign the task using the task service
        var taskId = itemResult.Value!.TaskId;
        var updateDto = new UpdateTaskDto { AssignedToId = dto.UserId };
        var result = await _taskService.UpdateAsync(taskId, updateDto, ct);
        return ToActionResult(result);
    }

    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdateStatus(string id, [FromBody] UpdateSprintItemStatusDto dto, CancellationToken ct)
    {
        // Get the sprint item to find the associated task
        var itemResult = await _sprintItemService.GetByIdAsync(id, ct);
        if (!itemResult.IsSuccess)
            return ToActionResult(itemResult);

        // Update the task status using the task service
        var taskId = itemResult.Value!.TaskId;
        var result = await _taskService.UpdateStatusAsync(taskId, dto.Status, ct);
        return ToActionResult(result);
    }

    [HttpPut("{id}/log-hours")]
    public async Task<IActionResult> LogHours(string id, [FromBody] LogHoursDto dto, CancellationToken ct)
    {
        // Get the sprint item to find the associated task
        var itemResult = await _sprintItemService.GetByIdAsync(id, ct);
        if (!itemResult.IsSuccess)
            return ToActionResult(itemResult);

        // Update the task with logged hours using the task service
        var taskId = itemResult.Value!.TaskId;
        var taskDto = new UpdateTaskDto
        {
            ActualHours = (int?)dto.Hours,
            Status = dto.Status
        };
        var result = await _taskService.UpdateAsync(taskId, taskDto, ct);
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

public record UpdateSprintItemDto
{
    public int? Order { get; init; }
}

public record AssignSprintItemDto
{
    public string? UserId { get; init; }
}

public record UpdateSprintItemStatusDto
{
    public required ArdaNova.Domain.Models.Enums.TaskStatus Status { get; init; }
}

public record LogHoursDto
{
    public decimal Hours { get; init; }
    public ArdaNova.Domain.Models.Enums.TaskStatus? Status { get; init; }
}
