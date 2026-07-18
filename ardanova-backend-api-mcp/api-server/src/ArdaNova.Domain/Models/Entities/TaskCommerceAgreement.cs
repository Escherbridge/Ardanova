using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using ArdaNova.Domain.Models.Enums;

namespace ArdaNova.Domain.Models.Entities;

[Index(nameof(semanticKey), IsUnique = true)]
[Index(nameof(taskId), IsUnique = true)]
[Index(nameof(bidId), IsUnique = true)]
[Index(nameof(escrowId), IsUnique = true)]
[Index(nameof(settlementId), IsUnique = true)]
[Table("TaskCommerceAgreement")]
public class TaskCommerceAgreement
{

    [Key]
    [Required]
    public string id { get; set; } = string.Empty;

    [Required]
    public string semanticKey { get; set; } = string.Empty;

    [Required]
    public TaskCommerceAgreementStatus status { get; set; }

    [Required]
    public string projectId { get; set; } = string.Empty;

    [Required]
    public string taskId { get; set; } = string.Empty;

    [Required]
    public string bidId { get; set; } = string.Empty;

    [Required]
    public string contributorUserId { get; set; } = string.Empty;

    public string? projectTokenConfigId { get; set; }

    public string? assetDefinitionId { get; set; }

    public string? projectTokenPolicyId { get; set; }

    public string? equityOrRedemptionRightPolicyId { get; set; }

    public string? eligibilityDecisionId { get; set; }

    [Required]
    public string assetCode { get; set; } = string.Empty;

    [Required]
    [Precision(38, 18)]
    public decimal awardAmount { get; set; }

    [Required]
    public int scale { get; set; }

    [Required]
    [Column(TypeName = "jsonb")]
    public string acceptedTermsSnapshot { get; set; } = string.Empty;

    [Required]
    public string termsHash { get; set; } = string.Empty;

    public string? escrowId { get; set; }

    public string? questRunId { get; set; }

    public string? settlementId { get; set; }

    public DateTime? acceptedAt { get; set; }

    public DateTime? releaseAuthorizedAt { get; set; }

    public DateTime? settledAt { get; set; }

    public DateTime? cancelledAt { get; set; }

    [Required]
    public DateTime createdAt { get; set; }

    [Required]
    public DateTime updatedAt { get; set; }

    [ForeignKey("projectId")]
    public virtual Project? Project { get; set; }

    [ForeignKey("taskId")]
    public virtual ProjectTask? Task { get; set; }

    [ForeignKey("bidId")]
    public virtual OpportunityBid? Bid { get; set; }

    [ForeignKey("contributorUserId")]
    public virtual User? ContributorUser { get; set; }

    [ForeignKey("projectTokenConfigId")]
    public virtual ProjectTokenConfig? ProjectTokenConfig { get; set; }

    [ForeignKey("escrowId")]
    public virtual TaskEscrow? Escrow { get; set; }

    [ForeignKey("settlementId")]
    public virtual EconomicSettlement? Settlement { get; set; }

    [ForeignKey("assetDefinitionId")]
    public virtual AssetDefinition? AssetDefinition { get; set; }

    [ForeignKey("projectTokenPolicyId")]
    public virtual ProjectTokenPolicy? ProjectTokenPolicy { get; set; }

    [ForeignKey("equityOrRedemptionRightPolicyId")]
    public virtual EquityOrRedemptionRightPolicy? EquityOrRedemptionRightPolicy { get; set; }

    [ForeignKey("eligibilityDecisionId")]
    public virtual EligibilityDecision? EligibilityDecision { get; set; }

}
