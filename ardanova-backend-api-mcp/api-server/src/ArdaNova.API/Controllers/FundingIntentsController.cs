namespace ArdaNova.API.Controllers;

using System.Security.Claims;
using ArdaNova.API.Middleware;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

/// <summary>Actor-scoped funding intent checkout and durable-status endpoints.</summary>
[ApiController]
[Route("api/funding-intents")]
[Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
public sealed class FundingIntentsController : ControllerBase
{
    private const string IdempotencyHeader = "X-Idempotency-Key";
    private readonly IFundingIntentService _fundingIntentService;

    public FundingIntentsController(IFundingIntentService fundingIntentService)
    {
        _fundingIntentService = fundingIntentService;
    }

    /// <summary>Creates or replays an actor-owned checkout from a durable funding intent.</summary>
    [HttpPost("checkout")]
    public async Task<IActionResult> CreateCheckout([FromBody] CreateFundingIntentDto request, CancellationToken ct)
    {
        if (!Request.Headers.TryGetValue(IdempotencyHeader, out var key) || key.Count != 1)
            return BadRequest(new { error = "X-Idempotency-Key is required" });

        var actorId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result = await _fundingIntentService.CreateCheckoutAsync(request, actorId, key[0]!, ct);
        return ToActionResult(result);
    }

    /// <summary>Reads only the durable funding state owned by the authenticated actor.</summary>
    [HttpGet("{intentId}")]
    public async Task<IActionResult> GetStatus(string intentId, CancellationToken ct)
    {
        var actorId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result = await _fundingIntentService.GetStatusAsync(intentId, actorId, ct);
        return ToActionResult(result);
    }

    private IActionResult ToActionResult<T>(Result<T> result)
        => result.IsSuccess
            ? Ok(result.Value)
            : result.Type switch
            {
                ResultType.NotFound => NotFound(new { error = result.Error }),
                ResultType.Unauthorized => Unauthorized(new { error = result.Error }),
                ResultType.Forbidden => Forbid(),
                ResultType.Conflict => Conflict(new { error = result.Error }),
                _ => BadRequest(new { error = result.Error }),
            };
}
