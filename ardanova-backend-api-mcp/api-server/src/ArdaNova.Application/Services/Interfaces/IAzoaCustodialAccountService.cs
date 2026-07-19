namespace ArdaNova.Application.Services.Interfaces;

using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;

public interface IAzoaCustodialAccountService
{
    Task<Result<AzoaCustodialAccountCapabilitiesDto>> GetCapabilitiesAsync(CancellationToken ct = default);
    Task<Result<AzoaCustodialAccountStatusDto>> EnsureAsync(string userId, CancellationToken ct = default);
    Task<Result<AzoaCustodialAccountStatusDto>> GetStatusAsync(string userId, CancellationToken ct = default);
    Task<Result<AzoaKycSessionDto>> BeginKycAsync(string userId, CancellationToken ct = default);
}

/// <summary>Tenant-scoped AZOA custody boundary; request and response contain no secret material.</summary>
public interface IAzoaCustodialAccountGateway
{
    Task<Result<AzoaCustodialAccountCapabilities>> GetCapabilitiesAsync(CancellationToken ct = default);

    Task<Result<AzoaCustodialAccountStatus>> EnsureAsync(
        AzoaCustodialAccountBinding binding,
        CancellationToken ct = default);

    Task<Result<AzoaCustodialAccountStatus>> GetStatusAsync(
        AzoaCustodialAccountBinding binding,
        CancellationToken ct = default);

    Task<Result<AzoaKycSession>> BeginKycAsync(
        AzoaCustodialAccountBinding binding,
        CancellationToken ct = default);

}

public sealed record AzoaCustodialAccountBinding(
    string TenantId,
    string ArdaNovaUserId,
    string IdempotencyKey,
    string KycSessionIdempotencyKey);

public sealed record AzoaCustodialAccountStatus(
    string TenantId,
    string ArdaNovaUserId,
    string? AvatarId,
    string? WalletId,
    string? WalletAddress,
    AzoaKycStatus KycStatus,
    bool IdentityReady,
    bool KycReady,
    bool WalletReady,
    bool Ready,
    string? UnavailableReason = null);

public sealed record AzoaCustodialAccountCapabilities(
    bool Enabled,
    string WalletChain,
    string CustodyMode,
    bool CustodyAvailable,
    bool BlockchainProviderAvailable,
    string KycProvider,
    bool KycAvailable,
    bool HostedVerification,
    bool AcceptsDocumentReferences,
    bool IdentityReady,
    bool KycReady,
    bool WalletProvisioningReady,
    bool Ready,
    bool DevelopmentSimulation = false,
    string? UnavailableReason = null);

public sealed record AzoaKycSession(
    string Provider,
    bool HostedVerification,
    bool AcceptsDocumentReferences,
    string? VerificationUrl,
    DateTime? ExpiresAt,
    string? Instructions,
    bool DevelopmentSimulation = false);
