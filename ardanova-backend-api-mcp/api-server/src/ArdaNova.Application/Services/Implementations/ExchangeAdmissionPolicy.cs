namespace ArdaNova.Application.Services.Implementations;

using System.Globalization;

/// <summary>
/// The admission deadline every exchange send carries (<c>Admit-Before</c>) and the window after which
/// the node's NOT_FOUND for a key is believed. See <c>Services/Implementations/AGENTS.md</c> §Node proof.
/// </summary>
public sealed record ExchangeAdmissionPolicy
{
    /// <summary>Configuration section the operator settings bind from.</summary>
    public const string SectionName = "Azoa:Exchange";

    /// <summary>The request header the node reads the deadline from.</summary>
    public const string AdmitBeforeHeader = "Admit-Before";

    /// <summary>W_admit: how long after its durable start stamp a send may still be admitted.</summary>
    public static readonly TimeSpan DefaultAdmitWindow = TimeSpan.FromMinutes(4);

    /// <summary>W: how long after the latest send stamp a NOT_FOUND read is believed.</summary>
    public static readonly TimeSpan DefaultQuietWindow = TimeSpan.FromMinutes(5);

    /// <summary>The largest BFF-to-node clock difference the proof tolerates (both hosts are NTP-synced).</summary>
    public static readonly TimeSpan DefaultMaximumClockSkew = TimeSpan.FromSeconds(30);

    /// <summary>The window round 4's proof used before any node enforced a deadline: the worker's 5-minute lease.</summary>
    public static readonly TimeSpan LegacyQuietWindow = TimeSpan.FromMinutes(5);

    private static readonly TimeSpan MaximumAdmitWindow = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan MaximumQuietWindow = TimeSpan.FromHours(1);

    /// <summary>The shipped defaults: W_admit 4 min, W 5 min, skew 30 s, enforcement not yet claimed.</summary>
    public static ExchangeAdmissionPolicy Default { get; } = new();

    public TimeSpan AdmitWindow { get; init; } = DefaultAdmitWindow;

    public TimeSpan QuietWindow { get; init; } = DefaultQuietWindow;

    public TimeSpan MaximumClockSkew { get; init; } = DefaultMaximumClockSkew;

    /// <summary>Operator attestation that the node refuses admission past <c>Admit-Before</c> (default false).</summary>
    public bool NodeEnforcesAdmitBefore { get; init; }

    /// <summary>
    /// How long after the latest send stamp a NOT_FOUND is worth ACTING ON WITHOUT claiming proof: a same-key
    /// resend (the outbox's no-effect reopen) and when a receipt read is worth making. Never below round 4's
    /// lease-bounded window until the node enforces the deadline. "Nothing was taken" uses <see cref="AbsenceIsBelievable"/>.
    /// </summary>
    public TimeSpan ProofWindow => NodeEnforcesAdmitBefore
        ? QuietWindow
        : QuietWindow > LegacyQuietWindow ? QuietWindow : LegacyQuietWindow;

    /// <summary>The smallest spare second W must keep beyond W_admit + skew (TIMESTAMP(3) stamps vs an unrounded request-path clock).</summary>
    public static readonly TimeSpan MinimumMargin = TimeSpan.FromSeconds(1);

    /// <summary>The <c>Admit-Before</c> instant for a send whose durable start stamp is <paramref name="sendStamp"/>.</summary>
    /// <remarks>Rounded DOWN to whole seconds: an earlier deadline can only shrink the window a late admission fits in.</remarks>
    public DateTimeOffset AdmitBefore(DateTime sendStamp)
    {
        var deadline = DateTime.SpecifyKind(sendStamp, DateTimeKind.Utc).Add(AdmitWindow);
        return new DateTimeOffset(deadline.Ticks - (deadline.Ticks % TimeSpan.TicksPerSecond), TimeSpan.Zero);
    }

    /// <summary>
    /// PROOF that nothing was or can be taken: a NOT_FOUND from a read that STARTED at
    /// <paramref name="readStartedAt"/>, for a key whose latest send was stamped at <paramref name="latestSendStamp"/>.
    /// A key nothing ever sent is believed at once; otherwise only a node attested to refuse admission past
    /// <c>Admit-Before</c> makes a late NOT_FOUND permanent - without it a stalled send can still land.
    /// </summary>
    public bool AbsenceIsBelievable(DateTime? latestSendStamp, DateTime readStartedAt)
        => latestSendStamp is not { } stamp
            || (NodeEnforcesAdmitBefore && readStartedAt >= stamp.Add(QuietWindow));

    /// <summary>
    /// Timing only, never proof: whether <see cref="ProofWindow"/> has passed since the latest send. Gates a same-key
    /// resend (it cannot double-pay) and how soon a receipt read is worth making.
    /// </summary>
    public bool ProofWindowHasPassed(DateTime? latestSendStamp, DateTime now)
        => latestSendStamp is not { } stamp || now >= stamp.Add(ProofWindow);

    /// <summary>The instant a NOT_FOUND for a send stamped at <paramref name="latestSendStamp"/> becomes proof (enforcing node only).</summary>
    public DateTime BelievableAt(DateTime latestSendStamp) => latestSendStamp.Add(QuietWindow);

    /// <summary>The header value: RFC 3339, UTC, whole seconds.</summary>
    public static string FormatAdmitBefore(DateTimeOffset admitBefore)
        => admitBefore.UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture);

    /// <summary>
    /// The startup check: W must be at least W_admit + the clock-skew allowance + <see cref="MinimumMargin"/>, or a
    /// NOT_FOUND read at the end of the window could precede an admission the node still allows. Throws otherwise.
    /// </summary>
    public void Validate()
    {
        if (AdmitWindow <= TimeSpan.Zero || AdmitWindow > MaximumAdmitWindow)
            throw new InvalidOperationException(
                $"{SectionName}:AdmitWindowSeconds must be between 1 and {MaximumAdmitWindow.TotalSeconds:0}; got {AdmitWindow.TotalSeconds:0}.");
        if (MaximumClockSkew < TimeSpan.Zero)
            throw new InvalidOperationException($"{SectionName}:MaximumClockSkewSeconds may not be negative.");
        if (QuietWindow > MaximumQuietWindow)
            throw new InvalidOperationException(
                $"{SectionName}:QuietWindowSeconds may be at most {MaximumQuietWindow.TotalSeconds:0}; got {QuietWindow.TotalSeconds:0}.");
        if (QuietWindow < AdmitWindow + MaximumClockSkew + MinimumMargin)
        {
            throw new InvalidOperationException(
                $"{SectionName}:QuietWindowSeconds ({QuietWindow.TotalSeconds:0}) must be at least AdmitWindowSeconds "
                + $"({AdmitWindow.TotalSeconds:0}) + MaximumClockSkewSeconds ({MaximumClockSkew.TotalSeconds:0}) + "
                + $"{MinimumMargin.TotalSeconds:0}s: a node NOT_FOUND read at the end of the quiet window is proof only if "
                + "no send can still be admitted (Services/Implementations/AGENTS.md §Node proof).");
        }
    }
}
