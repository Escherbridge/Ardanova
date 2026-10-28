namespace ArdaNova.Application.Tests.Feed;

using System.Text.RegularExpressions;
using ArdaNova.Application.Feed;
using ArdaNova.Domain.Models.Enums;
using FluentAssertions;

/// <summary>The mapping table from Application/Feed/AGENTS.md, as rows: who sees a card, and when a card must go.</summary>
public class FeedProjectionRulesTests
{
    private static readonly DateTime At = new(2026, 10, 27, 12, 0, 0, DateTimeKind.Utc);

    /// <summary>The client's verify-product-vocabulary list, for the copy the BFF writes itself.</summary>
    private static readonly Regex MachineryOrMoneyWord = new(
        @"\b(token|tokens|tokenomics|wallet|block ?chain|on[- ]?chain|off[- ]?chain|mint|mints|minted|minting|burn|burns|burned|nft|crypto|cryptocurrency|algorand|asa|gas fee|transaction hash|tx hash|web3|defi|dapp)\b",
        RegexOptions.IgnoreCase);

    /// <summary>One eligible row per FeedSourceType: a new kind without a rule fails here first.</summary>
    private static FeedSource EligibleSourceOf(FeedSourceType kind) => kind switch
    {
        FeedSourceType.PROJECT => new ProjectLaunchSource("p1", "Solar barn", "owner", At),
        FeedSourceType.PROJECT_UPDATE => new ProjectUpdateSource("u1", "p1", "owner", "Roof is on", At),
        FeedSourceType.PROJECT_MILESTONE => new ProjectMilestoneSource("m1", "p1", "owner", "Foundation", At),
        FeedSourceType.PROJECT_TASK => new ProjectTaskSource("t1", "p1", "owner", "Order timber", At),
        FeedSourceType.PROJECT_TOKEN_CONFIG => new ProjectFundingSource("c1", "p1", "owner", "Solar barn", 1000, 600, At),
        FeedSourceType.PROJECT_MEMBER => new ProjectMemberSource("pm1", "p1", "owner", "Solar barn", "member", At),
        FeedSourceType.PROJECT_COMMENT => new ProjectCommentSource("pc1", "p1", "member", null, CommentTargetType.PROJECT, "p1", At),
        FeedSourceType.GUILD => new GuildFoundedSource("g1", "Timber guild", "owner", At),
        FeedSourceType.GUILD_UPDATE => new GuildUpdateSource("gu1", "g1", "owner", "New kiln", At),
        FeedSourceType.GUILD_MEMBER => new GuildMemberSource("gm1", "g1", "owner", "Timber guild", "member", At),
        FeedSourceType.OPPORTUNITY => new OpportunitySource("o1", "Carpenter", "owner", OpportunityStatus.OPEN, "p1", null, At),
        FeedSourceType.OPPORTUNITY_UPDATE => new OpportunityUpdateSource("ou1", "o1", "owner", "Shortlist", At, OpportunityStatus.OPEN, "p1", null),
        FeedSourceType.OPPORTUNITY_COMMENT => new OpportunityCommentSource("oc1", "o1", "member", null, At, OpportunityStatus.OPEN, "p1", null),
        FeedSourceType.EVENT => new EventSource("e1", "Barn raising", "owner", EventStatus.SCHEDULED, EventVisibility.PUBLIC, "p1", null, At, At),
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Add an eligible row for this new source kind."),
    };

    public static TheoryData<FeedSourceType> EverySourceKind()
    {
        var data = new TheoryData<FeedSourceType>();
        foreach (var kind in Enum.GetValues<FeedSourceType>())
            data.Add(kind);
        return data;
    }

    [Theory]
    [MemberData(nameof(EverySourceKind))]
    public void EverySourceKind_ProjectsOneEnvelopeKeyedByItsRow_InPlainProductLanguage(FeedSourceType kind)
    {
        var envelope = FeedProjectionRules.Enter(EligibleSourceOf(kind));

        envelope.Should().NotBeNull();
        envelope!.Key.SourceType.Should().Be(kind);
        FeedProjectionRules.RowIdOf(envelope.Key).Should().Be(EligibleSourceOf(kind).Id,
            "the sweep finds a card's source through this id");
        MachineryOrMoneyWord.IsMatch(envelope.Content).Should().BeFalse(
            $"'{envelope.Content}' is member-facing copy and must say funds or units, never the machinery");
        envelope.Type.Should().NotBe(PostType.POST, "a projected card is never mistaken for a person's post");
    }

    public static TheoryData<string, FeedSource, PostVisibility?> AudienceRows() => new()
    {
        { "project-level talk is public", new ProjectCommentSource("c", "p", "a", null, CommentTargetType.PROJECT, "p", At), PostVisibility.PUBLIC },
        { "milestone talk is public", new ProjectCommentSource("c", "p", "a", null, CommentTargetType.MILESTONE, "m", At), PostVisibility.PUBLIC },
        { "backlog-item talk stays with the team", new ProjectCommentSource("c", "p", "a", null, CommentTargetType.PBI, "b", At), PostVisibility.PROJECT_MEMBERS },
        { "task talk stays with the team", new ProjectCommentSource("c", "p", "a", null, CommentTargetType.TASK, "t", At), PostVisibility.PROJECT_MEMBERS },
        { "epic talk stays with the team", new ProjectCommentSource("c", "p", "a", null, CommentTargetType.EPIC, "e", At), PostVisibility.PROJECT_MEMBERS },
        { "a reply lives in its native thread", new ProjectCommentSource("c", "p", "a", "parent", CommentTargetType.PROJECT, "p", At), null },
        { "an opportunity reply lives in its native thread", new OpportunityCommentSource("c", "o", "a", "parent", At, OpportunityStatus.OPEN, null, null), null },
        { "talk on a draft opportunity stays private", new OpportunityCommentSource("c", "o", "a", null, At, OpportunityStatus.DRAFT, null, null), null },
        { "an update on a draft opportunity stays private", new OpportunityUpdateSource("u", "o", "a", "t", At, OpportunityStatus.DRAFT, null, null), null },
        { "a draft opportunity is never opened", new OpportunitySource("o", "t", "a", OpportunityStatus.DRAFT, null, null, At), null },
        { "an opportunity under review was not just opened", new OpportunitySource("o", "t", "a", OpportunityStatus.IN_REVIEW, null, null, At), null },
        { "a public event is public", Event(EventStatus.SCHEDULED, EventVisibility.PUBLIC), PostVisibility.PUBLIC },
        { "a live event is still on", Event(EventStatus.LIVE, EventVisibility.PUBLIC), PostVisibility.PUBLIC },
        { "a project-members event keeps its audience", Event(EventStatus.SCHEDULED, EventVisibility.PROJECT_MEMBERS, projectId: "p"), PostVisibility.PROJECT_MEMBERS },
        { "a guild-members event keeps its audience", Event(EventStatus.SCHEDULED, EventVisibility.GUILD_MEMBERS, guildId: "g"), PostVisibility.GUILD_MEMBERS },
        { "a members-only event with no scope has no audience", Event(EventStatus.SCHEDULED, EventVisibility.GUILD_MEMBERS), null },
        { "an invite-only event is never projected", Event(EventStatus.SCHEDULED, EventVisibility.INVITE_ONLY, projectId: "p"), null },
        { "nor while it is live", Event(EventStatus.LIVE, EventVisibility.INVITE_ONLY), null },
        { "a draft event is not scheduled", Event(EventStatus.DRAFT, EventVisibility.PUBLIC), null },
        { "the founder is not news on their own project", new ProjectMemberSource("m", "p", "owner", "t", "owner", At), null },
        { "the owner is not news in their own guild", new GuildMemberSource("m", "g", "owner", "t", "owner", At), null },
        { "an unfinished milestone has no card", new ProjectMilestoneSource("m", "p", "owner", "t", null), null },
        { "an unpublished project has not launched", new ProjectLaunchSource("p", "t", "owner", null), null },
    };

    [Theory]
    [MemberData(nameof(AudienceRows))]
    public void EachSource_EntersTheFeedOnlyForItsOwnAudience(string because, FeedSource source, PostVisibility? expected)
    {
        PostVisibility? audience = FeedProjectionRules.Enter(source)?.Visibility;

        audience.Should().Be(expected, because);
    }

    public static TheoryData<string, FeedSource, PostVisibility?> RetentionRows() => new()
    {
        { "an opportunity that later closed did open", new OpportunitySource("o", "t", "a", OpportunityStatus.CLOSED, null, null, At), PostVisibility.PUBLIC },
        { "an opportunity sent back to draft leaves", new OpportunitySource("o", "t", "a", OpportunityStatus.DRAFT, null, null, At), null },
        { "a finished event did happen", Event(EventStatus.COMPLETED, EventVisibility.PUBLIC), PostVisibility.PUBLIC },
        { "a cancelled event leaves", Event(EventStatus.CANCELLED, EventVisibility.PUBLIC), null },
        { "an event made invite-only leaves", Event(EventStatus.SCHEDULED, EventVisibility.INVITE_ONLY), null },
        { "an event narrowed to its guild follows it", Event(EventStatus.SCHEDULED, EventVisibility.GUILD_MEMBERS, guildId: "g"), PostVisibility.GUILD_MEMBERS },
        { "a reopened task loses its card", new ProjectTaskSource("t", "p", "owner", "t", null), null },
        { "a funding mark, once reached, stays reached", new ProjectFundingSource("c", "p", "owner", "t", 1000, 10, At), PostVisibility.PUBLIC },
    };

    [Theory]
    [MemberData(nameof(RetentionRows))]
    public void AStoredCard_StaysOnlyWhileItsSourceStillEarnsIt(string because, FeedSource source, PostVisibility? expected)
        => FeedProjectionRules.Retain(source).Should().Be(expected, because);

    [Theory]
    [InlineData(1000, 0, null)]
    [InlineData(1000, 249.99, null)]
    [InlineData(1000, 250, 25)]
    [InlineData(1000, 600, 50)]
    [InlineData(1000, 999.99, 75)]
    [InlineData(1000, 1000, 100)]
    [InlineData(1000, 4000, 100)]
    [InlineData(0, 500, null)]
    [InlineData(1000, -5, null)]
    public void FundingProgress_IsTheHighestMarkReached(double goal, double raised, int? expected)
        => FeedProjectionRules.HighestFundingThreshold(goal, raised).Should().Be(expected);

    [Fact]
    public void AFundingCard_CarriesOnlyThePercent()
    {
        var envelope = FeedProjectionRules.Enter(
            new ProjectFundingSource("cfg-1", "p1", "owner", "Solar barn", 1234.56, 700.01, At))!;

        envelope.Key.Should().Be(new FeedSourceKey(FeedSourceType.PROJECT_TOKEN_CONFIG, "cfg-1:50"));
        envelope.Metadata.Should().Be("""{"percent":50}""");
        envelope.Content.Should().Be("Reached 50% of its funding goal.");
        $"{envelope.Title}{envelope.Content}{envelope.Metadata}".Should().NotContain("1234").And.NotContain("700");
    }

    private static EventSource Event(
        EventStatus status,
        EventVisibility visibility,
        string? projectId = null,
        string? guildId = null)
        => new("e", "t", "organizer", status, visibility, projectId, guildId, At, At);
}
