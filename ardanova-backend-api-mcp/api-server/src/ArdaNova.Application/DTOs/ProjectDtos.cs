namespace ArdaNova.Application.DTOs;

using ArdaNova.Domain.Models.Enums;

public record ProjectDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = null!;
    public string Slug { get; init; } = null!;
    public string Description { get; init; } = null!;
    public string ProblemStatement { get; init; } = null!;
    public string Solution { get; init; } = null!;
    public ProjectCategory Category { get; init; }
    public ProjectStatus Status { get; init; }
    public decimal? FundingGoal { get; init; }
    public decimal CurrentFunding { get; init; }
    public int SupportersCount { get; init; }
    public int VotesCount { get; init; }
    public int ViewsCount { get; init; }
    public bool Featured { get; init; }
    public string? Tags { get; init; }
    public string? Images { get; init; }
    public string? Videos { get; init; }
    public string? Documents { get; init; }
    public string? TargetAudience { get; init; }
    public string? ExpectedImpact { get; init; }
    public string? Timeline { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public DateTime? PublishedAt { get; init; }
    public DateTime? FundedAt { get; init; }
    public DateTime? CompletedAt { get; init; }
    public Guid CreatedById { get; init; }
    public Guid? AssignedAgencyId { get; init; }
}

public record CreateProjectDto
{
    public required Guid CreatedById { get; init; }
    public required string Title { get; init; }
    public required string Description { get; init; }
    public required string ProblemStatement { get; init; }
    public required string Solution { get; init; }
    public required ProjectCategory Category { get; init; }
    public decimal? FundingGoal { get; init; }
    public string? Tags { get; init; }
    public string? Images { get; init; }
    public string? Videos { get; init; }
    public string? Documents { get; init; }
    public string? TargetAudience { get; init; }
    public string? ExpectedImpact { get; init; }
    public string? Timeline { get; init; }
}

public record UpdateProjectDto
{
    public string? Title { get; init; }
    public string? Description { get; init; }
    public string? ProblemStatement { get; init; }
    public string? Solution { get; init; }
    public ProjectCategory? Category { get; init; }
    public ProjectStatus? Status { get; init; }
    public decimal? FundingGoal { get; init; }
    public string? Tags { get; init; }
    public string? Images { get; init; }
    public string? Videos { get; init; }
    public string? Documents { get; init; }
    public string? TargetAudience { get; init; }
    public string? ExpectedImpact { get; init; }
    public string? Timeline { get; init; }
}

public record ProjectTaskDto
{
    public Guid Id { get; init; }
    public Guid ProjectId { get; init; }
    public string Title { get; init; } = null!;
    public string? Description { get; init; }
    public TaskStatus Status { get; init; }
    public TaskPriority Priority { get; init; }
    public int? EstimatedHours { get; init; }
    public int? ActualHours { get; init; }
    public DateTime? DueDate { get; init; }
    public DateTime? CompletedAt { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public Guid? AssignedToId { get; init; }
}

public record CreateProjectTaskDto
{
    public required Guid ProjectId { get; init; }
    public required string Title { get; init; }
    public string? Description { get; init; }
    public TaskPriority Priority { get; init; } = TaskPriority.MEDIUM;
    public int? EstimatedHours { get; init; }
    public DateTime? DueDate { get; init; }
    public Guid? AssignedToId { get; init; }
}

public record UpdateProjectTaskDto
{
    public string? Title { get; init; }
    public string? Description { get; init; }
    public TaskPriority? Priority { get; init; }
    public int? EstimatedHours { get; init; }
    public DateTime? DueDate { get; init; }
    public Guid? AssignedToId { get; init; }
}

public record ProjectResourceDto
{
    public Guid Id { get; init; }
    public Guid ProjectId { get; init; }
    public string Name { get; init; } = null!;
    public string? Description { get; init; }
    public int Quantity { get; init; }
    public decimal? EstimatedCost { get; init; }
    public bool IsRequired { get; init; }
    public bool IsObtained { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record CreateProjectResourceDto
{
    public required Guid ProjectId { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public int Quantity { get; init; } = 1;
    public decimal? EstimatedCost { get; init; }
    public bool IsRequired { get; init; } = true;
}

public record UpdateProjectResourceDto
{
    public string? Name { get; init; }
    public string? Description { get; init; }
    public int? Quantity { get; init; }
    public decimal? EstimatedCost { get; init; }
    public bool? IsRequired { get; init; }
    public bool? IsObtained { get; init; }
}

public record ProjectMilestoneDto
{
    public Guid Id { get; init; }
    public Guid ProjectId { get; init; }
    public string Title { get; init; } = null!;
    public string? Description { get; init; }
    public DateTime TargetDate { get; init; }
    public DateTime? CompletedAt { get; init; }
    public bool IsCompleted { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record CreateProjectMilestoneDto
{
    public required Guid ProjectId { get; init; }
    public required string Title { get; init; }
    public string? Description { get; init; }
    public required DateTime TargetDate { get; init; }
}

public record UpdateProjectMilestoneDto
{
    public string? Title { get; init; }
    public string? Description { get; init; }
    public DateTime? TargetDate { get; init; }
}

public record ProjectSupportDto
{
    public Guid Id { get; init; }
    public Guid ProjectId { get; init; }
    public Guid UserId { get; init; }
    public SupportType SupportType { get; init; }
    public decimal? MonthlyAmount { get; init; }
    public string? Message { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}

public record CreateProjectSupportDto
{
    public required Guid ProjectId { get; init; }
    public required Guid UserId { get; init; }
    public required SupportType SupportType { get; init; }
    public decimal? MonthlyAmount { get; init; }
    public string? Message { get; init; }
}

public record ProjectApplicationDto
{
    public Guid Id { get; init; }
    public Guid ProjectId { get; init; }
    public Guid UserId { get; init; }
    public string RoleTitle { get; init; } = null!;
    public string Message { get; init; } = null!;
    public string? Skills { get; init; }
    public string? Experience { get; init; }
    public string? Availability { get; init; }
    public ApplicationStatus Status { get; init; }
    public DateTime AppliedAt { get; init; }
    public DateTime? ReviewedAt { get; init; }
    public string? ReviewMessage { get; init; }
}

public record CreateProjectApplicationDto
{
    public required Guid ProjectId { get; init; }
    public required Guid UserId { get; init; }
    public required string RoleTitle { get; init; }
    public required string Message { get; init; }
    public string? Skills { get; init; }
    public string? Experience { get; init; }
    public string? Availability { get; init; }
}

public record ProjectCommentDto
{
    public Guid Id { get; init; }
    public Guid ProjectId { get; init; }
    public Guid UserId { get; init; }
    public string Content { get; init; } = null!;
    public Guid? ParentId { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}

public record CreateProjectCommentDto
{
    public required Guid ProjectId { get; init; }
    public required Guid UserId { get; init; }
    public required string Content { get; init; }
    public Guid? ParentId { get; init; }
}

public record ProjectUpdateDto
{
    public Guid Id { get; init; }
    public Guid ProjectId { get; init; }
    public Guid UserId { get; init; }
    public string Title { get; init; } = null!;
    public string Content { get; init; } = null!;
    public string? Images { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record CreateProjectUpdateDto
{
    public required Guid ProjectId { get; init; }
    public required Guid UserId { get; init; }
    public required string Title { get; init; }
    public required string Content { get; init; }
    public string? Images { get; init; }
}

public record ProjectTaskDependencyDto
{
    public Guid Id { get; init; }
    public Guid TaskId { get; init; }
    public Guid DependsOnId { get; init; }
}

public record CreateProjectTaskDependencyDto
{
    public required Guid TaskId { get; init; }
    public required Guid DependsOnId { get; init; }
}

public record ProjectEquityDto
{
    public Guid Id { get; init; }
    public Guid ProjectId { get; init; }
    public Guid UserId { get; init; }
    public decimal SharePercent { get; init; }
    public decimal InvestmentAmount { get; init; }
    public DateTime GrantedAt { get; init; }
}

public record CreateProjectEquityDto
{
    public required Guid ProjectId { get; init; }
    public required Guid UserId { get; init; }
    public required decimal SharePercent { get; init; }
    public required decimal InvestmentAmount { get; init; }
}

public record UpdateProjectEquityDto
{
    public decimal? SharePercent { get; init; }
    public decimal? InvestmentAmount { get; init; }
}
