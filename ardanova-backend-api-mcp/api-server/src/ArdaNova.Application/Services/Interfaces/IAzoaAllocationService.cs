namespace ArdaNova.Application.Services.Interfaces;

using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;

/// <summary>
/// The exactly-once MONEY PATH onto AZOA (track
/// <c>treasury-reward-to-azoa-allocation</c>; contract §6, §7).
///
/// Every value move (task/bounty reward, funding settlement, refund) is pushed
/// to the recipient's avatar through the node's idempotent allocation door. The
/// service NEVER computes economics — the caller (escrow/funding services)
/// supplies an already-decided opaque <c>amount</c> string and the stable
/// economic-event id; this service only composes a STABLE idempotency key and
/// resolves the target avatar from the recipient's <c>azoaAvatarId</c> (§1, §3).
///
/// Exactly-once guarantees:
/// <list type="bullet">
///   <item>Idempotency keys are derived from the economic event (never random or
///   timestamped), so a redelivered call replays the same node operation rather
///   than moving value twice (§6, §7).</item>
///   <item>The node's <c>Replayed</c> flag is surfaced in the outcome DTO so
///   callers see exactly-once replay and can reconcile (§7).</item>
/// </list>
///
/// Layering: lives in the Application layer and talks to the node ONLY through
/// the Application-owned <see cref="IAzoaAllocationNode"/> port — never the
/// Infrastructure transport directly (the same dependency-inversion seam used by
/// <c>IAzoaAvatarNode</c> / <c>IAlgorandService</c>).
///
/// KYC is fail-closed and node-side: a node <see cref="ResultType.Forbidden"/>
/// (<c>KYC_FORBIDDEN</c>) is propagated verbatim, never swallowed (§6).
/// </summary>
public interface IAzoaAllocationService
{
    /// <summary>
    /// Push a task/bounty reward to the recipient's avatar. Idempotency key is
    /// <c>reward:{taskId}</c> (or <c>reward:{taskId}:{escrowReleaseId}</c> when the
    /// task pays out in multiple tranches). The reward MINTS new value to the
    /// recipient.
    /// </summary>
    /// <param name="recipientUserId">ArdaNova user receiving the reward.</param>
    /// <param name="taskId">The economic-event id (task/bounty) — drives the key.</param>
    /// <param name="amount">Already-decided opaque amount string. Passed through verbatim.</param>
    /// <param name="assetName">Display name for the allocated asset.</param>
    /// <param name="escrowReleaseId">Optional tranche id for multi-pay tasks; appended to the key.</param>
    Task<Result<AzoaAllocationDto>> AllocateRewardAsync(
        string recipientUserId,
        string taskId,
        string amount,
        string assetName,
        string? escrowReleaseId = null,
        CancellationToken ct = default);

    /// <summary>
    /// Settle a completed off-chain payment (e.g. Stripe) onto the recipient's
    /// avatar. The idempotency key is the payment-provider id (e.g. the Stripe
    /// <c>PaymentIntent</c> id) supplied by the caller — used VERBATIM so the
    /// provider's own at-least-once webhook delivery can never double-allocate.
    /// </summary>
    /// <param name="recipientUserId">ArdaNova user receiving the settlement.</param>
    /// <param name="paymentIntentId">Payment-provider id — used verbatim as the idempotency key.</param>
    /// <param name="amount">Already-decided opaque amount string. Passed through verbatim.</param>
    /// <param name="assetName">Display name for the allocated asset.</param>
    /// <param name="assetId">Optional existing asset id to settle against.</param>
    Task<Result<AzoaAllocationDto>> AllocateFundingSettlementAsync(
        string recipientUserId,
        string paymentIntentId,
        string amount,
        string assetName,
        string? assetId = null,
        CancellationToken ct = default);

    /// <summary>
    /// Refund a held escrow back to the recipient's avatar. The idempotency key is
    /// <c>refund:{escrowId}</c>. A refund TRANSFERS an existing asset record
    /// (<paramref name="assetRecordId"/>) rather than minting.
    /// </summary>
    /// <param name="recipientUserId">ArdaNova user receiving the refund.</param>
    /// <param name="escrowId">The escrow being refunded — drives the key.</param>
    /// <param name="amount">Already-decided opaque amount string. Passed through verbatim.</param>
    /// <param name="assetName">Display name for the refunded asset.</param>
    /// <param name="assetRecordId">The existing asset record to move back (Transfer).</param>
    Task<Result<AzoaAllocationDto>> RefundAsync(
        string recipientUserId,
        string escrowId,
        string amount,
        string assetName,
        Guid assetRecordId,
        CancellationToken ct = default);
}

/// <summary>
/// Application-layer PORT onto the AZOA node's allocation door, used by
/// <c>AzoaAllocationService</c>.
///
/// Why this exists: the typed-HttpClient transport (<c>IAzoaNodeClient</c>) lives
/// in <c>ArdaNova.Infrastructure.Azoa</c>, and the Application layer must not (and
/// at compile time cannot) reference Infrastructure. This port is the seam — the
/// same dependency-inversion pattern as <c>IAzoaAvatarNode</c>. An Infrastructure
/// adapter wraps <c>IAzoaNodeClient.AllocateAsync</c> and registers itself against
/// this port. Wire models stay in Infrastructure — this port speaks only in
/// Application-owned records so no Infrastructure type leaks into the service.
/// </summary>
public interface IAzoaAllocationNode
{
    /// <summary>
    /// Push an already-decided allocation to the target avatar (route-only,
    /// IDOR-safe). Idempotent on <paramref name="idempotencyKey"/> — a redelivered
    /// call with the same key replays the existing op and returns
    /// <see cref="AzoaAllocationOutcome.Replayed"/> = true with no second value
    /// move (§6, §7). Node failures (including the fail-closed
    /// <c>KYC_FORBIDDEN</c> → <see cref="ResultType.Forbidden"/>) surface verbatim
    /// via the result type.
    /// </summary>
    Task<Result<AzoaAllocationOutcome>> AllocateAsync(
        Guid avatarId,
        AzoaAllocationCommand command,
        string idempotencyKey,
        CancellationToken ct = default);
}

/// <summary>Whether the allocation mints new value or transfers an existing asset record.</summary>
public enum AzoaAllocationType
{
    Mint = 0,
    Transfer = 1,
}

/// <summary>
/// Application-owned allocation input for the node port. <c>Amount</c> is an opaque
/// string passed straight through — the Application layer derives no economics
/// (§1, §3). <c>ChainType</c> is optional; the Infrastructure adapter falls back to
/// the configured node default when null.
/// </summary>
public sealed record AzoaAllocationCommand(
    AzoaAllocationType Kind,
    string Amount,
    string Name,
    string? Description = null,
    string? AssetId = null,
    Guid? AssetRecordId = null,
    string? Memo = null,
    string? ChainType = null,
    IReadOnlyDictionary<string, string>? Metadata = null);

/// <summary>
/// Application-owned allocation result returned by the node port. <c>Replayed</c>
/// is the exactly-once reconciliation signal — true when the idempotency key
/// matched an existing op (§6, §7).
/// </summary>
public sealed record AzoaAllocationOutcome(
    Guid AvatarId,
    Guid WalletId,
    string WalletAddress,
    bool WalletProvisioned,
    Guid OperationId,
    bool Replayed,
    string IdempotencyKey);
