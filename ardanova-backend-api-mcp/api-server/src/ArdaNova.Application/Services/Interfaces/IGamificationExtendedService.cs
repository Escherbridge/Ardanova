namespace ArdaNova.Application.Services.Interfaces;

using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;

public interface IUserStreakService
{
    Task<Result<UserStreakDto>> GetByIdAsync(string id, CancellationToken ct = default);
    Task<Result<UserStreakDto>> GetByUserIdAsync(string userId, CancellationToken ct = default);
    Task<Result<UserStreakDto>> CreateAsync(CreateUserStreakDto dto, CancellationToken ct = default);
    Task<Result<UserStreakDto>> RecordActivityAsync(string userId, CancellationToken ct = default);
    Task<Result<UserStreakDto>> ResetStreakAsync(string userId, CancellationToken ct = default);
    Task<Result<bool>> DeleteAsync(string id, CancellationToken ct = default);
}

public interface IReferralService
{
    Task<Result<ReferralDto>> GetByIdAsync(string id, CancellationToken ct = default);
    Task<Result<IReadOnlyList<ReferralDto>>> GetByReferrerIdAsync(string referrerId, CancellationToken ct = default);
    Task<Result<ReferralDto>> GetByReferredIdAsync(string referredId, CancellationToken ct = default);
    Task<Result<ReferralDto>> GetByCodeAsync(string code, CancellationToken ct = default);
    Task<Result<ReferralDto>> CreateAsync(CreateReferralDto dto, CancellationToken ct = default);
    Task<Result<ReferralDto>> CompleteAsync(string id, CancellationToken ct = default);
    Task<Result<ReferralDto>> ClaimRewardAsync(string id, ClaimReferralRewardDto dto, CancellationToken ct = default);
    Task<Result<ReferralDto>> ExpireAsync(string id, CancellationToken ct = default);
    Task<Result<ReferralDto>> CancelAsync(string id, CancellationToken ct = default);
}
