using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ArdaNova.Domain.Models.Enums;

namespace ArdaNova.Domain.Models.Entities;

[Table("ProductBacklogItem")]
public class ProductBacklogItem
{
    [Key]
    public string id { get; set; }

    [Required]
    public string epicId { get; set; }

    [Required]
    public string title { get; set; }

    public string? description { get; set; }

    [Required]
    public PBIType type { get; set; }

    public int? storyPoints { get; set; }

    [Required]
    public PBIStatus status { get; set; }

    public string? acceptanceCriteria { get; set; }

    [Required]
    public Priority priority { get; set; }

    [Required]
    public DateTime createdAt { get; set; }

    [Required]
    public DateTime updatedAt { get; set; }

    [ForeignKey("epicId")]
    public virtual Epic Epic { get; set; }

    public virtual ICollection<BacklogItem> BacklogItems { get; set; } = new List<BacklogItem>();

}
