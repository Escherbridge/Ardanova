namespace ArdaNova.API.Controllers;

using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class UserStreaksController : ControllerBase
{
    private readonly IUserStreakService _streakService;

    public UserStreaksController(IUserStreakService streakService)
    {
        _streakService = streakService;
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await _streakService.GetByIdAsync(id, ct);
        return ToActionResult(result);
    }

    [HttpGet("user/{userId:guid}")]
    public async Task<IActionResult> GetByUserId(Guid userId, CancellationToken ct)
    {
        var result = await _streakService.GetByUserIdAsync(userId, ct);
        return ToActionResult(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUserStreakDto dto, CancellationToken ct)
    {
        var result = await _streakService.CreateAsync(dto, ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value)
            : ToActionResult(result);
    }

    [HttpPost("user/{userId:guid}/record")]
    public async Task<IActionResult> RecordActivity(Guid userId, CancellationToken ct)
    {
        var result = await _streakService.RecordActivityAsync(userId, ct);
        return ToActionResult(result);
    }

    [HttpPost("user/{userId:guid}/reset")]
    public async Task<IActionResult> ResetStreak(Guid userId, CancellationToken ct)
    {
        var result = await _streakService.ResetStreakAsync(userId, ct);
        return ToActionResult(result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var result = await _streakService.DeleteAsync(id, ct);
        return result.IsSuccess ? NoContent() : ToActionResult(result);
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
