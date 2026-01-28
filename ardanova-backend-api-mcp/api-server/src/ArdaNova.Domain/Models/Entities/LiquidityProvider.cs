using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using ArdaNova.Domain.Models.Enums;

namespace ArdaNova.Domain.Models.Entities;

[Table("LiquidityProvider")]
public class LiquidityProvider
{
    [Key]
    [Required]
    public string id { get; set; } = string.Empty;

    [Required]
    public string poolId { get; set; } = string.Empty;

    [Required]
    public string userId { get; set; } = string.Empty;

    [Required]
    [Precision(18, 8)]
    public decimal shares { get; set; }

    [Required]
    [Precision(18, 8)]
    public decimal share1In { get; set; }

    [Required]
    [Precision(18, 8)]
    public decimal share2In { get; set; }

    [Required]
    public DateTime createdAt { get; set; }

    [Required]
    public DateTime updatedAt { get; set; }

    [ForeignKey("poolId")]
    public virtual LiquidityPool? Pool { get; set; }

    [ForeignKey("userId")]
    public virtual User? User { get; set; }

}
