using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ArdaNova.Domain.Models.Enums;

namespace ArdaNova.Domain.Models.Entities;

[Table("LiquidityPool")]
public class LiquidityPool
{
    [Key]
    public string id { get; set; }

    [Required]
    public string token1Id { get; set; }

    [Required]
    public string token2Id { get; set; }

    [Required]
    public decimal reserve1 { get; set; }

    [Required]
    public decimal reserve2 { get; set; }

    [Required]
    public decimal totalShares { get; set; }

    [Required]
    public decimal feePercent { get; set; }

    [Required]
    public bool isActive { get; set; }

    [Required]
    public DateTime createdAt { get; set; }

    [Required]
    public DateTime updatedAt { get; set; }

    [ForeignKey("token1Id")]
    public virtual ProjectToken Token1 { get; set; }

    [ForeignKey("token2Id")]
    public virtual ProjectToken Token2 { get; set; }

    public virtual ICollection<LiquidityProvider> LiquidityProviders { get; set; } = new List<LiquidityProvider>();

}
