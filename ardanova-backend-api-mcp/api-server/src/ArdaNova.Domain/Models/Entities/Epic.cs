using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ArdaNova.Domain.Models.Enums;

namespace ArdaNova.Domain.Models.Entities;

[Table("Epic")]
public class Epic
{
    [Key]
    [Required]
    public string id { get; set; } = string.Empty;

    [Required]
    public string phaseId { get; set; } = string.Empty;

    [Required]
    public string title { get; set; } = string.Empty;

    public string? description { get; set; }

    [Required]
    public EpicStatus status { get; set; }

    [Required]
    public Priority priority { get; set; }

    public decimal? tokenBudget { get; set; }

    [Required]
    public int progress { get; set; }

    public DateTime? startDate { get; set; }

    public DateTime? targetDate { get; set; }

    [Required]
    public DateTime createdAt { get; set; }

    [Required]
    public DateTime updatedAt { get; set; }

    [ForeignKey("phaseId")]
    public virtual RoadmapPhase? Phase { get; set; }

    public virtual ICollection<ProductBacklogItem> ProductBacklogItems { get; set; } = new List<ProductBacklogItem>();

}
