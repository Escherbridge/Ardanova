using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using ArdaNova.Domain.Models.Enums;

namespace ArdaNova.Domain.Models.Entities;

/// <summary>One sponsorship, with both legs of the exchange and the declared rate locked before value moves.</summary>
/// <remarks>
/// Every field below the identity block is written once, when the person authorises the
/// sponsorship, and is immutable from then on: the quantity of project units they are buying, the
/// platform units they give up, and the rate that related the two. The node is asked to move
/// exactly these amounts, so a rate that moves afterwards cannot change what was agreed.
/// See <c>src/ArdaNova.Domain/Models/Entities/AGENTS.md</c> section sponsorship.
/// </remarks>
[Index(nameof(semanticKey), IsUnique = true)]
[Index(nameof(exchangeIdempotencyKey), IsUnique = true)]
[Index(nameof(settlementId), IsUnique = true)]
[Table("FundingIntent")]
public class FundingIntent
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

    // -- The exchange, locked at authorization ------------------------------
    // Two legs and one rate. Amounts are exact integer base units, never a float and never a
    // currency: the node settles token units, and a settlement denominated in "USD" is refused
    // at the gateway rather than sent.

    /// <summary>Platform unit the sponsor spends; "ARDA" on this node.</summary>
    [Required]
    public string platformAssetCode { get; set; } = string.Empty;

    /// <summary>Exact platform-unit base units the sponsor gives up.</summary>
    [Required]
    public long platformAmountBaseUnits { get; set; }

    /// <summary>Decimal places of the platform unit; two, so a person always thinks in whole currency.</summary>
    [Required]
    public int platformScale { get; set; }

    /// <summary>
    /// What the sponsor offered, before whole units were fitted to it. Kept because the
    /// idempotency key covers the REQUEST: a replay is only "different terms" when this differs,
    /// and the remainder below one unit is the sponsor's, not the platform's.
    /// </summary>
    [Required]
    public long requestedPlatformBaseUnits { get; set; }

    /// <summary>Symbol of the project unit the sponsor receives.</summary>
    [Required]
    public string projectUnitAssetCode { get; set; } = string.Empty;

    /// <summary>Whole project units bought. This is the quantity the person agreed to.</summary>
    [Required]
    public long projectUnitQuantity { get; set; }

    /// <summary>Decimal places of the project asset, as the node minted it.</summary>
    [Required]
    public int projectUnitScale { get; set; }

    /// <summary>Chain asset the project units are issued from; funding is impossible without it.</summary>
    [Required]
    public string projectAssetId { get; set; } = string.Empty;

    /// <summary>Exact currency reference the disclosure showed, in minor units. Never an input to the exchange.</summary>
    [Required]
    public long usdReferenceMinorUnits { get; set; }

    // -- The declared rate (D7/D16: ArdaNova prices, the node records) ------

    /// <summary>Platform units one whole project unit cost, as an exact decimal string.</summary>
    [Required]
    public string declaredRateArdaPerUnit { get; set; } = string.Empty;

    /// <summary>Currency reference for one whole project unit, as an exact decimal string.</summary>
    [Required]
    public string declaredRateUsdPerUnit { get; set; } = string.Empty;

    /// <summary>Where the rate came from; stamped on the node operation.</summary>
    [Required]
    public string declaredRateSource { get; set; } = string.Empty;

    /// <summary>When ArdaNova declared the rate this sponsorship was priced at.</summary>
    [Required]
    public DateTime declaredRateDeclaredAt { get; set; }

    /// <summary>Optional rate-policy version, when the source publishes one.</summary>
    public string? declaredRateVersion { get; set; }

    // -- Snapshots ----------------------------------------------------------

    [Required]
    [Column(TypeName = "jsonb")]
    public string eligibilitySnapshot { get; set; } = string.Empty;

    [Required]
    [Column(TypeName = "jsonb")]
    public string termsSnapshot { get; set; } = string.Empty;

    [Required]
    public string termsHash { get; set; } = string.Empty;

    // -- The node operation -------------------------------------------------

    /// <summary>
    /// The one key this sponsorship is ever settled under, shared with its settlement row so the
    /// synchronous attempt and the outbox worker can never create two economic effects.
    /// </summary>
    public string? exchangeIdempotencyKey { get; set; }

    /// <summary>Durable node operation that moved the value.</summary>
    public string? azoaOperationId { get; set; }

    /// <summary>Node-side hold the freshly issued units sit in until the window elapses.</summary>
    public string? holdReference { get; set; }

    /// <summary>When the held units become the sponsor's to move; surfaced as "yours after N days".</summary>
    public DateTime? releaseNotBefore { get; set; }

    public string? settlementId { get; set; }

    /// <summary>Node attempts this intent has made. Bounded: a lapse must never retry forever.</summary>
    [Required]
    public int settlementAttemptCount { get; set; }

    // -- Why it stopped -----------------------------------------------------

    /// <summary>Machine code an operator triages a held or failed sponsorship on.</summary>
    public string? holdingReasonCode { get; set; }

    /// <summary>Consumer-safe explanation shown to the sponsor.</summary>
    public string? holdingReasonDetail { get; set; }

    /// <summary>When the sponsorship was parked with the sponsor's platform units untouched.</summary>
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
