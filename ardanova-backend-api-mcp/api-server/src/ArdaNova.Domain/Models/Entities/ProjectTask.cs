using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using ArdaNova.Domain.Models.Enums;

namespace ArdaNova.Domain.Models.Entities;

[Table("ProjectTask")]
public class ProjectTask
{
    [Key]
    [Required]
    public string id { get; set; } = string.Empty;

    [Required]
    public string projectId { get; set; } = string.Empty;

    public string? backlogItemId { get; set; }

    [Required]
    public string title { get; set; } = string.Empty;

    [Column(TypeName = "text")]
    public string? description { get; set; }

    [Required]
    public ArdaNova.Domain.Models.Enums.TaskStatus status { get; set; }

    [Required]
    public TaskPriority priority { get; set; }

    [Required]
    public TaskType taskType { get; set; }

    public int? estimatedHours { get; set; }

    public int? actualHours { get; set; }

    [Precision(18, 8)]
    public decimal? tokenReward { get; set; }

    [Required]
    public EscrowStatus escrowStatus { get; set; }

    public DateTime? dueDate { get; set; }

    public DateTime? completedAt { get; set; }

    [Required]
    public DateTime createdAt { get; set; }

    [Required]
    public DateTime updatedAt { get; set; }

    public string? assignedToId { get; set; }

    [ForeignKey("projectId")]
    public virtual Project? Project { get; set; }

    [ForeignKey("assignedToId")]
    public virtual User? AssignedTo { get; set; }

    [ForeignKey("backlogItemId")]
    public virtual BacklogItem? BacklogItem { get; set; }

    public virtual ICollection<SprintItem> SprintItems { get; set; } = new List<SprintItem>();

    public virtual ICollection<TaskCompensation> TaskCompensations { get; set; } = new List<TaskCompensation>();

    public virtual ICollection<TaskSubmission> TaskSubmissions { get; set; } = new List<TaskSubmission>();

    public virtual ICollection<TaskBid> TaskBids { get; set; } = new List<TaskBid>();

    public virtual ICollection<TaskEscrow> TaskEscrows { get; set; } = new List<TaskEscrow>();

    public virtual ICollection<Opportunity> Opportunities { get; set; } = new List<Opportunity>();

    public virtual ICollection<ProjectTaskDependency> ProjectTaskDependenciesAsTask { get; set; } = new List<ProjectTaskDependency>();

    public virtual ICollection<ProjectTaskDependency> ProjectTaskDependenciesAsDependsOn { get; set; } = new List<ProjectTaskDependency>();

}
