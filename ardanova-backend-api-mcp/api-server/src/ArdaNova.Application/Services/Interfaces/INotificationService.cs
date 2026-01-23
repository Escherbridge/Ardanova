namespace ArdaNova.Application.Services.Interfaces;

using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Domain.Models.Enums;

public interface INotificationService
{
    Task<Result<NotificationDto>> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Result<IReadOnlyList<NotificationDto>>> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<Result<PagedResult<NotificationDto>>> GetByUserIdPagedAsync(Guid userId, int page, int pageSize, CancellationToken ct = default);
    Task<Result<IReadOnlyList<NotificationDto>>> GetUnreadByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<Result<NotificationSummaryDto>> GetSummaryAsync(Guid userId, CancellationToken ct = default);
    Task<Result<NotificationDto>> CreateAsync(CreateNotificationDto dto, CancellationToken ct = default);
    Task<Result<NotificationDto>> MarkAsReadAsync(Guid id, CancellationToken ct = default);
    Task<Result<bool>> MarkAllAsReadAsync(Guid userId, CancellationToken ct = default);
    Task<Result<bool>> DeleteAsync(Guid id, CancellationToken ct = default);
    Task<Result<bool>> DeleteAllByUserIdAsync(Guid userId, CancellationToken ct = default);
}
