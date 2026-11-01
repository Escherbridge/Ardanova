namespace ArdaNova.Application.Services.Implementations;

using ArdaNova.Application.Common.Interfaces;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Interfaces;
using ArdaNova.Application.Tasks;
using ArdaNova.Domain.Models.Entities;
using ArdaNova.Domain.Models.Enums;
using AutoMapper;

public class TaskService : ITaskService
{
    /// <summary>Why a claim was refused: someone else holds the task (<see cref="TaskClaimCodes.Taken"/>).</summary>
    public const string TaskAlreadyPickedUp = "Already picked up.";

    /// <summary>Why a claim was refused: the task is COMPLETED (<see cref="TaskClaimCodes.Done"/>).</summary>
    public const string TaskIsDone = "This task is done.";

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

    /// <summary>Longest task title the board accepts.</summary>
    public const int TaskTitleMaxLength = 200;

    /// <summary>Why a task was refused: the title would not fit a board card.</summary>
    public static readonly string TaskTitleTooLong = $"A task title can be at most {TaskTitleMaxLength} characters.";

    /// <summary>Why a task was refused: hours of work cannot be negative.</summary>
    public const string TaskHoursNegative = "Estimated and actual hours cannot be negative.";

    /// <summary>The first problem with a task's title or hours, or null when they are usable.</summary>
    private static string? DescribeInvalidTaskFields(string? title, bool titleSupplied, int? estimatedHours, int? actualHours)
    {
        if (titleSupplied && string.IsNullOrWhiteSpace(title))
            return TaskTitleRequired;
        if (title is not null && title.Trim().Length > TaskTitleMaxLength)
            return TaskTitleTooLong;
        if (estimatedHours is < 0 || actualHours is < 0)
            return TaskHoursNegative;
        return null;
    }

    /// <summary>completedAt follows the status: stamped once on COMPLETED, cleared when the task is reopened.</summary>
    private static void StampCompletion(ProjectTask task)
        => task.completedAt = task.status == TaskStatus.COMPLETED ? task.completedAt ?? DateTime.UtcNow : null;

    private readonly IRepository<ProjectTask> _repository;
    private readonly IRepository<User> _userRepository;
    private readonly IRepository<Project> _projectRepository;
    private readonly IRepository<TaskCommerceAgreement> _agreementRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IRepository<Post> _postRepository;
    private readonly ITaskClaimStore _claims;
    private readonly INotificationService _notifications;

    /// <param name="agreementRepository">
    /// Read-only lookup that decides whether this task's award is already an agreed one.
    /// Required rather than optional: a missing repository would silently skip the guard below,
    /// and a skipped guard lets a task's figure drift away from the award actually held.
    /// </param>
    /// <param name="postRepository">Reads a task's HELP_WANTED ask for <see cref="TaskDto.HelpWanted"/>.</param>
    /// <param name="claims">The self-claim's conditional write.</param>
    /// <param name="notifications">Stages the asker's "picked up" inside the claim's transaction.</param>
    public TaskService(
        IRepository<ProjectTask> repository,
        IRepository<User> userRepository,
        IRepository<Project> projectRepository,
        IRepository<TaskCommerceAgreement> agreementRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IRepository<Post> postRepository,
        ITaskClaimStore claims,
        INotificationService notifications)
    {
        _repository = repository;
        _userRepository = userRepository;
        _projectRepository = projectRepository;
        _agreementRepository = agreementRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _postRepository = postRepository;
        _claims = claims;
        _notifications = notifications;
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
        bool askableOnly = false,
        CancellationToken ct = default)
    {
        var query = VisibleTasks(viewer);

        // Filtered before the page is cut, so open tasks behind 100 done ones still reach the ask picker.
        if (askableOnly)
            query = query.Where(t => t.assignedToId == null && t.status != TaskStatus.COMPLETED);

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
        if (DescribeInvalidTaskFields(dto.Title, true, dto.EstimatedHours, null) is { } createRefusal)
            return Result<TaskDto>.ValidationError(createRefusal);

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
        if (DescribeInvalidTaskFields(dto.Title, dto.Title is not null, dto.EstimatedHours, dto.ActualHours) is { } updateRefusal)
            return Result<TaskDto>.ValidationError(updateRefusal);

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
        StampCompletion(task);

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
        StampCompletion(task);

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
            return Result<TaskDto>.Conflict(WorkerBelongsToTheAgreement).WithCode(TaskClaimCodes.UnderAgreement);

        task.assignedToId = string.IsNullOrWhiteSpace(userId) ? null : userId;
        task.updatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(task, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        var resultDto = await EnrichTaskDtoAsync(task, ct);
        return Result<TaskDto>.Success(resultDto);
    }

    public async Task<Result<TaskDto>> ClaimAsync(string id, string userId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return Result<TaskDto>.Unauthorized("A signed-in member is required");

        bool claimed;
        await _unitOfWork.BeginTransactionAsync(ct);
        try
        {
            claimed = await _claims.TryClaimAsync(id, userId, ct) == 1;
            if (claimed)
            {
                // Inside the claim's transaction: the losing claim writes no notification, the winner exactly one.
                await _notifications.StageAskPickedUpAsync(id, userId, ct);
                await _unitOfWork.SaveChangesAsync(ct);
                await _unitOfWork.CommitTransactionAsync(ct);
            }
            else
            {
                await _unitOfWork.RollbackTransactionAsync(ct);
            }
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(CancellationToken.None);
            _unitOfWork.ClearTrackedChanges();
            throw;
        }

        // The UPDATE bypassed the change tracker; drop what this scope tracked so the read below sees the row.
        _unitOfWork.ClearTrackedChanges();
        var task = await _repository.GetByIdAsync(id, ct);
        if (task is null)
            return Result<TaskDto>.NotFound($"Task with id {id} not found");

        // A double tap: the other request already made it theirs, which is the state asked for.
        if (claimed || string.Equals(task.assignedToId, userId, StringComparison.Ordinal))
            return Result<TaskDto>.Success(await EnrichTaskDtoAsync(task, ct));

        return await RefuseClaimAsync(task, ct);
    }

    /// <summary>Names why the conditional write found nothing to claim, from the task's facts as they are now.</summary>
    private async Task<Result<TaskDto>> RefuseClaimAsync(ProjectTask task, CancellationToken ct)
    {
        var hasAgreement = await _agreementRepository.ExistsAsync(item => item.taskId == task.id, ct);
        var facts = new TaskClaimFacts(task.assignedToId, task.status, task.escrowStatus, hasAgreement, task.equityReward);
        return TaskClaimRule.Evaluate(facts, viewerIsProjectMember: true) switch
        {
            TaskClaimVerdict.Completed => Result<TaskDto>.Conflict(TaskIsDone).WithCode(TaskClaimCodes.Done),
            TaskClaimVerdict.UnderAgreement =>
                Result<TaskDto>.Conflict(WorkerBelongsToTheAgreement).WithCode(TaskClaimCodes.UnderAgreement),
            // AlreadyTaken, or open again by this read: either way the write lost to another claim.
            _ => Result<TaskDto>.Conflict(TaskAlreadyPickedUp).WithCode(TaskClaimCodes.Taken),
        };
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
        => (await EnrichTaskDtosAsync([task], ct))[0];

    /// <summary>Maps tasks with assignee, project, agreement and ask; agreements, asks and askers are one read each per page.</summary>
    private async Task<IReadOnlyList<TaskDto>> EnrichTaskDtosAsync(IEnumerable<ProjectTask> tasks, CancellationToken ct)
    {
        var page = tasks.ToList();
        if (page.Count == 0)
            return [];

        var taskIds = page.Select(task => task.id).ToList();
        var agreed = (await _agreementRepository.FindAsync(agreement => taskIds.Contains(agreement.taskId), ct))
            .Select(agreement => agreement.taskId)
            .ToHashSet(StringComparer.Ordinal);
        // Only an ask whose own audience is everyone: these reads carry no viewer. See Implementations/AGENTS.md §Help wanted.
        var asks = (await _postRepository.FindAsync(
                post => post.type == PostType.HELP_WANTED && post.taskId != null && taskIds.Contains(post.taskId),
                ct))
            .Where(PostVisibilityPolicy.IsPubliclyVisible)
            .GroupBy(post => post.taskId!, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
        var askerIds = asks.Values.Select(post => post.authorId).Distinct().ToList();
        var askers = askerIds.Count == 0
            ? new Dictionary<string, User>(StringComparer.Ordinal)
            : (await _userRepository.FindAsync(user => askerIds.Contains(user.id), ct))
                .ToDictionary(user => user.id, StringComparer.Ordinal);

        var dtos = new List<TaskDto>(page.Count);
        foreach (var task in page)
        {
            var dto = _mapper.Map<TaskDto>(task) with { HasAgreement = agreed.Contains(task.id) };

            if (task.assignedToId is not null)
            {
                var user = await _userRepository.GetByIdAsync(task.assignedToId, ct);
                if (user is not null)
                    dto = dto with { AssignedTo = _mapper.Map<TaskUserDto>(user) };
            }

            var project = await _projectRepository.GetByIdAsync(task.projectId, ct);
            if (project is not null)
                dto = dto with { Project = _mapper.Map<TaskProjectDto>(project) };

            if (asks.TryGetValue(task.id, out var ask))
            {
                dto = dto with
                {
                    HelpWanted = new TaskHelpWantedDto
                    {
                        PostId = ask.id,
                        AskerId = ask.authorId,
                        AskerName = askers.GetValueOrDefault(ask.authorId)?.name,
                    },
                };
            }

            dtos.Add(dto);
        }
        return dtos;
    }
}
