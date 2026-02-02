using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using ArdaNova.Domain.Models.Enums;

namespace ArdaNova.Domain.Models.Entities;

[Table("Product")]
public class Product
{
    [Key]
    [Required]
    public string id { get; set; } = string.Empty;

    [Required]
    public string projectId { get; set; } = string.Empty;

    [Required]
    public string name { get; set; } = string.Empty;

    [Column(TypeName = "text")]
    public string? description { get; set; }

    public string? sku { get; set; }

    [Required]
    [Precision(18, 8)]
    public decimal price { get; set; }

    [Precision(18, 8)]
    public decimal? cost { get; set; }

    public string? category { get; set; }

    [Required]
    public bool isActive { get; set; }

    [Required]
    public DateTime createdAt { get; set; }

    [Required]
    public DateTime updatedAt { get; set; }

    [Required]
    public string userId { get; set; } = string.Empty;

    [ForeignKey("projectId")]
    public virtual Project? Project { get; set; }

    [ForeignKey("userId")]
    public virtual User? User { get; set; }

}
