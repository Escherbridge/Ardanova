namespace ArdaNova.Application.Feed;

/// <summary>A card the ranker may place: the viewer-independent facts it scores, explains and caps by.</summary>
/// <param name="Quality">Post.trendingScore; an unscored row (0) ranks at <see cref="FeedQualityScore.Baseline"/>.</param>
/// <param name="ProjectSectors">The card's project's sector codes (Project.categories).</param>
public sealed record FeedCandidate(
    string PostId,
    string AuthorId,
    string? ProjectId,
    string? GuildId,
    string? OpportunityId,
    string? EventId,
    DateTime CreatedAt,
    double Quality,
    DateTime? ProjectPublishedAt = null,
    IReadOnlyList<string>? ProjectSectors = null,
    IReadOnlyList<string>? Hashtags = null)
{
    /// <summary>What the card is about, for the per-page subject cap: its project, else guild, opportunity, event, else author.</summary>
    public string SubjectKey => ProjectId is { } project ? "project:" + project
        : GuildId is { } guild ? "guild:" + guild
        : OpportunityId is { } opportunity ? "opportunity:" + opportunity
        : EventId is { } evt ? "event:" + evt
        : "person:" + AuthorId;
}

/// <summary>Who the feed is ranked for: the subjects they follow or belong to, and the topics they are into.</summary>
/// <param name="Interests">Tags on posts they wrote, liked or replied to, plus sectors of projects they follow or
/// work on and of their skills - all canonicalised through <see cref="FeedTopicCanon"/>.</param>
public sealed record FeedViewer(
    string? UserId,
    IReadOnlySet<string> FollowedProjects,
    IReadOnlySet<string> MemberProjects,
    IReadOnlySet<string> FollowedGuilds,
    IReadOnlySet<string> MemberGuilds,
    IReadOnlySet<string> FollowedPeople,
    IReadOnlySet<FeedTopic> Interests)
{
    public static FeedViewer Anonymous { get; } = new(
        null,
        new HashSet<string>(),
        new HashSet<string>(),
        new HashSet<string>(),
        new HashSet<string>(),
        new HashSet<string>(),
        new HashSet<FeedTopic>());
}

/// <summary>Why a card is in this viewer's feed. The API renders the sentence; the kind is the stable contract.</summary>
public enum FeedReasonKind
{
    FollowedProject,
    ProjectTeam,
    FollowedGuild,
    GuildMember,
    FollowedPerson,
    YourActivity,
    TagInterest,
    YourSector,
    NewInSector,
    NewProject,
    Discovery,
    InScope,
}

/// <summary>What a reason names, so the sentence can be rendered with a live title.</summary>
public enum FeedReasonSubject
{
    Project,
    Guild,
    Person,
    Sector,
    Tag,
}

public sealed record FeedReason(FeedReasonKind Kind, FeedReasonSubject? About = null, string? AboutId = null);

/// <param name="IsFollowed">The viewer follows or belongs to its project/guild, follows its author, or wrote it.</param>
/// <param name="IsDiscovery">Home feed only: neither followed nor matching any interest (tag or sector).</param>
public sealed record RankedFeedCandidate(
    FeedCandidate Candidate,
    double Score,
    bool IsFollowed,
    bool IsDiscovery,
    IReadOnlyList<FeedReason> Reasons);

/// <summary>How one page is filled. Null caps / discovery switch that rule off (a subject-scoped feed).</summary>
public sealed record FeedPagePolicy(
    int Limit,
    int? SubjectCap,
    int? AuthorCap,
    int? DiscoveryEvery,
    bool RelaxCapsWhenShort);

public sealed record FeedPlacement(RankedFeedCandidate Card, bool InDiscoverySlot);

/// <summary>Per-viewer ranking and page assembly; pure, see Feed/AGENTS.md §Ranking.</summary>
public static class FeedRanking
{
    public static readonly TimeSpan HalfLife = TimeSpan.FromHours(48);

    public const double ProjectLinkWeight = 2.0;
    public const double GuildLinkWeight = 1.5;
    public const double PersonLinkWeight = 1.0;
    public const double SpecificInterestWeight = 0.75;
    public const double SectorInterestWeight = 0.5;

    /// <summary>The lift every reader gives a card from a newly opened project (fading with the project's age).</summary>
    public const double NewProjectBoost = 0.5;

    /// <summary>The further lift when that new project is in one of the viewer's sectors.</summary>
    public const double SectorNoveltyBoost = 0.5;

    /// <summary>How long a newly opened project's cards carry the novelty lift (fading linearly to none).</summary>
    public static readonly TimeSpan NoveltyWindow = TimeSpan.FromDays(14);

    /// <summary>The home feed's per-page diversity rules (owner spec): 2 per subject, 3 per author, discovery every 5th.</summary>
    public const int HomeSubjectCap = 2;
    public const int HomeAuthorCap = 3;
    public const int DiscoveryEvery = 5;

    /// <summary>Exponential half-life decay: 1 at asOf, 0.5 one half-life earlier; a card dated after asOf counts as new.</summary>
    public static double Decay(DateTime createdAt, DateTime asOf)
    {
        var ageHours = Math.Max(0, (asOf - createdAt).TotalHours);
        return Math.Pow(0.5, ageHours / HalfLife.TotalHours);
    }

    /// <summary>1 the moment a project opens, fading linearly to 0 at the end of <see cref="NoveltyWindow"/>.</summary>
    public static double Freshness(DateTime? publishedAt, DateTime asOf)
    {
        if (publishedAt is not { } opened)
            return 0;

        var age = asOf - opened;
        if (age < TimeSpan.Zero)
            return 1;
        return age >= NoveltyWindow ? 0 : 1 - age / NoveltyWindow;
    }

    /// <summary>quality x recency decay x affinity x novelty, with the reasons that produced it.</summary>
    /// <param name="home">The home feed: non-followed, non-interest cards are discovery.</param>
    /// <param name="scopeReason">The reason a card in a scoped feed carries when nothing more specific applies.</param>
    public static RankedFeedCandidate Rank(
        FeedCandidate card,
        FeedViewer viewer,
        DateTime asOf,
        FeedTopicCanon canon,
        bool home,
        FeedReason? scopeReason = null)
    {
        var reasons = new List<FeedReason>(3);
        var affinity = 1.0;
        var linkedSubject = false;

        if (card.ProjectId is { } project)
        {
            if (viewer.MemberProjects.Contains(project))
                reasons.Add(new(FeedReasonKind.ProjectTeam, FeedReasonSubject.Project, project));
            else if (viewer.FollowedProjects.Contains(project))
                reasons.Add(new(FeedReasonKind.FollowedProject, FeedReasonSubject.Project, project));
            if (reasons.Count > 0)
            {
                affinity += ProjectLinkWeight;
                linkedSubject = true;
            }
        }

        if (card.GuildId is { } guild && (viewer.MemberGuilds.Contains(guild) || viewer.FollowedGuilds.Contains(guild)))
        {
            reasons.Add(viewer.MemberGuilds.Contains(guild)
                ? new(FeedReasonKind.GuildMember, FeedReasonSubject.Guild, guild)
                : new(FeedReasonKind.FollowedGuild, FeedReasonSubject.Guild, guild));
            affinity += GuildLinkWeight;
            linkedSubject = true;
        }

        var followed = linkedSubject;
        if (viewer.UserId is { } self && card.AuthorId == self)
        {
            reasons.Add(new(FeedReasonKind.YourActivity));
            followed = true;
        }
        else if (viewer.FollowedPeople.Contains(card.AuthorId))
        {
            reasons.Add(new(FeedReasonKind.FollowedPerson, FeedReasonSubject.Person, card.AuthorId));
            affinity += PersonLinkWeight;
            followed = true;
        }

        var cardSectors = SectorsOf(card, canon);
        var interest = linkedSubject ? null : InterestMatch(card, cardSectors, viewer, canon);
        if (interest is { } match)
        {
            reasons.Add(match.Reason);
            affinity += match.Weight;
        }

        // Novelty is read-time only (it fades with time), so the stored quality changes only when engagement does.
        var novelty = 1.0;
        var freshness = Freshness(card.ProjectPublishedAt, asOf);
        if (freshness > 0)
        {
            novelty += NewProjectBoost * freshness;
            var sectorOfYours = cardSectors.FirstOrDefault(sector => viewer.Interests.Contains(FeedTopic.Sector(sector)));
            if (sectorOfYours is not null)
                novelty += SectorNoveltyBoost * freshness;
            var named = sectorOfYours ?? cardSectors.FirstOrDefault();
            reasons.Add(named is null
                ? new(FeedReasonKind.NewProject, FeedReasonSubject.Project, card.ProjectId)
                : new(FeedReasonKind.NewInSector, FeedReasonSubject.Sector, named));
        }

        var discovery = home && !followed && interest is null;
        if (discovery)
            reasons.Insert(0, DiscoveryReason(card, cardSectors));
        if (reasons.Count == 0 && scopeReason is not null)
            reasons.Add(scopeReason);

        var quality = Math.Max(FeedQualityScore.Baseline, card.Quality);
        var score = quality * Decay(card.CreatedAt, asOf) * affinity * novelty;
        return new RankedFeedCandidate(card, score, followed, discovery, reasons);
    }

    /// <summary>Highest score first; ties by newest, then id, so the order is total and repeatable.</summary>
    public static IReadOnlyList<RankedFeedCandidate> Order(IEnumerable<RankedFeedCandidate> ranked)
        => ranked
            .OrderByDescending(card => card.Score)
            .ThenByDescending(card => card.Candidate.CreatedAt)
            .ThenByDescending(card => card.Candidate.PostId, StringComparer.Ordinal)
            .ToList();

    /// <summary>
    /// Greedy fill in rank order under the caps; every Nth slot prefers a discovery card. A slot no card can fill
    /// under the caps is filled ignoring them only when <see cref="FeedPagePolicy.RelaxCapsWhenShort"/> - i.e. when
    /// there is nothing further to draw from - never to save a round trip.
    /// </summary>
    public static IReadOnlyList<FeedPlacement> SelectPage(IReadOnlyList<RankedFeedCandidate> ordered, FeedPagePolicy policy)
    {
        var placed = new List<FeedPlacement>(policy.Limit);
        var taken = new bool[ordered.Count];
        var bySubject = new Dictionary<string, int>(StringComparer.Ordinal);
        var byAuthor = new Dictionary<string, int>(StringComparer.Ordinal);

        while (placed.Count < policy.Limit)
        {
            var slot = placed.Count + 1;
            var discoverySlot = policy.DiscoveryEvery is { } every && every > 0 && slot % every == 0;

            var pick = -1;
            if (discoverySlot)
                pick = Find(card => card.IsDiscovery, withinCaps: true);
            if (pick < 0)
                pick = Find(_ => true, withinCaps: true);
            if (pick < 0 && policy.RelaxCapsWhenShort)
            {
                if (discoverySlot)
                    pick = Find(card => card.IsDiscovery, withinCaps: false);
                if (pick < 0)
                    pick = Find(_ => true, withinCaps: false);
            }

            if (pick < 0)
                break;

            var chosen = ordered[pick];
            taken[pick] = true;
            Count(bySubject, chosen.Candidate.SubjectKey);
            Count(byAuthor, chosen.Candidate.AuthorId);
            placed.Add(new FeedPlacement(chosen, discoverySlot && chosen.IsDiscovery));
        }

        return placed;

        int Find(Func<RankedFeedCandidate, bool> wanted, bool withinCaps)
        {
            for (var index = 0; index < ordered.Count; index++)
            {
                if (taken[index] || !wanted(ordered[index]))
                    continue;
                if (withinCaps && !FitsCaps(ordered[index].Candidate))
                    continue;
                return index;
            }

            return -1;
        }

        bool FitsCaps(FeedCandidate card)
            => (policy.SubjectCap is not { } subjectCap || bySubject.GetValueOrDefault(card.SubjectKey) < subjectCap)
               && (policy.AuthorCap is not { } authorCap || byAuthor.GetValueOrDefault(card.AuthorId) < authorCap);

        static void Count(Dictionary<string, int> counts, string key)
            => counts[key] = counts.GetValueOrDefault(key) + 1;
    }

    /// <summary>A card's sectors: its project's categories, then the sectors its tags canonicalise to.</summary>
    public static IReadOnlyList<string> SectorsOf(FeedCandidate card, FeedTopicCanon canon)
        => (card.ProjectSectors ?? [])
            .Concat((card.Hashtags ?? []).SelectMany(canon.TopicsOf).Select(topic => topic.SectorSlug).OfType<string>())
            .Select(sector => sector.ToUpperInvariant())
            .Distinct(StringComparer.Ordinal)
            .ToList();

    /// <summary>
    /// The strongest overlap between the card and the viewer's interests: a specific tag (a skill, a project type or a
    /// free-form tag) beats a sector reached through a tag, which beats a sector reached through the card's project.
    /// </summary>
    private static (FeedReason Reason, double Weight)? InterestMatch(
        FeedCandidate card,
        IReadOnlyList<string> cardSectors,
        FeedViewer viewer,
        FeedTopicCanon canon)
    {
        if (viewer.Interests.Count == 0)
            return null;

        var tags = card.Hashtags ?? [];
        foreach (var tag in tags)
        {
            if (canon.TopicsOf(tag).Any(topic => !topic.IsSector && viewer.Interests.Contains(topic)))
                return (new(FeedReasonKind.TagInterest, FeedReasonSubject.Tag, tag), SpecificInterestWeight);
        }

        foreach (var tag in tags)
        {
            if (canon.TopicsOf(tag).Any(topic => topic.IsSector && viewer.Interests.Contains(topic)))
                return (new(FeedReasonKind.TagInterest, FeedReasonSubject.Tag, tag), SectorInterestWeight);
        }

        var sector = cardSectors.FirstOrDefault(code => viewer.Interests.Contains(FeedTopic.Sector(code)));
        return sector is null
            ? null
            : (new(FeedReasonKind.YourSector, FeedReasonSubject.Sector, sector), SectorInterestWeight);
    }

    private static FeedReason DiscoveryReason(FeedCandidate card, IReadOnlyList<string> cardSectors)
        => cardSectors.Count > 0 ? new(FeedReasonKind.Discovery, FeedReasonSubject.Sector, cardSectors[0])
            : card.Hashtags is { Count: > 0 } tags ? new(FeedReasonKind.Discovery, FeedReasonSubject.Tag, tags[0])
            : new(FeedReasonKind.Discovery);
}
