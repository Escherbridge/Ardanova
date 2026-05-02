namespace ArdaNova.API.Controllers;

using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/users/{userId}/skills")]
public class UserSkillsController : ControllerBase
{
    private readonly IUserSkillService _userSkillService;

    public UserSkillsController(IUserSkillService userSkillService)
    {
        _userSkillService = userSkillService;
    }

    [HttpGet]
    public async Task<IActionResult> GetByUserId(string userId, CancellationToken ct)
    {
        var result = await _userSkillService.GetByUserIdAsync(userId, ct);
        return ToActionResult(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create(string userId, [FromBody] CreateUserSkillDto dto, CancellationToken ct)
    {
        var createDto = dto with { UserId = userId };
        var result = await _userSkillService.CreateAsync(createDto, ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetByUserId), new { userId }, result.Value)
            : ToActionResult(result);
    }

    [HttpPut("{skillId}")]
    public async Task<IActionResult> Update(string userId, string skillId, [FromBody] UpdateUserSkillDto dto, CancellationToken ct)
    {
        var result = await _userSkillService.UpdateAsync(skillId, dto, ct);
        return ToActionResult(result);
    }

    [HttpDelete("{skillId}")]
    public async Task<IActionResult> Delete(string userId, string skillId, CancellationToken ct)
    {
        var result = await _userSkillService.DeleteAsync(skillId, ct);
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
