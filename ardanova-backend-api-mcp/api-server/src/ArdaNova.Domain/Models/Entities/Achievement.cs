using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using ArdaNova.Domain.Models.Enums;

namespace ArdaNova.Domain.Models.Entities;

[Index(nameof(name), IsUnique = true)]
[Table("Achievement")]
public class Achievement
{
    [Key]
    [Required]
    public string id { get; set; } = string.Empty;

    [Required]
    public string name { get; set; } = string.Empty;

    [Required]
    public string description { get; set; } = string.Empty;

    [Required]
    public AchievementCategory category { get; set; }

    [Required]
    public string criteria { get; set; } = string.Empty;

    [Required]
    public int xpReward { get; set; }

    [Precision(18, 8)]
    public decimal? equityReward { get; set; }

    [Required]
    public AchievementRarity rarity { get; set; }

    public string? icon { get; set; }

    [Required]
    public bool isActive { get; set; }

    [Required]
    public DateTime createdAt { get; set; }

    public virtual ICollection<UserAchievement> UserAchievements { get; set; } = new List<UserAchievement>();

}
