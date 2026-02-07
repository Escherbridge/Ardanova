namespace ArdaNova.Application.Services.Implementations;

using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Interfaces;
using ArdaNova.Domain.Models.Enums;

public class ManualKycProviderService : IKycProviderService
{
    private static readonly HashSet<string> AllowedMimeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/gif",
        "image/webp",
        "image/bmp",
        "application/pdf"
    };

    private const int MaxFileSizeBytes = 10 * 1024 * 1024; // 10 MB

    public Task<Result<string>> CreateSessionAsync(string userId, List<KycDocumentDto> documents, CancellationToken ct = default)
    {
        // Manual provider does not create an external session.
        // Return the userId as a pseudo-session identifier; the actual
        // submission ID is managed by KycService.
        return Task.FromResult(Result<string>.Success(userId));
    }

    public Task<Result<KycStatus>> GetSessionStatusAsync(string providerSessionId, CancellationToken ct = default)
    {
        // Manual provider has no external session tracking.
        // Status is managed entirely via the database and admin review endpoints.
        return Task.FromResult(Result<KycStatus>.Success(KycStatus.PENDING));
    }

    public Task<Result<KycStatus>> HandleWebhookAsync(string payload, CancellationToken ct = default)
    {
        // No-op for manual provider. Manual flow uses approve/reject endpoints.
        return Task.FromResult(Result<KycStatus>.Success(KycStatus.PENDING));
    }

    public Task<Result<bool>> ValidateDocumentsAsync(List<SubmitKycDocumentDto> documents, CancellationToken ct = default)
    {
        if (documents is null || documents.Count == 0)
            return Task.FromResult(Result<bool>.ValidationError("At least one document is required"));

        foreach (var doc in documents)
        {
            if (string.IsNullOrWhiteSpace(doc.FileUrl))
                return Task.FromResult(Result<bool>.ValidationError($"Document '{doc.FileName}' is missing a file URL"));

            if (string.IsNullOrWhiteSpace(doc.FileName))
                return Task.FromResult(Result<bool>.ValidationError("Document file name is required"));

            if (!string.IsNullOrWhiteSpace(doc.MimeType) && !AllowedMimeTypes.Contains(doc.MimeType))
                return Task.FromResult(Result<bool>.ValidationError(
                    $"Document '{doc.FileName}' has an unsupported file type '{doc.MimeType}'. Allowed types: JPEG, PNG, GIF, WebP, BMP, PDF"));

            if (doc.FileSizeBytes.HasValue && doc.FileSizeBytes.Value > MaxFileSizeBytes)
                return Task.FromResult(Result<bool>.ValidationError(
                    $"Document '{doc.FileName}' exceeds the maximum file size of {MaxFileSizeBytes / (1024 * 1024)} MB"));

            if (doc.FileSizeBytes.HasValue && doc.FileSizeBytes.Value <= 0)
                return Task.FromResult(Result<bool>.ValidationError(
                    $"Document '{doc.FileName}' has an invalid file size"));
        }

        return Task.FromResult(Result<bool>.Success(true));
    }
}
