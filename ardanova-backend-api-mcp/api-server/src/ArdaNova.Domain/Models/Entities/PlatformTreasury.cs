using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using ArdaNova.Domain.Models.Enums;

namespace ArdaNova.Domain.Models.Entities;

[Table("PlatformTreasury")]
public class PlatformTreasury
{

    [Key]
    [Required]
    public string id { get; set; } = string.Empty;

    [Required]
    public long ardaTotalSupply { get; set; }

    [Required]
    public long ardaCirculatingSupply { get; set; }

    public string? ardaAssetId { get; set; }

    public string? ardaMintTxHash { get; set; }

    [Required]
    public double indexFundBalance { get; set; }

    [Required]
    public double liquidReserveBalance { get; set; }

    [Required]
    public double operationsBalance { get; set; }

    [Required]
    public double indexFundAllocationPct { get; set; }

    [Required]
    public double liquidReserveAllocationPct { get; set; }

    [Required]
    public double operationsAllocationPct { get; set; }

    [Required]
    public double indexFundAnnualReturn { get; set; }

    [Required]
    public double platformProfitSharePct { get; set; }

    [Required]
    public double trustProtectionRate { get; set; }

    [Required]
    public double totalInflows { get; set; }

    [Required]
    public double totalPayouts { get; set; }

    [Required]
    public double totalRebalanceTransfers { get; set; }

    public DateTime? lastRebalanceAt { get; set; }

    public DateTime? lastReconciliationAt { get; set; }

    [Required]
    public DateTime updatedAt { get; set; }

}
