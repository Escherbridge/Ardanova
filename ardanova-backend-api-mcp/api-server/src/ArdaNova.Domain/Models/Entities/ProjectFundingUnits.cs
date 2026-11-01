using System;

namespace ArdaNova.Domain.Models.Entities;

/// <summary>Units a <see cref="Project.fundingGoal"/> may be denominated in.</summary>
public static class ProjectFundingUnits
{
    /// <summary>The platform's own unit. It settles sponsorships; it never denominates an ask.</summary>
    public const string Arda = "ARDA";

    /// <summary>The unit a person types an ask in and reads a total in.</summary>
    public const string Usd = "USD";

    /// <summary>
    /// The one unit both funding columns are kept in - <c>Project.fundingGoal</c>,
    /// <c>ProjectTokenConfig.fundingGoal</c> and <c>ProjectTokenConfig.fundingRaised</c>.
    /// Read through <c>ProjectFundingLedger</c>, never compared column to column by hand.
    /// </summary>
    public const string Ledger = Usd;

    /// <summary>Whether a declared unit is the one this node keeps its funding figures in.</summary>
    public static bool IsLedgerUnit(string? unit) =>
        string.Equals(unit?.Trim(), Ledger, StringComparison.OrdinalIgnoreCase);
}
