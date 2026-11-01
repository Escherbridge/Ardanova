// Generated from database-architecture.dbml by generate:csharp - do not edit; hand-written members go in UserAchievement.Custom.cs
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using ArdaNova.Domain.Models.Enums;

namespace ArdaNova.Domain.Models.Entities;

[Table("UserAchievement")]
public partial class UserAchievement
{

    [Key]
    [Required]
    public string id { get; set; } = string.Empty;

    [Required]
    public string userId { get; set; } = string.Empty;

    [Required]
    public string achievementId { get; set; } = string.Empty;

    [Required]
    public int progress { get; set; }

    public DateTime? earnedAt { get; set; }

    [ForeignKey("userId")]
    public virtual User? User { get; set; }

    [ForeignKey("achievementId")]
    public virtual Achievement? Achievement { get; set; }

}
