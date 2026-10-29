namespace ArdaNova.API.Controllers;

using System.Security.Claims;
using ArdaNova.API.Middleware;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
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
    /// <remarks>Every refusal carries a money code; only <c>definite</c> lets a caller drop its key. See Controllers/AGENTS.md §Money refusal codes.</remarks>
    [HttpPost("checkout")]
    public async Task<IActionResult> CreateCheckout([FromBody] CreateFundingIntentDto request, CancellationToken ct)
    {
        if (!Request.Headers.TryGetValue(IdempotencyHeader, out var key) || key.Count != 1)
        {
            return BadRequest(MoneyRefusal.ToErrorBody(
                MoneyRefusal.For(MoneyRefusalCodes.Definite, IdempotencyHeader + " is required")));
        }

        var actorId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result = await _fundingIntentService.CreateCheckoutAsync(request, actorId, key[0]!, ct);
        if (result.IsSuccess)
            return Ok(result.Value);

        var body = MoneyRefusal.ToErrorBody(MoneyRefusal.From(result));
        return result.Type == ResultType.Forbidden
            ? StatusCode(StatusCodes.Status403Forbidden, body)
            : RefusalResult(result.Type, body);
    }

    /// <summary>
    /// Voids the actor's own request key, or reveals the sponsorship under it. Only a 200
    /// <c>voided</c> lets a caller drop its key; see Controllers/AGENTS.md §Request-key void.
    /// </summary>
    [HttpPost("requests/{requestKey}/void")]
    public async Task<IActionResult> VoidRequest(string requestKey, CancellationToken ct)
    {
        var actorId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result = await _fundingIntentService.VoidRequestAsync(actorId, requestKey, ct);
        if (result.IsSuccess)
            return Ok(result.Value);

        var body = MoneyRefusal.ToErrorBody(MoneyRefusal.ForFailedVoid(result));
        return result.Type == ResultType.Forbidden
            ? StatusCode(StatusCodes.Status403Forbidden, body)
            : RefusalResult(result.Type, body);
    }

    /// <summary>Reads only the durable funding state owned by the authenticated actor.</summary>
    [HttpGet("{intentId}")]
    public async Task<IActionResult> GetStatus(string intentId, CancellationToken ct)
    {
        var actorId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result = await _fundingIntentService.GetStatusAsync(intentId, actorId, ct);
        if (result.IsSuccess)
            return Ok(result.Value);

        // Another person's sponsorship stays a bare challenge: its existence is not this caller's to learn.
        return result.Type == ResultType.Forbidden
            ? Forbid()
            : RefusalResult(result.Type, MoneyRefusal.ToErrorBody(MoneyRefusal.From(result)));
    }

    private IActionResult RefusalResult(ResultType type, object body) => type switch
    {
        ResultType.NotFound => NotFound(body),
        ResultType.Unauthorized => Unauthorized(body),
        ResultType.Conflict => Conflict(body),
        ResultType.ValidationError or ResultType.BadRequest => BadRequest(body),
        _ => StatusCode(StatusCodes.Status503ServiceUnavailable, body),
    };
}
