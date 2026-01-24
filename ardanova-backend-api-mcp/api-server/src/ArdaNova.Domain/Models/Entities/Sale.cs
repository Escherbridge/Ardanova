using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ArdaNova.Domain.Models.Enums;

namespace ArdaNova.Domain.Models.Entities;

[Table("Sale")]
public class Sale
{
    [Key]
    public string id { get; set; }

    [Required]
    public string shopId { get; set; }

    public string? buyerId { get; set; }

    [Required]
    public decimal total { get; set; }

    public decimal? tax { get; set; }

    public decimal? discount { get; set; }

    [Required]
    public PaymentMethod paymentMethod { get; set; }

    public string? notes { get; set; }

    [Required]
    public DateTime createdAt { get; set; }

    [Required]
    public string userId { get; set; }

    [ForeignKey("shopId")]
    public virtual Shop Shop { get; set; }

    [ForeignKey("buyerId")]
    public virtual User Buyer { get; set; }

    [ForeignKey("userId")]
    public virtual User User { get; set; }

    public virtual ICollection<SaleItem> SaleItems { get; set; } = new List<SaleItem>();

}
