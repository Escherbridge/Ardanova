using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using ArdaNova.Domain.Models.Enums;

namespace ArdaNova.Domain.Models.Entities;

[Index(nameof(productId), IsUnique = true)]
[Table("InventoryItem")]
public class InventoryItem
{
    [Key]
    [Required]
    public string id { get; set; } = string.Empty;

    [Required]
    public string shopId { get; set; } = string.Empty;

    [Required]
    public string productId { get; set; } = string.Empty;

    [Required]
    public int currentStock { get; set; }

    [Required]
    public int minStock { get; set; }

    public int? maxStock { get; set; }

    public int? reorderPoint { get; set; }

    public DateTime? lastRestocked { get; set; }

    [Required]
    public DateTime createdAt { get; set; }

    [Required]
    public DateTime updatedAt { get; set; }

    [Required]
    public string userId { get; set; } = string.Empty;

    [ForeignKey("shopId")]
    public virtual Shop? Shop { get; set; }

    [ForeignKey("productId")]
    public virtual Product? Product { get; set; }

    [ForeignKey("userId")]
    public virtual User? User { get; set; }

}
