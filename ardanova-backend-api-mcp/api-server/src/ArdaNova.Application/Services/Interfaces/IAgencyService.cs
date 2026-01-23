namespace ArdaNova.Application.Services.Interfaces;

using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Domain.Models.Enums;

public interface IAgencyService
{
    Task<Result<AgencyDto>> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Result<AgencyDto>> GetBySlugAsync(string slug, CancellationToken ct = default);
    Task<Result<IReadOnlyList<AgencyDto>>> GetAllAsync(CancellationToken ct = default);
    Task<Result<PagedResult<AgencyDto>>> GetPagedAsync(int page, int pageSize, CancellationToken ct = default);
    Task<Result<AgencyDto>> GetByOwnerIdAsync(Guid ownerId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<AgencyDto>>> GetVerifiedAsync(CancellationToken ct = default);
    Task<Result<AgencyDto>> CreateAsync(CreateAgencyDto dto, CancellationToken ct = default);
    Task<Result<AgencyDto>> UpdateAsync(Guid id, UpdateAgencyDto dto, CancellationToken ct = default);
    Task<Result<bool>> DeleteAsync(Guid id, CancellationToken ct = default);
    Task<Result<AgencyDto>> VerifyAsync(Guid id, CancellationToken ct = default);
}

public interface IAgencyMemberService
{
    Task<Result<AgencyMemberDto>> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Result<IReadOnlyList<AgencyMemberDto>>> GetByAgencyIdAsync(Guid agencyId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<AgencyMemberDto>>> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<Result<AgencyMemberDto>> CreateAsync(CreateAgencyMemberDto dto, CancellationToken ct = default);
    Task<Result<AgencyMemberDto>> UpdateAsync(Guid id, UpdateAgencyMemberDto dto, CancellationToken ct = default);
    Task<Result<bool>> DeleteAsync(Guid id, CancellationToken ct = default);
}

public interface IProjectBidService
{
    Task<Result<ProjectBidDto>> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Result<IReadOnlyList<ProjectBidDto>>> GetByProjectIdAsync(Guid projectId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<ProjectBidDto>>> GetByAgencyIdAsync(Guid agencyId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<ProjectBidDto>>> GetByStatusAsync(BidStatus status, CancellationToken ct = default);
    Task<Result<ProjectBidDto>> CreateAsync(CreateProjectBidDto dto, CancellationToken ct = default);
    Task<Result<ProjectBidDto>> UpdateAsync(Guid id, UpdateProjectBidDto dto, CancellationToken ct = default);
    Task<Result<bool>> DeleteAsync(Guid id, CancellationToken ct = default);
    Task<Result<ProjectBidDto>> AcceptAsync(Guid id, CancellationToken ct = default);
    Task<Result<ProjectBidDto>> RejectAsync(Guid id, CancellationToken ct = default);
    Task<Result<ProjectBidDto>> WithdrawAsync(Guid id, CancellationToken ct = default);
}

public interface IAgencyReviewService
{
    Task<Result<AgencyReviewDto>> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Result<IReadOnlyList<AgencyReviewDto>>> GetByAgencyIdAsync(Guid agencyId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<AgencyReviewDto>>> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<Result<AgencyReviewDto>> CreateAsync(CreateAgencyReviewDto dto, CancellationToken ct = default);
    Task<Result<AgencyReviewDto>> UpdateAsync(Guid id, UpdateAgencyReviewDto dto, CancellationToken ct = default);
    Task<Result<bool>> DeleteAsync(Guid id, CancellationToken ct = default);
}
