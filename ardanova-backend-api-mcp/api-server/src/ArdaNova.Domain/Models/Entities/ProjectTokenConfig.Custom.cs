using System.ComponentModel.DataAnnotations.Schema;
using ArdaNova.Domain.Models.Enums;

namespace ArdaNova.Domain.Models.Entities;

/// <summary>A project's stake, and the three node-side facts that make it real.</summary>
/// <remarks>The predicates below READ those facts; see <c>Models/Entities/AGENTS.md</c> section project token.</remarks>
public partial class ProjectTokenConfig
{
    /// <summary>
    /// True only when all three node legs are persisted: the minted asset, the treasury
    /// the project's funds land in, and the reserve partitions the node draws against.
    /// </summary>
    [NotMapped]
    public bool IsNodeProvisioned =>
        !string.IsNullOrWhiteSpace(assetId)
        && !string.IsNullOrWhiteSpace(treasuryAddress)
        && partitionsDeclaredAt.HasValue;

    /// <summary>The one fundability predicate every value path reads. A frozen or dissolved token is never fundable.</summary>
    [NotMapped]
    public bool IsFundable =>
        IsNodeProvisioned && status == ProjectTokenStatus.ACTIVE;
}
