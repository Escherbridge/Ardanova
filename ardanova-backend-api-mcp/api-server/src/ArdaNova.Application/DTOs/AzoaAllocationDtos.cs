namespace ArdaNova.Application.DTOs;

/// <summary>
/// Outcome of a value move pushed through the AZOA allocation door (track
/// <c>treasury-reward-to-azoa-allocation</c>; contract §6, §7).
///
/// Carries only the thin references ArdaNova is allowed to hold plus the
/// exactly-once reconciliation signal. No keys, mnemonics or balances — those
/// live on the node, which remains the source of truth.
/// </summary>
public record AzoaAllocationDto
{
    /// <summary>The node operation id for this value move.</summary>
    public Guid OperationId { get; init; }

    /// <summary>The recipient avatar's wallet address (auto-provisioned on first allocation).</summary>
    public string WalletAddress { get; init; } = string.Empty;

    /// <summary>The recipient avatar's wallet id.</summary>
    public Guid WalletId { get; init; }

    /// <summary>True when this allocation auto-provisioned the avatar's wallet.</summary>
    public bool WalletProvisioned { get; init; }

    /// <summary>
    /// EXACTLY-ONCE signal: true when the idempotency key replayed an existing
    /// operation, meaning NO second value move occurred. Callers must surface /
    /// reconcile on this rather than treating a replay as a fresh allocation
    /// (§6, §7).
    /// </summary>
    public bool Replayed { get; init; }

    /// <summary>The stable idempotency key the value move was keyed on.</summary>
    public string IdempotencyKey { get; init; } = string.Empty;
}
