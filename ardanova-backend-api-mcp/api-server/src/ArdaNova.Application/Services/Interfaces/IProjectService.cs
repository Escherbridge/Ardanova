namespace ArdaNova.Application.Services.Interfaces;

using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Domain.Models.Enums;

public interface IProjectService
{
    Task<Result<ProjectDto>> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Result<ProjectDto>> GetBySlugAsync(string slug, CancellationToken ct = default);
    Task<Result<IReadOnlyList<ProjectDto>>> GetAllAsync(CancellationToken ct = default);
    Task<Result<PagedResult<ProjectDto>>> GetPagedAsync(int page, int pageSize, CancellationToken ct = default);
    Task<Result<IReadOnlyList<ProjectDto>>> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<ProjectDto>>> GetByStatusAsync(ProjectStatus status, CancellationToken ct = default);
    Task<Result<IReadOnlyList<ProjectDto>>> GetByCategory(ProjectCategory category, CancellationToken ct = default);
    Task<Result<IReadOnlyList<ProjectDto>>> GetFeaturedAsync(CancellationToken ct = default);
    Task<Result<ProjectDto>> CreateAsync(CreateProjectDto dto, CancellationToken ct = default);
    Task<Result<ProjectDto>> UpdateAsync(Guid id, UpdateProjectDto dto, CancellationToken ct = default);
    Task<Result<bool>> DeleteAsync(Guid id, CancellationToken ct = default);
    Task<Result<ProjectDto>> PublishAsync(Guid id, CancellationToken ct = default);
    Task<Result<ProjectDto>> SetFeaturedAsync(Guid id, bool featured, CancellationToken ct = default);
}

public interface IProjectTaskService
{
    Task<Result<ProjectTaskDto>> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Result<IReadOnlyList<ProjectTaskDto>>> GetByProjectIdAsync(Guid projectId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<ProjectTaskDto>>> GetByAssigneeIdAsync(Guid userId, CancellationToken ct = default);
    Task<Result<ProjectTaskDto>> CreateAsync(CreateProjectTaskDto dto, CancellationToken ct = default);
    Task<Result<ProjectTaskDto>> UpdateAsync(Guid id, UpdateProjectTaskDto dto, CancellationToken ct = default);
    Task<Result<bool>> DeleteAsync(Guid id, CancellationToken ct = default);
    Task<Result<ProjectTaskDto>> AssignAsync(Guid id, Guid? userId, CancellationToken ct = default);
    Task<Result<ProjectTaskDto>> UpdateStatusAsync(Guid id, TaskStatus status, CancellationToken ct = default);
}

public interface IProjectTaskDependencyService
{
    Task<Result<ProjectTaskDependencyDto>> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Result<IReadOnlyList<ProjectTaskDependencyDto>>> GetByTaskIdAsync(Guid taskId, CancellationToken ct = default);
    Task<Result<ProjectTaskDependencyDto>> CreateAsync(CreateProjectTaskDependencyDto dto, CancellationToken ct = default);
    Task<Result<bool>> DeleteAsync(Guid id, CancellationToken ct = default);
}

public interface IProjectResourceService
{
    Task<Result<ProjectResourceDto>> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Result<IReadOnlyList<ProjectResourceDto>>> GetByProjectIdAsync(Guid projectId, CancellationToken ct = default);
    Task<Result<ProjectResourceDto>> CreateAsync(CreateProjectResourceDto dto, CancellationToken ct = default);
    Task<Result<ProjectResourceDto>> UpdateAsync(Guid id, UpdateProjectResourceDto dto, CancellationToken ct = default);
    Task<Result<bool>> DeleteAsync(Guid id, CancellationToken ct = default);
    Task<Result<ProjectResourceDto>> MarkObtainedAsync(Guid id, bool obtained, CancellationToken ct = default);
}

public interface IProjectMilestoneService
{
    Task<Result<ProjectMilestoneDto>> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Result<IReadOnlyList<ProjectMilestoneDto>>> GetByProjectIdAsync(Guid projectId, CancellationToken ct = default);
    Task<Result<ProjectMilestoneDto>> CreateAsync(CreateProjectMilestoneDto dto, CancellationToken ct = default);
    Task<Result<ProjectMilestoneDto>> UpdateAsync(Guid id, UpdateProjectMilestoneDto dto, CancellationToken ct = default);
    Task<Result<bool>> DeleteAsync(Guid id, CancellationToken ct = default);
    Task<Result<ProjectMilestoneDto>> CompleteAsync(Guid id, CancellationToken ct = default);
}

public interface IProjectSupportService
{
    Task<Result<ProjectSupportDto>> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Result<IReadOnlyList<ProjectSupportDto>>> GetByProjectIdAsync(Guid projectId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<ProjectSupportDto>>> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<Result<ProjectSupportDto>> CreateAsync(CreateProjectSupportDto dto, CancellationToken ct = default);
    Task<Result<bool>> DeleteAsync(Guid id, CancellationToken ct = default);
    Task<Result<ProjectSupportDto>> ToggleActiveAsync(Guid id, CancellationToken ct = default);
}

public interface IProjectApplicationService
{
    Task<Result<ProjectApplicationDto>> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Result<IReadOnlyList<ProjectApplicationDto>>> GetByProjectIdAsync(Guid projectId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<ProjectApplicationDto>>> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<Result<ProjectApplicationDto>> CreateAsync(CreateProjectApplicationDto dto, CancellationToken ct = default);
    Task<Result<bool>> DeleteAsync(Guid id, CancellationToken ct = default);
    Task<Result<ProjectApplicationDto>> AcceptAsync(Guid id, string? reviewMessage, CancellationToken ct = default);
    Task<Result<ProjectApplicationDto>> RejectAsync(Guid id, string? reviewMessage, CancellationToken ct = default);
    Task<Result<ProjectApplicationDto>> WithdrawAsync(Guid id, CancellationToken ct = default);
}

public interface IProjectCommentService
{
    Task<Result<ProjectCommentDto>> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Result<IReadOnlyList<ProjectCommentDto>>> GetByProjectIdAsync(Guid projectId, CancellationToken ct = default);
    Task<Result<ProjectCommentDto>> CreateAsync(CreateProjectCommentDto dto, CancellationToken ct = default);
    Task<Result<ProjectCommentDto>> UpdateAsync(Guid id, string content, CancellationToken ct = default);
    Task<Result<bool>> DeleteAsync(Guid id, CancellationToken ct = default);
}

public interface IProjectUpdateService
{
    Task<Result<ProjectUpdateDto>> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Result<IReadOnlyList<ProjectUpdateDto>>> GetByProjectIdAsync(Guid projectId, CancellationToken ct = default);
    Task<Result<ProjectUpdateDto>> CreateAsync(CreateProjectUpdateDto dto, CancellationToken ct = default);
    Task<Result<bool>> DeleteAsync(Guid id, CancellationToken ct = default);
}

public interface IProjectEquityService
{
    Task<Result<ProjectEquityDto>> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Result<IReadOnlyList<ProjectEquityDto>>> GetByProjectIdAsync(Guid projectId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<ProjectEquityDto>>> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<Result<ProjectEquityDto>> CreateAsync(CreateProjectEquityDto dto, CancellationToken ct = default);
    Task<Result<ProjectEquityDto>> UpdateAsync(Guid id, UpdateProjectEquityDto dto, CancellationToken ct = default);
    Task<Result<bool>> DeleteAsync(Guid id, CancellationToken ct = default);
}
