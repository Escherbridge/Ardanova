using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using ArdaNova.Domain.Models.Enums;

namespace ArdaNova.Domain.Models.Entities;

[Table("AssetDefinition")]
public class AssetDefinition
{

    [Key]
    [Required]
    public string id { get; set; } = string.Empty;

    [Required]
    public AssetDefinitionKind kind { get; set; }

    [Required]
    public string chainType { get; set; } = string.Empty;

    [Required]
    public string chainNetwork { get; set; } = string.Empty;

    [Required]
    public string canonicalAssetId { get; set; } = string.Empty;

    [Required]
    public string symbol { get; set; } = string.Empty;

    [Required]
    public string displayName { get; set; } = string.Empty;

    [Required]
    public int scale { get; set; }

    public string? supersedesAssetDefinitionId { get; set; }

    [Required]
    public DateTime createdAt { get; set; }

    [ForeignKey("supersedesAssetDefinitionId")]
    public virtual AssetDefinition? SupersedesAssetDefinition { get; set; }

    public virtual ICollection<ProjectTokenPolicy> ProjectTokenPolicies { get; set; } = new List<ProjectTokenPolicy>();

    public virtual ICollection<EconomicSettlementLeg> EconomicSettlementLegs { get; set; } = new List<EconomicSettlementLeg>();

    public virtual ICollection<ProjectTokenConfig> ProjectTokenConfigs { get; set; } = new List<ProjectTokenConfig>();

    public virtual ICollection<EconomicSettlement> EconomicSettlements { get; set; } = new List<EconomicSettlement>();

    public virtual ICollection<TaskCommerceAgreement> TaskCommerceAgreements { get; set; } = new List<TaskCommerceAgreement>();

    public virtual ICollection<SwapQuote> SwapQuotesAsSourceAssetDefinition { get; set; } = new List<SwapQuote>();

    public virtual ICollection<SwapQuote> SwapQuotesAsArdaAssetDefinition { get; set; } = new List<SwapQuote>();

    public virtual ICollection<SwapQuote> SwapQuotesAsTargetAssetDefinition { get; set; } = new List<SwapQuote>();

    public virtual ICollection<FundingIntent> FundingIntentsAsPaymentAssetDefinition { get; set; } = new List<FundingIntent>();

    public virtual ICollection<FundingIntent> FundingIntentsAsAwardAssetDefinition { get; set; } = new List<FundingIntent>();

}
