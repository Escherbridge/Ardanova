using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ArdaNova.Domain.Models.Enums;

namespace ArdaNova.Domain.Models.Entities;

[Table("InventoryItem")]
public class InventoryItem
{
    [Key]
    public string id { get; set; }

    [Required]
    public string shopId { get; set; }

    [Required]
    public string productId { get; set; }

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
    public string userId { get; set; }

    [ForeignKey("shopId")]
    public virtual Shop Shop { get; set; }

    [ForeignKey("productId")]
    public virtual Product Product { get; set; }

    [ForeignKey("userId")]
    public virtual User User { get; set; }

}
