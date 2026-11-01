namespace ArdaNova.Application.Feed;

/// <summary>
/// What a post has earned from OTHER people, as the quality score reads it: each signal counts distinct accounts that
/// are not the post's author, read from the engagement rows (never the raw counters). Views, dwell time and negative
/// reactions are absent by design, so no caller can feed them in; see Feed/AGENTS.md §Quality score.
/// </summary>
/// <param name="Approvals">Distinct non-author accounts that liked it.</param>
/// <param name="Replies">Distinct non-author accounts that replied (on the card, or in a DISCUSSION card's native thread).</param>
/// <param name="Shares">Distinct non-author accounts that shared it.</param>
/// <param name="Bookmarks">Distinct non-author accounts that bookmarked it.</param>
public sealed record FeedQualitySignals(int Approvals, int Replies, int Shares, int Bookmarks);

/// <summary>The stored, viewer-independent quality behind Post.trendingScore; see Feed/AGENTS.md §Quality score.</summary>
/// <remarks>Time-free on purpose: a score changes only when engagement does, so the pass rewrites only those rows.
/// New-project novelty is applied at read time by <see cref="FeedRanking"/>.</remarks>
public static class FeedQualityScore
{
    public const double ReplyWeight = 0.5;
    public const double ShareWeight = 2.0;
    public const double BookmarkWeight = 1.5;

    /// <summary>A score is never below this, so an unscored row (stored 0) ranks as a fresh one.</summary>
    public const double Baseline = 1.0;

    /// <summary>Capped-reply log-engagement score: 1 + ln(1 + A + 0.5*min(R, A) + 2*S + 1.5*B).</summary>
    public static decimal Compute(FeedQualitySignals signals)
    {
        var approvals = Math.Max(0, signals.Approvals);
        // Replies only count as far as approvals vouch for them: a flame thread cannot climb on volume.
        var vouchedReplies = Math.Min(Math.Max(0, signals.Replies), approvals);
        var engagement = approvals
            + ReplyWeight * vouchedReplies
            + ShareWeight * Math.Max(0, signals.Shares)
            + BookmarkWeight * Math.Max(0, signals.Bookmarks);

        return Math.Round((decimal)(Baseline + Math.Log(1 + engagement)), 8, MidpointRounding.AwayFromZero);
    }
}
