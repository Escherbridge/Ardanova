using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using ArdaNova.Domain.Models.Enums;

namespace ArdaNova.Domain.Models.Entities;

[Table("Activity")]
public class Activity
{

    [Key]
    [Required]
    public string id { get; set; } = string.Empty;

    [Required]
    public string userId { get; set; } = string.Empty;

    public string? projectId { get; set; }

    [Required]
    public ActivityType type { get; set; }

    [Required]
    public string entityType { get; set; } = string.Empty;

    [Required]
    public string entityId { get; set; } = string.Empty;

    [Required]
    public string action { get; set; } = string.Empty;

    [Column(TypeName = "jsonb")]
    public string? metadata { get; set; }

    [Required]
    public DateTime createdAt { get; set; }

    [ForeignKey("userId")]
    public virtual User? User { get; set; }

    [ForeignKey("projectId")]
    public virtual Project? Project { get; set; }

}
