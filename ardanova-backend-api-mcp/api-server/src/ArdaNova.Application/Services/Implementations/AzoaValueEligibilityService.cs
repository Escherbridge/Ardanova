namespace ArdaNova.Application.Services.Implementations;

using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Interfaces;

/// <inheritdoc/>
public sealed class AzoaValueEligibilityService : IAzoaValueEligibilityService
{
    private readonly IAzoaCustodialAccountService _custodialAccountService;

    public AzoaValueEligibilityService(IAzoaCustodialAccountService custodialAccountService)
    {
        _custodialAccountService = custodialAccountService;
    }

    /// <inheritdoc/>
    public async Task<Result<AzoaCustodialAccountStatusDto>> RequireReadyAsync(
        string userId,
        string action,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return Result<AzoaCustodialAccountStatusDto>.Forbidden(NotReadyMessage(action));

        Result<AzoaCustodialAccountStatusDto> statusResult;
        try
        {
            statusResult = await _custodialAccountService.GetStatusAsync(userId, ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            return Result<AzoaCustodialAccountStatusDto>.Forbidden(NotReadyMessage(action));
        }

        if (!statusResult.IsSuccess || statusResult.Value is null || !IsReadyForValueAction(statusResult.Value))
            return Result<AzoaCustodialAccountStatusDto>.Forbidden(NotReadyMessage(action));

        return Result<AzoaCustodialAccountStatusDto>.Success(statusResult.Value);
    }

    private static bool IsReadyForValueAction(AzoaCustodialAccountStatusDto status)
        => status.Ready
           && status.IdentityReady
           && !string.IsNullOrWhiteSpace(status.AvatarId)
           && status.KycReady
           && status.KycStatus == AzoaKycStatus.Approved
           && status.WalletReady
           && !string.IsNullOrWhiteSpace(status.WalletId)
           && !string.IsNullOrWhiteSpace(status.WalletAddress);

    private static string NotReadyMessage(string action)
    {
        var actionDescription = string.IsNullOrWhiteSpace(action)
            ? "continue with this action"
            : action;
        return $"Complete your account setup before you can {actionDescription}. You can still create projects and collaborate while setup is pending.";
    }
}
