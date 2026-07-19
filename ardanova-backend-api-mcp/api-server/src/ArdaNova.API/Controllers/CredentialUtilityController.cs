namespace ArdaNova.API.Controllers;

using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Interfaces;
using Microsoft.AspNetCore.Http;
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
    public IActionResult GrantAndMint([FromBody] GrantMembershipCredentialDto dto, CancellationToken ct)
    {
        _ = dto;
        _ = ct;
        return MutationUnavailable();
    }

    [HttpPost("{id}/revoke-and-burn")]
    public IActionResult RevokeAndBurn(string id, CancellationToken ct)
    {
        _ = id;
        _ = ct;
        return MutationUnavailable();
    }

    [HttpPatch("{id}/upgrade-tier")]
    public IActionResult UpgradeTier(string id, [FromBody] UpdateCredentialTierDto dto, CancellationToken ct)
    {
        _ = id;
        _ = dto;
        _ = ct;
        return MutationUnavailable();
    }

    [HttpPost("check-auto-grant")]
    public IActionResult CheckAutoGrant([FromBody] CheckAutoGrantRequestDto dto, CancellationToken ct)
    {
        _ = dto;
        _ = ct;
        return MutationUnavailable();
    }

    [HttpPost("{id}/retry-mint")]
    public IActionResult RetryMint(string id, CancellationToken ct)
    {
        _ = id;
        _ = ct;
        return MutationUnavailable();
    }

    [HttpGet("{id}/chain-data")]
    public async Task<IActionResult> GetChainData(string id, CancellationToken ct)
    {
        var result = await _credentialUtilityService.GetCredentialWithChainDataAsync(id, ct);
        return ToActionResult(result);
    }

    private IActionResult MutationUnavailable()
        => Problem(
            statusCode: StatusCodes.Status501NotImplemented,
            title: "Credential lifecycle mutations are unavailable",
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
