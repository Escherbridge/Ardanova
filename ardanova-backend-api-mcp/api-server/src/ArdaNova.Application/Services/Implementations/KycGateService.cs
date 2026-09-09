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

    /// <summary>
    /// The identity gate has FOUR outcomes, not two, and it shares its words with the value
    /// gate so the two cannot drift. Collapsing an unreadable account into Forbidden told a
    /// person whose identity is approved to go and verify it again - blaming them for a
    /// platform state - and a held account was told to redo work that is already finished.
    /// The allow/deny answer is unchanged; the refusal type and the copy are what move, so
    /// the edge can answer 409 for "could not confirm" rather than 403 "not allowed".
    /// </summary>
    public async Task<Result<bool>> RequireProAsync(string userId, CancellationToken ct = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, ct);
        if (user is null)
            return Result<bool>.NotFound($"User with id {userId} not found");

        var status = await _azoaAccounts.GetStatusAsync(userId, ct);

        // Nothing at all is known about this person's setup. Saying "finish verifying" here
        // would be both false and unactionable.
        if (!status.IsSuccess || status.Value is null)
            return Result<bool>.Conflict(ValueEligibilityMessages.ServiceUnavailable);

        // Checked before approval, exactly as the value gate orders it: setup is done and a
        // reviewer still owes a decision.
        if (status.Value.KycStatus == AzoaKycStatus.UnderReview)
            return Result<bool>.Forbidden(ValueEligibilityMessages.UnderReview(status.Value.ReviewDueAt));

        if (status.Value is { IdentityReady: true, KycStatus: AzoaKycStatus.Approved })
            return Result<bool>.Success(true);

        // This gate does not know which action it is guarding, so the shared copy falls back
        // to its generic phrasing rather than inventing a specific one.
        return Result<bool>.Forbidden(ValueEligibilityMessages.SetupIncomplete(string.Empty));
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
