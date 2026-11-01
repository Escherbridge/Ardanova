// Generated from database-architecture.dbml by generate:csharp - do not edit; hand-written members go in ProjectTaskDependency.Custom.cs
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using ArdaNova.Domain.Models.Enums;

namespace ArdaNova.Domain.Models.Entities;

[Table("ProjectTaskDependency")]
public partial class ProjectTaskDependency
{

    [Key]
    [Required]
    public string id { get; set; } = string.Empty;

    [Required]
    public string taskId { get; set; } = string.Empty;

    [Required]
    public string dependsOnId { get; set; } = string.Empty;

    [ForeignKey("taskId")]
    [InverseProperty("ProjectTaskDependenciesAsTask")]
    public virtual ProjectTask? Task { get; set; }

    [ForeignKey("dependsOnId")]
    [InverseProperty("ProjectTaskDependenciesAsDependsOn")]
    public virtual ProjectTask? DependsOn { get; set; }

}
