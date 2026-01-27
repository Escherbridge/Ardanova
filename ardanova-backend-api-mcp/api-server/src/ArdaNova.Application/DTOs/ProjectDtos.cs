namespace ArdaNova.Application.DTOs;

using ArdaNova.Domain.Models.Enums;

public record ProjectCreatorDto
{
    public string Id { get; init; } = null!;
    public string? Name { get; init; }
    public string? Image { get; init; }
}

public record ProjectDto
{
    public string Id { get; init; } = null!;
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
    public string CreatedById { get; init; } = null!;
    public ProjectCreatorDto? CreatedBy { get; init; }
    public string? AssignedGuildId { get; init; }
}

public record CreateProjectDto
{
    public required string CreatedById { get; init; }
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
    public string Id { get; init; } = null!;
    public string ProjectId { get; init; } = null!;
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
    public string? AssignedToId { get; init; }
}

public record CreateProjectTaskDto
{
    public required string ProjectId { get; init; }
    public required string Title { get; init; }
    public string? Description { get; init; }
    public TaskPriority Priority { get; init; } = TaskPriority.MEDIUM;
    public int? EstimatedHours { get; init; }
    public DateTime? DueDate { get; init; }
    public string? AssignedToId { get; init; }
}

public record UpdateProjectTaskDto
{
    public string? Title { get; init; }
    public string? Description { get; init; }
    public TaskPriority? Priority { get; init; }
    public int? EstimatedHours { get; init; }
    public DateTime? DueDate { get; init; }
    public string? AssignedToId { get; init; }
}

public record ProjectResourceDto
{
    public string Id { get; init; } = null!;
    public string ProjectId { get; init; } = null!;
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
    public required string ProjectId { get; init; }
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
    public string Id { get; init; } = null!;
    public string ProjectId { get; init; } = null!;
    public string Title { get; init; } = null!;
    public string? Description { get; init; }
    public DateTime TargetDate { get; init; }
    public DateTime? CompletedAt { get; init; }
    public bool IsCompleted { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record CreateProjectMilestoneDto
{
    public required string ProjectId { get; init; }
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
    public string Id { get; init; } = null!;
    public string ProjectId { get; init; } = null!;
    public string UserId { get; init; } = null!;
    public SupportType SupportType { get; init; }
    public decimal? MonthlyAmount { get; init; }
    public string? Message { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}

public record CreateProjectSupportDto
{
    public required string ProjectId { get; init; }
    public required string UserId { get; init; }
    public required SupportType SupportType { get; init; }
    public decimal? MonthlyAmount { get; init; }
    public string? Message { get; init; }
}

public record ProjectApplicationDto
{
    public string Id { get; init; } = null!;
    public string ProjectId { get; init; } = null!;
    public string UserId { get; init; } = null!;
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
    public required string ProjectId { get; init; }
    public required string UserId { get; init; }
    public required string RoleTitle { get; init; }
    public required string Message { get; init; }
    public string? Skills { get; init; }
    public string? Experience { get; init; }
    public string? Availability { get; init; }
}

public record ProjectCommentDto
{
    public string Id { get; init; } = null!;
    public string ProjectId { get; init; } = null!;
    public string UserId { get; init; } = null!;
    public string Content { get; init; } = null!;
    public string? ParentId { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}

public record CreateProjectCommentDto
{
    public required string ProjectId { get; init; }
    public required string UserId { get; init; }
    public required string Content { get; init; }
    public string? ParentId { get; init; }
}

public record ProjectUpdateDto
{
    public string Id { get; init; } = null!;
    public string ProjectId { get; init; } = null!;
    public string UserId { get; init; } = null!;
    public string Title { get; init; } = null!;
    public string Content { get; init; } = null!;
    public string? Images { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record CreateProjectUpdateDto
{
    public required string ProjectId { get; init; }
    public required string UserId { get; init; }
    public required string Title { get; init; }
    public required string Content { get; init; }
    public string? Images { get; init; }
}

public record ProjectTaskDependencyDto
{
    public string Id { get; init; } = null!;
    public string TaskId { get; init; } = null!;
    public string DependsOnId { get; init; } = null!;
}

public record CreateProjectTaskDependencyDto
{
    public required string TaskId { get; init; }
    public required string DependsOnId { get; init; }
}

public record ProjectEquityDto
{
    public string Id { get; init; } = null!;
    public string ProjectId { get; init; } = null!;
    public string UserId { get; init; } = null!;
    public decimal SharePercent { get; init; }
    public decimal InvestmentAmount { get; init; }
    public DateTime GrantedAt { get; init; }
}

public record CreateProjectEquityDto
{
    public required string ProjectId { get; init; }
    public required string UserId { get; init; }
    public required decimal SharePercent { get; init; }
    public required decimal InvestmentAmount { get; init; }
}

public record UpdateProjectEquityDto
{
    public decimal? SharePercent { get; init; }
    public decimal? InvestmentAmount { get; init; }
}

public record ProjectMemberDto
{
    public string Id { get; init; } = null!;
    public string ProjectId { get; init; } = null!;
    public string UserId { get; init; } = null!;
    public ProjectRole Role { get; init; }
    public decimal TokenBalance { get; init; }
    public decimal VotingPower { get; init; }
    public DateTime JoinedAt { get; init; }
    public string? InvitedById { get; init; }
    public ProjectMemberUserDto? User { get; init; }
}

public record ProjectMemberUserDto
{
    public string Id { get; init; } = null!;
    public string? Name { get; init; }
    public string? Email { get; init; }
    public string? Image { get; init; }
}

public record CreateProjectMemberDto
{
    public required string ProjectId { get; init; }
    public required string UserId { get; init; }
    public required ProjectRole Role { get; init; }
    public string? InvitedById { get; init; }
}

public record UpdateProjectMemberDto
{
    public ProjectRole? Role { get; init; }
    public decimal? TokenBalance { get; init; }
    public decimal? VotingPower { get; init; }
}

public record ReviewProjectApplicationDto
{
    public required ApplicationStatus Status { get; init; }
    public string? ReviewMessage { get; init; }
}

public record ReviewProjectBidDto
{
    public required BidStatus Status { get; init; }
}
