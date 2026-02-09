namespace ArdaNova.API.Controllers;

using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class PayoutsController : ControllerBase
{
    private readonly IPayoutService _payoutService;

    public PayoutsController(IPayoutService payoutService)
    {
        _payoutService = payoutService;
    }

    [HttpPost]
    public async Task<IActionResult> RequestPayout([FromQuery] string userId, [FromBody] CreatePayoutRequestDto dto, CancellationToken ct)
    {
        var result = await _payoutService.RequestPayoutAsync(userId, dto, ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetPayoutsByUser), new { userId }, result.Value)
            : ToActionResult(result);
    }

    [HttpPost("{payoutRequestId}/process")]
    public async Task<IActionResult> ProcessPayout(string payoutRequestId, CancellationToken ct)
    {
        var result = await _payoutService.ProcessPayoutAsync(payoutRequestId, ct);
        return ToActionResult(result);
    }

    [HttpPost("{payoutRequestId}/cancel")]
    public async Task<IActionResult> CancelPayout(string payoutRequestId, CancellationToken ct)
    {
        var result = await _payoutService.CancelPayoutAsync(payoutRequestId, ct);
        return ToActionResult(result);
    }

    [HttpGet("by-user/{userId}")]
    public async Task<IActionResult> GetPayoutsByUser(string userId, CancellationToken ct)
    {
        var result = await _payoutService.GetPayoutsByUserAsync(userId, ct);
        return ToActionResult(result);
    }

    [HttpGet("pending")]
    public async Task<IActionResult> GetPendingPayouts(CancellationToken ct)
    {
        var result = await _payoutService.GetPendingPayoutsAsync(ct);
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
