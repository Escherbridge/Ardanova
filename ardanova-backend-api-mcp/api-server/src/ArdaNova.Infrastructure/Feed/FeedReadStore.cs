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

        // The viewer's own words and the posts they endorsed or answered, newest first.
        var engaged = await _context.Posts.AsNoTracking()
            .Where(post => post.hashtags.Length > 0)
            .Where(post => post.authorId == viewerId
                || post.PostLikes.Any(like => like.userId == viewerId)
                || post.PostComments.Any(reply => reply.authorId == viewerId))
            .OrderByDescending(post => post.createdAt)
            .Take(EngagedPostWindow)
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
            .Select(opportunity => new { opportunity.id, opportunity.title })
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
            opportunities.ToDictionary(opportunity => opportunity.id, opportunity => opportunity.title, StringComparer.Ordinal),
            events.ToDictionary(evt => evt.Id, StringComparer.Ordinal),
            people.ToDictionary(person => person.Id, StringComparer.Ordinal));
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
            .Select(post => new
            {
                post.id,
                post.type,
                post.sourceType,
                post.sourceId,
                post.likesCount,
                post.commentsCount,
                post.sharesCount,
                post.trendingScore,
                ProjectPublishedAt = post.Project != null ? post.Project.publishedAt : null,
            })
            .ToListAsync(ct);
        var bookmarks = await _context.PostBookmarks.AsNoTracking()
            .Where(bookmark => ids.Contains(bookmark.postId))
            .GroupBy(bookmark => bookmark.postId)
            .Select(group => new { PostId = group.Key, Count = group.Count() })
            .ToDictionaryAsync(row => row.PostId, row => row.Count, ct);

        var threadReplies = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var kind in new[] { FeedSourceType.PROJECT_COMMENT, FeedSourceType.OPPORTUNITY_COMMENT })
        {
            var roots = posts.Where(post => post.type == PostType.DISCUSSION && post.sourceType == kind && post.sourceId != null)
                .Select(post => post.sourceId!).ToList();
            foreach (var (root, count) in await ReadThreadReplyCountsAsync(kind, roots, ct))
                threadReplies[root] = count;
        }

        return posts
            .Select(post => new FeedQualityRow(
                post.id,
                post.trendingScore,
                new FeedQualitySignals(
                    post.likesCount,
                    post.commentsCount + (post.sourceId is { } root ? threadReplies.GetValueOrDefault(root) : 0),
                    post.sharesCount,
                    bookmarks.GetValueOrDefault(post.id),
                    post.ProjectPublishedAt)))
            .ToList();
    }

    /// <summary>Project.categories is comma-separated sector codes.</summary>
    private static IReadOnlyList<string> SplitCategories(string? categories)
        => string.IsNullOrWhiteSpace(categories)
            ? []
            : categories.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(code => code.ToUpperInvariant())
                .ToList();
}
