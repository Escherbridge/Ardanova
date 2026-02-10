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
    private readonly IProjectRepository _repository;
    private readonly IRepository<User> _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IKycGateService _kycGateService;

    public ProjectService(IProjectRepository repository, IRepository<User> userRepository, IUnitOfWork unitOfWork, IMapper mapper, IKycGateService kycGateService)
    {
        _repository = repository;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _kycGateService = kycGateService;
    }

    public async Task<Result<ProjectDto>> GetByIdAsync(string id, CancellationToken ct = default)
    {
        var project = await _repository.GetByIdAsync(id, ct);
        if (project is null)
            return Result<ProjectDto>.NotFound($"Project with id {id} not found");

        var dto = _mapper.Map<ProjectDto>(project);
        var user = await _userRepository.GetByIdAsync(project.createdById, ct);
        if (user is not null)
            dto = dto with { CreatedBy = _mapper.Map<ProjectCreatorDto>(user) };

        return Result<ProjectDto>.Success(dto);
    }

    public async Task<Result<ProjectDto>> GetBySlugAsync(string slug, CancellationToken ct = default)
    {
        var project = await _repository.FindOneAsync(p => p.slug == slug, ct);
        if (project is null)
            return Result<ProjectDto>.NotFound($"Project with slug {slug} not found");

        var dto = _mapper.Map<ProjectDto>(project);
        var user = await _userRepository.GetByIdAsync(project.createdById, ct);
        if (user is not null)
            dto = dto with { CreatedBy = _mapper.Map<ProjectCreatorDto>(user) };

        return Result<ProjectDto>.Success(dto);
    }

    // Helper method to enrich project DTOs with user data
    private async Task<IReadOnlyList<ProjectDto>> EnrichWithUserDataAsync(IEnumerable<Project> projects, CancellationToken ct)
    {
        var dtos = _mapper.Map<List<ProjectDto>>(projects);
        var userIds = projects.Select(p => p.createdById).Distinct().ToList();
        var users = new Dictionary<string, User>();

        foreach (var userId in userIds)
        {
            var user = await _userRepository.GetByIdAsync(userId, ct);
            if (user is not null)
                users[userId] = user;
        }

        return dtos.Select(dto =>
        {
            var project = projects.First(p => p.id == dto.Id);
            if (users.TryGetValue(project.createdById, out var user))
                return dto with { CreatedBy = _mapper.Map<ProjectCreatorDto>(user) };
            return dto;
        }).ToList();
    }

    public async Task<Result<IReadOnlyList<ProjectDto>>> GetAllAsync(CancellationToken ct = default)
    {
        var projects = await _repository.GetAllAsync(ct);
        var enrichedDtos = await EnrichWithUserDataAsync(projects, ct);
        return Result<IReadOnlyList<ProjectDto>>.Success(enrichedDtos);
    }

    public async Task<Result<PagedResult<ProjectDto>>> GetPagedAsync(int page, int pageSize, CancellationToken ct = default)
    {
        var result = await _repository.GetPagedAsync(page, pageSize, null, ct);
        var enrichedItems = await EnrichWithUserDataAsync(result.Items, ct);
        return Result<PagedResult<ProjectDto>>.Success(new PagedResult<ProjectDto>(
            enrichedItems.ToList(),
            result.TotalCount,
            result.Page,
            result.PageSize
        ));
    }

    public async Task<Result<PagedResult<ProjectDto>>> SearchAsync(
        string? searchTerm,
        ProjectStatus? status,
        string? category,
        ProjectType? projectType,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var result = await _repository.SearchAsync(searchTerm, status, category, projectType, page, pageSize, ct);
        var enrichedItems = await EnrichWithUserDataAsync(result.Items, ct);
        return Result<PagedResult<ProjectDto>>.Success(new PagedResult<ProjectDto>(
            enrichedItems.ToList(),
            result.TotalCount,
            result.Page,
            result.PageSize
        ));
    }

    public async Task<Result<IReadOnlyList<ProjectDto>>> GetByUserIdAsync(string userId, CancellationToken ct = default)
    {
        var projects = await _repository.FindAsync(p => p.createdById == userId, ct);
        var enrichedDtos = await EnrichWithUserDataAsync(projects, ct);
        return Result<IReadOnlyList<ProjectDto>>.Success(enrichedDtos);
    }

    public async Task<Result<IReadOnlyList<ProjectDto>>> GetByStatusAsync(ProjectStatus status, CancellationToken ct = default)
    {
        var projects = await _repository.FindAsync(p => p.status == status, ct);
        var enrichedDtos = await EnrichWithUserDataAsync(projects, ct);
        return Result<IReadOnlyList<ProjectDto>>.Success(enrichedDtos);
    }

    public async Task<Result<IReadOnlyList<ProjectDto>>> GetByCategory(string category, CancellationToken ct = default)
    {
        var projects = await _repository.FindAsync(p => p.categories.Contains(category), ct);
        var enrichedDtos = await EnrichWithUserDataAsync(projects, ct);
        return Result<IReadOnlyList<ProjectDto>>.Success(enrichedDtos);
    }

    public async Task<Result<IReadOnlyList<ProjectDto>>> GetByProjectTypeAsync(ProjectType projectType, CancellationToken ct = default)
    {
        var projects = await _repository.FindAsync(p => p.projectType == projectType, ct);
        var enrichedDtos = await EnrichWithUserDataAsync(projects, ct);
        return Result<IReadOnlyList<ProjectDto>>.Success(enrichedDtos);
    }

    public async Task<Result<IReadOnlyList<ProjectDto>>> GetFeaturedAsync(CancellationToken ct = default)
    {
        var projects = await _repository.FindAsync(p => p.featured, ct);
        var enrichedDtos = await EnrichWithUserDataAsync(projects, ct);
        return Result<IReadOnlyList<ProjectDto>>.Success(enrichedDtos);
    }

    private static readonly HashSet<string> ValidCategories = Enum.GetNames<ProjectCategory>()
        .ToHashSet(StringComparer.OrdinalIgnoreCase);

    private static Result<ProjectDto>? ValidateCategories(IEnumerable<string> categories)
    {
        foreach (var category in categories)
        {
            if (string.IsNullOrWhiteSpace(category))
                return Result<ProjectDto>.ValidationError("Category cannot be empty");
            if (!ValidCategories.Contains(category) && category.Length > 50)
                return Result<ProjectDto>.ValidationError($"Custom category '{category}' exceeds 50 characters");
        }
        return null;
    }

    public async Task<Result<ProjectDto>> CreateAsync(CreateProjectDto dto, CancellationToken ct = default)
    {
        var gateResult = await _kycGateService.RequireProAsync(dto.CreatedById, ct);
        if (!gateResult.IsSuccess)
            return Result<ProjectDto>.Forbidden(gateResult.Error!);

        var categoryError = ValidateCategories(dto.Categories);
        if (categoryError is not null) return categoryError;

        var project = new Project
        {
            id = Guid.NewGuid().ToString(),
            createdById = dto.CreatedById,
            title = dto.Title,
            slug = GenerateSlug(dto.Title),
            description = dto.Description,
            problemStatement = dto.ProblemStatement,
            solution = dto.Solution,
            categories = string.Join(",", dto.Categories),
            status = ProjectStatus.DRAFT,
            fundingGoal = dto.FundingGoal,
            currentFunding = 0,
            supportersCount = 0,
            votesCount = 0,
            viewsCount = 0,
            featured = false,
            tags = dto.Tags,
            targetAudience = dto.TargetAudience,
            expectedImpact = dto.ExpectedImpact,
            timeline = dto.Timeline,
            projectType = dto.ProjectType,
            duration = dto.Duration,
            images = dto.Images,
            videos = dto.Videos,
            documents = dto.Documents,
            createdAt = DateTime.UtcNow,
            updatedAt = DateTime.UtcNow
        };
        await _repository.AddAsync(project, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<ProjectDto>.Success(_mapper.Map<ProjectDto>(project));
    }

    public async Task<Result<ProjectDto>> UpdateAsync(string id, UpdateProjectDto dto, CancellationToken ct = default)
    {
        if (dto.Categories is not null)
        {
            var categoryError = ValidateCategories(dto.Categories);
            if (categoryError is not null) return categoryError;
        }

        var project = await _repository.GetByIdAsync(id, ct);
        if (project is null)
            return Result<ProjectDto>.NotFound($"Project with id {id} not found");

        if (dto.Title is not null) project.title = dto.Title;
        if (dto.Description is not null) project.description = dto.Description;
        if (dto.ProblemStatement is not null) project.problemStatement = dto.ProblemStatement;
        if (dto.Solution is not null) project.solution = dto.Solution;
        if (dto.Categories is not null) project.categories = string.Join(",", dto.Categories);
        if (dto.Status.HasValue) project.status = dto.Status.Value;
        if (dto.Tags is not null) project.tags = dto.Tags;
        if (dto.TargetAudience is not null) project.targetAudience = dto.TargetAudience;
        if (dto.ExpectedImpact is not null) project.expectedImpact = dto.ExpectedImpact;
        if (dto.Timeline is not null) project.timeline = dto.Timeline;
        if (dto.ProjectType.HasValue) project.projectType = dto.ProjectType.Value;
        if (dto.Duration.HasValue) project.duration = dto.Duration;
        if (dto.Images is not null) project.images = dto.Images;
        if (dto.Videos is not null) project.videos = dto.Videos;
        if (dto.Documents is not null) project.documents = dto.Documents;

        project.updatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(project, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<ProjectDto>.Success(_mapper.Map<ProjectDto>(project));
    }

    public async Task<Result<bool>> DeleteAsync(string id, CancellationToken ct = default)
    {
        var project = await _repository.GetByIdAsync(id, ct);
        if (project is null)
            return Result<bool>.NotFound($"Project with id {id} not found");

        await _repository.DeleteAsync(project, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<bool>.Success(true);
    }

    public async Task<Result<ProjectDto>> PublishAsync(string id, CancellationToken ct = default)
    {
        var project = await _repository.GetByIdAsync(id, ct);
        if (project is null)
            return Result<ProjectDto>.NotFound($"Project with id {id} not found");

        project.status = ProjectStatus.PUBLISHED;
        project.publishedAt = DateTime.UtcNow;
        project.updatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(project, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<ProjectDto>.Success(_mapper.Map<ProjectDto>(project));
    }

    public async Task<Result<ProjectDto>> SetFeaturedAsync(string id, bool featured, CancellationToken ct = default)
    {
        var project = await _repository.GetByIdAsync(id, ct);
        if (project is null)
            return Result<ProjectDto>.NotFound($"Project with id {id} not found");

        project.featured = featured;
        project.updatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(project, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<ProjectDto>.Success(_mapper.Map<ProjectDto>(project));
    }

    private static string GenerateSlug(string title)
    {
        return title.ToLowerInvariant()
            .Replace(" ", "-")
            .Replace("--", "-")
            + "-" + Guid.NewGuid().ToString("N")[..8];
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

    public async Task<Result<ProjectTaskDto>> GetByIdAsync(string id, CancellationToken ct = default)
    {
        var task = await _repository.GetByIdAsync(id, ct);
        if (task is null)
            return Result<ProjectTaskDto>.NotFound($"Task with id {id} not found");
        return Result<ProjectTaskDto>.Success(_mapper.Map<ProjectTaskDto>(task));
    }

    public async Task<Result<IReadOnlyList<ProjectTaskDto>>> GetByProjectIdAsync(string projectId, CancellationToken ct = default)
    {
        var tasks = await _repository.FindAsync(t => t.projectId == projectId, ct);
        return Result<IReadOnlyList<ProjectTaskDto>>.Success(_mapper.Map<IReadOnlyList<ProjectTaskDto>>(tasks));
    }

    public async Task<Result<IReadOnlyList<ProjectTaskDto>>> GetByAssigneeIdAsync(string userId, CancellationToken ct = default)
    {
        var tasks = await _repository.FindAsync(t => t.assignedToId == userId, ct);
        return Result<IReadOnlyList<ProjectTaskDto>>.Success(_mapper.Map<IReadOnlyList<ProjectTaskDto>>(tasks));
    }

    public async Task<Result<ProjectTaskDto>> CreateAsync(CreateProjectTaskDto dto, CancellationToken ct = default)
    {
        var task = new ProjectTask
        {
            id = Guid.NewGuid().ToString(),
            projectId = dto.ProjectId,
            title = dto.Title,
            description = dto.Description,
            status = TaskStatus.TODO,
            priority = dto.Priority,
            estimatedHours = dto.EstimatedHours,
            dueDate = dto.DueDate,
            assignedToId = dto.AssignedToId,
            escrowStatus = EscrowStatus.NONE,
            createdAt = DateTime.UtcNow,
            updatedAt = DateTime.UtcNow
        };
        await _repository.AddAsync(task, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<ProjectTaskDto>.Success(_mapper.Map<ProjectTaskDto>(task));
    }

    public async Task<Result<ProjectTaskDto>> UpdateAsync(string id, UpdateProjectTaskDto dto, CancellationToken ct = default)
    {
        var task = await _repository.GetByIdAsync(id, ct);
        if (task is null)
            return Result<ProjectTaskDto>.NotFound($"Task with id {id} not found");

        if (dto.Title is not null) task.title = dto.Title;
        if (dto.Description is not null) task.description = dto.Description;
        if (dto.Priority.HasValue) task.priority = dto.Priority.Value;
        if (dto.EstimatedHours.HasValue) task.estimatedHours = dto.EstimatedHours;
        if (dto.DueDate.HasValue) task.dueDate = dto.DueDate;
        if (dto.AssignedToId is not null) task.assignedToId = dto.AssignedToId;

        task.updatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(task, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<ProjectTaskDto>.Success(_mapper.Map<ProjectTaskDto>(task));
    }

    public async Task<Result<bool>> DeleteAsync(string id, CancellationToken ct = default)
    {
        var task = await _repository.GetByIdAsync(id, ct);
        if (task is null)
            return Result<bool>.NotFound($"Task with id {id} not found");

        await _repository.DeleteAsync(task, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<bool>.Success(true);
    }

    public async Task<Result<ProjectTaskDto>> AssignAsync(string id, string? userId, CancellationToken ct = default)
    {
        var task = await _repository.GetByIdAsync(id, ct);
        if (task is null)
            return Result<ProjectTaskDto>.NotFound($"Task with id {id} not found");

        task.assignedToId = userId;
        task.updatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(task, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<ProjectTaskDto>.Success(_mapper.Map<ProjectTaskDto>(task));
    }

    public async Task<Result<ProjectTaskDto>> UpdateStatusAsync(string id, TaskStatus status, CancellationToken ct = default)
    {
        var task = await _repository.GetByIdAsync(id, ct);
        if (task is null)
            return Result<ProjectTaskDto>.NotFound($"Task with id {id} not found");

        task.status = status;
        if (status == TaskStatus.COMPLETED)
            task.completedAt = DateTime.UtcNow;
        task.updatedAt = DateTime.UtcNow;

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

    public async Task<Result<ProjectTaskDependencyDto>> GetByIdAsync(string id, CancellationToken ct = default)
    {
        var dep = await _repository.GetByIdAsync(id, ct);
        if (dep is null)
            return Result<ProjectTaskDependencyDto>.NotFound($"Dependency with id {id} not found");
        return Result<ProjectTaskDependencyDto>.Success(_mapper.Map<ProjectTaskDependencyDto>(dep));
    }

    public async Task<Result<IReadOnlyList<ProjectTaskDependencyDto>>> GetByTaskIdAsync(string taskId, CancellationToken ct = default)
    {
        var deps = await _repository.FindAsync(d => d.taskId == taskId, ct);
        return Result<IReadOnlyList<ProjectTaskDependencyDto>>.Success(_mapper.Map<IReadOnlyList<ProjectTaskDependencyDto>>(deps));
    }

    public async Task<Result<ProjectTaskDependencyDto>> CreateAsync(CreateProjectTaskDependencyDto dto, CancellationToken ct = default)
    {
        var dep = new ProjectTaskDependency
        {
            id = Guid.NewGuid().ToString(),
            taskId = dto.TaskId,
            dependsOnId = dto.DependsOnId
        };
        await _repository.AddAsync(dep, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<ProjectTaskDependencyDto>.Success(_mapper.Map<ProjectTaskDependencyDto>(dep));
    }

    public async Task<Result<bool>> DeleteAsync(string id, CancellationToken ct = default)
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

    public async Task<Result<ProjectResourceDto>> GetByIdAsync(string id, CancellationToken ct = default)
    {
        var resource = await _repository.GetByIdAsync(id, ct);
        if (resource is null)
            return Result<ProjectResourceDto>.NotFound($"Resource with id {id} not found");
        return Result<ProjectResourceDto>.Success(_mapper.Map<ProjectResourceDto>(resource));
    }

    public async Task<Result<IReadOnlyList<ProjectResourceDto>>> GetByProjectIdAsync(string projectId, CancellationToken ct = default)
    {
        var resources = await _repository.FindAsync(r => r.projectId == projectId, ct);
        return Result<IReadOnlyList<ProjectResourceDto>>.Success(_mapper.Map<IReadOnlyList<ProjectResourceDto>>(resources));
    }

    public async Task<Result<ProjectResourceDto>> CreateAsync(CreateProjectResourceDto dto, CancellationToken ct = default)
    {
        if (dto.RecurringIntervalDays.HasValue && dto.RecurringIntervalDays.Value > 365)
            return Result<ProjectResourceDto>.ValidationError("Recurring interval cannot exceed 365 days");
        if (dto.RecurringIntervalDays.HasValue && dto.RecurringIntervalDays.Value < 1)
            return Result<ProjectResourceDto>.ValidationError("Recurring interval must be at least 1 day");

        var resource = new ProjectResource
        {
            id = Guid.NewGuid().ToString(),
            projectId = dto.ProjectId,
            name = dto.Name,
            description = dto.Description,
            quantity = dto.Quantity,
            estimatedCost = dto.EstimatedCost,
            recurringCost = dto.RecurringCost,
            recurringIntervalDays = dto.RecurringIntervalDays,
            isRequired = dto.IsRequired,
            isObtained = false,
            createdAt = DateTime.UtcNow
        };
        await _repository.AddAsync(resource, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<ProjectResourceDto>.Success(_mapper.Map<ProjectResourceDto>(resource));
    }

    public async Task<Result<ProjectResourceDto>> UpdateAsync(string id, UpdateProjectResourceDto dto, CancellationToken ct = default)
    {
        var resource = await _repository.GetByIdAsync(id, ct);
        if (resource is null)
            return Result<ProjectResourceDto>.NotFound($"Resource with id {id} not found");

        if (dto.RecurringIntervalDays.HasValue && dto.RecurringIntervalDays.Value > 365)
            return Result<ProjectResourceDto>.ValidationError("Recurring interval cannot exceed 365 days");
        if (dto.RecurringIntervalDays.HasValue && dto.RecurringIntervalDays.Value < 1)
            return Result<ProjectResourceDto>.ValidationError("Recurring interval must be at least 1 day");

        if (dto.Name is not null) resource.name = dto.Name;
        if (dto.Description is not null) resource.description = dto.Description;
        if (dto.Quantity.HasValue) resource.quantity = dto.Quantity.Value;
        if (dto.EstimatedCost.HasValue) resource.estimatedCost = dto.EstimatedCost;
        if (dto.RecurringCost.HasValue) resource.recurringCost = dto.RecurringCost;
        if (dto.RecurringIntervalDays.HasValue) resource.recurringIntervalDays = dto.RecurringIntervalDays;
        if (dto.IsRequired.HasValue) resource.isRequired = dto.IsRequired.Value;
        if (dto.IsObtained.HasValue) resource.isObtained = dto.IsObtained.Value;

        await _repository.UpdateAsync(resource, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<ProjectResourceDto>.Success(_mapper.Map<ProjectResourceDto>(resource));
    }

    public async Task<Result<bool>> DeleteAsync(string id, CancellationToken ct = default)
    {
        var resource = await _repository.GetByIdAsync(id, ct);
        if (resource is null)
            return Result<bool>.NotFound($"Resource with id {id} not found");

        await _repository.DeleteAsync(resource, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<bool>.Success(true);
    }

    public async Task<Result<ProjectResourceDto>> MarkObtainedAsync(string id, bool obtained, CancellationToken ct = default)
    {
        var resource = await _repository.GetByIdAsync(id, ct);
        if (resource is null)
            return Result<ProjectResourceDto>.NotFound($"Resource with id {id} not found");

        resource.isObtained = obtained;

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

    public async Task<Result<ProjectMilestoneDto>> GetByIdAsync(string id, CancellationToken ct = default)
    {
        var milestone = await _repository.GetByIdAsync(id, ct);
        if (milestone is null)
            return Result<ProjectMilestoneDto>.NotFound($"Milestone with id {id} not found");
        return Result<ProjectMilestoneDto>.Success(_mapper.Map<ProjectMilestoneDto>(milestone));
    }

    public async Task<Result<IReadOnlyList<ProjectMilestoneDto>>> GetByProjectIdAsync(string projectId, CancellationToken ct = default)
    {
        var milestones = await _repository.FindAsync(m => m.projectId == projectId, ct);
        return Result<IReadOnlyList<ProjectMilestoneDto>>.Success(_mapper.Map<IReadOnlyList<ProjectMilestoneDto>>(milestones));
    }

    public async Task<Result<ProjectMilestoneDto>> CreateAsync(CreateProjectMilestoneDto dto, CancellationToken ct = default)
    {
        var milestone = new ProjectMilestone
        {
            id = Guid.NewGuid().ToString(),
            projectId = dto.ProjectId,
            title = dto.Title,
            description = dto.Description,
            targetDate = dto.TargetDate,
            status = MilestoneStatus.PLANNED,
            priority = Priority.MEDIUM,
            order = 0,
            createdAt = DateTime.UtcNow,
            updatedAt = DateTime.UtcNow
        };
        await _repository.AddAsync(milestone, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<ProjectMilestoneDto>.Success(_mapper.Map<ProjectMilestoneDto>(milestone));
    }

    public async Task<Result<ProjectMilestoneDto>> UpdateAsync(string id, UpdateProjectMilestoneDto dto, CancellationToken ct = default)
    {
        var milestone = await _repository.GetByIdAsync(id, ct);
        if (milestone is null)
            return Result<ProjectMilestoneDto>.NotFound($"Milestone with id {id} not found");

        if (dto.Title is not null) milestone.title = dto.Title;
        if (dto.Description is not null) milestone.description = dto.Description;
        if (dto.TargetDate.HasValue) milestone.targetDate = dto.TargetDate.Value;
        if (dto.Status.HasValue) milestone.status = dto.Status.Value;
        if (dto.Priority.HasValue) milestone.priority = dto.Priority.Value;
        milestone.updatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(milestone, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<ProjectMilestoneDto>.Success(_mapper.Map<ProjectMilestoneDto>(milestone));
    }

    public async Task<Result<bool>> DeleteAsync(string id, CancellationToken ct = default)
    {
        var milestone = await _repository.GetByIdAsync(id, ct);
        if (milestone is null)
            return Result<bool>.NotFound($"Milestone with id {id} not found");

        await _repository.DeleteAsync(milestone, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<bool>.Success(true);
    }

    public async Task<Result<ProjectMilestoneDto>> CompleteAsync(string id, CancellationToken ct = default)
    {
        var milestone = await _repository.GetByIdAsync(id, ct);
        if (milestone is null)
            return Result<ProjectMilestoneDto>.NotFound($"Milestone with id {id} not found");

        milestone.status = MilestoneStatus.COMPLETED;
        milestone.completedAt = DateTime.UtcNow;

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

    public async Task<Result<ProjectSupportDto>> GetByIdAsync(string id, CancellationToken ct = default)
    {
        var support = await _repository.GetByIdAsync(id, ct);
        if (support is null)
            return Result<ProjectSupportDto>.NotFound($"Support with id {id} not found");
        return Result<ProjectSupportDto>.Success(_mapper.Map<ProjectSupportDto>(support));
    }

    public async Task<Result<IReadOnlyList<ProjectSupportDto>>> GetByProjectIdAsync(string projectId, CancellationToken ct = default)
    {
        var supports = await _repository.FindAsync(s => s.projectId == projectId, ct);
        return Result<IReadOnlyList<ProjectSupportDto>>.Success(_mapper.Map<IReadOnlyList<ProjectSupportDto>>(supports));
    }

    public async Task<Result<IReadOnlyList<ProjectSupportDto>>> GetByUserIdAsync(string userId, CancellationToken ct = default)
    {
        var supports = await _repository.FindAsync(s => s.userId == userId, ct);
        return Result<IReadOnlyList<ProjectSupportDto>>.Success(_mapper.Map<IReadOnlyList<ProjectSupportDto>>(supports));
    }

    public async Task<Result<ProjectSupportDto>> CreateAsync(CreateProjectSupportDto dto, CancellationToken ct = default)
    {
        var support = new ProjectSupport
        {
            id = Guid.NewGuid().ToString(),
            projectId = dto.ProjectId,
            userId = dto.UserId,
            supportType = dto.SupportType,
            monthlyAmount = dto.MonthlyAmount,
            message = dto.Message,
            isActive = true,
            createdAt = DateTime.UtcNow,
            updatedAt = DateTime.UtcNow
        };
        await _repository.AddAsync(support, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<ProjectSupportDto>.Success(_mapper.Map<ProjectSupportDto>(support));
    }

    public async Task<Result<bool>> DeleteAsync(string id, CancellationToken ct = default)
    {
        var support = await _repository.GetByIdAsync(id, ct);
        if (support is null)
            return Result<bool>.NotFound($"Support with id {id} not found");

        await _repository.DeleteAsync(support, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<bool>.Success(true);
    }

    public async Task<Result<ProjectSupportDto>> ToggleActiveAsync(string id, CancellationToken ct = default)
    {
        var support = await _repository.GetByIdAsync(id, ct);
        if (support is null)
            return Result<ProjectSupportDto>.NotFound($"Support with id {id} not found");

        support.isActive = !support.isActive;
        support.updatedAt = DateTime.UtcNow;

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

    public async Task<Result<ProjectApplicationDto>> GetByIdAsync(string id, CancellationToken ct = default)
    {
        var app = await _repository.GetByIdAsync(id, ct);
        if (app is null)
            return Result<ProjectApplicationDto>.NotFound($"Application with id {id} not found");
        return Result<ProjectApplicationDto>.Success(_mapper.Map<ProjectApplicationDto>(app));
    }

    public async Task<Result<IReadOnlyList<ProjectApplicationDto>>> GetByProjectIdAsync(string projectId, CancellationToken ct = default)
    {
        var apps = await _repository.FindAsync(a => a.projectId == projectId, ct);
        return Result<IReadOnlyList<ProjectApplicationDto>>.Success(_mapper.Map<IReadOnlyList<ProjectApplicationDto>>(apps));
    }

    public async Task<Result<IReadOnlyList<ProjectApplicationDto>>> GetByUserIdAsync(string userId, CancellationToken ct = default)
    {
        var apps = await _repository.FindAsync(a => a.userId == userId, ct);
        return Result<IReadOnlyList<ProjectApplicationDto>>.Success(_mapper.Map<IReadOnlyList<ProjectApplicationDto>>(apps));
    }

    public async Task<Result<ProjectApplicationDto>> CreateAsync(CreateProjectApplicationDto dto, CancellationToken ct = default)
    {
        var app = new ProjectApplication
        {
            id = Guid.NewGuid().ToString(),
            projectId = dto.ProjectId,
            userId = dto.UserId,
            roleTitle = dto.RoleTitle,
            message = dto.Message,
            skills = dto.Skills,
            experience = dto.Experience,
            availability = dto.Availability,
            status = ApplicationStatus.PENDING,
            appliedAt = DateTime.UtcNow
        };
        await _repository.AddAsync(app, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<ProjectApplicationDto>.Success(_mapper.Map<ProjectApplicationDto>(app));
    }

    public async Task<Result<bool>> DeleteAsync(string id, CancellationToken ct = default)
    {
        var app = await _repository.GetByIdAsync(id, ct);
        if (app is null)
            return Result<bool>.NotFound($"Application with id {id} not found");

        await _repository.DeleteAsync(app, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<bool>.Success(true);
    }

    public async Task<Result<ProjectApplicationDto>> AcceptAsync(string id, string? reviewMessage, CancellationToken ct = default)
    {
        var app = await _repository.GetByIdAsync(id, ct);
        if (app is null)
            return Result<ProjectApplicationDto>.NotFound($"Application with id {id} not found");

        app.status = ApplicationStatus.ACCEPTED;
        app.reviewMessage = reviewMessage;
        app.reviewedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(app, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<ProjectApplicationDto>.Success(_mapper.Map<ProjectApplicationDto>(app));
    }

    public async Task<Result<ProjectApplicationDto>> RejectAsync(string id, string? reviewMessage, CancellationToken ct = default)
    {
        var app = await _repository.GetByIdAsync(id, ct);
        if (app is null)
            return Result<ProjectApplicationDto>.NotFound($"Application with id {id} not found");

        app.status = ApplicationStatus.REJECTED;
        app.reviewMessage = reviewMessage;
        app.reviewedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(app, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<ProjectApplicationDto>.Success(_mapper.Map<ProjectApplicationDto>(app));
    }

    public async Task<Result<ProjectApplicationDto>> WithdrawAsync(string id, CancellationToken ct = default)
    {
        var app = await _repository.GetByIdAsync(id, ct);
        if (app is null)
            return Result<ProjectApplicationDto>.NotFound($"Application with id {id} not found");

        app.status = ApplicationStatus.WITHDRAWN;

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

    public async Task<Result<ProjectCommentDto>> GetByIdAsync(string id, CancellationToken ct = default)
    {
        var comment = await _repository.GetByIdAsync(id, ct);
        if (comment is null)
            return Result<ProjectCommentDto>.NotFound($"Comment with id {id} not found");
        return Result<ProjectCommentDto>.Success(_mapper.Map<ProjectCommentDto>(comment));
    }

    public async Task<Result<IReadOnlyList<ProjectCommentDto>>> GetByProjectIdAsync(string projectId, CancellationToken ct = default)
    {
        var comments = await _repository.FindAsync(c => c.projectId == projectId, ct);
        return Result<IReadOnlyList<ProjectCommentDto>>.Success(_mapper.Map<IReadOnlyList<ProjectCommentDto>>(comments));
    }

    public async Task<Result<ProjectCommentDto>> CreateAsync(CreateProjectCommentDto dto, CancellationToken ct = default)
    {
        var comment = new ProjectComment
        {
            id = Guid.NewGuid().ToString(),
            projectId = dto.ProjectId,
            userId = dto.UserId,
            content = dto.Content,
            parentId = dto.ParentId,
            createdAt = DateTime.UtcNow,
            updatedAt = DateTime.UtcNow
        };
        await _repository.AddAsync(comment, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<ProjectCommentDto>.Success(_mapper.Map<ProjectCommentDto>(comment));
    }

    public async Task<Result<ProjectCommentDto>> UpdateAsync(string id, string content, CancellationToken ct = default)
    {
        var comment = await _repository.GetByIdAsync(id, ct);
        if (comment is null)
            return Result<ProjectCommentDto>.NotFound($"Comment with id {id} not found");

        comment.content = content;
        comment.updatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(comment, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<ProjectCommentDto>.Success(_mapper.Map<ProjectCommentDto>(comment));
    }

    public async Task<Result<bool>> DeleteAsync(string id, CancellationToken ct = default)
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

    public async Task<Result<ProjectUpdateDto>> GetByIdAsync(string id, CancellationToken ct = default)
    {
        var update = await _repository.GetByIdAsync(id, ct);
        if (update is null)
            return Result<ProjectUpdateDto>.NotFound($"Update with id {id} not found");
        return Result<ProjectUpdateDto>.Success(_mapper.Map<ProjectUpdateDto>(update));
    }

    public async Task<Result<IReadOnlyList<ProjectUpdateDto>>> GetByProjectIdAsync(string projectId, CancellationToken ct = default)
    {
        var updates = await _repository.FindAsync(u => u.projectId == projectId, ct);
        return Result<IReadOnlyList<ProjectUpdateDto>>.Success(_mapper.Map<IReadOnlyList<ProjectUpdateDto>>(updates));
    }

    public async Task<Result<ProjectUpdateDto>> CreateAsync(CreateProjectUpdateDto dto, CancellationToken ct = default)
    {
        var update = new ProjectUpdate
        {
            id = Guid.NewGuid().ToString(),
            projectId = dto.ProjectId,
            userId = dto.UserId,
            title = dto.Title,
            content = dto.Content,
            images = dto.Images,
            createdAt = DateTime.UtcNow
        };
        await _repository.AddAsync(update, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<ProjectUpdateDto>.Success(_mapper.Map<ProjectUpdateDto>(update));
    }

    public async Task<Result<bool>> DeleteAsync(string id, CancellationToken ct = default)
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

    public async Task<Result<ProjectEquityDto>> GetByIdAsync(string id, CancellationToken ct = default)
    {
        var equity = await _repository.GetByIdAsync(id, ct);
        if (equity is null)
            return Result<ProjectEquityDto>.NotFound($"Equity with id {id} not found");
        return Result<ProjectEquityDto>.Success(_mapper.Map<ProjectEquityDto>(equity));
    }

    public async Task<Result<IReadOnlyList<ProjectEquityDto>>> GetByProjectIdAsync(string projectId, CancellationToken ct = default)
    {
        var equities = await _repository.FindAsync(e => e.projectId == projectId, ct);
        return Result<IReadOnlyList<ProjectEquityDto>>.Success(_mapper.Map<IReadOnlyList<ProjectEquityDto>>(equities));
    }

    public async Task<Result<IReadOnlyList<ProjectEquityDto>>> GetByUserIdAsync(string userId, CancellationToken ct = default)
    {
        var equities = await _repository.FindAsync(e => e.userId == userId, ct);
        return Result<IReadOnlyList<ProjectEquityDto>>.Success(_mapper.Map<IReadOnlyList<ProjectEquityDto>>(equities));
    }

    public async Task<Result<ProjectEquityDto>> CreateAsync(CreateProjectEquityDto dto, CancellationToken ct = default)
    {
        // Enforce 100% equity cap
        var existingEquities = await _repository.FindAsync(e => e.projectId == dto.ProjectId, ct);
        var currentTotal = existingEquities.Sum(e => e.sharePercent);
        if (currentTotal + dto.SharePercent > 100)
            return Result<ProjectEquityDto>.Failure(
                $"Total equity allocation cannot exceed 100%. Current: {currentTotal}%, requested: {dto.SharePercent}%, available: {100 - currentTotal}%");

        var equity = new ProjectEquity
        {
            id = Guid.NewGuid().ToString(),
            projectId = dto.ProjectId,
            userId = dto.UserId,
            sharePercent = dto.SharePercent,
            investmentAmount = dto.InvestmentAmount,
            grantedAt = DateTime.UtcNow
        };
        await _repository.AddAsync(equity, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<ProjectEquityDto>.Success(_mapper.Map<ProjectEquityDto>(equity));
    }

    public async Task<Result<ProjectEquityDto>> UpdateAsync(string id, UpdateProjectEquityDto dto, CancellationToken ct = default)
    {
        var equity = await _repository.GetByIdAsync(id, ct);
        if (equity is null)
            return Result<ProjectEquityDto>.NotFound($"Equity with id {id} not found");

        // Enforce 100% equity cap on share percent updates
        if (dto.SharePercent.HasValue)
        {
            var existingEquities = await _repository.FindAsync(e => e.projectId == equity.projectId, ct);
            var othersTotal = existingEquities.Where(e => e.id != id).Sum(e => e.sharePercent);
            if (othersTotal + dto.SharePercent.Value > 100)
                return Result<ProjectEquityDto>.Failure(
                    $"Total equity allocation cannot exceed 100%. Others: {othersTotal}%, requested: {dto.SharePercent.Value}%, available: {100 - othersTotal}%");

            equity.sharePercent = dto.SharePercent.Value;
        }
        if (dto.InvestmentAmount.HasValue) equity.investmentAmount = dto.InvestmentAmount.Value;

        await _repository.UpdateAsync(equity, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<ProjectEquityDto>.Success(_mapper.Map<ProjectEquityDto>(equity));
    }

    public async Task<Result<bool>> DeleteAsync(string id, CancellationToken ct = default)
    {
        var equity = await _repository.GetByIdAsync(id, ct);
        if (equity is null)
            return Result<bool>.NotFound($"Equity with id {id} not found");

        await _repository.DeleteAsync(equity, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<bool>.Success(true);
    }

    public async Task<Result<decimal>> GetTotalEquityAsync(string projectId, CancellationToken ct = default)
    {
        var equities = await _repository.FindAsync(e => e.projectId == projectId, ct);
        var total = equities.Sum(e => e.sharePercent);
        return Result<decimal>.Success(total);
    }
}

public class ProjectMemberService : IProjectMemberService
{
    private readonly IRepository<ProjectMember> _repository;
    private readonly IRepository<User> _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public ProjectMemberService(IRepository<ProjectMember> repository, IRepository<User> userRepository, IUnitOfWork unitOfWork, IMapper mapper)
    {
        _repository = repository;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<Result<ProjectMemberDto>> GetByIdAsync(string id, CancellationToken ct = default)
    {
        var member = await _repository.GetByIdAsync(id, ct);
        if (member is null)
            return Result<ProjectMemberDto>.NotFound($"Member with id {id} not found");

        var dto = _mapper.Map<ProjectMemberDto>(member);
        var user = await _userRepository.GetByIdAsync(member.userId, ct);
        if (user is not null)
            dto = dto with { User = _mapper.Map<ProjectMemberUserDto>(user) };

        return Result<ProjectMemberDto>.Success(dto);
    }

    public async Task<Result<IReadOnlyList<ProjectMemberDto>>> GetByProjectIdAsync(string projectId, CancellationToken ct = default)
    {
        var members = await _repository.FindAsync(m => m.projectId == projectId, ct);
        var dtos = await EnrichWithUserDataAsync(members, ct);
        return Result<IReadOnlyList<ProjectMemberDto>>.Success(dtos);
    }

    public async Task<Result<IReadOnlyList<ProjectMemberDto>>> GetByUserIdAsync(string userId, CancellationToken ct = default)
    {
        var members = await _repository.FindAsync(m => m.userId == userId, ct);
        var dtos = await EnrichWithUserDataAsync(members, ct);
        return Result<IReadOnlyList<ProjectMemberDto>>.Success(dtos);
    }

    private async Task<IReadOnlyList<ProjectMemberDto>> EnrichWithUserDataAsync(IEnumerable<ProjectMember> members, CancellationToken ct)
    {
        var dtos = _mapper.Map<List<ProjectMemberDto>>(members);
        var userIds = members.Select(m => m.userId).Distinct().ToList();
        var users = new Dictionary<string, User>();

        foreach (var userId in userIds)
        {
            var user = await _userRepository.GetByIdAsync(userId, ct);
            if (user is not null)
                users[userId] = user;
        }

        return dtos.Select(dto =>
        {
            var member = members.First(m => m.id == dto.Id);
            if (users.TryGetValue(member.userId, out var user))
                return dto with { User = _mapper.Map<ProjectMemberUserDto>(user) };
            return dto;
        }).ToList();
    }

    public async Task<Result<ProjectMemberDto>> CreateAsync(CreateProjectMemberDto dto, CancellationToken ct = default)
    {
        // Enforce one role per user per project
        var existing = await _repository.FindAsync(m => m.projectId == dto.ProjectId && m.userId == dto.UserId, ct);
        if (existing.Any())
            return Result<ProjectMemberDto>.ValidationError("User already has a role on this project");

        var member = new ProjectMember
        {
            id = Guid.NewGuid().ToString(),
            projectId = dto.ProjectId,
            userId = dto.UserId,
            role = dto.Role,
            shareBalance = 0,
            votingPower = 0,
            joinedAt = DateTime.UtcNow,
            invitedById = dto.InvitedById
        };
        await _repository.AddAsync(member, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        var result = _mapper.Map<ProjectMemberDto>(member);
        var user = await _userRepository.GetByIdAsync(member.userId, ct);
        if (user is not null)
            result = result with { User = _mapper.Map<ProjectMemberUserDto>(user) };

        return Result<ProjectMemberDto>.Success(result);
    }

    public async Task<Result<ProjectMemberDto>> UpdateAsync(string id, UpdateProjectMemberDto dto, CancellationToken ct = default)
    {
        var member = await _repository.GetByIdAsync(id, ct);
        if (member is null)
            return Result<ProjectMemberDto>.NotFound($"Member with id {id} not found");

        if (dto.Role.HasValue) member.role = dto.Role.Value;
        if (dto.ShareBalance.HasValue) member.shareBalance = dto.ShareBalance.Value;
        if (dto.VotingPower.HasValue) member.votingPower = dto.VotingPower.Value;

        await _repository.UpdateAsync(member, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        var result = _mapper.Map<ProjectMemberDto>(member);
        var user = await _userRepository.GetByIdAsync(member.userId, ct);
        if (user is not null)
            result = result with { User = _mapper.Map<ProjectMemberUserDto>(user) };

        return Result<ProjectMemberDto>.Success(result);
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

public class ProjectInvitationService : IProjectInvitationService
{
    private readonly IRepository<ProjectInvitation> _repository;
    private readonly IRepository<ProjectMember> _memberRepository;
    private readonly IRepository<User> _userRepository;
    private readonly IRepository<Project> _projectRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public ProjectInvitationService(
        IRepository<ProjectInvitation> repository,
        IRepository<ProjectMember> memberRepository,
        IRepository<User> userRepository,
        IRepository<Project> projectRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper)
    {
        _repository = repository;
        _memberRepository = memberRepository;
        _userRepository = userRepository;
        _projectRepository = projectRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<Result<ProjectInvitationDto>> GetByIdAsync(string id, CancellationToken ct = default)
    {
        var invitation = await _repository.GetByIdAsync(id, ct);
        if (invitation is null)
            return Result<ProjectInvitationDto>.NotFound($"Invitation with id {id} not found");

        return Result<ProjectInvitationDto>.Success(await EnrichAsync(invitation, ct));
    }

    public async Task<Result<IReadOnlyList<ProjectInvitationDto>>> GetByProjectIdAsync(string projectId, CancellationToken ct = default)
    {
        var invitations = await _repository.FindAsync(i => i.projectId == projectId, ct);
        var dtos = new List<ProjectInvitationDto>();
        foreach (var inv in invitations)
            dtos.Add(await EnrichAsync(inv, ct));
        return Result<IReadOnlyList<ProjectInvitationDto>>.Success(dtos);
    }

    public async Task<Result<IReadOnlyList<ProjectInvitationDto>>> GetByUserIdAsync(string userId, CancellationToken ct = default)
    {
        var invitations = await _repository.FindAsync(i => i.invitedUserId == userId, ct);
        var dtos = new List<ProjectInvitationDto>();
        foreach (var inv in invitations)
            dtos.Add(await EnrichAsync(inv, ct));
        return Result<IReadOnlyList<ProjectInvitationDto>>.Success(dtos);
    }

    public async Task<Result<ProjectInvitationDto>> CreateAsync(CreateProjectInvitationDto dto, CancellationToken ct = default)
    {
        // Validate no duplicate PENDING invitation for same (projectId, invitedUserId)
        if (dto.InvitedUserId is not null)
        {
            var existing = await _repository.FindAsync(
                i => i.projectId == dto.ProjectId
                     && i.invitedUserId == dto.InvitedUserId
                     && i.status == InvitationStatus.PENDING, ct);
            if (existing.Any())
                return Result<ProjectInvitationDto>.ValidationError("A pending invitation already exists for this user on this project");

            // Validate user is not already a member (one-role constraint)
            var members = await _memberRepository.FindAsync(m => m.projectId == dto.ProjectId && m.userId == dto.InvitedUserId, ct);
            if (members.Any())
                return Result<ProjectInvitationDto>.ValidationError("User already has a role on this project");
        }

        var invitation = new ProjectInvitation
        {
            id = Guid.NewGuid().ToString(),
            projectId = dto.ProjectId,
            invitedById = dto.InvitedById,
            invitedUserId = dto.InvitedUserId,
            invitedEmail = dto.InvitedEmail,
            role = Enum.Parse<ProjectRole>(dto.Role),
            message = dto.Message,
            status = InvitationStatus.PENDING,
            createdAt = DateTime.UtcNow
        };

        await _repository.AddAsync(invitation, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<ProjectInvitationDto>.Success(await EnrichAsync(invitation, ct));
    }

    public async Task<Result<ProjectInvitationDto>> AcceptAsync(string id, CancellationToken ct = default)
    {
        var invitation = await _repository.GetByIdAsync(id, ct);
        if (invitation is null)
            return Result<ProjectInvitationDto>.NotFound($"Invitation with id {id} not found");

        if (invitation.status != InvitationStatus.PENDING)
            return Result<ProjectInvitationDto>.ValidationError("Invitation is no longer pending");

        // Check user not already a member (one-role constraint)
        if (invitation.invitedUserId is not null)
        {
            var members = await _memberRepository.FindAsync(
                m => m.projectId == invitation.projectId && m.userId == invitation.invitedUserId, ct);
            if (members.Any())
                return Result<ProjectInvitationDto>.ValidationError("User already has a role on this project");
        }

        invitation.status = InvitationStatus.ACCEPTED;
        invitation.respondedAt = DateTime.UtcNow;
        await _repository.UpdateAsync(invitation, ct);

        // Auto-create ProjectMember with the invitation's role
        if (invitation.invitedUserId is not null)
        {
            var member = new ProjectMember
            {
                id = Guid.NewGuid().ToString(),
                projectId = invitation.projectId,
                userId = invitation.invitedUserId,
                role = invitation.role,
                shareBalance = 0,
                votingPower = 0,
                joinedAt = DateTime.UtcNow,
                invitedById = invitation.invitedById
            };
            await _memberRepository.AddAsync(member, ct);
        }

        await _unitOfWork.SaveChangesAsync(ct);
        return Result<ProjectInvitationDto>.Success(await EnrichAsync(invitation, ct));
    }

    public async Task<Result<ProjectInvitationDto>> RejectAsync(string id, CancellationToken ct = default)
    {
        var invitation = await _repository.GetByIdAsync(id, ct);
        if (invitation is null)
            return Result<ProjectInvitationDto>.NotFound($"Invitation with id {id} not found");

        if (invitation.status != InvitationStatus.PENDING)
            return Result<ProjectInvitationDto>.ValidationError("Invitation is no longer pending");

        invitation.status = InvitationStatus.DECLINED;
        invitation.respondedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(invitation, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<ProjectInvitationDto>.Success(await EnrichAsync(invitation, ct));
    }

    public async Task<Result<bool>> DeleteAsync(string id, CancellationToken ct = default)
    {
        var invitation = await _repository.GetByIdAsync(id, ct);
        if (invitation is null)
            return Result<bool>.NotFound($"Invitation with id {id} not found");

        await _repository.DeleteAsync(invitation, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<bool>.Success(true);
    }

    private async Task<ProjectInvitationDto> EnrichAsync(ProjectInvitation invitation, CancellationToken ct)
    {
        var dto = _mapper.Map<ProjectInvitationDto>(invitation);

        if (invitation.invitedUserId is not null)
        {
            var user = await _userRepository.GetByIdAsync(invitation.invitedUserId, ct);
            if (user is not null)
                dto = dto with { InvitedUser = _mapper.Map<ProjectMemberUserDto>(user) };
        }

        var invitedBy = await _userRepository.GetByIdAsync(invitation.invitedById, ct);
        if (invitedBy is not null)
            dto = dto with { InvitedBy = _mapper.Map<ProjectMemberUserDto>(invitedBy) };

        var project = await _projectRepository.GetByIdAsync(invitation.projectId, ct);
        if (project is not null)
            dto = dto with { Project = new ProjectInvitationProjectDto
            {
                Id = project.id,
                Title = project.title,
                Slug = project.slug,
                Images = project.images
            }};

        return dto;
    }
}
