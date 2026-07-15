using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using ArdaNova.Domain.Models.Enums;

namespace ArdaNova.Domain.Models.Entities;

[Index(nameof(semanticKey), IsUnique = true)]
[Index(nameof(providerCheckoutSessionId), IsUnique = true)]
[Index(nameof(providerPaymentIntentId), IsUnique = true)]
[Index(nameof(verifiedProviderEventId), IsUnique = true)]
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

    [Required]
    public string currencyCode { get; set; } = string.Empty;

    [Required]
    [Precision(38, 18)]
    public decimal amount { get; set; }

    [Required]
    public int scale { get; set; }

    [Required]
    public string disclosureVersion { get; set; } = string.Empty;

    [Required]
    [Column(TypeName = "jsonb")]
    public string eligibilitySnapshot { get; set; } = string.Empty;

    [Required]
    [Column(TypeName = "jsonb")]
    public string termsSnapshot { get; set; } = string.Empty;

    [Required]
    public string termsHash { get; set; } = string.Empty;

    public string? paymentProvider { get; set; }

    public string? providerCheckoutSessionId { get; set; }

    public string? providerPaymentIntentId { get; set; }

    public string? verifiedProviderEventId { get; set; }

    public string? settlementId { get; set; }

    public DateTime? expiresAt { get; set; }

    public DateTime? paymentVerifiedAt { get; set; }

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

}
