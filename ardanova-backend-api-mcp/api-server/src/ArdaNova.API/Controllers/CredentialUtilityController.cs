namespace ArdaNova.API.Controllers;

using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class CredentialUtilityController : ControllerBase
{
    private readonly ICredentialUtilityService _credentialUtilityService;

    public CredentialUtilityController(ICredentialUtilityService credentialUtilityService)
    {
        _credentialUtilityService = credentialUtilityService;
    }

    [HttpPost("grant-and-mint")]
    public async Task<IActionResult> GrantAndMint([FromBody] GrantMembershipCredentialDto dto, CancellationToken ct)
    {
        var result = await _credentialUtilityService.GrantAndMintAsync(dto, ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetChainData), new { id = result.Value!.Id }, result.Value)
            : ToActionResult(result);
    }

    [HttpPost("{id}/revoke-and-burn")]
    public async Task<IActionResult> RevokeAndBurn(string id, CancellationToken ct)
    {
        var result = await _credentialUtilityService.RevokeAndBurnAsync(id, ct);
        return ToActionResult(result);
    }

    [HttpPatch("{id}/upgrade-tier")]
    public async Task<IActionResult> UpgradeTier(string id, [FromBody] UpdateCredentialTierDto dto, CancellationToken ct)
    {
        var result = await _credentialUtilityService.UpgradeTierAsync(id, dto.Tier, ct);
        return ToActionResult(result);
    }

    [HttpPost("check-auto-grant")]
    public async Task<IActionResult> CheckAutoGrant([FromBody] CheckAutoGrantRequestDto dto, CancellationToken ct)
    {
        var result = await _credentialUtilityService.CheckAndAutoGrantAsync(dto.UserId, dto.ProjectId, dto.GuildId, ct);
        return ToActionResult(result);
    }

    [HttpPost("{id}/retry-mint")]
    public async Task<IActionResult> RetryMint(string id, CancellationToken ct)
    {
        var result = await _credentialUtilityService.RetryMintAsync(id, ct);
        return ToActionResult(result);
    }

    [HttpGet("{id}/chain-data")]
    public async Task<IActionResult> GetChainData(string id, CancellationToken ct)
    {
        var result = await _credentialUtilityService.GetCredentialWithChainDataAsync(id, ct);
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
