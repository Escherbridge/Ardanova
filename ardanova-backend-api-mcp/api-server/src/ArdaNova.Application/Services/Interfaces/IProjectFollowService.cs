namespace ArdaNova.Application.Services.Interfaces;

using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Implementations;

/// <summary>
/// Project follow (SR-03 Phase 3 §8-6). Hardens the three flaws GuildFollowService has: a
/// visibility check runs before every write (a hidden/draft project answers 404, same as a missing
/// one); FollowAsync has no exists-then-insert race (a concurrent follow is idempotent, not a 500);
/// and there is no follower-list route and no read of an arbitrary user's state - only the signed
/// actor's own. See Services/Implementations/AGENTS.md §Project follow.
/// </summary>
public interface IProjectFollowService
{
    /// <summary>Idempotent: following twice returns the same row's Success both times, never Conflict.</summary>
    Task<Result<ProjectFollowDto>> FollowAsync(string projectId, ProjectViewer viewer, CancellationToken ct = default);

    /// <summary>Idempotent: unfollowing a project the viewer never followed, or twice at once, still succeeds.</summary>
    Task<Result<bool>> UnfollowAsync(string projectId, ProjectViewer viewer, CancellationToken ct = default);

    /// <summary>The viewer's own state only - there is no route to read another user's.</summary>
    Task<Result<ProjectFollowStatusDto>> GetMyFollowStatusAsync(string projectId, ProjectViewer viewer, CancellationToken ct = default);
}
