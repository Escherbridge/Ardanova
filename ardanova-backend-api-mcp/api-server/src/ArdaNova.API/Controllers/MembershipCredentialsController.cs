namespace ArdaNova.API.Controllers;

using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Interfaces;
using Microsoft.AspNetCore.Http;
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
    public IActionResult Grant([FromBody] GrantMembershipCredentialDto dto, CancellationToken ct)
    {
        _ = dto;
        _ = ct;
        return MutationUnavailable();
    }

    [HttpPost("{id}/revoke")]
    public IActionResult Revoke(string id, [FromBody] RevokeMembershipCredentialDto? dto, CancellationToken ct)
    {
        _ = id;
        _ = dto;
        _ = ct;
        return MutationUnavailable();
    }

    [HttpPost("{id}/suspend")]
    public IActionResult Suspend(string id, CancellationToken ct)
    {
        _ = id;
        _ = ct;
        return MutationUnavailable();
    }

    [HttpPost("{id}/reactivate")]
    public IActionResult Reactivate(string id, CancellationToken ct)
    {
        _ = id;
        _ = ct;
        return MutationUnavailable();
    }

    [HttpPatch("{id}/mint")]
    public IActionResult UpdateMintInfo(string id, [FromBody] UpdateMembershipCredentialMintDto dto, CancellationToken ct)
    {
        _ = id;
        _ = dto;
        _ = ct;
        return MutationUnavailable();
    }

    [HttpPatch("{id}/tier")]
    public IActionResult UpdateTier(string id, [FromBody] UpdateCredentialTierDto dto, CancellationToken ct)
    {
        _ = id;
        _ = dto;
        _ = ct;
        return MutationUnavailable();
    }

    private IActionResult MutationUnavailable()
        => Problem(
            statusCode: StatusCodes.Status501NotImplemented,
            title: "Membership credential mutations are unavailable",
            detail: "Actor-bound scope, server-derived grant authority, and an auditable idempotent credential transition are required before this mutation surface can be enabled.");

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
