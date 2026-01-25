namespace ArdaNova.Application.Services.Implementations;

using ArdaNova.Application.Common.Interfaces;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Interfaces;
using ArdaNova.Domain.Models.Entities;
using ArdaNova.Domain.Models.Enums;
using AutoMapper;

public class GuildService : IGuildService
{
    private readonly IRepository<Guild> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GuildService(IRepository<Guild> repository, IUnitOfWork unitOfWork, IMapper mapper)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<Result<GuildDto>> GetByIdAsync(string id, CancellationToken ct = default)
    {
        var guild = await _repository.GetByIdAsync(id, ct);
        if (guild is null)
            return Result<GuildDto>.NotFound($"Guild with id {id} not found");
        return Result<GuildDto>.Success(_mapper.Map<GuildDto>(guild));
    }

    public async Task<Result<GuildDto>> GetBySlugAsync(string slug, CancellationToken ct = default)
    {
        var guild = await _repository.FindOneAsync(g => g.slug == slug, ct);
        if (guild is null)
            return Result<GuildDto>.NotFound($"Guild with slug {slug} not found");
        return Result<GuildDto>.Success(_mapper.Map<GuildDto>(guild));
    }

    public async Task<Result<IReadOnlyList<GuildDto>>> GetAllAsync(CancellationToken ct = default)
    {
        var guilds = await _repository.GetAllAsync(ct);
        return Result<IReadOnlyList<GuildDto>>.Success(_mapper.Map<IReadOnlyList<GuildDto>>(guilds));
    }

    public async Task<Result<PagedResult<GuildDto>>> GetPagedAsync(int page, int pageSize, CancellationToken ct = default)
    {
        var result = await _repository.GetPagedAsync(page, pageSize, null, ct);
        return Result<PagedResult<GuildDto>>.Success(result.Map(_mapper.Map<GuildDto>));
    }

    public async Task<Result<GuildDto>> GetByOwnerIdAsync(string ownerId, CancellationToken ct = default)
    {
        var guild = await _repository.FindOneAsync(g => g.ownerId == ownerId, ct);
        if (guild is null)
            return Result<GuildDto>.NotFound($"Guild with owner {ownerId} not found");
        return Result<GuildDto>.Success(_mapper.Map<GuildDto>(guild));
    }

    public async Task<Result<IReadOnlyList<GuildDto>>> GetVerifiedAsync(CancellationToken ct = default)
    {
        var guilds = await _repository.FindAsync(g => g.isVerified, ct);
        return Result<IReadOnlyList<GuildDto>>.Success(_mapper.Map<IReadOnlyList<GuildDto>>(guilds));
    }

    private static string GenerateSlug(string name)
    {
        return name.ToLowerInvariant()
            .Replace(" ", "-")
            .Replace("--", "-")
            + "-" + Guid.NewGuid().ToString("N")[..8];
    }

    public async Task<Result<GuildDto>> CreateAsync(CreateGuildDto dto, CancellationToken ct = default)
    {
        var guild = new Guild
        {
            id = Guid.NewGuid().ToString(),
            ownerId = dto.OwnerId,
            name = dto.Name,
            slug = GenerateSlug(dto.Name),
            description = dto.Description,
            email = dto.Email,
            website = dto.Website,
            phone = dto.Phone,
            isVerified = false,
            reviewsCount = 0,
            projectsCount = 0,
            createdAt = DateTime.UtcNow,
            updatedAt = DateTime.UtcNow
        };

        await _repository.AddAsync(guild, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<GuildDto>.Success(_mapper.Map<GuildDto>(guild));
    }

    public async Task<Result<GuildDto>> UpdateAsync(string id, UpdateGuildDto dto, CancellationToken ct = default)
    {
        var guild = await _repository.GetByIdAsync(id, ct);
        if (guild is null)
            return Result<GuildDto>.NotFound($"Guild with id {id} not found");

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

    public async Task<Result<bool>> DeleteAsync(string id, CancellationToken ct = default)
    {
        var guild = await _repository.GetByIdAsync(id, ct);
        if (guild is null)
            return Result<bool>.NotFound($"Guild with id {id} not found");

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
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GuildMemberService(IRepository<GuildMember> repository, IUnitOfWork unitOfWork, IMapper mapper)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<Result<GuildMemberDto>> GetByIdAsync(string id, CancellationToken ct = default)
    {
        var member = await _repository.GetByIdAsync(id, ct);
        if (member is null)
            return Result<GuildMemberDto>.NotFound($"Member with id {id} not found");
        return Result<GuildMemberDto>.Success(_mapper.Map<GuildMemberDto>(member));
    }

    public async Task<Result<IReadOnlyList<GuildMemberDto>>> GetByGuildIdAsync(string guildId, CancellationToken ct = default)
    {
        var members = await _repository.FindAsync(m => m.guildId == guildId, ct);
        return Result<IReadOnlyList<GuildMemberDto>>.Success(_mapper.Map<IReadOnlyList<GuildMemberDto>>(members));
    }

    public async Task<Result<IReadOnlyList<GuildMemberDto>>> GetByUserIdAsync(string userId, CancellationToken ct = default)
    {
        var members = await _repository.FindAsync(m => m.userId == userId, ct);
        return Result<IReadOnlyList<GuildMemberDto>>.Success(_mapper.Map<IReadOnlyList<GuildMemberDto>>(members));
    }

    public async Task<Result<GuildMemberDto>> CreateAsync(CreateGuildMemberDto dto, CancellationToken ct = default)
    {
        var member = new GuildMember
        {
            id = Guid.NewGuid().ToString(),
            guildId = dto.GuildId,
            userId = dto.UserId,
            role = dto.Role,
            joinedAt = DateTime.UtcNow
        };

        await _repository.AddAsync(member, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<GuildMemberDto>.Success(_mapper.Map<GuildMemberDto>(member));
    }

    public async Task<Result<GuildMemberDto>> UpdateAsync(string id, UpdateGuildMemberDto dto, CancellationToken ct = default)
    {
        var member = await _repository.GetByIdAsync(id, ct);
        if (member is null)
            return Result<GuildMemberDto>.NotFound($"Member with id {id} not found");

        if (dto.Role is not null) member.role = dto.Role;

        await _repository.UpdateAsync(member, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<GuildMemberDto>.Success(_mapper.Map<GuildMemberDto>(member));
    }

    public async Task<Result<bool>> DeleteAsync(string id, CancellationToken ct = default)
    {
        var member = await _repository.GetByIdAsync(id, ct);
        if (member is null)
            return Result<bool>.NotFound($"Member with id {id} not found");

        await _repository.DeleteAsync(member, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<bool>.Success(true);
    }
}

public class ProjectBidService : IProjectBidService
{
    private readonly IRepository<ProjectBid> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public ProjectBidService(IRepository<ProjectBid> repository, IUnitOfWork unitOfWork, IMapper mapper)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<Result<ProjectBidDto>> GetByIdAsync(string id, CancellationToken ct = default)
    {
        var bid = await _repository.GetByIdAsync(id, ct);
        if (bid is null)
            return Result<ProjectBidDto>.NotFound($"Bid with id {id} not found");
        return Result<ProjectBidDto>.Success(_mapper.Map<ProjectBidDto>(bid));
    }

    public async Task<Result<IReadOnlyList<ProjectBidDto>>> GetByProjectIdAsync(string projectId, CancellationToken ct = default)
    {
        var bids = await _repository.FindAsync(b => b.projectId == projectId, ct);
        return Result<IReadOnlyList<ProjectBidDto>>.Success(_mapper.Map<IReadOnlyList<ProjectBidDto>>(bids));
    }

    public async Task<Result<IReadOnlyList<ProjectBidDto>>> GetByGuildIdAsync(string guildId, CancellationToken ct = default)
    {
        var bids = await _repository.FindAsync(b => b.guildId == guildId, ct);
        return Result<IReadOnlyList<ProjectBidDto>>.Success(_mapper.Map<IReadOnlyList<ProjectBidDto>>(bids));
    }

    public async Task<Result<IReadOnlyList<ProjectBidDto>>> GetByStatusAsync(BidStatus status, CancellationToken ct = default)
    {
        var bids = await _repository.FindAsync(b => b.status == status, ct);
        return Result<IReadOnlyList<ProjectBidDto>>.Success(_mapper.Map<IReadOnlyList<ProjectBidDto>>(bids));
    }

    public async Task<Result<ProjectBidDto>> CreateAsync(CreateProjectBidDto dto, CancellationToken ct = default)
    {
        var bid = new ProjectBid
        {
            id = Guid.NewGuid().ToString(),
            projectId = dto.ProjectId,
            guildId = dto.GuildId,
            userId = dto.UserId,
            proposal = dto.Proposal,
            budget = dto.Budget,
            timeline = dto.Timeline,
            deliverables = dto.Deliverables,
            status = BidStatus.SUBMITTED,
            submittedAt = DateTime.UtcNow
        };

        await _repository.AddAsync(bid, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<ProjectBidDto>.Success(_mapper.Map<ProjectBidDto>(bid));
    }

    public async Task<Result<ProjectBidDto>> UpdateAsync(string id, UpdateProjectBidDto dto, CancellationToken ct = default)
    {
        var bid = await _repository.GetByIdAsync(id, ct);
        if (bid is null)
            return Result<ProjectBidDto>.NotFound($"Bid with id {id} not found");

        if (dto.Proposal is not null) bid.proposal = dto.Proposal;
        if (dto.Budget.HasValue) bid.budget = dto.Budget.Value;
        if (dto.Timeline is not null) bid.timeline = dto.Timeline;
        if (dto.Deliverables is not null) bid.deliverables = dto.Deliverables;

        await _repository.UpdateAsync(bid, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<ProjectBidDto>.Success(_mapper.Map<ProjectBidDto>(bid));
    }

    public async Task<Result<bool>> DeleteAsync(string id, CancellationToken ct = default)
    {
        var bid = await _repository.GetByIdAsync(id, ct);
        if (bid is null)
            return Result<bool>.NotFound($"Bid with id {id} not found");

        await _repository.DeleteAsync(bid, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<bool>.Success(true);
    }

    public async Task<Result<ProjectBidDto>> AcceptAsync(string id, CancellationToken ct = default)
    {
        var bid = await _repository.GetByIdAsync(id, ct);
        if (bid is null)
            return Result<ProjectBidDto>.NotFound($"Bid with id {id} not found");

        bid.status = BidStatus.ACCEPTED;
        bid.reviewedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(bid, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<ProjectBidDto>.Success(_mapper.Map<ProjectBidDto>(bid));
    }

    public async Task<Result<ProjectBidDto>> RejectAsync(string id, CancellationToken ct = default)
    {
        var bid = await _repository.GetByIdAsync(id, ct);
        if (bid is null)
            return Result<ProjectBidDto>.NotFound($"Bid with id {id} not found");

        bid.status = BidStatus.REJECTED;
        bid.reviewedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(bid, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<ProjectBidDto>.Success(_mapper.Map<ProjectBidDto>(bid));
    }

    public async Task<Result<ProjectBidDto>> WithdrawAsync(string id, CancellationToken ct = default)
    {
        var bid = await _repository.GetByIdAsync(id, ct);
        if (bid is null)
            return Result<ProjectBidDto>.NotFound($"Bid with id {id} not found");

        bid.status = BidStatus.WITHDRAWN;

        await _repository.UpdateAsync(bid, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<ProjectBidDto>.Success(_mapper.Map<ProjectBidDto>(bid));
    }
}

public class GuildReviewService : IGuildReviewService
{
    private readonly IRepository<GuildReview> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GuildReviewService(IRepository<GuildReview> repository, IUnitOfWork unitOfWork, IMapper mapper)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<Result<GuildReviewDto>> GetByIdAsync(string id, CancellationToken ct = default)
    {
        var review = await _repository.GetByIdAsync(id, ct);
        if (review is null)
            return Result<GuildReviewDto>.NotFound($"Review with id {id} not found");
        return Result<GuildReviewDto>.Success(_mapper.Map<GuildReviewDto>(review));
    }

    public async Task<Result<IReadOnlyList<GuildReviewDto>>> GetByGuildIdAsync(string guildId, CancellationToken ct = default)
    {
        var reviews = await _repository.FindAsync(r => r.guildId == guildId, ct);
        return Result<IReadOnlyList<GuildReviewDto>>.Success(_mapper.Map<IReadOnlyList<GuildReviewDto>>(reviews));
    }

    public async Task<Result<IReadOnlyList<GuildReviewDto>>> GetByUserIdAsync(string userId, CancellationToken ct = default)
    {
        var reviews = await _repository.FindAsync(r => r.userId == userId, ct);
        return Result<IReadOnlyList<GuildReviewDto>>.Success(_mapper.Map<IReadOnlyList<GuildReviewDto>>(reviews));
    }

    public async Task<Result<GuildReviewDto>> CreateAsync(CreateGuildReviewDto dto, CancellationToken ct = default)
    {
        var review = new GuildReview
        {
            id = Guid.NewGuid().ToString(),
            guildId = dto.GuildId,
            userId = dto.UserId,
            rating = dto.Rating,
            comment = dto.Comment,
            projectId = dto.ProjectId,
            createdAt = DateTime.UtcNow
        };

        await _repository.AddAsync(review, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<GuildReviewDto>.Success(_mapper.Map<GuildReviewDto>(review));
    }

    public async Task<Result<GuildReviewDto>> UpdateAsync(string id, UpdateGuildReviewDto dto, CancellationToken ct = default)
    {
        var review = await _repository.GetByIdAsync(id, ct);
        if (review is null)
            return Result<GuildReviewDto>.NotFound($"Review with id {id} not found");

        if (dto.Rating.HasValue) review.rating = dto.Rating.Value;
        if (dto.Comment is not null) review.comment = dto.Comment;

        await _repository.UpdateAsync(review, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<GuildReviewDto>.Success(_mapper.Map<GuildReviewDto>(review));
    }

    public async Task<Result<bool>> DeleteAsync(string id, CancellationToken ct = default)
    {
        var review = await _repository.GetByIdAsync(id, ct);
        if (review is null)
            return Result<bool>.NotFound($"Review with id {id} not found");

        await _repository.DeleteAsync(review, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<bool>.Success(true);
    }
}
