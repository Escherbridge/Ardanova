namespace ArdaNova.API.Controllers;

using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class TaskEscrowsController : ControllerBase
{
    private readonly ITaskEscrowService _taskEscrowService;

    public TaskEscrowsController(ITaskEscrowService taskEscrowService)
    {
        _taskEscrowService = taskEscrowService;
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await _taskEscrowService.GetByIdAsync(id, ct);
        return ToActionResult(result);
    }

    [HttpGet("task/{taskId:guid}")]
    public async Task<IActionResult> GetByTaskId(Guid taskId, CancellationToken ct)
    {
        var result = await _taskEscrowService.GetByTaskIdAsync(taskId, ct);
        return ToActionResult(result);
    }

    [HttpGet("funder/{funderId:guid}")]
    public async Task<IActionResult> GetByFunderId(Guid funderId, CancellationToken ct)
    {
        var result = await _taskEscrowService.GetByFunderIdAsync(funderId, ct);
        return ToActionResult(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTaskEscrowDto dto, CancellationToken ct)
    {
        var result = await _taskEscrowService.CreateAsync(dto, ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value)
            : ToActionResult(result);
    }

    [HttpPost("{id:guid}/release")]
    public async Task<IActionResult> Release(Guid id, [FromBody] ReleaseEscrowDto dto, CancellationToken ct)
    {
        var result = await _taskEscrowService.ReleaseAsync(id, dto, ct);
        return ToActionResult(result);
    }

    [HttpPost("{id:guid}/dispute")]
    public async Task<IActionResult> Dispute(Guid id, CancellationToken ct)
    {
        var result = await _taskEscrowService.DisputeAsync(id, ct);
        return ToActionResult(result);
    }

    [HttpPost("{id:guid}/refund")]
    public async Task<IActionResult> Refund(Guid id, [FromBody] RefundEscrowDto dto, CancellationToken ct)
    {
        var result = await _taskEscrowService.RefundAsync(id, dto, ct);
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
