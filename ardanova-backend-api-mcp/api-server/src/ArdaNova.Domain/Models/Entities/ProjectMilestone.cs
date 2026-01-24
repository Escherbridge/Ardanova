using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ArdaNova.Domain.Models.Enums;

namespace ArdaNova.Domain.Models.Entities;

[Table("ProjectMilestone")]
public class ProjectMilestone
{
    [Key]
    public string id { get; set; }

    [Required]
    public string projectId { get; set; }

    [Required]
    public string title { get; set; }

    public string? description { get; set; }

    [Required]
    public DateTime targetDate { get; set; }

    public DateTime? completedAt { get; set; }

    [Required]
    public bool isCompleted { get; set; }

    [Required]
    public DateTime createdAt { get; set; }

    [ForeignKey("projectId")]
    public virtual Project Project { get; set; }

}
