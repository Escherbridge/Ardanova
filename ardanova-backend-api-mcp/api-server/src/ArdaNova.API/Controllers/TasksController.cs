namespace ArdaNova.API.Controllers;

using System.Security.Claims;
using ArdaNova.API.Middleware;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Interfaces;
using ArdaNova.Domain.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class TasksController : ControllerBase
{
    private readonly ITaskService _taskService;
    private readonly IProjectService _projectService;

    public TasksController(ITaskService taskService, IProjectService projectService)
    {
        _taskService = taskService;
        _projectService = projectService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var result = await _taskService.GetAllAsync(ct);
        return ToActionResult(result);
    }

    [HttpGet("paged")]
    public async Task<IActionResult> GetPaged([FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken ct = default)
    {
        var result = await _taskService.GetPagedAsync(page, pageSize, ct);
        return ToActionResult(result);
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search(
        [FromQuery] string? searchTerm,
        [FromQuery] TaskStatus? status,
        [FromQuery] TaskPriority? priority,
        [FromQuery] TaskType? taskType,
        [FromQuery] string? projectId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken ct = default)
    {
        var result = await _taskService.SearchAsync(searchTerm, status, priority, taskType, projectId, page, pageSize, ct);
        return ToActionResult(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id, CancellationToken ct)
    {
        var result = await _taskService.GetByIdAsync(id, ct);
        return ToActionResult(result);
    }

    [HttpGet("me")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> GetMine(CancellationToken ct)
    {
        var result = await _taskService.GetByUserIdAsync(ActorId, ct);
        return ToActionResult(result);
    }

    [HttpGet("project/{projectId}")]
    public async Task<IActionResult> GetByProjectId(string projectId, CancellationToken ct)
    {
        var result = await _taskService.GetByProjectIdAsync(projectId, ct);
        return ToActionResult(result);
    }

    [HttpGet("pbi/{pbiId}")]
    public async Task<IActionResult> GetByPbiId(string pbiId, CancellationToken ct)
    {
        var result = await _taskService.GetByPbiIdAsync(pbiId, ct);
        return ToActionResult(result);
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> Create([FromBody] CreateTaskDto dto, CancellationToken ct)
    {
        if (!await ActorOwnsProjectAsync(dto.ProjectId, ct))
            return Forbid();

        var result = await _taskService.CreateAsync(dto, ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value)
            : ToActionResult(result);
    }

    [HttpPut("{id}")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateTaskDto dto, CancellationToken ct)
    {
        if (!await ActorOwnsTaskProjectAsync(id, ct))
            return Forbid();

        var result = await _taskService.UpdateAsync(id, dto, ct);
        return ToActionResult(result);
    }

    [HttpPatch("{id}/status")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> UpdateStatus(string id, [FromBody] UpdateTaskStatusDto dto, CancellationToken ct)
    {
        if (!await ActorMayUpdateTaskStatusAsync(id, ct))
            return Forbid();

        var result = await _taskService.UpdateStatusAsync(id, dto.Status, ct);
        return ToActionResult(result);
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> Delete(string id, CancellationToken ct)
    {
        if (!await ActorOwnsTaskProjectAsync(id, ct))
            return Forbid();

        var result = await _taskService.DeleteAsync(id, ct);
        return result.IsSuccess ? NoContent() : ToActionResult(result);
    }

    private string ActorId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    private bool IsAdmin => User.IsInRole(UserRole.ADMIN.ToString());

    private async Task<bool> ActorOwnsTaskProjectAsync(string taskId, CancellationToken ct)
    {
        var task = await _taskService.GetByIdAsync(taskId, ct);
        return task.IsSuccess && await ActorOwnsProjectAsync(task.Value!.ProjectId, ct);
    }

    private async Task<bool> ActorMayUpdateTaskStatusAsync(string taskId, CancellationToken ct)
    {
        var task = await _taskService.GetByIdAsync(taskId, ct);
        if (!task.IsSuccess)
            return false;

        return string.Equals(task.Value!.AssignedToId, ActorId, StringComparison.Ordinal)
            || await ActorOwnsProjectAsync(task.Value.ProjectId, ct);
    }

    private async Task<bool> ActorOwnsProjectAsync(string projectId, CancellationToken ct)
    {
        if (IsAdmin)
            return true;

        var project = await _projectService.GetByIdAsync(projectId, ct);
        return project.IsSuccess && string.Equals(project.Value!.CreatedById, ActorId, StringComparison.Ordinal);
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
