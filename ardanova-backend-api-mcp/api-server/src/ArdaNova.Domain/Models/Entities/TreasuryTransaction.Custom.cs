namespace ArdaNova.Domain.Models.Entities;

/// <summary>
/// RETIRED. A per-project transaction ledger that was never written and is never read.
/// </summary>
/// <remarks>Replaced, not revived; see <c>Models/Entities/AGENTS.md</c> section treasury.</remarks>
public partial class TreasuryTransaction
{
    /// <summary>Materialisation only. Not public: nothing may write a row to a retired ledger.</summary>
    protected TreasuryTransaction()
    {
    }
}
