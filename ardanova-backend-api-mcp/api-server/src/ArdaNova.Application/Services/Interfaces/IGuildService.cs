namespace ArdaNova.Application.Services.Interfaces;

using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Domain.Models.Enums;

public interface IGuildService
{
    Task<Result<GuildDto>> GetByIdAsync(string id, CancellationToken ct = default);
    Task<Result<GuildDto>> GetBySlugAsync(string slug, CancellationToken ct = default);
    Task<Result<IReadOnlyList<GuildDto>>> GetAllAsync(CancellationToken ct = default);
    Task<Result<PagedResult<GuildDto>>> GetPagedAsync(int page, int pageSize, CancellationToken ct = default);
    Task<Result<GuildDto>> GetByOwnerIdAsync(string ownerId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<GuildDto>>> GetVerifiedAsync(CancellationToken ct = default);
    Task<Result<GuildDto>> CreateAsync(CreateGuildDto dto, CancellationToken ct = default);
    Task<Result<GuildDto>> UpdateAsync(string id, UpdateGuildDto dto, CancellationToken ct = default);
    Task<Result<bool>> DeleteAsync(string id, CancellationToken ct = default);
    Task<Result<GuildDto>> VerifyAsync(string id, CancellationToken ct = default);
}

public interface IGuildMemberService
{
    Task<Result<GuildMemberDto>> GetByIdAsync(string id, CancellationToken ct = default);
    Task<Result<IReadOnlyList<GuildMemberDto>>> GetByGuildIdAsync(string guildId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<GuildMemberDto>>> GetByUserIdAsync(string userId, CancellationToken ct = default);
    Task<Result<GuildMemberDto>> CreateAsync(CreateGuildMemberDto dto, CancellationToken ct = default);
    Task<Result<GuildMemberDto>> UpdateAsync(string id, UpdateGuildMemberDto dto, CancellationToken ct = default);
    Task<Result<bool>> DeleteAsync(string id, CancellationToken ct = default);
}

public interface IProjectBidService
{
    Task<Result<ProjectBidDto>> GetByIdAsync(string id, CancellationToken ct = default);
    Task<Result<IReadOnlyList<ProjectBidDto>>> GetByProjectIdAsync(string projectId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<ProjectBidDto>>> GetByGuildIdAsync(string guildId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<ProjectBidDto>>> GetByStatusAsync(BidStatus status, CancellationToken ct = default);
    Task<Result<ProjectBidDto>> CreateAsync(CreateProjectBidDto dto, CancellationToken ct = default);
    Task<Result<ProjectBidDto>> UpdateAsync(string id, UpdateProjectBidDto dto, CancellationToken ct = default);
    Task<Result<bool>> DeleteAsync(string id, CancellationToken ct = default);
    Task<Result<ProjectBidDto>> AcceptAsync(string id, CancellationToken ct = default);
    Task<Result<ProjectBidDto>> RejectAsync(string id, CancellationToken ct = default);
    Task<Result<ProjectBidDto>> WithdrawAsync(string id, CancellationToken ct = default);
}

public interface IGuildReviewService
{
    Task<Result<GuildReviewDto>> GetByIdAsync(string id, CancellationToken ct = default);
    Task<Result<IReadOnlyList<GuildReviewDto>>> GetByGuildIdAsync(string guildId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<GuildReviewDto>>> GetByUserIdAsync(string userId, CancellationToken ct = default);
    Task<Result<GuildReviewDto>> CreateAsync(CreateGuildReviewDto dto, CancellationToken ct = default);
    Task<Result<GuildReviewDto>> UpdateAsync(string id, UpdateGuildReviewDto dto, CancellationToken ct = default);
    Task<Result<bool>> DeleteAsync(string id, CancellationToken ct = default);
}

public interface IGuildUpdateService
{
    Task<Result<GuildUpdateDto>> GetByIdAsync(string id, CancellationToken ct = default);
    Task<Result<IReadOnlyList<GuildUpdateDto>>> GetByGuildIdAsync(string guildId, CancellationToken ct = default);
    Task<Result<GuildUpdateDto>> CreateAsync(CreateGuildUpdateDto dto, CancellationToken ct = default);
    Task<Result<bool>> DeleteAsync(string id, CancellationToken ct = default);
}

public interface IGuildApplicationService
{
    Task<Result<GuildApplicationDto>> GetByIdAsync(string id, CancellationToken ct = default);
    Task<Result<IReadOnlyList<GuildApplicationDto>>> GetByGuildIdAsync(string guildId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<GuildApplicationDto>>> GetByUserIdAsync(string userId, CancellationToken ct = default);
    Task<Result<GuildApplicationDto>> CreateAsync(CreateGuildApplicationDto dto, CancellationToken ct = default);
    Task<Result<GuildApplicationDto>> AcceptAsync(string id, string? reviewMessage, CancellationToken ct = default);
    Task<Result<GuildApplicationDto>> RejectAsync(string id, string? reviewMessage, CancellationToken ct = default);
    Task<Result<GuildApplicationDto>> WithdrawAsync(string id, CancellationToken ct = default);
    Task<Result<bool>> DeleteAsync(string id, CancellationToken ct = default);
}

public interface IGuildInvitationService
{
    Task<Result<GuildInvitationDto>> GetByIdAsync(string id, CancellationToken ct = default);
    Task<Result<IReadOnlyList<GuildInvitationDto>>> GetByGuildIdAsync(string guildId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<GuildInvitationDto>>> GetByUserIdAsync(string userId, CancellationToken ct = default);
    Task<Result<GuildInvitationDto>> CreateAsync(CreateGuildInvitationDto dto, CancellationToken ct = default);
    Task<Result<GuildInvitationDto>> AcceptAsync(string id, CancellationToken ct = default);
    Task<Result<GuildInvitationDto>> RejectAsync(string id, CancellationToken ct = default);
    Task<Result<bool>> DeleteAsync(string id, CancellationToken ct = default);
}

public interface IGuildFollowService
{
    Task<Result<IReadOnlyList<GuildFollowDto>>> GetByGuildIdAsync(string guildId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<GuildFollowDto>>> GetByUserIdAsync(string userId, CancellationToken ct = default);
    Task<Result<GuildFollowDto>> FollowAsync(CreateGuildFollowDto dto, CancellationToken ct = default);
    Task<Result<bool>> UnfollowAsync(string guildId, string userId, CancellationToken ct = default);
    Task<Result<bool>> IsFollowingAsync(string guildId, string userId, CancellationToken ct = default);
}
