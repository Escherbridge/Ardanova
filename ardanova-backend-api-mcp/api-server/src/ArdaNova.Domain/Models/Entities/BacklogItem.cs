using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ArdaNova.Domain.Models.Enums;

namespace ArdaNova.Domain.Models.Entities;

[Table("BacklogItem")]
public class BacklogItem
{
    [Key]
    public string id { get; set; }

    [Required]
    public string pbiId { get; set; }

    [Required]
    public string title { get; set; }

    public string? description { get; set; }

    [Required]
    public BacklogItemType type { get; set; }

    [Required]
    public BacklogStatus status { get; set; }

    public int? estimate { get; set; }

    [Required]
    public DateTime createdAt { get; set; }

    [Required]
    public DateTime updatedAt { get; set; }

    public virtual ICollection<ProjectTask> ProjectTasks { get; set; } = new List<ProjectTask>();

    [ForeignKey("pbiId")]
    public virtual ProductBacklogItem Pbi { get; set; }

}
