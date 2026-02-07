namespace ArdaNova.Application.DTOs;

using ArdaNova.Domain.Models.Enums;

public record KycSubmissionDto
{
    public string Id { get; init; } = null!;
    public string UserId { get; init; } = null!;
    public KycProvider Provider { get; init; }
    public KycStatus Status { get; init; }
    public string? ReviewerId { get; init; }
    public string? ReviewNotes { get; init; }
    public string? RejectionReason { get; init; }
    public string? ProviderSessionId { get; init; }
    public DateTime SubmittedAt { get; init; }
    public DateTime? ReviewedAt { get; init; }
    public DateTime? ExpiresAt { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public List<KycDocumentDto> Documents { get; init; } = new();
}

public record SubmitKycDto
{
    public required string UserId { get; init; }
    public required List<SubmitKycDocumentDto> Documents { get; init; }
}

public record SubmitKycDocumentDto
{
    public required KycDocumentType Type { get; init; }
    public required string FileUrl { get; init; }
    public required string FileName { get; init; }
    public string? MimeType { get; init; }
    public int? FileSizeBytes { get; init; }
    public string? Metadata { get; init; }
}

public record ReviewKycDto
{
    public string? ReviewNotes { get; init; }
    public string? RejectionReason { get; init; }
}

public record KycStatusDto
{
    public KycStatus Status { get; init; }
    public DateTime SubmittedAt { get; init; }
    public DateTime? ReviewedAt { get; init; }
    public string? RejectionReason { get; init; }
}

public record KycDocumentDto
{
    public string Id { get; init; } = null!;
    public string SubmissionId { get; init; } = null!;
    public KycDocumentType Type { get; init; }
    public string FileUrl { get; init; } = null!;
    public string FileName { get; init; } = null!;
    public string? MimeType { get; init; }
    public int? FileSizeBytes { get; init; }
    public string? Metadata { get; init; }
    public DateTime CreatedAt { get; init; }
}
