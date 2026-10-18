using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using ArdaNova.Domain.Models.Enums;

namespace ArdaNova.Domain.Models.Entities;

[Table("EquityOrRedemptionRightPolicy")]
public class EquityOrRedemptionRightPolicy
{

    [Key]
    [Required]
    public string id { get; set; } = string.Empty;

    [Required]
    public string projectId { get; set; } = string.Empty;

    [Required]
    public EquityOrRedemptionRightKind kind { get; set; }

    [Required]
    public int version { get; set; }

    [Required]
    public string jurisdiction { get; set; } = string.Empty;

    [Required]
    public string disclosureVersion { get; set; } = string.Empty;

    [Required]
    public string eligibilityPolicyVersion { get; set; } = string.Empty;

    [Required]
    public string termsHash { get; set; } = string.Empty;

    [Required]
    [Column(TypeName = "jsonb")]
    public string termsSnapshot { get; set; } = string.Empty;

    [Required]
    public DateTime effectiveFrom { get; set; }

    public DateTime? retiredAt { get; set; }

    [Required]
    public DateTime createdAt { get; set; }

    [ForeignKey("projectId")]
    public virtual Project? Project { get; set; }

    public virtual ICollection<EligibilityDecision> EligibilityDecisions { get; set; } = new List<EligibilityDecision>();

    public virtual ICollection<EconomicSettlement> EconomicSettlements { get; set; } = new List<EconomicSettlement>();

    public virtual ICollection<FundingIntent> FundingIntents { get; set; } = new List<FundingIntent>();

    public virtual ICollection<TaskCommerceAgreement> TaskCommerceAgreements { get; set; } = new List<TaskCommerceAgreement>();

}
