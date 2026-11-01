using System.ComponentModel.DataAnnotations.Schema;

namespace ArdaNova.Domain.Models.Entities;

/// <summary>A holder's units in one asset — a READ-MODEL of chain truth, not the ledger of record.</summary>
/// <remarks>See <c>Models/Entities/AGENTS.md</c> section token balance.</remarks>
public partial class TokenBalance
{
    /// <summary>True only when a chain read produced this row against a named asset.</summary>
    [NotMapped]
    public bool IsChainObserved =>
        observedAt.HasValue && !string.IsNullOrWhiteSpace(assetId);

    /// <summary>Base units free to move.</summary>
    [NotMapped]
    public long AvailableBalance => balance - lockedBalance;
}
