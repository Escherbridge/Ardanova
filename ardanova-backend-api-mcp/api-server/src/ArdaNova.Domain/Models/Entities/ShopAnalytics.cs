using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ArdaNova.Domain.Models.Enums;

namespace ArdaNova.Domain.Models.Entities;

[Table("ShopAnalytics")]
public class ShopAnalytics
{
    [Key]
    public string id { get; set; }

    [Required]
    public string shopId { get; set; }

    [Required]
    public DateTime date { get; set; }

    [Required]
    public decimal revenue { get; set; }

    [Required]
    public decimal expenses { get; set; }

    [Required]
    public decimal profit { get; set; }

    [Required]
    public int salesCount { get; set; }

    [Required]
    public int newCustomers { get; set; }

    [Required]
    public DateTime createdAt { get; set; }

    [ForeignKey("shopId")]
    public virtual Shop Shop { get; set; }

}
