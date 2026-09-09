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
/// only reads two doubles ever could. The controls are the REFUSALS at the writing doors, all of
/// which state the unit rule in the words of <see cref="DescribeUnsupportedGoalUnit"/> and the
/// magnitude/precision rule in the words of <see cref="DescribeUnusableGoalAmount"/>:
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

    /// <summary>The smallest ask this platform accepts, in dollars: below a dollar there is nothing to raise.</summary>
    public const decimal MinimumGoalUsd = 1m;

    /// <summary>The largest ask this platform accepts, in dollars.</summary>
    public const decimal MaximumGoalUsd = 1_000_000_000m;

    /// <summary>Dollars carry two decimal places, so an ask cannot be finer than a cent.</summary>
    public const int GoalDecimals = 2;

    /// <summary>
    /// The one refusal every door that WRITES an ask states about its MAGNITUDE and PRECISION,
    /// beside <see cref="DescribeUnsupportedGoalUnit"/>, which states it about the unit.
    /// </summary>
    /// <remarks>
    /// The wizard already refuses these in the browser (`parseFundingGoalDollars`), and that was
    /// reported as "dollar min/max validation is enforced". It was not: `funding-tab.tsx` is a
    /// SECOND funding-goal field that parsed with a bare Number() and checked only `goal > 0`,
    /// the tRPC boundary took `z.number().positive()` with no ceiling and no cent rule, and this
    /// class validated only the UNIT. A $0.001 or a $1,000,000,000,000 ask reached the column
    /// the opening price divides through either of those doors. A bound a wizard applies is an
    /// affordance; a bound the writing door applies is a control.
    /// </remarks>
    /// <returns>Null when the amount is a usable dollar ask; the refusal to show otherwise.</returns>
    public static string? DescribeUnusableGoalAmount(double declaredGoal)
        => double.IsFinite(declaredGoal) && TryExact(declaredGoal, out var goal)
            ? DescribeUnusableGoalAmount(goal)
            : "Set a funding goal above zero.";

    /// <summary>
    /// The same refusal for a door whose ask is an OPTIONAL decimal - a project may be published
    /// with no ask at all, which is not the same as an ask of nothing.
    /// </summary>
    public static string? DescribeUnusableGoalAmount(decimal? declaredGoal)
        => declaredGoal is { } goal ? DescribeUnusableGoalAmount(goal) : null;

    /// <summary>The rule itself, on the exact figure. Every overload above lands here.</summary>
    public static string? DescribeUnusableGoalAmount(decimal declaredGoal)
    {
        if (declaredGoal <= 0m)
            return "Set a funding goal above zero.";

        if (declaredGoal < MinimumGoalUsd)
            return $"A funding goal must be at least {MinimumGoalUsd:C0}.";

        if (declaredGoal > MaximumGoalUsd)
            return $"A funding goal cannot be more than {MaximumGoalUsd:C0}.";

        if (decimal.Round(declaredGoal, GoalDecimals) != declaredGoal)
            return "A funding goal cannot be finer than a cent.";

        return null;
    }

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
