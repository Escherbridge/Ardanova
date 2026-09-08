using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using ArdaNova.Domain.Models.Enums;

namespace ArdaNova.Domain.Models.Entities;

[Index(nameof(idempotencyKey), IsUnique = true)]
[Index(nameof(externalEventId), IsUnique = true)]
[Table("EconomicSettlement")]
public class EconomicSettlement
{

    [Key]
    [Required]
    public string id { get; set; } = string.Empty;

    [Required]
    public EconomicSettlementKind kind { get; set; }

    [Required]
    public EconomicSettlementStatus status { get; set; }

    [Required]
    public string idempotencyKey { get; set; } = string.Empty;

    public string? externalEventId { get; set; }

    [Required]
    public string beneficiaryUserId { get; set; } = string.Empty;

    public string? authorizedByUserId { get; set; }

    public string? projectId { get; set; }

    public string? taskId { get; set; }

    public string? escrowId { get; set; }

    [Required]
    public string assetCode { get; set; } = string.Empty;

    [Required]
    [Precision(38, 18)]
    public decimal amount { get; set; }

    [Required]
    public int scale { get; set; }

    [Column(TypeName = "jsonb")]
    public string? termsSnapshot { get; set; }

    public string? azoaOperationId { get; set; }

    [Column(TypeName = "jsonb")]
    public string? azoaReceipt { get; set; }

    public bool? azoaReplayed { get; set; }

    /// <summary>Base units the payer side gave up, as the node's receipt reported them.</summary>
    public string? azoaGrossBaseUnits { get; set; }

    /// <summary>Base units the node kept between gross and net; a silently shaved fee is a defect, not a detail.</summary>
    public string? azoaFeeBaseUnits { get; set; }

    /// <summary>Base units the beneficiary side actually received.</summary>
    public string? azoaNetBaseUnits { get; set; }

    public string? failureCode { get; set; }

    public string? failureDetail { get; set; }

    [Required]
    public int version { get; set; }

    [Required]
    public DateTime createdAt { get; set; }

    public DateTime? authorizedAt { get; set; }

    public DateTime? submittedAt { get; set; }

    public DateTime? confirmedAt { get; set; }

    [Required]
    public DateTime updatedAt { get; set; }

    [ForeignKey("beneficiaryUserId")]
    [InverseProperty("EconomicSettlementsAsBeneficiaryUser")]
    public virtual User? BeneficiaryUser { get; set; }

    [ForeignKey("authorizedByUserId")]
    [InverseProperty("EconomicSettlementsAsAuthorizedByUser")]
    public virtual User? AuthorizedByUser { get; set; }

    [ForeignKey("projectId")]
    public virtual Project? Project { get; set; }

    [ForeignKey("taskId")]
    public virtual ProjectTask? Task { get; set; }

    [ForeignKey("escrowId")]
    public virtual TaskEscrow? Escrow { get; set; }

    public virtual ICollection<EconomicOutbox> EconomicOutboxes { get; set; } = new List<EconomicOutbox>();

    public virtual ICollection<FundingIntent> FundingIntents { get; set; } = new List<FundingIntent>();

    public virtual ICollection<TaskCommerceAgreement> TaskCommerceAgreements { get; set; } = new List<TaskCommerceAgreement>();

}
