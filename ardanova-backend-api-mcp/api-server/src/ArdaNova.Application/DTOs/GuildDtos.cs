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

public record CreateGuildDto
{
    public required string OwnerId { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required string Email { get; init; }
    public string? Website { get; init; }
    public string? Phone { get; init; }
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
    public string Role { get; init; } = null!;
    public DateTime JoinedAt { get; init; }
}

public record CreateGuildMemberDto
{
    public required string GuildId { get; init; }
    public required string UserId { get; init; }
    public required string Role { get; init; }
}

public record UpdateGuildMemberDto
{
    public string? Role { get; init; }
}

public record ProjectBidDto
{
    public string Id { get; init; }
    public string ProjectId { get; init; }
    public string GuildId { get; init; }
    public string UserId { get; init; }
    public string Proposal { get; init; } = null!;
    public string? Timeline { get; init; }
    public decimal Budget { get; init; }
    public string? Deliverables { get; init; }
    public BidStatus Status { get; init; }
    public DateTime SubmittedAt { get; init; }
    public DateTime? ReviewedAt { get; init; }
}

public record CreateProjectBidDto
{
    public required string ProjectId { get; init; }
    public required string GuildId { get; init; }
    public required string UserId { get; init; }
    public required string Proposal { get; init; }
    public required decimal Budget { get; init; }
    public string? Timeline { get; init; }
    public string? Deliverables { get; init; }
}

public record UpdateProjectBidDto
{
    public string? Proposal { get; init; }
    public decimal? Budget { get; init; }
    public string? Timeline { get; init; }
    public string? Deliverables { get; init; }
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

public record CreateGuildReviewDto
{
    public required string GuildId { get; init; }
    public required string UserId { get; init; }
    public required int Rating { get; init; }
    public string? Comment { get; init; }
    public string? ProjectId { get; init; }
}

public record UpdateGuildReviewDto
{
    public int? Rating { get; init; }
    public string? Comment { get; init; }
}
