namespace ArdaNova.Application.Services.Implementations;

using System.Globalization;
using System.Text.Json;
using ArdaNova.Application.Common.Interfaces;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Interfaces;
using ArdaNova.Domain.Models.Entities;
using ArdaNova.Domain.Models.Enums;
using AutoMapper;
using Microsoft.EntityFrameworkCore;

public class NotificationService : INotificationService
{
    /// <summary>Title of the asker's notification when a member picks up their task.</summary>
    public const string AskPickedUpTitle = "Picked up";

    /// <summary>Title of a post author's notification for one unread reply.</summary>
    public const string OneReplyTitle = "New reply";

    /// <summary>How many characters of a reply the notification quotes.</summary>
    public const int ReplyExcerptLength = 80;

    /// <summary>Stands in for a person with no name on file.</summary>
    public const string UnnamedPerson = "Someone";

    private readonly IRepository<Notification> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IRepository<Post> _posts;
    private readonly IRepository<ProjectTask> _tasks;
    private readonly IRepository<User> _users;
    private readonly IRepository<Project> _projects;

    public NotificationService(
        IRepository<Notification> repository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IRepository<Post> posts,
        IRepository<ProjectTask> tasks,
        IRepository<User> users,
        IRepository<Project> projects)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _posts = posts;
        _tasks = tasks;
        _users = users;
        _projects = projects;
    }

    public async Task<Result<NotificationDto>> GetByIdAsync(string id, CancellationToken ct = default)
    {
        var notification = await _repository.GetByIdAsync(id, ct);
        if (notification is null)
            return Result<NotificationDto>.NotFound($"Notification with id {id} not found");
        return Result<NotificationDto>.Success(_mapper.Map<NotificationDto>(notification));
    }

    public async Task<Result<IReadOnlyList<NotificationDto>>> GetByUserIdAsync(string userId, CancellationToken ct = default)
    {
        var notifications = await _repository.FindAsync(n => n.userId == userId, ct);
        var ordered = notifications.OrderByDescending(n => n.createdAt).ToList();
        return Result<IReadOnlyList<NotificationDto>>.Success(_mapper.Map<IReadOnlyList<NotificationDto>>(ordered));
    }

    public async Task<Result<PagedResult<NotificationDto>>> GetByUserIdPagedAsync(string userId, int page, int pageSize, CancellationToken ct = default)
    {
        var result = await _repository.GetPagedAsync(page, pageSize, n => n.userId == userId, ct);
        return Result<PagedResult<NotificationDto>>.Success(result.Map(_mapper.Map<NotificationDto>));
    }

    public async Task<Result<IReadOnlyList<NotificationDto>>> GetUnreadByUserIdAsync(string userId, CancellationToken ct = default)
    {
        var notifications = await _repository.FindAsync(n => n.userId == userId && !n.isRead, ct);
        var ordered = notifications.OrderByDescending(n => n.createdAt).ToList();
        return Result<IReadOnlyList<NotificationDto>>.Success(_mapper.Map<IReadOnlyList<NotificationDto>>(ordered));
    }

    public async Task<Result<NotificationSummaryDto>> GetSummaryAsync(string userId, CancellationToken ct = default)
    {
        var total = await _repository.CountAsync(n => n.userId == userId, ct);
        var unread = await _repository.CountAsync(n => n.userId == userId && !n.isRead, ct);
        return Result<NotificationSummaryDto>.Success(new NotificationSummaryDto
        {
            TotalCount = total,
            UnreadCount = unread
        });
    }

    public async Task<Result<NotificationDto>> CreateAsync(CreateNotificationDto dto, CancellationToken ct = default)
    {
        var notification = new Notification
        {
            id = Guid.NewGuid().ToString(),
            userId = dto.UserId,
            type = dto.Type,
            title = dto.Title,
            message = dto.Message,
            data = dto.Data,
            actionUrl = dto.ActionUrl,
            isRead = false,
            createdAt = DateTime.UtcNow
        };

        await _repository.AddAsync(notification, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<NotificationDto>.Success(_mapper.Map<NotificationDto>(notification));
    }

    public async Task<Result<NotificationDto>> MarkAsReadAsync(string id, CancellationToken ct = default)
    {
        var notification = await _repository.GetByIdAsync(id, ct);
        if (notification is null)
            return Result<NotificationDto>.NotFound($"Notification with id {id} not found");

        notification.isRead = true;
        notification.readAt = DateTime.UtcNow;
        await _repository.UpdateAsync(notification, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<NotificationDto>.Success(_mapper.Map<NotificationDto>(notification));
    }

    public async Task<Result<bool>> MarkAllAsReadAsync(string userId, CancellationToken ct = default)
    {
        var notifications = await _repository.FindAsync(n => n.userId == userId && !n.isRead, ct);
        foreach (var notification in notifications)
        {
            notification.isRead = true;
            notification.readAt = DateTime.UtcNow;
            await _repository.UpdateAsync(notification, ct);
        }
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<bool>.Success(true);
    }

    public async Task<Result<bool>> DeleteAsync(string id, CancellationToken ct = default)
    {
        var notification = await _repository.GetByIdAsync(id, ct);
        if (notification is null)
            return Result<bool>.NotFound($"Notification with id {id} not found");

        await _repository.DeleteAsync(notification, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<bool>.Success(true);
    }

    public async Task<Result<bool>> DeleteAllByUserIdAsync(string userId, CancellationToken ct = default)
    {
        var notifications = await _repository.FindAsync(n => n.userId == userId, ct);
        await _repository.DeleteRangeAsync(notifications, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<bool>.Success(true);
    }

    /// <inheritdoc/>
    public async Task StageAskPickedUpAsync(string taskId, string claimerId, CancellationToken ct = default)
    {
        var ask = (await _posts.FindAsync(post => post.taskId == taskId && post.type == PostType.HELP_WANTED, ct))
            .FirstOrDefault();
        if (ask is null || string.Equals(ask.authorId, claimerId, StringComparison.Ordinal))
            return;
        // The ask lives on the task's project, so this also stops news about a draft the asker has left.
        if (!await RecipientMayReadAsync(ask.id, ask.authorId, ct))
            return;

        var task = await _tasks.GetByIdAsync(taskId, ct);
        if (task is null)
            return;

        var claimer = await _users.GetByIdAsync(claimerId, ct);
        await _repository.AddAsync(new Notification
        {
            id = Guid.NewGuid().ToString(),
            userId = ask.authorId,
            type = NotificationType.TASK_ASSIGNED,
            title = AskPickedUpTitle,
            message = AskPickedUpMessage(NameOf(claimer), task.title),
            data = JsonSerializer.Serialize(new AskPickedUpData(ask.id, taskId, claimerId), DataJson),
            actionUrl = $"/tasks/{taskId}",
            postId = ask.id,
            isRead = false,
            createdAt = DateTime.UtcNow
        }, ct);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// The fold is a conditional write that runs at once, so the caller must hold a transaction for the reply and its
    /// notification to land together (PostService.AddCommentAsync does). See Implementations/AGENTS.md §Come-back notifications.
    /// </remarks>
    public async Task StageReplyAsync(string postId, string replierId, string replyText, CancellationToken ct = default)
    {
        var post = await _posts.GetByIdAsync(postId, ct);
        if (post is null || string.Equals(post.authorId, replierId, StringComparison.Ordinal))
            return;
        // An author removed from a draft can no longer open the post; the bell must not quote it to them.
        if (!await RecipientMayReadAsync(postId, post.authorId, ct))
            return;

        var replier = NameOf(await _users.GetByIdAsync(replierId, ct));
        var message = ReplyMessage(replier, replyText);
        var now = DateTime.UtcNow;
        var unread = await _repository.Query()
            .Where(notification => notification.userId == post.authorId
                && notification.postId == postId
                && notification.type == NotificationType.COMMENT_REPLY
                && !notification.isRead)
            .Select(notification => new { notification.id, notification.data })
            .FirstOrDefaultAsync(ct);

        if (unread is not null && await FoldIntoAsync(unread.id, unread.data, postId, replierId, message, now, ct))
            return;

        await _repository.AddAsync(new Notification
        {
            id = Guid.NewGuid().ToString(),
            userId = post.authorId,
            type = NotificationType.COMMENT_REPLY,
            title = RepliesTitle(1),
            message = message,
            data = JsonSerializer.Serialize(new ReplyData(postId, 1, replierId), DataJson),
            actionUrl = await ProjectLinkAsync(post.projectId, ct),
            postId = postId,
            isRead = false,
            createdAt = now
        }, ct);
    }

    /// <summary>Compare-and-set of the collapse fields only; false when a mark-read or another reply's fold got there first.</summary>
    private async Task<bool> FoldIntoAsync(
        string notificationId,
        string? foldSeen,
        string postId,
        string replierId,
        string message,
        DateTime now,
        CancellationToken ct)
    {
        var count = ReplyCountIn(foldSeen) + 1;
        var title = RepliesTitle(count);
        var data = JsonSerializer.Serialize(new ReplyData(postId, count, replierId), DataJson);
        var folded = await _repository.Query()
            .Where(notification => notification.id == notificationId
                && !notification.isRead
                && notification.data == foldSeen)
            .ExecuteUpdateAsync(set => set
                .SetProperty(notification => notification.title, title)
                .SetProperty(notification => notification.message, message)
                .SetProperty(notification => notification.data, data)
                // The bell orders by createdAt; the folded row rises with its latest reply.
                .SetProperty(notification => notification.createdAt, now), ct);
        return folded == 1;
    }

    /// <summary>The one post read gate (<see cref="PostReadFilters.ReadableBy"/>), asked for the person a notification would reach.</summary>
    private Task<bool> RecipientMayReadAsync(string postId, string recipientId, CancellationToken ct)
        => _posts.Query().Where(post => post.id == postId).ReadableBy(recipientId).AnyAsync(ct);

    /// <inheritdoc/>
    public async Task StageForgetPostAsync(string postId, CancellationToken ct = default)
    {
        var about = await _repository.FindAsync(notification => notification.postId == postId, ct);
        if (about.Count > 0)
            await _repository.DeleteRangeAsync(about, ct);
    }

    /// <summary>"{name} took {task}".</summary>
    public static string AskPickedUpMessage(string claimerName, string taskTitle) => $"{claimerName} took {taskTitle}";

    /// <summary>"New reply" for one, "{n} new replies" once several are folded together.</summary>
    public static string RepliesTitle(int count) => count <= 1 ? OneReplyTitle : $"{count} new replies";

    /// <summary>"{name}: “{first 80 characters}”", whitespace collapsed, cut on a whole character.</summary>
    public static string ReplyMessage(string replierName, string replyText) => $"{replierName}: “{Excerpt(replyText)}”";

    private static readonly JsonSerializerOptions DataJson = new(JsonSerializerDefaults.Web);

    private sealed record AskPickedUpData(string PostId, string TaskId, string ClaimerId);

    private sealed record ReplyData(string PostId, int ReplyCount, string LatestReplierId);

    private static string NameOf(User? person)
        => person?.name is { } name && !string.IsNullOrWhiteSpace(name) ? name.Trim() : UnnamedPerson;

    /// <summary>Text elements, not UTF-16 units, so an emoji or accent is never split in half.</summary>
    private static string Excerpt(string text)
    {
        var collapsed = string.Join(' ', text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        var info = new StringInfo(collapsed);
        return info.LengthInTextElements <= ReplyExcerptLength
            ? collapsed
            : info.SubstringByTextElements(0, ReplyExcerptLength).TrimEnd() + "…";
    }

    private static int ReplyCountIn(string? data)
    {
        if (string.IsNullOrWhiteSpace(data))
            return 1;
        try
        {
            using var document = JsonDocument.Parse(data);
            return document.RootElement.TryGetProperty("replyCount", out var count) && count.TryGetInt32(out var value)
                ? Math.Max(1, value)
                : 1;
        }
        catch (JsonException)
        {
            return 1;
        }
    }

    /// <summary>The project page (its Feed tab is the default) for a project post; null for one with no project.</summary>
    private async Task<string?> ProjectLinkAsync(string? projectId, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(projectId))
            return null;
        var project = await _projects.GetByIdAsync(projectId, ct);
        return project is null ? null : $"/projects/{project.slug}";
    }
}
