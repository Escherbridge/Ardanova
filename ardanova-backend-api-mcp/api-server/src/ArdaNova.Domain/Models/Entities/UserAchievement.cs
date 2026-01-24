using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ArdaNova.Domain.Models.Enums;

namespace ArdaNova.Domain.Models.Entities;

[Table("UserAchievement")]
public class UserAchievement
{
    [Key]
    public string id { get; set; }

    [Required]
    public string userId { get; set; }

    [Required]
    public string achievementId { get; set; }

    [Required]
    public int progress { get; set; }

    public DateTime? earnedAt { get; set; }

    [ForeignKey("userId")]
    public virtual User User { get; set; }

    [ForeignKey("achievementId")]
    public virtual Achievement Achievement { get; set; }

}
