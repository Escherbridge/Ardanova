namespace ArdaNova.Application.DTOs;

using ArdaNova.Domain.Models.Enums;

public record AgencyDto
{
    public Guid Id { get; init; }
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
    public Guid OwnerId { get; init; }
}

public record CreateAgencyDto
{
    public required Guid OwnerId { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required string Email { get; init; }
    public string? Website { get; init; }
    public string? Phone { get; init; }
}

public record UpdateAgencyDto
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

public record AgencyMemberDto
{
    public Guid Id { get; init; }
    public Guid AgencyId { get; init; }
    public Guid UserId { get; init; }
    public string Role { get; init; } = null!;
    public DateTime JoinedAt { get; init; }
}

public record CreateAgencyMemberDto
{
    public required Guid AgencyId { get; init; }
    public required Guid UserId { get; init; }
    public required string Role { get; init; }
}

public record UpdateAgencyMemberDto
{
    public string? Role { get; init; }
}

public record ProjectBidDto
{
    public Guid Id { get; init; }
    public Guid ProjectId { get; init; }
    public Guid AgencyId { get; init; }
    public Guid UserId { get; init; }
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
    public required Guid ProjectId { get; init; }
    public required Guid AgencyId { get; init; }
    public required Guid UserId { get; init; }
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

public record AgencyReviewDto
{
    public Guid Id { get; init; }
    public Guid AgencyId { get; init; }
    public Guid? ProjectId { get; init; }
    public Guid UserId { get; init; }
    public int Rating { get; init; }
    public string? Comment { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record CreateAgencyReviewDto
{
    public required Guid AgencyId { get; init; }
    public required Guid UserId { get; init; }
    public required int Rating { get; init; }
    public string? Comment { get; init; }
    public Guid? ProjectId { get; init; }
}

public record UpdateAgencyReviewDto
{
    public int? Rating { get; init; }
    public string? Comment { get; init; }
}
