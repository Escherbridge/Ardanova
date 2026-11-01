namespace ArdaNova.Application.Feed;

using ArdaNova.Domain.Models.Enums;

/// <summary>
/// The feed's reads that are not the post rows themselves: the viewer's graph, names for cards and reasons, the
/// vocabulary and engagement counts. Pure data access - every visibility rule stays in <see cref="FeedService"/>.
/// </summary>
public interface IFeedReadStore
{
    /// <summary>What the viewer follows, belongs to and has engaged with.</summary>
    Task<FeedViewerFacts> ReadViewerAsync(string viewerId, CancellationToken ct = default);

    /// <summary>Every active TaxonomyTerm, for the canonical tag layer and sector labels.</summary>
    Task<IReadOnlyList<FeedTaxonomyTerm>> ReadTaxonomyAsync(CancellationToken ct = default);

    /// <summary>Titles and names for the rows a page of cards points at; a missing row is simply absent.</summary>
    Task<FeedNames> ReadNamesAsync(FeedNameRequest request, CancellationToken ct = default);

    /// <summary>Replies under each root comment of a DISCUSSION card's native thread, by root comment id.</summary>
    Task<IReadOnlyDictionary<string, int>> ReadThreadReplyCountsAsync(
        FeedSourceType kind,
        IReadOnlyCollection<string> rootCommentIds,
        CancellationToken ct = default);

    /// <summary>Ids of every live (not retracted) post, in id order, for the quality pass to batch over.</summary>
    Task<IReadOnlyList<string>> ReadScorablePostIdsAsync(CancellationToken ct = default);

    /// <summary>The allowed quality signals (and the stored score) for a batch of posts.</summary>
    Task<IReadOnlyList<FeedQualityRow>> ReadQualitySignalsAsync(
        IReadOnlyCollection<string> postIds,
        CancellationToken ct = default);
}

/// <param name="ProjectSectors">Sector codes of projects the viewer follows, created or works on.</param>
/// <param name="EngagedHashtags">Tags on the viewer's most recent posts written, liked or replied to.</param>
/// <param name="SkillNames">The viewer's UserSkill names, free text.</param>
public sealed record FeedViewerFacts(
    IReadOnlySet<string> FollowedProjects,
    IReadOnlySet<string> MemberProjects,
    IReadOnlySet<string> FollowedGuilds,
    IReadOnlySet<string> MemberGuilds,
    IReadOnlySet<string> FollowedPeople,
    IReadOnlyList<string> ProjectSectors,
    IReadOnlyList<string> EngagedHashtags,
    IReadOnlyList<string> SkillNames);

public sealed record FeedNameRequest(
    IReadOnlyCollection<string> ProjectIds,
    IReadOnlyCollection<string> GuildIds,
    IReadOnlyCollection<string> OpportunityIds,
    IReadOnlyCollection<string> EventIds,
    IReadOnlyCollection<string> PersonIds);

public sealed record FeedProjectName(string Id, string Title, string Slug, string? Summary, IReadOnlyList<string> Sectors);

public sealed record FeedGuildName(string Id, string Name, string Slug);

public sealed record FeedPersonName(string Id, string? Name, string? Image);

public sealed record FeedEventName(string Id, string Title, DateTime StartsAt);

public sealed record FeedNames(
    IReadOnlyDictionary<string, FeedProjectName> Projects,
    IReadOnlyDictionary<string, FeedGuildName> Guilds,
    IReadOnlyDictionary<string, string> OpportunityTitles,
    IReadOnlyDictionary<string, FeedEventName> Events,
    IReadOnlyDictionary<string, FeedPersonName> People);

/// <summary>One post's stored score and the signals it is recomputed from.</summary>
public sealed record FeedQualityRow(string PostId, decimal StoredQuality, FeedQualitySignals Signals);

/// <summary>Which slice of the feed a page reads: home, one project, one guild, or one tag.</summary>
public sealed record FeedScope(FeedScopeKind Kind, string? Id)
{
    public static FeedScope Home { get; } = new(FeedScopeKind.Home, null);

    /// <summary>One project or guild is one subject, read newest first; home and a tag are ranked.</summary>
    public FeedPagingMode Mode => Kind is FeedScopeKind.Project or FeedScopeKind.Guild
        ? FeedPagingMode.Recency
        : FeedPagingMode.Ranked;

    /// <summary>The canonical text form, which a cursor is bound to.</summary>
    public string Key => Kind switch
    {
        FeedScopeKind.Project => "project:" + Id,
        FeedScopeKind.Guild => "guild:" + Id,
        FeedScopeKind.Tag => "tag:" + Id,
        _ => "home",
    };

    /// <summary>"home" (or nothing), "project:{id}", "guild:{id}" or "tag:{tag}" - the tag normalised like a stored one.</summary>
    public static bool TryParse(string? text, out FeedScope scope)
    {
        scope = Home;
        if (string.IsNullOrWhiteSpace(text) || text.Trim() == "home")
            return true;

        var separator = text.IndexOf(':');
        if (separator <= 0 || separator == text.Length - 1)
            return false;

        var kind = text[..separator].Trim();
        var id = text[(separator + 1)..].Trim();
        if (id.Length is 0 or > 64)
            return false;

        switch (kind)
        {
            case "project":
                scope = new FeedScope(FeedScopeKind.Project, id);
                return true;
            case "guild":
                scope = new FeedScope(FeedScopeKind.Guild, id);
                return true;
            case "tag" when Hashtags.Normalize(id) is { } tag:
                scope = new FeedScope(FeedScopeKind.Tag, tag);
                return true;
            default:
                return false;
        }
    }
}

public enum FeedScopeKind
{
    Home,
    Project,
    Guild,
    Tag,
}

/// <summary>Renders a reason as the sentence a card shows, with live names; see Feed/AGENTS.md §Reasons.</summary>
public static class FeedReasonText
{
    public static string Render(
        FeedReason reason,
        FeedNames names,
        IReadOnlyDictionary<string, string> sectorLabels)
    {
        var about = reason.AboutId;
        string Project() => about is not null && names.Projects.TryGetValue(about, out var project) ? project.Title : "a project";
        string Guild() => about is not null && names.Guilds.TryGetValue(about, out var guild) ? guild.Name : "a guild";
        string Person() => about is not null && names.People.TryGetValue(about, out var person) && person.Name is { Length: > 0 } name
            ? name
            : "someone";
        string Sector() => about is not null && sectorLabels.TryGetValue(about, out var label) ? label : Humanize(about);
        string Tag() => "#" + about;

        return reason.Kind switch
        {
            FeedReasonKind.FollowedProject => $"Because you follow {Project()}",
            FeedReasonKind.ProjectTeam => $"Because you're on the {Project()} team",
            FeedReasonKind.FollowedGuild => $"Because you follow {Guild()}",
            FeedReasonKind.GuildMember => $"Because you're in {Guild()}",
            FeedReasonKind.FollowedPerson => $"Because you follow {Person()}",
            FeedReasonKind.YourActivity => "Your activity",
            FeedReasonKind.TagInterest => $"Because you're into {Tag()}",
            FeedReasonKind.YourSector => $"In {Sector()}, one of your sectors",
            FeedReasonKind.NewInSector => $"New in {Sector()}",
            FeedReasonKind.NewProject => "New on ArdaNova",
            FeedReasonKind.Discovery => reason.About switch
            {
                FeedReasonSubject.Sector => $"Discovery: {Sector()}",
                FeedReasonSubject.Tag => $"Discovery: {Tag()}",
                _ => "Discovery: something you don't follow yet",
            },
            FeedReasonKind.InScope => reason.About switch
            {
                FeedReasonSubject.Project => $"From {Project()}",
                FeedReasonSubject.Guild => $"From {Guild()}",
                FeedReasonSubject.Tag => $"Tagged {Tag()}",
                FeedReasonSubject.Sector => $"In {Sector()}",
                _ => "On ArdaNova",
            },
            _ => "On ArdaNova",
        };
    }

    /// <summary>A sector code with no TaxonomyTerm row, made readable: CARE_SERVICES -> "Care services".</summary>
    private static string Humanize(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return "a sector";
        var words = code.Replace('_', ' ').ToLowerInvariant();
        return char.ToUpperInvariant(words[0]) + words[1..];
    }
}
