namespace ArdaNova.Infrastructure.Feed;

using ArdaNova.Application.Feed;
using ArdaNova.Domain.Models.Enums;
using ArdaNova.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

/// <inheritdoc/>
/// <remarks>Data access only, AsNoTracking throughout; every visibility decision stays in FeedService.</remarks>
public sealed class FeedReadStore : IFeedReadStore
{
    /// <summary>How many of the viewer's most recent engaged posts their interest tags are read from.</summary>
    public const int EngagedPostWindow = 200;

    private readonly ArdaNovaDbContext _context;

    public FeedReadStore(ArdaNovaDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc/>
    public async Task<FeedViewerFacts> ReadViewerAsync(string viewerId, CancellationToken ct = default)
    {
        var followedProjects = await _context.ProjectFollows.AsNoTracking()
            .Where(follow => follow.userId == viewerId).Select(follow => follow.projectId).ToListAsync(ct);
        var memberProjects = await _context.ProjectMembers.AsNoTracking()
            .Where(member => member.userId == viewerId).Select(member => member.projectId)
            .Union(_context.Projects.Where(project => project.createdById == viewerId).Select(project => project.id))
            .ToListAsync(ct);
        var followedGuilds = await _context.GuildFollows.AsNoTracking()
            .Where(follow => follow.userId == viewerId).Select(follow => follow.guildId).ToListAsync(ct);
        var memberGuilds = await _context.GuildMembers.AsNoTracking()
            .Where(member => member.userId == viewerId).Select(member => member.guildId)
            .Union(_context.Guilds.Where(guild => guild.ownerId == viewerId).Select(guild => guild.id))
            .ToListAsync(ct);
        var followedPeople = await _context.UserFollows.AsNoTracking()
            .Where(follow => follow.followerId == viewerId).Select(follow => follow.followingId).ToListAsync(ct);

        var ownProjects = followedProjects.Concat(memberProjects).Distinct().ToArray();
        var categories = await _context.Projects.AsNoTracking()
            .Where(project => ownProjects.Contains(project.id))
            .Select(project => project.categories)
            .ToListAsync(ct);

        // Three index range reads - Post(authorId, createdAt), PostLike(userId, createdAt), PostComment(authorId,
        // createdAt) - each the viewer's latest 200, newest first, so the cost is O(200) whatever their history.
        // No hashtag filter here on purpose: joining Post to filter would make Postgres fetch and sort every like or
        // reply before the LIMIT. Untagged posts are read below and simply contribute no tags.
        var authored = await _context.Posts.AsNoTracking()
            .Where(post => post.authorId == viewerId)
            .OrderByDescending(post => post.createdAt)
            .Take(EngagedPostWindow)
            .Select(post => new Engagement(post.id, post.createdAt))
            .ToListAsync(ct);
        var liked = await _context.PostLikes.AsNoTracking()
            .Where(like => like.userId == viewerId)
            .OrderByDescending(like => like.createdAt)
            .Take(EngagedPostWindow)
            .Select(like => new Engagement(like.postId, like.createdAt))
            .ToListAsync(ct);
        var replied = await _context.PostComments.AsNoTracking()
            .Where(reply => reply.authorId == viewerId)
            .OrderByDescending(reply => reply.createdAt)
            .Take(EngagedPostWindow)
            .Select(reply => new Engagement(reply.postId, reply.createdAt))
            .ToListAsync(ct);
        var engagedIds = authored.Concat(liked).Concat(replied)
            .GroupBy(engagement => engagement.PostId, StringComparer.Ordinal)
            .OrderByDescending(group => group.Max(engagement => engagement.At))
            .Take(EngagedPostWindow)
            .Select(group => group.Key)
            .ToArray();
        var engaged = engagedIds.Length == 0 ? [] : await _context.Posts.AsNoTracking()
            .Where(post => engagedIds.Contains(post.id) && post.hashtags.Length > 0)
            .Select(post => post.hashtags)
            .ToListAsync(ct);

        var skills = await _context.UserSkills.AsNoTracking()
            .Where(skill => skill.userId == viewerId).Select(skill => skill.skill).ToListAsync(ct);

        return new FeedViewerFacts(
            followedProjects.ToHashSet(StringComparer.Ordinal),
            memberProjects.ToHashSet(StringComparer.Ordinal),
            followedGuilds.ToHashSet(StringComparer.Ordinal),
            memberGuilds.ToHashSet(StringComparer.Ordinal),
            followedPeople.ToHashSet(StringComparer.Ordinal),
            categories.SelectMany(SplitCategories).Distinct(StringComparer.Ordinal).ToList(),
            engaged.SelectMany(tags => tags).Distinct(StringComparer.Ordinal).ToList(),
            skills);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<FeedTaxonomyTerm>> ReadTaxonomyAsync(CancellationToken ct = default)
        => await _context.TaxonomyTerms.AsNoTracking()
            .Where(term => term.isActive)
            .Select(term => new FeedTaxonomyTerm(term.kind, term.slug, term.label, term.Parent != null ? term.Parent.slug : null))
            .ToListAsync(ct);

    /// <inheritdoc/>
    public async Task<FeedNames> ReadNamesAsync(FeedNameRequest request, CancellationToken ct = default)
    {
        var projectIds = request.ProjectIds.ToArray();
        var guildIds = request.GuildIds.ToArray();
        var opportunityIds = request.OpportunityIds.ToArray();
        var eventIds = request.EventIds.ToArray();
        var personIds = request.PersonIds.ToArray();

        var projects = projectIds.Length == 0 ? [] : await _context.Projects.AsNoTracking()
            .Where(project => projectIds.Contains(project.id))
            .Select(project => new { project.id, project.title, project.slug, project.summary, project.categories })
            .ToListAsync(ct);
        var guilds = guildIds.Length == 0 ? [] : await _context.Guilds.AsNoTracking()
            .Where(guild => guildIds.Contains(guild.id))
            .Select(guild => new FeedGuildName(guild.id, guild.name, guild.slug))
            .ToListAsync(ct);
        var opportunities = opportunityIds.Length == 0 ? [] : await _context.Opportunities.AsNoTracking()
            .Where(opportunity => opportunityIds.Contains(opportunity.id))
            .Select(opportunity => new FeedOpportunityName(opportunity.id, opportunity.title, opportunity.slug))
            .ToListAsync(ct);
        var events = eventIds.Length == 0 ? [] : await _context.Events.AsNoTracking()
            .Where(evt => eventIds.Contains(evt.id))
            .Select(evt => new FeedEventName(evt.id, evt.title, evt.startDate))
            .ToListAsync(ct);
        var people = personIds.Length == 0 ? [] : await _context.Users.AsNoTracking()
            .Where(user => personIds.Contains(user.id))
            .Select(user => new FeedPersonName(user.id, user.name, user.image))
            .ToListAsync(ct);

        return new FeedNames(
            projects.ToDictionary(
                project => project.id,
                project => new FeedProjectName(project.id, project.title, project.slug, project.summary, SplitCategories(project.categories)),
                StringComparer.Ordinal),
            guilds.ToDictionary(guild => guild.Id, StringComparer.Ordinal),
            opportunities.ToDictionary(opportunity => opportunity.Id, StringComparer.Ordinal),
            events.ToDictionary(evt => evt.Id, StringComparer.Ordinal),
            people.ToDictionary(person => person.Id, StringComparer.Ordinal));
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyDictionary<string, FeedHelpTask>> ReadHelpTasksAsync(
        IReadOnlyCollection<string> taskIds,
        CancellationToken ct = default)
    {
        var ids = taskIds.ToArray();
        if (ids.Length == 0)
            return new Dictionary<string, FeedHelpTask>(StringComparer.Ordinal);

        // One read: the task row, its assignee's name and an EXISTS on TaskCommerceAgreement(taskId).
        var tasks = await _context.ProjectTasks.AsNoTracking()
            .Where(task => ids.Contains(task.id))
            .Select(task => new FeedHelpTask(
                task.id,
                task.projectId,
                task.assignedToId,
                task.AssignedTo != null ? task.AssignedTo.name : null,
                task.status,
                task.escrowStatus,
                _context.TaskCommerceAgreements.Any(agreement => agreement.taskId == task.id),
                task.equityReward))
            .ToListAsync(ct);
        return tasks.ToDictionary(task => task.Id, StringComparer.Ordinal);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyDictionary<string, int>> ReadThreadReplyCountsAsync(
        FeedSourceType kind,
        IReadOnlyCollection<string> rootCommentIds,
        CancellationToken ct = default)
    {
        var roots = rootCommentIds.ToArray();
        if (roots.Length == 0)
            return new Dictionary<string, int>();

        var counts = kind switch
        {
            FeedSourceType.PROJECT_COMMENT => await _context.ProjectComments.AsNoTracking()
                .Where(reply => reply.parentId != null && roots.Contains(reply.parentId))
                .GroupBy(reply => reply.parentId!)
                .Select(group => new { Root = group.Key, Count = group.Count() })
                .ToListAsync(ct),
            FeedSourceType.OPPORTUNITY_COMMENT => await _context.OpportunityComments.AsNoTracking()
                .Where(reply => reply.parentId != null && roots.Contains(reply.parentId))
                .GroupBy(reply => reply.parentId!)
                .Select(group => new { Root = group.Key, Count = group.Count() })
                .ToListAsync(ct),
            _ => [],
        };
        return counts.ToDictionary(row => row.Root, row => row.Count, StringComparer.Ordinal);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<string>> ReadScorablePostIdsAsync(CancellationToken ct = default)
        => await _context.Posts.AsNoTracking()
            .Where(post => post.retractedAt == null)
            .OrderBy(post => post.id)
            .Select(post => post.id)
            .ToListAsync(ct);

    /// <inheritdoc/>
    public async Task<IReadOnlyList<FeedQualityRow>> ReadQualitySignalsAsync(
        IReadOnlyCollection<string> postIds,
        CancellationToken ct = default)
    {
        var ids = postIds.ToArray();
        if (ids.Length == 0)
            return [];

        var posts = await _context.Posts.AsNoTracking()
            .Where(post => ids.Contains(post.id))
            .Select(post => new { post.id, post.authorId, post.type, post.sourceType, post.sourceId, post.trendingScore })
            .ToListAsync(ct);

        // Distinct accounts other than the post's author, counted from the rows: a self-like, a repeated share or a
        // counter that drifted cannot move the score (Feed/AGENTS.md §Quality score).
        var likers = await CountOthersAsync(_context.PostLikes.AsNoTracking()
            .Where(like => ids.Contains(like.postId) && like.userId != like.Post!.authorId)
            .Select(like => new Engager { PostId = like.postId, UserId = like.userId }), ct);
        var sharers = await CountOthersAsync(_context.PostShares.AsNoTracking()
            .Where(share => ids.Contains(share.postId) && share.userId != share.Post!.authorId)
            .Select(share => new Engager { PostId = share.postId, UserId = share.userId }), ct);
        var bookmarkers = await CountOthersAsync(_context.PostBookmarks.AsNoTracking()
            .Where(bookmark => ids.Contains(bookmark.postId) && bookmark.userId != bookmark.Post!.authorId)
            .Select(bookmark => new Engager { PostId = bookmark.postId, UserId = bookmark.userId }), ct);

        // Repliers: on the card, plus a DISCUSSION card's native thread; one person counts once across both.
        var repliers = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        void AddReplier(string postId, string userId) =>
            (repliers.TryGetValue(postId, out var people) ? people : repliers[postId] = new HashSet<string>(StringComparer.Ordinal))
                .Add(userId);

        foreach (var reply in await _context.PostComments.AsNoTracking()
                     .Where(reply => ids.Contains(reply.postId) && reply.authorId != reply.Post!.authorId)
                     .Select(reply => new { reply.postId, reply.authorId })
                     .Distinct()
                     .ToListAsync(ct))
            AddReplier(reply.postId, reply.authorId);

        foreach (var kind in new[] { FeedSourceType.PROJECT_COMMENT, FeedSourceType.OPPORTUNITY_COMMENT })
        {
            var cardsByRoot = posts
                .Where(post => post.type == PostType.DISCUSSION && post.sourceType == kind && post.sourceId != null)
                .ToDictionary(post => post.sourceId!, StringComparer.Ordinal);
            foreach (var (root, userId) in await ReadThreadRepliersAsync(kind, cardsByRoot.Keys.ToArray(), ct))
            {
                var card = cardsByRoot[root];
                if (userId != card.authorId)
                    AddReplier(card.id, userId);
            }
        }

        return posts
            .Select(post => new FeedQualityRow(
                post.id,
                post.trendingScore,
                new FeedQualitySignals(
                    likers.GetValueOrDefault(post.id),
                    repliers.TryGetValue(post.id, out var people) ? people.Count : 0,
                    sharers.GetValueOrDefault(post.id),
                    bookmarkers.GetValueOrDefault(post.id))))
            .ToList();
    }

    /// <summary>COUNT(DISTINCT userId) per post, in SQL.</summary>
    private static Task<Dictionary<string, int>> CountOthersAsync(IQueryable<Engager> engagers, CancellationToken ct)
        => engagers
            .GroupBy(engager => engager.PostId)
            .Select(group => new { PostId = group.Key, People = group.Select(engager => engager.UserId).Distinct().Count() })
            .ToDictionaryAsync(row => row.PostId, row => row.People, StringComparer.Ordinal, ct);

    /// <summary>Distinct (root comment, replier) pairs under the given native-thread roots.</summary>
    private async Task<List<(string Root, string UserId)>> ReadThreadRepliersAsync(
        FeedSourceType kind,
        string[] roots,
        CancellationToken ct)
    {
        if (roots.Length == 0)
            return [];

        var pairs = kind switch
        {
            FeedSourceType.PROJECT_COMMENT => await _context.ProjectComments.AsNoTracking()
                .Where(reply => reply.parentId != null && roots.Contains(reply.parentId))
                .Select(reply => new { Root = reply.parentId!, reply.userId })
                .Distinct()
                .ToListAsync(ct),
            FeedSourceType.OPPORTUNITY_COMMENT => await _context.OpportunityComments.AsNoTracking()
                .Where(reply => reply.parentId != null && roots.Contains(reply.parentId))
                .Select(reply => new { Root = reply.parentId!, reply.userId })
                .Distinct()
                .ToListAsync(ct),
            _ => [],
        };
        return pairs.Select(pair => (pair.Root, pair.userId)).ToList();
    }

    /// <summary>One account's engagement with one post.</summary>
    private sealed class Engager
    {
        public string PostId { get; init; } = string.Empty;
        public string UserId { get; init; } = string.Empty;
    }

    /// <summary>When the viewer engaged with a post (wrote, liked or replied).</summary>
    private sealed record Engagement(string PostId, DateTime At);

    /// <summary>Project.categories is comma-separated sector codes.</summary>
    private static IReadOnlyList<string> SplitCategories(string? categories)
        => string.IsNullOrWhiteSpace(categories)
            ? []
            : categories.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(code => code.ToUpperInvariant())
                .ToList();
}
