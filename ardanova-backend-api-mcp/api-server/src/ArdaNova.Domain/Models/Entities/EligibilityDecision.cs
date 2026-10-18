using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using ArdaNova.Domain.Models.Enums;

namespace ArdaNova.Domain.Models.Entities;

[Table("EligibilityDecision")]
public class EligibilityDecision
{

    [Key]
    [Required]
    public string id { get; set; } = string.Empty;

    [Required]
    public string userId { get; set; } = string.Empty;

    [Required]
    public string equityOrRedemptionRightPolicyId { get; set; } = string.Empty;

    [Required]
    public EligibilityDecisionStatus status { get; set; }

    [Required]
    public string evidenceDigest { get; set; } = string.Empty;

    [Required]
    public string reasonCode { get; set; } = string.Empty;

    public string? decidedByUserId { get; set; }

    public DateTime? expiresAt { get; set; }

    public DateTime? reviewedAt { get; set; }

    [Required]
    public DateTime createdAt { get; set; }

    [ForeignKey("userId")]
    [InverseProperty("EligibilityDecisionsAsUser")]
    public virtual User? User { get; set; }

    [ForeignKey("equityOrRedemptionRightPolicyId")]
    public virtual EquityOrRedemptionRightPolicy? EquityOrRedemptionRightPolicy { get; set; }

    [ForeignKey("decidedByUserId")]
    [InverseProperty("EligibilityDecisionsAsDecidedByUser")]
    public virtual User? DecidedByUser { get; set; }

    public virtual ICollection<EconomicSettlement> EconomicSettlements { get; set; } = new List<EconomicSettlement>();

    public virtual ICollection<FundingIntent> FundingIntents { get; set; } = new List<FundingIntent>();

    public virtual ICollection<TaskCommerceAgreement> TaskCommerceAgreements { get; set; } = new List<TaskCommerceAgreement>();

}
