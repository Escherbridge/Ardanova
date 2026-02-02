using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using ArdaNova.Domain.Models.Enums;

namespace ArdaNova.Domain.Models.Entities;

[Table("Feature")]
public class Feature
{

    [Key]
    [Required]
    public string id { get; set; } = string.Empty;

    [Required]
    public string sprintId { get; set; } = string.Empty;

    [Required]
    public string title { get; set; } = string.Empty;

    [Column(TypeName = "text")]
    public string? description { get; set; }

    [Required]
    public FeatureStatus status { get; set; }

    [Required]
    public Priority priority { get; set; }

    [Required]
    public int order { get; set; }

    [Required]
    public DateTime createdAt { get; set; }

    [Required]
    public DateTime updatedAt { get; set; }

    public string? assigneeId { get; set; }

    [ForeignKey("sprintId")]
    public virtual Sprint? Sprint { get; set; }

    [ForeignKey("assigneeId")]
    public virtual User? Assignee { get; set; }

    public virtual ICollection<ProductBacklogItem> ProductBacklogItems { get; set; } = new List<ProductBacklogItem>();

}
