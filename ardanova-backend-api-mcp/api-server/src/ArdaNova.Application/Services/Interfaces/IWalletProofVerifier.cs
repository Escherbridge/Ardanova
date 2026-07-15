namespace ArdaNova.Application.Services.Interfaces;

public interface IWalletProofVerifier
{
    string Chain { get; }
    string Network { get; }
    bool TryNormalizeAddress(string address, out string canonicalAddress);
    bool Verify(string canonicalAddress, string message, string encodedSignature);
}
