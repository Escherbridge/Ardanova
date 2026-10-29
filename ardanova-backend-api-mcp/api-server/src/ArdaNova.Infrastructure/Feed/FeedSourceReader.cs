namespace ArdaNova.Infrastructure.Feed;

using System.Linq.Expressions;
using ArdaNova.Application.Feed;
using ArdaNova.Domain.Models.Enums;
using ArdaNova.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

/// <inheritdoc/>
/// <remarks>Each kind is read by the timestamps that can change its envelope; see Feed/AGENTS.md §Windows.</remarks>
public sealed class FeedSourceReader : IFeedSourceReader
{
    private readonly ArdaNovaDbContext _context;

    public FeedSourceReader(ArdaNovaDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<FeedSource>> ReadChangedSinceAsync(
        FeedSourceType kind,
        DateTime? since,
        CancellationToken ct = default)
        => await ReadAsync(kind, new Scope(since, null), ct);

    /// <inheritdoc/>
    public async Task<IReadOnlyList<FeedSource>> ReadByIdsAsync(
        FeedSourceType kind,
        IReadOnlyCollection<string> rowIds,
        CancellationToken ct = default)
        => rowIds.Count == 0 ? [] : await ReadAsync(kind, new Scope(null, rowIds.ToArray()), ct);

    private Task<List<FeedSource>> ReadAsync(FeedSourceType kind, Scope scope, CancellationToken ct)
    {
        var ids = scope.Ids ?? [];
        var since = scope.Since ?? DateTime.MinValue;
        return kind switch
        {
            FeedSourceType.PROJECT => Read(
                Within(_context.Projects, scope, p => ids.Contains(p.id),
                    p => p.publishedAt >= since || p.updatedAt >= since),
                p => new ProjectLaunchSource(p.id, p.title, p.createdById, p.publishedAt), ct),

            FeedSourceType.PROJECT_UPDATE => Read(
                Within(_context.ProjectUpdates, scope, u => ids.Contains(u.id), u => u.createdAt >= since),
                u => new ProjectUpdateSource(u.id, u.projectId, u.userId, u.title, u.createdAt), ct),

            FeedSourceType.PROJECT_MILESTONE => Read(
                Within(_context.ProjectMilestones, scope, m => ids.Contains(m.id),
                    m => m.completedAt >= since || m.updatedAt >= since),
                m => new ProjectMilestoneSource(m.id, m.projectId, m.Project!.createdById, m.title, m.completedAt,
                    m.assigneeId), ct),

            FeedSourceType.PROJECT_TASK => Read(
                Within(_context.ProjectTasks, scope, t => ids.Contains(t.id),
                    t => t.completedAt >= since || t.updatedAt >= since),
                t => new ProjectTaskSource(t.id, t.projectId, t.Project!.createdById, t.title, t.completedAt,
                    t.assignedToId), ct),

            FeedSourceType.PROJECT_TOKEN_CONFIG => Read(
                Within(_context.ProjectTokenConfigs, scope, c => ids.Contains(c.id), c => c.updatedAt >= since),
                c => new ProjectFundingSource(c.id, c.projectId, c.Project!.createdById, c.Project.title,
                    c.fundingGoal, c.fundingRaised, c.updatedAt), ct),

            FeedSourceType.PROJECT_MEMBER => Read(
                Within(_context.ProjectMembers, scope, m => ids.Contains(m.id), m => m.joinedAt >= since),
                m => new ProjectMemberSource(m.id, m.projectId, m.Project!.createdById, m.Project.title, m.userId,
                    m.joinedAt), ct),

            FeedSourceType.PROJECT_COMMENT => Read(
                Within(_context.ProjectComments, scope, c => ids.Contains(c.id), c => c.createdAt >= since),
                c => new ProjectCommentSource(c.id, c.projectId, c.userId, c.parentId, c.targetType, c.targetId,
                    c.createdAt), ct),

            FeedSourceType.GUILD => Read(
                Within(_context.Guilds, scope, g => ids.Contains(g.id), g => g.createdAt >= since),
                g => new GuildFoundedSource(g.id, g.name, g.ownerId, g.createdAt), ct),

            FeedSourceType.GUILD_UPDATE => Read(
                Within(_context.GuildUpdates, scope, u => ids.Contains(u.id), u => u.createdAt >= since),
                u => new GuildUpdateSource(u.id, u.guildId, u.createdById, u.title, u.createdAt), ct),

            FeedSourceType.GUILD_MEMBER => Read(
                Within(_context.GuildMembers, scope, m => ids.Contains(m.id), m => m.joinedAt >= since),
                m => new GuildMemberSource(m.id, m.guildId, m.Guild!.ownerId, m.Guild.name, m.userId, m.joinedAt), ct),

            FeedSourceType.OPPORTUNITY => Read(
                Within(_context.Opportunities, scope, o => ids.Contains(o.id), o => o.updatedAt >= since),
                o => new OpportunitySource(o.id, o.title, o.posterId, o.status, o.projectId, o.guildId, o.updatedAt), ct),

            // The parent's timestamp is read too: opening (or re-drafting) an opportunity changes whether
            // its existing updates and comments may be in the feed.
            FeedSourceType.OPPORTUNITY_UPDATE => Read(
                Within(_context.OpportunityUpdates, scope, u => ids.Contains(u.id),
                    u => u.createdAt >= since || u.Opportunity!.updatedAt >= since),
                u => new OpportunityUpdateSource(u.id, u.opportunityId, u.userId, u.title, u.createdAt,
                    u.Opportunity!.status, u.Opportunity.projectId, u.Opportunity.guildId), ct),

            FeedSourceType.OPPORTUNITY_COMMENT => Read(
                Within(_context.OpportunityComments, scope, c => ids.Contains(c.id),
                    c => c.createdAt >= since || c.Opportunity!.updatedAt >= since),
                c => new OpportunityCommentSource(c.id, c.opportunityId, c.userId, c.parentId, c.createdAt,
                    c.Opportunity!.status, c.Opportunity.projectId, c.Opportunity.guildId), ct),

            FeedSourceType.EVENT => Read(
                Within(_context.Events, scope, e => ids.Contains(e.id), e => e.updatedAt >= since),
                e => new EventSource(e.id, e.title, e.organizerId, e.status, e.visibility, e.projectId, e.guildId,
                    e.startDate, e.updatedAt), ct),

            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "No feed source reader for this kind."),
        };
    }

    private static IQueryable<TEntity> Within<TEntity>(
        IQueryable<TEntity> rows,
        Scope scope,
        Expression<Func<TEntity, bool>> byIds,
        Expression<Func<TEntity, bool>> changedSince)
        => scope.Ids is not null ? rows.Where(byIds)
            : scope.Since is not null ? rows.Where(changedSince)
            : rows;

    private static async Task<List<FeedSource>> Read<TEntity, TSource>(
        IQueryable<TEntity> rows,
        Expression<Func<TEntity, TSource>> projection,
        CancellationToken ct)
        where TEntity : class
        where TSource : FeedSource
        => (await rows.AsNoTracking().Select(projection).ToListAsync(ct)).Cast<FeedSource>().ToList();

    private sealed record Scope(DateTime? Since, string[]? Ids);
}
