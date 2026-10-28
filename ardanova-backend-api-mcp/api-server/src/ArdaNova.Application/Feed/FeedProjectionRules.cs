namespace ArdaNova.Application.Feed;

using System.Linq.Expressions;
using System.Text.Json;
using ArdaNova.Domain.Models.Entities;
using ArdaNova.Domain.Models.Enums;
using ArdaNova.Domain.Policies;

/// <summary>The row an envelope was projected from: (sourceType, sourceId), unique on Post.</summary>
public readonly record struct FeedSourceKey(FeedSourceType SourceType, string SourceId);

/// <summary>The rows an envelope points at; the one that matches its source kind is its reason to exist.</summary>
public sealed record FeedEnvelopeTargets(
    string? ProjectId = null,
    string? GuildId = null,
    string? OpportunityId = null,
    string? EventId = null);

/// <summary>An envelope the rules want to exist, before it is a Post row.</summary>
public sealed record FeedEnvelope(
    PostType Type,
    FeedSourceKey Key,
    PostVisibility Visibility,
    string AuthorId,
    DateTime OccurredAt,
    string? Title,
    string Content,
    string? Metadata,
    FeedEnvelopeTargets Targets);

/// <summary>An envelope already stored, as far as the projector needs to judge it.</summary>
public sealed record StoredFeedEnvelope(
    string Id,
    FeedSourceKey Key,
    PostVisibility Visibility,
    FeedEnvelopeTargets Targets);

/// <summary>A feedable source row, as the facts the rules read; one sealed record per FeedSourceType.</summary>
public abstract record FeedSource(string Id)
{
    public abstract FeedSourceType Kind { get; }
}

public sealed record ProjectLaunchSource(string Id, string Title, string OwnerId, DateTime? PublishedAt)
    : FeedSource(Id)
{
    public override FeedSourceType Kind => FeedSourceType.PROJECT;
}

public sealed record ProjectUpdateSource(string Id, string ProjectId, string AuthorId, string Title, DateTime CreatedAt)
    : FeedSource(Id)
{
    public override FeedSourceType Kind => FeedSourceType.PROJECT_UPDATE;
}

public sealed record ProjectMilestoneSource(
    string Id, string ProjectId, string ProjectOwnerId, string Title, DateTime? CompletedAt)
    : FeedSource(Id)
{
    public override FeedSourceType Kind => FeedSourceType.PROJECT_MILESTONE;
}

public sealed record ProjectTaskSource(
    string Id, string ProjectId, string ProjectOwnerId, string Title, DateTime? CompletedAt)
    : FeedSource(Id)
{
    public override FeedSourceType Kind => FeedSourceType.PROJECT_TASK;
}

/// <summary>A project's funding gate; Goal and Raised are read only to derive a percent and never leave the rules.</summary>
public sealed record ProjectFundingSource(
    string Id,
    string ProjectId,
    string ProjectOwnerId,
    string ProjectTitle,
    double Goal,
    double Raised,
    DateTime UpdatedAt)
    : FeedSource(Id)
{
    public override FeedSourceType Kind => FeedSourceType.PROJECT_TOKEN_CONFIG;
}

public sealed record ProjectMemberSource(
    string Id, string ProjectId, string ProjectOwnerId, string ProjectTitle, string UserId, DateTime JoinedAt)
    : FeedSource(Id)
{
    public override FeedSourceType Kind => FeedSourceType.PROJECT_MEMBER;
}

public sealed record ProjectCommentSource(
    string Id,
    string ProjectId,
    string AuthorId,
    string? ParentId,
    CommentTargetType TargetType,
    string TargetId,
    DateTime CreatedAt)
    : FeedSource(Id)
{
    public override FeedSourceType Kind => FeedSourceType.PROJECT_COMMENT;
}

public sealed record GuildFoundedSource(string Id, string Name, string OwnerId, DateTime CreatedAt)
    : FeedSource(Id)
{
    public override FeedSourceType Kind => FeedSourceType.GUILD;
}

public sealed record GuildUpdateSource(string Id, string GuildId, string AuthorId, string Title, DateTime CreatedAt)
    : FeedSource(Id)
{
    public override FeedSourceType Kind => FeedSourceType.GUILD_UPDATE;
}

public sealed record GuildMemberSource(
    string Id, string GuildId, string GuildOwnerId, string GuildName, string UserId, DateTime JoinedAt)
    : FeedSource(Id)
{
    public override FeedSourceType Kind => FeedSourceType.GUILD_MEMBER;
}

public sealed record OpportunitySource(
    string Id,
    string Title,
    string PosterId,
    OpportunityStatus Status,
    string? ProjectId,
    string? GuildId,
    DateTime UpdatedAt)
    : FeedSource(Id)
{
    public override FeedSourceType Kind => FeedSourceType.OPPORTUNITY;
}

public sealed record OpportunityUpdateSource(
    string Id,
    string OpportunityId,
    string AuthorId,
    string Title,
    DateTime CreatedAt,
    OpportunityStatus OpportunityStatus,
    string? ProjectId,
    string? GuildId)
    : FeedSource(Id)
{
    public override FeedSourceType Kind => FeedSourceType.OPPORTUNITY_UPDATE;
}

public sealed record OpportunityCommentSource(
    string Id,
    string OpportunityId,
    string AuthorId,
    string? ParentId,
    DateTime CreatedAt,
    OpportunityStatus OpportunityStatus,
    string? ProjectId,
    string? GuildId)
    : FeedSource(Id)
{
    public override FeedSourceType Kind => FeedSourceType.OPPORTUNITY_COMMENT;
}

public sealed record EventSource(
    string Id,
    string Title,
    string OrganizerId,
    EventStatus Status,
    EventVisibility Visibility,
    string? ProjectId,
    string? GuildId,
    DateTime StartDate,
    DateTime UpdatedAt)
    : FeedSource(Id)
{
    public override FeedSourceType Kind => FeedSourceType.EVENT;
}

/// <summary>The system lines a projected card carries; member-facing, so "funds"/"units", never "token".</summary>
public static class FeedEnvelopeCopy
{
    public const string ProjectLaunched = "Opened to the public.";
    public const string MilestoneReached = "Reached a milestone.";
    public const string TaskCompleted = "Finished a piece of work.";
    public const string OpportunityOpened = "Opened a new opportunity.";
    public const string GuildFounded = "Founded a new guild.";
    public const string EventScheduled = "Scheduled an event.";
    public const string JoinedProject = "Joined the project team.";
    public const string JoinedGuild = "Joined the guild.";

    /// <summary>User-written text (updates, comments) is hydrated live from its source, never copied here.</summary>
    public const string HydratedFromSource = "";

    public static string FundingProgress(int percent) => $"Reached {percent}% of its funding goal.";
}

/// <summary>What becomes a feed envelope, who may see it, and when it must go; see Feed/AGENTS.md §Mapping.</summary>
public static class FeedProjectionRules
{
    /// <summary>The funding marks a card is written for; only the highest one reached is projected.</summary>
    public static readonly IReadOnlyList<int> FundingThresholds = [25, 50, 75, 100];

    /// <summary>The envelope a source row should have, or null while it should have none.</summary>
    public static FeedEnvelope? Enter(FeedSource source) => source switch
    {
        ProjectLaunchSource project => project.PublishedAt is { } publishedAt
            ? Envelope(PostType.PROJECT_LAUNCHED, project, PostVisibility.PUBLIC, project.OwnerId, publishedAt,
                project.Title, FeedEnvelopeCopy.ProjectLaunched, null, new(ProjectId: project.Id))
            : null,

        ProjectUpdateSource update => Envelope(PostType.PROJECT_UPDATE, update, PostVisibility.PUBLIC,
            update.AuthorId, update.CreatedAt, update.Title, FeedEnvelopeCopy.HydratedFromSource, null,
            new(ProjectId: update.ProjectId)),

        ProjectMilestoneSource milestone => milestone.CompletedAt is { } reachedAt
            ? Envelope(PostType.MILESTONE, milestone, PostVisibility.PUBLIC, milestone.ProjectOwnerId, reachedAt,
                milestone.Title, FeedEnvelopeCopy.MilestoneReached, null, new(ProjectId: milestone.ProjectId))
            : null,

        ProjectTaskSource task => task.CompletedAt is { } finishedAt
            ? Envelope(PostType.TASK_COMPLETED, task, PostVisibility.PUBLIC, task.ProjectOwnerId, finishedAt,
                task.Title, FeedEnvelopeCopy.TaskCompleted, null, new(ProjectId: task.ProjectId))
            : null,

        ProjectFundingSource funding => HighestFundingThreshold(funding.Goal, funding.Raised) is { } percent
            ? new FeedEnvelope(
                PostType.FUNDING_PROGRESS,
                FundingKey(funding.Id, percent),
                PostVisibility.PUBLIC,
                funding.ProjectOwnerId,
                funding.UpdatedAt,
                funding.ProjectTitle,
                FeedEnvelopeCopy.FundingProgress(percent),
                JsonSerializer.Serialize(new { percent }),
                new(ProjectId: funding.ProjectId))
            : null,

        ProjectMemberSource member => member.UserId == member.ProjectOwnerId
            ? null
            : Envelope(PostType.MEMBER_JOINED, member, PostVisibility.PUBLIC, member.UserId, member.JoinedAt,
                member.ProjectTitle, FeedEnvelopeCopy.JoinedProject, null, new(ProjectId: member.ProjectId)),

        ProjectCommentSource comment => comment.ParentId is not null
            ? null
            : Envelope(PostType.DISCUSSION, comment, DiscussionVisibility(comment.TargetType), comment.AuthorId,
                comment.CreatedAt, null, FeedEnvelopeCopy.HydratedFromSource,
                JsonSerializer.Serialize(new
                {
                    thread = new
                    {
                        source = nameof(FeedSourceType.PROJECT_COMMENT),
                        targetType = comment.TargetType.ToString(),
                        targetId = comment.TargetId,
                        commentId = comment.Id,
                    },
                }),
                new(ProjectId: comment.ProjectId)),

        GuildFoundedSource guild => Envelope(PostType.GUILD_FOUNDED, guild, PostVisibility.PUBLIC, guild.OwnerId,
            guild.CreatedAt, guild.Name, FeedEnvelopeCopy.GuildFounded, null, new(GuildId: guild.Id)),

        GuildUpdateSource update => Envelope(PostType.GUILD_UPDATE, update, PostVisibility.PUBLIC, update.AuthorId,
            update.CreatedAt, update.Title, FeedEnvelopeCopy.HydratedFromSource, null, new(GuildId: update.GuildId)),

        GuildMemberSource member => member.UserId == member.GuildOwnerId
            ? null
            : Envelope(PostType.MEMBER_JOINED, member, PostVisibility.PUBLIC, member.UserId, member.JoinedAt,
                member.GuildName, FeedEnvelopeCopy.JoinedGuild, null, new(GuildId: member.GuildId)),

        OpportunitySource opportunity => opportunity.Status == OpportunityStatus.OPEN
            ? Envelope(PostType.OPPORTUNITY_OPENED, opportunity, PostVisibility.PUBLIC, opportunity.PosterId,
                opportunity.UpdatedAt, opportunity.Title, FeedEnvelopeCopy.OpportunityOpened, null,
                new(opportunity.ProjectId, opportunity.GuildId, OpportunityId: opportunity.Id))
            : null,

        OpportunityUpdateSource update => update.OpportunityStatus == OpportunityStatus.DRAFT
            ? null
            : Envelope(PostType.OPPORTUNITY_UPDATE, update, PostVisibility.PUBLIC, update.AuthorId, update.CreatedAt,
                update.Title, FeedEnvelopeCopy.HydratedFromSource, null,
                new(update.ProjectId, update.GuildId, OpportunityId: update.OpportunityId)),

        OpportunityCommentSource comment => comment.ParentId is not null
            || comment.OpportunityStatus == OpportunityStatus.DRAFT
            ? null
            : Envelope(PostType.DISCUSSION, comment, PostVisibility.PUBLIC, comment.AuthorId, comment.CreatedAt, null,
                FeedEnvelopeCopy.HydratedFromSource,
                JsonSerializer.Serialize(new
                {
                    thread = new
                    {
                        source = nameof(FeedSourceType.OPPORTUNITY_COMMENT),
                        opportunityId = comment.OpportunityId,
                        commentId = comment.Id,
                    },
                }),
                new(comment.ProjectId, comment.GuildId, OpportunityId: comment.OpportunityId)),

        EventSource evt => evt.Status is EventStatus.SCHEDULED or EventStatus.LIVE
            && EventAudience(evt) is { } audience
            ? Envelope(PostType.EVENT_SCHEDULED, evt, audience, evt.OrganizerId, evt.UpdatedAt, evt.Title,
                FeedEnvelopeCopy.EventScheduled,
                JsonSerializer.Serialize(new { startsAt = evt.StartDate }),
                new(evt.ProjectId, evt.GuildId, EventId: evt.Id))
            : null,

        _ => throw new ArgumentOutOfRangeException(nameof(source), source.Kind, "No feed rule for this source."),
    };

    /// <summary>The audience an existing envelope should have now, or null when it must be removed.</summary>
    /// <remarks>Wider than <see cref="Enter"/> on purpose: an opportunity that has closed or an event that has
    /// finished really did open or happen. Never wider on privacy - a draft or invite-only row always goes.</remarks>
    public static PostVisibility? Retain(FeedSource source) => source switch
    {
        ProjectFundingSource => PostVisibility.PUBLIC,
        OpportunitySource opportunity => opportunity.Status == OpportunityStatus.DRAFT ? null : PostVisibility.PUBLIC,
        EventSource evt => evt.Status is EventStatus.DRAFT or EventStatus.CANCELLED ? null : EventAudience(evt),
        _ => Enter(source)?.Visibility,
    };

    /// <summary>The id of the source row inside a key; a funding key carries its threshold after the row id.</summary>
    public static string RowIdOf(FeedSourceKey key)
    {
        if (key.SourceType != FeedSourceType.PROJECT_TOKEN_CONFIG)
            return key.SourceId;

        var separator = key.SourceId.LastIndexOf(':');
        return separator > 0 ? key.SourceId[..separator] : key.SourceId;
    }

    /// <summary>The funding key: sourceId = "{configId}:{percent}", one envelope per mark.</summary>
    public static FeedSourceKey FundingKey(string configId, int percent)
        => new(FeedSourceType.PROJECT_TOKEN_CONFIG, $"{configId}:{percent}");

    /// <summary>The highest funding mark reached, or null when no goal is set or the figures are unreadable.</summary>
    public static int? HighestFundingThreshold(double goal, double raised)
    {
        // ProjectFundingLedger is the one place the goal/raise pair is interpreted.
        var config = new ProjectTokenConfig { fundingGoal = goal, fundingRaised = raised };
        if (!ProjectFundingLedger.TryRead(config, out var figures) || !figures.GoalIsSet || figures.RaisedIsBroken)
            return null;
        if (figures.GoalIsMet)
            return FundingThresholds[^1];

        var percent = figures.RaisedUsd * 100m / figures.GoalUsd;
        int? reached = null;
        foreach (var threshold in FundingThresholds)
        {
            if (percent >= threshold)
                reached = threshold;
        }

        return reached;
    }

    /// <summary>
    /// An envelope whose defining target FK is NULL: that row was deleted (the FKs are ON DELETE SET NULL),
    /// so the envelope is as gone as its source.
    /// </summary>
    public static readonly Expression<Func<Post, bool>> HasLostItsTarget = post =>
        (post.projectId == null
            && (post.sourceType == FeedSourceType.PROJECT
                || post.sourceType == FeedSourceType.PROJECT_UPDATE
                || post.sourceType == FeedSourceType.PROJECT_MILESTONE
                || post.sourceType == FeedSourceType.PROJECT_TASK
                || post.sourceType == FeedSourceType.PROJECT_TOKEN_CONFIG
                || post.sourceType == FeedSourceType.PROJECT_MEMBER
                || post.sourceType == FeedSourceType.PROJECT_COMMENT))
        || (post.guildId == null
            && (post.sourceType == FeedSourceType.GUILD
                || post.sourceType == FeedSourceType.GUILD_UPDATE
                || post.sourceType == FeedSourceType.GUILD_MEMBER))
        || (post.opportunityId == null
            && (post.sourceType == FeedSourceType.OPPORTUNITY
                || post.sourceType == FeedSourceType.OPPORTUNITY_UPDATE
                || post.sourceType == FeedSourceType.OPPORTUNITY_COMMENT))
        || (post.eventId == null && post.sourceType == FeedSourceType.EVENT);

    /// <summary>The Post row an envelope is stored as; counters start at zero like any new post.</summary>
    public static Post ToPost(FeedEnvelope envelope, DateTime projectedAt) => new()
    {
        id = Guid.NewGuid().ToString(),
        authorId = envelope.AuthorId,
        projectId = envelope.Targets.ProjectId,
        guildId = envelope.Targets.GuildId,
        opportunityId = envelope.Targets.OpportunityId,
        eventId = envelope.Targets.EventId,
        type = envelope.Type,
        visibility = envelope.Visibility,
        title = envelope.Title,
        content = envelope.Content,
        metadata = envelope.Metadata,
        sourceType = envelope.Key.SourceType,
        sourceId = envelope.Key.SourceId,
        likesCount = 0,
        commentsCount = 0,
        sharesCount = 0,
        viewsCount = 0,
        isPinned = false,
        isTrending = false,
        trendingScore = 0,
        createdAt = envelope.OccurredAt,
        updatedAt = projectedAt,
    };

    /// <summary>Project- and milestone-level talk is public; talk on backlog work stays with the team.</summary>
    private static PostVisibility DiscussionVisibility(CommentTargetType target)
        => target is CommentTargetType.PROJECT or CommentTargetType.MILESTONE
            ? PostVisibility.PUBLIC
            : PostVisibility.PROJECT_MEMBERS;

    /// <summary>An event's own audience; INVITE_ONLY, or a members-only event with no scope, has no feed audience.</summary>
    private static PostVisibility? EventAudience(EventSource evt) => evt.Visibility switch
    {
        EventVisibility.PUBLIC => PostVisibility.PUBLIC,
        EventVisibility.PROJECT_MEMBERS when !string.IsNullOrWhiteSpace(evt.ProjectId) => PostVisibility.PROJECT_MEMBERS,
        EventVisibility.GUILD_MEMBERS when !string.IsNullOrWhiteSpace(evt.GuildId) => PostVisibility.GUILD_MEMBERS,
        _ => null,
    };

    private static FeedEnvelope Envelope(
        PostType type,
        FeedSource source,
        PostVisibility visibility,
        string authorId,
        DateTime occurredAt,
        string? title,
        string content,
        string? metadata,
        FeedEnvelopeTargets targets)
        => new(type, new FeedSourceKey(source.Kind, source.Id), visibility, authorId, occurredAt, title, content,
            metadata, targets);
}
