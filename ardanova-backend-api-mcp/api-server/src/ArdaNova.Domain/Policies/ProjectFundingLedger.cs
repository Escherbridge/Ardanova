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
/// The correction is not a comment: the wire now carries the unit
/// (<c>ProvisionProjectFundingRequest.FundingGoalUnit</c>), provisioning refuses anything but
/// dollars by name, and every read of the pair goes through this one type. A future writer
/// that puts a platform-unit figure back into either column fails at the door rather than
/// silently re-pricing a raise.
/// </para>
/// </remarks>
public static class ProjectFundingLedger
{
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
