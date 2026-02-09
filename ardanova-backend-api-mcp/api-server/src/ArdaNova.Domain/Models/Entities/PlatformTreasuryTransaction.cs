using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using ArdaNova.Domain.Models.Enums;

namespace ArdaNova.Domain.Models.Entities;

[Table("PlatformTreasuryTransaction")]
public class PlatformTreasuryTransaction
{

    [Key]
    [Required]
    public string id { get; set; } = string.Empty;

    [Required]
    public PlatformTreasuryTransactionType type { get; set; }

    [Required]
    public double amount { get; set; }

    public string? fromBucket { get; set; }

    public string? toBucket { get; set; }

    public string? relatedProjectId { get; set; }

    public string? relatedPayoutRequestId { get; set; }

    public string? description { get; set; }

    [Required]
    public double balanceAfter { get; set; }

    [Required]
    public DateTime createdAt { get; set; }

}
