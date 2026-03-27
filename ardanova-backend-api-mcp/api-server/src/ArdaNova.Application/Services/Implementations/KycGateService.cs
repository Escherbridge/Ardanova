namespace ArdaNova.Application.Services.Implementations;

using ArdaNova.Application.Common.Interfaces;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.Services.Interfaces;
using ArdaNova.Domain.Models.Entities;
using ArdaNova.Domain.Models.Enums;

public class KycGateService : IKycGateService
{
    private readonly IRepository<User> _userRepository;

    public KycGateService(IRepository<User> userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<Result<bool>> RequireProAsync(string userId, CancellationToken ct = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, ct);
        if (user is null)
            return Result<bool>.NotFound($"User with id {userId} not found");

        if (user.verificationLevel >= VerificationLevel.PRO)
            return Result<bool>.Success(true);

        return Result<bool>.Forbidden(
            "KYC verification required. Complete identity verification to unlock this feature. " +
            "Visit /settings/verification to begin the process.");
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
