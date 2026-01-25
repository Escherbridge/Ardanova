using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using ArdaNova.Domain.Models.Enums;

namespace ArdaNova.Domain.Models.Entities;

[Table("LeaderboardEntry")]
public class LeaderboardEntry
{
    [Key]
    [Required]
    public string id { get; set; } = string.Empty;

    [Required]
    public string leaderboardId { get; set; } = string.Empty;

    [Required]
    public string userId { get; set; } = string.Empty;

    [Required]
    public int rank { get; set; }

    [Required]
    public int score { get; set; }

    public string? metadata { get; set; }

    [ForeignKey("leaderboardId")]
    public virtual Leaderboard? Leaderboard { get; set; }

    [ForeignKey("userId")]
    public virtual User? User { get; set; }

}
