namespace ArdaNova.API.Controllers;

using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Interfaces;
using ArdaNova.Domain.Models.Enums;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class AchievementsController : ControllerBase
{
    private readonly IAchievementService _achievementService;

    public AchievementsController(IAchievementService achievementService)
    {
        _achievementService = achievementService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var result = await _achievementService.GetAllAsync(ct);
        return ToActionResult(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id, CancellationToken ct)
    {
        var result = await _achievementService.GetByIdAsync(id, ct);
        return ToActionResult(result);
    }

    [HttpGet("category/{category}")]
    public async Task<IActionResult> GetByCategory(string category, CancellationToken ct)
    {
        if (!Enum.TryParse<AchievementCategory>(category, true, out var parsed))
            return BadRequest(new { error = $"Invalid category: {category}" });

        var result = await _achievementService.GetByCategoryAsync(parsed, ct);
        return ToActionResult(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateAchievementDto dto, CancellationToken ct)
    {
        var result = await _achievementService.CreateAsync(dto, ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value)
            : ToActionResult(result);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateAchievementDto dto, CancellationToken ct)
    {
        var result = await _achievementService.UpdateAsync(id, dto, ct);
        return ToActionResult(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id, CancellationToken ct)
    {
        var result = await _achievementService.DeleteAsync(id, ct);
        return ToActionResult(result);
    }

    [HttpGet("user/{userId}")]
    public async Task<IActionResult> GetUserAchievements(string userId, CancellationToken ct)
    {
        var result = await _achievementService.GetUserAchievementsAsync(userId, ct);
        return ToActionResult(result);
    }

    [HttpPut("user/{userId}/{achievementId}/progress")]
    public async Task<IActionResult> UpdateProgress(
        string userId, string achievementId, [FromBody] UpdateProgressDto dto, CancellationToken ct)
    {
        var result = await _achievementService.UpdateProgressAsync(userId, achievementId, dto, ct);
        return ToActionResult(result);
    }

    [HttpPost("user/{userId}/{achievementId}/award")]
    public async Task<IActionResult> AwardAchievement(
        string userId, string achievementId, CancellationToken ct)
    {
        var result = await _achievementService.AwardAchievementAsync(userId, achievementId, ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetUserAchievements), new { userId }, result.Value)
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
