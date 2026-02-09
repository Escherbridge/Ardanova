using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using ArdaNova.Domain.Models.Enums;

namespace ArdaNova.Domain.Models.Entities;

[Table("ProductBacklogItem")]
public class ProductBacklogItem
{

    [Key]
    [Required]
    public string id { get; set; } = string.Empty;

    [Required]
    public string featureId { get; set; } = string.Empty;

    [Required]
    public string title { get; set; } = string.Empty;

    [Column(TypeName = "text")]
    public string? description { get; set; }

    [Required]
    public PBIType type { get; set; }

    public int? storyPoints { get; set; }

    [Required]
    public PBIStatus status { get; set; }

    [Column(TypeName = "text")]
    public string? acceptanceCriteria { get; set; }

    [Required]
    public Priority priority { get; set; }

    [Required]
    public DateTime createdAt { get; set; }

    [Required]
    public DateTime updatedAt { get; set; }

    public string? assigneeId { get; set; }

    public virtual ICollection<ProjectTask> ProjectTasks { get; set; } = new List<ProjectTask>();

    [ForeignKey("featureId")]
    public virtual Feature? Feature { get; set; }

    [ForeignKey("assigneeId")]
    public virtual User? Assignee { get; set; }

    public virtual ICollection<TokenAllocation> TokenAllocations { get; set; } = new List<TokenAllocation>();

}
