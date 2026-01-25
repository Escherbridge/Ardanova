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
}
