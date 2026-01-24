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
    public string id { get; set; }

    [Required]
    public string taskId { get; set; }

    [Required]
    public string dependsOnId { get; set; }

    [ForeignKey("taskId")]
    public virtual ProjectTask Task { get; set; }

    [ForeignKey("dependsOnId")]
    public virtual ProjectTask DependsOn { get; set; }

}
