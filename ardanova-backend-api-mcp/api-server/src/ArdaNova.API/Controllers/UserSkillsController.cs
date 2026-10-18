namespace ArdaNova.API.Controllers;

using System.Security.Claims;
using ArdaNova.API.Middleware;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

/// <summary>Profile skills; writes are owner-only (see Controllers/AGENTS.md §Profile ownership).</summary>
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
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> Create(string userId, [FromBody] CreateUserSkillDto dto, CancellationToken ct)
    {
        if (!IsActor(userId)) return ForbiddenProfile();
        var createDto = dto with { UserId = userId };
        var result = await _userSkillService.CreateAsync(createDto, ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetByUserId), new { userId }, result.Value)
            : ToActionResult(result);
    }

    [HttpPut("{skillId}")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> Update(string userId, string skillId, [FromBody] UpdateUserSkillDto dto, CancellationToken ct)
    {
        if (!IsActor(userId)) return ForbiddenProfile();
        var result = await _userSkillService.UpdateAsync(userId, skillId, dto, ct);
        return ToActionResult(result);
    }

    [HttpDelete("{skillId}")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> Delete(string userId, string skillId, CancellationToken ct)
    {
        if (!IsActor(userId)) return ForbiddenProfile();
        var result = await _userSkillService.DeleteAsync(userId, skillId, ct);
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
        => StatusCode(StatusCodes.Status403Forbidden, new { error = "Only the profile owner can change skills." });

    private bool IsActor(string userId)
        => string.Equals(User.FindFirstValue(ClaimTypes.NameIdentifier), userId, StringComparison.Ordinal);
}
