using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ArdaNova.Domain.Models.Enums;

namespace ArdaNova.Domain.Models.Entities;

[Table("ProjectApplication")]
public class ProjectApplication
{
    [Key]
    public string id { get; set; }

    [Required]
    public string projectId { get; set; }

    [Required]
    public string userId { get; set; }

    [Required]
    public string roleTitle { get; set; }

    [Required]
    public string message { get; set; }

    public string? skills { get; set; }

    public string? experience { get; set; }

    public string? availability { get; set; }

    [Required]
    public ApplicationStatus status { get; set; }

    [Required]
    public DateTime appliedAt { get; set; }

    public DateTime? reviewedAt { get; set; }

    public string? reviewMessage { get; set; }

    [ForeignKey("projectId")]
    public virtual Project Project { get; set; }

    [ForeignKey("userId")]
    public virtual User User { get; set; }

}
