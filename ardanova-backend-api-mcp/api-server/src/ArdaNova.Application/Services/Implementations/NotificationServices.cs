namespace ArdaNova.Application.Services.Implementations;

using ArdaNova.Application.Common.Interfaces;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Interfaces;
using ArdaNova.Domain.Models.Entities;
using AutoMapper;

public class NotificationService : INotificationService
{
    private readonly IRepository<Notification> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public NotificationService(IRepository<Notification> repository, IUnitOfWork unitOfWork, IMapper mapper)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
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
}
