namespace ArdaNova.Application.Services.Interfaces;

using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Domain.Models.Enums;

public interface INotificationService
{
    Task<Result<NotificationDto>> GetByIdAsync(string id, CancellationToken ct = default);
    Task<Result<IReadOnlyList<NotificationDto>>> GetByUserIdAsync(string userId, CancellationToken ct = default);
    Task<Result<PagedResult<NotificationDto>>> GetByUserIdPagedAsync(string userId, int page, int pageSize, CancellationToken ct = default);
    Task<Result<IReadOnlyList<NotificationDto>>> GetUnreadByUserIdAsync(string userId, CancellationToken ct = default);
    Task<Result<NotificationSummaryDto>> GetSummaryAsync(string userId, CancellationToken ct = default);
    Task<Result<NotificationDto>> CreateAsync(CreateNotificationDto dto, CancellationToken ct = default);
    Task<Result<NotificationDto>> MarkAsReadAsync(string id, CancellationToken ct = default);
    Task<Result<bool>> MarkAllAsReadAsync(string userId, CancellationToken ct = default);
    Task<Result<bool>> DeleteAsync(string id, CancellationToken ct = default);
    Task<Result<bool>> DeleteAllByUserIdAsync(string userId, CancellationToken ct = default);

    /// <summary>Stages "picked up" for the asker of the task's live HELP_WANTED; the caller's save commits it.</summary>
    /// <remarks>Nothing when the task has no ask or the claimer is the asker. See Implementations/AGENTS.md §Come-back notifications.</remarks>
    Task StageAskPickedUpAsync(string taskId, string claimerId, CancellationToken ct = default);

    /// <summary>Stages a reply notification for the post's author, folded into their one unread per post; the caller's save commits it.</summary>
    /// <remarks>Nothing when the replier wrote the post. See Implementations/AGENTS.md §Come-back notifications.</remarks>
    Task StageReplyAsync(string postId, string replierId, string replyText, CancellationToken ct = default);

    /// <summary>Stages deleting every notification about a post, as the database's ON DELETE CASCADE does.</summary>
    Task StageForgetPostAsync(string postId, CancellationToken ct = default);
}
