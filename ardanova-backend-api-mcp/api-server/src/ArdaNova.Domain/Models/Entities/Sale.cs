using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using ArdaNova.Domain.Models.Enums;

namespace ArdaNova.Domain.Models.Entities;

[Table("Sale")]
public class Sale
{
    [Key]
    [Required]
    public string id { get; set; } = string.Empty;

    [Required]
    public string shopId { get; set; } = string.Empty;

    public string? buyerId { get; set; }

    [Required]
    [Precision(18, 8)]
    public decimal total { get; set; }

    [Precision(18, 8)]
    public decimal? tax { get; set; }

    [Precision(18, 8)]
    public decimal? discount { get; set; }

    [Required]
    public PaymentMethod paymentMethod { get; set; }

    [Column(TypeName = "text")]
    public string? notes { get; set; }

    [Required]
    public DateTime createdAt { get; set; }

    [Required]
    public string userId { get; set; } = string.Empty;

    [ForeignKey("shopId")]
    public virtual Shop? Shop { get; set; }

    [ForeignKey("buyerId")]
    [InverseProperty("SalesAsBuyer")]
    public virtual User? Buyer { get; set; }

    [ForeignKey("userId")]
    [InverseProperty("SalesAsUser")]
    public virtual User? User { get; set; }

    public virtual ICollection<SaleItem> SaleItems { get; set; } = new List<SaleItem>();

}
