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
    public string token1Id { get; set; } = string.Empty;

    [Required]
    public string token2Id { get; set; } = string.Empty;

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

    [ForeignKey("token1Id")]
    [InverseProperty("LiquidityPoolsAsToken1")]
    public virtual ProjectToken? Token1 { get; set; }

    [ForeignKey("token2Id")]
    [InverseProperty("LiquidityPoolsAsToken2")]
    public virtual ProjectToken? Token2 { get; set; }

    public virtual ICollection<LiquidityProvider> LiquidityProviders { get; set; } = new List<LiquidityProvider>();

}
