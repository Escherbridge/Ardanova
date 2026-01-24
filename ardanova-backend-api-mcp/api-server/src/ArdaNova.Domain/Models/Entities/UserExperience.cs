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
    public string id { get; set; }

    [Required]
    public string userId { get; set; }

    [Required]
    public string title { get; set; }

    [Required]
    public string company { get; set; }

    public string? description { get; set; }

    [Required]
    public DateTime startDate { get; set; }

    public DateTime? endDate { get; set; }

    [Required]
    public bool isCurrent { get; set; }

    [ForeignKey("userId")]
    public virtual User User { get; set; }

}
