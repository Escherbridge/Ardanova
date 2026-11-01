namespace ArdaNova.Application.Tests.Feed;

using ArdaNova.Application.Feed;
using ArdaNova.Domain.Models.Enums;
using FluentAssertions;

/// <summary>The ranker's pure rules, table-driven: decay, the reply cap, affinity, novelty, caps and discovery slots.</summary>
public class FeedRankingTests
{
    private static readonly DateTime AsOf = new(2026, 10, 31, 12, 0, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData(0, 1.0)]
    [InlineData(48, 0.5)]
    [InlineData(96, 0.25)]
    [InlineData(-5, 1.0)]
    public void Decay_HalvesEveryHalfLife_AndNeverRewardsAFutureDate(double hoursOld, double expected)
        => FeedRanking.Decay(AsOf.AddHours(-hoursOld), AsOf).Should().BeApproximately(expected, 1e-9);

    public static TheoryData<FeedQualitySignals, FeedQualitySignals, string> HigherQuality => new()
    {
        { new(Approvals: 3, Replies: 0, Shares: 0, Bookmarks: 0), new(1, 0, 0, 0), "more approvals" },
        { new(2, 2, 0, 0), new(2, 1, 0, 0), "replies count while approvals vouch for them" },
        { new(0, 0, 1, 0), new(0, 0, 0, 1), "a share is a stronger endorsement than a bookmark" },
        { new(0, 0, 0, 1), new(0, 30, 0, 0), "a bookmark beats thirty unvouched replies" },
    };

    [Theory]
    [MemberData(nameof(HigherQuality))]
    public void Quality_RanksAllowedSignals(FeedQualitySignals higher, FeedQualitySignals lower, string because)
        => FeedQualityScore.Compute(higher).Should().BeGreaterThan(FeedQualityScore.Compute(lower), because);

    [Theory]
    [InlineData(2, 3)]
    [InlineData(2, 50)]
    [InlineData(0, 20)]
    public void Quality_CapsRepliesAtApprovals_SoAFlameThreadCannotClimbOnVolume(int approvals, int replies)
        => FeedQualityScore.Compute(new(approvals, replies, 0, 0))
            .Should().Be(FeedQualityScore.Compute(new(approvals, approvals, 0, 0)));

    [Fact]
    public void Novelty_IsAppliedAtReadTime_SoANewProjectsCardOutranksAnOldOnesForAnyReader()
    {
        var fresh = Rank(Card("a", project: "p1", publishedAt: AsOf.AddDays(-1)), FeedViewer.Anonymous);
        var old = Rank(Card("b", project: "p2", publishedAt: AsOf.AddDays(-20)), FeedViewer.Anonymous);

        fresh.Score.Should().BeGreaterThan(old.Score);
        fresh.Reasons.Should().Contain(reason => reason.Kind == FeedReasonKind.NewProject);
    }

    [Fact]
    public void Affinity_PutsAFollowedProjectsCardAboveAStrangersOfEqualQualityAndAge()
    {
        var viewer = Viewer(followedProjects: ["project-followed"]);
        var followed = Rank(Card("a", project: "project-followed"), viewer);
        var stranger = Rank(Card("b", project: "project-other"), viewer);

        followed.Score.Should().BeGreaterThan(stranger.Score);
        followed.Reasons.Should().ContainSingle().Which.Should().Be(
            new FeedReason(FeedReasonKind.FollowedProject, FeedReasonSubject.Project, "project-followed"));
        followed.IsDiscovery.Should().BeFalse();
        stranger.IsDiscovery.Should().BeTrue();
        stranger.Reasons[0].Kind.Should().Be(FeedReasonKind.Discovery);
    }

    [Fact]
    public void AnInterestTag_IsAffinity_NotDiscovery_AndNamesTheTag()
    {
        var viewer = Viewer(interests: [new FeedTopic("skill:CARPENTRY")]);
        var ranked = Rank(Card("a", project: "project-other", hashtags: ["carpentry"]), viewer);

        ranked.IsDiscovery.Should().BeFalse();
        ranked.Reasons.Should().Contain(new FeedReason(FeedReasonKind.TagInterest, FeedReasonSubject.Tag, "carpentry"));
        ranked.Score.Should().BeGreaterThan(Rank(Card("b", project: "project-other"), viewer).Score);
    }

    [Fact]
    public void Novelty_LiftsANewProjectInTheViewersSector_OverTheSameCardFromAnOldOne()
    {
        var viewer = Viewer(interests: [FeedTopic.Sector("CONSTRUCTION")]);
        var fresh = Rank(Card("a", project: "p1", sectors: ["CONSTRUCTION"], publishedAt: AsOf.AddDays(-1)), viewer);
        var old = Rank(Card("b", project: "p2", sectors: ["CONSTRUCTION"], publishedAt: AsOf.AddDays(-60)), viewer);

        fresh.Score.Should().BeGreaterThan(old.Score);
        fresh.Reasons.Should().Contain(new FeedReason(FeedReasonKind.NewInSector, FeedReasonSubject.Sector, "CONSTRUCTION"));
    }

    [Fact]
    public void SelectPage_HoldsTwoPerSubjectAndThreePerAuthor_WhileOtherCardsCanFillThePage()
    {
        // Twelve subjects of five consecutive cards each, so recency alone would stack them; "author-0" wrote the
        // newest card of every subject, so recency alone would also give that author ten slots.
        var cards = Enumerable.Range(0, 60)
            .Select(index => Card($"post-{index:D2}", project: $"project-{index / 5}",
                author: index % 5 == 0 ? "author-0" : $"author-{index}", minutesOld: index))
            .ToList();

        var page = Select(cards, new FeedPagePolicy(20, 2, 3, null, RelaxCapsWhenShort: false));

        page.Should().HaveCount(20);
        page.GroupBy(placement => placement.Card.Candidate.SubjectKey).Should().OnlyContain(group => group.Count() <= 2);
        page.GroupBy(placement => placement.Card.Candidate.AuthorId).Should().OnlyContain(group => group.Count() <= 3);
    }

    [Fact]
    public void SelectPage_GivesEveryFifthSlotToDiscovery_EvenWhenFollowedCardsOutrankIt()
    {
        var viewer = Viewer(followedPeople: Enumerable.Range(0, 30).Select(index => $"followed-{index}").ToArray());
        var followed = Enumerable.Range(0, 30).Select(index =>
            Card($"f-{index:D2}", project: $"pf-{index}", author: $"followed-{index}", minutesOld: index));
        var discovery = Enumerable.Range(0, 10).Select(index =>
            Card($"d-{index:D2}", project: $"pd-{index}", author: $"stranger-{index}", minutesOld: 1000 + index));

        var page = Select(followed.Concat(discovery), new FeedPagePolicy(20, 2, 3, 5, false), viewer);

        page.Select(placement => placement.Card.IsDiscovery).Should().Equal(
            Enumerable.Range(1, 20).Select(slot => slot % 5 == 0));
        page.Where(placement => placement.InDiscoverySlot).Should().HaveCount(4);
    }

    [Fact]
    public void SelectPage_FillsADiscoverySlotWithTheNextBestCard_WhenNothingUnfollowedIsLeft()
    {
        var viewer = Viewer(followedProjects: Enumerable.Range(0, 8).Select(index => $"p-{index}").ToArray());
        var cards = Enumerable.Range(0, 8).Select(index =>
            Card($"f-{index}", project: $"p-{index}", author: $"author-{index}", minutesOld: index));

        var page = Select(cards, new FeedPagePolicy(6, 2, 3, 5, false), viewer);

        page.Should().HaveCount(6, "a discovery slot with no discovery card left is not a hole in the page");
        page.Should().OnlyContain(placement => !placement.InDiscoverySlot);
    }

    [Theory]
    [InlineData(false, 2)]
    [InlineData(true, 6)]
    public void SelectPage_RelaxesCapsOnlyWhenTheresNothingElseToDrawFrom(bool relax, int expected)
    {
        var oneSubject = Enumerable.Range(0, 6).Select(index => Card($"p-{index}", project: "only", author: $"a-{index}", minutesOld: index));

        Select(oneSubject, new FeedPagePolicy(10, 2, 3, null, relax)).Should().HaveCount(expected);
    }

    private static IReadOnlyList<FeedPlacement> Select(
        IEnumerable<FeedCandidate> cards,
        FeedPagePolicy policy,
        FeedViewer? viewer = null)
        => FeedRanking.SelectPage(
            FeedRanking.Order(cards.Select(card => Rank(card, viewer ?? FeedViewer.Anonymous))),
            policy);

    private static RankedFeedCandidate Rank(FeedCandidate card, FeedViewer viewer)
        => FeedRanking.Rank(card, viewer, AsOf, Canon, home: true);

    private static readonly FeedTopicCanon Canon = FeedTopicCanon.Build(
    [
        new FeedTaxonomyTerm(TaxonomyKind.SECTOR, "CONSTRUCTION", "Construction", null),
        new FeedTaxonomyTerm(TaxonomyKind.SKILL, "CARPENTRY", "Carpentry", "CONSTRUCTION"),
    ]);

    private static FeedCandidate Card(
        string id,
        string? project = null,
        string author = "author",
        double minutesOld = 0,
        string[]? sectors = null,
        DateTime? publishedAt = null,
        string[]? hashtags = null)
        => new(id, author, project, null, null, null, AsOf.AddMinutes(-minutesOld), FeedQualityScore.Baseline,
            publishedAt, sectors, hashtags);

    private static FeedViewer Viewer(
        string[]? followedProjects = null,
        string[]? followedPeople = null,
        FeedTopic[]? interests = null)
        => new(
            "viewer",
            (followedProjects ?? []).ToHashSet(),
            new HashSet<string>(),
            new HashSet<string>(),
            new HashSet<string>(),
            (followedPeople ?? []).ToHashSet(),
            (interests ?? []).ToHashSet());
}
