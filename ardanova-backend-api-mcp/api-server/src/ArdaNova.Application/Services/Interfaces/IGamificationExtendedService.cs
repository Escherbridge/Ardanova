namespace ArdaNova.Application.Services.Interfaces;

using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;

public interface IUserStreakService
{
    Task<Result<UserStreakDto>> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Result<UserStreakDto>> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<Result<UserStreakDto>> CreateAsync(CreateUserStreakDto dto, CancellationToken ct = default);
    Task<Result<UserStreakDto>> RecordActivityAsync(Guid userId, CancellationToken ct = default);
    Task<Result<UserStreakDto>> ResetStreakAsync(Guid userId, CancellationToken ct = default);
    Task<Result<bool>> DeleteAsync(Guid id, CancellationToken ct = default);
}

public interface IReferralService
{
    Task<Result<ReferralDto>> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Result<IReadOnlyList<ReferralDto>>> GetByReferrerIdAsync(Guid referrerId, CancellationToken ct = default);
    Task<Result<ReferralDto>> GetByReferredIdAsync(Guid referredId, CancellationToken ct = default);
    Task<Result<ReferralDto>> GetByCodeAsync(string code, CancellationToken ct = default);
    Task<Result<ReferralDto>> CreateAsync(CreateReferralDto dto, CancellationToken ct = default);
    Task<Result<ReferralDto>> CompleteAsync(Guid id, CancellationToken ct = default);
    Task<Result<ReferralDto>> ClaimRewardAsync(Guid id, ClaimReferralRewardDto dto, CancellationToken ct = default);
    Task<Result<ReferralDto>> ExpireAsync(Guid id, CancellationToken ct = default);
    Task<Result<ReferralDto>> CancelAsync(Guid id, CancellationToken ct = default);
}
