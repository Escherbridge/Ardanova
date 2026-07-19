namespace ArdaNova.Application.Services.Implementations;

using ArdaNova.Application.Services.Interfaces;
using ArdaNova.Domain.Models.Enums;

public sealed class HierarchyAuthorizationService : IHierarchyAuthorizationService
{
    private static readonly HashSet<ProjectRole> ManagerRoles =
    [
        ProjectRole.FOUNDER,
        ProjectRole.LEADER,
        ProjectRole.CORE_CONTRIBUTOR
    ];

    private readonly IProjectService _projectService;
    private readonly IProjectMemberService _memberService;
    private readonly IProjectMilestoneService _milestoneService;
    private readonly IEpicService _epicService;
    private readonly ISprintService _sprintService;
    private readonly IFeatureService _featureService;
    private readonly IProductBacklogItemService _pbiService;
    private readonly ITaskService _taskService;

    public HierarchyAuthorizationService(
        IProjectService projectService,
        IProjectMemberService memberService,
        IProjectMilestoneService milestoneService,
        IEpicService epicService,
        ISprintService sprintService,
        IFeatureService featureService,
        IProductBacklogItemService pbiService,
        ITaskService taskService)
    {
        _projectService = projectService;
        _memberService = memberService;
        _milestoneService = milestoneService;
        _epicService = epicService;
        _sprintService = sprintService;
        _featureService = featureService;
        _pbiService = pbiService;
        _taskService = taskService;
    }

    public async Task<bool> CanManageProjectAsync(
        string actorId,
        string projectId,
        CancellationToken ct = default)
    {
        var project = await _projectService.GetByIdAsync(projectId, ct);
        if (!project.IsSuccess)
            return false;
        if (string.Equals(project.Value!.CreatedById, actorId, StringComparison.Ordinal))
            return true;

        var members = await _memberService.GetByProjectIdAsync(projectId, ct);
        return members.IsSuccess && members.Value!.Any(member =>
            string.Equals(member.UserId, actorId, StringComparison.Ordinal) &&
            ManagerRoles.Contains(member.Role));
    }

    public async Task<bool> CanWorkOnItemAsync(
        string actorId,
        string projectId,
        string? assigneeId,
        CancellationToken ct = default)
        => string.Equals(actorId, assigneeId, StringComparison.Ordinal) ||
           await CanManageProjectAsync(actorId, projectId, ct);

    public async Task<bool> IsProjectMemberAsync(
        string userId,
        string projectId,
        CancellationToken ct = default)
    {
        var project = await _projectService.GetByIdAsync(projectId, ct);
        if (!project.IsSuccess)
            return false;
        if (string.Equals(project.Value!.CreatedById, userId, StringComparison.Ordinal))
            return true;

        var members = await _memberService.GetByProjectIdAsync(projectId, ct);
        return members.IsSuccess && members.Value!.Any(member =>
            string.Equals(member.UserId, userId, StringComparison.Ordinal));
    }

    public async Task<string?> ResolveCommentTargetProjectAsync(
        CommentTargetType targetType,
        string targetId,
        CancellationToken ct = default)
    {
        var target = await ResolveTargetAsync(targetType, targetId, ct);
        return target?.ProjectId;
    }

    private async Task<ResolvedTarget?> ResolveTargetAsync(
        CommentTargetType targetType,
        string targetId,
        CancellationToken ct)
    {
        switch (targetType)
        {
            case CommentTargetType.PROJECT:
            {
                var project = await _projectService.GetByIdAsync(targetId, ct);
                return project.IsSuccess &&
                       string.Equals(project.Value!.Id, targetId, StringComparison.Ordinal)
                    ? CreateResolvedTarget(targetType, targetId, project.Value.Id, [])
                    : null;
            }
            case CommentTargetType.MILESTONE:
            {
                var milestone = await _milestoneService.GetByIdAsync(targetId, ct);
                if (!milestone.IsSuccess)
                    return null;
                var project = await ResolveTargetAsync(
                    CommentTargetType.PROJECT,
                    milestone.Value!.ProjectId,
                    ct);
                return CreateResolvedTarget(
                    targetType,
                    targetId,
                    milestone.Value.ProjectId,
                    [project]);
            }
            case CommentTargetType.EPIC:
            {
                var epic = await _epicService.GetByIdAsync(targetId, ct);
                if (!epic.IsSuccess)
                    return null;
                var milestone = await ResolveTargetAsync(
                    CommentTargetType.MILESTONE,
                    epic.Value!.MilestoneId,
                    ct);
                return CreateResolvedTarget(
                    targetType,
                    targetId,
                    epic.Value.ProjectId,
                    [milestone]);
            }
            case CommentTargetType.SPRINT:
            {
                var sprint = await _sprintService.GetByIdAsync(targetId, ct);
                if (!sprint.IsSuccess)
                    return null;
                var epic = await ResolveTargetAsync(
                    CommentTargetType.EPIC,
                    sprint.Value!.EpicId,
                    ct);
                return CreateResolvedTarget(
                    targetType,
                    targetId,
                    sprint.Value.ProjectId,
                    [epic]);
            }
            case CommentTargetType.FEATURE:
            {
                var feature = await _featureService.GetByIdAsync(targetId, ct);
                if (!feature.IsSuccess)
                    return null;
                var sprint = await ResolveTargetAsync(
                    CommentTargetType.SPRINT,
                    feature.Value!.SprintId,
                    ct);
                return CreateResolvedTarget(
                    targetType,
                    targetId,
                    feature.Value.ProjectId,
                    [sprint]);
            }
            case CommentTargetType.PBI:
            {
                var item = await _pbiService.GetByIdAsync(targetId, ct);
                if (!item.IsSuccess)
                    return null;

                var parentTasks = new List<Task<ResolvedTarget?>>();
                if (item.Value!.MilestoneId is not null)
                    parentTasks.Add(ResolveTargetAsync(CommentTargetType.MILESTONE, item.Value.MilestoneId, ct));
                if (item.Value.EpicId is not null)
                    parentTasks.Add(ResolveTargetAsync(CommentTargetType.EPIC, item.Value.EpicId, ct));
                if (item.Value.SprintId is not null)
                    parentTasks.Add(ResolveTargetAsync(CommentTargetType.SPRINT, item.Value.SprintId, ct));
                if (item.Value.FeatureId is not null)
                    parentTasks.Add(ResolveTargetAsync(CommentTargetType.FEATURE, item.Value.FeatureId, ct));
                if (parentTasks.Count == 0)
                    parentTasks.Add(ResolveTargetAsync(CommentTargetType.PROJECT, item.Value.ProjectId, ct));

                return CreateResolvedTarget(
                    targetType,
                    targetId,
                    item.Value.ProjectId,
                    await Task.WhenAll(parentTasks));
            }
            case CommentTargetType.TASK:
            {
                var task = await _taskService.GetByIdAsync(targetId, ct);
                if (!task.IsSuccess)
                    return null;

                var parentTasks = new List<Task<ResolvedTarget?>>();
                if (task.Value!.MilestoneId is not null)
                    parentTasks.Add(ResolveTargetAsync(CommentTargetType.MILESTONE, task.Value.MilestoneId, ct));
                if (task.Value.EpicId is not null)
                    parentTasks.Add(ResolveTargetAsync(CommentTargetType.EPIC, task.Value.EpicId, ct));
                if (task.Value.SprintId is not null)
                    parentTasks.Add(ResolveTargetAsync(CommentTargetType.SPRINT, task.Value.SprintId, ct));
                if (task.Value.FeatureId is not null)
                    parentTasks.Add(ResolveTargetAsync(CommentTargetType.FEATURE, task.Value.FeatureId, ct));
                if (task.Value.PbiId is not null)
                    parentTasks.Add(ResolveTargetAsync(CommentTargetType.PBI, task.Value.PbiId, ct));
                if (parentTasks.Count == 0)
                    parentTasks.Add(ResolveTargetAsync(CommentTargetType.PROJECT, task.Value.ProjectId, ct));

                return CreateResolvedTarget(
                    targetType,
                    targetId,
                    task.Value.ProjectId,
                    await Task.WhenAll(parentTasks));
            }
            default:
                return null;
        }
    }

    private static ResolvedTarget? CreateResolvedTarget(
        CommentTargetType targetType,
        string targetId,
        string projectId,
        IReadOnlyList<ResolvedTarget?> parents)
    {
        if (parents.Any(parent => parent is null ||
            !string.Equals(parent.ProjectId, projectId, StringComparison.Ordinal)))
            return null;

        var concreteParents = parents.OfType<ResolvedTarget>().ToArray();
        if (concreteParents.Length > 1)
        {
            var deepest = concreteParents.MaxBy(parent => HierarchyDepth(parent.TargetType))!;
            foreach (var parent in concreteParents)
            {
                if (!deepest.Ancestry.TryGetValue(parent.TargetType, out var ancestorId) ||
                    !string.Equals(ancestorId, parent.TargetId, StringComparison.Ordinal))
                    return null;
            }
        }

        var ancestry = new Dictionary<CommentTargetType, string>();
        foreach (var parent in concreteParents)
        {
            foreach (var (ancestorType, ancestorId) in parent.Ancestry)
            {
                if (ancestry.TryGetValue(ancestorType, out var existingId) &&
                    !string.Equals(existingId, ancestorId, StringComparison.Ordinal))
                    return null;
                ancestry[ancestorType] = ancestorId;
            }
        }
        ancestry[targetType] = targetId;

        return new ResolvedTarget(targetType, targetId, projectId, ancestry);
    }

    private static int HierarchyDepth(CommentTargetType targetType) => targetType switch
    {
        CommentTargetType.PROJECT => 0,
        CommentTargetType.MILESTONE => 1,
        CommentTargetType.EPIC => 2,
        CommentTargetType.SPRINT => 3,
        CommentTargetType.FEATURE => 4,
        CommentTargetType.PBI => 5,
        CommentTargetType.TASK => 6,
        _ => -1
    };

    private sealed record ResolvedTarget(
        CommentTargetType TargetType,
        string TargetId,
        string ProjectId,
        IReadOnlyDictionary<CommentTargetType, string> Ancestry);
}
