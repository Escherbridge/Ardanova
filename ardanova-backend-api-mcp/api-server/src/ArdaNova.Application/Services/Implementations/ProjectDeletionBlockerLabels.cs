namespace ArdaNova.Application.Services.Implementations;

/// <summary>Plain-language names for the tables that stop a project delete. See Services/Implementations/AGENTS.md §Project delete.</summary>
public static class ProjectDeletionBlockerLabels
{
    /// <summary>One label per entity whose required FK to Project is RESTRICT; a test keeps this in step with the EF model.</summary>
    public static readonly IReadOnlyDictionary<string, string> Labels = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["DelegatedVote"] = "vote delegations",
        ["Epic"] = "epics",
        ["EquityOrRedemptionRightPolicy"] = "an equity or redemption policy",
        ["Feature"] = "features",
        ["FundingIntent"] = "funding intents",
        ["Product"] = "products",
        ["ProductBacklogItem"] = "backlog items",
        ["ProjectApplication"] = "applications",
        ["ProjectComment"] = "comments",
        ["ProjectEquity"] = "equity records",
        ["ProjectFollow"] = "followers",
        ["ProjectInvitation"] = "invitations",
        ["ProjectMember"] = "members",
        ["ProjectMembershipRequest"] = "membership requests",
        ["ProjectMilestone"] = "milestones",
        ["ProjectResource"] = "resources",
        ["ProjectShare"] = "share holdings",
        ["ProjectSupport"] = "supporters",
        ["ProjectTask"] = "tasks",
        ["ProjectTokenConfig"] = "a token configuration",
        ["ProjectTokenPolicy"] = "a token policy",
        ["ProjectUpdate"] = "updates",
        ["ProjectWallet"] = "wallets",
        ["Proposal"] = "governance proposals",
        ["Sprint"] = "sprints",
        ["TaskCommerceAgreement"] = "task agreements",
        ["Treasury"] = "a treasury",
    };

    public static string Describe(string kind) => Labels.TryGetValue(kind, out var label) ? label : kind;
}
