namespace ArdaNova.Application.DTOs;

using ArdaNova.Domain.Models.Enums;

public record EventDto
{
    public string Id { get; init; } = null!;
    public string Title { get; init; } = null!;
    public string Slug { get; init; } = null!;
    public string? Description { get; init; }
    public EventType Type { get; init; }
    public EventVisibility Visibility { get; init; }
    public EventStatus Status { get; init; }
    public string? Location { get; init; }
    public string? LocationUrl { get; init; }
    public bool IsOnline { get; init; }
    public string? MeetingUrl { get; init; }
    public string Timezone { get; init; } = null!;
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public int? MaxAttendees { get; init; }
    public int AttendeesCount { get; init; }
    public string? CoverImage { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public string OrganizerId { get; init; } = null!;
    public string? ProjectId { get; init; }
    public string? GuildId { get; init; }
    public EventOrganizerDto? Organizer { get; init; }
}

public record EventOrganizerDto
{
    public string Id { get; init; } = null!;
    public string? Name { get; init; }
    public string? Image { get; init; }
}

public record CreateEventDto
{
    public required string OrganizerId { get; init; }
    public required string Title { get; init; }
    public string? Description { get; init; }
    public required EventType Type { get; init; }
    public EventVisibility Visibility { get; init; } = EventVisibility.PUBLIC;
    public string? Location { get; init; }
    public string? LocationUrl { get; init; }
    public bool IsOnline { get; init; }
    public string? MeetingUrl { get; init; }
    public required string Timezone { get; init; }
    public required DateTime StartDate { get; init; }
    public required DateTime EndDate { get; init; }
    public int? MaxAttendees { get; init; }
    public string? CoverImage { get; init; }
    public string? ProjectId { get; init; }
    public string? GuildId { get; init; }
}

public record UpdateEventDto
{
    public string? Title { get; init; }
    public string? Description { get; init; }
    public EventType? Type { get; init; }
    public EventVisibility? Visibility { get; init; }
    public EventStatus? Status { get; init; }
    public string? Location { get; init; }
    public string? LocationUrl { get; init; }
    public bool? IsOnline { get; init; }
    public string? MeetingUrl { get; init; }
    public string? Timezone { get; init; }
    public DateTime? StartDate { get; init; }
    public DateTime? EndDate { get; init; }
    public int? MaxAttendees { get; init; }
    public string? CoverImage { get; init; }
}

public record EventAttendeeDto
{
    public string Id { get; init; } = null!;
    public string EventId { get; init; } = null!;
    public string UserId { get; init; } = null!;
    public AttendeeStatus Status { get; init; }
    public DateTime RsvpAt { get; init; }
    public DateTime? AttendedAt { get; init; }
    public string? Notes { get; init; }
    public EventAttendeeUserDto? User { get; init; }
}

public record EventAttendeeUserDto
{
    public string Id { get; init; } = null!;
    public string? Name { get; init; }
    public string? Image { get; init; }
}

public record RegisterEventDto
{
    public required string UserId { get; init; }
    public string? Notes { get; init; }
}
