namespace ArdaNova.Application.Services.Implementations;

using System.Security.Cryptography;
using ArdaNova.Application.Common.Interfaces;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Interfaces;
using ArdaNova.Domain.Models.Entities;

/// <summary>
/// Links an ArdaNova <c>User</c> to a self-sovereign AZOA avatar and exposes the
/// wallet-bound Tier-2 readiness check (track <c>azoa-avatar-onboarding</c>;
/// contract §2–§5.1, §11).
///
/// Layering: this service lives in the Application layer (registered in
/// <c>ArdaNova.Application.DependencyInjection</c>) and talks to the node through
/// the Application-owned <see cref="IAzoaAvatarNode"/> port, NOT the
/// Infrastructure transport directly — the same dependency-inversion seam used by
/// <c>IAlgorandService</c>.
///
/// Persistence is a THIN reference only: <c>azoaAvatarId</c> (and, once an
/// allocation supplies them, <c>azoaWalletId</c>/<c>azoaWalletAddress</c>). Keys,
/// mnemonics and balances are never stored here (§2, acceptance criteria).
/// </summary>
public class AzoaAvatarService : IAzoaAvatarService
{
    private readonly IRepository<User> _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAzoaAvatarNode _node;

    public AzoaAvatarService(
        IRepository<User> userRepository,
        IUnitOfWork unitOfWork,
        IAzoaAvatarNode node)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _node = node;
    }

    /// <inheritdoc/>
    public async Task<Result<AzoaAvatarStatusDto>> EnsureAvatarAsync(string userId, CancellationToken ct = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, ct);
        if (user is null)
            return Result<AzoaAvatarStatusDto>.NotFound($"User with id {userId} not found");

        // Idempotent: already linked → return existing reference, no second register.
        if (!string.IsNullOrWhiteSpace(user.azoaAvatarId))
            return Result<AzoaAvatarStatusDto>.Success(ToStatus(user));

        // Self-register the user as its own avatar (no tenant:provision, no
        // acting-as; §11.2). The password is a strong random secret the NODE
        // custodies — ArdaNova never persists or sees it again (§2).
        var registration = new AzoaAvatarRegistration(
            Username: DeriveUsername(user),
            Email: user.email,
            Password: GenerateAvatarSecret(),
            Title: null,
            FirstName: null,
            LastName: user.name);

        var registerResult = await _node.RegisterAvatarAsync(registration, ct);
        if (registerResult.IsFailure)
            // Propagate the node's failure type verbatim (Forbidden for
            // KYC_FORBIDDEN, etc.). No persistence on failure.
            return MapFailure<AzoaAvatarRef, AzoaAvatarStatusDto>(registerResult);

        var avatar = registerResult.Value!;
        if (string.IsNullOrWhiteSpace(avatar.AvatarId))
            return Result<AzoaAvatarStatusDto>.Failure("AZOA avatar registration returned no avatar id.");

        user.azoaAvatarId = avatar.AvatarId;
        user.updatedAt = DateTime.UtcNow;
        await _userRepository.UpdateAsync(user, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return Result<AzoaAvatarStatusDto>.Success(ToStatus(user));
    }

    /// <inheritdoc/>
    public async Task<Result<AzoaAvatarStatusDto>> EnsureWalletAsync(string userId, CancellationToken ct = default)
    {
        // Wallet binding is completed LAZILY by the allocation path: the first
        // tenant-pushed allocation auto-provisions the avatar's wallet and returns
        // its id/address (treasury-reward-to-azoa-allocation track). We do NOT
        // call /wallet/generate with the tenant key — that endpoint is
        // caller-self-scoped and would target the tenant's own avatar, not the
        // user's (§11.4 note). So here we guarantee the avatar exists and surface
        // whatever wallet reference is already cached; the wallet fields are
        // populated later by an allocation result.
        var ensureAvatar = await EnsureAvatarAsync(userId, ct);
        if (ensureAvatar.IsFailure)
            return ensureAvatar;

        return ensureAvatar;
    }

    /// <inheritdoc/>
    public async Task<Result<AzoaTier2ReadinessDto>> IsTier2ReadyAsync(string userId, CancellationToken ct = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, ct);
        if (user is null)
            return Result<AzoaTier2ReadinessDto>.NotFound($"User with id {userId} not found");

        var hasAvatar = !string.IsNullOrWhiteSpace(user.azoaAvatarId);
        var walletBound = !string.IsNullOrWhiteSpace(user.azoaWalletId)
            || !string.IsNullOrWhiteSpace(user.azoaWalletAddress);

        if (!hasAvatar)
        {
            return Result<AzoaTier2ReadinessDto>.Success(new AzoaTier2ReadinessDto
            {
                Ready = false,
                Reason = "No AZOA avatar linked. Run avatar onboarding before any value-bearing action.",
                AvatarId = null,
                WalletBound = false,
            });
        }

        // Avatar present → Tier-2 ready on the consumer side. A wallet is
        // auto-provisioned by the node on the first allocation, so we do NOT
        // block on walletBound here. KYC remains the node-side gate (403 at
        // value-move time), not enforced here.
        return Result<AzoaTier2ReadinessDto>.Success(new AzoaTier2ReadinessDto
        {
            Ready = true,
            Reason = null,
            AvatarId = user.azoaAvatarId,
            WalletBound = walletBound,
        });
    }

    /// <inheritdoc/>
    public async Task<Result<AzoaAvatarStatusDto>> GetStatusAsync(string userId, CancellationToken ct = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, ct);
        if (user is null)
            return Result<AzoaAvatarStatusDto>.NotFound($"User with id {userId} not found");

        return Result<AzoaAvatarStatusDto>.Success(ToStatus(user));
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static AzoaAvatarStatusDto ToStatus(User user)
    {
        var avatarLinked = !string.IsNullOrWhiteSpace(user.azoaAvatarId);
        var walletBound = !string.IsNullOrWhiteSpace(user.azoaWalletId)
            || !string.IsNullOrWhiteSpace(user.azoaWalletAddress);

        return new AzoaAvatarStatusDto
        {
            AvatarId = user.azoaAvatarId,
            WalletId = user.azoaWalletId,
            WalletAddress = user.azoaWalletAddress,
            AvatarLinked = avatarLinked,
            WalletBound = walletBound,
            // Consumer-side Tier-2 readiness mirrors avatar presence (wallet is
            // auto-provisioned on first allocation); KYC is the node-side gate.
            Tier2Ready = avatarLinked,
        };
    }

    /// <summary>
    /// Derive a node username from the ArdaNova identity. The local-part of the
    /// email is stable and unique enough for the node's username slot; the email
    /// is sent separately. Self-sovereign avatars are anonymous (§4), so no PII
    /// beyond what the user already provided is introduced.
    /// </summary>
    private static string DeriveUsername(User user)
    {
        var local = user.email.Split('@', 2)[0];
        return string.IsNullOrWhiteSpace(local) ? $"user-{user.id}" : local;
    }

    /// <summary>
    /// Generate a strong random secret for the node-custodied avatar. ArdaNova
    /// never persists this — the node owns the credential after registration (§2).
    /// </summary>
    private static string GenerateAvatarSecret()
    {
        // 32 bytes of CSPRNG entropy, URL-safe base64. Comfortably exceeds any
        // node password-strength policy and is never stored on this side.
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }

    /// <summary>
    /// Re-projects a failed <c>Result&lt;TIn&gt;</c> onto <c>Result&lt;TOut&gt;</c>
    /// preserving the original <see cref="ResultType"/> — so the node's fail-closed
    /// KYC (<c>KYC_FORBIDDEN</c> → Forbidden) and other semantics survive.
    /// </summary>
    private static Result<TOut> MapFailure<TIn, TOut>(Result<TIn> source)
    {
        var error = source.Error ?? "AZOA node call failed.";
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
