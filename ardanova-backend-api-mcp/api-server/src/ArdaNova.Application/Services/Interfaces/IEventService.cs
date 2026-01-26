namespace ArdaNova.Application.Services.Interfaces;

using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Domain.Models.Enums;

public interface IEventService
{
    Task<Result<EventDto>> GetByIdAsync(string id, CancellationToken ct = default);
    Task<Result<EventDto>> GetBySlugAsync(string slug, CancellationToken ct = default);
    Task<Result<IReadOnlyList<EventDto>>> GetAllAsync(CancellationToken ct = default);
    Task<Result<PagedResult<EventDto>>> GetPagedAsync(int page, int pageSize, CancellationToken ct = default);
    Task<Result<PagedResult<EventDto>>> SearchAsync(
        string? searchTerm,
        EventType? type,
        EventStatus? status,
        bool? upcoming,
        int page,
        int pageSize,
        CancellationToken ct = default);
    Task<Result<IReadOnlyList<EventDto>>> GetUpcomingAsync(int limit, CancellationToken ct = default);
    Task<Result<IReadOnlyList<EventDto>>> GetByOrganizerIdAsync(string organizerId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<EventDto>>> GetRegisteredByUserIdAsync(string userId, CancellationToken ct = default);
    Task<Result<EventDto>> CreateAsync(CreateEventDto dto, CancellationToken ct = default);
    Task<Result<EventDto>> UpdateAsync(string id, UpdateEventDto dto, CancellationToken ct = default);
    Task<Result<bool>> DeleteAsync(string id, CancellationToken ct = default);
    Task<Result<EventAttendeeDto>> RegisterAsync(string eventId, RegisterEventDto dto, CancellationToken ct = default);
    Task<Result<bool>> UnregisterAsync(string eventId, string userId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<EventAttendeeDto>>> GetAttendeesAsync(string eventId, CancellationToken ct = default);
}
