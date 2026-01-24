using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ArdaNova.Domain.Models.Enums;

namespace ArdaNova.Domain.Models.Entities;

[Table("Product")]
public class Product
{
    [Key]
    public string id { get; set; }

    [Required]
    public string shopId { get; set; }

    [Required]
    public string name { get; set; }

    public string? description { get; set; }

    public string? sku { get; set; }

    [Required]
    public decimal price { get; set; }

    public decimal? cost { get; set; }

    public string? category { get; set; }

    [Required]
    public bool isActive { get; set; }

    [Required]
    public DateTime createdAt { get; set; }

    [Required]
    public DateTime updatedAt { get; set; }

    [Required]
    public string userId { get; set; }

    [ForeignKey("shopId")]
    public virtual Shop Shop { get; set; }

    [ForeignKey("userId")]
    public virtual User User { get; set; }

    public virtual ICollection<SaleItem> SaleItems { get; set; } = new List<SaleItem>();

    public virtual ICollection<InventoryItem> InventoryItems { get; set; } = new List<InventoryItem>();

}
