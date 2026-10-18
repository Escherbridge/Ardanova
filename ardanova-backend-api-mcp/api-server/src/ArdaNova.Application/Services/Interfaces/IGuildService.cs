namespace ArdaNova.Application.Services.Interfaces;

using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;

/// <summary>Guild profiles. Every mutation takes the signed <see cref="GuildActor"/>; see <c>Services/AGENTS.md</c> §Guild authority.</summary>
public interface IGuildService
{
    Task<Result<GuildDto>> GetByIdAsync(string id, CancellationToken ct = default);
    Task<Result<GuildDto>> GetBySlugAsync(string slug, CancellationToken ct = default);
    Task<Result<IReadOnlyList<GuildDto>>> GetAllAsync(CancellationToken ct = default);
    Task<Result<PagedResult<GuildDto>>> GetPagedAsync(int page, int pageSize, CancellationToken ct = default);
    Task<Result<GuildDto>> GetByOwnerIdAsync(string ownerId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<GuildDto>>> GetVerifiedAsync(CancellationToken ct = default);
    Task<Result<GuildDto>> CreateAsync(GuildActor actor, CreateGuildDto dto, CancellationToken ct = default);
    Task<Result<GuildDto>> UpdateAsync(string id, GuildActor actor, UpdateGuildDto dto, CancellationToken ct = default);
    Task<Result<bool>> DeleteAsync(string id, GuildActor actor, CancellationToken ct = default);
    /// <summary>Platform-operator action; the controller gates it on the admin API key.</summary>
    Task<Result<GuildDto>> VerifyAsync(string id, CancellationToken ct = default);
}

public interface IGuildMemberService
{
    Task<Result<GuildMemberDto>> GetByIdAsync(string guildId, string id, CancellationToken ct = default);
    Task<Result<IReadOnlyList<GuildMemberDto>>> GetByGuildIdAsync(string guildId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<GuildMemberDto>>> GetByUserIdAsync(string userId, CancellationToken ct = default);
    Task<Result<GuildMemberDto>> CreateAsync(string guildId, GuildActor actor, CreateGuildMemberDto dto, CancellationToken ct = default);
    Task<Result<GuildMemberDto>> UpdateAsync(string guildId, string id, GuildActor actor, UpdateGuildMemberDto dto, CancellationToken ct = default);
    Task<Result<bool>> DeleteAsync(string guildId, string id, GuildActor actor, CancellationToken ct = default);
}

public interface IGuildReviewService
{
    Task<Result<GuildReviewDto>> GetByIdAsync(string guildId, string id, CancellationToken ct = default);
    Task<Result<IReadOnlyList<GuildReviewDto>>> GetByGuildIdAsync(string guildId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<GuildReviewDto>>> GetByUserIdAsync(string userId, CancellationToken ct = default);
    Task<Result<GuildReviewDto>> CreateAsync(string guildId, GuildActor actor, CreateGuildReviewDto dto, CancellationToken ct = default);
    Task<Result<GuildReviewDto>> UpdateAsync(string guildId, string id, GuildActor actor, UpdateGuildReviewDto dto, CancellationToken ct = default);
    Task<Result<bool>> DeleteAsync(string guildId, string id, GuildActor actor, CancellationToken ct = default);
}

public interface IGuildUpdateService
{
    Task<Result<GuildUpdateDto>> GetByIdAsync(string guildId, string id, CancellationToken ct = default);
    Task<Result<IReadOnlyList<GuildUpdateDto>>> GetByGuildIdAsync(string guildId, CancellationToken ct = default);
    Task<Result<GuildUpdateDto>> CreateAsync(string guildId, GuildActor actor, CreateGuildUpdateDto dto, CancellationToken ct = default);
    Task<Result<bool>> DeleteAsync(string guildId, string id, GuildActor actor, CancellationToken ct = default);
}

public interface IGuildApplicationService
{
    /// <summary>Readable by the guild's managers and by the applicant.</summary>
    Task<Result<GuildApplicationDto>> GetByIdAsync(string guildId, string id, GuildActor actor, CancellationToken ct = default);
    /// <summary>Readable by the guild's managers only.</summary>
    Task<Result<IReadOnlyList<GuildApplicationDto>>> GetByGuildIdAsync(string guildId, GuildActor actor, CancellationToken ct = default);
    Task<Result<IReadOnlyList<GuildApplicationDto>>> GetByUserIdAsync(string userId, CancellationToken ct = default);
    Task<Result<GuildApplicationDto>> CreateAsync(string guildId, GuildActor actor, CreateGuildApplicationDto dto, CancellationToken ct = default);
    Task<Result<GuildApplicationDto>> AcceptAsync(string guildId, string id, GuildActor actor, string? reviewMessage, CancellationToken ct = default);
    Task<Result<GuildApplicationDto>> RejectAsync(string guildId, string id, GuildActor actor, string? reviewMessage, CancellationToken ct = default);
    Task<Result<GuildApplicationDto>> WithdrawAsync(string guildId, string id, GuildActor actor, CancellationToken ct = default);
    Task<Result<bool>> DeleteAsync(string guildId, string id, GuildActor actor, CancellationToken ct = default);
}

public interface IGuildInvitationService
{
    /// <summary>Readable by the guild's managers and by the invitee.</summary>
    Task<Result<GuildInvitationDto>> GetByIdAsync(string guildId, string id, GuildActor actor, CancellationToken ct = default);
    /// <summary>Readable by the guild's managers only.</summary>
    Task<Result<IReadOnlyList<GuildInvitationDto>>> GetByGuildIdAsync(string guildId, GuildActor actor, CancellationToken ct = default);
    /// <summary>The actor's own invitations that can still be answered.</summary>
    Task<Result<IReadOnlyList<GuildInvitationDto>>> GetPendingForUserAsync(string userId, CancellationToken ct = default);
    Task<Result<GuildInvitationDto>> CreateAsync(string guildId, GuildActor actor, CreateGuildInvitationDto dto, CancellationToken ct = default);
    Task<Result<GuildInvitationDto>> AcceptAsync(string guildId, string id, GuildActor actor, CancellationToken ct = default);
    Task<Result<GuildInvitationDto>> RejectAsync(string guildId, string id, GuildActor actor, CancellationToken ct = default);
    Task<Result<bool>> DeleteAsync(string guildId, string id, GuildActor actor, CancellationToken ct = default);
}

public interface IGuildFollowService
{
    Task<Result<IReadOnlyList<GuildFollowDto>>> GetByGuildIdAsync(string guildId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<GuildFollowDto>>> GetByUserIdAsync(string userId, CancellationToken ct = default);
    Task<Result<GuildFollowDto>> FollowAsync(string guildId, GuildActor actor, CreateGuildFollowDto dto, CancellationToken ct = default);
    Task<Result<bool>> UnfollowAsync(string guildId, GuildActor actor, CancellationToken ct = default);
    Task<Result<bool>> IsFollowingAsync(string guildId, string userId, CancellationToken ct = default);
}
