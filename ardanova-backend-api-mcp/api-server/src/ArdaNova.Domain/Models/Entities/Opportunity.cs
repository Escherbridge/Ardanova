using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using ArdaNova.Domain.Models.Enums;

namespace ArdaNova.Domain.Models.Entities;

[Index(nameof(slug), IsUnique = true)]
[Table("Opportunity")]
public class Opportunity
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
    public OpportunityType type { get; set; }

    [Required]
    public OpportunityOrigin origin { get; set; }

    [Required]
    public OpportunityStatus status { get; set; }

    [Required]
    public ExperienceLevel experienceLevel { get; set; }

    [Column(TypeName = "text")]
    public string? requirements { get; set; }

    [Column(TypeName = "text")]
    public string? skills { get; set; }

    [Column(TypeName = "text")]
    public string? benefits { get; set; }

    public string? location { get; set; }

    [Required]
    public bool isRemote { get; set; }

    [Precision(18, 8)]
    public decimal? compensation { get; set; }

    public string? compensationDetails { get; set; }

    public DateTime? deadline { get; set; }

    public int? maxApplications { get; set; }

    [Required]
    public int applicationsCount { get; set; }

    [Required]
    public int bidsCount { get; set; }

    public string? coverImage { get; set; }

    [Required]
    public DateTime createdAt { get; set; }

    [Required]
    public DateTime updatedAt { get; set; }

    public DateTime? closedAt { get; set; }

    [Required]
    public string posterId { get; set; } = string.Empty;

    public string? guildId { get; set; }

    public string? projectId { get; set; }

    public string? taskId { get; set; }

    public virtual ICollection<ProjectTask> ProjectTasks { get; set; } = new List<ProjectTask>();

    [ForeignKey("posterId")]
    public virtual User? Poster { get; set; }

    [ForeignKey("guildId")]
    public virtual Guild? Guild { get; set; }

    [ForeignKey("projectId")]
    public virtual Project? Project { get; set; }

    [ForeignKey("taskId")]
    public virtual ProjectTask? Task { get; set; }

    public virtual ICollection<OpportunityApplication> OpportunityApplications { get; set; } = new List<OpportunityApplication>();

    public virtual ICollection<OpportunityUpdate> OpportunityUpdates { get; set; } = new List<OpportunityUpdate>();

    public virtual ICollection<OpportunityComment> OpportunityComments { get; set; } = new List<OpportunityComment>();

    public virtual ICollection<OpportunityBid> OpportunityBids { get; set; } = new List<OpportunityBid>();

}
