namespace ArdaNova.Application.Services.Interfaces;

using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Domain.Models.Enums;

public interface IProjectService
{
    Task<Result<ProjectDto>> GetByIdAsync(string id, CancellationToken ct = default);
    Task<Result<ProjectDto>> GetBySlugAsync(string slug, CancellationToken ct = default);
    Task<Result<IReadOnlyList<ProjectDto>>> GetAllAsync(CancellationToken ct = default);
    Task<Result<PagedResult<ProjectDto>>> GetPagedAsync(int page, int pageSize, CancellationToken ct = default);
    Task<Result<PagedResult<ProjectDto>>> SearchAsync(
        string? searchTerm,
        ProjectStatus? status,
        string? category,
        ProjectType? projectType,
        int page,
        int pageSize,
        CancellationToken ct = default);
    Task<Result<IReadOnlyList<ProjectDto>>> GetByUserIdAsync(string userId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<ProjectDto>>> GetByStatusAsync(ProjectStatus status, CancellationToken ct = default);
    Task<Result<IReadOnlyList<ProjectDto>>> GetByCategory(string category, CancellationToken ct = default);
    Task<Result<IReadOnlyList<ProjectDto>>> GetByProjectTypeAsync(ProjectType projectType, CancellationToken ct = default);
    Task<Result<IReadOnlyList<ProjectDto>>> GetFeaturedAsync(CancellationToken ct = default);
    Task<Result<ProjectDto>> CreateAsync(CreateProjectDto dto, CancellationToken ct = default);
    Task<Result<ProjectDto>> UpdateAsync(string id, UpdateProjectDto dto, CancellationToken ct = default);
    Task<Result<bool>> DeleteAsync(string id, CancellationToken ct = default);
    Task<Result<ProjectDto>> PublishAsync(string id, CancellationToken ct = default);
    Task<Result<ProjectDto>> SetFeaturedAsync(string id, bool featured, CancellationToken ct = default);
}

public interface IProjectTaskService
{
    Task<Result<ProjectTaskDto>> GetByIdAsync(string id, CancellationToken ct = default);
    Task<Result<IReadOnlyList<ProjectTaskDto>>> GetByProjectIdAsync(string projectId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<ProjectTaskDto>>> GetByAssigneeIdAsync(string userId, CancellationToken ct = default);
    Task<Result<ProjectTaskDto>> CreateAsync(CreateProjectTaskDto dto, CancellationToken ct = default);
    Task<Result<ProjectTaskDto>> UpdateAsync(string id, UpdateProjectTaskDto dto, CancellationToken ct = default);
    Task<Result<bool>> DeleteAsync(string id, CancellationToken ct = default);
    Task<Result<ProjectTaskDto>> AssignAsync(string id, string? userId, CancellationToken ct = default);
    Task<Result<ProjectTaskDto>> UpdateStatusAsync(string id, TaskStatus status, CancellationToken ct = default);
}

public interface IProjectTaskDependencyService
{
    Task<Result<ProjectTaskDependencyDto>> GetByIdAsync(string id, CancellationToken ct = default);
    Task<Result<IReadOnlyList<ProjectTaskDependencyDto>>> GetByTaskIdAsync(string taskId, CancellationToken ct = default);
    Task<Result<ProjectTaskDependencyDto>> CreateAsync(CreateProjectTaskDependencyDto dto, CancellationToken ct = default);
    Task<Result<bool>> DeleteAsync(string id, CancellationToken ct = default);
}

public interface IProjectResourceService
{
    Task<Result<ProjectResourceDto>> GetByIdAsync(string id, CancellationToken ct = default);
    Task<Result<IReadOnlyList<ProjectResourceDto>>> GetByProjectIdAsync(string projectId, CancellationToken ct = default);
    Task<Result<ProjectResourceDto>> CreateAsync(CreateProjectResourceDto dto, CancellationToken ct = default);
    Task<Result<ProjectResourceDto>> UpdateAsync(string id, UpdateProjectResourceDto dto, CancellationToken ct = default);
    Task<Result<bool>> DeleteAsync(string id, CancellationToken ct = default);
    Task<Result<ProjectResourceDto>> MarkObtainedAsync(string id, bool obtained, CancellationToken ct = default);
}

public interface IProjectMilestoneService
{
    Task<Result<ProjectMilestoneDto>> GetByIdAsync(string id, CancellationToken ct = default);
    Task<Result<IReadOnlyList<ProjectMilestoneDto>>> GetByProjectIdAsync(string projectId, CancellationToken ct = default);
    Task<Result<ProjectMilestoneDto>> CreateAsync(CreateProjectMilestoneDto dto, CancellationToken ct = default);
    Task<Result<ProjectMilestoneDto>> UpdateAsync(string id, UpdateProjectMilestoneDto dto, CancellationToken ct = default);
    Task<Result<bool>> DeleteAsync(string id, CancellationToken ct = default);
    Task<Result<ProjectMilestoneDto>> CompleteAsync(string id, CancellationToken ct = default);
}

public interface IProjectSupportService
{
    Task<Result<ProjectSupportDto>> GetByIdAsync(string id, CancellationToken ct = default);
    Task<Result<IReadOnlyList<ProjectSupportDto>>> GetByProjectIdAsync(string projectId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<ProjectSupportDto>>> GetByUserIdAsync(string userId, CancellationToken ct = default);
    Task<Result<ProjectSupportDto>> CreateAsync(CreateProjectSupportDto dto, CancellationToken ct = default);
    Task<Result<bool>> DeleteAsync(string id, CancellationToken ct = default);
    Task<Result<ProjectSupportDto>> ToggleActiveAsync(string id, CancellationToken ct = default);
}

public interface IProjectApplicationService
{
    Task<Result<ProjectApplicationDto>> GetByIdAsync(string id, CancellationToken ct = default);
    Task<Result<IReadOnlyList<ProjectApplicationDto>>> GetByProjectIdAsync(string projectId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<ProjectApplicationDto>>> GetByUserIdAsync(string userId, CancellationToken ct = default);
    Task<Result<ProjectApplicationDto>> CreateAsync(CreateProjectApplicationDto dto, CancellationToken ct = default);
    Task<Result<bool>> DeleteAsync(string id, CancellationToken ct = default);
    Task<Result<ProjectApplicationDto>> AcceptAsync(string id, string? reviewMessage, CancellationToken ct = default);
    Task<Result<ProjectApplicationDto>> RejectAsync(string id, string? reviewMessage, CancellationToken ct = default);
    Task<Result<ProjectApplicationDto>> WithdrawAsync(string id, CancellationToken ct = default);
}

public interface IProjectCommentService
{
    Task<Result<ProjectCommentDto>> GetByIdAsync(string id, CancellationToken ct = default);
    Task<Result<IReadOnlyList<ProjectCommentDto>>> GetByProjectIdAsync(string projectId, CancellationToken ct = default);
    Task<Result<ProjectCommentDto>> CreateAsync(CreateProjectCommentDto dto, CancellationToken ct = default);
    Task<Result<ProjectCommentDto>> UpdateAsync(string id, string content, CancellationToken ct = default);
    Task<Result<bool>> DeleteAsync(string id, CancellationToken ct = default);
}

public interface IProjectUpdateService
{
    Task<Result<ProjectUpdateDto>> GetByIdAsync(string id, CancellationToken ct = default);
    Task<Result<IReadOnlyList<ProjectUpdateDto>>> GetByProjectIdAsync(string projectId, CancellationToken ct = default);
    Task<Result<ProjectUpdateDto>> CreateAsync(CreateProjectUpdateDto dto, CancellationToken ct = default);
    Task<Result<bool>> DeleteAsync(string id, CancellationToken ct = default);
}

public interface IProjectEquityService
{
    Task<Result<ProjectEquityDto>> GetByIdAsync(string id, CancellationToken ct = default);
    Task<Result<IReadOnlyList<ProjectEquityDto>>> GetByProjectIdAsync(string projectId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<ProjectEquityDto>>> GetByUserIdAsync(string userId, CancellationToken ct = default);
    Task<Result<ProjectEquityDto>> CreateAsync(CreateProjectEquityDto dto, CancellationToken ct = default);
    Task<Result<ProjectEquityDto>> UpdateAsync(string id, UpdateProjectEquityDto dto, CancellationToken ct = default);
    Task<Result<bool>> DeleteAsync(string id, CancellationToken ct = default);
}

public interface IProjectMemberService
{
    Task<Result<ProjectMemberDto>> GetByIdAsync(string id, CancellationToken ct = default);
    Task<Result<IReadOnlyList<ProjectMemberDto>>> GetByProjectIdAsync(string projectId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<ProjectMemberDto>>> GetByUserIdAsync(string userId, CancellationToken ct = default);
    Task<Result<ProjectMemberDto>> CreateAsync(CreateProjectMemberDto dto, CancellationToken ct = default);
    Task<Result<ProjectMemberDto>> UpdateAsync(string id, UpdateProjectMemberDto dto, CancellationToken ct = default);
    Task<Result<bool>> DeleteAsync(string id, CancellationToken ct = default);
}

public interface IProjectInvitationService
{
    Task<Result<ProjectInvitationDto>> GetByIdAsync(string id, CancellationToken ct = default);
    Task<Result<IReadOnlyList<ProjectInvitationDto>>> GetByProjectIdAsync(string projectId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<ProjectInvitationDto>>> GetByUserIdAsync(string userId, CancellationToken ct = default);
    Task<Result<ProjectInvitationDto>> CreateAsync(CreateProjectInvitationDto dto, CancellationToken ct = default);
    Task<Result<ProjectInvitationDto>> AcceptAsync(string id, CancellationToken ct = default);
    Task<Result<ProjectInvitationDto>> RejectAsync(string id, CancellationToken ct = default);
    Task<Result<bool>> DeleteAsync(string id, CancellationToken ct = default);
}
