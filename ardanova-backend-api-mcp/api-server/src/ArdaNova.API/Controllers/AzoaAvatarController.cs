namespace ArdaNova.API.Controllers;

using System.Security.Claims;
using ArdaNova.API.Middleware;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// AZOA avatar onboarding endpoints for the signed BFF actor.
/// </summary>
[ApiController]
[Route("api/azoa/avatar")]
[Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
public class AzoaAvatarController : ControllerBase
{
    private readonly IAzoaAvatarService _avatarService;

    public AzoaAvatarController(IAzoaAvatarService avatarService)
    {
        _avatarService = avatarService;
    }

    /// <summary>
    /// Idempotently link the authenticated user to an AZOA avatar.
    /// POST /api/azoa/avatar/ensure
    /// </summary>
    [HttpPost("ensure")]
    public IActionResult EnsureAvatar(CancellationToken ct)
    {
        _ = ct;
        return LegacyMutationGone();
    }

    /// <summary>
    /// Ensure a wallet reference is bound/cached for the authenticated user's avatar.
    /// Wallet binding completes lazily via the allocation path; this guarantees the
    /// avatar exists and surfaces any cached wallet reference.
    /// POST /api/azoa/avatar/wallet
    /// </summary>
    [HttpPost("wallet")]
    public IActionResult EnsureWallet(CancellationToken ct)
    {
        _ = ct;
        return LegacyMutationGone();
    }

    /// <summary>
    /// Read the authenticated user's avatar/wallet linkage status.
    /// GET /api/azoa/avatar/status
    /// </summary>
    [HttpGet("status")]
    public async Task<IActionResult> GetStatus(CancellationToken ct)
    {
        var result = await _avatarService.GetStatusAsync(ActorId, ct);
        return ToActionResult(result);
    }

    /// <summary>
    /// The actor policy guarantees this claim is present.
    /// </summary>
    private string ActorId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    private IActionResult LegacyMutationGone()
        => Problem(
            statusCode: StatusCodes.Status410Gone,
            title: "Legacy Azoa onboarding route retired",
            detail: "Use /api/azoa/custodial-account for tenant-bound avatar, wallet, and KYC onboarding.");

    private IActionResult ToActionResult<T>(Result<T> result)
    {
        if (result.IsSuccess)
            return Ok(result.Value);

        return result.Type switch
        {
            ResultType.NotFound => NotFound(new { error = result.Error }),
            ResultType.ValidationError => BadRequest(new { error = result.Error }),
            ResultType.Unauthorized => Unauthorized(new { error = result.Error }),
            // KYC_FORBIDDEN from the node surfaces here as Forbidden; the client
            // translates it into an actionable "KYC required to transact" message.
            ResultType.Forbidden => StatusCode(StatusCodes.Status403Forbidden, new { error = result.Error }),
            ResultType.Conflict => Conflict(new { error = result.Error }),
            _ => BadRequest(new { error = result.Error })
        };
    }
}
