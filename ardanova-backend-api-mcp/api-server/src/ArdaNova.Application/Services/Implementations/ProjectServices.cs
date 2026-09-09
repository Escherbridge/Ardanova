namespace ArdaNova.Application.Services.Implementations;

using System.Linq.Expressions;
using ArdaNova.Application.Common.Interfaces;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Interfaces;
using ArdaNova.Domain.Models.Entities;
using ArdaNova.Domain.Models.Enums;
using ArdaNova.Domain.Policies;
using AutoMapper;
using TaskStatus = ArdaNova.Domain.Models.Enums.TaskStatus;

/// <summary>Who a project read is answered for. Drafts are private to their creator, project members and admins.</summary>
public sealed record ProjectViewer(string? UserId, bool IsAdmin)
{
    /// <summary>No verified actor accompanied the request, so only work its owner chose to present is visible.</summary>
    public static readonly ProjectViewer Anonymous = new(null, false);
}

/// <summary>Viewer-scoped project reads; every list, search and detail route answers through this surface.</summary>
public interface IProjectVisibility
{
    Task<Result<IReadOnlyList<ProjectDto>>> GetAllVisibleAsync(
        ProjectViewer viewer,
        CancellationToken ct = default);

    Task<Result<PagedResult<ProjectDto>>> GetPagedVisibleAsync(
        ProjectViewer viewer,
        int page,
        int pageSize,
        CancellationToken ct = default);

    Task<Result<PagedResult<ProjectDto>>> SearchVisibleAsync(
        ProjectViewer viewer,
        string? searchTerm,
        ProjectStatus? status,
        string? category,
        ProjectType? projectType,
        int page,
        int pageSize,
        CancellationToken ct = default);

    Task<Result<ProjectDto>> GetVisibleByIdAsync(
        ProjectViewer viewer,
        string id,
        CancellationToken ct = default);

    Task<Result<ProjectDto>> GetVisibleBySlugAsync(
        ProjectViewer viewer,
        string slug,
        CancellationToken ct = default);

    Task<Result<IReadOnlyList<ProjectDto>>> GetVisibleByUserIdAsync(
        ProjectViewer viewer,
        string userId,
        CancellationToken ct = default);

    Task<Result<IReadOnlyList<ProjectDto>>> GetVisibleByStatusAsync(
        ProjectViewer viewer,
        ProjectStatus status,
        CancellationToken ct = default);

    Task<Result<IReadOnlyList<ProjectDto>>> GetVisibleByCategoryAsync(
        ProjectViewer viewer,
        string category,
        CancellationToken ct = default);

    Task<Result<IReadOnlyList<ProjectDto>>> GetVisibleByProjectTypeAsync(
        ProjectViewer viewer,
        ProjectType projectType,
        CancellationToken ct = default);

    Task<Result<IReadOnlyList<ProjectDto>>> GetFeaturedVisibleAsync(
        ProjectViewer viewer,
        CancellationToken ct = default);
}

/// <summary>
/// The one definition of what a project search term matches and of what a viewer may see. The service
/// and the repository both compose these, so a public listing and a search can never disagree.
/// </summary>
public static class ProjectQueryFilters
{
    /// <summary>A DRAFT — including the half-finished recovery draft the create wizard leaves behind — is private.</summary>
    public static Expression<Func<Project, bool>> VisibleTo(ProjectViewer viewer)
    {
        if (viewer.IsAdmin)
            return project => true;

        var viewerId = viewer.UserId;
        if (string.IsNullOrEmpty(viewerId))
            return project => project.status != ProjectStatus.DRAFT;

        return project => project.status != ProjectStatus.DRAFT
            || project.createdById == viewerId
            || project.ProjectMembers.Any(member => member.userId == viewerId);
    }

    /// <summary>The searchable surface of a project: the problem and the solution, not just the title.</summary>
    public static Expression<Func<Project, bool>> MatchesSearchTerm(string searchTerm)
    {
        var term = searchTerm.Trim().ToLowerInvariant();

        return project => project.title.ToLower().Contains(term)
            || project.description.ToLower().Contains(term)
            || project.problemStatement.ToLower().Contains(term)
            || project.solution.ToLower().Contains(term)
            // summary is the one-line ask that lands on Project beside the media paths and funding goal.
            || (project.summary != null && project.summary.ToLower().Contains(term))
            || (project.targetAudience != null && project.targetAudience.ToLower().Contains(term))
            || (project.expectedImpact != null && project.expectedImpact.ToLower().Contains(term))
            || (project.tags != null && project.tags.ToLower().Contains(term));
    }

    /// <summary>Visibility first, then the caller filters — so no filter can widen what a viewer may see.</summary>
    public static Expression<Func<Project, bool>> Build(
        ProjectViewer viewer,
        string? searchTerm,
        ProjectStatus? status,
        string? category,
        ProjectType? projectType)
    {
        var predicate = VisibleTo(viewer);

        if (!string.IsNullOrWhiteSpace(searchTerm))
            predicate = predicate.And(MatchesSearchTerm(searchTerm));

        if (status.HasValue)
        {
            var requestedStatus = status.Value;
            predicate = predicate.And(project => project.status == requestedStatus);
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            var requestedCategory = category;
            predicate = predicate.And(project => project.categories.Contains(requestedCategory));
        }

        if (projectType.HasValue)
        {
            var requestedType = projectType.Value;
            predicate = predicate.And(project => project.projectType == requestedType);
        }

        return predicate;
    }

    /// <summary>Joins two predicates into one lambda so EF Core still translates the whole filter to SQL.</summary>
    public static Expression<Func<T, bool>> And<T>(
        this Expression<Func<T, bool>> left,
        Expression<Func<T, bool>> right)
    {
        var parameter = Expression.Parameter(typeof(T), "entity");
        var body = Expression.AndAlso(
            ParameterRebinder.Rebind(left.Body, left.Parameters[0], parameter),
            ParameterRebinder.Rebind(right.Body, right.Parameters[0], parameter));

        return Expression.Lambda<Func<T, bool>>(body, parameter);
    }

    private sealed class ParameterRebinder : ExpressionVisitor
    {
        private readonly ParameterExpression _replaced;
        private readonly ParameterExpression _replacement;

        private ParameterRebinder(ParameterExpression replaced, ParameterExpression replacement)
        {
            _replaced = replaced;
            _replacement = replacement;
        }

        public static Expression Rebind(
            Expression body,
            ParameterExpression replaced,
            ParameterExpression replacement)
            => new ParameterRebinder(replaced, replacement).Visit(body);

        protected override Expression VisitParameter(ParameterExpression node)
            => ReferenceEquals(node, _replaced) ? _replacement : base.VisitParameter(node);
    }
}

/// <summary>
/// The one definition of how a project may move through its lifecycle, and the one place that
/// stamps its first publish date. Every entry point that carries a status — the edit route and the
/// publish route alike — moves the project through <see cref="MoveTo"/>, so no route can reach a
/// state the others refuse and no second publish can move the date the first one set.
/// </summary>
public static class ProjectLifecycle
{
    /// <summary>Statuses a sponsor sees as a live ask: DRAFT is still private and CANCELLED is withdrawn.</summary>
    public static bool IsLive(ProjectStatus status)
        => status is not ProjectStatus.DRAFT and not ProjectStatus.CANCELLED;

    private static readonly IReadOnlyDictionary<ProjectStatus, ProjectStatus[]> AllowedDestinations =
        new Dictionary<ProjectStatus, ProjectStatus[]>
        {
            // Leaving DRAFT is publishing, so a draft has exactly one live destination.
            [ProjectStatus.DRAFT] = new[] { ProjectStatus.PUBLISHED, ProjectStatus.CANCELLED },
            [ProjectStatus.PUBLISHED] = new[]
            {
                ProjectStatus.SEEKING_SUPPORT,
                ProjectStatus.FUNDED,
                ProjectStatus.IN_PROGRESS,
                ProjectStatus.COMPLETED,
                ProjectStatus.CANCELLED
            },
            [ProjectStatus.SEEKING_SUPPORT] = new[]
            {
                ProjectStatus.FUNDED,
                ProjectStatus.IN_PROGRESS,
                ProjectStatus.COMPLETED,
                ProjectStatus.CANCELLED
            },
            [ProjectStatus.FUNDED] = new[]
            {
                ProjectStatus.IN_PROGRESS,
                ProjectStatus.COMPLETED,
                ProjectStatus.CANCELLED
            },
            [ProjectStatus.IN_PROGRESS] = new[] { ProjectStatus.COMPLETED, ProjectStatus.CANCELLED },
            // Finished work stays finished, and a withdrawn ask comes back only as a private draft.
            [ProjectStatus.COMPLETED] = Array.Empty<ProjectStatus>(),
            [ProjectStatus.CANCELLED] = new[] { ProjectStatus.DRAFT }
        };

    /// <summary>Standing still is always legal, which is what makes a repeated publish idempotent.</summary>
    public static bool CanMove(ProjectStatus from, ProjectStatus to)
        => from == to
            || (AllowedDestinations.TryGetValue(from, out var destinations) && destinations.Contains(to));

    /// <summary>Names the refused move, so the caller learns which transition was rejected and not merely that one was.</summary>
    public static string RefusalFor(ProjectStatus from, ProjectStatus to)
    {
        var refusal = $"A project cannot move from {from} to {to}.";
        return from == ProjectStatus.CANCELLED
            ? $"{refusal} Reopen it as a {ProjectStatus.DRAFT} first."
            : refusal;
    }

    /// <summary>
    /// Moves the project, or refuses by name and leaves it untouched. The publish date is stamped the
    /// first time the project becomes live and is never re-stamped, so a second publish keeps the first date.
    /// </summary>
    /// <returns>The refusal reason, or <c>null</c> when the move was applied.</returns>
    public static string? MoveTo(Project project, ProjectStatus requested)
    {
        if (!CanMove(project.status, requested))
            return RefusalFor(project.status, requested);

        project.status = requested;

        if (IsLive(project.status))
            project.publishedAt ??= DateTime.UtcNow;

        return null;
    }
}

public class ProjectService : IProjectService, IProjectVisibility
{
    private readonly IProjectRepository _repository;
    private readonly IRepository<User> _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public ProjectService(
        IProjectRepository repository,
        IRepository<User> userRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper)
    {
        _repository = repository;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    /// <summary>Unscoped lookup by id. Authorization reads through this, so it must not hide drafts.</summary>
    public async Task<Result<ProjectDto>> GetByIdAsync(string id, CancellationToken ct = default)
    {
        var project = await _repository.GetByIdAsync(id, ct);
        if (project is null)
            return Result<ProjectDto>.NotFound($"Project with id {id} not found");

        return Result<ProjectDto>.Success(await WithCreatorAsync(project, ct));
    }

    /// <summary>Unscoped lookup by slug. Public reads go through <see cref="GetVisibleBySlugAsync"/>.</summary>
    public async Task<Result<ProjectDto>> GetBySlugAsync(string slug, CancellationToken ct = default)
    {
        var project = await _repository.FindOneAsync(p => p.slug == slug, ct);
        if (project is null)
            return Result<ProjectDto>.NotFound($"Project with slug {slug} not found");

        return Result<ProjectDto>.Success(await WithCreatorAsync(project, ct));
    }

    public Task<Result<ProjectDto>> GetVisibleByIdAsync(
        ProjectViewer viewer,
        string id,
        CancellationToken ct = default)
        => FindVisibleAsync(
            ProjectQueryFilters.VisibleTo(viewer).And(project => project.id == id),
            $"Project with id {id} not found",
            ct);

    public Task<Result<ProjectDto>> GetVisibleBySlugAsync(
        ProjectViewer viewer,
        string slug,
        CancellationToken ct = default)
        => FindVisibleAsync(
            ProjectQueryFilters.VisibleTo(viewer).And(project => project.slug == slug),
            $"Project with slug {slug} not found",
            ct);

    /// <summary>Legacy unscoped listing; answered as the anonymous public would see it.</summary>
    public Task<Result<IReadOnlyList<ProjectDto>>> GetAllAsync(CancellationToken ct = default)
        => GetAllVisibleAsync(ProjectViewer.Anonymous, ct);

    public async Task<Result<IReadOnlyList<ProjectDto>>> GetAllVisibleAsync(
        ProjectViewer viewer,
        CancellationToken ct = default)
        => Result<IReadOnlyList<ProjectDto>>.Success(
            await ListVisibleAsync(ProjectQueryFilters.VisibleTo(viewer), ct));

    /// <summary>Legacy unscoped paging; answered as the anonymous public would see it.</summary>
    public Task<Result<PagedResult<ProjectDto>>> GetPagedAsync(
        int page,
        int pageSize,
        CancellationToken ct = default)
        => GetPagedVisibleAsync(ProjectViewer.Anonymous, page, pageSize, ct);

    public async Task<Result<PagedResult<ProjectDto>>> GetPagedVisibleAsync(
        ProjectViewer viewer,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var result = await _repository.GetPagedAsync(
            page,
            pageSize,
            ProjectQueryFilters.VisibleTo(viewer),
            ct);

        return Result<PagedResult<ProjectDto>>.Success(await EnrichPageAsync(result, ct));
    }

    /// <summary>Legacy unscoped search; the repository overload it calls excludes drafts by construction.</summary>
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
        return Result<PagedResult<ProjectDto>>.Success(await EnrichPageAsync(result, ct));
    }

    public async Task<Result<PagedResult<ProjectDto>>> SearchVisibleAsync(
        ProjectViewer viewer,
        string? searchTerm,
        ProjectStatus? status,
        string? category,
        ProjectType? projectType,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var result = await _repository.GetPagedAsync(
            page,
            pageSize,
            ProjectQueryFilters.Build(viewer, searchTerm, status, category, projectType),
            ct);

        return Result<PagedResult<ProjectDto>>.Success(await EnrichPageAsync(result, ct));
    }

    public Task<Result<IReadOnlyList<ProjectDto>>> GetByUserIdAsync(string userId, CancellationToken ct = default)
        => GetVisibleByUserIdAsync(ProjectViewer.Anonymous, userId, ct);

    public async Task<Result<IReadOnlyList<ProjectDto>>> GetVisibleByUserIdAsync(
        ProjectViewer viewer,
        string userId,
        CancellationToken ct = default)
        => Result<IReadOnlyList<ProjectDto>>.Success(
            await ListVisibleAsync(
                ProjectQueryFilters.VisibleTo(viewer).And(project => project.createdById == userId),
                ct));

    public Task<Result<IReadOnlyList<ProjectDto>>> GetByStatusAsync(ProjectStatus status, CancellationToken ct = default)
        => GetVisibleByStatusAsync(ProjectViewer.Anonymous, status, ct);

    public async Task<Result<IReadOnlyList<ProjectDto>>> GetVisibleByStatusAsync(
        ProjectViewer viewer,
        ProjectStatus status,
        CancellationToken ct = default)
        => Result<IReadOnlyList<ProjectDto>>.Success(
            await ListVisibleAsync(
                ProjectQueryFilters.VisibleTo(viewer).And(project => project.status == status),
                ct));

    public Task<Result<IReadOnlyList<ProjectDto>>> GetByCategory(string category, CancellationToken ct = default)
        => GetVisibleByCategoryAsync(ProjectViewer.Anonymous, category, ct);

    public async Task<Result<IReadOnlyList<ProjectDto>>> GetVisibleByCategoryAsync(
        ProjectViewer viewer,
        string category,
        CancellationToken ct = default)
        => Result<IReadOnlyList<ProjectDto>>.Success(
            await ListVisibleAsync(
                ProjectQueryFilters.VisibleTo(viewer).And(project => project.categories.Contains(category)),
                ct));

    public Task<Result<IReadOnlyList<ProjectDto>>> GetByProjectTypeAsync(ProjectType projectType, CancellationToken ct = default)
        => GetVisibleByProjectTypeAsync(ProjectViewer.Anonymous, projectType, ct);

    public async Task<Result<IReadOnlyList<ProjectDto>>> GetVisibleByProjectTypeAsync(
        ProjectViewer viewer,
        ProjectType projectType,
        CancellationToken ct = default)
        => Result<IReadOnlyList<ProjectDto>>.Success(
            await ListVisibleAsync(
                ProjectQueryFilters.VisibleTo(viewer).And(project => project.projectType == projectType),
                ct));

    public Task<Result<IReadOnlyList<ProjectDto>>> GetFeaturedAsync(CancellationToken ct = default)
        => GetFeaturedVisibleAsync(ProjectViewer.Anonymous, ct);

    public async Task<Result<IReadOnlyList<ProjectDto>>> GetFeaturedVisibleAsync(
        ProjectViewer viewer,
        CancellationToken ct = default)
        => Result<IReadOnlyList<ProjectDto>>.Success(
            await ListVisibleAsync(
                ProjectQueryFilters.VisibleTo(viewer).And(project => project.featured),
                ct));

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

    /// <summary>
    /// The declared unit of an ask, refused unless it is the one the funding columns are kept in.
    /// A goal in any other unit is stored beside a dollar total and divided into a dollar price.
    /// </summary>
    private static string? DescribeUnsupportedFundingUnit(string? declaredUnit) =>
        declaredUnit is null
            ? null
            : ProjectFundingLedger.DescribeUnsupportedGoalUnit(declaredUnit);

    public async Task<Result<ProjectDto>> CreateAsync(CreateProjectDto dto, CancellationToken ct = default)
    {
        var categoryError = ValidateCategories(dto.Categories);
        if (categoryError is not null) return categoryError;

        if (DescribeUnsupportedFundingUnit(dto.FundingGoalUnit) is { } createUnitRefusal)
            return Result<ProjectDto>.ValidationError(createUnitRefusal);

        var project = new Project
        {
            id = Guid.NewGuid().ToString(),
            createdById = dto.CreatedById,
            title = dto.Title,
            slug = GenerateSlug(dto.Title),
            description = dto.Description,
            summary = dto.Summary,
            problemStatement = dto.ProblemStatement,
            solution = dto.Solution,
            categories = string.Join(",", dto.Categories),
            status = ProjectStatus.DRAFT,
            fundingGoal = dto.FundingGoal,
            fundingGoalUnit = dto.FundingGoalUnit ?? ProjectFundingUnits.Ledger,
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
            mediaPaths = dto.MediaPaths ?? ProjectMediaPaths.Empty,
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

        // Decided before any field is touched, for the same reason the lifecycle move below is:
        // a refused unit must leave the tracked entity exactly as it was read.
        if (DescribeUnsupportedFundingUnit(dto.FundingGoalUnit) is { } updateUnitRefusal)
            return Result<ProjectDto>.ValidationError(updateUnitRefusal);

        var project = await _repository.GetByIdAsync(id, ct);
        if (project is null)
            return Result<ProjectDto>.NotFound($"Project with id {id} not found");

        // The lifecycle move — and the publish stamp it implies — is decided before any field is
        // touched, so a refused transition leaves the tracked entity exactly as it was read.
        var refusal = ProjectLifecycle.MoveTo(project, dto.Status ?? project.status);
        if (refusal is not null)
            return Result<ProjectDto>.ValidationError(refusal);

        if (dto.Title is not null) project.title = dto.Title;
        if (dto.Description is not null) project.description = dto.Description;
        if (dto.Summary is not null) project.summary = dto.Summary;
        if (dto.ProblemStatement is not null) project.problemStatement = dto.ProblemStatement;
        if (dto.Solution is not null) project.solution = dto.Solution;
        if (dto.Categories is not null) project.categories = string.Join(",", dto.Categories);
        if (dto.Tags is not null) project.tags = dto.Tags;
        if (dto.TargetAudience is not null) project.targetAudience = dto.TargetAudience;
        if (dto.ExpectedImpact is not null) project.expectedImpact = dto.ExpectedImpact;
        if (dto.Timeline is not null) project.timeline = dto.Timeline;
        if (dto.ProjectType.HasValue) project.projectType = dto.ProjectType.Value;
        if (dto.Duration.HasValue) project.duration = dto.Duration;
        if (dto.Images is not null) project.images = dto.Images;
        if (dto.Videos is not null) project.videos = dto.Videos;
        if (dto.Documents is not null) project.documents = dto.Documents;
        if (dto.MediaPaths is not null) project.mediaPaths = dto.MediaPaths;
        if (dto.FundingGoalUnit is not null) project.fundingGoalUnit = dto.FundingGoalUnit;

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

    /// <summary>Idempotent: a project that is already public comes back untouched and keeps its first publish date.</summary>
    public async Task<Result<ProjectDto>> PublishAsync(string id, CancellationToken ct = default)
    {
        var project = await _repository.GetByIdAsync(id, ct);
        if (project is null)
            return Result<ProjectDto>.NotFound($"Project with id {id} not found");

        // Already live and dated: publishing again is a no-op that must not move the first date.
        if (ProjectLifecycle.IsLive(project.status) && project.publishedAt.HasValue)
            return Result<ProjectDto>.Success(await WithCreatorAsync(project, ct));

        // A live but undated project is only missing its stamp; anything else is asked to become PUBLISHED,
        // which is where a CANCELLED project is refused by the same predicate the edit route answers to.
        var requested = ProjectLifecycle.IsLive(project.status) ? project.status : ProjectStatus.PUBLISHED;
        var refusal = ProjectLifecycle.MoveTo(project, requested);
        if (refusal is not null)
            return Result<ProjectDto>.ValidationError(refusal);

        project.updatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(project, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<ProjectDto>.Success(await WithCreatorAsync(project, ct));
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

    private async Task<Result<ProjectDto>> FindVisibleAsync(
        Expression<Func<Project, bool>> predicate,
        string notFoundMessage,
        CancellationToken ct)
    {
        var project = await _repository.FindOneAsync(predicate, ct);

        // A draft the viewer may not see is reported absent, never forbidden: its existence is private too.
        if (project is null)
            return Result<ProjectDto>.NotFound(notFoundMessage);

        return Result<ProjectDto>.Success(await WithCreatorAsync(project, ct));
    }

    private async Task<IReadOnlyList<ProjectDto>> ListVisibleAsync(
        Expression<Func<Project, bool>> predicate,
        CancellationToken ct)
        => await EnrichWithUserDataAsync(await _repository.FindAsync(predicate, ct), ct);

    private async Task<PagedResult<ProjectDto>> EnrichPageAsync(PagedResult<Project> page, CancellationToken ct)
        => new PagedResult<ProjectDto>(
            (await EnrichWithUserDataAsync(page.Items, ct)).ToList(),
            page.TotalCount,
            page.Page,
            page.PageSize);

    private async Task<ProjectDto> WithCreatorAsync(Project project, CancellationToken ct)
    {
        var dto = _mapper.Map<ProjectDto>(project);
        var creator = await _userRepository.GetByIdAsync(project.createdById, ct);
        return creator is null ? dto : dto with { CreatedBy = _mapper.Map<ProjectCreatorDto>(creator) };
    }

    /// <summary>Attaches each creator once, however many of their projects are in the list.</summary>
    private async Task<IReadOnlyList<ProjectDto>> EnrichWithUserDataAsync(
        IEnumerable<Project> projects,
        CancellationToken ct)
    {
        var materialized = projects as IReadOnlyList<Project> ?? projects.ToList();
        var dtos = _mapper.Map<List<ProjectDto>>(materialized);
        if (dtos.Count != materialized.Count)
            return dtos;

        var creators = new Dictionary<string, User>(StringComparer.Ordinal);
        foreach (var creatorId in materialized.Select(project => project.createdById).Distinct(StringComparer.Ordinal))
        {
            var creator = await _userRepository.GetByIdAsync(creatorId, ct);
            if (creator is not null)
                creators[creatorId] = creator;
        }

        for (var index = 0; index < dtos.Count; index++)
        {
            if (creators.TryGetValue(materialized[index].createdById, out var creator))
                dtos[index] = dtos[index] with { CreatedBy = _mapper.Map<ProjectCreatorDto>(creator) };
        }

        return dtos;
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
    private readonly IRepository<User> _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public ProjectCommentService(IRepository<ProjectComment> repository, IRepository<User> userRepository, IUnitOfWork unitOfWork, IMapper mapper)
    {
        _repository = repository;
        _userRepository = userRepository;
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

    public async Task<Result<IReadOnlyList<ProjectCommentDto>>> GetByTargetAsync(CommentTargetType targetType, string targetId, CancellationToken ct = default)
    {
        var comments = await _repository.FindAsync(c => c.targetType == targetType && c.targetId == targetId, ct);
        var dtos = new List<ProjectCommentDto>();
        foreach (var comment in comments.OrderBy(c => c.createdAt))
        {
            var dto = _mapper.Map<ProjectCommentDto>(comment);
            var user = await _userRepository.GetByIdAsync(comment.userId, ct);
            if (user is not null)
                dto = dto with { Author = new ProjectCommentAuthorDto { Id = user.id, Name = user.name, Image = user.image } };
            dtos.Add(dto);
        }
        return Result<IReadOnlyList<ProjectCommentDto>>.Success(dtos);
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
            targetType = dto.TargetType,
            targetId = dto.TargetId ?? dto.ProjectId,
            createdAt = DateTime.UtcNow,
            updatedAt = DateTime.UtcNow
        };
        await _repository.AddAsync(comment, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        var resultDto = _mapper.Map<ProjectCommentDto>(comment);
        var user = await _userRepository.GetByIdAsync(comment.userId, ct);
        if (user is not null)
            resultDto = resultDto with { Author = new ProjectCommentAuthorDto { Id = user.id, Name = user.name, Image = user.image } };
        return Result<ProjectCommentDto>.Success(resultDto);
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
        if (dto.ShareBalance.HasValue || dto.VotingPower.HasValue)
        {
            return Result<ProjectMemberDto>.ValidationError(
                "Project-unit and voting-power allocation is unavailable until provider-backed settlement and governance controls are enabled.");
        }

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
