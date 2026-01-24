using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ArdaNova.Domain.Models.Enums;

namespace ArdaNova.Domain.Models.Entities;

[Table("Sprint")]
public class Sprint
{
    [Key]
    public string id { get; set; }

    [Required]
    public string projectId { get; set; }

    [Required]
    public string name { get; set; }

    public string? goal { get; set; }

    [Required]
    public DateTime startDate { get; set; }

    [Required]
    public DateTime endDate { get; set; }

    public decimal? tokenBudget { get; set; }

    public int? velocity { get; set; }

    [Required]
    public SprintStatus status { get; set; }

    [Required]
    public DateTime createdAt { get; set; }

    [Required]
    public DateTime updatedAt { get; set; }

    [ForeignKey("projectId")]
    public virtual Project Project { get; set; }

    public virtual ICollection<SprintItem> SprintItems { get; set; } = new List<SprintItem>();

}
