namespace ArdaNova.Application.Services.Interfaces;

using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;

/// <summary>
/// Links an ArdaNova <c>User</c> to a self-sovereign AZOA avatar on the
/// shared/managed node, and exposes the wallet-bound readiness check that every
/// value-bearing (Tier-2) operation depends on
/// (track <c>azoa-avatar-onboarding</c>; contract §2, §3, §4, §5.1, §11).
///
/// ArdaNova stores only a thin reference (<c>azoaAvatarId</c> and, once known, the
/// cached <c>azoaWalletId</c>/<c>azoaWalletAddress</c>). Keys, mnemonics and
/// balances live on the node only — never persisted here.
///
/// The KYC value-gate is enforced NODE-SIDE, fail-closed (<c>KYC_FORBIDDEN</c> →
/// 403). This service does NOT enforce it; it only surfaces it. Accordingly
/// <see cref="IsTier2ReadyAsync"/> is a wallet/avatar readiness check, not a KYC
/// check (§6, §11.4).
/// </summary>
public interface IAzoaAvatarService
{
    /// <summary>
    /// Idempotently create or resolve the calling user's AZOA avatar and persist
    /// its id as <c>User.azoaAvatarId</c>. If the user is already linked, returns
    /// the existing avatar reference without registering a second avatar.
    /// </summary>
    Task<Result<AzoaAvatarStatusDto>> EnsureAvatarAsync(string userId, CancellationToken ct = default);

    /// <summary>
    /// Ensure a wallet reference is cached for the user's avatar.
    ///
    /// Wallet binding for an avatar is completed lazily by the ALLOCATION path —
    /// the first tenant-pushed allocation auto-provisions the wallet and returns
    /// its id/address (see <c>treasury-reward-to-azoa-allocation</c>). We do NOT
    /// call the caller-self-scoped <c>/wallet/generate</c> with the tenant key
    /// (that would target the tenant's own avatar, not the user's). This method
    /// therefore ensures the avatar exists and surfaces any already-cached wallet
    /// reference; it is a no-op on the wallet fields until an allocation result
    /// supplies them.
    /// </summary>
    Task<Result<AzoaAvatarStatusDto>> EnsureWalletAsync(string userId, CancellationToken ct = default);

    /// <summary>
    /// Consumer-side readiness mirror of AZOA's <c>ChainCapabilityGate</c>
    /// (fail-closed). Returns <c>false</c> with a clear reason when the user has
    /// no linked avatar yet, <c>true</c> once an avatar is present (a wallet is
    /// auto-provisioned on first allocation). Does NOT check KYC — that gate is
    /// node-side and surfaces as a 403 at value-move time.
    /// </summary>
    Task<Result<AzoaTier2ReadinessDto>> IsTier2ReadyAsync(string userId, CancellationToken ct = default);

    /// <summary>
    /// Return the current avatar/wallet linkage status for the user without
    /// mutating anything.
    /// </summary>
    Task<Result<AzoaAvatarStatusDto>> GetStatusAsync(string userId, CancellationToken ct = default);
}

/// <summary>
/// Application-layer PORT onto the AZOA node, used by <c>AzoaAvatarService</c>.
///
/// Why this exists: the typed-HttpClient transport (<c>IAzoaNodeClient</c>) lives
/// in <c>ArdaNova.Infrastructure.Azoa</c>, and the Application layer must not (and
/// at compile time cannot) reference Infrastructure. This port is the seam — the
/// same dependency-inversion pattern already used by <c>IAlgorandService</c>
/// (interface in Application, implementation in Infrastructure). An Infrastructure
/// adapter wraps <c>IAzoaNodeClient.RegisterAvatarAsync</c> and registers itself
/// against this port.
///
/// Only the avatar-onboarding surface is modelled here; allocation/quest seams
/// have their own ports. Wire models stay in Infrastructure — this port speaks in
/// Application-owned records so no Infrastructure type leaks into the service.
/// </summary>
public interface IAzoaAvatarNode
{
    /// <summary>
    /// Self-register an end user as a self-sovereign avatar (anonymous; §4, §11.2).
    /// Returns the node's avatar id on success. Node failures (including the
    /// fail-closed <c>KYC_FORBIDDEN</c> → <see cref="ResultType.Forbidden"/>) are
    /// surfaced verbatim via the result type.
    /// </summary>
    Task<Result<AzoaAvatarRef>> RegisterAvatarAsync(AzoaAvatarRegistration registration, CancellationToken ct = default);
}

/// <summary>Application-owned registration input for the avatar onboarding port.</summary>
public sealed record AzoaAvatarRegistration(
    string Username,
    string Email,
    string Password,
    string? Title = null,
    string? FirstName = null,
    string? LastName = null);

/// <summary>Application-owned avatar reference returned by the node port.</summary>
public sealed record AzoaAvatarRef(string AvatarId, string? Username = null, string? Email = null);
