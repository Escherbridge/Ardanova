namespace ArdaNova.Application.Services.Interfaces;

/// <summary>Runtime capabilities of the selected Algorand boundary.</summary>
public sealed record AlgorandProviderCapabilities(
    string Provider,
    bool IsNoChain,
    bool SupportsAddressBasedCredentialLifecycle);
