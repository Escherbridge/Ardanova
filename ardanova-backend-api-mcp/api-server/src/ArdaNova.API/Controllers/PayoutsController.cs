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
public class PayoutsController : ControllerBase
{
    private const string PayoutProcessingDisabledMessage =
        "Payout processing is disabled until a verified provider transfer and durable settlement reconciliation are available.";

    private readonly IPayoutService _payoutService;

    public PayoutsController(IPayoutService payoutService)
    {
        _payoutService = payoutService;
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> RequestPayout([FromBody] CreatePayoutRequestDto dto, CancellationToken ct)
    {
        var result = await _payoutService.RequestPayoutAsync(ActorId, dto, ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetMine), null, result.Value)
            : ToActionResult(result);
    }

    [HttpPost("{payoutRequestId}/process")]
    [Authorize(Policy = AuthorizationPolicies.AdminApiKey)]
    public Task<IActionResult> ProcessPayout(string payoutRequestId, CancellationToken ct)
        => Task.FromResult<IActionResult>(StatusCode(StatusCodes.Status503ServiceUnavailable,
            new { error = PayoutProcessingDisabledMessage }));

    [HttpPost("{payoutRequestId}/cancel")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> CancelPayout(string payoutRequestId, CancellationToken ct)
    {
        var mine = await _payoutService.GetPayoutsByUserAsync(ActorId, ct);
        if (!mine.IsSuccess)
            return ToActionResult(mine);
        if (!mine.Value!.Any(payout => string.Equals(payout.Id, payoutRequestId, StringComparison.Ordinal)))
            return Forbid();

        return ToActionResult(await _payoutService.CancelPayoutAsync(payoutRequestId, ct));
    }

    [HttpGet("me")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> GetMine(CancellationToken ct)
    {
        return ToActionResult(await _payoutService.GetPayoutsByUserAsync(ActorId, ct));
    }

    [HttpGet("pending")]
    [Authorize(Policy = AuthorizationPolicies.AdminApiKey)]
    public async Task<IActionResult> GetPendingPayouts(CancellationToken ct)
    {
        return ToActionResult(await _payoutService.GetPendingPayoutsAsync(ct));
    }

    private string ActorId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

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
