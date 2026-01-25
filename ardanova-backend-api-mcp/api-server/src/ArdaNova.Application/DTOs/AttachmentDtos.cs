namespace ArdaNova.Application.DTOs;

using ArdaNova.Domain.Models.Enums;

/// <summary>
/// DTO for returning attachment information
/// </summary>
public record AttachmentDto
{
    public string Id { get; init; } = null!;
    public string UploadedById { get; init; } = null!;
    public string? BucketPath { get; init; }
    public MimeType Type { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? LastUsedAt { get; init; }
    public string? Url { get; init; }
    public string? FileName { get; init; }
    public long? FileSize { get; init; }
}

/// <summary>
/// DTO for creating an attachment record (after upload)
/// </summary>
public record CreateAttachmentDto
{
    public required string UploadedById { get; init; }
    public required string BucketPath { get; init; }
    public required MimeType Type { get; init; }
    public string? FileName { get; init; }
    public long? FileSize { get; init; }
}

/// <summary>
/// DTO for updating attachment metadata
/// </summary>
public record UpdateAttachmentDto
{
    public DateTime? LastUsedAt { get; init; }
}

/// <summary>
/// DTO for requesting a presigned upload URL
/// </summary>
public record UploadRequestDto
{
    public required string FileName { get; init; }
    public required string ContentType { get; init; }
    public long? FileSize { get; init; }
    public string? Folder { get; init; }
}

/// <summary>
/// DTO for the response containing presigned upload URL
/// </summary>
public record UploadResponseDto
{
    public string UploadUrl { get; init; } = null!;
    public string BucketPath { get; init; } = null!;
    public string PublicUrl { get; init; } = null!;
    public DateTime ExpiresAt { get; init; }
    public Dictionary<string, string>? Headers { get; init; }
}

/// <summary>
/// DTO for requesting a presigned download URL
/// </summary>
public record DownloadRequestDto
{
    public required string AttachmentId { get; init; }
    public int ExpirationMinutes { get; init; } = 60;
}

/// <summary>
/// DTO for the response containing presigned download URL
/// </summary>
public record DownloadResponseDto
{
    public string DownloadUrl { get; init; } = null!;
    public string FileName { get; init; } = null!;
    public DateTime ExpiresAt { get; init; }
}

/// <summary>
/// DTO for bulk upload request
/// </summary>
public record BulkUploadRequestDto
{
    public required List<UploadRequestDto> Files { get; init; }
}

/// <summary>
/// DTO for bulk upload response
/// </summary>
public record BulkUploadResponseDto
{
    public List<UploadResponseDto> Uploads { get; init; } = new();
}

/// <summary>
/// DTO for attachment with user information
/// </summary>
public record AttachmentWithUserDto : AttachmentDto
{
    public UserDto? UploadedBy { get; init; }
}
