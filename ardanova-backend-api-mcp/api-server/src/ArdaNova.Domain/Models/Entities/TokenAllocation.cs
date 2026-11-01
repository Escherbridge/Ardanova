// Generated from database-architecture.dbml by generate:csharp - do not edit; hand-written members go in TokenAllocation.Custom.cs
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using ArdaNova.Domain.Models.Enums;

namespace ArdaNova.Domain.Models.Entities;

[Index(nameof(holdId), IsUnique = true)]
[Index(nameof(workerHoldId), IsUnique = true)]
[Table("TokenAllocation")]
public partial class TokenAllocation
{

    [Key]
    [Required]
    public string id { get; set; } = string.Empty;

    [Required]
    public string projectTokenConfigId { get; set; } = string.Empty;

    public string? pbiId { get; set; }

    public string? recipientUserId { get; set; }

    [Required]
    public double equityPercentage { get; set; }

    [Required]
    public int tokenAmount { get; set; }

    [Required]
    public AllocationStatus status { get; set; }

    [Required]
    public TokenHolderClass holderClass { get; set; }

    [Required]
    public bool isLiquid { get; set; }

    public DateTime? distributedAt { get; set; }

    public string? distributionTxHash { get; set; }

    public DateTime? burnedAt { get; set; }

    public string? taskCommerceAgreementId { get; set; }

    public string? holdChain { get; set; }

    public string? reservePartition { get; set; }

    public string? holdId { get; set; }

    public string? holdState { get; set; }

    public string? workerHoldId { get; set; }

    public string? workerHoldState { get; set; }

    public DateTime? workerHoldReleasesAt { get; set; }

    public DateTime? holdSyncedAt { get; set; }

    [Required]
    public DateTime createdAt { get; set; }

    [Required]
    public DateTime updatedAt { get; set; }

    [ForeignKey("projectTokenConfigId")]
    public virtual ProjectTokenConfig? ProjectTokenConfig { get; set; }

    [ForeignKey("pbiId")]
    public virtual ProductBacklogItem? Pbi { get; set; }

    [ForeignKey("recipientUserId")]
    public virtual User? RecipientUser { get; set; }

}
