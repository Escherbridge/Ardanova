namespace ArdaNova.Infrastructure.Kyc;

using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Interfaces;
using ArdaNova.Domain.Models.Enums;

/// <summary>
/// Stub implementation for the Veriff KYC provider.
/// This will be fully implemented when Veriff integration is ready.
/// Only registered when KYC_PROVIDER=veriff.
/// </summary>
public class VeriffKycProviderService : IKycProviderService
{
    private const string NotImplementedMessage =
        "Veriff integration not yet implemented. Set KYC_PROVIDER=manual to use the manual review provider.";

    public Task<Result<string>> CreateSessionAsync(string userId, List<KycDocumentDto> documents, CancellationToken ct = default)
    {
        throw new NotImplementedException(NotImplementedMessage);
    }

    public Task<Result<KycStatus>> GetSessionStatusAsync(string providerSessionId, CancellationToken ct = default)
    {
        throw new NotImplementedException(NotImplementedMessage);
    }

    public Task<Result<KycStatus>> HandleWebhookAsync(string payload, CancellationToken ct = default)
    {
        throw new NotImplementedException(NotImplementedMessage);
    }

    public Task<Result<bool>> ValidateDocumentsAsync(List<SubmitKycDocumentDto> documents, CancellationToken ct = default)
    {
        throw new NotImplementedException(NotImplementedMessage);
    }
}
