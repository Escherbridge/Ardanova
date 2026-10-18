namespace ArdaNova.Application.DTOs;

using ArdaNova.Domain.Models.Enums;

public record GuildDto
{
    public string Id { get; init; }
    public string Name { get; init; } = null!;
    public string Slug { get; init; } = null!;
    public string Description { get; init; } = null!;
    public string? Website { get; init; }
    public string Email { get; init; } = null!;
    public string? Phone { get; init; }
    public string? Address { get; init; }
    public string? Logo { get; init; }
    public string? Portfolio { get; init; }
    public string? Specialties { get; init; }
    public bool IsVerified { get; init; }
    public decimal? Rating { get; init; }
    public int ReviewsCount { get; init; }
    public int ProjectsCount { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public string OwnerId { get; init; }
}

/// <summary>The acting identity and whether it administers the platform; always derived from the signed actor assertion, never a request body.</summary>
public sealed record GuildActor(string Id, bool IsPlatformAdmin = false);

/// <summary>A new guild; the owner is the signed actor, so no body field names one.</summary>
public record CreateGuildDto
{
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required string Email { get; init; }
    public string? Website { get; init; }
    public string? Phone { get; init; }
    public string? Address { get; init; }
    public string? Logo { get; init; }
    public string? Portfolio { get; init; }
    public string? Specialties { get; init; }
}

public record UpdateGuildDto
{
    public string? Name { get; init; }
    public string? Description { get; init; }
    public string? Email { get; init; }
    public string? Website { get; init; }
    public string? Phone { get; init; }
    public string? Address { get; init; }
    public string? Logo { get; init; }
    public string? Portfolio { get; init; }
    public string? Specialties { get; init; }
}

public record GuildMemberDto
{
    public string Id { get; init; }
    public string GuildId { get; init; }
    public string UserId { get; init; }
    public GuildMemberRole Role { get; init; }
    public DateTime JoinedAt { get; init; }
    public GuildMemberUserDto? User { get; init; }
}

/// <summary>The public face of a member: no email, which stays private to the member.</summary>
public record GuildMemberUserDto
{
    public string Id { get; init; } = null!;
    public string? Name { get; init; }
    public string? Image { get; init; }
}

/// <summary>Adding an existing user directly; the guild comes from the route.</summary>
public record CreateGuildMemberDto
{
    public required string UserId { get; init; }
    public required GuildMemberRole Role { get; init; }
}

public record UpdateGuildMemberDto
{
    public GuildMemberRole? Role { get; init; }
}

public record GuildReviewDto
{
    public string Id { get; init; }
    public string GuildId { get; init; }
    public string? ProjectId { get; init; }
    public string UserId { get; init; }
    public int Rating { get; init; }
    public string? Comment { get; init; }
    public DateTime CreatedAt { get; init; }
}

/// <summary>A review by the signed actor of the routed guild.</summary>
public record CreateGuildReviewDto
{
    public required int Rating { get; init; }
    public string? Comment { get; init; }
    public string? ProjectId { get; init; }
}

public record UpdateGuildReviewDto
{
    public int? Rating { get; init; }
    public string? Comment { get; init; }
}

public record GuildUpdateDto
{
    public string Id { get; init; }
    public string GuildId { get; init; }
    public string Title { get; init; } = null!;
    public string Content { get; init; } = null!;
    public string? Images { get; init; }
    public DateTime CreatedAt { get; init; }
    public string CreatedById { get; init; }
}

/// <summary>A post to the routed guild by the signed actor.</summary>
public record CreateGuildUpdateDto
{
    public required string Title { get; init; }
    public required string Content { get; init; }
    public string? Images { get; init; }
}

public record GuildApplicationDto
{
    public string Id { get; init; }
    public string GuildId { get; init; }
    public string UserId { get; init; }
    public string RequestedRole { get; init; } = null!;
    public string Message { get; init; } = null!;
    public string? Skills { get; init; }
    public string? Experience { get; init; }
    public string? Portfolio { get; init; }
    public string? Availability { get; init; }
    public string Status { get; init; } = null!;
    public DateTime AppliedAt { get; init; }
    public DateTime? ReviewedAt { get; init; }
    public string? ReviewMessage { get; init; }
    public string? ReviewedById { get; init; }
}

/// <summary>The signed actor applying to the routed guild.</summary>
public record CreateGuildApplicationDto
{
    public required string RequestedRole { get; init; }
    public required string Message { get; init; }
    public string? Skills { get; init; }
    public string? Experience { get; init; }
    public string? Portfolio { get; init; }
    public string? Availability { get; init; }
}

public record ReviewGuildApplicationDto
{
    public string? ReviewMessage { get; init; }
}

public record GuildInvitationDto
{
    public string Id { get; init; }
    public string GuildId { get; init; }
    public string? GuildName { get; init; }
    public string? GuildSlug { get; init; }
    public string InvitedById { get; init; }
    public string? InvitedUserId { get; init; }
    public string? InvitedEmail { get; init; }
    public string Role { get; init; } = null!;
    public string? Message { get; init; }
    public string Status { get; init; } = null!;
    public string? Token { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? ExpiresAt { get; init; }
    public DateTime? RespondedAt { get; init; }
}

/// <summary>An invitation from the signed actor; the invitee is named by user id or by the email on their account.</summary>
public record CreateGuildInvitationDto
{
    public string? InvitedUserId { get; init; }
    public string? InvitedEmail { get; init; }
    public required string Role { get; init; }
    public string? Message { get; init; }
}

public record GuildFollowDto
{
    public string Id { get; init; }
    public string GuildId { get; init; }
    public string UserId { get; init; }
    public bool NotifyUpdates { get; init; }
    public bool NotifyEvents { get; init; }
    public bool NotifyProjects { get; init; }
    public DateTime CreatedAt { get; init; }
}

/// <summary>The signed actor following the routed guild.</summary>
public record CreateGuildFollowDto
{
    public bool NotifyUpdates { get; init; } = true;
    public bool NotifyEvents { get; init; } = true;
    public bool NotifyProjects { get; init; } = true;
}
