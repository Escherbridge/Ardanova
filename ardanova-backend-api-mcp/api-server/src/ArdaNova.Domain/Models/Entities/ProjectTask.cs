using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ArdaNova.Domain.Models.Enums;

namespace ArdaNova.Domain.Models.Entities;

[Table("ProjectTask")]
public class ProjectTask
{
    [Key]
    public string id { get; set; }

    [Required]
    public string projectId { get; set; }

    public string? backlogItemId { get; set; }

    [Required]
    public string title { get; set; }

    public string? description { get; set; }

    [Required]
    public Enums.TaskStatus status { get; set; }

    [Required]
    public TaskPriority priority { get; set; }

    public int? estimatedHours { get; set; }

    public int? actualHours { get; set; }

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
    public virtual Project Project { get; set; }

    [ForeignKey("assignedToId")]
    public virtual User AssignedTo { get; set; }

    [ForeignKey("backlogItemId")]
    public virtual BacklogItem BacklogItem { get; set; }

    public virtual ICollection<ProjectTaskDependency> DependsOn { get; set; } = new List<ProjectTaskDependency>();

    public virtual ICollection<ProjectTaskDependency> DependedOnBy { get; set; } = new List<ProjectTaskDependency>();

    public virtual ICollection<SprintItem> SprintItems { get; set; } = new List<SprintItem>();

    public virtual ICollection<TaskCompensation> TaskCompensations { get; set; } = new List<TaskCompensation>();

    public virtual ICollection<TaskSubmission> TaskSubmissions { get; set; } = new List<TaskSubmission>();

    public virtual ICollection<TaskEscrow> TaskEscrows { get; set; } = new List<TaskEscrow>();

}
