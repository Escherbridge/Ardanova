using System.ComponentModel.DataAnnotations.Schema;

namespace ArdaNova.Domain.Models.Entities;

/// <summary>
/// One commitment of a project's stake to a person or a backlog item, and — once the node has
/// actually drawn it — the read-model of the HOLD that backs it.
/// </summary>
/// <remarks>The award of record is <see cref="tokenAmount"/>; see <c>Models/Entities/AGENTS.md</c> section token allocation.</remarks>
public partial class TokenAllocation
{
    /// <summary>True only when the node reported a live commitment for this allocation.</summary>
    [NotMapped]
    public bool IsNodeHeld => !string.IsNullOrWhiteSpace(holdId);

    /// <summary>True once the escrow hold has been continued into the worker's own window.</summary>
    [NotMapped]
    public bool IsAwaitingWorkerWindow => !string.IsNullOrWhiteSpace(workerHoldId);
}
