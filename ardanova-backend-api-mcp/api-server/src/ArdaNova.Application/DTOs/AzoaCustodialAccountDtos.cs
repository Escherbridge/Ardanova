namespace ArdaNova.Application.DTOs;

public enum AzoaKycStatus
{
    Unknown,
    Pending,
    Approved,
    Rejected,
}

/// <summary>Authoritative AZOA account, wallet, and KYC readiness without custody material.</summary>
public sealed record AzoaCustodialAccountStatusDto
{
    public string? AvatarId { get; init; }
    public string? WalletId { get; init; }
    public string? WalletAddress { get; init; }
    public AzoaKycStatus KycStatus { get; init; }
    public bool IdentityReady { get; init; }
    public bool KycReady { get; init; }
    public bool WalletReady { get; init; }
    public bool Ready { get; init; }
    public string? UnavailableReason { get; init; }
}

/// <summary>Runtime custody and KYC capabilities reported by the selected AZOA node.</summary>
public sealed record AzoaCustodialAccountCapabilitiesDto
{
    public bool Enabled { get; init; }
    public string WalletChain { get; init; } = string.Empty;
    public string CustodyMode { get; init; } = string.Empty;
    public bool CustodyAvailable { get; init; }
    public bool BlockchainProviderAvailable { get; init; }
    public string KycProvider { get; init; } = string.Empty;
    public bool KycAvailable { get; init; }
    public bool HostedVerification { get; init; }
    public bool AcceptsDocumentReferences { get; init; }
    public bool DevelopmentSimulation { get; init; }
    public bool IdentityReady { get; init; }
    public bool KycReady { get; init; }
    public bool WalletProvisioningReady { get; init; }
    public bool Ready { get; init; }
    public string? UnavailableReason { get; init; }
}

/// <summary>Provider-neutral KYC session; no provider credentials or document data.</summary>
public sealed record AzoaKycSessionDto
{
    public string Provider { get; init; } = string.Empty;
    public bool HostedVerification { get; init; }
    public bool AcceptsDocumentReferences { get; init; }
    public bool DevelopmentSimulation { get; init; }
    public string? VerificationUrl { get; init; }
    public DateTime? ExpiresAt { get; init; }
    public string? Instructions { get; init; }
}
