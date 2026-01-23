namespace ArdaNova.Application.Services.Implementations;

using ArdaNova.Application.Common.Interfaces;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Interfaces;
using ArdaNova.Domain.Models.Entities;
using ArdaNova.Domain.Models.Enums;
using AutoMapper;
using TaskStatus = ArdaNova.Domain.Models.Enums.TaskStatus;

public class ProjectService : IProjectService
{
    private readonly IRepository<Project> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public ProjectService(IRepository<Project> repository, IUnitOfWork unitOfWork, IMapper mapper)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<Result<ProjectDto>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var project = await _repository.GetByIdAsync(id, ct);
        if (project is null)
            return Result<ProjectDto>.NotFound($"Project with id {id} not found");
        return Result<ProjectDto>.Success(_mapper.Map<ProjectDto>(project));
    }

    public async Task<Result<ProjectDto>> GetBySlugAsync(string slug, CancellationToken ct = default)
    {
        var project = await _repository.FindOneAsync(p => p.Slug == slug, ct);
        if (project is null)
            return Result<ProjectDto>.NotFound($"Project with slug {slug} not found");
        return Result<ProjectDto>.Success(_mapper.Map<ProjectDto>(project));
    }

    public async Task<Result<IReadOnlyList<ProjectDto>>> GetAllAsync(CancellationToken ct = default)
    {
        var projects = await _repository.GetAllAsync(ct);
        return Result<IReadOnlyList<ProjectDto>>.Success(_mapper.Map<IReadOnlyList<ProjectDto>>(projects));
    }

    public async Task<Result<PagedResult<ProjectDto>>> GetPagedAsync(int page, int pageSize, CancellationToken ct = default)
    {
        var result = await _repository.GetPagedAsync(page, pageSize, null, ct);
        return Result<PagedResult<ProjectDto>>.Success(result.Map(_mapper.Map<ProjectDto>));
    }

    public async Task<Result<IReadOnlyList<ProjectDto>>> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
    {
        var projects = await _repository.FindAsync(p => p.CreatedById == userId, ct);
        return Result<IReadOnlyList<ProjectDto>>.Success(_mapper.Map<IReadOnlyList<ProjectDto>>(projects));
    }

    public async Task<Result<IReadOnlyList<ProjectDto>>> GetByStatusAsync(ProjectStatus status, CancellationToken ct = default)
    {
        var projects = await _repository.FindAsync(p => p.Status == status, ct);
        return Result<IReadOnlyList<ProjectDto>>.Success(_mapper.Map<IReadOnlyList<ProjectDto>>(projects));
    }

    public async Task<Result<IReadOnlyList<ProjectDto>>> GetByCategory(ProjectCategory category, CancellationToken ct = default)
    {
        var projects = await _repository.FindAsync(p => p.Category == category, ct);
        return Result<IReadOnlyList<ProjectDto>>.Success(_mapper.Map<IReadOnlyList<ProjectDto>>(projects));
    }

    public async Task<Result<IReadOnlyList<ProjectDto>>> GetFeaturedAsync(CancellationToken ct = default)
    {
        var projects = await _repository.FindAsync(p => p.Featured, ct);
        return Result<IReadOnlyList<ProjectDto>>.Success(_mapper.Map<IReadOnlyList<ProjectDto>>(projects));
    }

    public async Task<Result<ProjectDto>> CreateAsync(CreateProjectDto dto, CancellationToken ct = default)
    {
        var project = Project.Create(dto.CreatedById, dto.Title, dto.Description, dto.ProblemStatement, dto.Solution, dto.Category, dto.FundingGoal);
        project.SetDetails(dto.Tags, dto.TargetAudience, dto.ExpectedImpact, dto.Timeline);
        project.SetMedia(dto.Images, dto.Videos, dto.Documents);
        await _repository.AddAsync(project, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<ProjectDto>.Success(_mapper.Map<ProjectDto>(project));
    }

    public async Task<Result<ProjectDto>> UpdateAsync(Guid id, UpdateProjectDto dto, CancellationToken ct = default)
    {
        var project = await _repository.GetByIdAsync(id, ct);
        if (project is null)
            return Result<ProjectDto>.NotFound($"Project with id {id} not found");

        if (dto.Title is not null || dto.Description is not null || dto.ProblemStatement is not null || dto.Solution is not null || dto.Category.HasValue)
        {
            project.Update(
                dto.Title ?? project.Title,
                dto.Description ?? project.Description,
                dto.ProblemStatement ?? project.ProblemStatement,
                dto.Solution ?? project.Solution,
                dto.Category ?? project.Category
            );
        }

        if (dto.Status.HasValue)
        {
            project.SetStatus(dto.Status.Value);
        }

        project.SetMedia(dto.Images ?? project.Images, dto.Videos ?? project.Videos, dto.Documents ?? project.Documents);
        project.SetDetails(dto.Tags ?? project.Tags, dto.TargetAudience ?? project.TargetAudience, dto.ExpectedImpact ?? project.ExpectedImpact, dto.Timeline ?? project.Timeline);

        await _repository.UpdateAsync(project, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<ProjectDto>.Success(_mapper.Map<ProjectDto>(project));
    }

    public async Task<Result<bool>> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var project = await _repository.GetByIdAsync(id, ct);
        if (project is null)
            return Result<bool>.NotFound($"Project with id {id} not found");

        await _repository.DeleteAsync(project, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<bool>.Success(true);
    }

    public async Task<Result<ProjectDto>> PublishAsync(Guid id, CancellationToken ct = default)
    {
        var project = await _repository.GetByIdAsync(id, ct);
        if (project is null)
            return Result<ProjectDto>.NotFound($"Project with id {id} not found");

        project.Publish();
        await _repository.UpdateAsync(project, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<ProjectDto>.Success(_mapper.Map<ProjectDto>(project));
    }

    public async Task<Result<ProjectDto>> SetFeaturedAsync(Guid id, bool featured, CancellationToken ct = default)
    {
        var project = await _repository.GetByIdAsync(id, ct);
        if (project is null)
            return Result<ProjectDto>.NotFound($"Project with id {id} not found");

        project.SetFeatured(featured);
        await _repository.UpdateAsync(project, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<ProjectDto>.Success(_mapper.Map<ProjectDto>(project));
    }
}

public class ProjectTaskService : IProjectTaskService
{
    private readonly IRepository<ProjectTask> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public ProjectTaskService(IRepository<ProjectTask> repository, IUnitOfWork unitOfWork, IMapper mapper)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<Result<ProjectTaskDto>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var task = await _repository.GetByIdAsync(id, ct);
        if (task is null)
            return Result<ProjectTaskDto>.NotFound($"Task with id {id} not found");
        return Result<ProjectTaskDto>.Success(_mapper.Map<ProjectTaskDto>(task));
    }

    public async Task<Result<IReadOnlyList<ProjectTaskDto>>> GetByProjectIdAsync(Guid projectId, CancellationToken ct = default)
    {
        var tasks = await _repository.FindAsync(t => t.ProjectId == projectId, ct);
        return Result<IReadOnlyList<ProjectTaskDto>>.Success(_mapper.Map<IReadOnlyList<ProjectTaskDto>>(tasks));
    }

    public async Task<Result<IReadOnlyList<ProjectTaskDto>>> GetByAssigneeIdAsync(Guid userId, CancellationToken ct = default)
    {
        var tasks = await _repository.FindAsync(t => t.AssignedToId == userId, ct);
        return Result<IReadOnlyList<ProjectTaskDto>>.Success(_mapper.Map<IReadOnlyList<ProjectTaskDto>>(tasks));
    }

    public async Task<Result<ProjectTaskDto>> CreateAsync(CreateProjectTaskDto dto, CancellationToken ct = default)
    {
        var task = ProjectTask.Create(dto.ProjectId, dto.Title, dto.Description, dto.Priority, dto.EstimatedHours, dto.DueDate, dto.AssignedToId);
        await _repository.AddAsync(task, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<ProjectTaskDto>.Success(_mapper.Map<ProjectTaskDto>(task));
    }

    public async Task<Result<ProjectTaskDto>> UpdateAsync(Guid id, UpdateProjectTaskDto dto, CancellationToken ct = default)
    {
        var task = await _repository.GetByIdAsync(id, ct);
        if (task is null)
            return Result<ProjectTaskDto>.NotFound($"Task with id {id} not found");

        task.Update(
            dto.Title ?? task.Title,
            dto.Description ?? task.Description,
            dto.Priority ?? task.Priority,
            dto.EstimatedHours ?? task.EstimatedHours,
            dto.DueDate ?? task.DueDate
        );

        if (dto.AssignedToId.HasValue)
            task.AssignTo(dto.AssignedToId);

        await _repository.UpdateAsync(task, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<ProjectTaskDto>.Success(_mapper.Map<ProjectTaskDto>(task));
    }

    public async Task<Result<bool>> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var task = await _repository.GetByIdAsync(id, ct);
        if (task is null)
            return Result<bool>.NotFound($"Task with id {id} not found");

        await _repository.DeleteAsync(task, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<bool>.Success(true);
    }

    public async Task<Result<ProjectTaskDto>> AssignAsync(Guid id, Guid? userId, CancellationToken ct = default)
    {
        var task = await _repository.GetByIdAsync(id, ct);
        if (task is null)
            return Result<ProjectTaskDto>.NotFound($"Task with id {id} not found");

        task.AssignTo(userId);
        await _repository.UpdateAsync(task, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<ProjectTaskDto>.Success(_mapper.Map<ProjectTaskDto>(task));
    }

    public async Task<Result<ProjectTaskDto>> UpdateStatusAsync(Guid id, TaskStatus status, CancellationToken ct = default)
    {
        var task = await _repository.GetByIdAsync(id, ct);
        if (task is null)
            return Result<ProjectTaskDto>.NotFound($"Task with id {id} not found");

        switch (status)
        {
            case TaskStatus.IN_PROGRESS: task.StartProgress(); break;
            case TaskStatus.REVIEW: task.SubmitForReview(); break;
            case TaskStatus.COMPLETED: task.Complete(); break;
            case TaskStatus.BLOCKED: task.Block(); break;
            case TaskStatus.TODO: task.Reopen(); break;
        }

        await _repository.UpdateAsync(task, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<ProjectTaskDto>.Success(_mapper.Map<ProjectTaskDto>(task));
    }
}

public class ProjectTaskDependencyService : IProjectTaskDependencyService
{
    private readonly IRepository<ProjectTaskDependency> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public ProjectTaskDependencyService(IRepository<ProjectTaskDependency> repository, IUnitOfWork unitOfWork, IMapper mapper)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<Result<ProjectTaskDependencyDto>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var dep = await _repository.GetByIdAsync(id, ct);
        if (dep is null)
            return Result<ProjectTaskDependencyDto>.NotFound($"Dependency with id {id} not found");
        return Result<ProjectTaskDependencyDto>.Success(_mapper.Map<ProjectTaskDependencyDto>(dep));
    }

    public async Task<Result<IReadOnlyList<ProjectTaskDependencyDto>>> GetByTaskIdAsync(Guid taskId, CancellationToken ct = default)
    {
        var deps = await _repository.FindAsync(d => d.TaskId == taskId, ct);
        return Result<IReadOnlyList<ProjectTaskDependencyDto>>.Success(_mapper.Map<IReadOnlyList<ProjectTaskDependencyDto>>(deps));
    }

    public async Task<Result<ProjectTaskDependencyDto>> CreateAsync(CreateProjectTaskDependencyDto dto, CancellationToken ct = default)
    {
        var dep = ProjectTaskDependency.Create(dto.TaskId, dto.DependsOnId);
        await _repository.AddAsync(dep, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<ProjectTaskDependencyDto>.Success(_mapper.Map<ProjectTaskDependencyDto>(dep));
    }

    public async Task<Result<bool>> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var dep = await _repository.GetByIdAsync(id, ct);
        if (dep is null)
            return Result<bool>.NotFound($"Dependency with id {id} not found");

        await _repository.DeleteAsync(dep, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<bool>.Success(true);
    }
}

public class ProjectResourceService : IProjectResourceService
{
    private readonly IRepository<ProjectResource> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public ProjectResourceService(IRepository<ProjectResource> repository, IUnitOfWork unitOfWork, IMapper mapper)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<Result<ProjectResourceDto>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var resource = await _repository.GetByIdAsync(id, ct);
        if (resource is null)
            return Result<ProjectResourceDto>.NotFound($"Resource with id {id} not found");
        return Result<ProjectResourceDto>.Success(_mapper.Map<ProjectResourceDto>(resource));
    }

    public async Task<Result<IReadOnlyList<ProjectResourceDto>>> GetByProjectIdAsync(Guid projectId, CancellationToken ct = default)
    {
        var resources = await _repository.FindAsync(r => r.ProjectId == projectId, ct);
        return Result<IReadOnlyList<ProjectResourceDto>>.Success(_mapper.Map<IReadOnlyList<ProjectResourceDto>>(resources));
    }

    public async Task<Result<ProjectResourceDto>> CreateAsync(CreateProjectResourceDto dto, CancellationToken ct = default)
    {
        var resource = ProjectResource.Create(dto.ProjectId, dto.Name, dto.Description, dto.Quantity, dto.EstimatedCost, dto.IsRequired);
        await _repository.AddAsync(resource, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<ProjectResourceDto>.Success(_mapper.Map<ProjectResourceDto>(resource));
    }

    public async Task<Result<ProjectResourceDto>> UpdateAsync(Guid id, UpdateProjectResourceDto dto, CancellationToken ct = default)
    {
        var resource = await _repository.GetByIdAsync(id, ct);
        if (resource is null)
            return Result<ProjectResourceDto>.NotFound($"Resource with id {id} not found");

        resource.Update(
            dto.Name ?? resource.Name,
            dto.Description ?? resource.Description,
            dto.Quantity ?? resource.Quantity,
            dto.EstimatedCost ?? resource.EstimatedCost,
            dto.IsRequired ?? resource.IsRequired
        );

        if (dto.IsObtained.HasValue)
        {
            if (dto.IsObtained.Value) resource.MarkObtained();
            else resource.MarkNotObtained();
        }

        await _repository.UpdateAsync(resource, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<ProjectResourceDto>.Success(_mapper.Map<ProjectResourceDto>(resource));
    }

    public async Task<Result<bool>> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var resource = await _repository.GetByIdAsync(id, ct);
        if (resource is null)
            return Result<bool>.NotFound($"Resource with id {id} not found");

        await _repository.DeleteAsync(resource, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<bool>.Success(true);
    }

    public async Task<Result<ProjectResourceDto>> MarkObtainedAsync(Guid id, bool obtained, CancellationToken ct = default)
    {
        var resource = await _repository.GetByIdAsync(id, ct);
        if (resource is null)
            return Result<ProjectResourceDto>.NotFound($"Resource with id {id} not found");

        if (obtained) resource.MarkObtained();
        else resource.MarkNotObtained();

        await _repository.UpdateAsync(resource, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<ProjectResourceDto>.Success(_mapper.Map<ProjectResourceDto>(resource));
    }
}

public class ProjectMilestoneService : IProjectMilestoneService
{
    private readonly IRepository<ProjectMilestone> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public ProjectMilestoneService(IRepository<ProjectMilestone> repository, IUnitOfWork unitOfWork, IMapper mapper)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<Result<ProjectMilestoneDto>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var milestone = await _repository.GetByIdAsync(id, ct);
        if (milestone is null)
            return Result<ProjectMilestoneDto>.NotFound($"Milestone with id {id} not found");
        return Result<ProjectMilestoneDto>.Success(_mapper.Map<ProjectMilestoneDto>(milestone));
    }

    public async Task<Result<IReadOnlyList<ProjectMilestoneDto>>> GetByProjectIdAsync(Guid projectId, CancellationToken ct = default)
    {
        var milestones = await _repository.FindAsync(m => m.ProjectId == projectId, ct);
        return Result<IReadOnlyList<ProjectMilestoneDto>>.Success(_mapper.Map<IReadOnlyList<ProjectMilestoneDto>>(milestones));
    }

    public async Task<Result<ProjectMilestoneDto>> CreateAsync(CreateProjectMilestoneDto dto, CancellationToken ct = default)
    {
        var milestone = ProjectMilestone.Create(dto.ProjectId, dto.Title, dto.TargetDate, dto.Description);
        await _repository.AddAsync(milestone, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<ProjectMilestoneDto>.Success(_mapper.Map<ProjectMilestoneDto>(milestone));
    }

    public async Task<Result<ProjectMilestoneDto>> UpdateAsync(Guid id, UpdateProjectMilestoneDto dto, CancellationToken ct = default)
    {
        var milestone = await _repository.GetByIdAsync(id, ct);
        if (milestone is null)
            return Result<ProjectMilestoneDto>.NotFound($"Milestone with id {id} not found");

        milestone.Update(
            dto.Title ?? milestone.Title,
            dto.Description ?? milestone.Description,
            dto.TargetDate ?? milestone.TargetDate
        );

        await _repository.UpdateAsync(milestone, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<ProjectMilestoneDto>.Success(_mapper.Map<ProjectMilestoneDto>(milestone));
    }

    public async Task<Result<bool>> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var milestone = await _repository.GetByIdAsync(id, ct);
        if (milestone is null)
            return Result<bool>.NotFound($"Milestone with id {id} not found");

        await _repository.DeleteAsync(milestone, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<bool>.Success(true);
    }

    public async Task<Result<ProjectMilestoneDto>> CompleteAsync(Guid id, CancellationToken ct = default)
    {
        var milestone = await _repository.GetByIdAsync(id, ct);
        if (milestone is null)
            return Result<ProjectMilestoneDto>.NotFound($"Milestone with id {id} not found");

        milestone.Complete();
        await _repository.UpdateAsync(milestone, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<ProjectMilestoneDto>.Success(_mapper.Map<ProjectMilestoneDto>(milestone));
    }
}

public class ProjectSupportService : IProjectSupportService
{
    private readonly IRepository<ProjectSupport> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public ProjectSupportService(IRepository<ProjectSupport> repository, IUnitOfWork unitOfWork, IMapper mapper)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<Result<ProjectSupportDto>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var support = await _repository.GetByIdAsync(id, ct);
        if (support is null)
            return Result<ProjectSupportDto>.NotFound($"Support with id {id} not found");
        return Result<ProjectSupportDto>.Success(_mapper.Map<ProjectSupportDto>(support));
    }

    public async Task<Result<IReadOnlyList<ProjectSupportDto>>> GetByProjectIdAsync(Guid projectId, CancellationToken ct = default)
    {
        var supports = await _repository.FindAsync(s => s.ProjectId == projectId, ct);
        return Result<IReadOnlyList<ProjectSupportDto>>.Success(_mapper.Map<IReadOnlyList<ProjectSupportDto>>(supports));
    }

    public async Task<Result<IReadOnlyList<ProjectSupportDto>>> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
    {
        var supports = await _repository.FindAsync(s => s.UserId == userId, ct);
        return Result<IReadOnlyList<ProjectSupportDto>>.Success(_mapper.Map<IReadOnlyList<ProjectSupportDto>>(supports));
    }

    public async Task<Result<ProjectSupportDto>> CreateAsync(CreateProjectSupportDto dto, CancellationToken ct = default)
    {
        var support = ProjectSupport.Create(dto.ProjectId, dto.UserId, dto.SupportType, dto.MonthlyAmount, dto.Message);
        await _repository.AddAsync(support, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<ProjectSupportDto>.Success(_mapper.Map<ProjectSupportDto>(support));
    }

    public async Task<Result<bool>> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var support = await _repository.GetByIdAsync(id, ct);
        if (support is null)
            return Result<bool>.NotFound($"Support with id {id} not found");

        await _repository.DeleteAsync(support, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<bool>.Success(true);
    }

    public async Task<Result<ProjectSupportDto>> ToggleActiveAsync(Guid id, CancellationToken ct = default)
    {
        var support = await _repository.GetByIdAsync(id, ct);
        if (support is null)
            return Result<ProjectSupportDto>.NotFound($"Support with id {id} not found");

        if (support.IsActive) support.Deactivate();
        else support.Reactivate();

        await _repository.UpdateAsync(support, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<ProjectSupportDto>.Success(_mapper.Map<ProjectSupportDto>(support));
    }
}

public class ProjectApplicationService : IProjectApplicationService
{
    private readonly IRepository<ProjectApplication> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public ProjectApplicationService(IRepository<ProjectApplication> repository, IUnitOfWork unitOfWork, IMapper mapper)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<Result<ProjectApplicationDto>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var app = await _repository.GetByIdAsync(id, ct);
        if (app is null)
            return Result<ProjectApplicationDto>.NotFound($"Application with id {id} not found");
        return Result<ProjectApplicationDto>.Success(_mapper.Map<ProjectApplicationDto>(app));
    }

    public async Task<Result<IReadOnlyList<ProjectApplicationDto>>> GetByProjectIdAsync(Guid projectId, CancellationToken ct = default)
    {
        var apps = await _repository.FindAsync(a => a.ProjectId == projectId, ct);
        return Result<IReadOnlyList<ProjectApplicationDto>>.Success(_mapper.Map<IReadOnlyList<ProjectApplicationDto>>(apps));
    }

    public async Task<Result<IReadOnlyList<ProjectApplicationDto>>> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
    {
        var apps = await _repository.FindAsync(a => a.UserId == userId, ct);
        return Result<IReadOnlyList<ProjectApplicationDto>>.Success(_mapper.Map<IReadOnlyList<ProjectApplicationDto>>(apps));
    }

    public async Task<Result<ProjectApplicationDto>> CreateAsync(CreateProjectApplicationDto dto, CancellationToken ct = default)
    {
        var app = ProjectApplication.Create(dto.ProjectId, dto.UserId, dto.RoleTitle, dto.Message, dto.Skills, dto.Experience, dto.Availability);
        await _repository.AddAsync(app, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<ProjectApplicationDto>.Success(_mapper.Map<ProjectApplicationDto>(app));
    }

    public async Task<Result<bool>> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var app = await _repository.GetByIdAsync(id, ct);
        if (app is null)
            return Result<bool>.NotFound($"Application with id {id} not found");

        await _repository.DeleteAsync(app, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<bool>.Success(true);
    }

    public async Task<Result<ProjectApplicationDto>> AcceptAsync(Guid id, string? reviewMessage, CancellationToken ct = default)
    {
        var app = await _repository.GetByIdAsync(id, ct);
        if (app is null)
            return Result<ProjectApplicationDto>.NotFound($"Application with id {id} not found");

        app.Accept(reviewMessage);
        await _repository.UpdateAsync(app, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<ProjectApplicationDto>.Success(_mapper.Map<ProjectApplicationDto>(app));
    }

    public async Task<Result<ProjectApplicationDto>> RejectAsync(Guid id, string? reviewMessage, CancellationToken ct = default)
    {
        var app = await _repository.GetByIdAsync(id, ct);
        if (app is null)
            return Result<ProjectApplicationDto>.NotFound($"Application with id {id} not found");

        app.Reject(reviewMessage);
        await _repository.UpdateAsync(app, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<ProjectApplicationDto>.Success(_mapper.Map<ProjectApplicationDto>(app));
    }

    public async Task<Result<ProjectApplicationDto>> WithdrawAsync(Guid id, CancellationToken ct = default)
    {
        var app = await _repository.GetByIdAsync(id, ct);
        if (app is null)
            return Result<ProjectApplicationDto>.NotFound($"Application with id {id} not found");

        app.Withdraw();
        await _repository.UpdateAsync(app, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<ProjectApplicationDto>.Success(_mapper.Map<ProjectApplicationDto>(app));
    }
}

public class ProjectCommentService : IProjectCommentService
{
    private readonly IRepository<ProjectComment> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public ProjectCommentService(IRepository<ProjectComment> repository, IUnitOfWork unitOfWork, IMapper mapper)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<Result<ProjectCommentDto>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var comment = await _repository.GetByIdAsync(id, ct);
        if (comment is null)
            return Result<ProjectCommentDto>.NotFound($"Comment with id {id} not found");
        return Result<ProjectCommentDto>.Success(_mapper.Map<ProjectCommentDto>(comment));
    }

    public async Task<Result<IReadOnlyList<ProjectCommentDto>>> GetByProjectIdAsync(Guid projectId, CancellationToken ct = default)
    {
        var comments = await _repository.FindAsync(c => c.ProjectId == projectId, ct);
        return Result<IReadOnlyList<ProjectCommentDto>>.Success(_mapper.Map<IReadOnlyList<ProjectCommentDto>>(comments));
    }

    public async Task<Result<ProjectCommentDto>> CreateAsync(CreateProjectCommentDto dto, CancellationToken ct = default)
    {
        var comment = ProjectComment.Create(dto.ProjectId, dto.UserId, dto.Content, dto.ParentId);
        await _repository.AddAsync(comment, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<ProjectCommentDto>.Success(_mapper.Map<ProjectCommentDto>(comment));
    }

    public async Task<Result<ProjectCommentDto>> UpdateAsync(Guid id, string content, CancellationToken ct = default)
    {
        var comment = await _repository.GetByIdAsync(id, ct);
        if (comment is null)
            return Result<ProjectCommentDto>.NotFound($"Comment with id {id} not found");

        comment.UpdateContent(content);
        await _repository.UpdateAsync(comment, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<ProjectCommentDto>.Success(_mapper.Map<ProjectCommentDto>(comment));
    }

    public async Task<Result<bool>> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var comment = await _repository.GetByIdAsync(id, ct);
        if (comment is null)
            return Result<bool>.NotFound($"Comment with id {id} not found");

        await _repository.DeleteAsync(comment, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<bool>.Success(true);
    }
}

public class ProjectUpdateService : IProjectUpdateService
{
    private readonly IRepository<ProjectUpdate> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public ProjectUpdateService(IRepository<ProjectUpdate> repository, IUnitOfWork unitOfWork, IMapper mapper)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<Result<ProjectUpdateDto>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var update = await _repository.GetByIdAsync(id, ct);
        if (update is null)
            return Result<ProjectUpdateDto>.NotFound($"Update with id {id} not found");
        return Result<ProjectUpdateDto>.Success(_mapper.Map<ProjectUpdateDto>(update));
    }

    public async Task<Result<IReadOnlyList<ProjectUpdateDto>>> GetByProjectIdAsync(Guid projectId, CancellationToken ct = default)
    {
        var updates = await _repository.FindAsync(u => u.ProjectId == projectId, ct);
        return Result<IReadOnlyList<ProjectUpdateDto>>.Success(_mapper.Map<IReadOnlyList<ProjectUpdateDto>>(updates));
    }

    public async Task<Result<ProjectUpdateDto>> CreateAsync(CreateProjectUpdateDto dto, CancellationToken ct = default)
    {
        var update = ProjectUpdate.Create(dto.ProjectId, dto.UserId, dto.Title, dto.Content, dto.Images);
        await _repository.AddAsync(update, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<ProjectUpdateDto>.Success(_mapper.Map<ProjectUpdateDto>(update));
    }

    public async Task<Result<bool>> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var update = await _repository.GetByIdAsync(id, ct);
        if (update is null)
            return Result<bool>.NotFound($"Update with id {id} not found");

        await _repository.DeleteAsync(update, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<bool>.Success(true);
    }
}

public class ProjectEquityService : IProjectEquityService
{
    private readonly IRepository<ProjectEquity> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public ProjectEquityService(IRepository<ProjectEquity> repository, IUnitOfWork unitOfWork, IMapper mapper)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<Result<ProjectEquityDto>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var equity = await _repository.GetByIdAsync(id, ct);
        if (equity is null)
            return Result<ProjectEquityDto>.NotFound($"Equity with id {id} not found");
        return Result<ProjectEquityDto>.Success(_mapper.Map<ProjectEquityDto>(equity));
    }

    public async Task<Result<IReadOnlyList<ProjectEquityDto>>> GetByProjectIdAsync(Guid projectId, CancellationToken ct = default)
    {
        var equities = await _repository.FindAsync(e => e.ProjectId == projectId, ct);
        return Result<IReadOnlyList<ProjectEquityDto>>.Success(_mapper.Map<IReadOnlyList<ProjectEquityDto>>(equities));
    }

    public async Task<Result<IReadOnlyList<ProjectEquityDto>>> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
    {
        var equities = await _repository.FindAsync(e => e.UserId == userId, ct);
        return Result<IReadOnlyList<ProjectEquityDto>>.Success(_mapper.Map<IReadOnlyList<ProjectEquityDto>>(equities));
    }

    public async Task<Result<ProjectEquityDto>> CreateAsync(CreateProjectEquityDto dto, CancellationToken ct = default)
    {
        var equity = ProjectEquity.Create(dto.ProjectId, dto.UserId, dto.SharePercent, dto.InvestmentAmount);
        await _repository.AddAsync(equity, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<ProjectEquityDto>.Success(_mapper.Map<ProjectEquityDto>(equity));
    }

    public async Task<Result<ProjectEquityDto>> UpdateAsync(Guid id, UpdateProjectEquityDto dto, CancellationToken ct = default)
    {
        var equity = await _repository.GetByIdAsync(id, ct);
        if (equity is null)
            return Result<ProjectEquityDto>.NotFound($"Equity with id {id} not found");

        equity.UpdateShare(dto.SharePercent ?? equity.SharePercent, dto.InvestmentAmount ?? equity.InvestmentAmount);
        await _repository.UpdateAsync(equity, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<ProjectEquityDto>.Success(_mapper.Map<ProjectEquityDto>(equity));
    }

    public async Task<Result<bool>> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var equity = await _repository.GetByIdAsync(id, ct);
        if (equity is null)
            return Result<bool>.NotFound($"Equity with id {id} not found");

        await _repository.DeleteAsync(equity, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<bool>.Success(true);
    }
}
