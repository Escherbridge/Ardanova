namespace ArdaNova.Application.Services.Implementations;

using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Interfaces;
using Microsoft.Extensions.Logging;

/// <inheritdoc/>
public sealed class AzoaValueEligibilityService : IAzoaValueEligibilityService
{
    private readonly IAzoaCustodialAccountService _custodialAccountService;
    private readonly ILogger<AzoaValueEligibilityService> _logger;

    public AzoaValueEligibilityService(
        IAzoaCustodialAccountService custodialAccountService,
        ILogger<AzoaValueEligibilityService> logger)
    {
        _custodialAccountService = custodialAccountService;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<Result<AzoaCustodialAccountStatusDto>> RequireReadyAsync(
        string userId,
        string action,
        CancellationToken ct = default)
    {
        // A missing subject is its own refusal. The gate is never skipped just because
        // the caller does not happen to know who the action is for.
        if (string.IsNullOrWhiteSpace(userId))
        {
            _logger.LogWarning(
                "Value-action eligibility for {ValueAction} was requested without a subject; refused.",
                action);
            return Result<AzoaCustodialAccountStatusDto>.Forbidden(ValueEligibilityMessages.SubjectUnknown);
        }

        Result<AzoaCustodialAccountStatusDto> statusResult;
        try
        {
            statusResult = await _custodialAccountService.GetStatusAsync(userId, ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            return Unconfirmable(userId, action, exception.GetType().Name, exception);
        }

        // The account was never read, so nothing at all is known about this person's setup.
        if (statusResult.IsFailure || statusResult.Value is null)
            return Unconfirmable(userId, action, statusResult.Error ?? "no account status was returned", null);

        var status = statusResult.Value;

        // The node itself reported it cannot provide the account. That is a platform state;
        // telling this person to finish setup would be both false and unactionable.
        if (!string.IsNullOrWhiteSpace(status.UnavailableReason))
            return Unconfirmable(userId, action, status.UnavailableReason, null);

        if (!IsReadyForValueAction(status))
        {
            return Result<AzoaCustodialAccountStatusDto>.Forbidden(
                ValueEligibilityMessages.SetupIncomplete(action));
        }

        return Result<AzoaCustodialAccountStatusDto>.Success(status);
    }

    /// <summary>One readiness rule for every participant: funder, owner, assignee and beneficiary alike.</summary>
    private static bool IsReadyForValueAction(AzoaCustodialAccountStatusDto status)
        => status.Ready
           && status.IdentityReady
           && !string.IsNullOrWhiteSpace(status.AvatarId)
           && status.KycReady
           && status.KycStatus == AzoaKycStatus.Approved
           && status.WalletReady
           && !string.IsNullOrWhiteSpace(status.WalletId)
           && !string.IsNullOrWhiteSpace(status.WalletAddress);

    /// <summary>Fails closed on an unreadable account while keeping the provider diagnostic out of the person's message.</summary>
    private Result<AzoaCustodialAccountStatusDto> Unconfirmable(
        string userId,
        string action,
        string diagnostic,
        Exception? exception)
    {
        _logger.LogWarning(
            exception,
            "Value-action eligibility for {ValueAction} could not be confirmed for user {UserId}: {Diagnostic}",
            action,
            userId,
            diagnostic);

        return Result<AzoaCustodialAccountStatusDto>.Conflict(ValueEligibilityMessages.ServiceUnavailable);
    }
}
