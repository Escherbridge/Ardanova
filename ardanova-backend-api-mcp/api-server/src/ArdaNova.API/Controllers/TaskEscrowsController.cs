namespace ArdaNova.API.Controllers;

using System.Security.Claims;
using ArdaNova.API.Middleware;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
public class TaskEscrowsController : ControllerBase
{
    private readonly ITaskEscrowService _taskEscrowService;

    public TaskEscrowsController(ITaskEscrowService taskEscrowService)
    {
        _taskEscrowService = taskEscrowService;
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id, CancellationToken ct)
    {
        var result = await _taskEscrowService.GetByIdAsync(id, ct);
        return IsActorFunder(result) ? ToActionResult(result) : Forbid();
    }

    [HttpGet("task/{taskId}")]
    public async Task<IActionResult> GetByTaskId(string taskId, CancellationToken ct)
    {
        var result = await _taskEscrowService.GetByTaskIdAsync(taskId, ct);
        return IsActorFunder(result) ? ToActionResult(result) : Forbid();
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetMine(CancellationToken ct)
    {
        return ToActionResult(await _taskEscrowService.GetByFunderIdAsync(ActorId, ct));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTaskEscrowRequest dto, CancellationToken ct)
    {
        var result = await _taskEscrowService.CreateAsync(new CreateTaskEscrowDto
        {
            TaskId = dto.TaskId,
            FunderId = ActorId,
            ShareId = dto.ShareId,
            Amount = dto.Amount,
            TxHashFund = dto.TxHashFund
        }, ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value)
            : ToActionResult(result);
    }

    [HttpPost("{id}/release")]
    public async Task<IActionResult> Release(string id, [FromBody] ReleaseEscrowDto dto, CancellationToken ct)
    {
        if (!await ActorFundsEscrowAsync(id, ct))
            return Forbid();

        return ToActionResult(await _taskEscrowService.ReleaseAsync(id, dto, ct));
    }

    [HttpPost("{id}/dispute")]
    public async Task<IActionResult> Dispute(string id, CancellationToken ct)
    {
        if (!await ActorFundsEscrowAsync(id, ct))
            return Forbid();

        return ToActionResult(await _taskEscrowService.DisputeAsync(id, ct));
    }

    [HttpPost("{id}/refund")]
    public async Task<IActionResult> Refund(string id, [FromBody] RefundEscrowDto dto, CancellationToken ct)
    {
        if (!await ActorFundsEscrowAsync(id, ct))
            return Forbid();

        return ToActionResult(await _taskEscrowService.RefundAsync(id, dto, ct));
    }

    private string ActorId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    private async Task<bool> ActorFundsEscrowAsync(string id, CancellationToken ct)
        => IsActorFunder(await _taskEscrowService.GetByIdAsync(id, ct));

    private bool IsActorFunder(Result<TaskEscrowDto> result)
        => result.IsSuccess && string.Equals(result.Value!.FunderId, ActorId, StringComparison.Ordinal);

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

    public sealed record CreateTaskEscrowRequest(
        string TaskId,
        string ShareId,
        decimal Amount,
        string? TxHashFund);
}
