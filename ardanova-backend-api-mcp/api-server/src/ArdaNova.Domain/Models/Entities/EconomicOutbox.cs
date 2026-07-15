using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using ArdaNova.Domain.Models.Enums;

namespace ArdaNova.Domain.Models.Entities;

[Index(nameof(settlementId), IsUnique = true)]
[Table("EconomicOutbox")]
public class EconomicOutbox
{

    [Key]
    [Required]
    public string id { get; set; } = string.Empty;

    [Required]
    public string settlementId { get; set; } = string.Empty;

    [Required]
    public EconomicOutboxStatus status { get; set; }

    [Required]
    public int payloadVersion { get; set; }

    [Required]
    public int attemptCount { get; set; }

    [Required]
    public DateTime availableAt { get; set; }

    public string? leaseToken { get; set; }

    public DateTime? leaseExpiresAt { get; set; }

    public DateTime? lastAttemptAt { get; set; }

    public DateTime? dispatchedAt { get; set; }

    public DateTime? reconciliationRequiredAt { get; set; }

    public string? failureCode { get; set; }

    public string? failureDetail { get; set; }

    [Required]
    public DateTime createdAt { get; set; }

    [Required]
    public DateTime updatedAt { get; set; }

    [ForeignKey("settlementId")]
    public virtual EconomicSettlement? Settlement { get; set; }

}
