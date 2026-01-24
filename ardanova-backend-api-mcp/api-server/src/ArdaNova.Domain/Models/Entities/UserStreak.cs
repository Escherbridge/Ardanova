using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ArdaNova.Domain.Models.Enums;

namespace ArdaNova.Domain.Models.Entities;

[Table("UserStreak")]
public class UserStreak
{
    [Key]
    public string id { get; set; }

    [Required]
    public string userId { get; set; }

    [Required]
    public int currentStreak { get; set; }

    [Required]
    public int longestStreak { get; set; }

    public DateTime? lastActivityDate { get; set; }

    [Required]
    public StreakType streakType { get; set; }

    [Required]
    public DateTime createdAt { get; set; }

    [Required]
    public DateTime updatedAt { get; set; }

    [ForeignKey("userId")]
    public virtual User User { get; set; }

}
