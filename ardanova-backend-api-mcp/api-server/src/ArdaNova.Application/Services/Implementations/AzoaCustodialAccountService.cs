namespace ArdaNova.Application.Services.Implementations;

using System.Security.Cryptography;
using System.Text;
using ArdaNova.Application.Common.Interfaces;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Interfaces;
using ArdaNova.Domain.Models.Entities;
using ArdaNova.Domain.Models.Enums;
using Microsoft.Extensions.Configuration;

/// <summary>Orchestrates one tenant-bound custodial AZOA account per ArdaNova user.</summary>
public sealed class AzoaCustodialAccountService : IAzoaCustodialAccountService
{
    private readonly IRepository<User> _users;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAzoaCustodialAccountGateway _gateway;
    private readonly string _tenantId;

    public AzoaCustodialAccountService(
        IRepository<User> users,
        IUnitOfWork unitOfWork,
        IAzoaCustodialAccountGateway gateway,
        IConfiguration configuration)
    {
        _users = users;
        _unitOfWork = unitOfWork;
        _gateway = gateway;
        _tenantId = configuration["Azoa:TenantId"] ?? string.Empty;
    }

    public async Task<Result<AzoaCustodialAccountCapabilitiesDto>> GetCapabilitiesAsync(
        CancellationToken ct = default)
    {
        var result = await _gateway.GetCapabilitiesAsync(ct);
        if (result.IsFailure)
            return MapFailure<AzoaCustodialAccountCapabilities, AzoaCustodialAccountCapabilitiesDto>(result);

        var value = result.Value!;
        if ((value.KycReady && string.IsNullOrWhiteSpace(value.KycProvider))
            || (value.WalletProvisioningReady
                && (!value.CustodyAvailable || !value.BlockchainProviderAvailable))
            || (value.DevelopmentSimulation
                && (!value.KycAvailable || !value.KycReady || value.HostedVerification))
            || (value.Ready
                && (!value.Enabled
                    || !value.IdentityReady
                    || !value.KycReady
                    || !value.WalletProvisioningReady)))
        {
            return Result<AzoaCustodialAccountCapabilitiesDto>.Conflict(
                "AZOA returned inconsistent tenant capability readiness.");
        }

        return Result<AzoaCustodialAccountCapabilitiesDto>.Success(new AzoaCustodialAccountCapabilitiesDto
        {
            Enabled = value.Enabled,
            WalletChain = value.WalletChain,
            CustodyMode = value.CustodyMode,
            CustodyAvailable = value.CustodyAvailable,
            BlockchainProviderAvailable = value.BlockchainProviderAvailable,
            KycProvider = value.KycProvider,
            KycAvailable = value.KycAvailable,
            HostedVerification = value.HostedVerification,
            AcceptsDocumentReferences = value.AcceptsDocumentReferences,
            DevelopmentSimulation = value.DevelopmentSimulation,
            IdentityReady = value.IdentityReady,
            KycReady = value.KycReady,
            WalletProvisioningReady = value.WalletProvisioningReady,
            Ready = value.Ready,
            UnavailableReason = value.UnavailableReason,
        });
    }

    public async Task<Result<AzoaCustodialAccountStatusDto>> EnsureAsync(
        string userId,
        CancellationToken ct = default)
    {
        var userResult = await GetUserAndBindingAsync(userId, ct);
        if (userResult.IsFailure)
            return MapFailure<UserBinding, AzoaCustodialAccountStatusDto>(userResult);

        var context = userResult.Value!;
        var result = await _gateway.EnsureAsync(context.Binding, ct);
        if (result.IsFailure)
            return MapFailure<AzoaCustodialAccountStatus, AzoaCustodialAccountStatusDto>(result);

        return await ValidatePersistAndMapAsync(context.User, context.Binding, result.Value!, ct);
    }

    public async Task<Result<AzoaCustodialAccountStatusDto>> GetStatusAsync(
        string userId,
        CancellationToken ct = default)
    {
        var userResult = await GetUserAndBindingAsync(userId, ct);
        if (userResult.IsFailure)
            return MapFailure<UserBinding, AzoaCustodialAccountStatusDto>(userResult);

        var context = userResult.Value!;
        var result = await _gateway.GetStatusAsync(context.Binding, ct);
        if (result.IsFailure)
            return MapFailure<AzoaCustodialAccountStatus, AzoaCustodialAccountStatusDto>(result);

        return await ValidatePersistAndMapAsync(context.User, context.Binding, result.Value!, ct);
    }

    public async Task<Result<AzoaKycSessionDto>> BeginKycAsync(
        string userId,
        CancellationToken ct = default)
    {
        var userResult = await GetUserAndBindingAsync(userId, ct);
        if (userResult.IsFailure)
            return MapFailure<UserBinding, AzoaKycSessionDto>(userResult);

        var result = await _gateway.BeginKycAsync(userResult.Value!.Binding, ct);
        if (result.IsFailure)
            return MapFailure<AzoaKycSession, AzoaKycSessionDto>(result);

        var value = result.Value!;
        if (string.IsNullOrWhiteSpace(value.Provider)
            || value.Provider.Length > 100
            || value.Instructions?.Length > 2000)
        {
            return Result<AzoaKycSessionDto>.Conflict(
                "AZOA returned invalid KYC session metadata.");
        }

        if (value.HostedVerification
            && (string.IsNullOrWhiteSpace(value.VerificationUrl)
                || !IsSafeHostedVerificationUrl(value.VerificationUrl)
                || value.ExpiresAt is null
                || value.ExpiresAt <= DateTime.UtcNow))
        {
            return Result<AzoaKycSessionDto>.Conflict(
                "AZOA returned an invalid hosted verification destination.");
        }

        if (value.DevelopmentSimulation
            && (value.HostedVerification || !string.IsNullOrWhiteSpace(value.VerificationUrl)))
        {
            return Result<AzoaKycSessionDto>.Conflict(
                "AZOA returned inconsistent development simulation metadata.");
        }

        if (!value.HostedVerification && !value.DevelopmentSimulation)
        {
            return Result<AzoaKycSessionDto>.Conflict(
                "AZOA returned a KYC session without an ArdaNova-supported verification method.");
        }

        return Result<AzoaKycSessionDto>.Success(new AzoaKycSessionDto
        {
            Provider = value.Provider,
            HostedVerification = value.HostedVerification,
            AcceptsDocumentReferences = value.AcceptsDocumentReferences,
            DevelopmentSimulation = value.DevelopmentSimulation,
            VerificationUrl = value.VerificationUrl,
            ExpiresAt = value.ExpiresAt,
            Instructions = value.Instructions,
        });
    }

    private async Task<Result<UserBinding>> GetUserAndBindingAsync(string userId, CancellationToken ct)
    {
        if (!Guid.TryParse(_tenantId, out var tenantId))
            return Result<UserBinding>.Conflict("AZOA tenant-bound custodial account onboarding is not configured.");

        var user = await _users.GetByIdAsync(userId, ct);
        if (user is null)
            return Result<UserBinding>.NotFound("User not found.");

        return Result<UserBinding>.Success(new UserBinding(
            user,
            new AzoaCustodialAccountBinding(
                tenantId.ToString("D"),
                user.id,
                StableIdempotencyKey("ardanova-custodial-account", tenantId.ToString("D"), user.id),
                StableIdempotencyKey("ardanova-kyc-session", tenantId.ToString("D"), user.id))));
    }

    private async Task<Result<AzoaCustodialAccountStatusDto>> ValidatePersistAndMapAsync(
        User user,
        AzoaCustodialAccountBinding binding,
        AzoaCustodialAccountStatus status,
        CancellationToken ct)
    {
        if (!string.Equals(status.TenantId, binding.TenantId, StringComparison.Ordinal)
            || !string.Equals(status.ArdaNovaUserId, binding.ArdaNovaUserId, StringComparison.Ordinal))
        {
            return Result<AzoaCustodialAccountStatusDto>.Conflict(
                "AZOA returned a custodial account bound to a different tenant or user.");
        }

        if (status.IdentityReady != !string.IsNullOrWhiteSpace(status.AvatarId)
            || status.WalletReady != (!string.IsNullOrWhiteSpace(status.WalletId)
                && !string.IsNullOrWhiteSpace(status.WalletAddress))
            || (status.KycStatus == AzoaKycStatus.Approved && !status.IdentityReady)
            || (status.Ready
                && (!status.IdentityReady
                    || !status.KycReady
                    || status.KycStatus != AzoaKycStatus.Approved
                    || !status.WalletReady)))
        {
            return Result<AzoaCustodialAccountStatusDto>.Conflict(
                "AZOA returned inconsistent custodial account readiness.");
        }

        var bindingError = StableReferenceError("avatar", user.azoaAvatarId, status.AvatarId)
            ?? StableReferenceError("wallet", user.azoaWalletId, status.WalletId)
            ?? StableReferenceError("wallet address", user.azoaWalletAddress, status.WalletAddress);
        if (bindingError is not null)
            return Result<AzoaCustodialAccountStatusDto>.Conflict(bindingError);

        var projectedVerificationLevel = ProjectVerificationLevel(user, status.KycStatus);
        var hasChanges =
            (string.IsNullOrWhiteSpace(user.azoaAvatarId) && !string.IsNullOrWhiteSpace(status.AvatarId))
            || (string.IsNullOrWhiteSpace(user.azoaWalletId) && !string.IsNullOrWhiteSpace(status.WalletId))
            || (string.IsNullOrWhiteSpace(user.azoaWalletAddress) && !string.IsNullOrWhiteSpace(status.WalletAddress))
            || user.verificationLevel != projectedVerificationLevel;

        if (hasChanges)
        {
            user.azoaAvatarId ??= status.AvatarId;
            user.azoaWalletId ??= status.WalletId;
            user.azoaWalletAddress ??= status.WalletAddress;
            user.verificationLevel = projectedVerificationLevel;
            user.updatedAt = DateTime.UtcNow;
            await _users.UpdateAsync(user, ct);
            await _unitOfWork.SaveChangesAsync(ct);
        }

        return Result<AzoaCustodialAccountStatusDto>.Success(new AzoaCustodialAccountStatusDto
        {
            AvatarId = user.azoaAvatarId,
            WalletId = user.azoaWalletId,
            WalletAddress = user.azoaWalletAddress,
            KycStatus = status.KycStatus,
            IdentityReady = status.IdentityReady,
            KycReady = status.KycReady,
            WalletReady = status.WalletReady,
            Ready = status.Ready,
            UnavailableReason = status.UnavailableReason,
        });
    }

    private static string StableIdempotencyKey(string operation, string tenantId, string userId)
    {
        var digest = SHA256.HashData(Encoding.UTF8.GetBytes($"{tenantId}\n{userId}"));
        return $"{operation}:{Convert.ToHexString(digest).ToLowerInvariant()}";
    }

    private static VerificationLevel ProjectVerificationLevel(User user, AzoaKycStatus status)
    {
        if (status == AzoaKycStatus.Approved && user.verificationLevel < VerificationLevel.PRO)
            return VerificationLevel.PRO;

        if (status != AzoaKycStatus.Approved && user.verificationLevel == VerificationLevel.PRO)
            return user.emailVerified.HasValue ? VerificationLevel.VERIFIED : VerificationLevel.ANONYMOUS;

        return user.verificationLevel;
    }

    private static string? StableReferenceError(string label, string? existing, string? returned)
        => !string.IsNullOrWhiteSpace(existing)
            && !string.IsNullOrWhiteSpace(returned)
            && !string.Equals(existing, returned, StringComparison.Ordinal)
                ? $"AZOA returned a different {label} for an existing custodial account binding."
                : null;

    private static bool IsSafeHostedVerificationUrl(string value)
        => Uri.TryCreate(value, UriKind.Absolute, out var uri)
            && uri.Scheme == Uri.UriSchemeHttps
            && string.IsNullOrEmpty(uri.UserInfo);

    private static Result<TOut> MapFailure<TIn, TOut>(Result<TIn> source)
    {
        var error = source.Error ?? "AZOA custodial account operation failed.";
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

    private sealed record UserBinding(User User, AzoaCustodialAccountBinding Binding);
}
