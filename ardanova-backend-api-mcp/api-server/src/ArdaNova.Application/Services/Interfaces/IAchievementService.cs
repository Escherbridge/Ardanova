namespace ArdaNova.Application.Services.Interfaces;

using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Domain.Models.Enums;

public interface IAchievementService
{
    Task<Result<AchievementDto>> GetByIdAsync(string id, CancellationToken ct = default);
    Task<Result<IReadOnlyList<AchievementDto>>> GetAllAsync(CancellationToken ct = default);
    Task<Result<IReadOnlyList<AchievementDto>>> GetByCategoryAsync(AchievementCategory category, CancellationToken ct = default);
    Task<Result<AchievementDto>> CreateAsync(CreateAchievementDto dto, CancellationToken ct = default);
    Task<Result<AchievementDto>> UpdateAsync(string id, UpdateAchievementDto dto, CancellationToken ct = default);
    Task<Result<bool>> DeleteAsync(string id, CancellationToken ct = default);

    // User progress
    Task<Result<IReadOnlyList<UserAchievementDto>>> GetUserAchievementsAsync(string userId, CancellationToken ct = default);
    Task<Result<UserAchievementDto>> UpdateProgressAsync(string userId, string achievementId, UpdateProgressDto dto, CancellationToken ct = default);
    Task<Result<UserAchievementDto>> AwardAchievementAsync(string userId, string achievementId, CancellationToken ct = default);
}
