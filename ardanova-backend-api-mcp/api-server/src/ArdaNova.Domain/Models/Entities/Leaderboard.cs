using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ArdaNova.Domain.Models.Enums;

namespace ArdaNova.Domain.Models.Entities;

[Table("Leaderboard")]
public class Leaderboard
{
    [Key]
    [Required]
    public string id { get; set; } = string.Empty;

    [Required]
    public LeaderboardPeriod period { get; set; }

    [Required]
    public LeaderboardCategory category { get; set; }

    [Required]
    public DateTime startDate { get; set; }

    [Required]
    public DateTime endDate { get; set; }

    [Required]
    public DateTime createdAt { get; set; }

    public virtual ICollection<LeaderboardEntry> LeaderboardEntries { get; set; } = new List<LeaderboardEntry>();

}
