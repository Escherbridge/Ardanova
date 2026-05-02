namespace ArdaNova.API.Controllers;

using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class MembershipCredentialsController : ControllerBase
{
    private readonly IMembershipCredentialService _membershipCredentialService;

    public MembershipCredentialsController(IMembershipCredentialService membershipCredentialService)
    {
        _membershipCredentialService = membershipCredentialService;
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id, CancellationToken ct)
    {
        var result = await _membershipCredentialService.GetByIdAsync(id, ct);
        return ToActionResult(result);
    }

    [HttpGet("project/{projectId}")]
    public async Task<IActionResult> GetByProjectId(string projectId, CancellationToken ct)
    {
        var result = await _membershipCredentialService.GetByProjectIdAsync(projectId, ct);
        return ToActionResult(result);
    }

    [HttpGet("project/{projectId}/active")]
    public async Task<IActionResult> GetActiveByProjectId(string projectId, CancellationToken ct)
    {
        var result = await _membershipCredentialService.GetActiveByProjectIdAsync(projectId, ct);
        return ToActionResult(result);
    }

    [HttpGet("user/{userId}")]
    public async Task<IActionResult> GetByUserId(string userId, CancellationToken ct)
    {
        var result = await _membershipCredentialService.GetByUserIdAsync(userId, ct);
        return ToActionResult(result);
    }

    [HttpGet("project/{projectId}/user/{userId}")]
    public async Task<IActionResult> GetByProjectAndUser(string projectId, string userId, CancellationToken ct)
    {
        var result = await _membershipCredentialService.GetByProjectAndUserAsync(projectId, userId, ct);
        return ToActionResult(result);
    }

    [HttpGet("guild/{guildId}")]
    public async Task<IActionResult> GetByGuildId(string guildId, CancellationToken ct)
    {
        var result = await _membershipCredentialService.GetByGuildIdAsync(guildId, ct);
        return ToActionResult(result);
    }

    [HttpGet("guild/{guildId}/active")]
    public async Task<IActionResult> GetActiveByGuildId(string guildId, CancellationToken ct)
    {
        var result = await _membershipCredentialService.GetActiveByGuildIdAsync(guildId, ct);
        return ToActionResult(result);
    }

    [HttpGet("guild/{guildId}/user/{userId}")]
    public async Task<IActionResult> GetByGuildAndUser(string guildId, string userId, CancellationToken ct)
    {
        var result = await _membershipCredentialService.GetByGuildAndUserAsync(guildId, userId, ct);
        return ToActionResult(result);
    }

    [HttpGet("eligibility")]
    public async Task<IActionResult> CheckEligibility([FromQuery] string userId, [FromQuery] string? projectId, [FromQuery] string? guildId, CancellationToken ct)
    {
        var result = await _membershipCredentialService.CheckEligibilityAsync(userId, projectId, guildId, ct);
        return ToActionResult(result);
    }

    [HttpPost]
    public async Task<IActionResult> Grant([FromBody] GrantMembershipCredentialDto dto, CancellationToken ct)
    {
        var result = await _membershipCredentialService.GrantAsync(dto, ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value)
            : ToActionResult(result);
    }

    [HttpPost("{id}/revoke")]
    public async Task<IActionResult> Revoke(string id, [FromBody] RevokeMembershipCredentialDto? dto, CancellationToken ct)
    {
        var result = await _membershipCredentialService.RevokeAsync(id, dto, ct);
        return ToActionResult(result);
    }

    [HttpPost("{id}/suspend")]
    public async Task<IActionResult> Suspend(string id, CancellationToken ct)
    {
        var result = await _membershipCredentialService.SuspendAsync(id, ct);
        return ToActionResult(result);
    }

    [HttpPost("{id}/reactivate")]
    public async Task<IActionResult> Reactivate(string id, CancellationToken ct)
    {
        var result = await _membershipCredentialService.ReactivateAsync(id, ct);
        return ToActionResult(result);
    }

    [HttpPatch("{id}/mint")]
    public async Task<IActionResult> UpdateMintInfo(string id, [FromBody] UpdateMembershipCredentialMintDto dto, CancellationToken ct)
    {
        var result = await _membershipCredentialService.UpdateMintInfoAsync(id, dto, ct);
        return ToActionResult(result);
    }

    [HttpPatch("{id}/tier")]
    public async Task<IActionResult> UpdateTier(string id, [FromBody] UpdateCredentialTierDto dto, CancellationToken ct)
    {
        var result = await _membershipCredentialService.UpdateTierAsync(id, dto, ct);
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
