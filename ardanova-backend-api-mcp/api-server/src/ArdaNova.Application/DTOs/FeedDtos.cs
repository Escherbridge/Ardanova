namespace ArdaNova.Application.DTOs;

/// <summary>One page of the infinite ranked feed; a null NextCursor means nothing visible is left.</summary>
public sealed record FeedPageDto(IReadOnlyList<FeedCardDto> Items, string? NextCursor);

/// <summary>
/// One polymorphic card; clients render by <see cref="PostType"/> and must tolerate kinds they do not know.
/// See Application/Feed/AGENTS.md §Cards for what each field may carry (funding: a percent, never an amount or a backer).
/// </summary>
/// <param name="SourceType">The table an envelope was projected from; null for a post a person wrote.</param>
/// <param name="Body">The card's main copy: the person's own words when <paramref name="BodyIsUserWritten"/>, else a system line.</param>
/// <param name="Task">A HELP_WANTED card's task facts; null on every other card.</param>
public sealed record FeedCardDto(
    string Id,
    string PostType,
    string? SourceType,
    DateTime OccurredAt,
    FeedPersonDto Author,
    string? Title,
    string Body,
    bool BodyIsUserWritten,
    IReadOnlyList<string> Hashtags,
    FeedProjectSummaryDto? Project,
    FeedGuildSummaryDto? Guild,
    FeedLinkDto? Opportunity,
    FeedEventSummaryDto? Event,
    FeedFundingDto? Funding,
    FeedThreadDto? Thread,
    FeedCountersDto Counters,
    FeedViewerRelationDto Viewer,
    bool IsDiscovery,
    IReadOnlyList<FeedReasonDto> Reasons,
    FeedTaskDto? Task);

/// <summary>
/// The reader's standing toward the card's subject, computed server-side; clients pick the primary action from
/// this, never from Reasons text. All false for a signed-out reader.
/// </summary>
/// <param name="IsMember">On the card's project team (creator or member) or in its guild (owner or member).</param>
/// <param name="IsFollowing">Follows the card's project or guild.</param>
/// <param name="CanClaim">A HELP_WANTED card whose task this viewer may pick up in one tap (TaskClaimRule.CanClaimInPlace); false otherwise.</param>
public sealed record FeedViewerRelationDto(bool IsMember, bool IsFollowing, bool CanClaim);

/// <summary>
/// The task a HELP_WANTED card asks for, shown only to a viewer who can see its project (Feed/AGENTS.md §Help wanted).
/// </summary>
/// <param name="State">OPEN (unassigned, not completed), PICKED_UP (assigned, not completed) or DONE (COMPLETED).</param>
/// <param name="AssigneeName">The assignee's display name once the task is picked up; null while OPEN.</param>
/// <param name="Paid">equityReward &gt; 0: the task page runs the payout readiness check, so the card never claims in place.</param>
public sealed record FeedTaskDto(string Id, string State, string? AssigneeName, bool Paid);

public sealed record FeedPersonDto(string Id, string? Name, string? Image);

/// <summary>The card's project, as public as the card itself: no funding figures.</summary>
public sealed record FeedProjectSummaryDto(string Id, string Title, string Slug, string? Summary, IReadOnlyList<string> Sectors);

public sealed record FeedGuildSummaryDto(string Id, string Name, string Slug);

/// <summary>The card's opportunity; the opportunity page resolves by <paramref name="Slug"/>.</summary>
public sealed record FeedLinkDto(string Id, string Title, string Slug);

public sealed record FeedEventSummaryDto(string Id, string Title, DateTime StartsAt);

/// <summary>Aggregate progress only: the highest funding mark reached. Never an amount, never a backer.</summary>
public sealed record FeedFundingDto(int Percent);

/// <summary>Where a DISCUSSION card's conversation lives; replies go there, never onto the card.</summary>
public sealed record FeedThreadDto(
    string Source,
    string CommentId,
    string? TargetType,
    string? TargetId,
    string? OpportunityId);

/// <param name="Replies">Replies on the card; for a DISCUSSION card, the native thread's replies.</param>
public sealed record FeedCountersDto(int Likes, int Replies, int Shares);

/// <param name="Kind">Stable code (FeedReasonKind); <paramref name="Text"/> is the sentence to show.</param>
/// <param name="About">PROJECT, GUILD, PERSON, SECTOR or TAG when the reason names something.</param>
public sealed record FeedReasonDto(string Kind, string Text, string? About, string? AboutId);
