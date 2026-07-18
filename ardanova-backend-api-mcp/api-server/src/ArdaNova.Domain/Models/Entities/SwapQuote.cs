using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using ArdaNova.Domain.Models.Enums;

namespace ArdaNova.Domain.Models.Entities;

[Index(nameof(semanticKey), IsUnique = true)]
[Table("SwapQuote")]
public class SwapQuote
{

    [Key]
    [Required]
    public string id { get; set; } = string.Empty;

    [Required]
    public string semanticKey { get; set; } = string.Empty;

    [Required]
    public string actorUserId { get; set; } = string.Empty;

    [Required]
    public string sourceAssetDefinitionId { get; set; } = string.Empty;

    [Required]
    public string ardaAssetDefinitionId { get; set; } = string.Empty;

    [Required]
    public string targetAssetDefinitionId { get; set; } = string.Empty;

    [Required]
    public string sourceProjectTokenPolicyId { get; set; } = string.Empty;

    [Required]
    public string targetProjectTokenPolicyId { get; set; } = string.Empty;

    [Required]
    public string sourceAmountAtoms { get; set; } = string.Empty;

    [Required]
    public string ardaAmountAtoms { get; set; } = string.Empty;

    [Required]
    public string targetAmountAtoms { get; set; } = string.Empty;

    [Required]
    [Column(TypeName = "jsonb")]
    public string liquiditySnapshot { get; set; } = string.Empty;

    [Required]
    [Column(TypeName = "jsonb")]
    public string gateDecisionSnapshot { get; set; } = string.Empty;

    [Required]
    public string termsHash { get; set; } = string.Empty;

    [Required]
    public DateTime expiresAt { get; set; }

    [Required]
    public DateTime createdAt { get; set; }

    [ForeignKey("actorUserId")]
    public virtual User? ActorUser { get; set; }

    [ForeignKey("sourceAssetDefinitionId")]
    [InverseProperty("SwapQuotesAsSourceAssetDefinition")]
    public virtual AssetDefinition? SourceAssetDefinition { get; set; }

    [ForeignKey("ardaAssetDefinitionId")]
    [InverseProperty("SwapQuotesAsArdaAssetDefinition")]
    public virtual AssetDefinition? ArdaAssetDefinition { get; set; }

    [ForeignKey("targetAssetDefinitionId")]
    [InverseProperty("SwapQuotesAsTargetAssetDefinition")]
    public virtual AssetDefinition? TargetAssetDefinition { get; set; }

    [ForeignKey("sourceProjectTokenPolicyId")]
    [InverseProperty("SwapQuotesAsSourceProjectTokenPolicy")]
    public virtual ProjectTokenPolicy? SourceProjectTokenPolicy { get; set; }

    [ForeignKey("targetProjectTokenPolicyId")]
    [InverseProperty("SwapQuotesAsTargetProjectTokenPolicy")]
    public virtual ProjectTokenPolicy? TargetProjectTokenPolicy { get; set; }

    public virtual ICollection<SwapOrder> SwapOrders { get; set; } = new List<SwapOrder>();

}
