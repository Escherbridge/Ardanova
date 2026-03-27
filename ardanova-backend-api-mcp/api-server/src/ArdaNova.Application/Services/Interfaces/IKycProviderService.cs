namespace ArdaNova.Application.Services.Interfaces;

using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Domain.Models.Enums;

public interface IKycProviderService
{
    Task<Result<string>> CreateSessionAsync(string userId, List<KycDocumentDto> documents, CancellationToken ct = default);
    Task<Result<KycStatus>> GetSessionStatusAsync(string providerSessionId, CancellationToken ct = default);
    Task<Result<KycStatus>> HandleWebhookAsync(string payload, CancellationToken ct = default);
    Task<Result<bool>> ValidateDocumentsAsync(List<SubmitKycDocumentDto> documents, CancellationToken ct = default);
}
