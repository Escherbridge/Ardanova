namespace ArdaNova.API.Controllers;

using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/users/{userId}/experience")]
public class UserExperienceController : ControllerBase
{
    private readonly IUserExperienceService _userExperienceService;

    public UserExperienceController(IUserExperienceService userExperienceService)
    {
        _userExperienceService = userExperienceService;
    }

    [HttpGet]
    public async Task<IActionResult> GetByUserId(string userId, CancellationToken ct)
    {
        var result = await _userExperienceService.GetByUserIdAsync(userId, ct);
        return ToActionResult(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create(string userId, [FromBody] CreateUserExperienceDto dto, CancellationToken ct)
    {
        var createDto = dto with { UserId = userId };
        var result = await _userExperienceService.CreateAsync(createDto, ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetByUserId), new { userId }, result.Value)
            : ToActionResult(result);
    }

    [HttpPut("{experienceId}")]
    public async Task<IActionResult> Update(string userId, string experienceId, [FromBody] UpdateUserExperienceDto dto, CancellationToken ct)
    {
        var result = await _userExperienceService.UpdateAsync(experienceId, dto, ct);
        return ToActionResult(result);
    }

    [HttpDelete("{experienceId}")]
    public async Task<IActionResult> Delete(string userId, string experienceId, CancellationToken ct)
    {
        var result = await _userExperienceService.DeleteAsync(experienceId, ct);
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
