using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ArdaNova.Domain.Models.Enums;

namespace ArdaNova.Domain.Models.Entities;

[Table("Activity")]
public class Activity
{
    [Key]
    public string id { get; set; }

    [Required]
    public string userId { get; set; }

    public string? projectId { get; set; }

    [Required]
    public ActivityType type { get; set; }

    [Required]
    public string entityType { get; set; }

    [Required]
    public string entityId { get; set; }

    [Required]
    public string action { get; set; }

    public string? metadata { get; set; }

    [Required]
    public DateTime createdAt { get; set; }

    [ForeignKey("userId")]
    public virtual User User { get; set; }

    [ForeignKey("projectId")]
    public virtual Project Project { get; set; }

}
