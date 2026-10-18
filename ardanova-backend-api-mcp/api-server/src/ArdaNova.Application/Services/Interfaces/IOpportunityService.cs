namespace ArdaNova.Application.Services.Interfaces;

using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Implementations;
using ArdaNova.Domain.Models.Enums;

public interface IOpportunityService
{
    Task<Result<OpportunityDto>> GetByIdAsync(string id, CancellationToken ct = default);
    /// <summary>The opportunity only when this viewer may see it: its project is visible and it is not someone else's DRAFT.</summary>
    Task<Result<OpportunityDto>> GetVisibleByIdAsync(ProjectViewer viewer, string id, CancellationToken ct = default);
    Task<Result<OpportunityDto>> GetBySlugAsync(ProjectViewer viewer, string slug, CancellationToken ct = default);
    Task<Result<IReadOnlyList<OpportunityDto>>> GetAllAsync(ProjectViewer viewer, CancellationToken ct = default);
    Task<Result<PagedResult<OpportunityDto>>> GetPagedAsync(ProjectViewer viewer, int page, int pageSize, CancellationToken ct = default);
    Task<Result<PagedResult<OpportunityDto>>> SearchAsync(
        ProjectViewer viewer,
        string? searchTerm,
        OpportunityType? type,
        OpportunityStatus? status,
        ExperienceLevel? experienceLevel,
        string? skills,
        string? sourceType,
        int page,
        int pageSize,
        CancellationToken ct = default);
    Task<Result<IReadOnlyList<OpportunityDto>>> GetByPosterIdAsync(ProjectViewer viewer, string posterId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<OpportunityDto>>> GetByGuildIdAsync(ProjectViewer viewer, string guildId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<OpportunityDto>>> GetByProjectIdAsync(ProjectViewer viewer, string projectId, CancellationToken ct = default);
    Task<Result<OpportunityDto>> CreateAsync(CreateOpportunityDto dto, CancellationToken ct = default);
    Task<Result<OpportunityDto>> UpdateAsync(string id, UpdateOpportunityDto dto, CancellationToken ct = default);
    Task<Result<OpportunityDto>> CloseAsync(string id, CancellationToken ct = default);
    Task<Result<bool>> DeleteAsync(string id, CancellationToken ct = default);
    Task<Result<OpportunityApplicationDto>> ApplyAsync(string opportunityId, ApplyToOpportunityDto dto, CancellationToken ct = default);
    Task<Result<OpportunityApplicationDto>> GetApplicationByIdAsync(string applicationId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<OpportunityApplicationDto>>> GetApplicationsAsync(string opportunityId, CancellationToken ct = default);
    /// <summary>A reviewer's decision on a PENDING application; ACCEPTED on a project role also seats the applicant on the team.</summary>
    Task<Result<OpportunityApplicationDto>> UpdateApplicationStatusAsync(string applicationId, UpdateApplicationStatusDto dto, CancellationToken ct = default);

    // Updates
    Task<Result<OpportunityUpdateDto>> GetUpdateByIdAsync(string updateId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<OpportunityUpdateDto>>> GetUpdatesAsync(string opportunityId, CancellationToken ct = default);
    Task<Result<OpportunityUpdateDto>> CreateUpdateAsync(CreateOpportunityUpdateDto dto, CancellationToken ct = default);
    Task<Result<bool>> DeleteUpdateAsync(string updateId, CancellationToken ct = default);

    // Comments
    Task<Result<OpportunityCommentDto>> GetCommentByIdAsync(string commentId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<OpportunityCommentDto>>> GetCommentsAsync(string opportunityId, CancellationToken ct = default);
    Task<Result<OpportunityCommentDto>> AddCommentAsync(CreateOpportunityCommentDto dto, CancellationToken ct = default);
    Task<Result<bool>> DeleteCommentAsync(string commentId, CancellationToken ct = default);
}
