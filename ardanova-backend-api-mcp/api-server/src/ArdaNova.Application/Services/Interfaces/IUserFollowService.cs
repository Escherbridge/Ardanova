namespace ArdaNova.Application.Services.Interfaces;

using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;

public interface IUserFollowService
{
    Task<Result<IReadOnlyList<UserFollowDto>>> GetFollowersAsync(string userId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<UserFollowDto>>> GetFollowingAsync(string userId, CancellationToken ct = default);
    Task<Result<UserFollowDto>> FollowAsync(CreateUserFollowDto dto, CancellationToken ct = default);
    Task<Result<bool>> UnfollowAsync(string followerId, string followingId, CancellationToken ct = default);
    Task<Result<bool>> IsFollowingAsync(string followerId, string followingId, CancellationToken ct = default);
    Task<Result<UserFollowCountsDto>> GetFollowCountsAsync(string userId, CancellationToken ct = default);
}
