using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using ArdaNova.Domain.Models.Enums;

namespace ArdaNova.Domain.Models.Entities;

[Table("TokenAllocation")]
public class TokenAllocation
{

    [Key]
    [Required]
    public string id { get; set; } = string.Empty;

    [Required]
    public string projectTokenConfigId { get; set; } = string.Empty;

    public string? taskId { get; set; }

    public string? recipientUserId { get; set; }

    [Required]
    public double equityPercentage { get; set; }

    [Required]
    public int tokenAmount { get; set; }

    [Required]
    public AllocationStatus status { get; set; }

    [Required]
    public TokenHolderClass holderClass { get; set; }

    [Required]
    public bool isLiquid { get; set; }

    public DateTime? distributedAt { get; set; }

    public string? distributionTxHash { get; set; }

    public DateTime? burnedAt { get; set; }

    [Required]
    public DateTime createdAt { get; set; }

    [Required]
    public DateTime updatedAt { get; set; }

    [ForeignKey("projectTokenConfigId")]
    public virtual ProjectTokenConfig? ProjectTokenConfig { get; set; }

    [ForeignKey("taskId")]
    public virtual ProductBacklogItem? Task { get; set; }

    [ForeignKey("recipientUserId")]
    public virtual User? RecipientUser { get; set; }

}
