using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ArdaNova.Domain.Models.Enums;

namespace ArdaNova.Domain.Models.Entities;

[Table("UserExperience")]
public class UserExperience
{
    [Key]
    [Required]
    public string id { get; set; } = string.Empty;

    [Required]
    public string userId { get; set; } = string.Empty;

    [Required]
    public string title { get; set; } = string.Empty;

    [Required]
    public string company { get; set; } = string.Empty;

    public string? description { get; set; }

    [Required]
    public DateTime startDate { get; set; }

    public DateTime? endDate { get; set; }

    [Required]
    public bool isCurrent { get; set; }

    [ForeignKey("userId")]
    public virtual User? User { get; set; }

}
