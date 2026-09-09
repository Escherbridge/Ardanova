namespace ArdaNova.Domain.Policies;

using ArdaNova.Domain.Models.Entities;

/// <summary>
/// What a project is actually SELLING in its primary raise, and when that raise is finished.
/// The one type the price and the gate both read, so the two can never disagree about it.
/// </summary>
/// <remarks>
/// <para>
/// A project declares an ask in dollars AND a split of its stake: some units for the people who
/// fund the work, some for the people who do it, some kept by the founder. Only the SPONSOR
/// partition is on offer - workers are paid in stake, they do not buy it - so the ask is priced
/// over the sponsor partition and nothing else.
/// </para>
/// <para>
/// The invariant that makes the raise reachable: <c>offering price x units on offer == the ask</c>.
/// Pricing the ask over the WHOLE supply instead capped every raise at
/// <c>(sponsor partition / total supply) x ask</c>, which is strictly below the ask for any project
/// that sets aside a single unit for a worker - so Gate 1 could never clear, sponsors paid real
/// dollars into a raise that could not complete, and every worker's stake stayed frozen forever
/// (<see cref="TokenLiquidityPolicy"/> makes contributor units liquid only from ACTIVE). The only
/// shape that still worked was a project that reserved nothing for workers: the opposite of the
/// point.
/// </para>
/// <para>
/// <see cref="RaiseIsComplete"/> therefore has TWO clearing conditions, not one. The dollar ask
/// being met is the headline; the sponsor stake being SOLD OUT is the backstop, because each
/// sponsorship's dollar reference is FLOORED to the cent (<c>FundingIntentService.TryQuote</c>),
/// so selling all 600 units of a $50,000 offering at $83.333... a unit can settle a few cents
/// short of the ask forever. A raise with nothing left to sell is finished whatever the rounding
/// says.
/// </para>
/// </remarks>
public static class ProjectRaiseOffering
{
    /// <summary>
    /// The stake on offer in the primary raise - the declared sponsor partition - or false when a
    /// project has not set any aside. A project selling nothing has no price; it is refused by name
    /// rather than divided by zero or quietly priced over some other denominator.
    /// </summary>
    public static bool TryUnitsOnOffer(ProjectTokenConfig config, out int unitsOnOffer)
    {
        unitsOnOffer = config.investorPartitionSupply;
        return unitsOnOffer > 0;
    }

    /// <summary>
    /// Every unit a project offered sponsors has been taken. Requires a partition to have been
    /// DECLARED: a config whose sponsor partition is 0 has not sold out, it has sold nothing, and
    /// treating <c>0 &gt;= 0</c> as sold out would clear Gate 1 on a project that never took a cent.
    /// </summary>
    public static bool SponsorStakeIsSoldOut(ProjectTokenConfig config) =>
        config.investorPartitionSupply > 0
        && config.investorSupply >= config.investorPartitionSupply;

    /// <summary>
    /// The primary raise is finished: an ask was declared, and either it has been met or there is
    /// no sponsor stake left to sell AND that stake was actually paid for. Gate 1's whole
    /// question, read here rather than hand-rolled.
    /// </summary>
    /// <remarks>
    /// The sold-out branch carries <c>!NothingRaised</c> because <c>investorSupply</c> has a
    /// second writer - <c>ProjectTokenService.AllocateToInvestorAsync</c>, the operator's grant
    /// route - and a branch that read committed UNITS alone would clear the gate on a project
    /// that had taken no money at all. Sold out has to mean sold.
    /// </remarks>
    public static bool RaiseIsComplete(ProjectTokenConfig config) =>
        ProjectFundingLedger.TryRead(config, out var figures)
        && figures.GoalIsSet
        && !figures.RaisedIsBroken
        && (figures.GoalIsMet || (SponsorStakeIsSoldOut(config) && !figures.NothingRaised));
}
