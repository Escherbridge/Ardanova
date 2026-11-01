// Generated from database-architecture.dbml by generate:csharp - do not edit; hand-written members go in FundingIntent.Custom.cs
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using ArdaNova.Domain.Models.Enums;

namespace ArdaNova.Domain.Models.Entities;

[Index(nameof(semanticKey), IsUnique = true)]
[Index(nameof(exchangeIdempotencyKey), IsUnique = true)]
[Index(nameof(settlementId), IsUnique = true)]
[Table("FundingIntent")]
public partial class FundingIntent
{

    [Key]
    [Required]
    public string id { get; set; } = string.Empty;

    [Required]
    public string semanticKey { get; set; } = string.Empty;

    [Required]
    public string idempotencyKey { get; set; } = string.Empty;

    [Required]
    public FundingIntentStatus status { get; set; }

    [Required]
    public string funderUserId { get; set; } = string.Empty;

    [Required]
    public string projectId { get; set; } = string.Empty;

    [Required]
    public string projectTokenConfigId { get; set; } = string.Empty;

    public string? paymentAssetDefinitionId { get; set; }

    public string? awardAssetDefinitionId { get; set; }

    public string? awardAmountAtoms { get; set; }

    public string? projectTokenPolicyId { get; set; }

    public string? equityOrRedemptionRightPolicyId { get; set; }

    public string? eligibilityDecisionId { get; set; }

    [Required]
    public string disclosureVersion { get; set; } = string.Empty;

    [Required]
    public string platformAssetCode { get; set; } = string.Empty;

    [Required]
    public long platformAmountBaseUnits { get; set; }

    [Required]
    public int platformScale { get; set; }

    [Required]
    public long requestedPlatformBaseUnits { get; set; }

    [Required]
    public string projectUnitAssetCode { get; set; } = string.Empty;

    [Required]
    public long projectUnitQuantity { get; set; }

    [Required]
    public int projectUnitScale { get; set; }

    [Required]
    public string projectAssetId { get; set; } = string.Empty;

    [Required]
    public long usdReferenceMinorUnits { get; set; }

    [Required]
    public string declaredRateArdaPerUnit { get; set; } = string.Empty;

    [Required]
    public string declaredRateUsdPerUnit { get; set; } = string.Empty;

    [Required]
    public string declaredRateSource { get; set; } = string.Empty;

    [Required]
    public DateTime declaredRateDeclaredAt { get; set; }

    public string? declaredRateVersion { get; set; }

    [Required]
    [Column(TypeName = "jsonb")]
    public string eligibilitySnapshot { get; set; } = string.Empty;

    [Required]
    [Column(TypeName = "jsonb")]
    public string termsSnapshot { get; set; } = string.Empty;

    [Required]
    public string termsHash { get; set; } = string.Empty;

    public string? exchangeIdempotencyKey { get; set; }

    public string? azoaOperationId { get; set; }

    public string? holdReference { get; set; }

    public DateTime? releaseNotBefore { get; set; }

    public string? settlementId { get; set; }

    [Required]
    public int settlementAttemptCount { get; set; }

    public string? holdingReasonCode { get; set; }

    public string? holdingReasonDetail { get; set; }

    public DateTime? heldAt { get; set; }

    public DateTime? expiresAt { get; set; }

    public DateTime? settledAt { get; set; }

    public DateTime? cancelledAt { get; set; }

    [Required]
    public DateTime createdAt { get; set; }

    [Required]
    public DateTime updatedAt { get; set; }

    [ForeignKey("funderUserId")]
    public virtual User? FunderUser { get; set; }

    [ForeignKey("projectId")]
    public virtual Project? Project { get; set; }

    [ForeignKey("projectTokenConfigId")]
    public virtual ProjectTokenConfig? ProjectTokenConfig { get; set; }

    [ForeignKey("settlementId")]
    public virtual EconomicSettlement? Settlement { get; set; }

    [ForeignKey("paymentAssetDefinitionId")]
    [InverseProperty("FundingIntentsAsPaymentAssetDefinition")]
    public virtual AssetDefinition? PaymentAssetDefinition { get; set; }

    [ForeignKey("awardAssetDefinitionId")]
    [InverseProperty("FundingIntentsAsAwardAssetDefinition")]
    public virtual AssetDefinition? AwardAssetDefinition { get; set; }

    [ForeignKey("projectTokenPolicyId")]
    public virtual ProjectTokenPolicy? ProjectTokenPolicy { get; set; }

    [ForeignKey("equityOrRedemptionRightPolicyId")]
    public virtual EquityOrRedemptionRightPolicy? EquityOrRedemptionRightPolicy { get; set; }

    [ForeignKey("eligibilityDecisionId")]
    public virtual EligibilityDecision? EligibilityDecision { get; set; }

}
