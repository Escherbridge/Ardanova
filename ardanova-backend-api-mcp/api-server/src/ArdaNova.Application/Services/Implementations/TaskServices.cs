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
    private readonly IRepository<ProjectTask> _repository;
    private readonly IRepository<User> _userRepository;
    private readonly IRepository<Project> _projectRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public TaskService(
        IRepository<ProjectTask> repository,
        IRepository<User> userRepository,
        IRepository<Project> projectRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper)
    {
        _repository = repository;
        _userRepository = userRepository;
        _projectRepository = projectRepository;
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

    public async Task<Result<IReadOnlyList<TaskDto>>> GetAllAsync(CancellationToken ct = default)
    {
        var tasks = await _repository.GetAllAsync(ct);
        var dtos = await EnrichTaskDtosAsync(tasks, ct);
        return Result<IReadOnlyList<TaskDto>>.Success(dtos);
    }

    public async Task<Result<PagedResult<TaskDto>>> GetPagedAsync(int page, int pageSize, CancellationToken ct = default)
    {
        var result = await _repository.GetPagedAsync(page, pageSize, null, ct);
        var dtos = await EnrichTaskDtosAsync(result.Items, ct);
        return Result<PagedResult<TaskDto>>.Success(new PagedResult<TaskDto>(dtos.ToList(), result.TotalCount, result.Page, result.PageSize));
    }

    public async Task<Result<PagedResult<TaskDto>>> SearchAsync(
        string? searchTerm,
        TaskStatus? status,
        TaskPriority? priority,
        TaskType? taskType,
        string? projectId,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var query = _repository.Query();

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
        var project = await _projectRepository.GetByIdAsync(dto.ProjectId, ct);
        if (project is null)
            return Result<TaskDto>.NotFound($"Project with id {dto.ProjectId} not found");

        var task = new ProjectTask
        {
            id = Guid.NewGuid().ToString(),
            projectId = dto.ProjectId,
            title = dto.Title,
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

        if (dto.Title is not null) task.title = dto.Title;
        if (dto.Description is not null) task.description = dto.Description;
        if (dto.Status.HasValue) task.status = dto.Status.Value;
        if (dto.Priority.HasValue) task.priority = dto.Priority.Value;
        if (dto.TaskType.HasValue) task.taskType = dto.TaskType.Value;
        if (dto.EstimatedHours.HasValue) task.estimatedHours = dto.EstimatedHours;
        if (dto.ActualHours.HasValue) task.actualHours = dto.ActualHours;
        if (dto.EquityReward.HasValue) task.equityReward = dto.EquityReward;
        if (dto.DueDate.HasValue) task.dueDate = dto.DueDate;
        if (dto.AssignedToId is not null) task.assignedToId = dto.AssignedToId;
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

    public async Task<Result<bool>> DeleteAsync(string id, CancellationToken ct = default)
    {
        var task = await _repository.GetByIdAsync(id, ct);
        if (task is null)
            return Result<bool>.NotFound($"Task with id {id} not found");

        await _repository.DeleteAsync(task, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<bool>.Success(true);
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
