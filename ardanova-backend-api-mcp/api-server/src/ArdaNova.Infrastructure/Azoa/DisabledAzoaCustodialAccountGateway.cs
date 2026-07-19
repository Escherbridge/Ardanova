namespace ArdaNova.Infrastructure.Azoa;

using ArdaNova.Application.Common.Results;
using ArdaNova.Application.Services.Interfaces;

/// <summary>Fail-closed until AZOA exposes an attested tenant/user custodial account API.</summary>
public sealed class DisabledAzoaCustodialAccountGateway : IAzoaCustodialAccountGateway
{
    private const string Message =
        "AZOA tenant-bound custodial account onboarding/status is unavailable; no account or wallet was created.";

    public Task<Result<AzoaCustodialAccountCapabilities>> GetCapabilitiesAsync(
        CancellationToken ct = default)
        => Task.FromResult(Result<AzoaCustodialAccountCapabilities>.Success(new(
            Enabled: false,
            WalletChain: string.Empty,
            CustodyMode: "Disabled",
            CustodyAvailable: false,
            BlockchainProviderAvailable: false,
            KycProvider: string.Empty,
            KycAvailable: false,
            HostedVerification: false,
            AcceptsDocumentReferences: false,
            IdentityReady: false,
            KycReady: false,
            WalletProvisioningReady: false,
            Ready: false,
            UnavailableReason: Message)));

    public Task<Result<AzoaCustodialAccountStatus>> EnsureAsync(
        AzoaCustodialAccountBinding binding,
        CancellationToken ct = default)
        => Task.FromResult(Result<AzoaCustodialAccountStatus>.Conflict(Message));

    public Task<Result<AzoaCustodialAccountStatus>> GetStatusAsync(
        AzoaCustodialAccountBinding binding,
        CancellationToken ct = default)
        => Task.FromResult(Result<AzoaCustodialAccountStatus>.Conflict(Message));

    public Task<Result<AzoaKycSession>> BeginKycAsync(
        AzoaCustodialAccountBinding binding,
        CancellationToken ct = default)
        => Task.FromResult(Result<AzoaKycSession>.Conflict(Message));

}
