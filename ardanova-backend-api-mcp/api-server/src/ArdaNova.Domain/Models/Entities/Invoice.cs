using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using ArdaNova.Domain.Models.Enums;

namespace ArdaNova.Domain.Models.Entities;

[Index(nameof(invoiceNumber), IsUnique = true)]
[Table("Invoice")]
public class Invoice
{
    [Key]
    [Required]
    public string id { get; set; } = string.Empty;

    [Required]
    public string shopId { get; set; } = string.Empty;

    [Required]
    public string buyerId { get; set; } = string.Empty;

    [Required]
    public string invoiceNumber { get; set; } = string.Empty;

    [Required]
    [Precision(18, 8)]
    public decimal amount { get; set; }

    [Precision(18, 8)]
    public decimal? tax { get; set; }

    [Precision(18, 8)]
    public decimal? discount { get; set; }

    [Required]
    [Precision(18, 8)]
    public decimal total { get; set; }

    [Required]
    public InvoiceStatus status { get; set; }

    [Required]
    public DateTime dueDate { get; set; }

    public DateTime? paidAt { get; set; }

    [Column(TypeName = "text")]
    public string? notes { get; set; }

    [Required]
    public DateTime createdAt { get; set; }

    [Required]
    public DateTime updatedAt { get; set; }

    [Required]
    public string userId { get; set; } = string.Empty;

    [ForeignKey("shopId")]
    public virtual Shop? Shop { get; set; }

    [ForeignKey("buyerId")]
    [InverseProperty("InvoicesAsBuyer")]
    public virtual User? Buyer { get; set; }

    [ForeignKey("userId")]
    [InverseProperty("InvoicesAsUser")]
    public virtual User? User { get; set; }

}
