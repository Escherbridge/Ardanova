using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ArdaNova.Domain.Models.Enums;

namespace ArdaNova.Domain.Models.Entities;

[Table("Roadmap")]
public class Roadmap
{
    [Key]
    public string id { get; set; }

    [Required]
    public string projectId { get; set; }

    [Required]
    public string title { get; set; }

    public string? vision { get; set; }

    [Required]
    public RoadmapStatus status { get; set; }

    [Required]
    public DateTime createdAt { get; set; }

    [Required]
    public DateTime updatedAt { get; set; }

    [ForeignKey("projectId")]
    public virtual Project Project { get; set; }

    public virtual ICollection<RoadmapPhase> RoadmapPhases { get; set; } = new List<RoadmapPhase>();

}
