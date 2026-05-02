namespace ArdaNova.Application.Services.Interfaces;

using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Domain.Models.Enums;

public interface ILeaderboardService
{
    Task<Result<LeaderboardDto>> GetByIdAsync(string id, CancellationToken ct = default);
    Task<Result<IReadOnlyList<LeaderboardDto>>> GetByPeriodAsync(LeaderboardPeriod period, CancellationToken ct = default);
    Task<Result<IReadOnlyList<LeaderboardDto>>> GetByCategoryAsync(LeaderboardCategory category, CancellationToken ct = default);
    Task<Result<LeaderboardDto>> CreateAsync(CreateLeaderboardDto dto, CancellationToken ct = default);
    Task<Result<bool>> DeleteAsync(string id, CancellationToken ct = default);

    // Entries
    Task<Result<IReadOnlyList<LeaderboardEntryDto>>> GetEntriesAsync(string leaderboardId, CancellationToken ct = default);
    Task<Result<LeaderboardEntryDto>> AddEntryAsync(CreateLeaderboardEntryDto dto, CancellationToken ct = default);
    Task<Result<LeaderboardEntryDto>> UpdateEntryAsync(string entryId, UpdateLeaderboardEntryDto dto, CancellationToken ct = default);
    Task<Result<bool>> DeleteEntryAsync(string entryId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<LeaderboardEntryDto>>> GetUserRankingsAsync(string userId, CancellationToken ct = default);
}
