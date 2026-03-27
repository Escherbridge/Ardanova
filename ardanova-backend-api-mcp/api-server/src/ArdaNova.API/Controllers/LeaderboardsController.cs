namespace ArdaNova.API.Controllers;

using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Interfaces;
using ArdaNova.Domain.Models.Enums;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class LeaderboardsController : ControllerBase
{
    private readonly ILeaderboardService _leaderboardService;

    public LeaderboardsController(ILeaderboardService leaderboardService)
    {
        _leaderboardService = leaderboardService;
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id, CancellationToken ct)
    {
        var result = await _leaderboardService.GetByIdAsync(id, ct);
        return ToActionResult(result);
    }

    [HttpGet("period/{period}")]
    public async Task<IActionResult> GetByPeriod(string period, CancellationToken ct)
    {
        if (!Enum.TryParse<LeaderboardPeriod>(period, true, out var parsed))
            return BadRequest(new { error = $"Invalid period: {period}" });

        var result = await _leaderboardService.GetByPeriodAsync(parsed, ct);
        return ToActionResult(result);
    }

    [HttpGet("category/{category}")]
    public async Task<IActionResult> GetByCategory(string category, CancellationToken ct)
    {
        if (!Enum.TryParse<LeaderboardCategory>(category, true, out var parsed))
            return BadRequest(new { error = $"Invalid category: {category}" });

        var result = await _leaderboardService.GetByCategoryAsync(parsed, ct);
        return ToActionResult(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateLeaderboardDto dto, CancellationToken ct)
    {
        var result = await _leaderboardService.CreateAsync(dto, ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value)
            : ToActionResult(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id, CancellationToken ct)
    {
        var result = await _leaderboardService.DeleteAsync(id, ct);
        return ToActionResult(result);
    }

    [HttpGet("{id}/entries")]
    public async Task<IActionResult> GetEntries(string id, CancellationToken ct)
    {
        var result = await _leaderboardService.GetEntriesAsync(id, ct);
        return ToActionResult(result);
    }

    [HttpPost("entries")]
    public async Task<IActionResult> AddEntry([FromBody] CreateLeaderboardEntryDto dto, CancellationToken ct)
    {
        var result = await _leaderboardService.AddEntryAsync(dto, ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetEntries), new { id = result.Value!.LeaderboardId }, result.Value)
            : ToActionResult(result);
    }

    [HttpPut("entries/{entryId}")]
    public async Task<IActionResult> UpdateEntry(string entryId, [FromBody] UpdateLeaderboardEntryDto dto, CancellationToken ct)
    {
        var result = await _leaderboardService.UpdateEntryAsync(entryId, dto, ct);
        return ToActionResult(result);
    }

    [HttpDelete("entries/{entryId}")]
    public async Task<IActionResult> DeleteEntry(string entryId, CancellationToken ct)
    {
        var result = await _leaderboardService.DeleteEntryAsync(entryId, ct);
        return ToActionResult(result);
    }

    [HttpGet("user/{userId}/rankings")]
    public async Task<IActionResult> GetUserRankings(string userId, CancellationToken ct)
    {
        var result = await _leaderboardService.GetUserRankingsAsync(userId, ct);
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
