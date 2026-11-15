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
    private readonly IAzoaAccountHandoffService? _handoff;

    public AzoaCustodialAccountController(
        IAzoaCustodialAccountService accounts,
        IAzoaCustodialAccountGateway custody,
        IAzoaAccountHandoffService? handoff = null)
    {
        _accounts = accounts;
        _custody = custody;
        _handoff = handoff;
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
            return Conflict(MoneyRefusal.ToErrorBody(MoneyRefusal.For(
                MoneyRefusalCodes.PaymentsNotConfigured,
                "We could not reach the operator to hand your permission back. Nothing was changed.")));
        }

        if (!revocation.CanRevokeConsent)
            return ToActionResult(await revocation.RevokeProvisionedConsentAsync(ActorId, ct));
        var status = await _accounts.GetStatusAsync(ActorId, ct);
        if (status is null || status.IsFailure || status.Value is null)
            return Conflict(new { error = "The account permission state could not be confirmed. Nothing was changed." });
        var result = status.Value.Ownership == "claimed"
            ? await revocation.RevokeClaimedConsentAsync(ActorId, ct)
            : await revocation.RevokeProvisionedConsentAsync(ActorId, ct);
        return result.IsSuccess
            ? Ok(new AzoaConsentRevocationResponse(result.Value!.Revoked, result.Value.RevokedAt))
            : ToActionResult(result);
    }

    [HttpPost("handoff")]
    public async Task<IActionResult> BeginHandoff(CancellationToken ct)
        => _handoff is null ? Conflict(new { error = "Account handoff is unavailable." })
            : ToActionResult(await _handoff.BeginAsync(ActorId, null, ct));

    [HttpPost("handoff/{requestId}/resend")]
    public async Task<IActionResult> ResendHandoff(string requestId, CancellationToken ct)
        => _handoff is null ? Conflict(new { error = "Account handoff is unavailable." })
            : ToActionResult(await _handoff.BeginAsync(ActorId, requestId, ct));

    [HttpGet("handoff/{requestId}")]
    public async Task<IActionResult> ReadHandoff(string requestId, CancellationToken ct)
        => _handoff is null ? Conflict(new { error = "Account handoff is unavailable." })
            : ToActionResult(await _handoff.ReadAsync(ActorId, requestId, ct));

    private string ActorId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    // CustodialAccountLifecycle: the node's KYB_REQUIRED code wins; only an UNCODED Forbidden falls
    // back to kyb_required. See Controllers/AGENTS.md §Money refusal codes.
    private IActionResult ToActionResult<T>(Result<T> result)
    {
        if (result.Type == ResultType.Success)
            return Ok(result.Value);

        var body = MoneyRefusal.ToErrorBody(MoneyRefusal.From(result, MoneyRefusalContext.CustodialAccountLifecycle));
        return result.Type switch
        {
            ResultType.NotFound => NotFound(body),
            ResultType.Unauthorized => Unauthorized(body),
            ResultType.Forbidden => StatusCode(StatusCodes.Status403Forbidden, body),
            ResultType.Conflict => Conflict(body),
            ResultType.ValidationError or ResultType.BadRequest => BadRequest(body),
            // A transport failure is the platform's, never the caller's request: 503, not 400.
            _ => StatusCode(StatusCodes.Status503ServiceUnavailable, body),
        };
    }
}

/// <summary>Wire shape the verification screen reads; mirrors AzoaConsentRevocationDto on the client.</summary>
/// <param name="Revoked">The permission is no longer in force.</param>
/// <param name="RevokedAt">When this request ended it; null when it was already withdrawn.</param>
public sealed record AzoaConsentRevocationResponse(bool Revoked, DateTimeOffset? RevokedAt);
