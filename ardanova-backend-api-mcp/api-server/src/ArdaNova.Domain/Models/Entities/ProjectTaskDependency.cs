using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ArdaNova.Domain.Models.Enums;

namespace ArdaNova.Domain.Models.Entities;

[Table("ProjectTaskDependency")]
public class ProjectTaskDependency
{
    [Key]
    [Required]
    public string id { get; set; } = string.Empty;

    [Required]
    public string taskId { get; set; } = string.Empty;

    [Required]
    public string dependsOnId { get; set; } = string.Empty;

    [ForeignKey("taskId")]
    public virtual ProjectTask? Task { get; set; }

    [ForeignKey("dependsOnId")]
    public virtual ProjectTask? DependsOn { get; set; }

}
