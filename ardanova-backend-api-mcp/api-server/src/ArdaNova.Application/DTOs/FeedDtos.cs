namespace ArdaNova.Application.DTOs;

/// <summary>One page of the infinite ranked feed; a null NextCursor means nothing visible is left.</summary>
public sealed record FeedPageDto(IReadOnlyList<FeedCardDto> Items, string? NextCursor);

/// <summary>
/// One polymorphic card; clients render by <see cref="PostType"/> and must tolerate kinds they do not know.
/// See Application/Feed/AGENTS.md §Cards for what each field may carry (funding: a percent, never an amount or a backer).
/// </summary>
/// <param name="SourceType">The table an envelope was projected from; null for a post a person wrote.</param>
/// <param name="Body">The card's main copy: the person's own words when <paramref name="BodyIsUserWritten"/>, else a system line.</param>
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
    IReadOnlyList<FeedReasonDto> Reasons);

/// <summary>
/// The reader's standing toward the card's subject, computed server-side; clients pick the primary action from
/// this, never from Reasons text. All false for a signed-out reader.
/// </summary>
/// <param name="IsMember">On the card's project team (creator or member) or in its guild (owner or member).</param>
/// <param name="IsFollowing">Follows the card's project or guild.</param>
/// <param name="CanClaim">Always false until claimable (help-wanted / task-linked) cards exist.</param>
public sealed record FeedViewerRelationDto(bool IsMember, bool IsFollowing, bool CanClaim);

public sealed record FeedPersonDto(string Id, string? Name, string? Image);

/// <summary>The card's project, as public as the card itself: no funding figures.</summary>
public sealed record FeedProjectSummaryDto(string Id, string Title, string Slug, string? Summary, IReadOnlyList<string> Sectors);

public sealed record FeedGuildSummaryDto(string Id, string Name, string Slug);

public sealed record FeedLinkDto(string Id, string Title);

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
