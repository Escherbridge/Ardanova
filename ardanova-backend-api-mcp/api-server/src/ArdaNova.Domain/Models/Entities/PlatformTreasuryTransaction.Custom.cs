using System.ComponentModel.DataAnnotations.Schema;

namespace ArdaNova.Domain.Models.Entities;

/// <summary>A treasury RECEIPT: what ArdaNova recorded about a move the node actually made.</summary>
/// <remarks>Not a ledger of record; see <c>Models/Entities/AGENTS.md</c> section treasury receipts.</remarks>
public partial class PlatformTreasuryTransaction
{
    /// <summary>True only when this row points at a node operation that can be reconciled.</summary>
    [NotMapped]
    public bool IsChainReceipt => !string.IsNullOrWhiteSpace(azoaOperationId);
}
