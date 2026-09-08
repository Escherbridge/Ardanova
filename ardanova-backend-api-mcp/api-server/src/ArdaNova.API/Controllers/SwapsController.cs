namespace ArdaNova.API.Controllers;

using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Interfaces;
using ArdaNova.API.Middleware;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// Trading one project's units directly for another's - a legacy surface, not the way out.
/// </summary>
/// <remarks>
/// <para>
/// The path a person actually takes to turn units into money is <c>PayoutsController</c>: sell
/// units for platform funds, then send those funds to a bank. This controller is the older
/// unit-for-unit exchange, kept for its preview and its history.
/// </para>
/// <para>
/// <b>Executing one stays refused, and the refusal lives in the service.</b> The controller used
/// to hard-code its own 503 beside a service that also refused, which is two places to change and
/// one of them can be forgotten. It now carries whatever <see cref="ISwapService"/> answers, so
/// the day a unit-for-unit swap becomes possible there is exactly one line to change.
/// </para>
/// </remarks>
[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
public class SwapsController : ControllerBase
{
    private readonly ISwapService _swapService;

    public SwapsController(ISwapService swapService)
    {
        _swapService = swapService;
    }

    /// <summary>What one project's units would be worth in another's, priced through platform funds.</summary>
    [HttpGet("preview")]
    public async Task<IActionResult> GetPreview(
        [FromQuery] string sourceConfigId,
        [FromQuery] string targetConfigId,
        [FromQuery] int sourceTokenAmount,
        CancellationToken ct)
    {
        var result = await _swapService.GetSwapPreviewAsync(
            ActorId, sourceConfigId, targetConfigId, sourceTokenAmount, ct);

        return ToActionResult(result);
    }

    /// <summary>Trades one project's units for another's, if the service allows it.</summary>
    /// <remarks>
    /// A plain refusal here means "not available", which is a 503 rather than a 400: nothing the
    /// caller sent was wrong, and telling them it was sends them to fix a request that was fine.
    /// </remarks>
    [HttpPost]
    public async Task<IActionResult> ExecuteSwap([FromBody] SwapRequestDto dto, CancellationToken ct)
    {
        var result = await _swapService.ExecuteSwapAsync(ActorId, dto, ct);
        if (result.Type == ResultType.Failure)
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { error = result.Error });

        return ToActionResult(result);
    }

    /// <summary>This person's own unit-for-unit exchange records.</summary>
    [HttpGet("history")]
    public async Task<IActionResult> GetHistory(CancellationToken ct)
    {
        var result = await _swapService.GetSwapHistoryAsync(ActorId, ct);
        return ToActionResult(result);
    }

    private string ActorId => User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value;

    /// <summary>Carries a service answer to a status code WITH its wording.</summary>
    /// <remarks>
    /// Forbidden becomes a 403 carrying its message rather than <c>Forbid()</c>, which returns an
    /// empty body and turns an explanation a person can act on into a blank wall.
    /// </remarks>
    private IActionResult ToActionResult<T>(Result<T> result)
    {
        if (result.IsSuccess)
            return Ok(result.Value);

        return result.Type switch
        {
            ResultType.NotFound => NotFound(new { error = result.Error }),
            ResultType.ValidationError => BadRequest(new { error = result.Error }),
            ResultType.Unauthorized => Unauthorized(new { error = result.Error }),
            ResultType.Forbidden => StatusCode(StatusCodes.Status403Forbidden, new { error = result.Error }),
            ResultType.Conflict => Conflict(new { error = result.Error }),
            _ => BadRequest(new { error = result.Error })
        };
    }
}
