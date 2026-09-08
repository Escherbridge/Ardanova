namespace ArdaNova.Application.Services.Implementations;

using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Interfaces;

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
/// <b>Only the refund path remains.</b> A refund TRANSFERS an asset record that
/// already exists. The two allocation paths that MINTED new value — the task reward
/// and the fiat funding settlement — are GONE from this type and from
/// <see cref="IAzoaAllocationService"/>: ArdaNova no longer creates units out of
/// nothing on a per-allocation basis. Sponsorship value now moves as an AZOA exchange
/// operation through <c>IAzoaSettlementGateway</c>, drawn from the platform reserve,
/// and rewards move out of a project's own reserve partition. The
/// <see cref="AzoaAllocationType.Mint"/> kind is kept ONLY so the guard in
/// <c>AllocateAsync</c> stays a real refusal rather than a vacuous one: a future
/// caller that reaches the shared push with a mint command is turned away there, not
/// merely at an entry point that no longer exists.
///
/// Layering: lives in the Application layer (registered in
/// <c>ArdaNova.Application.DependencyInjection</c>) and talks to the node only
/// through the Application-owned <see cref="IAzoaAllocationNode"/> port — the same
/// dependency-inversion seam as <c>IAlgorandService</c>.
/// </summary>
public class AzoaAllocationService : IAzoaAllocationService
{
    /// <summary>Refusal text for the retired mint kind, naming what replaced it.</summary>
    private const string RetiredMintPathMessage =
        "This allocation path is retired: ArdaNova no longer mints value per allocation. Settle the "
        + "economic decision through the AZOA settlement gateway, which draws the units from a reserve "
        + "partition as one exchange operation.";

    private readonly IAzoaValueEligibilityService _valueEligibility;
    private readonly IAzoaAllocationNode _node;

    public AzoaAllocationService(
        IAzoaValueEligibilityService valueEligibility,
        IAzoaAllocationNode node)
    {
        _valueEligibility = valueEligibility;
        _node = node;
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
    /// Shared push: confirm recipient value readiness, then pass the decided
    /// allocation and stable key to the node port.
    /// </summary>
    private async Task<Result<AzoaAllocationDto>> AllocateAsync(
        string recipientUserId,
        AzoaAllocationCommand command,
        string idempotencyKey,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
            return Result<AzoaAllocationDto>.ValidationError("A non-empty idempotency key is required for an allocation.");

        // Defence in depth for the retirement above: even a future caller that reaches this
        // method cannot mint, because minting is refused here rather than only at the entry points.
        if (command.Kind != AzoaAllocationType.Transfer)
            return Result<AzoaAllocationDto>.Failure(RetiredMintPathMessage);

        var readiness = await _valueEligibility.RequireReadyAsync(
            recipientUserId,
            "receive a value allocation",
            ct);
        if (readiness.IsFailure)
            return MapFailure<AzoaCustodialAccountStatusDto, AzoaAllocationDto>(readiness);

        // avatarId is the ROUTE value (IDOR-safe), resolved from the recipient's
        // thin avatar reference — never a redirectable body field (§3).
        var avatarReference = readiness.Value!.AvatarId;
        if (string.IsNullOrWhiteSpace(avatarReference))
            return Result<AzoaAllocationDto>.Failure(
                "Recipient account setup did not return an avatar reference for this allocation.");

        if (!Guid.TryParse(avatarReference, out var avatarId))
            return Result<AzoaAllocationDto>.Failure(
                "Recipient account setup returned an invalid avatar reference for this allocation.");

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
    /// builder owns the prefix convention. The reward and funding-settlement key
    /// builders were removed with the mint paths that used them.
    /// </summary>
    internal static class AllocationKeys
    {
        public const string RefundPrefix = "refund";

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
