namespace ArdaNova.Application.Services.Implementations;

using System.Data.Common;
using ArdaNova.Application.Common.Interfaces;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Interfaces;
using ArdaNova.Domain.Models.Entities;
using ArdaNova.Domain.Models.Enums;
using AutoMapper;
using Microsoft.EntityFrameworkCore;

/// <summary>Who may act on a guild. One rule set for every guild service; rationale in <c>Services/AGENTS.md</c> §Guild authority.</summary>
internal static class GuildAuthority
{
    /// <summary>The guild's owner: the profile's owner id, an OWNER membership row, or a platform administrator.</summary>
    public static async Task<bool> IsOwnerAsync(
        Guild guild, GuildActor actor, IRepository<GuildMember> members, CancellationToken ct)
    {
        if (actor.IsPlatformAdmin)
            return true;
        if (string.IsNullOrWhiteSpace(actor.Id))
            return false;
        if (string.Equals(guild.ownerId, actor.Id, StringComparison.Ordinal))
            return true;

        var guildId = guild.id;
        var actorId = actor.Id;
        return await members.ExistsAsync(
            m => m.guildId == guildId && m.userId == actorId && m.role == GuildMemberRole.OWNER, ct);
    }

    /// <summary>The owner, or a member holding the ADMIN role.</summary>
    public static async Task<bool> CanManageAsync(
        Guild guild, GuildActor actor, IRepository<GuildMember> members, CancellationToken ct)
    {
        if (await IsOwnerAsync(guild, actor, members, ct))
            return true;
        if (string.IsNullOrWhiteSpace(actor.Id))
            return false;

        var guildId = guild.id;
        var actorId = actor.Id;
        return await members.ExistsAsync(
            m => m.guildId == guildId && m.userId == actorId && m.role == GuildMemberRole.ADMIN, ct);
    }

    /// <summary>Whether <paramref name="userId"/> already belongs to the guild: its owner of record (even with no OWNER row, as on legacy guilds) or any membership row.</summary>
    public static async Task<bool> IsMemberAsync(
        Guild guild, string userId, IRepository<GuildMember> members, CancellationToken ct)
    {
        if (string.Equals(guild.ownerId, userId, StringComparison.Ordinal))
            return true;

        var guildId = guild.id;
        return await members.ExistsAsync(m => m.guildId == guildId && m.userId == userId, ct);
    }

    /// <summary>Parses a role a caller may hand out. OWNER is never grantable: ownership moves only by an explicit transfer, which does not exist yet.</summary>
    public static bool TryParseGrantableRole(string? value, out GuildMemberRole role)
    {
        role = GuildMemberRole.MEMBER;
        if (string.IsNullOrWhiteSpace(value) ||
            !Enum.TryParse(value.Trim(), ignoreCase: true, out GuildMemberRole parsed) ||
            !Enum.IsDefined(parsed) ||
            parsed == GuildMemberRole.OWNER)
            return false;

        role = parsed;
        return true;
    }

    /// <summary>A (guildId, userId) unique index (membership or review) refused a second row: a concurrent request stored this person first.</summary>
    public static bool IsDuplicateGuildUserRow(DbUpdateException failure) =>
        failure.InnerException is DbException { SqlState: "23505" };

    public const string GrantableRolesMessage = "Role must be one of ADMIN, MANAGER, MEMBER or APPRENTICE";
}

public class GuildService : IGuildService
{
    private readonly IRepository<Guild> _repository;
    private readonly IRepository<GuildMember> _memberRepository;
    private readonly IRepository<ProjectTask> _taskRepository;
    private readonly IRepository<Project>? _projectRepository;
    private readonly IRepository<GuildReview>? _reviewRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GuildService(
        IRepository<Guild> repository,
        IRepository<GuildMember> memberRepository,
        IRepository<ProjectTask> taskRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IRepository<Project>? projectRepository = null,
        IRepository<GuildReview>? reviewRepository = null)
    {
        _repository = repository;
        _memberRepository = memberRepository;
        _taskRepository = taskRepository;
        _projectRepository = projectRepository;
        _reviewRepository = reviewRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    /// <summary>ProjectsCount, ReviewsCount and Rating are counted from the rows themselves, never read from stored counters that concurrent writes can leave stale.</summary>
    private async Task<IReadOnlyList<GuildDto>> WithProjectCountsAsync(IReadOnlyList<GuildDto> guilds, CancellationToken ct)
    {
        if (guilds.Count == 0)
            return guilds;

        var guildIds = guilds.Select(g => g.Id).ToList();
        var projectCounts = new Dictionary<string, int>();
        if (_projectRepository is not null)
        {
            var assigned = await _projectRepository.FindProjectedAsync(
                p => p.assignedGuildId != null && guildIds.Contains(p.assignedGuildId),
                p => p.assignedGuildId!,
                ct);
            projectCounts = assigned.GroupBy(id => id).ToDictionary(g => g.Key, g => g.Count());
        }

        Dictionary<string, List<int>>? ratings = null;
        if (_reviewRepository is not null)
        {
            var reviews = await _reviewRepository.FindProjectedAsync(
                r => guildIds.Contains(r.guildId),
                r => new { r.guildId, r.rating },
                ct);
            ratings = reviews.GroupBy(r => r.guildId).ToDictionary(g => g.Key, g => g.Select(r => r.rating).ToList());
        }

        return guilds.Select(g =>
        {
            var derived = g with { ProjectsCount = g.Id is null ? 0 : projectCounts.GetValueOrDefault(g.Id) };
            if (ratings is null || g.Id is null)
                return derived;
            var guildRatings = ratings.GetValueOrDefault(g.Id);
            return derived with
            {
                ReviewsCount = guildRatings?.Count ?? 0,
                Rating = guildRatings is { Count: > 0 } ? (decimal)guildRatings.Sum() / guildRatings.Count : null
            };
        }).ToList();
    }

    private async Task<GuildDto> WithProjectCountAsync(GuildDto guild, CancellationToken ct)
        => (await WithProjectCountsAsync(new[] { guild }, ct))[0];

    public async Task<Result<GuildDto>> GetByIdAsync(string id, CancellationToken ct = default)
    {
        var guild = await _repository.GetByIdAsync(id, ct);
        if (guild is null)
            return Result<GuildDto>.NotFound($"Guild with id {id} not found");
        return Result<GuildDto>.Success(await WithProjectCountAsync(_mapper.Map<GuildDto>(guild), ct));
    }

    public async Task<Result<GuildDto>> GetBySlugAsync(string slug, CancellationToken ct = default)
    {
        var guild = await _repository.FindOneAsync(g => g.slug == slug, ct);
        if (guild is null)
            return Result<GuildDto>.NotFound($"Guild with slug {slug} not found");
        return Result<GuildDto>.Success(await WithProjectCountAsync(_mapper.Map<GuildDto>(guild), ct));
    }

    public async Task<Result<IReadOnlyList<GuildDto>>> GetAllAsync(CancellationToken ct = default)
    {
        var guilds = await _repository.GetAllAsync(ct);
        return Result<IReadOnlyList<GuildDto>>.Success(await WithProjectCountsAsync(_mapper.Map<IReadOnlyList<GuildDto>>(guilds), ct));
    }

    public async Task<Result<PagedResult<GuildDto>>> GetPagedAsync(int page, int pageSize, CancellationToken ct = default)
    {
        var result = await _repository.GetPagedAsync(page, pageSize, null, ct);
        var items = await WithProjectCountsAsync(result.Items.Select(_mapper.Map<GuildDto>).ToList(), ct);
        return Result<PagedResult<GuildDto>>.Success(new PagedResult<GuildDto>(items.ToList(), result.TotalCount, result.Page, result.PageSize));
    }

    public async Task<Result<GuildDto>> GetByOwnerIdAsync(string ownerId, CancellationToken ct = default)
    {
        var guild = await _repository.FindOneAsync(g => g.ownerId == ownerId, ct);
        if (guild is null)
            return Result<GuildDto>.NotFound($"Guild with owner {ownerId} not found");
        return Result<GuildDto>.Success(await WithProjectCountAsync(_mapper.Map<GuildDto>(guild), ct));
    }

    public async Task<Result<IReadOnlyList<GuildDto>>> GetVerifiedAsync(CancellationToken ct = default)
    {
        var guilds = await _repository.FindAsync(g => g.isVerified, ct);
        return Result<IReadOnlyList<GuildDto>>.Success(await WithProjectCountsAsync(_mapper.Map<IReadOnlyList<GuildDto>>(guilds), ct));
    }

    private static string GenerateSlug(string name)
    {
        return name.ToLowerInvariant()
            .Replace(" ", "-")
            .Replace("--", "-")
            + "-" + Guid.NewGuid().ToString("N")[..8];
    }

    /// <summary>The signed actor founds the guild and becomes its OWNER row in the same save. One guild per owner.</summary>
    public async Task<Result<GuildDto>> CreateAsync(GuildActor actor, CreateGuildDto dto, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(actor.Id))
            return Result<GuildDto>.Unauthorized("A guild must be founded by a signed-in member");
        if (string.IsNullOrWhiteSpace(dto.Name) || string.IsNullOrWhiteSpace(dto.Description) || string.IsNullOrWhiteSpace(dto.Email))
            return Result<GuildDto>.ValidationError("A guild needs a name, a description and a contact email");

        var ownerId = actor.Id;
        if (await _repository.ExistsAsync(g => g.ownerId == ownerId, ct))
            return Result<GuildDto>.Conflict("You already own a guild");

        var guild = new Guild
        {
            id = Guid.NewGuid().ToString(),
            ownerId = ownerId,
            name = dto.Name.Trim(),
            slug = GenerateSlug(dto.Name.Trim()),
            description = dto.Description,
            email = dto.Email.Trim(),
            website = dto.Website,
            phone = dto.Phone,
            address = dto.Address,
            logo = dto.Logo,
            portfolio = dto.Portfolio,
            specialties = dto.Specialties,
            isVerified = false,
            reviewsCount = 0,
            projectsCount = 0,
            membersCount = 1,
            createdAt = DateTime.UtcNow,
            updatedAt = DateTime.UtcNow
        };

        await _repository.AddAsync(guild, ct);

        var ownerMembership = new GuildMember
        {
            id = Guid.NewGuid().ToString(),
            guildId = guild.id,
            userId = ownerId,
            role = GuildMemberRole.OWNER,
            joinedAt = DateTime.UtcNow
        };
        await _memberRepository.AddAsync(ownerMembership, ct);

        await _unitOfWork.SaveChangesAsync(ct);
        return Result<GuildDto>.Success(_mapper.Map<GuildDto>(guild));
    }

    public async Task<Result<GuildDto>> UpdateAsync(string id, GuildActor actor, UpdateGuildDto dto, CancellationToken ct = default)
    {
        var guild = await _repository.GetByIdAsync(id, ct);
        if (guild is null)
            return Result<GuildDto>.NotFound($"Guild with id {id} not found");
        if (!await GuildAuthority.CanManageAsync(guild, actor, _memberRepository, ct))
            return Result<GuildDto>.Forbidden("Only the guild's owner or admins can edit its profile");

        if (dto.Name is not null) guild.name = dto.Name;
        if (dto.Description is not null) guild.description = dto.Description;
        if (dto.Email is not null) guild.email = dto.Email;
        if (dto.Website is not null) guild.website = dto.Website;
        if (dto.Phone is not null) guild.phone = dto.Phone;
        if (dto.Address is not null) guild.address = dto.Address;
        if (dto.Logo is not null) guild.logo = dto.Logo;
        if (dto.Portfolio is not null) guild.portfolio = dto.Portfolio;
        if (dto.Specialties is not null) guild.specialties = dto.Specialties;
        guild.updatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(guild, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<GuildDto>.Success(_mapper.Map<GuildDto>(guild));
    }

    public async Task<Result<bool>> DeleteAsync(string id, GuildActor actor, CancellationToken ct = default)
    {
        var guild = await _repository.GetByIdAsync(id, ct);
        if (guild is null)
            return Result<bool>.NotFound($"Guild with id {id} not found");

        if (!await GuildAuthority.IsOwnerAsync(guild, actor, _memberRepository, ct))
            return Result<bool>.Forbidden("Only the guild owner can delete a guild");

        var outstandingTasks = await _taskRepository.FindAsync(
            t => t.guildId == id && t.status != Domain.Models.Enums.TaskStatus.COMPLETED && t.status != Domain.Models.Enums.TaskStatus.BLOCKED, ct)
            ?? Array.Empty<ProjectTask>();
        if (outstandingTasks.Any())
            return Result<bool>.BadRequest($"Cannot delete guild with {outstandingTasks.Count} outstanding tasks. Reassign or complete them first.");

        await _repository.DeleteAsync(guild, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<bool>.Success(true);
    }

    public async Task<Result<GuildDto>> VerifyAsync(string id, CancellationToken ct = default)
    {
        var guild = await _repository.GetByIdAsync(id, ct);
        if (guild is null)
            return Result<GuildDto>.NotFound($"Guild with id {id} not found");

        guild.isVerified = true;
        guild.updatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(guild, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<GuildDto>.Success(_mapper.Map<GuildDto>(guild));
    }
}

public class GuildMemberService : IGuildMemberService
{
    private readonly IRepository<GuildMember> _repository;
    private readonly IRepository<Guild> _guildRepository;
    private readonly IRepository<User> _userRepository;
    private readonly IUnitOfWork _unitOfWork;

    public GuildMemberService(
        IRepository<GuildMember> repository,
        IRepository<Guild> guildRepository,
        IRepository<User> userRepository,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _guildRepository = guildRepository;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
    }

    internal static GuildMemberDto ToDto(GuildMember member, User? user) => new()
    {
        Id = member.id,
        GuildId = member.guildId,
        UserId = member.userId,
        Role = member.role,
        JoinedAt = member.joinedAt,
        User = user is null ? null : new GuildMemberUserDto { Id = user.id, Name = user.name, Image = user.image }
    };

    public async Task<Result<GuildMemberDto>> GetByIdAsync(string guildId, string id, CancellationToken ct = default)
    {
        var member = await _repository.GetByIdAsync(id, ct);
        if (member is null || member.guildId != guildId)
            return Result<GuildMemberDto>.NotFound($"Member with id {id} not found in this guild");
        return Result<GuildMemberDto>.Success(ToDto(member, await _userRepository.GetByIdAsync(member.userId, ct)));
    }

    public async Task<Result<IReadOnlyList<GuildMemberDto>>> GetByGuildIdAsync(string guildId, CancellationToken ct = default)
    {
        var members = await _repository.FindAsync(m => m.guildId == guildId, ct);
        var userIds = members.Select(m => m.userId).Distinct().ToList();
        var users = userIds.Count == 0
            ? new Dictionary<string, User>()
            : (await _userRepository.FindAsync(u => userIds.Contains(u.id), ct)).ToDictionary(u => u.id);

        var dtos = members
            .OrderBy(m => m.role)
            .ThenBy(m => m.joinedAt)
            .Select(m => ToDto(m, users.GetValueOrDefault(m.userId)))
            .ToList();
        return Result<IReadOnlyList<GuildMemberDto>>.Success(dtos);
    }

    public async Task<Result<IReadOnlyList<GuildMemberDto>>> GetByUserIdAsync(string userId, CancellationToken ct = default)
    {
        var members = await _repository.FindAsync(m => m.userId == userId, ct);
        return Result<IReadOnlyList<GuildMemberDto>>.Success(members.Select(m => ToDto(m, null)).ToList());
    }

    /// <summary>Platform-operator repair path only: everyone else joins by accepting an invitation or an approved application, so nobody is listed in a guild without consenting.</summary>
    public async Task<Result<GuildMemberDto>> CreateAsync(string guildId, GuildActor actor, CreateGuildMemberDto dto, CancellationToken ct = default)
    {
        var guild = await _guildRepository.GetByIdAsync(guildId, ct);
        if (guild is null)
            return Result<GuildMemberDto>.NotFound($"Guild with id {guildId} not found");
        if (!actor.IsPlatformAdmin)
            return Result<GuildMemberDto>.Forbidden("People join a guild by accepting an invitation or through an approved application");
        if (dto.Role == GuildMemberRole.OWNER || !Enum.IsDefined(dto.Role))
            return Result<GuildMemberDto>.ValidationError(GuildAuthority.GrantableRolesMessage);

        var user = await _userRepository.GetByIdAsync(dto.UserId, ct);
        if (user is null)
            return Result<GuildMemberDto>.NotFound("That user does not exist");

        var userId = dto.UserId;
        if (await GuildAuthority.IsMemberAsync(guild, userId, _repository, ct))
            return Result<GuildMemberDto>.Conflict("That person is already a member of this guild");

        var member = new GuildMember
        {
            id = Guid.NewGuid().ToString(),
            guildId = guildId,
            userId = userId,
            role = dto.Role,
            joinedAt = DateTime.UtcNow
        };

        await _repository.AddAsync(member, ct);
        guild.membersCount++;
        await _guildRepository.UpdateAsync(guild, ct);
        try
        {
            await _unitOfWork.SaveChangesAsync(ct);
        }
        catch (DbUpdateException failure) when (GuildAuthority.IsDuplicateGuildUserRow(failure))
        {
            _unitOfWork.ClearTrackedChanges();
            return Result<GuildMemberDto>.Conflict("That person is already a member of this guild");
        }
        return Result<GuildMemberDto>.Success(ToDto(member, user));
    }

    /// <summary>Changes a member's role inside the routed guild. The OWNER row is immovable here, and only the owner moves people in or out of ADMIN.</summary>
    public async Task<Result<GuildMemberDto>> UpdateAsync(string guildId, string id, GuildActor actor, UpdateGuildMemberDto dto, CancellationToken ct = default)
    {
        var guild = await _guildRepository.GetByIdAsync(guildId, ct);
        if (guild is null)
            return Result<GuildMemberDto>.NotFound($"Guild with id {guildId} not found");

        // Scoped by (guildId, id): a member id from another guild is simply not found here.
        var member = await _repository.GetByIdAsync(id, ct);
        if (member is null || member.guildId != guildId)
            return Result<GuildMemberDto>.NotFound($"Member with id {id} not found in this guild");

        if (!await GuildAuthority.CanManageAsync(guild, actor, _repository, ct))
            return Result<GuildMemberDto>.Forbidden("Only the guild's owner or admins can change roles");

        if (!dto.Role.HasValue)
            return Result<GuildMemberDto>.Success(ToDto(member, null));

        var newRole = dto.Role.Value;
        if (newRole == GuildMemberRole.OWNER || !Enum.IsDefined(newRole))
            return Result<GuildMemberDto>.ValidationError(GuildAuthority.GrantableRolesMessage);
        if (member.role == GuildMemberRole.OWNER || member.userId == guild.ownerId)
            return Result<GuildMemberDto>.ValidationError("The owner's role cannot be changed here");

        var touchesAdmin = newRole == GuildMemberRole.ADMIN || member.role == GuildMemberRole.ADMIN;
        if (touchesAdmin && !await GuildAuthority.IsOwnerAsync(guild, actor, _repository, ct))
            return Result<GuildMemberDto>.Forbidden("Only the guild owner can appoint or demote admins");

        member.role = newRole;
        await _repository.UpdateAsync(member, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<GuildMemberDto>.Success(ToDto(member, null));
    }

    /// <summary>Removes a member from the routed guild: managers remove others, anyone may leave, and the owner row never goes.</summary>
    public async Task<Result<bool>> DeleteAsync(string guildId, string id, GuildActor actor, CancellationToken ct = default)
    {
        var guild = await _guildRepository.GetByIdAsync(guildId, ct);
        if (guild is null)
            return Result<bool>.NotFound($"Guild with id {guildId} not found");

        var member = await _repository.GetByIdAsync(id, ct);
        if (member is null || member.guildId != guildId)
            return Result<bool>.NotFound($"Member with id {id} not found in this guild");

        if (member.role == GuildMemberRole.OWNER || member.userId == guild.ownerId)
            return Result<bool>.BadRequest("Cannot remove the guild owner. Transfer ownership first.");

        var isLeaving = !string.IsNullOrWhiteSpace(actor.Id) && member.userId == actor.Id;
        if (!isLeaving)
        {
            if (!await GuildAuthority.CanManageAsync(guild, actor, _repository, ct))
                return Result<bool>.Forbidden("Only the guild's owner or admins can remove members");
            if (member.role == GuildMemberRole.ADMIN && !await GuildAuthority.IsOwnerAsync(guild, actor, _repository, ct))
                return Result<bool>.Forbidden("Only the guild owner can remove an admin");
        }

        await _repository.DeleteAsync(member, ct);
        guild.membersCount = Math.Max(0, guild.membersCount - 1);
        await _guildRepository.UpdateAsync(guild, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<bool>.Success(true);
    }
}

public class GuildReviewService : IGuildReviewService
{
    private readonly IRepository<GuildReview> _repository;
    private readonly IRepository<Guild> _guildRepository;
    private readonly IRepository<GuildMember> _memberRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IRepository<Project>? _projectRepository;

    public GuildReviewService(
        IRepository<GuildReview> repository,
        IRepository<Guild> guildRepository,
        IRepository<GuildMember> memberRepository,
        IUnitOfWork unitOfWork,
        IRepository<Project>? projectRepository = null)
    {
        _repository = repository;
        _guildRepository = guildRepository;
        _memberRepository = memberRepository;
        _unitOfWork = unitOfWork;
        _projectRepository = projectRepository;
    }

    private static GuildReviewDto ToDto(GuildReview review) => new()
    {
        Id = review.id,
        GuildId = review.guildId,
        ProjectId = review.projectId,
        UserId = review.userId,
        Rating = review.rating,
        Comment = review.comment,
        CreatedAt = review.createdAt
    };

    public async Task<Result<GuildReviewDto>> GetByIdAsync(string guildId, string id, CancellationToken ct = default)
    {
        var review = await _repository.GetByIdAsync(id, ct);
        if (review is null || review.guildId != guildId)
            return Result<GuildReviewDto>.NotFound($"Review with id {id} not found in this guild");
        return Result<GuildReviewDto>.Success(ToDto(review));
    }

    public async Task<Result<IReadOnlyList<GuildReviewDto>>> GetByGuildIdAsync(string guildId, CancellationToken ct = default)
    {
        var reviews = await _repository.FindAsync(r => r.guildId == guildId, ct);
        return Result<IReadOnlyList<GuildReviewDto>>.Success(reviews.OrderByDescending(r => r.createdAt).Select(ToDto).ToList());
    }

    public async Task<Result<IReadOnlyList<GuildReviewDto>>> GetByUserIdAsync(string userId, CancellationToken ct = default)
    {
        var reviews = await _repository.FindAsync(r => r.userId == userId, ct);
        return Result<IReadOnlyList<GuildReviewDto>>.Success(reviews.Select(ToDto).ToList());
    }

    /// <summary>One review per person per guild, never by the guild's own members; the (guildId, userId) unique index settles a race. See Services/AGENTS.md §Guild reviews.</summary>
    public async Task<Result<GuildReviewDto>> CreateAsync(string guildId, GuildActor actor, CreateGuildReviewDto dto, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(actor.Id))
            return Result<GuildReviewDto>.Unauthorized("A review must be written by a signed-in member");
        if (dto.Rating < 1 || dto.Rating > 5)
            return Result<GuildReviewDto>.ValidationError("Rating must be between 1 and 5");

        var guild = await _guildRepository.GetByIdAsync(guildId, ct);
        if (guild is null)
            return Result<GuildReviewDto>.NotFound($"Guild with id {guildId} not found");

        var reviewerId = actor.Id;
        if (await GuildAuthority.IsMemberAsync(guild, reviewerId, _memberRepository, ct))
            return Result<GuildReviewDto>.Forbidden("Members cannot review their own guild");

        if (!string.IsNullOrEmpty(dto.ProjectId) && !await IsReviewersProjectWithGuildAsync(dto.ProjectId, guildId, reviewerId, ct))
            return Result<GuildReviewDto>.ValidationError("A review can only name a project of yours that this guild was assigned to");

        if (await _repository.ExistsAsync(r => r.guildId == guildId && r.userId == reviewerId, ct))
            return Result<GuildReviewDto>.Conflict(AlreadyReviewed);

        var review = new GuildReview
        {
            id = Guid.NewGuid().ToString(),
            guildId = guildId,
            userId = reviewerId,
            rating = dto.Rating,
            comment = dto.Comment,
            projectId = string.IsNullOrEmpty(dto.ProjectId) ? null : dto.ProjectId,
            createdAt = DateTime.UtcNow
        };

        await _repository.AddAsync(review, ct);
        try
        {
            await _unitOfWork.SaveChangesAsync(ct);
        }
        catch (DbUpdateException failure) when (GuildAuthority.IsDuplicateGuildUserRow(failure))
        {
            _unitOfWork.ClearTrackedChanges();
            return Result<GuildReviewDto>.Conflict(AlreadyReviewed);
        }

        // Recounted from the stored rows after the insert, so a concurrent review is included rather than overwritten.
        await RecalculateAsync(guildId, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<GuildReviewDto>.Success(ToDto(review));
    }

    private const string AlreadyReviewed = "You have already reviewed this guild";

    /// <summary>The named project exists, the guild was assigned to it, and the reviewer created it.</summary>
    private async Task<bool> IsReviewersProjectWithGuildAsync(string projectId, string guildId, string reviewerId, CancellationToken ct)
    {
        if (_projectRepository is null)
            return false;
        var project = await _projectRepository.GetByIdAsync(projectId, ct);
        return project is not null
            && project.assignedGuildId == guildId
            && project.createdById == reviewerId;
    }

    public async Task<Result<GuildReviewDto>> UpdateAsync(string guildId, string id, GuildActor actor, UpdateGuildReviewDto dto, CancellationToken ct = default)
    {
        var review = await _repository.GetByIdAsync(id, ct);
        if (review is null || review.guildId != guildId)
            return Result<GuildReviewDto>.NotFound($"Review with id {id} not found in this guild");
        if (review.userId != actor.Id)
            return Result<GuildReviewDto>.Forbidden("Only the reviewer can edit this review");

        if (dto.Rating.HasValue)
        {
            if (dto.Rating.Value < 1 || dto.Rating.Value > 5)
                return Result<GuildReviewDto>.BadRequest("Rating must be between 1 and 5");
            review.rating = dto.Rating.Value;
        }
        if (dto.Comment is not null) review.comment = dto.Comment;

        await _repository.UpdateAsync(review, ct);
        await RecalculateAsync(guildId, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<GuildReviewDto>.Success(ToDto(review));
    }

    public async Task<Result<bool>> DeleteAsync(string guildId, string id, GuildActor actor, CancellationToken ct = default)
    {
        var review = await _repository.GetByIdAsync(id, ct);
        if (review is null || review.guildId != guildId)
            return Result<bool>.NotFound($"Review with id {id} not found in this guild");
        if (review.userId != actor.Id && !actor.IsPlatformAdmin)
            return Result<bool>.Forbidden("Only the reviewer can delete this review");

        await _repository.DeleteAsync(review, ct);
        await RecalculateAsync(guildId, ct, excludeId: id);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<bool>.Success(true);
    }

    private async Task RecalculateAsync(string guildId, CancellationToken ct, string? excludeId = null)
    {
        var guild = await _guildRepository.GetByIdAsync(guildId, ct);
        if (guild is null)
            return;

        var remaining = (await _repository.FindAsync(r => r.guildId == guildId, ct))
            .Where(r => r.id != excludeId)
            .ToList();
        guild.reviewsCount = remaining.Count;
        guild.rating = remaining.Count > 0 ? (decimal)remaining.Sum(r => r.rating) / remaining.Count : null;
        await _guildRepository.UpdateAsync(guild, ct);
    }
}

public class GuildUpdateService : IGuildUpdateService
{
    private readonly IRepository<GuildUpdate> _repository;
    private readonly IRepository<Guild> _guildRepository;
    private readonly IRepository<GuildMember> _memberRepository;
    private readonly IUnitOfWork _unitOfWork;

    public GuildUpdateService(
        IRepository<GuildUpdate> repository,
        IRepository<Guild> guildRepository,
        IRepository<GuildMember> memberRepository,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _guildRepository = guildRepository;
        _memberRepository = memberRepository;
        _unitOfWork = unitOfWork;
    }

    private static GuildUpdateDto ToDto(GuildUpdate update) => new()
    {
        Id = update.id,
        GuildId = update.guildId,
        Title = update.title,
        Content = update.content,
        Images = update.images,
        CreatedAt = update.createdAt,
        CreatedById = update.createdById
    };

    public async Task<Result<GuildUpdateDto>> GetByIdAsync(string guildId, string id, CancellationToken ct = default)
    {
        var update = await _repository.GetByIdAsync(id, ct);
        if (update is null || update.guildId != guildId)
            return Result<GuildUpdateDto>.NotFound($"Update with id {id} not found in this guild");
        return Result<GuildUpdateDto>.Success(ToDto(update));
    }

    public async Task<Result<IReadOnlyList<GuildUpdateDto>>> GetByGuildIdAsync(string guildId, CancellationToken ct = default)
    {
        var updates = await _repository.FindAsync(u => u.guildId == guildId, ct);
        return Result<IReadOnlyList<GuildUpdateDto>>.Success(updates.OrderByDescending(u => u.createdAt).Select(ToDto).ToList());
    }

    public async Task<Result<GuildUpdateDto>> CreateAsync(string guildId, GuildActor actor, CreateGuildUpdateDto dto, CancellationToken ct = default)
    {
        var guild = await _guildRepository.GetByIdAsync(guildId, ct);
        if (guild is null)
            return Result<GuildUpdateDto>.NotFound($"Guild with id {guildId} not found");
        if (!await GuildAuthority.CanManageAsync(guild, actor, _memberRepository, ct))
            return Result<GuildUpdateDto>.Forbidden("Only the guild's owner or admins can post updates");
        if (string.IsNullOrWhiteSpace(dto.Title) || string.IsNullOrWhiteSpace(dto.Content))
            return Result<GuildUpdateDto>.ValidationError("An update needs a title and some content");

        var update = new GuildUpdate
        {
            id = Guid.NewGuid().ToString(),
            guildId = guildId,
            title = dto.Title,
            content = dto.Content,
            images = dto.Images,
            createdById = actor.Id,
            createdAt = DateTime.UtcNow
        };

        await _repository.AddAsync(update, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<GuildUpdateDto>.Success(ToDto(update));
    }

    public async Task<Result<bool>> DeleteAsync(string guildId, string id, GuildActor actor, CancellationToken ct = default)
    {
        var guild = await _guildRepository.GetByIdAsync(guildId, ct);
        if (guild is null)
            return Result<bool>.NotFound($"Guild with id {guildId} not found");
        var update = await _repository.GetByIdAsync(id, ct);
        if (update is null || update.guildId != guildId)
            return Result<bool>.NotFound($"Update with id {id} not found in this guild");
        if (!await GuildAuthority.CanManageAsync(guild, actor, _memberRepository, ct))
            return Result<bool>.Forbidden("Only the guild's owner or admins can delete updates");

        await _repository.DeleteAsync(update, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<bool>.Success(true);
    }
}

public class GuildApplicationService : IGuildApplicationService
{
    private readonly IRepository<GuildApplication> _repository;
    private readonly IRepository<GuildMember> _memberRepository;
    private readonly IRepository<Guild> _guildRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IRepository<User>? _userRepository;

    public GuildApplicationService(
        IRepository<GuildApplication> repository,
        IRepository<GuildMember> memberRepository,
        IRepository<Guild> guildRepository,
        IUnitOfWork unitOfWork,
        IRepository<User>? userRepository = null)
    {
        _repository = repository;
        _memberRepository = memberRepository;
        _guildRepository = guildRepository;
        _unitOfWork = unitOfWork;
        _userRepository = userRepository;
    }

    private static GuildApplicationDto ToDto(GuildApplication application) => new()
    {
        Id = application.id,
        GuildId = application.guildId,
        UserId = application.userId,
        RequestedRole = application.requestedRole.ToString(),
        Message = application.message,
        Skills = application.skills,
        Experience = application.experience,
        Portfolio = application.portfolio,
        Availability = application.availability,
        Status = application.status.ToString(),
        AppliedAt = application.appliedAt,
        ReviewedAt = application.reviewedAt,
        ReviewMessage = application.reviewMessage,
        ReviewedById = application.reviewedById
    };

    private async Task<(Guild? Guild, GuildApplication? Application)> LoadAsync(string guildId, string id, CancellationToken ct)
    {
        var guild = await _guildRepository.GetByIdAsync(guildId, ct);
        if (guild is null)
            return (null, null);
        var application = await _repository.GetByIdAsync(id, ct);
        return (guild, application is not null && application.guildId == guildId ? application : null);
    }

    public async Task<Result<GuildApplicationDto>> GetByIdAsync(string guildId, string id, GuildActor actor, CancellationToken ct = default)
    {
        var (guild, application) = await LoadAsync(guildId, id, ct);
        if (guild is null || application is null)
            return Result<GuildApplicationDto>.NotFound($"Application with id {id} not found in this guild");
        if (application.userId != actor.Id && !await GuildAuthority.CanManageAsync(guild, actor, _memberRepository, ct))
            return Result<GuildApplicationDto>.Forbidden("Only the applicant or the guild's managers can read this application");
        return Result<GuildApplicationDto>.Success(ToDto(application));
    }

    public async Task<Result<IReadOnlyList<GuildApplicationDto>>> GetByGuildIdAsync(string guildId, GuildActor actor, CancellationToken ct = default)
    {
        var guild = await _guildRepository.GetByIdAsync(guildId, ct);
        if (guild is null)
            return Result<IReadOnlyList<GuildApplicationDto>>.NotFound($"Guild with id {guildId} not found");
        if (!await GuildAuthority.CanManageAsync(guild, actor, _memberRepository, ct))
            return Result<IReadOnlyList<GuildApplicationDto>>.Forbidden("Only the guild's owner or admins can read applications");

        var applications = await _repository.FindAsync(a => a.guildId == guildId, ct);
        var applicantIds = applications.Select(a => a.userId).Distinct().ToList();
        var applicants = _userRepository is null || applicantIds.Count == 0
            ? new Dictionary<string, User>()
            : (await _userRepository.FindAsync(u => applicantIds.Contains(u.id), ct)).ToDictionary(u => u.id);

        return Result<IReadOnlyList<GuildApplicationDto>>.Success(applications
            .OrderByDescending(a => a.appliedAt)
            .Select(a => ToDto(a) with
            {
                Applicant = applicants.TryGetValue(a.userId, out var user)
                    ? new GuildMemberUserDto { Id = user.id, Name = user.name, Image = user.image }
                    : null
            })
            .ToList());
    }

    public async Task<Result<IReadOnlyList<GuildApplicationDto>>> GetByUserIdAsync(string userId, CancellationToken ct = default)
    {
        var applications = await _repository.FindAsync(a => a.userId == userId, ct);
        return Result<IReadOnlyList<GuildApplicationDto>>.Success(applications.Select(ToDto).ToList());
    }

    public async Task<Result<IReadOnlyList<GuildApplicationDto>>> GetMineAsync(string guildId, GuildActor actor, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(actor.Id))
            return Result<IReadOnlyList<GuildApplicationDto>>.Success(Array.Empty<GuildApplicationDto>());

        var applicantId = actor.Id;
        var applications = await _repository.FindAsync(a => a.guildId == guildId && a.userId == applicantId, ct);
        return Result<IReadOnlyList<GuildApplicationDto>>.Success(
            applications.OrderByDescending(a => a.appliedAt).Select(ToDto).ToList());
    }

    public async Task<Result<GuildApplicationDto>> CreateAsync(string guildId, GuildActor actor, CreateGuildApplicationDto dto, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(actor.Id))
            return Result<GuildApplicationDto>.Unauthorized("An application must come from a signed-in member");
        if (!GuildAuthority.TryParseGrantableRole(dto.RequestedRole, out var requestedRole))
            return Result<GuildApplicationDto>.ValidationError(GuildAuthority.GrantableRolesMessage);

        var guild = await _guildRepository.GetByIdAsync(guildId, ct);
        if (guild is null)
            return Result<GuildApplicationDto>.NotFound($"Guild with id {guildId} not found");

        var applicantId = actor.Id;
        if (await GuildAuthority.IsMemberAsync(guild, applicantId, _memberRepository, ct))
            return Result<GuildApplicationDto>.Conflict("You are already a member of this guild");
        if (await _repository.ExistsAsync(a => a.guildId == guildId && a.userId == applicantId && a.status == MembershipRequestStatus.PENDING, ct))
            return Result<GuildApplicationDto>.Conflict("You already have an application waiting for this guild");

        var application = new GuildApplication
        {
            id = Guid.NewGuid().ToString(),
            guildId = guildId,
            userId = applicantId,
            requestedRole = requestedRole,
            message = dto.Message,
            skills = dto.Skills,
            experience = dto.Experience,
            portfolio = dto.Portfolio,
            availability = dto.Availability,
            status = MembershipRequestStatus.PENDING,
            appliedAt = DateTime.UtcNow
        };

        await _repository.AddAsync(application, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<GuildApplicationDto>.Success(ToDto(application));
    }

    /// <summary>A pending application becomes a membership in the same save. Only the owner may admit someone as ADMIN.</summary>
    public async Task<Result<GuildApplicationDto>> AcceptAsync(string guildId, string id, GuildActor actor, string? reviewMessage, CancellationToken ct = default)
    {
        var (guild, application) = await LoadAsync(guildId, id, ct);
        if (guild is null || application is null)
            return Result<GuildApplicationDto>.NotFound($"Application with id {id} not found in this guild");
        if (!await GuildAuthority.CanManageAsync(guild, actor, _memberRepository, ct))
            return Result<GuildApplicationDto>.Forbidden("Only the guild's owner or admins can review applications");
        if (application.status != MembershipRequestStatus.PENDING)
            return Result<GuildApplicationDto>.ValidationError("This application has already been answered");
        if (application.requestedRole == GuildMemberRole.OWNER)
            return Result<GuildApplicationDto>.ValidationError(GuildAuthority.GrantableRolesMessage);
        if (application.requestedRole == GuildMemberRole.ADMIN && !await GuildAuthority.IsOwnerAsync(guild, actor, _memberRepository, ct))
            return Result<GuildApplicationDto>.Forbidden("Only the guild owner can admit an admin");

        var applicantId = application.userId;
        if (await GuildAuthority.IsMemberAsync(guild, applicantId, _memberRepository, ct))
            return Result<GuildApplicationDto>.Conflict("User is already a member of this guild");

        application.status = MembershipRequestStatus.APPROVED;
        application.reviewMessage = reviewMessage;
        application.reviewedAt = DateTime.UtcNow;
        application.reviewedById = actor.Id;
        await _repository.UpdateAsync(application, ct);

        await _memberRepository.AddAsync(new GuildMember
        {
            id = Guid.NewGuid().ToString(),
            guildId = guildId,
            userId = applicantId,
            role = application.requestedRole,
            joinedAt = DateTime.UtcNow
        }, ct);
        guild.membersCount++;
        await _guildRepository.UpdateAsync(guild, ct);

        try
        {
            await _unitOfWork.SaveChangesAsync(ct);
        }
        catch (DbUpdateException failure) when (GuildAuthority.IsDuplicateGuildUserRow(failure))
        {
            _unitOfWork.ClearTrackedChanges();
            return Result<GuildApplicationDto>.Conflict("User is already a member of this guild");
        }
        return Result<GuildApplicationDto>.Success(ToDto(application));
    }

    public async Task<Result<GuildApplicationDto>> RejectAsync(string guildId, string id, GuildActor actor, string? reviewMessage, CancellationToken ct = default)
    {
        var (guild, application) = await LoadAsync(guildId, id, ct);
        if (guild is null || application is null)
            return Result<GuildApplicationDto>.NotFound($"Application with id {id} not found in this guild");
        if (!await GuildAuthority.CanManageAsync(guild, actor, _memberRepository, ct))
            return Result<GuildApplicationDto>.Forbidden("Only the guild's owner or admins can review applications");
        if (application.status != MembershipRequestStatus.PENDING)
            return Result<GuildApplicationDto>.ValidationError("This application has already been answered");

        application.status = MembershipRequestStatus.REJECTED;
        application.reviewMessage = reviewMessage;
        application.reviewedAt = DateTime.UtcNow;
        application.reviewedById = actor.Id;

        await _repository.UpdateAsync(application, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<GuildApplicationDto>.Success(ToDto(application));
    }

    public async Task<Result<GuildApplicationDto>> WithdrawAsync(string guildId, string id, GuildActor actor, CancellationToken ct = default)
    {
        var (guild, application) = await LoadAsync(guildId, id, ct);
        if (guild is null || application is null)
            return Result<GuildApplicationDto>.NotFound($"Application with id {id} not found in this guild");
        if (application.userId != actor.Id)
            return Result<GuildApplicationDto>.Forbidden("Only the applicant can withdraw an application");
        if (application.status != MembershipRequestStatus.PENDING)
            return Result<GuildApplicationDto>.ValidationError("This application has already been answered");

        application.status = MembershipRequestStatus.WITHDRAWN;
        await _repository.UpdateAsync(application, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<GuildApplicationDto>.Success(ToDto(application));
    }

    public async Task<Result<bool>> DeleteAsync(string guildId, string id, GuildActor actor, CancellationToken ct = default)
    {
        var (guild, application) = await LoadAsync(guildId, id, ct);
        if (guild is null || application is null)
            return Result<bool>.NotFound($"Application with id {id} not found in this guild");
        if (application.userId != actor.Id && !await GuildAuthority.CanManageAsync(guild, actor, _memberRepository, ct))
            return Result<bool>.Forbidden("Only the applicant or the guild's managers can delete this application");

        await _repository.DeleteAsync(application, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<bool>.Success(true);
    }
}

public class GuildInvitationService : IGuildInvitationService
{
    /// <summary>How long an unanswered invitation stays answerable.</summary>
    internal static readonly TimeSpan InvitationLifetime = TimeSpan.FromDays(14);

    private readonly IRepository<GuildInvitation> _repository;
    private readonly IRepository<GuildMember> _memberRepository;
    private readonly IRepository<Guild> _guildRepository;
    private readonly IRepository<User> _userRepository;
    private readonly IUnitOfWork _unitOfWork;

    public GuildInvitationService(
        IRepository<GuildInvitation> repository,
        IRepository<GuildMember> memberRepository,
        IRepository<Guild> guildRepository,
        IRepository<User> userRepository,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _memberRepository = memberRepository;
        _guildRepository = guildRepository;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
    }

    /// <summary>The invitee's view. Managers get <see cref="ToManagerDto"/>.</summary>
    private static GuildInvitationDto ToDto(GuildInvitation invitation, Guild? guild) => new()
    {
        Id = invitation.id,
        GuildId = invitation.guildId,
        GuildName = guild?.name,
        GuildSlug = guild?.slug,
        InvitedById = invitation.invitedById,
        InvitedUserId = invitation.invitedUserId,
        InvitedEmail = invitation.invitedEmail,
        Role = invitation.role.ToString(),
        Message = invitation.message,
        Status = invitation.status.ToString(),
        CreatedAt = invitation.createdAt,
        ExpiresAt = invitation.expiresAt,
        RespondedAt = invitation.respondedAt
    };

    /// <summary>A manager's view: an email invitation never says which account (if any) the email resolved to. See Services/AGENTS.md §Email invitations.</summary>
    private static GuildInvitationDto ToManagerDto(GuildInvitation invitation, Guild? guild)
    {
        var dto = ToDto(invitation, guild);
        return invitation.invitedEmail is null ? dto : dto with { InvitedUserId = null };
    }

    private static bool IsAnswerable(GuildInvitation invitation) =>
        invitation.status == InvitationStatus.PENDING &&
        (!invitation.expiresAt.HasValue || invitation.expiresAt.Value > DateTime.UtcNow);

    private async Task<(Guild? Guild, GuildInvitation? Invitation)> LoadAsync(string guildId, string id, CancellationToken ct)
    {
        var guild = await _guildRepository.GetByIdAsync(guildId, ct);
        if (guild is null)
            return (null, null);
        var invitation = await _repository.GetByIdAsync(id, ct);
        return (guild, invitation is not null && invitation.guildId == guildId ? invitation : null);
    }

    public async Task<Result<GuildInvitationDto>> GetByIdAsync(string guildId, string id, GuildActor actor, CancellationToken ct = default)
    {
        var (guild, invitation) = await LoadAsync(guildId, id, ct);
        if (guild is null || invitation is null)
            return Result<GuildInvitationDto>.NotFound($"Invitation with id {id} not found in this guild");
        var isInvitee = invitation.invitedUserId is not null && invitation.invitedUserId == actor.Id;
        if (!isInvitee && !await GuildAuthority.CanManageAsync(guild, actor, _memberRepository, ct))
            return Result<GuildInvitationDto>.Forbidden("Only the invitee or the guild's managers can read this invitation");
        return Result<GuildInvitationDto>.Success(isInvitee ? ToDto(invitation, guild) : ToManagerDto(invitation, guild));
    }

    public async Task<Result<IReadOnlyList<GuildInvitationDto>>> GetByGuildIdAsync(string guildId, GuildActor actor, CancellationToken ct = default)
    {
        var guild = await _guildRepository.GetByIdAsync(guildId, ct);
        if (guild is null)
            return Result<IReadOnlyList<GuildInvitationDto>>.NotFound($"Guild with id {guildId} not found");
        if (!await GuildAuthority.CanManageAsync(guild, actor, _memberRepository, ct))
            return Result<IReadOnlyList<GuildInvitationDto>>.Forbidden("Only the guild's owner or admins can read its invitations");

        var invitations = await _repository.FindAsync(i => i.guildId == guildId, ct);
        return Result<IReadOnlyList<GuildInvitationDto>>.Success(
            invitations.OrderByDescending(i => i.createdAt).Select(i => ToManagerDto(i, guild)).ToList());
    }

    public async Task<Result<IReadOnlyList<GuildInvitationDto>>> GetPendingForUserAsync(string userId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return Result<IReadOnlyList<GuildInvitationDto>>.Success(Array.Empty<GuildInvitationDto>());

        var invitations = (await _repository.FindAsync(
                i => i.invitedUserId == userId && i.status == InvitationStatus.PENDING, ct))
            .Where(IsAnswerable)
            .ToList();
        var guildIds = invitations.Select(i => i.guildId).Distinct().ToList();
        var guilds = guildIds.Count == 0
            ? new Dictionary<string, Guild>()
            : (await _guildRepository.FindAsync(g => guildIds.Contains(g.id), ct)).ToDictionary(g => g.id);

        return Result<IReadOnlyList<GuildInvitationDto>>.Success(invitations
            .OrderByDescending(i => i.createdAt)
            .Select(i => ToDto(i, guilds.GetValueOrDefault(i.guildId)))
            .ToList());
    }

    /// <summary>By account id: bound to that account. By email: always answers the same way, whether or not an account uses the email (Services/AGENTS.md §Email invitations).</summary>
    public async Task<Result<GuildInvitationDto>> CreateAsync(string guildId, GuildActor actor, CreateGuildInvitationDto dto, CancellationToken ct = default)
    {
        var guild = await _guildRepository.GetByIdAsync(guildId, ct);
        if (guild is null)
            return Result<GuildInvitationDto>.NotFound($"Guild with id {guildId} not found");
        if (!await GuildAuthority.CanManageAsync(guild, actor, _memberRepository, ct))
            return Result<GuildInvitationDto>.Forbidden("Only the guild's owner or admins can invite people");
        if (!GuildAuthority.TryParseGrantableRole(dto.Role, out var role))
            return Result<GuildInvitationDto>.ValidationError(GuildAuthority.GrantableRolesMessage);
        if (role == GuildMemberRole.ADMIN && !await GuildAuthority.IsOwnerAsync(guild, actor, _memberRepository, ct))
            return Result<GuildInvitationDto>.Forbidden("Only the guild owner can invite an admin");

        string? inviteeId;
        string? invitedEmail = null;
        if (!string.IsNullOrWhiteSpace(dto.InvitedUserId))
        {
            // Account ids come from public member and profile lists, so naming one reveals nothing new.
            var invitee = await _userRepository.GetByIdAsync(dto.InvitedUserId, ct);
            if (invitee is null)
                return Result<GuildInvitationDto>.NotFound("That user does not exist");

            inviteeId = invitee.id;
            if (await GuildAuthority.IsMemberAsync(guild, inviteeId, _memberRepository, ct))
                return Result<GuildInvitationDto>.Conflict("That person is already a member of this guild");
            if (await HasAnswerableInvitationAsync(guildId, inviteeId, ct))
                return Result<GuildInvitationDto>.Conflict("That person already has an invitation waiting");
        }
        else if (!string.IsNullOrWhiteSpace(dto.InvitedEmail))
        {
            invitedEmail = dto.InvitedEmail.Trim().ToLowerInvariant();
            var email = invitedEmail;

            // Only the inviter's own earlier email invitations decide a conflict here, never anything about the account.
            var sameEmail = await _repository.FindAsync(
                i => i.guildId == guildId && i.invitedEmail == email && i.status == InvitationStatus.PENDING, ct);
            if (sameEmail.Any(IsAnswerable))
                return Result<GuildInvitationDto>.Conflict("An invitation to that email is already waiting");

            // Bind only when an account owns the email, is not yet a member, and has nothing waiting; otherwise store an
            // unbound row that nobody can accept. Either way the inviter sees the same answer.
            var owner = await _userRepository.FindOneAsync(u => u.email.ToLower() == email, ct);
            inviteeId = owner is not null &&
                        !await GuildAuthority.IsMemberAsync(guild, owner.id, _memberRepository, ct) &&
                        !await HasAnswerableInvitationAsync(guildId, owner.id, ct)
                ? owner.id
                : null;
        }
        else
        {
            return Result<GuildInvitationDto>.ValidationError("Name the person to invite by their account email");
        }

        var invitation = new GuildInvitation
        {
            id = Guid.NewGuid().ToString(),
            guildId = guildId,
            invitedUserId = inviteeId,
            // Only what the inviter typed; an id-based invite must not disclose the account email to managers.
            invitedEmail = invitedEmail,
            invitedById = actor.Id,
            role = role,
            message = dto.Message,
            status = InvitationStatus.PENDING,
            createdAt = DateTime.UtcNow,
            expiresAt = DateTime.UtcNow.Add(InvitationLifetime)
        };

        await _repository.AddAsync(invitation, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<GuildInvitationDto>.Success(ToManagerDto(invitation, guild));
    }

    private async Task<bool> HasAnswerableInvitationAsync(string guildId, string userId, CancellationToken ct)
    {
        var open = await _repository.FindAsync(
            i => i.guildId == guildId && i.invitedUserId == userId && i.status == InvitationStatus.PENDING, ct);
        return open.Any(IsAnswerable);
    }

    /// <summary>Only the bound invitee may accept, once, while the invitation is still pending and unexpired.</summary>
    public async Task<Result<GuildInvitationDto>> AcceptAsync(string guildId, string id, GuildActor actor, CancellationToken ct = default)
    {
        var (guild, invitation) = await LoadAsync(guildId, id, ct);
        if (guild is null || invitation is null)
            return Result<GuildInvitationDto>.NotFound($"Invitation with id {id} not found in this guild");

        // Fail closed for email-only rows: an unbound invitation has no provable owner (Controllers/AGENTS.md §Email-only invitations).
        if (string.IsNullOrWhiteSpace(invitation.invitedUserId) || invitation.invitedUserId != actor.Id)
            return Result<GuildInvitationDto>.Forbidden("This invitation was not sent to you");
        if (!IsAnswerable(invitation))
            return Result<GuildInvitationDto>.ValidationError("This invitation can no longer be accepted");

        var inviteeId = invitation.invitedUserId;
        if (await GuildAuthority.IsMemberAsync(guild, inviteeId, _memberRepository, ct))
            return Result<GuildInvitationDto>.Conflict("You are already a member of this guild");

        invitation.status = InvitationStatus.ACCEPTED;
        invitation.respondedAt = DateTime.UtcNow;
        await _repository.UpdateAsync(invitation, ct);

        await _memberRepository.AddAsync(new GuildMember
        {
            id = Guid.NewGuid().ToString(),
            guildId = guildId,
            userId = inviteeId,
            role = invitation.role == GuildMemberRole.OWNER ? GuildMemberRole.MEMBER : invitation.role,
            joinedAt = DateTime.UtcNow
        }, ct);
        guild.membersCount++;
        await _guildRepository.UpdateAsync(guild, ct);

        try
        {
            await _unitOfWork.SaveChangesAsync(ct);
        }
        catch (DbUpdateException failure) when (GuildAuthority.IsDuplicateGuildUserRow(failure))
        {
            _unitOfWork.ClearTrackedChanges();
            return Result<GuildInvitationDto>.Conflict("You are already a member of this guild");
        }
        return Result<GuildInvitationDto>.Success(ToDto(invitation, guild));
    }

    public async Task<Result<GuildInvitationDto>> RejectAsync(string guildId, string id, GuildActor actor, CancellationToken ct = default)
    {
        var (guild, invitation) = await LoadAsync(guildId, id, ct);
        if (guild is null || invitation is null)
            return Result<GuildInvitationDto>.NotFound($"Invitation with id {id} not found in this guild");
        if (string.IsNullOrWhiteSpace(invitation.invitedUserId) || invitation.invitedUserId != actor.Id)
            return Result<GuildInvitationDto>.Forbidden("This invitation was not sent to you");
        if (invitation.status != InvitationStatus.PENDING)
            return Result<GuildInvitationDto>.ValidationError("This invitation has already been answered");

        invitation.status = InvitationStatus.DECLINED;
        invitation.respondedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(invitation, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<GuildInvitationDto>.Success(ToDto(invitation, guild));
    }

    public async Task<Result<bool>> DeleteAsync(string guildId, string id, GuildActor actor, CancellationToken ct = default)
    {
        var (guild, invitation) = await LoadAsync(guildId, id, ct);
        if (guild is null || invitation is null)
            return Result<bool>.NotFound($"Invitation with id {id} not found in this guild");
        if (!await GuildAuthority.CanManageAsync(guild, actor, _memberRepository, ct))
            return Result<bool>.Forbidden("Only the guild's owner or admins can withdraw an invitation");

        await _repository.DeleteAsync(invitation, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<bool>.Success(true);
    }
}

public class GuildFollowService : IGuildFollowService
{
    private readonly IRepository<GuildFollow> _repository;
    private readonly IRepository<Guild> _guildRepository;
    private readonly IUnitOfWork _unitOfWork;

    public GuildFollowService(IRepository<GuildFollow> repository, IRepository<Guild> guildRepository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _guildRepository = guildRepository;
        _unitOfWork = unitOfWork;
    }

    private static GuildFollowDto ToDto(GuildFollow follow) => new()
    {
        Id = follow.id,
        GuildId = follow.guildId,
        UserId = follow.userId,
        NotifyUpdates = follow.notifyUpdates,
        NotifyEvents = follow.notifyEvents,
        NotifyProjects = follow.notifyProjects,
        CreatedAt = follow.createdAt
    };

    public async Task<Result<IReadOnlyList<GuildFollowDto>>> GetByGuildIdAsync(string guildId, CancellationToken ct = default)
    {
        var follows = await _repository.FindAsync(f => f.guildId == guildId, ct);
        return Result<IReadOnlyList<GuildFollowDto>>.Success(follows.Select(ToDto).ToList());
    }

    public async Task<Result<IReadOnlyList<GuildFollowDto>>> GetByUserIdAsync(string userId, CancellationToken ct = default)
    {
        var follows = await _repository.FindAsync(f => f.userId == userId, ct);
        return Result<IReadOnlyList<GuildFollowDto>>.Success(follows.Select(ToDto).ToList());
    }

    public async Task<Result<GuildFollowDto>> FollowAsync(string guildId, GuildActor actor, CreateGuildFollowDto dto, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(actor.Id))
            return Result<GuildFollowDto>.Unauthorized("Sign in to follow a guild");
        if (!await _guildRepository.ExistsAsync(g => g.id == guildId, ct))
            return Result<GuildFollowDto>.NotFound($"Guild with id {guildId} not found");

        var userId = actor.Id;
        if (await _repository.ExistsAsync(f => f.guildId == guildId && f.userId == userId, ct))
            return Result<GuildFollowDto>.Conflict("Already following this guild");

        var follow = new GuildFollow
        {
            id = Guid.NewGuid().ToString(),
            guildId = guildId,
            userId = userId,
            notifyUpdates = dto.NotifyUpdates,
            notifyEvents = dto.NotifyEvents,
            notifyProjects = dto.NotifyProjects,
            createdAt = DateTime.UtcNow
        };

        await _repository.AddAsync(follow, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<GuildFollowDto>.Success(ToDto(follow));
    }

    public async Task<Result<bool>> UnfollowAsync(string guildId, GuildActor actor, CancellationToken ct = default)
    {
        var userId = actor.Id;
        var follow = await _repository.FindOneAsync(f => f.guildId == guildId && f.userId == userId, ct);
        if (follow is null)
            return Result<bool>.NotFound("Follow relationship not found");

        await _repository.DeleteAsync(follow, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<bool>.Success(true);
    }

    public async Task<Result<bool>> IsFollowingAsync(string guildId, string userId, CancellationToken ct = default)
    {
        var follow = await _repository.FindOneAsync(f => f.guildId == guildId && f.userId == userId, ct);
        return Result<bool>.Success(follow is not null);
    }
}
