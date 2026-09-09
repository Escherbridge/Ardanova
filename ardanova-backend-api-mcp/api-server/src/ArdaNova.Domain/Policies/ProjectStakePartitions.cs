namespace ArdaNova.Domain.Policies;

using System;
using ArdaNova.Domain.Models.Entities;

/// <summary>
/// How much of a project's declared stake is still free to commit, in the ONE shape every
/// caller that draws from it must ask.
/// </summary>
/// <remarks>
/// Worker awards and sponsorships draw the same declared partitions, so an allowance policed
/// on one path and ignored on the other is not policed at all. The sponsorship path used to
/// count nothing: a settled sponsorship moved <c>fundingRaised</c> and left
/// <c>investorSupply</c> where it was, so the sponsor allowance could be drawn without limit
/// and the units a project had promised were never counted against what it had left.
/// </remarks>
public static class ProjectStakePartitions
{
    /// <summary>The allowance a worker award is drawn from, in the words a person is shown.</summary>
    public const string WorkerAllowance = "worker stake";

    /// <summary>The allowance a sponsorship is drawn from, in the words a person is shown.</summary>
    public const string SponsorAllowance = "sponsor stake";

    /// <summary>Mirrors the node's partition check so ArdaNova never promises units the node would refuse.</summary>
    public static string? DescribeOversubscription(
        string allowanceName,
        int declared,
        int committed,
        int requested)
    {
        if (requested <= 0)
            return $"A {allowanceName} allocation must be at least one unit.";

        if (committed + (long)requested > declared)
        {
            return $"The {allowanceName} allowance is oversubscribed: {requested} units requested, "
                + $"{Math.Max(declared - committed, 0)} of {declared} still available.";
        }

        return null;
    }

    /// <summary>Units of the whole supply nobody has committed yet.</summary>
    public static long UncommittedSupply(ProjectTokenConfig config) =>
        (long)config.totalSupply
        - config.contributorSupply
        - config.investorSupply
        - config.founderSupply
        - config.burnedSupply;

    /// <summary>
    /// Whether a sponsorship of <paramref name="requested"/> whole units can be committed: it
    /// must fit the declared sponsor allowance AND the supply nobody has claimed.
    /// </summary>
    public static string? DescribeSponsorDraw(ProjectTokenConfig config, int requested)
    {
        var oversubscribed = DescribeOversubscription(
            SponsorAllowance,
            config.investorPartitionSupply,
            config.investorSupply,
            requested);
        if (oversubscribed is not null)
            return oversubscribed;

        var uncommitted = UncommittedSupply(config);
        return requested > uncommitted
            ? $"There are only {Math.Max(uncommitted, 0)} units of this project's stake left to commit, "
                + $"and this sponsorship needs {requested}."
            : null;
    }
}
