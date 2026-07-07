namespace ArdaNova.Application.DTOs;

/// <summary>
/// Snapshot of a user's AZOA avatar/wallet linkage (track
/// <c>azoa-avatar-onboarding</c>). Carries only the thin reference ArdaNova is
/// allowed to hold — no keys, mnemonics or balances (those live on the node).
/// </summary>
public record AzoaAvatarStatusDto
{
    /// <summary>The user's AZOA avatar id, or null when not yet linked.</summary>
    public string? AvatarId { get; init; }

    /// <summary>Cached wallet address once a wallet is bound; null until then.</summary>
    public string? WalletAddress { get; init; }

    /// <summary>Cached wallet id once a wallet is bound; null until then.</summary>
    public string? WalletId { get; init; }

    /// <summary>True when an avatar is linked.</summary>
    public bool AvatarLinked { get; init; }

    /// <summary>
    /// True when a wallet reference is cached for the avatar. Wallet binding is
    /// completed lazily by the first allocation; chain stays source of truth.
    /// </summary>
    public bool WalletBound { get; init; }

    /// <summary>
    /// Consumer-side Tier-2 readiness (avatar present). Does NOT reflect KYC —
    /// the KYC value-gate is node-side and surfaces as a 403 at value-move time.
    /// </summary>
    public bool Tier2Ready { get; init; }
}

/// <summary>
/// Result of the consumer-side Tier-2 readiness check. When not ready,
/// <see cref="Reason"/> explains why (e.g. no avatar linked) so the client can
/// show an actionable message rather than a silent no-op.
/// </summary>
public record AzoaTier2ReadinessDto
{
    public bool Ready { get; init; }

    /// <summary>Human-readable reason when <see cref="Ready"/> is false; null when ready.</summary>
    public string? Reason { get; init; }

    public string? AvatarId { get; init; }
    public bool WalletBound { get; init; }
}
