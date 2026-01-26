namespace ArdaNova.Application.Services.Implementations;

using ArdaNova.Application.Common.Interfaces;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Interfaces;
using ArdaNova.Domain.Models.Entities;
using ArdaNova.Domain.Models.Enums;
using AutoMapper;

public class EventService : IEventService
{
    private readonly IRepository<Event> _repository;
    private readonly IRepository<EventAttendee> _attendeeRepository;
    private readonly IRepository<User> _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public EventService(
        IRepository<Event> repository,
        IRepository<EventAttendee> attendeeRepository,
        IRepository<User> userRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper)
    {
        _repository = repository;
        _attendeeRepository = attendeeRepository;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    private static string GenerateSlug(string title)
    {
        return title.ToLowerInvariant()
            .Replace(" ", "-")
            .Replace("--", "-")
            + "-" + Guid.NewGuid().ToString("N")[..8];
    }

    public async Task<Result<EventDto>> GetByIdAsync(string id, CancellationToken ct = default)
    {
        var evt = await _repository.GetByIdAsync(id, ct);
        if (evt is null)
            return Result<EventDto>.NotFound($"Event with id {id} not found");

        var dto = await EnrichEventDtoAsync(evt, ct);
        return Result<EventDto>.Success(dto);
    }

    public async Task<Result<EventDto>> GetBySlugAsync(string slug, CancellationToken ct = default)
    {
        var evt = await _repository.FindOneAsync(e => e.slug == slug, ct);
        if (evt is null)
            return Result<EventDto>.NotFound($"Event with slug {slug} not found");

        var dto = await EnrichEventDtoAsync(evt, ct);
        return Result<EventDto>.Success(dto);
    }

    public async Task<Result<IReadOnlyList<EventDto>>> GetAllAsync(CancellationToken ct = default)
    {
        var events = await _repository.GetAllAsync(ct);
        var dtos = await EnrichEventDtosAsync(events, ct);
        return Result<IReadOnlyList<EventDto>>.Success(dtos);
    }

    public async Task<Result<PagedResult<EventDto>>> GetPagedAsync(int page, int pageSize, CancellationToken ct = default)
    {
        var result = await _repository.GetPagedAsync(page, pageSize, null, ct);
        var dtos = await EnrichEventDtosAsync(result.Items, ct);
        return Result<PagedResult<EventDto>>.Success(new PagedResult<EventDto>(dtos.ToList(), result.TotalCount, result.Page, result.PageSize));
    }

    public async Task<Result<PagedResult<EventDto>>> SearchAsync(
        string? searchTerm,
        EventType? type,
        EventStatus? status,
        bool? upcoming,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var query = _repository.Query();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.ToLower();
            query = query.Where(e => e.title.ToLower().Contains(term) ||
                (e.description != null && e.description.ToLower().Contains(term)));
        }

        if (type.HasValue)
            query = query.Where(e => e.type == type.Value);

        if (status.HasValue)
            query = query.Where(e => e.status == status.Value);

        if (upcoming == true)
            query = query.Where(e => e.startDate > DateTime.UtcNow);

        query = query.OrderBy(e => e.startDate);

        var totalCount = query.Count();
        var items = query.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        var dtos = await EnrichEventDtosAsync(items, ct);

        return Result<PagedResult<EventDto>>.Success(new PagedResult<EventDto>(dtos.ToList(), totalCount, page, pageSize));
    }

    public async Task<Result<IReadOnlyList<EventDto>>> GetUpcomingAsync(int limit, CancellationToken ct = default)
    {
        var events = _repository.Query()
            .Where(e => e.startDate > DateTime.UtcNow && e.status == EventStatus.SCHEDULED)
            .OrderBy(e => e.startDate)
            .Take(limit)
            .ToList();

        var dtos = await EnrichEventDtosAsync(events, ct);
        return Result<IReadOnlyList<EventDto>>.Success(dtos);
    }

    public async Task<Result<IReadOnlyList<EventDto>>> GetByOrganizerIdAsync(string organizerId, CancellationToken ct = default)
    {
        var events = await _repository.FindAsync(e => e.organizerId == organizerId, ct);
        var dtos = await EnrichEventDtosAsync(events, ct);
        return Result<IReadOnlyList<EventDto>>.Success(dtos);
    }

    public async Task<Result<IReadOnlyList<EventDto>>> GetRegisteredByUserIdAsync(string userId, CancellationToken ct = default)
    {
        var attendees = await _attendeeRepository.FindAsync(a => a.userId == userId, ct);
        var eventIds = attendees.Select(a => a.eventId).ToList();

        var events = await _repository.FindAsync(e => eventIds.Contains(e.id), ct);
        var dtos = await EnrichEventDtosAsync(events, ct);
        return Result<IReadOnlyList<EventDto>>.Success(dtos);
    }

    public async Task<Result<EventDto>> CreateAsync(CreateEventDto dto, CancellationToken ct = default)
    {
        var evt = new Event
        {
            id = Guid.NewGuid().ToString(),
            title = dto.Title,
            slug = GenerateSlug(dto.Title),
            description = dto.Description,
            type = dto.Type,
            visibility = dto.Visibility,
            status = EventStatus.SCHEDULED,
            location = dto.Location,
            locationUrl = dto.LocationUrl,
            isOnline = dto.IsOnline,
            meetingUrl = dto.MeetingUrl,
            timezone = dto.Timezone,
            startDate = dto.StartDate,
            endDate = dto.EndDate,
            maxAttendees = dto.MaxAttendees,
            attendeesCount = 0,
            coverImage = dto.CoverImage,
            organizerId = dto.OrganizerId,
            projectId = dto.ProjectId,
            guildId = dto.GuildId,
            createdAt = DateTime.UtcNow,
            updatedAt = DateTime.UtcNow
        };

        await _repository.AddAsync(evt, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        var resultDto = await EnrichEventDtoAsync(evt, ct);
        return Result<EventDto>.Success(resultDto);
    }

    public async Task<Result<EventDto>> UpdateAsync(string id, UpdateEventDto dto, CancellationToken ct = default)
    {
        var evt = await _repository.GetByIdAsync(id, ct);
        if (evt is null)
            return Result<EventDto>.NotFound($"Event with id {id} not found");

        if (dto.Title is not null) evt.title = dto.Title;
        if (dto.Description is not null) evt.description = dto.Description;
        if (dto.Type.HasValue) evt.type = dto.Type.Value;
        if (dto.Visibility.HasValue) evt.visibility = dto.Visibility.Value;
        if (dto.Status.HasValue) evt.status = dto.Status.Value;
        if (dto.Location is not null) evt.location = dto.Location;
        if (dto.LocationUrl is not null) evt.locationUrl = dto.LocationUrl;
        if (dto.IsOnline.HasValue) evt.isOnline = dto.IsOnline.Value;
        if (dto.MeetingUrl is not null) evt.meetingUrl = dto.MeetingUrl;
        if (dto.Timezone is not null) evt.timezone = dto.Timezone;
        if (dto.StartDate.HasValue) evt.startDate = dto.StartDate.Value;
        if (dto.EndDate.HasValue) evt.endDate = dto.EndDate.Value;
        if (dto.MaxAttendees.HasValue) evt.maxAttendees = dto.MaxAttendees;
        if (dto.CoverImage is not null) evt.coverImage = dto.CoverImage;
        evt.updatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(evt, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        var resultDto = await EnrichEventDtoAsync(evt, ct);
        return Result<EventDto>.Success(resultDto);
    }

    public async Task<Result<bool>> DeleteAsync(string id, CancellationToken ct = default)
    {
        var evt = await _repository.GetByIdAsync(id, ct);
        if (evt is null)
            return Result<bool>.NotFound($"Event with id {id} not found");

        await _repository.DeleteAsync(evt, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<bool>.Success(true);
    }

    public async Task<Result<EventAttendeeDto>> RegisterAsync(string eventId, RegisterEventDto dto, CancellationToken ct = default)
    {
        var evt = await _repository.GetByIdAsync(eventId, ct);
        if (evt is null)
            return Result<EventAttendeeDto>.NotFound($"Event with id {eventId} not found");

        var existingAttendee = await _attendeeRepository.FindOneAsync(
            a => a.eventId == eventId && a.userId == dto.UserId, ct);
        if (existingAttendee is not null)
            return Result<EventAttendeeDto>.ValidationError("User is already registered for this event");

        if (evt.maxAttendees.HasValue && evt.attendeesCount >= evt.maxAttendees.Value)
            return Result<EventAttendeeDto>.ValidationError("Event has reached maximum attendees");

        var attendee = new EventAttendee
        {
            id = Guid.NewGuid().ToString(),
            eventId = eventId,
            userId = dto.UserId,
            status = AttendeeStatus.GOING,
            rsvpAt = DateTime.UtcNow,
            notes = dto.Notes
        };

        await _attendeeRepository.AddAsync(attendee, ct);

        evt.attendeesCount++;
        await _repository.UpdateAsync(evt, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        var attendeeDto = _mapper.Map<EventAttendeeDto>(attendee);
        var user = await _userRepository.GetByIdAsync(dto.UserId, ct);
        if (user is not null)
            attendeeDto = attendeeDto with { User = _mapper.Map<EventAttendeeUserDto>(user) };

        return Result<EventAttendeeDto>.Success(attendeeDto);
    }

    public async Task<Result<bool>> UnregisterAsync(string eventId, string userId, CancellationToken ct = default)
    {
        var evt = await _repository.GetByIdAsync(eventId, ct);
        if (evt is null)
            return Result<bool>.NotFound($"Event with id {eventId} not found");

        var attendee = await _attendeeRepository.FindOneAsync(
            a => a.eventId == eventId && a.userId == userId, ct);
        if (attendee is null)
            return Result<bool>.NotFound("User is not registered for this event");

        await _attendeeRepository.DeleteAsync(attendee, ct);

        evt.attendeesCount = Math.Max(0, evt.attendeesCount - 1);
        await _repository.UpdateAsync(evt, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return Result<bool>.Success(true);
    }

    public async Task<Result<IReadOnlyList<EventAttendeeDto>>> GetAttendeesAsync(string eventId, CancellationToken ct = default)
    {
        var evt = await _repository.GetByIdAsync(eventId, ct);
        if (evt is null)
            return Result<IReadOnlyList<EventAttendeeDto>>.NotFound($"Event with id {eventId} not found");

        var attendees = await _attendeeRepository.FindAsync(a => a.eventId == eventId, ct);
        var dtos = new List<EventAttendeeDto>();

        foreach (var attendee in attendees)
        {
            var dto = _mapper.Map<EventAttendeeDto>(attendee);
            var user = await _userRepository.GetByIdAsync(attendee.userId, ct);
            if (user is not null)
                dto = dto with { User = _mapper.Map<EventAttendeeUserDto>(user) };
            dtos.Add(dto);
        }

        return Result<IReadOnlyList<EventAttendeeDto>>.Success(dtos);
    }

    private async Task<EventDto> EnrichEventDtoAsync(Event evt, CancellationToken ct)
    {
        var dto = _mapper.Map<EventDto>(evt);

        var organizer = await _userRepository.GetByIdAsync(evt.organizerId, ct);
        if (organizer is not null)
            dto = dto with { Organizer = _mapper.Map<EventOrganizerDto>(organizer) };

        return dto;
    }

    private async Task<IReadOnlyList<EventDto>> EnrichEventDtosAsync(IEnumerable<Event> events, CancellationToken ct)
    {
        var dtos = new List<EventDto>();
        foreach (var evt in events)
        {
            dtos.Add(await EnrichEventDtoAsync(evt, ct));
        }
        return dtos;
    }
}
