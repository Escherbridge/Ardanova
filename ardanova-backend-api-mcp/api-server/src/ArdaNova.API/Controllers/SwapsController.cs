namespace ArdaNova.API.Controllers;

using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

// TODO: Once JWT forwarding from Next.js is configured, uncomment [Authorize]
// and extract userId from HttpContext.User claims instead of query params.
[ApiController]
[Route("api/[controller]")]
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
        [FromQuery] string userId,
        [FromQuery] string sourceConfigId,
        [FromQuery] string targetConfigId,
        [FromQuery] int sourceTokenAmount,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return BadRequest(new { error = "userId is required" });

        var result = await _swapService.GetSwapPreviewAsync(
            userId, sourceConfigId, targetConfigId, sourceTokenAmount, ct);

        return ToActionResult(result);
    }

    /// <summary>
    /// Execute a token swap.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> ExecuteSwap(
        [FromQuery] string userId,
        [FromBody] SwapRequestDto dto,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return BadRequest(new { error = "userId is required" });

        var result = await _swapService.ExecuteSwapAsync(userId, dto, ct);
        return ToActionResult(result);
    }

    /// <summary>
    /// Get swap history for a user.
    /// </summary>
    [HttpGet("history/{userId}")]
    public async Task<IActionResult> GetHistory(string userId, CancellationToken ct)
    {
        var result = await _swapService.GetSwapHistoryAsync(userId, ct);
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
