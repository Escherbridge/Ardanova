using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using ArdaNova.Domain.Models.Enums;

namespace ArdaNova.Domain.Models.Entities;

[Table("LiquidityPool")]
public class LiquidityPool
{
    [Key]
    [Required]
    public string id { get; set; } = string.Empty;

    [Required]
    public string share1Id { get; set; } = string.Empty;

    [Required]
    public string share2Id { get; set; } = string.Empty;

    [Required]
    [Precision(18, 8)]
    public decimal reserve1 { get; set; }

    [Required]
    [Precision(18, 8)]
    public decimal reserve2 { get; set; }

    [Required]
    [Precision(18, 8)]
    public decimal totalShares { get; set; }

    [Required]
    [Precision(18, 8)]
    public decimal feePercent { get; set; }

    [Required]
    public bool isActive { get; set; }

    [Required]
    public DateTime createdAt { get; set; }

    [Required]
    public DateTime updatedAt { get; set; }

    [ForeignKey("share1Id")]
    [InverseProperty("LiquidityPoolsAsShare1")]
    public virtual ProjectShare? Share1 { get; set; }

    [ForeignKey("share2Id")]
    [InverseProperty("LiquidityPoolsAsShare2")]
    public virtual ProjectShare? Share2 { get; set; }

    public virtual ICollection<LiquidityProvider> LiquidityProviders { get; set; } = new List<LiquidityProvider>();

}
