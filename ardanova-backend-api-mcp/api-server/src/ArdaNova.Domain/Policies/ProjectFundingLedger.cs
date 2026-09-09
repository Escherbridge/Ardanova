namespace ArdaNova.Domain.Policies;

using ArdaNova.Domain.Models.Entities;

/// <summary>
/// A project's ask and the progress against it, read as ONE unit by everything that compares
/// or divides them.
/// </summary>
/// <remarks>
/// <para>
/// <c>ProjectTokenConfig.fundingGoal</c> and <c>ProjectTokenConfig.fundingRaised</c> are both
/// US DOLLARS (<see cref="ProjectFundingUnits.Ledger"/>). That used to be true of only one of
/// them: the raise accumulated the sponsorship's own dollar reference while the goal carried
/// the founder's ARDA figure, so the opening offer divided ARDA into a field declared as
/// dollars and the first sponsor of every project was charged about a hundred times the
/// founder's price for a hundredth of the promised units.
/// </para>
/// <para>
/// This type READS the pair; it cannot tell what unit a stored double is in, and nothing that
/// only reads two doubles ever could. The controls are the three REFUSALS at the writing doors,
/// all of which state the rule in the words of <see cref="DescribeUnsupportedGoalUnit"/>:
/// <c>ProjectTokenService.ValidateDeclaration</c> (the owner's declaration),
/// <c>ProjectServices.CreateAsync</c>/<c>UpdateAsync</c> (the project's own ask) and
/// <c>ProjectTokenService.CreateConfigAsync</c> (the operator's admin door). A writer that
/// bypasses all three still gets a mis-priced raise - the fix for that is another refusal at
/// that door, not a claim made here.
/// </para>
/// </remarks>
public static class ProjectFundingLedger
{
    /// <summary>
    /// The one refusal every door that WRITES an ask states, so a goal can only ever be stored in
    /// the unit the raise is compared and divided in. A goal in any other unit sits beside a
    /// dollar total and is divided into a dollar price.
    /// </summary>
    /// <returns>Null when the declared unit is the ledger unit; the refusal to show otherwise.</returns>
    public static string? DescribeUnsupportedGoalUnit(string? declaredUnit) =>
        ProjectFundingUnits.IsLedgerUnit(declaredUnit)
            ? null
            : $"A funding goal must be declared in {ProjectFundingUnits.Ledger}; "
                + $"'{declaredUnit}' was sent, and a goal in any other unit would be priced "
                + "against a dollar total.";

    /// <summary>
    /// Reads both figures as exact decimals, or refuses. A stored double that is not finite, or
    /// that no decimal can hold, has no honest reading - so it is refused by name rather than
    /// throwing on a value path.
    /// </summary>
    public static bool TryRead(ProjectTokenConfig config, out ProjectFundingFigures figures)
    {
        figures = default;

        if (!TryExact(config.fundingGoal, out var goal) || !TryExact(config.fundingRaised, out var raised))
        {
            return false;
        }

        figures = new ProjectFundingFigures(goal, raised);
        return true;
    }

    /// <summary>Whether the raise has reached the ask. The one place the two columns are compared.</summary>
    public static bool GoalIsMet(ProjectTokenConfig config) =>
        TryRead(config, out var figures) && figures.GoalIsMet;

    private static bool TryExact(double stored, out decimal exact)
    {
        exact = 0m;
        if (!double.IsFinite(stored))
        {
            return false;
        }

        try
        {
            exact = (decimal)stored;
            return true;
        }
        catch (OverflowException)
        {
            return false;
        }
    }
}

/// <summary>What a project asked for and what it has taken, both in US dollars.</summary>
/// <param name="GoalUsd">The founder's declared ask. Zero or less means no ask was declared.</param>
/// <param name="RaisedUsd">Dollars actually settled into the raise. Negative is a broken figure.</param>
public readonly record struct ProjectFundingFigures(decimal GoalUsd, decimal RaisedUsd)
{
    /// <summary>A project with no declared ask has no opening price.</summary>
    public bool GoalIsSet => GoalUsd > 0m;

    /// <summary>Nothing has settled yet. Not the same as a broken figure.</summary>
    public bool NothingRaised => RaisedUsd == 0m;

    /// <summary>A figure below zero is broken, not unsponsored.</summary>
    public bool RaisedIsBroken => RaisedUsd < 0m;

    /// <summary>The primary raise is still open: an ask exists and it has not been reached.</summary>
    public bool RaiseIsOpen => GoalIsSet && RaisedUsd < GoalUsd;

    /// <summary>The ask has been reached. Gate 1's whole question.</summary>
    public bool GoalIsMet => GoalIsSet && RaisedUsd >= GoalUsd;
}
