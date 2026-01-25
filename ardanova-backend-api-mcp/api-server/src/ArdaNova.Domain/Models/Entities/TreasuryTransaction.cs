using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ArdaNova.Domain.Models.Enums;

namespace ArdaNova.Domain.Models.Entities;

[Table("TreasuryTransaction")]
public class TreasuryTransaction
{
    [Key]
    [Required]
    public string id { get; set; } = string.Empty;

    [Required]
    public string treasuryId { get; set; } = string.Empty;

    [Required]
    public TransactionType type { get; set; }

    [Required]
    public decimal amount { get; set; }

    public string? description { get; set; }

    public string? txHash { get; set; }

    public string? proposalId { get; set; }

    [Required]
    public DateTime createdAt { get; set; }

    [ForeignKey("treasuryId")]
    public virtual Treasury? Treasury { get; set; }

}
