using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using ArdaNova.Domain.Models.Enums;

namespace ArdaNova.Domain.Models.Entities;

[Table("TaxonomyTerm")]
public class TaxonomyTerm
{

    [Key]
    [Required]
    public string id { get; set; } = string.Empty;

    [Required]
    public TaxonomyKind kind { get; set; }

    [Required]
    public string slug { get; set; } = string.Empty;

    [Required]
    public string label { get; set; } = string.Empty;

    [Column(TypeName = "text")]
    public string? description { get; set; }

    public string? parentId { get; set; }

    [Required]
    public int sortOrder { get; set; }

    [Required]
    public bool isActive { get; set; }

    [Required]
    public DateTime createdAt { get; set; }

    [Required]
    public DateTime updatedAt { get; set; }

    [ForeignKey("parentId")]
    public virtual TaxonomyTerm? Parent { get; set; }

}
