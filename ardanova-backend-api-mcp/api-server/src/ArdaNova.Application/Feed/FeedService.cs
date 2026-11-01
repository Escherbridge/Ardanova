namespace ArdaNova.Application.Feed;

using System.Linq.Expressions;
using System.Text.Json;
using ArdaNova.Application.Common.Interfaces;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Implementations;
using ArdaNova.Application.Tasks;
using ArdaNova.Domain.Models.Entities;
using ArdaNova.Domain.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

/// <summary>The infinite ranked feed: home, or one project, guild or tag. See Feed/AGENTS.md §Paging.</summary>
public interface IFeedService
{
    /// <summary>One page for this viewer (null = signed out, PUBLIC only), continuing from an issued cursor.</summary>
    Task<Result<FeedPageDto>> GetPageAsync(FeedPageRequest request, CancellationToken ct = default);
}

/// <param name="Scope">"home" (default), "project:{id}", "guild:{id}" or "tag:{tag}".</param>
public sealed record FeedPageRequest(string? ViewerId, string? Scope = null, string? Cursor = null, int? Limit = null);

/// <inheritdoc/>
/// <remarks>
/// Home and tag scopes: ranked windows over a keyset - each page ranks the open window (the next
/// <see cref="WindowChunk"/> visible cards by recency, grown up to <see cref="MaxWindow"/> when caps leave a page short)
/// and serves it in rank order, so time decay is pinned by the cursor's asOf and no card is served twice. A project or
/// guild scope is one subject, read newest first. Visibility is re-applied on every read in every mode.
/// </remarks>
public sealed class FeedService : IFeedService
{
    public const int DefaultLimit = 20;
    public const int MaxLimit = 50;

    /// <summary>How many cards one window opening reads.</summary>
    public const int WindowChunk = 60;

    /// <summary>The most cards an open window holds before caps relax rather than the window growing again.</summary>
    public const int MaxWindow = 150;

    /// <summary>Re-plans per request (window growth, or refills after hydration dropped a vanished card).</summary>
    private const int MaxRounds = 12;

    private readonly IRepository<Post> _posts;
    private readonly IRepository<Project> _projects;
    private readonly IRepository<Guild> _guilds;
    private readonly IFeedSourceReader _sources;
    private readonly IFeedReadStore _reads;
    private readonly TimeProvider _clock;
    private readonly ILogger<FeedService> _logger;

    public FeedService(
        IRepository<Post> posts,
        IRepository<Project> projects,
        IRepository<Guild> guilds,
        IFeedSourceReader sources,
        IFeedReadStore reads,
        TimeProvider clock,
        ILogger<FeedService>? logger = null)
    {
        _posts = posts;
        _projects = projects;
        _guilds = guilds;
        _sources = sources;
        _reads = reads;
        _clock = clock;
        _logger = logger ?? NullLogger<FeedService>.Instance;
    }

    /// <inheritdoc/>
    public async Task<Result<FeedPageDto>> GetPageAsync(FeedPageRequest request, CancellationToken ct = default)
    {
        if (!FeedScope.TryParse(request.Scope, out var scope))
            return Result<FeedPageDto>.ValidationError(
                "Unknown feed scope; use home, project:{id}, guild:{id} or tag:{tag}");

        FeedCursor? cursor = null;
        if (!string.IsNullOrWhiteSpace(request.Cursor))
        {
            if (!FeedCursor.TryDecode(request.Cursor, out cursor))
                return Result<FeedPageDto>.ValidationError("This feed cursor was not issued by this feed");
            if (cursor!.Scope != scope.Key || cursor.Mode != scope.Mode)
                return Result<FeedPageDto>.ValidationError("This feed cursor belongs to a different feed");
        }

        var viewerId = string.IsNullOrWhiteSpace(request.ViewerId) ? null : request.ViewerId;
        if (!await ScopeIsVisibleAsync(scope, viewerId, ct))
            return Result<FeedPageDto>.NotFound($"{scope.Kind} {scope.Id} not found");

        var limit = Math.Clamp(request.Limit ?? DefaultLimit, 1, MaxLimit);
        var now = _clock.GetUtcNow().UtcDateTime;
        // A cursor can only pin time back, never forward: a tampered asOf in the future reads as now.
        var asOf = cursor is not null && cursor.AsOf < now ? cursor.AsOf : now;

        var terms = await _reads.ReadTaxonomyAsync(ct);
        var canon = FeedTopicCanon.Build(terms);
        var viewer = viewerId is null ? FeedViewer.Anonymous : ViewerFrom(viewerId, await _reads.ReadViewerAsync(viewerId, ct), canon);
        // A tag that names a SECTOR term (#construction) also reaches that sector's untagged cards.
        var tagSector = scope.Kind == FeedScopeKind.Tag && canon.Resolve(scope.Id!) is { Kind: TaxonomyKind.SECTOR } term
            ? term.Slug.ToUpperInvariant()
            : null;
        var context = new PageContext(scope, viewerId, viewer, canon, asOf, Visible(scope, viewerId, asOf, tagSector))
        {
            TagSector = tagSector,
            SectorLabels = terms
                .Where(term => term.Kind == TaxonomyKind.SECTOR)
                .GroupBy(term => term.Slug.ToUpperInvariant())
                .ToDictionary(group => group.Key, group => group.First().Label, StringComparer.Ordinal),
        };
        if (scope.Mode == FeedPagingMode.Recency)
            return Result<FeedPageDto>.Success(await RecencyPageAsync(context, cursor, limit, ct));

        var window = new Window(cursor);

        var (placements, cards, windowRows, vanished) = await AssemblePageAsync(context, window, limit, ct);

        foreach (var id in placements.Select(placement => placement.Card.Candidate.PostId).Concat(vanished))
            window.Served.Add(FeedCursor.Fingerprint(id));
        // An issued cursor's served set holds only this window's rows (<= MaxWindow + WindowChunk = 210 < MaxServed);
        // only a hand-made one can exceed it. Keeping the fingerprints of rows still in the window bounds it again
        // without skipping anything this viewer has not been served.
        if (window.Served.Count > FeedCursor.MaxServed)
            window.Served.IntersectWith(windowRows.Select(row => row.Fingerprint));
        var unserved = windowRows.Count(row => !window.Served.Contains(row.Fingerprint));
        if (unserved == 0)
        {
            // The window is spent: move below it with an empty served set.
            window.Top = window.Floor ?? window.Top;
            window.Floor = null;
            window.Served.Clear();
            if (!window.Exhausted)
                window.Exhausted = !await Below(context.Candidates, window.Top).AnyAsync(ct);
            unserved = 0;
        }

        var items = placements
            .Where(placement => cards.ContainsKey(placement.Card.Candidate.PostId))
            .Select(placement => cards[placement.Card.Candidate.PostId])
            .ToList();
        var nextCursor = unserved == 0 && window.Exhausted
            ? null
            : new FeedCursor(asOf, scope.Key, window.Top, window.Floor, window.Served, window.Exhausted).Encode();
        return Result<FeedPageDto>.Success(new FeedPageDto(items, nextCursor));
    }

    /// <summary>
    /// One project's or guild's own feed: newest first on a (createdAt, id) keyset - one subject, so no caps and no
    /// discovery. A card dropped at hydration is replaced from further down; the cursor is the last key examined.
    /// </summary>
    private async Task<FeedPageDto> RecencyPageAsync(PageContext context, FeedCursor? cursor, int limit, CancellationToken ct)
    {
        var items = new List<FeedCardDto>(limit);
        var after = cursor?.Top;
        var exhausted = cursor?.Exhausted ?? false;

        for (var round = 0; round < MaxRounds && items.Count < limit && !exhausted; round++)
        {
            var wanted = limit - items.Count;
            var rows = await Newest(Below(context.Candidates, after)).Take(wanted + 1).Select(ToRow).ToListAsync(ct);
            exhausted = rows.Count <= wanted;
            var batch = rows.Take(wanted).ToList();
            if (batch.Count == 0)
                break;

            // The scope's own policy (PolicyFor: uncapped, no discovery) keeps every row, in recency order. A cap here
            // would thin the page AND skip the thinned rows, since the keyset below moves past the whole batch.
            var placements = FeedRanking.SelectPage(
                batch
                    .Select(row => FeedRanking.Rank(
                        row.ToCandidate(), context.Viewer, context.AsOf, context.Canon, home: false, ScopeReasonFor(context, row)))
                    .ToList(),
                PolicyFor(context.Scope, batch.Count));
            var cards = await HydrateAsync(context, placements, ct);
            items.AddRange(placements
                .Where(placement => cards.ContainsKey(placement.Card.Candidate.PostId))
                .Select(placement => cards[placement.Card.Candidate.PostId]));
            after = batch[^1].Key;
        }

        var nextCursor = exhausted
            ? null
            : new FeedCursor(context.AsOf, context.Scope.Key, after, null, new HashSet<ulong>(), false, FeedPagingMode.Recency).Encode();
        return new FeedPageDto(items, nextCursor);
    }

    /// <summary>Rank the open window, fill a page under the caps, hydrate it; grow or refill until full or spent.</summary>
    private async Task<(IReadOnlyList<FeedPlacement> Placements, Dictionary<string, FeedCardDto> Cards, List<CandidateRow> WindowRows, HashSet<string> Vanished)>
        AssemblePageAsync(PageContext context, Window window, int limit, CancellationToken ct)
    {
        var policy = PolicyFor(context.Scope, limit);
        var cards = new Dictionary<string, FeedCardDto>(StringComparer.Ordinal);
        var vanished = new HashSet<string>(StringComparer.Ordinal);
        IReadOnlyList<FeedPlacement> placements = [];
        List<CandidateRow> windowRows = [];

        for (var round = 0; round < MaxRounds; round++)
        {
            if (window.Floor is null && !window.Exhausted)
                await OpenChunkAsync(context, window, ct);

            windowRows = await ReadWindowAsync(context, window, ct);
            var pool = windowRows
                .Where(row => !window.Served.Contains(row.Fingerprint) && !vanished.Contains(row.Id))
                .Select(row => FeedRanking.Rank(
                    row.ToCandidate(), context.Viewer, context.AsOf, context.Canon,
                    home: context.Scope.Kind == FeedScopeKind.Home, ScopeReasonFor(context, row)));
            var canGrow = !window.Exhausted && windowRows.Count < MaxWindow;
            // Caps relax only when nothing further exists to draw from; a full window that is not the end of the feed
            // returns a short page instead (owner rule; see Feed/AGENTS.md §Ranking).
            placements = FeedRanking.SelectPage(FeedRanking.Order(pool), policy with { RelaxCapsWhenShort = window.Exhausted });

            if (placements.Count < limit && canGrow)
            {
                await OpenChunkAsync(context, window, ct);
                continue;
            }

            var fresh = placements.Where(placement => !cards.ContainsKey(placement.Card.Candidate.PostId)).ToList();
            var hydrated = await HydrateAsync(context, fresh, ct);
            var dropped = 0;
            foreach (var placement in fresh)
            {
                var id = placement.Card.Candidate.PostId;
                if (hydrated.TryGetValue(id, out var card))
                    cards[id] = card;
                else if (vanished.Add(id))
                    dropped++;
            }

            // A card whose source vanished between projector cycles is dropped and the page refilled - the scroll goes on.
            if (dropped == 0)
                break;
        }

        return (placements, cards, windowRows, vanished);
    }

    /// <summary>Opens (or extends) the window by the next chunk of visible cards below it.</summary>
    private static async Task OpenChunkAsync(PageContext context, Window window, CancellationToken ct)
    {
        var rows = await Newest(Below(context.Candidates, window.Floor ?? window.Top))
            .Take(WindowChunk)
            .Select(ToRow)
            .ToListAsync(ct);
        if (rows.Count == 0)
        {
            window.Exhausted = true;
            return;
        }

        window.Floor = rows[^1].Key;
        if (rows.Count < WindowChunk)
            window.Exhausted = true;
    }

    /// <summary>Every visible card in [Floor, Top) - re-read each page, so visibility is today's, not the cursor's.</summary>
    private static async Task<List<CandidateRow>> ReadWindowAsync(PageContext context, Window window, CancellationToken ct)
    {
        if (window.Floor is not { } floor)
            return [];

        return await Newest(Below(AtOrAbove(context.Candidates, floor), window.Top))
            .Take(MaxWindow + WindowChunk)
            .Select(ToRow)
            .ToListAsync(ct);
    }

    /// <summary>
    /// The cards this viewer may see right now: the shared post read gate (audience, project draft rule, retraction),
    /// the pinned asOf, and the scope's filter. A tampered cursor narrows this; nothing widens it.
    /// </summary>
    private IQueryable<Post> Visible(FeedScope scope, string? viewerId, DateTime asOf, string? tagSector)
    {
        var posts = _posts.Query().AsNoTracking().ReadableBy(viewerId).Where(post => post.createdAt <= asOf);
        return scope.Kind switch
        {
            FeedScopeKind.Project => posts.Where(post => post.projectId == scope.Id),
            FeedScopeKind.Guild => posts.Where(post => post.guildId == scope.Id),
            FeedScopeKind.Tag => posts.Where(TaggedWith(scope.Id!, tagSector)),
            _ => posts,
        };
    }

    /// <summary>
    /// Array containment - Npgsql emits ARRAY[tag] &lt;@ hashtags, which Postgres commutes to hashtags @> ARRAY[tag]
    /// for the GIN index - or, for a tag naming a sector, a card whose project carries that sector code (whole-token,
    /// as ProjectQueryFilters.HasCategory). A plain hashtags.Contains(tag) would emit "= ANY", which GIN cannot serve.
    /// </summary>
    private static Expression<Func<Post, bool>> TaggedWith(string tag, string? sector)
    {
        var wanted = new[] { tag };
        if (sector is null)
            return post => wanted.All(candidate => post.hashtags.Contains(candidate));

        var needle = "," + sector + ",";
        return post => wanted.All(candidate => post.hashtags.Contains(candidate))
            || (post.Project != null && ("," + post.Project.categories + ",").Contains(needle));
    }

    /// <summary>A scoped feed answers a project or guild this viewer cannot see exactly as a missing one.</summary>
    private async Task<bool> ScopeIsVisibleAsync(FeedScope scope, string? viewerId, CancellationToken ct) => scope.Kind switch
    {
        FeedScopeKind.Project => await _projects.Query()
            .Where(project => project.id == scope.Id)
            .Where(ProjectQueryFilters.VisibleTo(new ProjectViewer(viewerId, IsAdmin: false)))
            .AnyAsync(ct),
        FeedScopeKind.Guild => await _guilds.Query().AnyAsync(guild => guild.id == scope.Id, ct),
        _ => true,
    };

    /// <summary>
    /// The cards that are still what they were when ranked: re-read through the read gate, and - for an envelope -
    /// its source still exists, still earns the card for the same audience and still points at the same rows (the
    /// projector's own Retain/TargetsOf). Anything else is left out; see Feed/AGENTS.md §Phase 2 defence in depth.
    /// </summary>
    private async Task<Dictionary<string, FeedCardDto>> HydrateAsync(
        PageContext context,
        IReadOnlyList<FeedPlacement> placements,
        CancellationToken ct)
    {
        var cards = new Dictionary<string, FeedCardDto>(StringComparer.Ordinal);
        if (placements.Count == 0)
            return cards;

        var ids = placements.Select(placement => placement.Card.Candidate.PostId).ToArray();
        var rows = (await _posts.Query().AsNoTracking()
                .Where(post => ids.Contains(post.id))
                .ReadableBy(context.ViewerId)
                .Select(post => new HydrationRow
                {
                    Id = post.id,
                    Type = post.type,
                    SourceType = post.sourceType,
                    SourceId = post.sourceId,
                    AuthorId = post.authorId,
                    ProjectId = post.projectId,
                    GuildId = post.guildId,
                    OpportunityId = post.opportunityId,
                    EventId = post.eventId,
                    Visibility = post.visibility,
                    Title = post.title,
                    Content = post.content,
                    Metadata = post.metadata,
                    Hashtags = post.hashtags,
                    Likes = post.likesCount,
                    Comments = post.commentsCount,
                    Shares = post.sharesCount,
                    CreatedAt = post.createdAt,
                    TaskId = post.taskId,
                })
                .ToListAsync(ct))
            .ToDictionary(row => row.Id, StringComparer.Ordinal);

        var sources = await CurrentSourcesAsync(rows.Values, ct);
        var helpTasks = await HelpTasksAsync(rows.Values, ct);
        var live = rows.Values
            .Where(row => StillEarnsItsCard(row, SourceOf(row, sources)))
            .Where(row => row.Type != PostType.HELP_WANTED || HelpTaskOf(row, helpTasks) is not null)
            .ToList();
        if (live.Count == 0)
            return cards;
        var liveIds = live.Select(row => row.Id).ToHashSet(StringComparer.Ordinal);

        var reasonIds = placements.SelectMany(placement => placement.Card.Reasons).ToList();
        var names = await _reads.ReadNamesAsync(new FeedNameRequest(
            live.Select(row => row.ProjectId).Concat(IdsAbout(reasonIds, FeedReasonSubject.Project)).OfType<string>().ToHashSet(),
            live.Select(row => row.GuildId).Concat(IdsAbout(reasonIds, FeedReasonSubject.Guild)).OfType<string>().ToHashSet(),
            live.Select(row => row.OpportunityId).OfType<string>().ToHashSet(),
            live.Select(row => row.EventId).OfType<string>().ToHashSet(),
            live.Select(row => row.AuthorId).Concat(IdsAbout(reasonIds, FeedReasonSubject.Person)).OfType<string>().ToHashSet()), ct);
        var threadReplies = await ThreadRepliesAsync(live, ct);
        var sectorLabels = context.SectorLabels;

        foreach (var placement in placements)
        {
            if (!rows.TryGetValue(placement.Card.Candidate.PostId, out var row) || !liveIds.Contains(row.Id))
                continue;
            cards[row.Id] = ToCard(
                row, SourceOf(row, sources), HelpTaskOf(row, helpTasks), placement, names, threadReplies, sectorLabels, context.Viewer);
        }

        return cards;
    }

    /// <summary>The tasks this page's HELP_WANTED rows ask for, by task id; a failed read leaves those cards out.</summary>
    private async Task<IReadOnlyDictionary<string, FeedHelpTask>> HelpTasksAsync(
        IEnumerable<HydrationRow> rows,
        CancellationToken ct)
    {
        var taskIds = rows
            .Where(row => row.Type == PostType.HELP_WANTED && row.TaskId is not null)
            .Select(row => row.TaskId!)
            .Distinct(StringComparer.Ordinal)
            .ToList();
        if (taskIds.Count == 0)
            return new Dictionary<string, FeedHelpTask>();

        try
        {
            return await _reads.ReadHelpTasksAsync(taskIds, ct);
        }
        catch (Exception failure) when (!ct.IsCancellationRequested)
        {
            // Fail closed, like a source kind whose read throws: the asks are left out of this page only.
            _logger.LogWarning(failure, "Feed: reading help-wanted tasks for hydration failed; their cards were left out.");
            return new Dictionary<string, FeedHelpTask>();
        }
    }

    /// <summary>
    /// A HELP_WANTED card's task while it still earns the card: the task exists (its FK nulls taskId on delete) and sits
    /// on the card's own project - the one the read gate just checked - so its facts reach only who may see that project.
    /// </summary>
    private static FeedHelpTask? HelpTaskOf(HydrationRow row, IReadOnlyDictionary<string, FeedHelpTask> tasks)
        => row.Type == PostType.HELP_WANTED
           && row.TaskId is { } taskId
           && tasks.TryGetValue(taskId, out var task)
           && task.ProjectId == row.ProjectId
            ? task
            : null;

    /// <summary>An envelope's current source row (a funding key "{config}:{mark}" names the config row).</summary>
    private static FeedSource? SourceOf(HydrationRow row, IReadOnlyDictionary<FeedSourceKey, FeedSource> sources)
        => row.SourceKey is { } key
            ? sources.GetValueOrDefault(new FeedSourceKey(key.SourceType, FeedProjectionRules.RowIdOf(key)))
            : null;

    /// <summary>The current source row of each envelope, keyed by its (sourceType, row id); unreadable kinds are absent.</summary>
    private async Task<Dictionary<FeedSourceKey, FeedSource>> CurrentSourcesAsync(
        IEnumerable<HydrationRow> rows,
        CancellationToken ct)
    {
        var current = new Dictionary<FeedSourceKey, FeedSource>();
        foreach (var kind in rows.Where(row => row.SourceKey is not null).GroupBy(row => row.SourceKey!.Value.SourceType))
        {
            var rowIds = kind.Select(row => FeedProjectionRules.RowIdOf(row.SourceKey!.Value)).Distinct().ToList();
            try
            {
                foreach (var source in await _sources.ReadByIdsAsync(kind.Key, rowIds, ct))
                    current[new FeedSourceKey(kind.Key, source.Id)] = source;
            }
            catch (Exception failure) when (!ct.IsCancellationRequested)
            {
                // Fail closed: a card whose source cannot be read right now is left out of this page.
                _logger.LogWarning(failure, "Feed: reading {SourceKind} sources for hydration failed; their cards were left out.", kind.Key);
            }
        }

        return current;
    }

    private static bool StillEarnsItsCard(HydrationRow row, FeedSource? source)
    {
        if (row.SourceKey is null)
            return true;
        if (source is null)
            return false;

        // At most as visible as its source: a narrower (or no) audience than the stored one means the next cycle
        // will change the card, so until then it is not shown at all.
        return FeedProjectionRules.Retain(source) == row.Visibility
            && FeedProjectionRules.TargetsOf(source) == new FeedEnvelopeTargets(row.ProjectId, row.GuildId, row.OpportunityId, row.EventId);
    }

    private async Task<Dictionary<string, int>> ThreadRepliesAsync(IReadOnlyList<HydrationRow> rows, CancellationToken ct)
    {
        var replies = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var kind in new[] { FeedSourceType.PROJECT_COMMENT, FeedSourceType.OPPORTUNITY_COMMENT })
        {
            var roots = rows.Where(row => row.SourceType == kind && row.SourceId is not null).Select(row => row.SourceId!).ToList();
            if (roots.Count == 0)
                continue;
            foreach (var (root, count) in await _reads.ReadThreadReplyCountsAsync(kind, roots, ct))
                replies[root] = count;
        }

        return replies;
    }

    private static FeedCardDto ToCard(
        HydrationRow row,
        FeedSource? source,
        FeedHelpTask? task,
        FeedPlacement placement,
        FeedNames names,
        IReadOnlyDictionary<string, int> threadReplies,
        IReadOnlyDictionary<string, string> sectorLabels,
        FeedViewer viewer)
    {
        // A person's post IS their words; an envelope shows its source's live words when a person wrote them.
        var (body, userWritten) = row.SourceType is null ? (row.Content, true)
            : source?.Body is { } words ? (words, true)
            : (row.Content, false);
        var replies = row.Type == PostType.DISCUSSION && row.SourceId is not null
            ? threadReplies.GetValueOrDefault(row.SourceId)
            : row.Comments;
        var author = names.People.GetValueOrDefault(row.AuthorId);

        return new FeedCardDto(
            row.Id,
            row.Type.ToString(),
            row.SourceType?.ToString(),
            row.CreatedAt,
            new FeedPersonDto(row.AuthorId, author?.Name, author?.Image),
            row.Title,
            body,
            userWritten,
            row.Hashtags,
            row.ProjectId is { } projectId && names.Projects.TryGetValue(projectId, out var project)
                ? new FeedProjectSummaryDto(project.Id, project.Title, project.Slug, project.Summary, project.Sectors)
                : null,
            row.GuildId is { } guildId && names.Guilds.TryGetValue(guildId, out var guild)
                ? new FeedGuildSummaryDto(guild.Id, guild.Name, guild.Slug)
                : null,
            row.OpportunityId is { } opportunityId && names.Opportunities.TryGetValue(opportunityId, out var opportunity)
                ? new FeedLinkDto(opportunity.Id, opportunity.Title, opportunity.Slug)
                : null,
            row.EventId is { } eventId && names.Events.TryGetValue(eventId, out var evt)
                ? new FeedEventSummaryDto(evt.Id, evt.Title, evt.StartsAt)
                : null,
            row.Type == PostType.FUNDING_PROGRESS ? FundingOf(row.Metadata) : null,
            row.Type == PostType.DISCUSSION ? ThreadOf(row.Metadata) : null,
            new FeedCountersDto(row.Likes, replies, row.Shares),
            RelationOf(viewer, row.AuthorId, row.ProjectId, row.GuildId, task),
            placement.Card.IsDiscovery,
            placement.Card.Reasons
                .Select(reason => new FeedReasonDto(
                    reason.Kind.ToString(),
                    FeedReasonText.Render(reason, names, sectorLabels),
                    reason.About?.ToString().ToUpperInvariant(),
                    reason.AboutId))
                .ToList(),
            task is null ? null : new FeedTaskDto(task.Id, task.State.ToString(), task.AssigneeName, Paid: task.EquityReward is > 0m));
    }

    /// <summary>
    /// The reader's standing toward the card's subject. CanClaim is the claim write's own rule (TaskClaimRule), with
    /// membership of the TASK's project - never a guild - as the claim route checks it, and never on the reader's own
    /// ask (a card-level choice only: the claim route still lets an asker take their task; AGENTS.md §Help wanted).
    /// </summary>
    private static FeedViewerRelationDto RelationOf(
        FeedViewer viewer,
        string authorId,
        string? projectId,
        string? guildId,
        FeedHelpTask? task)
        => new(
            IsMember: (projectId is not null && viewer.MemberProjects.Contains(projectId))
                || (guildId is not null && viewer.MemberGuilds.Contains(guildId)),
            IsFollowing: (projectId is not null && viewer.FollowedProjects.Contains(projectId))
                || (guildId is not null && viewer.FollowedGuilds.Contains(guildId)),
            CanClaim: task is not null
                && !string.Equals(authorId, viewer.UserId, StringComparison.Ordinal)
                && TaskClaimRule.CanClaimInPlace(task.ClaimFacts, viewerIsProjectMember: viewer.MemberProjects.Contains(task.ProjectId)));

    /// <summary>The funding mark from an envelope's {"percent":N} metadata - the only figure a funding card carries.</summary>
    private static FeedFundingDto? FundingOf(string? metadata)
    {
        if (string.IsNullOrWhiteSpace(metadata))
            return null;
        try
        {
            using var document = JsonDocument.Parse(metadata);
            return document.RootElement.TryGetProperty("percent", out var percent) && percent.TryGetInt32(out var value)
                ? new FeedFundingDto(value)
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static FeedThreadDto? ThreadOf(string? metadata)
    {
        if (string.IsNullOrWhiteSpace(metadata))
            return null;
        try
        {
            using var document = JsonDocument.Parse(metadata);
            if (!document.RootElement.TryGetProperty("thread", out var thread))
                return null;
            string? Text(string name) => thread.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
                ? value.GetString()
                : null;
            return Text("source") is { } source && Text("commentId") is { } commentId
                ? new FeedThreadDto(source, commentId, Text("targetType"), Text("targetId"), Text("opportunityId"))
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static IEnumerable<string?> IdsAbout(IEnumerable<FeedReason> reasons, FeedReasonSubject subject)
        => reasons.Where(reason => reason.About == subject).Select(reason => reason.AboutId);

    /// <summary>One page rule per scope kind, read by both paging modes (Feed/AGENTS.md §Paging, Q1).</summary>
    private static FeedPagePolicy PolicyFor(FeedScope scope, int limit) => scope.Kind switch
    {
        FeedScopeKind.Home => new FeedPagePolicy(
            limit, SubjectCap: FeedRanking.HomeSubjectCap, AuthorCap: FeedRanking.HomeAuthorCap, DiscoveryEvery: FeedRanking.DiscoveryEvery, RelaxCapsWhenShort: false),
        // One subject read newest first: any cap would thin a one-writer project and reorder it.
        FeedScopeKind.Project or FeedScopeKind.Guild => new FeedPagePolicy(
            limit, SubjectCap: null, AuthorCap: null, DiscoveryEvery: null, RelaxCapsWhenShort: false),
        // A tag cuts across subjects: only the per-author cap, so one prolific writer cannot own the tag.
        FeedScopeKind.Tag => new FeedPagePolicy(
            limit, SubjectCap: null, AuthorCap: FeedRanking.HomeAuthorCap, DiscoveryEvery: null, RelaxCapsWhenShort: false),
        _ => throw new ArgumentOutOfRangeException(nameof(scope), scope.Kind, "Unknown feed scope kind"),
    };

    /// <summary>Why a card is in a scoped feed when nothing more specific applies: its subject, its tag, or its sector.</summary>
    private static FeedReason? ScopeReasonFor(PageContext context, CandidateRow row) => context.Scope.Kind switch
    {
        FeedScopeKind.Project => new(FeedReasonKind.InScope, FeedReasonSubject.Project, context.Scope.Id),
        FeedScopeKind.Guild => new(FeedReasonKind.InScope, FeedReasonSubject.Guild, context.Scope.Id),
        FeedScopeKind.Tag when context.TagSector is { } sector && !row.Hashtags.Contains(context.Scope.Id!)
            => new(FeedReasonKind.InScope, FeedReasonSubject.Sector, sector),
        FeedScopeKind.Tag => new(FeedReasonKind.InScope, FeedReasonSubject.Tag, context.Scope.Id),
        _ => null,
    };

    /// <summary>The viewer's follows plus their interests in topic space: engaged tags, project sectors, skills.</summary>
    private static FeedViewer ViewerFrom(string viewerId, FeedViewerFacts facts, FeedTopicCanon canon)
    {
        var interests = new HashSet<FeedTopic>();
        foreach (var tag in facts.EngagedHashtags)
            interests.UnionWith(canon.TopicsOf(tag));
        foreach (var sector in facts.ProjectSectors)
            interests.Add(FeedTopic.Sector(sector));
        foreach (var skill in facts.SkillNames)
            interests.UnionWith(canon.TopicsOfName(skill));

        return new FeedViewer(
            viewerId,
            facts.FollowedProjects,
            facts.MemberProjects,
            facts.FollowedGuilds,
            facts.MemberGuilds,
            facts.FollowedPeople,
            interests);
    }

    private static IQueryable<Post> Newest(IQueryable<Post> posts)
        => posts.OrderByDescending(post => post.createdAt).ThenByDescending(post => post.id);

    /// <summary>Strictly after <paramref name="key"/> in (createdAt desc, id desc) order; null = everything.</summary>
    private static IQueryable<Post> Below(IQueryable<Post> posts, FeedKey? key)
    {
        if (key is null)
            return posts;
        var createdAt = key.CreatedAt;
        var id = key.Id;
        return posts.Where(post => post.createdAt < createdAt
            || (post.createdAt == createdAt && string.Compare(post.id, id) < 0));
    }

    private static IQueryable<Post> AtOrAbove(IQueryable<Post> posts, FeedKey key)
    {
        var createdAt = key.CreatedAt;
        var id = key.Id;
        return posts.Where(post => post.createdAt > createdAt
            || (post.createdAt == createdAt && string.Compare(post.id, id) >= 0));
    }

    private static readonly Expression<Func<Post, CandidateRow>> ToRow = post => new CandidateRow
    {
        Id = post.id,
        AuthorId = post.authorId,
        ProjectId = post.projectId,
        GuildId = post.guildId,
        OpportunityId = post.opportunityId,
        EventId = post.eventId,
        CreatedAt = post.createdAt,
        Quality = post.trendingScore,
        Hashtags = post.hashtags,
        ProjectPublishedAt = post.Project != null ? post.Project.publishedAt : null,
        ProjectCategories = post.Project != null ? post.Project.categories : null,
    };

    private sealed record PageContext(
        FeedScope Scope,
        string? ViewerId,
        FeedViewer Viewer,
        FeedTopicCanon Canon,
        DateTime AsOf,
        IQueryable<Post> Candidates)
    {
        public IReadOnlyDictionary<string, string> SectorLabels { get; init; } = new Dictionary<string, string>();

        /// <summary>The sector a tag scope's tag names (via TaxonomyTerm), if it names one.</summary>
        public string? TagSector { get; init; }
    }

    /// <summary>The cursor's window state, mutable for the length of one request.</summary>
    private sealed class Window
    {
        public Window(FeedCursor? cursor)
        {
            Top = cursor?.Top;
            Floor = cursor?.Floor;
            Served = new HashSet<ulong>(cursor?.Served ?? new HashSet<ulong>());
            Exhausted = cursor?.Exhausted ?? false;
        }

        public FeedKey? Top { get; set; }
        public FeedKey? Floor { get; set; }
        public HashSet<ulong> Served { get; }
        public bool Exhausted { get; set; }
    }

    private sealed class CandidateRow
    {
        private ulong? _fingerprint;

        public string Id { get; init; } = string.Empty;
        public string AuthorId { get; init; } = string.Empty;
        public string? ProjectId { get; init; }
        public string? GuildId { get; init; }
        public string? OpportunityId { get; init; }
        public string? EventId { get; init; }
        public DateTime CreatedAt { get; init; }
        public decimal Quality { get; init; }
        public string[] Hashtags { get; init; } = [];
        public DateTime? ProjectPublishedAt { get; init; }
        public string? ProjectCategories { get; init; }

        public ulong Fingerprint => _fingerprint ??= FeedCursor.Fingerprint(Id);

        /// <summary>As UTC: Npgsql's legacy mode reads 'timestamp without time zone' as Unspecified; stored values are UTC.</summary>
        public FeedKey Key => new(DateTime.SpecifyKind(CreatedAt, DateTimeKind.Utc), Id);

        public FeedCandidate ToCandidate() => new(
            Id,
            AuthorId,
            ProjectId,
            GuildId,
            OpportunityId,
            EventId,
            CreatedAt,
            (double)Quality,
            ProjectPublishedAt,
            SplitCategories(ProjectCategories),
            Hashtags);
    }

    private sealed class HydrationRow
    {
        public string Id { get; init; } = string.Empty;
        public PostType Type { get; init; }
        public FeedSourceType? SourceType { get; init; }
        public string? SourceId { get; init; }
        public string AuthorId { get; init; } = string.Empty;
        public string? ProjectId { get; init; }
        public string? GuildId { get; init; }
        public string? OpportunityId { get; init; }
        public string? EventId { get; init; }
        public PostVisibility Visibility { get; init; }
        public string? Title { get; init; }
        public string Content { get; init; } = string.Empty;
        public string? Metadata { get; init; }
        public string[] Hashtags { get; init; } = [];
        public int Likes { get; init; }
        public int Comments { get; init; }
        public int Shares { get; init; }
        public DateTime CreatedAt { get; init; }

        /// <summary>The task a HELP_WANTED post asks for; NULL once that task is deleted (FK ON DELETE SET NULL).</summary>
        public string? TaskId { get; init; }

        public FeedSourceKey? SourceKey => SourceType is { } kind && SourceId is { } id ? new FeedSourceKey(kind, id) : null;
    }

    /// <summary>Project.categories is comma-separated sector codes (see ProjectQueryFilters.HasCategory).</summary>
    private static IReadOnlyList<string> SplitCategories(string? categories)
        => string.IsNullOrWhiteSpace(categories)
            ? []
            : categories.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(code => code.ToUpperInvariant())
                .Distinct(StringComparer.Ordinal)
                .ToList();
}
