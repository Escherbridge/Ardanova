using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ArdaNova.Domain.Models.Enums;

namespace ArdaNova.Domain.Models.Entities;

[Table("Project")]
public class Project
{
    [Key]
    public string id { get; set; }

    [Required]
    public string title { get; set; }

    [Required]
    public string slug { get; set; }

    [Required]
    public string description { get; set; }

    [Required]
    public string problemStatement { get; set; }

    [Required]
    public string solution { get; set; }

    [Required]
    public ProjectCategory category { get; set; }

    [Required]
    public ProjectStatus status { get; set; }

    public decimal? fundingGoal { get; set; }

    [Required]
    public decimal currentFunding { get; set; }

    [Required]
    public int supportersCount { get; set; }

    [Required]
    public int votesCount { get; set; }

    [Required]
    public int viewsCount { get; set; }

    [Required]
    public bool featured { get; set; }

    public string? tags { get; set; }

    public string? images { get; set; }

    public string? videos { get; set; }

    public string? documents { get; set; }

    public string? targetAudience { get; set; }

    public string? expectedImpact { get; set; }

    public string? timeline { get; set; }

    [Required]
    public DateTime createdAt { get; set; }

    [Required]
    public DateTime updatedAt { get; set; }

    public DateTime? publishedAt { get; set; }

    public DateTime? fundedAt { get; set; }

    public DateTime? completedAt { get; set; }

    [Required]
    public string createdById { get; set; }

    public string? assignedGuildId { get; set; }

    public virtual ICollection<Activity> Activitys { get; set; } = new List<Activity>();

    [ForeignKey("createdById")]
    public virtual User CreatedBy { get; set; }

    [ForeignKey("assignedGuildId")]
    public virtual Guild AssignedGuild { get; set; }

    public virtual ICollection<ProjectTask> ProjectTasks { get; set; } = new List<ProjectTask>();

    public virtual ICollection<ProjectResource> ProjectResources { get; set; } = new List<ProjectResource>();

    public virtual ICollection<ProjectMilestone> ProjectMilestones { get; set; } = new List<ProjectMilestone>();

    public virtual ICollection<ProjectSupport> ProjectSupports { get; set; } = new List<ProjectSupport>();

    public virtual ICollection<ProjectApplication> ProjectApplications { get; set; } = new List<ProjectApplication>();

    public virtual ICollection<ProjectComment> ProjectComments { get; set; } = new List<ProjectComment>();

    public virtual ICollection<ProjectUpdate> ProjectUpdates { get; set; } = new List<ProjectUpdate>();

    public virtual ICollection<Roadmap> Roadmaps { get; set; } = new List<Roadmap>();

    public virtual ICollection<Sprint> Sprints { get; set; } = new List<Sprint>();

    public virtual ICollection<ProjectMember> ProjectMembers { get; set; } = new List<ProjectMember>();

    public virtual ICollection<Proposal> Proposals { get; set; } = new List<Proposal>();

    public virtual ICollection<DelegatedVote> DelegatedVotes { get; set; } = new List<DelegatedVote>();

    public virtual ICollection<ProjectBid> ProjectBids { get; set; } = new List<ProjectBid>();

    public virtual ICollection<ProjectToken> ProjectTokens { get; set; } = new List<ProjectToken>();

    public virtual ICollection<ProjectEquity> ProjectEquitys { get; set; } = new List<ProjectEquity>();

    public virtual ICollection<Treasury> Treasurys { get; set; } = new List<Treasury>();

}
