using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using ArdaNova.Domain.Models.Enums;

namespace ArdaNova.Domain.Models.Entities;

[Index(nameof(slug), IsUnique = true)]
[Table("Project")]
public class Project
{
    [Key]
    [Required]
    public string id { get; set; } = string.Empty;

    [Required]
    public string title { get; set; } = string.Empty;

    [Required]
    public string slug { get; set; } = string.Empty;

    [Required]
    [Column(TypeName = "text")]
    public string description { get; set; } = string.Empty;

    [Required]
    [Column(TypeName = "text")]
    public string problemStatement { get; set; } = string.Empty;

    [Required]
    [Column(TypeName = "text")]
    public string solution { get; set; } = string.Empty;

    [Required]
    [Column(TypeName = "text")]
    public string categories { get; set; } = string.Empty;

    [Required]
    public ProjectType projectType { get; set; }

    public ProjectDuration? duration { get; set; }

    [Required]
    public ProjectStatus status { get; set; }

    [Precision(18, 8)]
    public decimal? fundingGoal { get; set; }

    [Required]
    [Precision(18, 8)]
    public decimal currentFunding { get; set; }

    [Required]
    public int supportersCount { get; set; }

    [Required]
    public int votesCount { get; set; }

    [Required]
    public int viewsCount { get; set; }

    [Required]
    public bool featured { get; set; }

    [Required]
    public bool isTrending { get; set; }

    [Required]
    [Precision(18, 8)]
    public decimal trendingScore { get; set; }

    public int? trendingRank { get; set; }

    public DateTime? trendingAt { get; set; }

    [Column(TypeName = "text")]
    public string? tags { get; set; }

    [Column(TypeName = "text")]
    public string? images { get; set; }

    [Column(TypeName = "text")]
    public string? videos { get; set; }

    [Column(TypeName = "text")]
    public string? documents { get; set; }

    [Column(TypeName = "text")]
    public string? targetAudience { get; set; }

    [Column(TypeName = "text")]
    public string? expectedImpact { get; set; }

    [Column(TypeName = "text")]
    public string? timeline { get; set; }

    [Required]
    public DateTime createdAt { get; set; }

    [Required]
    public DateTime updatedAt { get; set; }

    public DateTime? publishedAt { get; set; }

    public DateTime? fundedAt { get; set; }

    public DateTime? completedAt { get; set; }

    [Required]
    public bool commerceEnabled { get; set; }

    [Column(TypeName = "text")]
    public string? storefrontDescription { get; set; }

    [Required]
    public string createdById { get; set; } = string.Empty;

    public string? assignedGuildId { get; set; }

    public virtual ICollection<Activity> Activities { get; set; } = new List<Activity>();

    [ForeignKey("createdById")]
    public virtual User? CreatedBy { get; set; }

    [ForeignKey("assignedGuildId")]
    public virtual Guild? AssignedGuild { get; set; }

    public virtual ICollection<ProjectTask> ProjectTasks { get; set; } = new List<ProjectTask>();

    public virtual ICollection<ProjectResource> ProjectResources { get; set; } = new List<ProjectResource>();

    public virtual ICollection<ProjectMilestone> ProjectMilestones { get; set; } = new List<ProjectMilestone>();

    public virtual ICollection<ProjectSupport> ProjectSupports { get; set; } = new List<ProjectSupport>();

    public virtual ICollection<ProjectApplication> ProjectApplications { get; set; } = new List<ProjectApplication>();

    public virtual ICollection<ProjectInvitation> ProjectInvitations { get; set; } = new List<ProjectInvitation>();

    public virtual ICollection<ProjectMembershipRequest> ProjectMembershipRequests { get; set; } = new List<ProjectMembershipRequest>();

    public virtual ICollection<ProjectComment> ProjectComments { get; set; } = new List<ProjectComment>();

    public virtual ICollection<ProjectUpdate> ProjectUpdates { get; set; } = new List<ProjectUpdate>();

    public virtual ICollection<ProjectMember> ProjectMembers { get; set; } = new List<ProjectMember>();

    public virtual ICollection<Proposal> Proposals { get; set; } = new List<Proposal>();

    public virtual ICollection<DelegatedVote> DelegatedVotes { get; set; } = new List<DelegatedVote>();

    public virtual ICollection<Product> Products { get; set; } = new List<Product>();

    public virtual ICollection<ProjectShare> ProjectShares { get; set; } = new List<ProjectShare>();

    public virtual ICollection<ProjectEquity> ProjectEquities { get; set; } = new List<ProjectEquity>();

    public virtual ICollection<Treasury> Treasuries { get; set; } = new List<Treasury>();

    public virtual ICollection<Post> Posts { get; set; } = new List<Post>();

    public virtual ICollection<PostShare> PostShares { get; set; } = new List<PostShare>();

    public virtual ICollection<Event> Events { get; set; } = new List<Event>();

    public virtual ICollection<ProjectFollow> ProjectFollows { get; set; } = new List<ProjectFollow>();

    public virtual ICollection<Opportunity> Opportunities { get; set; } = new List<Opportunity>();

}
