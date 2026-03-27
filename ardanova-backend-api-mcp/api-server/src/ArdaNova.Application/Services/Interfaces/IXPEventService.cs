namespace ArdaNova.Application.Services.Interfaces;

using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Domain.Models.Enums;

public interface IXPEventService
{
    Task<Result<XPEventDto>> AwardXPAsync(AwardXPDto dto, CancellationToken ct = default);
    Task<Result<int>> GetTotalXPAsync(string userId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<XPEventDto>>> GetHistoryAsync(string userId, XPEventType? eventType = null, int limit = 50, int offset = 0, CancellationToken ct = default);
    Task<Result<int>> GetXPByEventTypeAsync(string userId, XPEventType eventType, CancellationToken ct = default);
    Task<Result<XPSummaryDto>> GetXPSummaryAsync(string userId, CancellationToken ct = default);
    XPRewardsConfigDto GetRewardsConfig();
    LevelInfoDto GetLevelInfo(int level);
    int CalculateLevel(int totalXP);
    UserTier CalculateTier(int totalXP);
}
