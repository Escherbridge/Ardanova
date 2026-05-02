namespace ArdaNova.API.Controllers;

using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Interfaces;
using ArdaNova.Domain.Models.Enums;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class XPEventsController : ControllerBase
{
    private readonly IXPEventService _xpEventService;

    public XPEventsController(IXPEventService xpEventService)
    {
        _xpEventService = xpEventService;
    }

    [HttpGet("user/{userId}/total")]
    public async Task<IActionResult> GetTotalXP(string userId, CancellationToken ct)
    {
        var result = await _xpEventService.GetTotalXPAsync(userId, ct);
        return ToActionResult(result);
    }

    [HttpGet("user/{userId}/history")]
    public async Task<IActionResult> GetHistory(
        string userId,
        [FromQuery] string? eventType = null,
        [FromQuery] int limit = 50,
        [FromQuery] int offset = 0,
        CancellationToken ct = default)
    {
        XPEventType? parsedEventType = null;
        if (!string.IsNullOrEmpty(eventType))
        {
            if (Enum.TryParse<XPEventType>(eventType, true, out var parsed))
                parsedEventType = parsed;
            else
                return BadRequest(new { error = $"Invalid event type: {eventType}" });
        }

        var result = await _xpEventService.GetHistoryAsync(userId, parsedEventType, limit, offset, ct);
        return ToActionResult(result);
    }

    [HttpGet("user/{userId}/by-type/{eventType}")]
    public async Task<IActionResult> GetXPByEventType(string userId, string eventType, CancellationToken ct)
    {
        if (!Enum.TryParse<XPEventType>(eventType, true, out var parsed))
            return BadRequest(new { error = $"Invalid event type: {eventType}" });

        var result = await _xpEventService.GetXPByEventTypeAsync(userId, parsed, ct);
        return ToActionResult(result);
    }

    [HttpGet("user/{userId}/summary")]
    public async Task<IActionResult> GetXPSummary(string userId, CancellationToken ct)
    {
        var result = await _xpEventService.GetXPSummaryAsync(userId, ct);
        return ToActionResult(result);
    }

    [HttpGet("rewards-config")]
    public IActionResult GetRewardsConfig()
    {
        var config = _xpEventService.GetRewardsConfig();
        return Ok(config);
    }

    [HttpGet("level-info/{level:int}")]
    public IActionResult GetLevelInfo(int level)
    {
        if (level < 1)
            return BadRequest(new { error = "Level must be at least 1" });

        var info = _xpEventService.GetLevelInfo(level);
        return Ok(info);
    }

    [HttpPost("award")]
    public async Task<IActionResult> AwardXP([FromBody] AwardXPDto dto, CancellationToken ct)
    {
        var result = await _xpEventService.AwardXPAsync(dto, ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetTotalXP), new { userId = dto.UserId }, result.Value)
            : ToActionResult(result);
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
