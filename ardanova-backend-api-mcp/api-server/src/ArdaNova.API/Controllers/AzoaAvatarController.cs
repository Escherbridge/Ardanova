namespace ArdaNova.API.Controllers;

using System.Security.Claims;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// AZOA avatar onboarding endpoints (track <c>azoa-avatar-onboarding</c>).
///
/// IDOR-SAFE BY DESIGN: the target user is always the authenticated principal —
/// resolved from the session/JWT claim, NEVER from a request body or a
/// body-supplied id. A caller cannot onboard or read another user's avatar.
///
/// JWT forwarding from Next.js is not yet wired in this API (see SwapsController
/// for the same interim state). Until <c>[Authorize]</c> + JWT bearer auth is
/// enabled at startup, the user id falls back to an explicit <c>userId</c> query
/// parameter — still never a body field, so the IDOR-safe contract holds. Once
/// auth forwarding lands, uncomment <c>[Authorize]</c> and the claim becomes the
/// sole source.
/// </summary>
// TODO: enable once JWT forwarding from Next.js is configured.
// [Authorize]
[ApiController]
[Route("api/azoa/avatar")]
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
    public async Task<IActionResult> EnsureAvatar([FromQuery] string? userId, CancellationToken ct)
    {
        var resolved = ResolveUserId(userId);
        if (resolved is null)
            return Unauthorized(new { error = "Authenticated user could not be resolved." });

        var result = await _avatarService.EnsureAvatarAsync(resolved, ct);
        return ToActionResult(result);
    }

    /// <summary>
    /// Ensure a wallet reference is bound/cached for the authenticated user's avatar.
    /// Wallet binding completes lazily via the allocation path; this guarantees the
    /// avatar exists and surfaces any cached wallet reference.
    /// POST /api/azoa/avatar/wallet
    /// </summary>
    [HttpPost("wallet")]
    public async Task<IActionResult> EnsureWallet([FromQuery] string? userId, CancellationToken ct)
    {
        var resolved = ResolveUserId(userId);
        if (resolved is null)
            return Unauthorized(new { error = "Authenticated user could not be resolved." });

        var result = await _avatarService.EnsureWalletAsync(resolved, ct);
        return ToActionResult(result);
    }

    /// <summary>
    /// Read the authenticated user's avatar/wallet linkage status.
    /// GET /api/azoa/avatar/status
    /// </summary>
    [HttpGet("status")]
    public async Task<IActionResult> GetStatus([FromQuery] string? userId, CancellationToken ct)
    {
        var resolved = ResolveUserId(userId);
        if (resolved is null)
            return Unauthorized(new { error = "Authenticated user could not be resolved." });

        var result = await _avatarService.GetStatusAsync(resolved, ct);
        return ToActionResult(result);
    }

    /// <summary>
    /// Resolve the target user id. Prefers the authenticated principal's claim
    /// (the only source once JWT forwarding is wired); falls back to the explicit
    /// <paramref name="queryUserId"/> during the auth-forwarding gap. NEVER reads
    /// a user id from the request body — that is what keeps these endpoints
    /// IDOR-safe.
    /// </summary>
    private string? ResolveUserId(string? queryUserId)
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub")
            ?? User.FindFirstValue("id");

        if (!string.IsNullOrWhiteSpace(claim))
            return claim;

        // Interim only: JWT forwarding not yet wired (see class remarks).
        return string.IsNullOrWhiteSpace(queryUserId) ? null : queryUserId;
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
            // KYC_FORBIDDEN from the node surfaces here as Forbidden; the client
            // translates it into an actionable "KYC required to transact" message.
            ResultType.Forbidden => StatusCode(StatusCodes.Status403Forbidden, new { error = result.Error }),
            ResultType.Conflict => Conflict(new { error = result.Error }),
            _ => BadRequest(new { error = result.Error })
        };
    }
}
