namespace ArdaNova.Application.Services.Implementations;

using ArdaNova.Application.Common.Interfaces;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Interfaces;
using ArdaNova.Domain.Models.Entities;
using ArdaNova.Domain.Models.Enums;

public class KycGateService : IKycGateService
{
    private readonly IRepository<User> _userRepository;
    private readonly IAzoaCustodialAccountService _azoaAccounts;

    public KycGateService(
        IRepository<User> userRepository,
        IAzoaCustodialAccountService azoaAccounts)
    {
        _userRepository = userRepository;
        _azoaAccounts = azoaAccounts;
    }

    public async Task<Result<bool>> RequireProAsync(string userId, CancellationToken ct = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, ct);
        if (user is null)
            return Result<bool>.NotFound($"User with id {userId} not found");

        var status = await _azoaAccounts.GetStatusAsync(userId, ct);
        if (status.IsSuccess
            && status.Value is { IdentityReady: true, KycStatus: AzoaKycStatus.Approved })
            return Result<bool>.Success(true);

        return Result<bool>.Forbidden(
            "A current AZOA identity approval is required. Open /settings/verification to check or continue verification.");
    }

    public async Task<Result<bool>> RequireVerifiedAsync(string userId, CancellationToken ct = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, ct);
        if (user is null)
            return Result<bool>.NotFound($"User with id {userId} not found");

        if (user.verificationLevel >= VerificationLevel.VERIFIED)
            return Result<bool>.Success(true);

        return Result<bool>.Forbidden(
            "Email verification required. Please verify your email to access this feature.");
    }

    public async Task<Result<VerificationLevel>> GetVerificationLevelAsync(string userId, CancellationToken ct = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, ct);
        if (user is null)
            return Result<VerificationLevel>.NotFound($"User with id {userId} not found");

        return Result<VerificationLevel>.Success(user.verificationLevel);
    }
}
