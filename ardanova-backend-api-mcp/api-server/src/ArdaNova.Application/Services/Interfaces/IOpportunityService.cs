namespace ArdaNova.Application.Services.Interfaces;

using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Domain.Models.Enums;

public interface IOpportunityService
{
    Task<Result<OpportunityDto>> GetByIdAsync(string id, CancellationToken ct = default);
    Task<Result<OpportunityDto>> GetBySlugAsync(string slug, CancellationToken ct = default);
    Task<Result<IReadOnlyList<OpportunityDto>>> GetAllAsync(CancellationToken ct = default);
    Task<Result<PagedResult<OpportunityDto>>> GetPagedAsync(int page, int pageSize, CancellationToken ct = default);
    Task<Result<PagedResult<OpportunityDto>>> SearchAsync(
        string? searchTerm,
        OpportunityType? type,
        OpportunityStatus? status,
        ExperienceLevel? experienceLevel,
        string? skills,
        int page,
        int pageSize,
        CancellationToken ct = default);
    Task<Result<IReadOnlyList<OpportunityDto>>> GetByPosterIdAsync(string posterId, CancellationToken ct = default);
    Task<Result<OpportunityDto>> CreateAsync(CreateOpportunityDto dto, CancellationToken ct = default);
    Task<Result<OpportunityDto>> UpdateAsync(string id, UpdateOpportunityDto dto, CancellationToken ct = default);
    Task<Result<OpportunityDto>> CloseAsync(string id, CancellationToken ct = default);
    Task<Result<bool>> DeleteAsync(string id, CancellationToken ct = default);
    Task<Result<OpportunityApplicationDto>> ApplyAsync(string opportunityId, ApplyToOpportunityDto dto, CancellationToken ct = default);
    Task<Result<IReadOnlyList<OpportunityApplicationDto>>> GetApplicationsAsync(string opportunityId, CancellationToken ct = default);
    Task<Result<OpportunityApplicationDto>> UpdateApplicationStatusAsync(string applicationId, UpdateApplicationStatusDto dto, CancellationToken ct = default);
}
