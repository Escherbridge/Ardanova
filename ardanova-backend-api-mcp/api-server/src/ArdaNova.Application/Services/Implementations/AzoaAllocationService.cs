namespace ArdaNova.Application.Services.Implementations;

using ArdaNova.Application.Common.Interfaces;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Interfaces;
using ArdaNova.Domain.Models.Entities;

/// <summary>
/// The exactly-once MONEY PATH onto AZOA (track
/// <c>treasury-reward-to-azoa-allocation</c>; contract §6, §7).
///
/// Responsibilities (and, just as importantly, NON-responsibilities):
/// <list type="bullet">
///   <item>Compose a STABLE idempotency key from the caller-supplied economic-event
///   id — never random or timestamped. A redelivered call replays the same node
///   op instead of moving value twice (§6, §7).</item>
///   <item>Resolve the target avatar's <see cref="Guid"/> from the recipient
///   user's <c>azoaAvatarId</c> and pass it as the ROUTE value on the node call,
///   never a redirectable body field (IDOR-safe; §3). No avatar → clear failure
///   (the recipient must onboard first).</item>
///   <item>Pass <c>amount</c> through as an OPAQUE STRING — this service derives no
///   economics; the caller decides the amount (§1, §3).</item>
///   <item>Surface the node's <c>Replayed</c> flag so callers can reconcile
///   exactly-once delivery (§7).</item>
///   <item>Propagate a node <see cref="ResultType.Forbidden"/> (fail-closed
///   <c>KYC_FORBIDDEN</c>) VERBATIM — never swallow it (§6).</item>
/// </list>
///
/// Layering: lives in the Application layer (registered in
/// <c>ArdaNova.Application.DependencyInjection</c>) and talks to the node only
/// through the Application-owned <see cref="IAzoaAllocationNode"/> port — the same
/// dependency-inversion seam as <c>AzoaAvatarService</c>.
/// </summary>
public class AzoaAllocationService : IAzoaAllocationService
{
    private readonly IRepository<User> _userRepository;
    private readonly IAzoaAllocationNode _node;

    public AzoaAllocationService(
        IRepository<User> userRepository,
        IAzoaAllocationNode node)
    {
        _userRepository = userRepository;
        _node = node;
    }

    /// <inheritdoc/>
    public Task<Result<AzoaAllocationDto>> AllocateRewardAsync(
        string recipientUserId,
        string taskId,
        string amount,
        string assetName,
        string? escrowReleaseId = null,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(taskId))
            return Task.FromResult(
                Result<AzoaAllocationDto>.ValidationError("A taskId is required to build a stable reward idempotency key."));

        var idempotencyKey = AllocationKeys.Reward(taskId, escrowReleaseId);

        // Rewards MINT new value to the recipient.
        var command = new AzoaAllocationCommand(
            Kind: AzoaAllocationType.Mint,
            Amount: amount,
            Name: assetName,
            Description: $"ArdaNova task reward for {taskId}",
            Memo: idempotencyKey);

        return AllocateAsync(recipientUserId, command, idempotencyKey, ct);
    }

    /// <inheritdoc/>
    public Task<Result<AzoaAllocationDto>> AllocateFundingSettlementAsync(
        string recipientUserId,
        string paymentIntentId,
        string amount,
        string assetName,
        string? assetId = null,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(paymentIntentId))
            return Task.FromResult(
                Result<AzoaAllocationDto>.ValidationError(
                    "A payment-provider id (e.g. Stripe PaymentIntent id) is required as the funding-settlement idempotency key."));

        // The provider's own id IS the stable key — used verbatim so at-least-once
        // webhook delivery can never double-allocate (§6, §7).
        var idempotencyKey = AllocationKeys.FundingSettlement(paymentIntentId);

        // A funding settlement mints the settled value to the recipient.
        var command = new AzoaAllocationCommand(
            Kind: AzoaAllocationType.Mint,
            Amount: amount,
            Name: assetName,
            Description: $"ArdaNova funding settlement for {paymentIntentId}",
            AssetId: assetId,
            Memo: idempotencyKey);

        return AllocateAsync(recipientUserId, command, idempotencyKey, ct);
    }

    /// <inheritdoc/>
    public Task<Result<AzoaAllocationDto>> RefundAsync(
        string recipientUserId,
        string escrowId,
        string amount,
        string assetName,
        Guid assetRecordId,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(escrowId))
            return Task.FromResult(
                Result<AzoaAllocationDto>.ValidationError("An escrowId is required to build a stable refund idempotency key."));

        var idempotencyKey = AllocationKeys.Refund(escrowId);

        // A refund TRANSFERS the existing held asset record back to the recipient.
        var command = new AzoaAllocationCommand(
            Kind: AzoaAllocationType.Transfer,
            Amount: amount,
            Name: assetName,
            Description: $"ArdaNova escrow refund for {escrowId}",
            AssetRecordId: assetRecordId,
            Memo: idempotencyKey);

        return AllocateAsync(recipientUserId, command, idempotencyKey, ct);
    }

    // ── Core push ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Shared push: resolve the recipient's avatar, then hand the already-decided
    /// allocation + stable key to the node port. Defensive on a blank key so we
    /// never depend on the node's blank-key validation as a backstop.
    /// </summary>
    private async Task<Result<AzoaAllocationDto>> AllocateAsync(
        string recipientUserId,
        AzoaAllocationCommand command,
        string idempotencyKey,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
            return Result<AzoaAllocationDto>.ValidationError("A non-empty idempotency key is required for an allocation.");

        var user = await _userRepository.GetByIdAsync(recipientUserId, ct);
        if (user is null)
            return Result<AzoaAllocationDto>.NotFound($"Recipient user with id {recipientUserId} not found.");

        // avatarId is the ROUTE value (IDOR-safe), resolved from the recipient's
        // thin avatar reference — never a redirectable body field (§3).
        if (string.IsNullOrWhiteSpace(user.azoaAvatarId))
            return Result<AzoaAllocationDto>.Failure(
                "Recipient has no linked AZOA avatar. They must complete avatar onboarding before any value can be allocated to them.");

        if (!Guid.TryParse(user.azoaAvatarId, out var avatarId))
            return Result<AzoaAllocationDto>.Failure(
                $"Recipient's AZOA avatar id '{user.azoaAvatarId}' is not a valid avatar identifier.");

        // Recipient-bind the key so a cross-user collision is STRUCTURALLY
        // impossible, not merely unlikely (security review M1). ArdaNova uses a
        // single tenant API key for all users, so the node partitions idempotency
        // by (apiKeyId, key) — without the avatar prefix, two recipients sharing an
        // event id (e.g. a taskId only unique within a project) would collide and
        // the second recipient would silently receive nothing. Prefixing with the
        // resolved avatarId costs nothing for a true single-recipient event.
        var scopedKey = $"{avatarId}:{idempotencyKey}";

        var allocation = await _node.AllocateAsync(avatarId, command, scopedKey, ct);
        if (allocation.IsFailure)
            // Propagate the node's failure type verbatim — including the fail-closed
            // KYC_FORBIDDEN → Forbidden. Never swallowed (§6).
            return MapFailure<AzoaAllocationOutcome, AzoaAllocationDto>(allocation);

        var outcome = allocation.Value!;
        return Result<AzoaAllocationDto>.Success(new AzoaAllocationDto
        {
            OperationId = outcome.OperationId,
            WalletAddress = outcome.WalletAddress,
            WalletId = outcome.WalletId,
            WalletProvisioned = outcome.WalletProvisioned,
            // Surface exactly-once replay so callers reconcile rather than
            // double-count (§7). A replay is still a success.
            Replayed = outcome.Replayed,
            IdempotencyKey = outcome.IdempotencyKey,
        });
    }

    // ── Stable idempotency-key builder (§6, §7) ──────────────────────────────────

    /// <summary>
    /// Composes the STABLE, deterministic idempotency keys the contract mandates.
    /// Keys are derived purely from the economic event — never random, never
    /// timestamped — so a redelivered call replays the same node op instead of
    /// moving value twice (§6, §7). The caller supplies the stable event id; this
    /// builder owns the prefix convention.
    /// </summary>
    internal static class AllocationKeys
    {
        public const string RewardPrefix = "reward";
        public const string RefundPrefix = "refund";

        /// <summary><c>reward:{taskId}</c>, or <c>reward:{taskId}:{escrowReleaseId}</c> for multi-pay tasks.</summary>
        public static string Reward(string taskId, string? escrowReleaseId = null) =>
            string.IsNullOrWhiteSpace(escrowReleaseId)
                ? $"{RewardPrefix}:{taskId}"
                : $"{RewardPrefix}:{taskId}:{escrowReleaseId}";

        /// <summary>The payment-provider id IS the key (e.g. Stripe PaymentIntent id), used verbatim.</summary>
        public static string FundingSettlement(string paymentIntentId) => paymentIntentId;

        /// <summary><c>refund:{escrowId}</c>.</summary>
        public static string Refund(string escrowId) => $"{RefundPrefix}:{escrowId}";
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Re-projects a failed <c>Result&lt;TIn&gt;</c> onto <c>Result&lt;TOut&gt;</c>
    /// preserving the original <see cref="ResultType"/> — so the node's fail-closed
    /// KYC (<c>KYC_FORBIDDEN</c> → Forbidden) and other semantics survive.
    /// </summary>
    private static Result<TOut> MapFailure<TIn, TOut>(Result<TIn> source)
    {
        var error = source.Error ?? "AZOA allocation failed.";
        return source.Type switch
        {
            ResultType.NotFound => Result<TOut>.NotFound(error),
            ResultType.Forbidden => Result<TOut>.Forbidden(error),
            ResultType.Unauthorized => Result<TOut>.Unauthorized(error),
            ResultType.Conflict => Result<TOut>.Conflict(error),
            ResultType.ValidationError => Result<TOut>.ValidationError(error),
            ResultType.BadRequest => Result<TOut>.BadRequest(error),
            _ => Result<TOut>.Failure(error),
        };
    }
}
