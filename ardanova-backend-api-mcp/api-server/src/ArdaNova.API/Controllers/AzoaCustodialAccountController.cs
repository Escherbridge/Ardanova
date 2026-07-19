namespace ArdaNova.API.Controllers;

using System.Security.Claims;
using ArdaNova.API.Middleware;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/azoa/custodial-account")]
[Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
public sealed class AzoaCustodialAccountController : ControllerBase
{
    private readonly IAzoaCustodialAccountService _accounts;

    public AzoaCustodialAccountController(IAzoaCustodialAccountService accounts)
    {
        _accounts = accounts;
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
