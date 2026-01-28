namespace ArdaNova.API.Controllers;

using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Interfaces;
using ArdaNova.Domain.Models.Enums;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/task-bids")]
public class TaskBidsController : ControllerBase
{
    private readonly ITaskBidService _taskBidService;

    public TaskBidsController(ITaskBidService taskBidService)
    {
        _taskBidService = taskBidService;
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search(
        [FromQuery] string? taskId,
        [FromQuery] string? guildId,
        [FromQuery] TaskBidStatus? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken ct = default)
    {
        var result = await _taskBidService.SearchAsync(taskId, guildId, status, page, pageSize, ct);
        return ToActionResult(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id, CancellationToken ct)
    {
        var result = await _taskBidService.GetByIdAsync(id, ct);
        return ToActionResult(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTaskBidDto dto, CancellationToken ct)
    {
        var result = await _taskBidService.CreateAsync(dto, ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value)
            : ToActionResult(result);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateTaskBidDto dto, CancellationToken ct)
    {
        var result = await _taskBidService.UpdateAsync(id, dto, ct);
        return ToActionResult(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id, CancellationToken ct)
    {
        var result = await _taskBidService.DeleteAsync(id, ct);
        return result.IsSuccess ? NoContent() : ToActionResult(result);
    }

    [HttpPost("{id}/accept")]
    public async Task<IActionResult> Accept(string id, CancellationToken ct)
    {
        var result = await _taskBidService.AcceptAsync(id, ct);
        return ToActionResult(result);
    }

    [HttpPost("{id}/reject")]
    public async Task<IActionResult> Reject(string id, CancellationToken ct)
    {
        var result = await _taskBidService.RejectAsync(id, ct);
        return ToActionResult(result);
    }

    [HttpPost("{id}/withdraw")]
    public async Task<IActionResult> Withdraw(string id, CancellationToken ct)
    {
        var result = await _taskBidService.WithdrawAsync(id, ct);
        return ToActionResult(result);
    }

    [HttpPost("{id}/complete")]
    public async Task<IActionResult> Complete(string id, CancellationToken ct)
    {
        var result = await _taskBidService.CompleteAsync(id, ct);
        return ToActionResult(result);
    }

    [HttpPost("{id}/review")]
    public async Task<IActionResult> Review(string id, CancellationToken ct)
    {
        var result = await _taskBidService.ReviewAsync(id, ct);
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

// Additional route mappings for nested resources
[ApiController]
[Route("api/tasks/{taskId}/bids")]
public class TaskBidsNestedController : ControllerBase
{
    private readonly ITaskBidService _taskBidService;

    public TaskBidsNestedController(ITaskBidService taskBidService)
    {
        _taskBidService = taskBidService;
    }

    [HttpGet]
    public async Task<IActionResult> GetByTaskId(string taskId, CancellationToken ct)
    {
        var result = await _taskBidService.GetByTaskIdAsync(taskId, ct);
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

[ApiController]
[Route("api/guilds/{guildId}/task-bids")]
public class GuildTaskBidsController : ControllerBase
{
    private readonly ITaskBidService _taskBidService;

    public GuildTaskBidsController(ITaskBidService taskBidService)
    {
        _taskBidService = taskBidService;
    }

    [HttpGet]
    public async Task<IActionResult> GetByGuildId(string guildId, CancellationToken ct)
    {
        var result = await _taskBidService.GetByGuildIdAsync(guildId, ct);
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
