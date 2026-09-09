namespace ArdaNova.API.Controllers;

using System.Security.Claims;
using ArdaNova.API.Middleware;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.Services.Interfaces;
using ArdaNova.Infrastructure.Azoa;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/azoa/custodial-account")]
[Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
public sealed class AzoaCustodialAccountController : ControllerBase
{
    private readonly IAzoaCustodialAccountService _accounts;
    private readonly IAzoaCustodialAccountGateway _custody;

    public AzoaCustodialAccountController(
        IAzoaCustodialAccountService accounts,
        IAzoaCustodialAccountGateway custody)
    {
        _accounts = accounts;
        _custody = custody;
    }

    [HttpGet("capabilities")]
    public async Task<IActionResult> Capabilities(CancellationToken ct)
        => ToActionResult(await _accounts.GetCapabilitiesAsync(ct));

    [HttpPost("ensure")]
    public async Task<IActionResult> Ensure(CancellationToken ct)
        => ToActionResult(await _accounts.EnsureAsync(ActorId, ct));

    [HttpGet("status")]
    public async Task<IActionResult> Status(CancellationToken ct)
        => ToActionResult(await _accounts.GetStatusAsync(ActorId, ct));

    [HttpPost("kyc/session")]
    public async Task<IActionResult> BeginKyc(CancellationToken ct)
        => ToActionResult(await _accounts.BeginKycAsync(ActorId, ct));

    /// <summary>Hands the caller's own standing permission back to them; there is no parameter for anyone else.</summary>
    [HttpDelete("consent")]
    public async Task<IActionResult> RevokeConsent(CancellationToken ct)
    {
        // The withdrawal port is implemented only by the ENABLED custody gateway. When custody is
        // switched off, DisabledAzoaCustodialAccountGateway does not implement it and this refuses
        // rather than reporting a withdrawal that never reached the node.
        if (_custody is not IAzoaConsentRevocationGateway revocation)
        {
            return Conflict(new
            {
                error = "We could not reach the operator to hand your permission back. Nothing was changed.",
            });
        }

        var result = await revocation.RevokeProvisionedConsentAsync(ActorId, ct);
        return result.IsSuccess
            ? Ok(new AzoaConsentRevocationResponse(result.Value!.Revoked, result.Value.RevokedAt))
            : ToActionResult(result);
    }

    private string ActorId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    private IActionResult ToActionResult<T>(Result<T> result)
        => result.Type switch
        {
            ResultType.Success => Ok(result.Value),
            ResultType.NotFound => NotFound(new { error = result.Error }),
            ResultType.Unauthorized => Unauthorized(new { error = result.Error }),
            ResultType.Forbidden => StatusCode(StatusCodes.Status403Forbidden, new { error = result.Error }),
            ResultType.Conflict => Conflict(new { error = result.Error }),
            _ => BadRequest(new { error = result.Error }),
        };
}

/// <summary>Wire shape the verification screen reads; mirrors AzoaConsentRevocationDto on the client.</summary>
/// <param name="Revoked">The permission is no longer in force.</param>
/// <param name="RevokedAt">When this request ended it; null when it was already withdrawn.</param>
public sealed record AzoaConsentRevocationResponse(bool Revoked, DateTimeOffset? RevokedAt);
