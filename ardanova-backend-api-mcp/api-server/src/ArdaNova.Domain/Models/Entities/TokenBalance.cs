using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using ArdaNova.Domain.Models.Enums;

namespace ArdaNova.Domain.Models.Entities;

/// <summary>A holder's units in one asset — a READ-MODEL of chain truth, not the ledger of record.</summary>
/// <remarks>
/// The balance of record for a person's platform unit is their custodial wallet on the
/// node. These rows are ArdaNova's own bookkeeping until the node holds the units (W7.2),
/// which is what the three provenance columns below make checkable.
/// </remarks>
[Table("TokenBalance")]
public class TokenBalance
{

    [Key]
    [Required]
    public string id { get; set; } = string.Empty;

    [Required]
    public string userId { get; set; } = string.Empty;

    public string? projectTokenConfigId { get; set; }

    /// <summary>Legacy marker. It NO LONGER locates a person's ARDA: that is read from their custodial wallet.</summary>
    [Required]
    public bool isPlatformToken { get; set; }

    public TokenHolderClass? holderClass { get; set; }

    [Required]
    public bool isLiquid { get; set; }

    /// <summary>Units in the asset's BASE units. Fixed scale, never a float, never narrowed to int.</summary>
    [Required]
    public long balance { get; set; }

    /// <summary>Base units committed elsewhere and therefore unavailable to move.</summary>
    [Required]
    public long lockedBalance { get; set; }

    [Required]
    public DateTime updatedAt { get; set; }

    // -- Chain provenance (W5.3) -------------------------------------------
    // Three facts that say whether this row is chain truth or only ArdaNova's
    // own bookkeeping. Nothing stores a separate "is real" flag: IsChainObserved
    // is DERIVED, so it can never disagree with what was actually recorded.

    /// <summary>The on-chain asset these units count; null means this row was never resolved to one.</summary>
    public string? assetId { get; set; }

    /// <summary>Custodial address the units were observed at.</summary>
    public string? walletAddress { get; set; }

    /// <summary>When a chain read produced these units. Null means never observed.</summary>
    public DateTime? observedAt { get; set; }

    /// <summary>True only when a chain read produced this row against a named asset.</summary>
    [NotMapped]
    public bool IsChainObserved =>
        observedAt.HasValue && !string.IsNullOrWhiteSpace(assetId);

    /// <summary>Base units free to move.</summary>
    [NotMapped]
    public long AvailableBalance => balance - lockedBalance;

    [ForeignKey("userId")]
    public virtual User? User { get; set; }

    [ForeignKey("projectTokenConfigId")]
    public virtual ProjectTokenConfig? ProjectTokenConfig { get; set; }

}
