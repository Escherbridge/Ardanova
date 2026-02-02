using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using ArdaNova.Domain.Models.Enums;

namespace ArdaNova.Domain.Models.Entities;

[Table("ProjectApplication")]
public class ProjectApplication
{

    [Key]
    [Required]
    public string id { get; set; } = string.Empty;

    [Required]
    public string projectId { get; set; } = string.Empty;

    [Required]
    public string userId { get; set; } = string.Empty;

    [Required]
    public string roleTitle { get; set; } = string.Empty;

    [Required]
    [Column(TypeName = "text")]
    public string message { get; set; } = string.Empty;

    [Column(TypeName = "text")]
    public string? skills { get; set; }

    [Column(TypeName = "text")]
    public string? experience { get; set; }

    public string? availability { get; set; }

    [Required]
    public ApplicationStatus status { get; set; }

    [Required]
    public DateTime appliedAt { get; set; }

    public DateTime? reviewedAt { get; set; }

    [Column(TypeName = "text")]
    public string? reviewMessage { get; set; }

    [ForeignKey("projectId")]
    public virtual Project? Project { get; set; }

    [ForeignKey("userId")]
    public virtual User? User { get; set; }

}
