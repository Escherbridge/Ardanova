using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ArdaNova.Domain.Models.Enums;

namespace ArdaNova.Domain.Models.Entities;

[Table("SaleItem")]
public class SaleItem
{
    [Key]
    [Required]
    public string id { get; set; } = string.Empty;

    [Required]
    public string saleId { get; set; } = string.Empty;

    [Required]
    public string productId { get; set; } = string.Empty;

    [Required]
    public int quantity { get; set; }

    [Required]
    public decimal price { get; set; }

    [Required]
    public decimal total { get; set; }

    [ForeignKey("saleId")]
    public virtual Sale? Sale { get; set; }

    [ForeignKey("productId")]
    public virtual Product? Product { get; set; }

}
