namespace ArdaNova.Application.Services.Implementations;

using ArdaNova.Application.Common.Interfaces;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Interfaces;
using ArdaNova.Domain.Models.Entities;
using ArdaNova.Domain.Models.Enums;
using AutoMapper;

public class AgencyService : IAgencyService
{
    private readonly IRepository<Agency> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public AgencyService(IRepository<Agency> repository, IUnitOfWork unitOfWork, IMapper mapper)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<Result<AgencyDto>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var agency = await _repository.GetByIdAsync(id, ct);
        if (agency is null)
            return Result<AgencyDto>.NotFound($"Agency with id {id} not found");
        return Result<AgencyDto>.Success(_mapper.Map<AgencyDto>(agency));
    }

    public async Task<Result<AgencyDto>> GetBySlugAsync(string slug, CancellationToken ct = default)
    {
        var agency = await _repository.FindOneAsync(a => a.Slug == slug, ct);
        if (agency is null)
            return Result<AgencyDto>.NotFound($"Agency with slug {slug} not found");
        return Result<AgencyDto>.Success(_mapper.Map<AgencyDto>(agency));
    }

    public async Task<Result<IReadOnlyList<AgencyDto>>> GetAllAsync(CancellationToken ct = default)
    {
        var agencies = await _repository.GetAllAsync(ct);
        return Result<IReadOnlyList<AgencyDto>>.Success(_mapper.Map<IReadOnlyList<AgencyDto>>(agencies));
    }

    public async Task<Result<PagedResult<AgencyDto>>> GetPagedAsync(int page, int pageSize, CancellationToken ct = default)
    {
        var result = await _repository.GetPagedAsync(page, pageSize, null, ct);
        return Result<PagedResult<AgencyDto>>.Success(result.Map(_mapper.Map<AgencyDto>));
    }

    public async Task<Result<AgencyDto>> GetByOwnerIdAsync(Guid ownerId, CancellationToken ct = default)
    {
        var agency = await _repository.FindOneAsync(a => a.OwnerId == ownerId, ct);
        if (agency is null)
            return Result<AgencyDto>.NotFound($"Agency with owner {ownerId} not found");
        return Result<AgencyDto>.Success(_mapper.Map<AgencyDto>(agency));
    }

    public async Task<Result<IReadOnlyList<AgencyDto>>> GetVerifiedAsync(CancellationToken ct = default)
    {
        var agencies = await _repository.FindAsync(a => a.IsVerified, ct);
        return Result<IReadOnlyList<AgencyDto>>.Success(_mapper.Map<IReadOnlyList<AgencyDto>>(agencies));
    }

    public async Task<Result<AgencyDto>> CreateAsync(CreateAgencyDto dto, CancellationToken ct = default)
    {
        var agency = Agency.Create(dto.OwnerId, dto.Name, dto.Description, dto.Email, dto.Website, dto.Phone);
        await _repository.AddAsync(agency, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<AgencyDto>.Success(_mapper.Map<AgencyDto>(agency));
    }

    public async Task<Result<AgencyDto>> UpdateAsync(Guid id, UpdateAgencyDto dto, CancellationToken ct = default)
    {
        var agency = await _repository.GetByIdAsync(id, ct);
        if (agency is null)
            return Result<AgencyDto>.NotFound($"Agency with id {id} not found");

        agency.Update(
            dto.Name ?? agency.Name,
            dto.Description ?? agency.Description,
            dto.Email ?? agency.Email,
            dto.Website ?? agency.Website,
            dto.Phone ?? agency.Phone,
            dto.Address ?? agency.Address
        );

        if (dto.Logo is not null) agency.SetLogo(dto.Logo);
        if (dto.Portfolio is not null) agency.SetPortfolio(dto.Portfolio);
        if (dto.Specialties is not null) agency.SetSpecialties(dto.Specialties);

        await _repository.UpdateAsync(agency, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<AgencyDto>.Success(_mapper.Map<AgencyDto>(agency));
    }

    public async Task<Result<bool>> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var agency = await _repository.GetByIdAsync(id, ct);
        if (agency is null)
            return Result<bool>.NotFound($"Agency with id {id} not found");

        await _repository.DeleteAsync(agency, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<bool>.Success(true);
    }

    public async Task<Result<AgencyDto>> VerifyAsync(Guid id, CancellationToken ct = default)
    {
        var agency = await _repository.GetByIdAsync(id, ct);
        if (agency is null)
            return Result<AgencyDto>.NotFound($"Agency with id {id} not found");

        agency.Verify();
        await _repository.UpdateAsync(agency, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<AgencyDto>.Success(_mapper.Map<AgencyDto>(agency));
    }
}

public class AgencyMemberService : IAgencyMemberService
{
    private readonly IRepository<AgencyMember> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public AgencyMemberService(IRepository<AgencyMember> repository, IUnitOfWork unitOfWork, IMapper mapper)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<Result<AgencyMemberDto>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var member = await _repository.GetByIdAsync(id, ct);
        if (member is null)
            return Result<AgencyMemberDto>.NotFound($"Member with id {id} not found");
        return Result<AgencyMemberDto>.Success(_mapper.Map<AgencyMemberDto>(member));
    }

    public async Task<Result<IReadOnlyList<AgencyMemberDto>>> GetByAgencyIdAsync(Guid agencyId, CancellationToken ct = default)
    {
        var members = await _repository.FindAsync(m => m.AgencyId == agencyId, ct);
        return Result<IReadOnlyList<AgencyMemberDto>>.Success(_mapper.Map<IReadOnlyList<AgencyMemberDto>>(members));
    }

    public async Task<Result<IReadOnlyList<AgencyMemberDto>>> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
    {
        var members = await _repository.FindAsync(m => m.UserId == userId, ct);
        return Result<IReadOnlyList<AgencyMemberDto>>.Success(_mapper.Map<IReadOnlyList<AgencyMemberDto>>(members));
    }

    public async Task<Result<AgencyMemberDto>> CreateAsync(CreateAgencyMemberDto dto, CancellationToken ct = default)
    {
        var member = AgencyMember.Create(dto.AgencyId, dto.UserId, dto.Role);
        await _repository.AddAsync(member, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<AgencyMemberDto>.Success(_mapper.Map<AgencyMemberDto>(member));
    }

    public async Task<Result<AgencyMemberDto>> UpdateAsync(Guid id, UpdateAgencyMemberDto dto, CancellationToken ct = default)
    {
        var member = await _repository.GetByIdAsync(id, ct);
        if (member is null)
            return Result<AgencyMemberDto>.NotFound($"Member with id {id} not found");

        if (dto.Role is not null) member.ChangeRole(dto.Role);
        await _repository.UpdateAsync(member, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<AgencyMemberDto>.Success(_mapper.Map<AgencyMemberDto>(member));
    }

    public async Task<Result<bool>> DeleteAsync(Guid id, CancellationToken ct = default)
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

    public async Task<Result<ProjectBidDto>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var bid = await _repository.GetByIdAsync(id, ct);
        if (bid is null)
            return Result<ProjectBidDto>.NotFound($"Bid with id {id} not found");
        return Result<ProjectBidDto>.Success(_mapper.Map<ProjectBidDto>(bid));
    }

    public async Task<Result<IReadOnlyList<ProjectBidDto>>> GetByProjectIdAsync(Guid projectId, CancellationToken ct = default)
    {
        var bids = await _repository.FindAsync(b => b.ProjectId == projectId, ct);
        return Result<IReadOnlyList<ProjectBidDto>>.Success(_mapper.Map<IReadOnlyList<ProjectBidDto>>(bids));
    }

    public async Task<Result<IReadOnlyList<ProjectBidDto>>> GetByAgencyIdAsync(Guid agencyId, CancellationToken ct = default)
    {
        var bids = await _repository.FindAsync(b => b.AgencyId == agencyId, ct);
        return Result<IReadOnlyList<ProjectBidDto>>.Success(_mapper.Map<IReadOnlyList<ProjectBidDto>>(bids));
    }

    public async Task<Result<IReadOnlyList<ProjectBidDto>>> GetByStatusAsync(BidStatus status, CancellationToken ct = default)
    {
        var bids = await _repository.FindAsync(b => b.Status == status, ct);
        return Result<IReadOnlyList<ProjectBidDto>>.Success(_mapper.Map<IReadOnlyList<ProjectBidDto>>(bids));
    }

    public async Task<Result<ProjectBidDto>> CreateAsync(CreateProjectBidDto dto, CancellationToken ct = default)
    {
        var bid = ProjectBid.Create(dto.ProjectId, dto.AgencyId, dto.UserId, dto.Proposal, dto.Budget, dto.Timeline, dto.Deliverables);
        await _repository.AddAsync(bid, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<ProjectBidDto>.Success(_mapper.Map<ProjectBidDto>(bid));
    }

    public async Task<Result<ProjectBidDto>> UpdateAsync(Guid id, UpdateProjectBidDto dto, CancellationToken ct = default)
    {
        var bid = await _repository.GetByIdAsync(id, ct);
        if (bid is null)
            return Result<ProjectBidDto>.NotFound($"Bid with id {id} not found");

        bid.UpdateProposal(
            dto.Proposal ?? bid.Proposal,
            dto.Budget ?? bid.Budget,
            dto.Timeline ?? bid.Timeline,
            dto.Deliverables ?? bid.Deliverables
        );

        await _repository.UpdateAsync(bid, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<ProjectBidDto>.Success(_mapper.Map<ProjectBidDto>(bid));
    }

    public async Task<Result<bool>> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var bid = await _repository.GetByIdAsync(id, ct);
        if (bid is null)
            return Result<bool>.NotFound($"Bid with id {id} not found");

        await _repository.DeleteAsync(bid, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<bool>.Success(true);
    }

    public async Task<Result<ProjectBidDto>> AcceptAsync(Guid id, CancellationToken ct = default)
    {
        var bid = await _repository.GetByIdAsync(id, ct);
        if (bid is null)
            return Result<ProjectBidDto>.NotFound($"Bid with id {id} not found");

        bid.Accept();
        await _repository.UpdateAsync(bid, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<ProjectBidDto>.Success(_mapper.Map<ProjectBidDto>(bid));
    }

    public async Task<Result<ProjectBidDto>> RejectAsync(Guid id, CancellationToken ct = default)
    {
        var bid = await _repository.GetByIdAsync(id, ct);
        if (bid is null)
            return Result<ProjectBidDto>.NotFound($"Bid with id {id} not found");

        bid.Reject();
        await _repository.UpdateAsync(bid, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<ProjectBidDto>.Success(_mapper.Map<ProjectBidDto>(bid));
    }

    public async Task<Result<ProjectBidDto>> WithdrawAsync(Guid id, CancellationToken ct = default)
    {
        var bid = await _repository.GetByIdAsync(id, ct);
        if (bid is null)
            return Result<ProjectBidDto>.NotFound($"Bid with id {id} not found");

        bid.Withdraw();
        await _repository.UpdateAsync(bid, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<ProjectBidDto>.Success(_mapper.Map<ProjectBidDto>(bid));
    }
}

public class AgencyReviewService : IAgencyReviewService
{
    private readonly IRepository<AgencyReview> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public AgencyReviewService(IRepository<AgencyReview> repository, IUnitOfWork unitOfWork, IMapper mapper)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<Result<AgencyReviewDto>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var review = await _repository.GetByIdAsync(id, ct);
        if (review is null)
            return Result<AgencyReviewDto>.NotFound($"Review with id {id} not found");
        return Result<AgencyReviewDto>.Success(_mapper.Map<AgencyReviewDto>(review));
    }

    public async Task<Result<IReadOnlyList<AgencyReviewDto>>> GetByAgencyIdAsync(Guid agencyId, CancellationToken ct = default)
    {
        var reviews = await _repository.FindAsync(r => r.AgencyId == agencyId, ct);
        return Result<IReadOnlyList<AgencyReviewDto>>.Success(_mapper.Map<IReadOnlyList<AgencyReviewDto>>(reviews));
    }

    public async Task<Result<IReadOnlyList<AgencyReviewDto>>> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
    {
        var reviews = await _repository.FindAsync(r => r.UserId == userId, ct);
        return Result<IReadOnlyList<AgencyReviewDto>>.Success(_mapper.Map<IReadOnlyList<AgencyReviewDto>>(reviews));
    }

    public async Task<Result<AgencyReviewDto>> CreateAsync(CreateAgencyReviewDto dto, CancellationToken ct = default)
    {
        var review = AgencyReview.Create(dto.AgencyId, dto.UserId, dto.Rating, dto.Comment, dto.ProjectId);
        await _repository.AddAsync(review, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<AgencyReviewDto>.Success(_mapper.Map<AgencyReviewDto>(review));
    }

    public async Task<Result<AgencyReviewDto>> UpdateAsync(Guid id, UpdateAgencyReviewDto dto, CancellationToken ct = default)
    {
        var review = await _repository.GetByIdAsync(id, ct);
        if (review is null)
            return Result<AgencyReviewDto>.NotFound($"Review with id {id} not found");

        review.UpdateReview(dto.Rating ?? review.Rating, dto.Comment ?? review.Comment);
        await _repository.UpdateAsync(review, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<AgencyReviewDto>.Success(_mapper.Map<AgencyReviewDto>(review));
    }

    public async Task<Result<bool>> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var review = await _repository.GetByIdAsync(id, ct);
        if (review is null)
            return Result<bool>.NotFound($"Review with id {id} not found");

        await _repository.DeleteAsync(review, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<bool>.Success(true);
    }
}
