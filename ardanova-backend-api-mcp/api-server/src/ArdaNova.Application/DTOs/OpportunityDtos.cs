namespace ArdaNova.Application.DTOs;

using ArdaNova.Domain.Models.Enums;

public record OpportunityDto
{
    public string Id { get; init; } = null!;
    public string Title { get; init; } = null!;
    public string Slug { get; init; } = null!;
    public string Description { get; init; } = null!;
    public OpportunityType Type { get; init; }
    public OpportunityStatus Status { get; init; }
    public ExperienceLevel ExperienceLevel { get; init; }
    public string? Requirements { get; init; }
    public string? Skills { get; init; }
    public string? Benefits { get; init; }
    public string? Location { get; init; }
    public bool IsRemote { get; init; }
    public decimal? Compensation { get; init; }
    public string? CompensationDetails { get; init; }
    public DateTime? Deadline { get; init; }
    public int? MaxApplications { get; init; }
    public int ApplicationsCount { get; init; }
    public string? CoverImage { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public DateTime? ClosedAt { get; init; }
    public string PosterId { get; init; } = null!;
    public string? GuildId { get; init; }
    public string? ProjectId { get; init; }
    public string? TaskId { get; init; }
    public OpportunityOrigin Origin { get; init; }
    public ProjectRole? ProjectRole { get; init; }
    public OpportunityPosterDto? Poster { get; init; }
    public OpportunitySourceDto? Source { get; init; }
    public OpportunityTaskDto? Task { get; init; }
}

public record OpportunityTaskDto
{
    public string Id { get; init; } = null!;
    public string Title { get; init; } = null!;
    public TaskStatus Status { get; init; }
    public TaskPriority Priority { get; init; }
    public TaskType TaskType { get; init; }
    public int? EstimatedHours { get; init; }
    public string? PbiId { get; init; }
}

public record OpportunityPosterDto
{
    public string Id { get; init; } = null!;
    public string? Name { get; init; }
    public string? Image { get; init; }
}

public record OpportunitySourceDto
{
    public string Type { get; init; } = null!; // "guild" | "project"
    public string Id { get; init; } = null!;
    public string Name { get; init; } = null!;
    public string? Logo { get; init; }
    public string Slug { get; init; } = null!;
}

public record CreateOpportunityDto
{
    public required string PosterId { get; init; }
    public required string Title { get; init; }
    public required string Description { get; init; }
    public required OpportunityType Type { get; init; }
    public required ExperienceLevel ExperienceLevel { get; init; }
    public string? Requirements { get; init; }
    public string? Skills { get; init; }
    public string? Benefits { get; init; }
    public string? Location { get; init; }
    public bool IsRemote { get; init; } = true;
    public decimal? Compensation { get; init; }
    public string? CompensationDetails { get; init; }
    public DateTime? Deadline { get; init; }
    public int? MaxApplications { get; init; }
    public string? CoverImage { get; init; }
    public string? GuildId { get; init; }
    public string? ProjectId { get; init; }
    public string? TaskId { get; init; }
    public OpportunityOrigin Origin { get; init; } = OpportunityOrigin.TEAM_POSITION;
    public ProjectRole? ProjectRole { get; init; }
}

public record UpdateOpportunityDto
{
    public string? Title { get; init; }
    public string? Description { get; init; }
    public OpportunityType? Type { get; init; }
    public OpportunityStatus? Status { get; init; }
    public ExperienceLevel? ExperienceLevel { get; init; }
    public string? Requirements { get; init; }
    public string? Skills { get; init; }
    public string? Benefits { get; init; }
    public string? Location { get; init; }
    public bool? IsRemote { get; init; }
    public decimal? Compensation { get; init; }
    public string? CompensationDetails { get; init; }
    public DateTime? Deadline { get; init; }
    public int? MaxApplications { get; init; }
    public string? CoverImage { get; init; }
    public ProjectRole? ProjectRole { get; init; }
}

public record OpportunityApplicationDto
{
    public string Id { get; init; } = null!;
    public string OpportunityId { get; init; } = null!;
    public string ApplicantId { get; init; } = null!;
    public string CoverLetter { get; init; } = null!;
    public string? Portfolio { get; init; }
    public string? AdditionalInfo { get; init; }
    public ApplicationStatus Status { get; init; }
    public string? ReviewNotes { get; init; }
    public DateTime AppliedAt { get; init; }
    public DateTime? ReviewedAt { get; init; }
    public OpportunityApplicationApplicantDto? Applicant { get; init; }
}

public record OpportunityApplicationApplicantDto
{
    public string Id { get; init; } = null!;
    public string? Name { get; init; }
    public string? Image { get; init; }
    public string? Email { get; init; }
}

public record ApplyToOpportunityDto
{
    public required string ApplicantId { get; init; }
    public required string CoverLetter { get; init; }
    public string? Portfolio { get; init; }
    public string? AdditionalInfo { get; init; }
}

public record UpdateApplicationStatusDto
{
    public required ApplicationStatus Status { get; init; }
    public string? ReviewNotes { get; init; }
}

// ===== Opportunity Updates =====

public record OpportunityUpdateDto
{
    public string Id { get; init; } = null!;
    public string OpportunityId { get; init; } = null!;
    public string UserId { get; init; } = null!;
    public string Title { get; init; } = null!;
    public string Content { get; init; } = null!;
    public string? Images { get; init; }
    public DateTime CreatedAt { get; init; }
    public OpportunityUpdateAuthorDto? User { get; init; }
}

public record OpportunityUpdateAuthorDto
{
    public string Id { get; init; } = null!;
    public string? Name { get; init; }
    public string? Image { get; init; }
}

public record CreateOpportunityUpdateDto
{
    public required string OpportunityId { get; init; }
    public required string UserId { get; init; }
    public required string Title { get; init; }
    public required string Content { get; init; }
    public string? Images { get; init; }
}

// ===== Opportunity Comments =====

public record OpportunityCommentDto
{
    public string Id { get; init; } = null!;
    public string OpportunityId { get; init; } = null!;
    public string UserId { get; init; } = null!;
    public string Content { get; init; } = null!;
    public string? ParentId { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public OpportunityCommentAuthorDto? Author { get; init; }
}

public record OpportunityCommentAuthorDto
{
    public string Id { get; init; } = null!;
    public string? Name { get; init; }
    public string? Image { get; init; }
    public string? Email { get; init; }
}

public record CreateOpportunityCommentDto
{
    public required string OpportunityId { get; init; }
    public required string UserId { get; init; }
    public required string Content { get; init; }
    public string? ParentId { get; init; }
}
