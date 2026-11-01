// Generated from database-architecture.dbml by generate:csharp - do not edit; hand-written members go in ProjectTokenConfig.Custom.cs
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using ArdaNova.Domain.Models.Enums;

namespace ArdaNova.Domain.Models.Entities;

[Index(nameof(projectId), IsUnique = true)]
[Table("ProjectTokenConfig")]
public partial class ProjectTokenConfig
{

    [Key]
    [Required]
    public string id { get; set; } = string.Empty;

    [Required]
    public string projectId { get; set; } = string.Empty;

    public string? assetId { get; set; }

    public string? assetDefinitionId { get; set; }

    [Required]
    public string assetName { get; set; } = string.Empty;

    [Required]
    public string unitName { get; set; } = string.Empty;

    public int? assetScale { get; set; }

    [Required]
    public int totalSupply { get; set; }

    [Required]
    public int allocatedSupply { get; set; }

    [Required]
    public int distributedSupply { get; set; }

    [Required]
    public int reservedSupply { get; set; }

    public string? mintTxHash { get; set; }

    [Required]
    public ProjectTokenStatus status { get; set; }

    [Required]
    public DateTime createdAt { get; set; }

    [Required]
    public DateTime updatedAt { get; set; }

    [Required]
    public double fundingGoal { get; set; }

    [Required]
    public double fundingRaised { get; set; }

    [Required]
    public ProjectGateStatus gateStatus { get; set; }

    public DateTime? gate1ClearedAt { get; set; }

    public DateTime? gate2ClearedAt { get; set; }

    public DateTime? failedAt { get; set; }

    [Required]
    public int contributorSupply { get; set; }

    [Required]
    public int investorSupply { get; set; }

    [Required]
    public int founderSupply { get; set; }

    [Required]
    public int burnedSupply { get; set; }

    [Column(TypeName = "text")]
    public string? successCriteria { get; set; }

    public string? successVerifiedBy { get; set; }

    public string? assetChain { get; set; }

    public string? treasuryChain { get; set; }

    public string? treasuryReference { get; set; }

    public string? treasuryAddress { get; set; }

    public DateTime? treasuryResolvedAt { get; set; }

    [Required]
    public int contributorPartitionSupply { get; set; }

    [Required]
    public int investorPartitionSupply { get; set; }

    public DateTime? partitionsDeclaredAt { get; set; }

    [Column(TypeName = "text")]
    public string? unitsDisclosure { get; set; }

    public DateTime? provisionedAt { get; set; }

    public string? provisionedByUserId { get; set; }

    [Column(TypeName = "text")]
    public string? provisioningFailureReason { get; set; }

    [ForeignKey("projectId")]
    public virtual Project? Project { get; set; }

    public virtual ICollection<TokenAllocation> TokenAllocations { get; set; } = new List<TokenAllocation>();

    public virtual ICollection<TokenBalance> TokenBalances { get; set; } = new List<TokenBalance>();

    public virtual ICollection<PayoutRequest> PayoutRequests { get; set; } = new List<PayoutRequest>();

    public virtual ICollection<ProjectInvestment> ProjectInvestments { get; set; } = new List<ProjectInvestment>();

    public virtual ICollection<FundingIntent> FundingIntents { get; set; } = new List<FundingIntent>();

    public virtual ICollection<TaskCommerceAgreement> TaskCommerceAgreements { get; set; } = new List<TaskCommerceAgreement>();

    [ForeignKey("assetDefinitionId")]
    public virtual AssetDefinition? AssetDefinition { get; set; }

}
