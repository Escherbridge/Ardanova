using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ArdaNova.Domain.Models.Enums;

namespace ArdaNova.Domain.Models.Entities;

[Table("Invoice")]
public class Invoice
{
    [Key]
    public string id { get; set; }

    [Required]
    public string shopId { get; set; }

    [Required]
    public string buyerId { get; set; }

    [Required]
    public string invoiceNumber { get; set; }

    [Required]
    public decimal amount { get; set; }

    public decimal? tax { get; set; }

    public decimal? discount { get; set; }

    [Required]
    public decimal total { get; set; }

    [Required]
    public InvoiceStatus status { get; set; }

    [Required]
    public DateTime dueDate { get; set; }

    public DateTime? paidAt { get; set; }

    public string? notes { get; set; }

    [Required]
    public DateTime createdAt { get; set; }

    [Required]
    public DateTime updatedAt { get; set; }

    [Required]
    public string userId { get; set; }

    [ForeignKey("shopId")]
    public virtual Shop Shop { get; set; }

    [ForeignKey("buyerId")]
    public virtual User Buyer { get; set; }

    [ForeignKey("userId")]
    public virtual User User { get; set; }

}
