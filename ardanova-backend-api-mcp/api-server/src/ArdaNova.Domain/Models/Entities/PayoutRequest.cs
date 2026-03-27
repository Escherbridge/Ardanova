using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using ArdaNova.Domain.Models.Enums;

namespace ArdaNova.Domain.Models.Entities;

[Table("PayoutRequest")]
public class PayoutRequest
{

    [Key]
    [Required]
    public string id { get; set; } = string.Empty;

    [Required]
    public string userId { get; set; } = string.Empty;

    public string? sourceProjectTokenConfigId { get; set; }

    [Required]
    public int sourceTokenAmount { get; set; }

    public int? ardaTokenAmount { get; set; }

    public double? usdAmount { get; set; }

    [Required]
    public PayoutStatus status { get; set; }

    [Required]
    public TokenHolderClass holderClass { get; set; }

    [Required]
    public ProjectGateStatus gateStatusAtRequest { get; set; }

    public string? conversionTxHash { get; set; }

    public string? payoutTxHash { get; set; }

    public string? stripePayoutId { get; set; }

    public string? failureReason { get; set; }

    [Required]
    public DateTime requestedAt { get; set; }

    public DateTime? processedAt { get; set; }

    public DateTime? completedAt { get; set; }

    [ForeignKey("userId")]
    public virtual User? User { get; set; }

    [ForeignKey("sourceProjectTokenConfigId")]
    public virtual ProjectTokenConfig? SourceProjectTokenConfig { get; set; }

}
