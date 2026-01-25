using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ArdaNova.Domain.Models.Enums;

namespace ArdaNova.Domain.Models.Entities;

[Table("ProjectResource")]
public class ProjectResource
{
    [Key]
    [Required]
    public string id { get; set; } = string.Empty;

    [Required]
    public string projectId { get; set; } = string.Empty;

    [Required]
    public string name { get; set; } = string.Empty;

    public string? description { get; set; }

    [Required]
    public int quantity { get; set; }

    public decimal? estimatedCost { get; set; }

    [Required]
    public bool isRequired { get; set; }

    [Required]
    public bool isObtained { get; set; }

    [Required]
    public DateTime createdAt { get; set; }

    [ForeignKey("projectId")]
    public virtual Project? Project { get; set; }

}
