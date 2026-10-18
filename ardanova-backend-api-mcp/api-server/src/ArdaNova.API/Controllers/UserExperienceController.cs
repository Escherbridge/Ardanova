namespace ArdaNova.API.Controllers;

using System.Security.Claims;
using ArdaNova.API.Middleware;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

/// <summary>Profile experience entries; writes are owner-only (see Controllers/AGENTS.md §Profile ownership).</summary>
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
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> Create(string userId, [FromBody] CreateUserExperienceDto dto, CancellationToken ct)
    {
        if (!IsActor(userId)) return ForbiddenProfile();
        var createDto = dto with { UserId = userId };
        var result = await _userExperienceService.CreateAsync(createDto, ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetByUserId), new { userId }, result.Value)
            : ToActionResult(result);
    }

    [HttpPut("{experienceId}")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> Update(string userId, string experienceId, [FromBody] UpdateUserExperienceDto dto, CancellationToken ct)
    {
        if (!IsActor(userId)) return ForbiddenProfile();
        var result = await _userExperienceService.UpdateAsync(userId, experienceId, dto, ct);
        return ToActionResult(result);
    }

    [HttpDelete("{experienceId}")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> Delete(string userId, string experienceId, CancellationToken ct)
    {
        if (!IsActor(userId)) return ForbiddenProfile();
        var result = await _userExperienceService.DeleteAsync(userId, experienceId, ct);
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
            ResultType.Forbidden => ForbiddenProfile(),
            _ => BadRequest(new { error = result.Error })
        };
    }

    private ObjectResult ForbiddenProfile()
        => StatusCode(StatusCodes.Status403Forbidden, new { error = "Only the profile owner can change experience entries." });

    private bool IsActor(string userId)
        => string.Equals(User.FindFirstValue(ClaimTypes.NameIdentifier), userId, StringComparison.Ordinal);
}
