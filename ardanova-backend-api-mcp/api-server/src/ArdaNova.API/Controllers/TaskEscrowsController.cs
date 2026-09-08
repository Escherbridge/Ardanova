namespace ArdaNova.API.Controllers;

using System.Security.Claims;
using ArdaNova.API.Middleware;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
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

    // No class-level [Authorize] here: this controller mixes the funder's own self-service
    // actions (gated on ActorAssertion, the signed per-request identity from the Next.js BFF)
    // with an administrator's dispute resolution (gated on AdminApiKey, the API-key role).
    // Stacking a class-level policy with a method-level one is an AND, not an override, so
    // Resolve would otherwise need an impossible identity that is both at once — see
    // PayoutsController for the same split.

    [HttpGet("{id}")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> GetById(string id, CancellationToken ct)
    {
        var result = await _taskEscrowService.GetByIdAsync(id, ct);
        return IsActorFunder(result) ? ToActionResult(result) : Forbid();
    }

    [HttpGet("task/{taskId}")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> GetByTaskId(string taskId, CancellationToken ct)
    {
        var result = await _taskEscrowService.GetByTaskIdAsync(taskId, ct);
        return IsActorFunder(result) ? ToActionResult(result) : Forbid();
    }

    [HttpGet("me")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> GetMine(CancellationToken ct)
    {
        return ToActionResult(await _taskEscrowService.GetByFunderIdAsync(ActorId, ct));
    }

    /// <summary>The admin adjudication queue: every disputed escrow, across every funder.</summary>
    [HttpGet("disputed")]
    [Authorize(Policy = AuthorizationPolicies.AdminApiKey)]
    public async Task<IActionResult> GetDisputed(CancellationToken ct)
    {
        return ToActionResult(await _taskEscrowService.GetDisputedAsync(ct));
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public IActionResult Create([FromBody] CreateTaskEscrowRequest dto)
    {
        _ = dto;
        return StatusCode(StatusCodes.Status501NotImplemented, new
        {
            error = "Direct escrow creation is disabled until task, funding instrument, amount, and provider settlement are verified server-side."
        });
    }

    /// <summary>
    /// Authorizes release. Nothing in the request body moves the hold — the caller supplies no
    /// chain evidence at all; the escrow's own funder identity (never a request field) is the
    /// only input, and the resulting hold state comes back from the node itself.
    /// </summary>
    [HttpPost("{id}/release")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> Release(string id, [FromBody] ReleaseEscrowRequest _, CancellationToken ct)
    {
        if (!await ActorFundsEscrowAsync(id, ct))
            return Forbid();

        return ToActionResult(await _taskEscrowService.ReleaseAsync(id, new ReleaseEscrowDto
        {
            ReleasedByUserId = ActorId
        }, ct));
    }

    [HttpPost("{id}/dispute")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> Dispute(string id, [FromBody] DisputeEscrowRequest dto, CancellationToken ct)
    {
        if (!await ActorFundsEscrowAsync(id, ct))
            return Forbid();

        return ToActionResult(await _taskEscrowService.DisputeAsync(id, new DisputeEscrowDto
        {
            Reason = dto.Reason,
            Description = dto.Description,
            DisputedByUserId = ActorId
        }, ct));
    }

    /// <summary>The funder's own refund request. A free-text reason is an audit note, never a chain artifact.</summary>
    [HttpPost("{id}/refund")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> Refund(string id, [FromBody] RefundEscrowRequest dto, CancellationToken ct)
    {
        if (!await ActorFundsEscrowAsync(id, ct))
            return Forbid();

        return ToActionResult(await _taskEscrowService.RefundAsync(id, new RefundEscrowDto
        {
            RefundedByUserId = ActorId,
            Reason = dto.Reason
        }, ct));
    }

    /// <summary>
    /// Administrator adjudication of a disputed escrow. REFUND returns the held units to the
    /// contributor partition; REJECT dismisses the dispute and restores FUNDED so the funder's
    /// own release action pays the worker next.
    /// </summary>
    /// <remarks>
    /// Gated on the admin API-key policy alone — this is the same service credential
    /// <c>PayoutsController.ProcessPayout</c> requires, issued only to the trusted Next.js
    /// server, never to a browser. That credential carries no per-user identity (unlike the
    /// ActorAssertion the other actions on this controller rely on), so the resolving
    /// administrator's id travels as an explicit request field, populated server-side by the
    /// Next.js admin procedure from its own verified session — never typed by a form.
    /// </remarks>
    [HttpPost("{id}/resolve")]
    [Authorize(Policy = AuthorizationPolicies.AdminApiKey)]
    public async Task<IActionResult> Resolve(string id, [FromBody] ResolveDisputeRequest dto, CancellationToken ct)
    {
        return ToActionResult(await _taskEscrowService.ResolveAsync(id, new ResolveDisputeDto
        {
            Outcome = dto.Outcome,
            ResolvedByUserId = dto.ResolvedByUserId
        }, ct));
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
        decimal Amount);

    /// <summary>Empty on purpose: release evidence is the hold's own confirmed state, not a form field.</summary>
    public sealed record ReleaseEscrowRequest;

    public sealed record DisputeEscrowRequest(string Reason, string Description);

    public sealed record RefundEscrowRequest(string? Reason);

    public sealed record ResolveDisputeRequest(string Outcome, string ResolvedByUserId);
}
