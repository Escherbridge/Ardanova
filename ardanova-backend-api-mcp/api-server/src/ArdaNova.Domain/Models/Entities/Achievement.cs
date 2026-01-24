using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ArdaNova.Domain.Models.Enums;

namespace ArdaNova.Domain.Models.Entities;

[Table("Achievement")]
public class Achievement
{
    [Key]
    public string id { get; set; }

    [Required]
    public string name { get; set; }

    [Required]
    public string description { get; set; }

    [Required]
    public AchievementCategory category { get; set; }

    [Required]
    public string criteria { get; set; }

    [Required]
    public int xpReward { get; set; }

    public decimal? tokenReward { get; set; }

    [Required]
    public AchievementRarity rarity { get; set; }

    public string? icon { get; set; }

    [Required]
    public bool isActive { get; set; }

    [Required]
    public DateTime createdAt { get; set; }

    public virtual ICollection<UserAchievement> UserAchievements { get; set; } = new List<UserAchievement>();

}
