namespace ArdaNova.Application.Services.Implementations;

using ArdaNova.Application.Common.Interfaces;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Interfaces;
using ArdaNova.Domain.Models.Entities;
using ArdaNova.Domain.Models.Enums;
using AutoMapper;

public class TaskService : ITaskService
{
    /// <summary>
    /// Why a task's own award figure stops being editable once a bid has been accepted on it.
    /// The accepted agreement is the award of record and the units behind it are already set
    /// aside on the shared node; the task's figure is written from the agreement and nowhere
    /// else, so a board and an agreement can never quote different numbers.
    /// </summary>
    public const string AwardBelongsToTheAgreement =
        "This task's award was agreed when the bid was accepted and the units are already set "
        + "aside for the worker, so it can no longer be changed here.";

    /// <summary>Why a task's worker cannot be swapped once a bid was accepted or escrow is in play.</summary>
    public const string WorkerBelongsToTheAgreement =
        "This task's worker was agreed through an accepted bid, so it can no longer be reassigned here.";

    /// <summary>Why a task was refused: a blank title leaves an unlabeled card on the board.</summary>
    public const string TaskTitleRequired = "A task needs a title.";

    private readonly IRepository<ProjectTask> _repository;
    private readonly IRepository<User> _userRepository;
    private readonly IRepository<Project> _projectRepository;
    private readonly IRepository<TaskCommerceAgreement> _agreementRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    /// <param name="agreementRepository">
    /// Read-only lookup that decides whether this task's award is already an agreed one.
    /// Required rather than optional: a missing repository would silently skip the guard below,
    /// and a skipped guard lets a task's figure drift away from the award actually held.
    /// </param>
    public TaskService(
        IRepository<ProjectTask> repository,
        IRepository<User> userRepository,
        IRepository<Project> projectRepository,
        IRepository<TaskCommerceAgreement> agreementRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper)
    {
        _repository = repository;
        _userRepository = userRepository;
        _projectRepository = projectRepository;
        _agreementRepository = agreementRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<Result<TaskDto>> GetByIdAsync(string id, CancellationToken ct = default)
    {
        var task = await _repository.GetByIdAsync(id, ct);
        if (task is null)
            return Result<TaskDto>.NotFound($"Task with id {id} not found");

        var dto = await EnrichTaskDtoAsync(task, ct);
        return Result<TaskDto>.Success(dto);
    }

    public async Task<Result<IReadOnlyList<TaskDto>>> GetAllAsync(ProjectViewer viewer, CancellationToken ct = default)
    {
        var tasks = VisibleTasks(viewer).ToList();
        var dtos = await EnrichTaskDtosAsync(tasks, ct);
        return Result<IReadOnlyList<TaskDto>>.Success(dtos);
    }

    public async Task<Result<PagedResult<TaskDto>>> GetPagedAsync(ProjectViewer viewer, int page, int pageSize, CancellationToken ct = default)
    {
        var query = VisibleTasks(viewer).OrderByDescending(t => t.createdAt);
        var totalCount = query.Count();
        var items = query.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        var dtos = await EnrichTaskDtosAsync(items, ct);
        return Result<PagedResult<TaskDto>>.Success(new PagedResult<TaskDto>(dtos.ToList(), totalCount, page, pageSize));
    }

    public async Task<Result<PagedResult<TaskDto>>> SearchAsync(
        ProjectViewer viewer,
        string? searchTerm,
        TaskStatus? status,
        TaskPriority? priority,
        TaskType? taskType,
        string? projectId,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var query = VisibleTasks(viewer);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.ToLower();
            query = query.Where(t => t.title.ToLower().Contains(term) ||
                (t.description != null && t.description.ToLower().Contains(term)));
        }

        if (status.HasValue)
            query = query.Where(t => t.status == status.Value);

        if (priority.HasValue)
            query = query.Where(t => t.priority == priority.Value);

        if (taskType.HasValue)
            query = query.Where(t => t.taskType == taskType.Value);

        if (!string.IsNullOrEmpty(projectId))
            query = query.Where(t => t.projectId == projectId);

        query = query.OrderByDescending(t => t.createdAt);

        var totalCount = query.Count();
        var items = query.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        var dtos = await EnrichTaskDtosAsync(items, ct);

        return Result<PagedResult<TaskDto>>.Success(new PagedResult<TaskDto>(dtos.ToList(), totalCount, page, pageSize));
    }

    public async Task<Result<IReadOnlyList<TaskDto>>> GetByUserIdAsync(string userId, CancellationToken ct = default)
    {
        var tasks = await _repository.FindAsync(t => t.assignedToId == userId, ct);
        var dtos = await EnrichTaskDtosAsync(tasks, ct);
        return Result<IReadOnlyList<TaskDto>>.Success(dtos);
    }

    public async Task<Result<IReadOnlyList<TaskDto>>> GetByProjectIdAsync(string projectId, CancellationToken ct = default)
    {
        var tasks = await _repository.FindAsync(t => t.projectId == projectId, ct);
        var dtos = await EnrichTaskDtosAsync(tasks, ct);
        return Result<IReadOnlyList<TaskDto>>.Success(dtos);
    }

    public async Task<Result<IReadOnlyList<TaskDto>>> GetByPbiIdAsync(string pbiId, CancellationToken ct = default)
    {
        var tasks = await _repository.FindAsync(t => t.pbiId == pbiId, ct);
        var dtos = await EnrichTaskDtosAsync(tasks, ct);
        return Result<IReadOnlyList<TaskDto>>.Success(dtos);
    }

    public async Task<Result<TaskDto>> CreateAsync(CreateTaskDto dto, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Title))
            return Result<TaskDto>.ValidationError(TaskTitleRequired);

        var project = await _projectRepository.GetByIdAsync(dto.ProjectId, ct);
        if (project is null)
            return Result<TaskDto>.NotFound($"Project with id {dto.ProjectId} not found");

        var task = new ProjectTask
        {
            id = Guid.NewGuid().ToString(),
            projectId = dto.ProjectId,
            title = dto.Title.Trim(),
            description = dto.Description,
            status = TaskStatus.TODO,
            priority = dto.Priority,
            taskType = dto.TaskType,
            estimatedHours = dto.EstimatedHours,
            equityReward = dto.EquityReward,
            escrowStatus = EscrowStatus.NONE,
            dueDate = dto.DueDate,
            assignedToId = dto.AssignedToId,
            pbiId = dto.PbiId,
            featureId = dto.FeatureId,
            sprintId = dto.SprintId,
            epicId = dto.EpicId,
            milestoneId = dto.MilestoneId,
            guildId = dto.GuildId,
            createdAt = DateTime.UtcNow,
            updatedAt = DateTime.UtcNow
        };

        await _repository.AddAsync(task, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        var resultDto = await EnrichTaskDtoAsync(task, ct);
        return Result<TaskDto>.Success(resultDto);
    }

    public async Task<Result<TaskDto>> UpdateAsync(string id, UpdateTaskDto dto, CancellationToken ct = default)
    {
        var task = await _repository.GetByIdAsync(id, ct);
        if (task is null)
            return Result<TaskDto>.NotFound($"Task with id {id} not found");
        if (dto.Title is not null && string.IsNullOrWhiteSpace(dto.Title))
            return Result<TaskDto>.ValidationError(TaskTitleRequired);

        // The award collapse: one award figure of record per task. Once a bid has been
        // accepted the agreement owns it and the units behind it are already set aside on the
        // shared node, so this route may no longer write a second, different number.
        if (dto.EquityReward.HasValue)
        {
            var agreement = await _agreementRepository.FindOneAsync(item => item.taskId == id, ct);
            if (agreement is not null && dto.EquityReward.Value != agreement.awardAmount)
                return Result<TaskDto>.Conflict(AwardBelongsToTheAgreement);

            task.equityReward = dto.EquityReward;
        }

        if (dto.Title is not null) task.title = dto.Title.Trim();
        if (dto.Description is not null) task.description = dto.Description;
        if (dto.Status.HasValue) task.status = dto.Status.Value;
        if (dto.Priority.HasValue) task.priority = dto.Priority.Value;
        if (dto.TaskType.HasValue) task.taskType = dto.TaskType.Value;
        if (dto.EstimatedHours.HasValue) task.estimatedHours = dto.EstimatedHours;
        if (dto.ActualHours.HasValue) task.actualHours = dto.ActualHours;
        if (dto.DueDate.HasValue) task.dueDate = dto.DueDate;
        task.updatedAt = DateTime.UtcNow;

        if (dto.Status == TaskStatus.COMPLETED && task.completedAt == null)
            task.completedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(task, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        var resultDto = await EnrichTaskDtoAsync(task, ct);
        return Result<TaskDto>.Success(resultDto);
    }

    public async Task<Result<TaskDto>> UpdateStatusAsync(string id, TaskStatus status, CancellationToken ct = default)
    {
        var task = await _repository.GetByIdAsync(id, ct);
        if (task is null)
            return Result<TaskDto>.NotFound($"Task with id {id} not found");

        task.status = status;
        task.updatedAt = DateTime.UtcNow;

        if (status == TaskStatus.COMPLETED && task.completedAt == null)
            task.completedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(task, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        var resultDto = await EnrichTaskDtoAsync(task, ct);
        return Result<TaskDto>.Success(resultDto);
    }

    public async Task<Result<TaskDto>> AssignAsync(string id, string? userId, CancellationToken ct = default)
    {
        var task = await _repository.GetByIdAsync(id, ct);
        if (task is null)
            return Result<TaskDto>.NotFound($"Task with id {id} not found");

        var agreement = await _agreementRepository.FindOneAsync(item => item.taskId == id, ct);
        if (agreement is not null || task.escrowStatus != EscrowStatus.NONE)
            return Result<TaskDto>.Conflict(WorkerBelongsToTheAgreement);

        task.assignedToId = string.IsNullOrWhiteSpace(userId) ? null : userId;
        task.updatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(task, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        var resultDto = await EnrichTaskDtoAsync(task, ct);
        return Result<TaskDto>.Success(resultDto);
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

    /// <summary>Tasks whose project this viewer may see, composed from the one project visibility predicate.</summary>
    private IQueryable<ProjectTask> VisibleTasks(ProjectViewer viewer)
    {
        if (viewer.IsAdmin)
            return _repository.Query();

        var visibleProjectIds = _projectRepository.Query()
            .Where(ProjectQueryFilters.VisibleTo(viewer))
            .Select(project => project.id);
        return _repository.Query().Where(task => visibleProjectIds.Contains(task.projectId));
    }

    private async Task<TaskDto> EnrichTaskDtoAsync(ProjectTask task, CancellationToken ct)
    {
        var dto = _mapper.Map<TaskDto>(task);

        if (task.assignedToId is not null)
        {
            var user = await _userRepository.GetByIdAsync(task.assignedToId, ct);
            if (user is not null)
                dto = dto with { AssignedTo = _mapper.Map<TaskUserDto>(user) };
        }

        var project = await _projectRepository.GetByIdAsync(task.projectId, ct);
        if (project is not null)
            dto = dto with { Project = _mapper.Map<TaskProjectDto>(project) };

        return dto;
    }

    private async Task<IReadOnlyList<TaskDto>> EnrichTaskDtosAsync(IEnumerable<ProjectTask> tasks, CancellationToken ct)
    {
        var dtos = new List<TaskDto>();
        foreach (var task in tasks)
        {
            dtos.Add(await EnrichTaskDtoAsync(task, ct));
        }
        return dtos;
    }
}
