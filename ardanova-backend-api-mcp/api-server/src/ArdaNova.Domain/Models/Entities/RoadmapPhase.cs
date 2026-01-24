using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ArdaNova.Domain.Models.Enums;

namespace ArdaNova.Domain.Models.Entities;

[Table("RoadmapPhase")]
public class RoadmapPhase
{
    [Key]
    public string id { get; set; }

    [Required]
    public string roadmapId { get; set; }

    [Required]
    public string name { get; set; }

    public string? description { get; set; }

    [Required]
    public int order { get; set; }

    public DateTime? startDate { get; set; }

    public DateTime? endDate { get; set; }

    [Required]
    public PhaseStatus status { get; set; }

    [Required]
    public DateTime createdAt { get; set; }

    [Required]
    public DateTime updatedAt { get; set; }

    [ForeignKey("roadmapId")]
    public virtual Roadmap Roadmap { get; set; }

    public virtual ICollection<Epic> Epics { get; set; } = new List<Epic>();

}
