namespace ArdaNova.Application.Feed;

/// <summary>
/// What a post has earned, as the quality score reads it. Views, dwell time and negative reactions are absent by
/// design, so no caller can feed them in; see Feed/AGENTS.md §Quality score.
/// </summary>
/// <param name="Approvals">Likes.</param>
/// <param name="Replies">Replies on the card, plus the native thread's replies for a DISCUSSION card.</param>
/// <param name="ProjectPublishedAt">When the card's project opened to the public, if the card is about one.</param>
public sealed record FeedQualitySignals(
    int Approvals,
    int Replies,
    int Shares,
    int Bookmarks,
    DateTime? ProjectPublishedAt = null);

/// <summary>The stored, viewer-independent quality behind Post.trendingScore; see Feed/AGENTS.md §Quality score.</summary>
public static class FeedQualityScore
{
    public const double ReplyWeight = 0.5;
    public const double ShareWeight = 2.0;
    public const double BookmarkWeight = 1.5;
    public const double NewProjectBoost = 1.0;

    /// <summary>A score is never below this, so an unscored row (stored 0) ranks as a fresh one.</summary>
    public const double Baseline = 1.0;

    /// <summary>How long a newly opened project's cards carry the novelty boost (fading linearly to none).</summary>
    public static readonly TimeSpan NoveltyWindow = TimeSpan.FromDays(14);

    /// <summary>
    /// Capped-reply log-engagement score: 1 + ln(1 + A + 0.5*min(R, A) + 2*S + 1.5*B) + new-project boost.
    /// </summary>
    public static decimal Compute(FeedQualitySignals signals, DateTime now)
    {
        var approvals = Math.Max(0, signals.Approvals);
        // Replies only count as far as approvals vouch for them: a flame thread cannot climb on volume.
        var vouchedReplies = Math.Min(Math.Max(0, signals.Replies), approvals);
        var engagement = approvals
            + ReplyWeight * vouchedReplies
            + ShareWeight * Math.Max(0, signals.Shares)
            + BookmarkWeight * Math.Max(0, signals.Bookmarks);

        var quality = Baseline + Math.Log(1 + engagement) + NewProjectBoost * Novelty(signals.ProjectPublishedAt, now);
        return Math.Round((decimal)quality, 8, MidpointRounding.AwayFromZero);
    }

    /// <summary>1 the moment a project opens, fading linearly to 0 at the end of <see cref="NoveltyWindow"/>.</summary>
    public static double Novelty(DateTime? publishedAt, DateTime now)
    {
        if (publishedAt is not { } opened)
            return 0;

        var age = now - opened;
        if (age < TimeSpan.Zero)
            return 1;
        return age >= NoveltyWindow ? 0 : 1 - age / NoveltyWindow;
    }
}
