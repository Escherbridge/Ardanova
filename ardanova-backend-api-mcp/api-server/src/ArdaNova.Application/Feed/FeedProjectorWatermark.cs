namespace ArdaNova.Application.Feed;

using ArdaNova.Domain.Models.Enums;

/// <summary>
/// High-water mark per source kind: when the last cycle that read AND applied that kind began. In memory, one
/// per runner (the hosted service), not thread-safe. See Feed/AGENTS.md §Windows for the restart behaviour.
/// </summary>
public sealed class FeedProjectorWatermark
{
    private readonly Dictionary<FeedSourceType, DateTime> _lastReadAt = new();

    /// <summary>
    /// Where a cycle starting at <paramref name="now"/> reads <paramref name="kind"/> from:
    /// min(lastReadAt - margin, now - lookBack), so a stall is caught up instead of skipped.
    /// </summary>
    /// <remarks>The margin is the look-back itself: a row stamped just before the last read began but committed
    /// after it is exactly as late as any row a normal cycle tolerates.</remarks>
    public DateTime SinceFor(FeedSourceType kind, DateTime now, TimeSpan lookBack)
    {
        var steadyState = now - lookBack;
        return _lastReadAt.TryGetValue(kind, out var lastReadAt) && lastReadAt - lookBack < steadyState
            ? lastReadAt - lookBack
            : steadyState;
    }

    /// <summary>Records that a read of <paramref name="kind"/> begun at <paramref name="readAt"/> was fully applied.</summary>
    public void Advance(FeedSourceType kind, DateTime readAt)
    {
        if (!_lastReadAt.TryGetValue(kind, out var previous) || readAt > previous)
            _lastReadAt[kind] = readAt;
    }
}
