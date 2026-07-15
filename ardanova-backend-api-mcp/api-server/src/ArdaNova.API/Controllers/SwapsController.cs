namespace ArdaNova.API.Controllers;

using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Interfaces;
using ArdaNova.API.Middleware;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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

    /// <summary>
    /// Get a swap preview (conversion chain: sourceTokens → ARDA → targetTokens).
    /// </summary>
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

    /// <summary>
    /// Execute a token swap.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> ExecuteSwap(
        [FromBody] SwapRequestDto dto,
        CancellationToken ct)
    {
        var result = await _swapService.ExecuteSwapAsync(ActorId, dto, ct);
        return ToActionResult(result);
    }

    /// <summary>
    /// Get swap history for a user.
    /// </summary>
    [HttpGet("history")]
    public async Task<IActionResult> GetHistory(CancellationToken ct)
    {
        var result = await _swapService.GetSwapHistoryAsync(ActorId, ct);
        return ToActionResult(result);
    }

    private string ActorId => User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value;

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
