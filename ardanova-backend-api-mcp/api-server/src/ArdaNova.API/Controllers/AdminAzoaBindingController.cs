namespace ArdaNova.API.Controllers;

using System.Security.Claims;
using ArdaNova.API.Middleware;
using ArdaNova.Application.Common.Interfaces;
using ArdaNova.Application.Common.Results;
using ArdaNova.Domain.Models.Entities;
using ArdaNova.Domain.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

/// <summary>
/// Operator-only recovery for ArdaNova's local AZOA binding. AZOA is the single writer of
/// verificationLevel and the source of azoaAvatarId/azoaWalletId/azoaWalletAddress, but a node
/// rebuild (dev volume wipe) invalidates those pointers on AZOA's side while
/// `[Index(nameof(azoaAvatarId), IsUnique = true)]` on User keeps enforcing the stale one, so
/// every affected user's next EnsureAsync Conflicts against StableReferenceError forever. This
/// unbinds locally so AZOA can re-provision a fresh binding on the next call.
/// </summary>
[ApiController]
[Route("api/admin/azoa-binding")]
[Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
[Authorize(Policy = AuthorizationPolicies.AdminApiKey)]
public sealed class AdminAzoaBindingController : ControllerBase
{
    private const int MaximumReasonLength = 1000;

    private readonly IRepository<User> _users;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AdminAzoaBindingController> _logger;

    public AdminAzoaBindingController(
        IRepository<User> users,
        IUnitOfWork unitOfWork,
        ILogger<AdminAzoaBindingController> logger)
    {
        _users = users;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>Current local AZOA binding for one user (looked up by id or email), so an operator can confirm before unbinding.</summary>
    [HttpGet("{identifier}")]
    public async Task<IActionResult> GetBinding(string identifier, CancellationToken ct)
        => ToActionResult(await LookupAsync(identifier, ct));

    /// <summary>Clears the stale local AZOA binding; requires an audit reason and is logged with the operator's actor id.</summary>
    [HttpPost("{identifier}/unbind")]
    public async Task<IActionResult> Unbind(string identifier, [FromBody] UnbindAzoaAccountRequest? request, CancellationToken ct)
        => ToActionResult(await UnbindAsync(identifier, request, ct));

    private async Task<Result<AzoaBindingResponse>> LookupAsync(string identifier, CancellationToken ct)
    {
        var user = await ResolveUserAsync(identifier, ct);
        if (user is null)
            return Result<AzoaBindingResponse>.NotFound("User not found.");

        return Result<AzoaBindingResponse>.Success(ToResponse(user));
    }

    private async Task<Result<AzoaBindingUnboundResponse>> UnbindAsync(
        string identifier,
        UnbindAzoaAccountRequest? request,
        CancellationToken ct)
    {
        var reason = request?.Reason?.Trim();
        if (string.IsNullOrWhiteSpace(reason) || reason.Length > MaximumReasonLength)
            return Result<AzoaBindingUnboundResponse>.ValidationError(
                $"A reason of 1-{MaximumReasonLength} characters is required to unbind an AZOA account.");

        var user = await ResolveUserAsync(identifier, ct);
        if (user is null)
            return Result<AzoaBindingUnboundResponse>.NotFound("User not found.");

        if (string.IsNullOrWhiteSpace(user.azoaAvatarId)
            && string.IsNullOrWhiteSpace(user.azoaWalletId)
            && string.IsNullOrWhiteSpace(user.azoaWalletAddress))
            return Result<AzoaBindingUnboundResponse>.Conflict("This user has no AZOA binding to unbind.");

        var previous = ToResponse(user);
        var operatorActorId = ActorId;

        user.azoaAvatarId = null;
        user.azoaWalletId = null;
        user.azoaWalletAddress = null;

        // AZOA's identity binding backing this grade is gone; mirrors the demotion
        // AzoaCustodialAccountService.ProjectVerificationLevel applies for any non-approved status.
        if (user.verificationLevel == VerificationLevel.PRO)
            user.verificationLevel = user.emailVerified.HasValue
                ? VerificationLevel.VERIFIED
                : VerificationLevel.ANONYMOUS;
        user.updatedAt = DateTime.UtcNow;

        await _users.UpdateAsync(user, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogWarning(
            "AZOA binding unbound for user {UserId} by operator {OperatorActorId}. Reason: {Reason}. " +
            "Previous avatarId={PreviousAvatarId} walletId={PreviousWalletId} walletAddress={PreviousWalletAddress} verificationLevel={PreviousVerificationLevel}.",
            user.id,
            operatorActorId,
            reason,
            previous.AzoaAvatarId,
            previous.AzoaWalletId,
            previous.AzoaWalletAddress,
            previous.VerificationLevel);

        return Result<AzoaBindingUnboundResponse>.Success(new AzoaBindingUnboundResponse(
            UserId: user.id,
            OperatorActorId: operatorActorId,
            Reason: reason,
            PreviousAzoaAvatarId: previous.AzoaAvatarId,
            PreviousAzoaWalletId: previous.AzoaWalletId,
            PreviousAzoaWalletAddress: previous.AzoaWalletAddress,
            PreviousVerificationLevel: previous.VerificationLevel,
            VerificationLevel: user.verificationLevel.ToString(),
            UnboundAt: user.updatedAt));
    }

    /// <summary>Resolves an operator-supplied identifier as a User id first, then an email.</summary>
    private async Task<User?> ResolveUserAsync(string identifier, CancellationToken ct)
    {
        var trimmed = identifier.Trim();
        var byId = await _users.GetByIdAsync(trimmed, ct);
        if (byId is not null)
            return byId;

        return trimmed.Contains('@', StringComparison.Ordinal)
            ? await _users.FindOneAsync(u => u.email == trimmed, ct)
            : null;
    }

    private static AzoaBindingResponse ToResponse(User user) => new(
        UserId: user.id,
        AzoaAvatarId: user.azoaAvatarId,
        AzoaWalletId: user.azoaWalletId,
        AzoaWalletAddress: user.azoaWalletAddress,
        VerificationLevel: user.verificationLevel.ToString());

    private string ActorId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

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
            ResultType.Conflict => Conflict(new { error = result.Error }),
            _ => BadRequest(new { error = result.Error }),
        };
    }
}

/// <summary>Local AZOA binding pointers for one user, safe for operator display.</summary>
public sealed record AzoaBindingResponse(
    string UserId,
    string? AzoaAvatarId,
    string? AzoaWalletId,
    string? AzoaWalletAddress,
    string VerificationLevel);

/// <summary>Reason an operator gives for severing a stale AZOA binding; required for the audit log.</summary>
public sealed record UnbindAzoaAccountRequest(string? Reason);

/// <summary>Confirms an unbind: previous pointers plus who did it and why, for the audit trail.</summary>
public sealed record AzoaBindingUnboundResponse(
    string UserId,
    string OperatorActorId,
    string Reason,
    string? PreviousAzoaAvatarId,
    string? PreviousAzoaWalletId,
    string? PreviousAzoaWalletAddress,
    string PreviousVerificationLevel,
    string VerificationLevel,
    DateTime UnboundAt);
