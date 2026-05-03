using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using ArdaNova.Domain.Models.Enums;

namespace ArdaNova.Domain.Models.Entities;

[Table("Sprint")]
public class Sprint
{

    [Key]
    [Required]
    public string id { get; set; } = string.Empty;

    [Required]
    public string projectId { get; set; } = string.Empty;

    public string? epicId { get; set; }

    public string? milestoneId { get; set; }

    public string? guildId { get; set; }

    [Required]
    public string name { get; set; } = string.Empty;

    [Column(TypeName = "text")]
    public string? goal { get; set; }

    [Required]
    public DateTime startDate { get; set; }

    [Required]
    public DateTime endDate { get; set; }

    [Precision(18, 8)]
    public decimal? equityBudget { get; set; }

    public int? velocity { get; set; }

    [Required]
    public SprintStatus status { get; set; }

    [Required]
    public DateTime createdAt { get; set; }

    [Required]
    public DateTime updatedAt { get; set; }

    public string? assigneeId { get; set; }

    public virtual ICollection<ProjectTask> ProjectTasks { get; set; } = new List<ProjectTask>();

    [ForeignKey("projectId")]
    public virtual Project? Project { get; set; }

    [ForeignKey("epicId")]
    public virtual Epic? Epic { get; set; }

    [ForeignKey("milestoneId")]
    public virtual ProjectMilestone? Milestone { get; set; }

    [ForeignKey("guildId")]
    public virtual Guild? Guild { get; set; }

    [ForeignKey("assigneeId")]
    public virtual User? Assignee { get; set; }

    public virtual ICollection<Feature> Features { get; set; } = new List<Feature>();

    public virtual ICollection<ProductBacklogItem> ProductBacklogItems { get; set; } = new List<ProductBacklogItem>();

}
