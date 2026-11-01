// Generated from database-architecture.dbml by generate:csharp - do not edit; hand-written members go in TokenBalance.Custom.cs
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using ArdaNova.Domain.Models.Enums;

namespace ArdaNova.Domain.Models.Entities;

[Table("TokenBalance")]
public partial class TokenBalance
{

    [Key]
    [Required]
    public string id { get; set; } = string.Empty;

    [Required]
    public string userId { get; set; } = string.Empty;

    public string? projectTokenConfigId { get; set; }

    [Required]
    public bool isPlatformToken { get; set; }

    public TokenHolderClass? holderClass { get; set; }

    [Required]
    public bool isLiquid { get; set; }

    [Required]
    public long balance { get; set; }

    [Required]
    public long lockedBalance { get; set; }

    public string? assetId { get; set; }

    public string? walletAddress { get; set; }

    public DateTime? observedAt { get; set; }

    [Required]
    public DateTime updatedAt { get; set; }

    [ForeignKey("userId")]
    public virtual User? User { get; set; }

    [ForeignKey("projectTokenConfigId")]
    public virtual ProjectTokenConfig? ProjectTokenConfig { get; set; }

}
