namespace ArdaNova.Application.Services.Interfaces;

using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Domain.Models.Enums;

/// <summary>
/// Interface for cloud storage operations (S3, Azure Blob, etc.)
/// </summary>
public interface IStorageService
{
    /// <summary>
    /// Generates a presigned URL for uploading a file
    /// </summary>
    Task<Result<UploadResponseDto>> GetPresignedUploadUrlAsync(
        UploadRequestDto request,
        string userId,
        CancellationToken ct = default);

    /// <summary>
    /// Generates a presigned URL for downloading a file
    /// </summary>
    Task<Result<DownloadResponseDto>> GetPresignedDownloadUrlAsync(
        string bucketPath,
        string? fileName = null,
        int expirationMinutes = 60,
        CancellationToken ct = default);

    /// <summary>
    /// Uploads a file directly (for server-side uploads)
    /// </summary>
    Task<Result<string>> UploadAsync(
        Stream fileStream,
        string fileName,
        string contentType,
        string? folder = null,
        CancellationToken ct = default);

    /// <summary>
    /// Deletes a file from storage
    /// </summary>
    Task<Result<bool>> DeleteAsync(string bucketPath, CancellationToken ct = default);

    /// <summary>
    /// Checks if a file exists in storage
    /// </summary>
    Task<Result<bool>> ExistsAsync(string bucketPath, CancellationToken ct = default);

    /// <summary>
    /// Gets the public URL for a file (if bucket is public)
    /// </summary>
    string GetPublicUrl(string bucketPath);

    /// <summary>
    /// Determines the MimeType enum from a content type string
    /// </summary>
    MimeType GetMimeType(string contentType);

    /// <summary>
    /// Generates bulk presigned upload URLs
    /// </summary>
    Task<Result<BulkUploadResponseDto>> GetPresignedUploadUrlsAsync(
        BulkUploadRequestDto request,
        string userId,
        CancellationToken ct = default);

    /// <summary>
    /// Copies a file within the storage
    /// </summary>
    Task<Result<string>> CopyAsync(
        string sourcePath,
        string destinationPath,
        CancellationToken ct = default);
}

/// <summary>
/// Interface for attachment database operations
/// </summary>
public interface IAttachmentService
{
    Task<Result<AttachmentDto>> GetByIdAsync(string id, CancellationToken ct = default);
    Task<Result<IReadOnlyList<AttachmentDto>>> GetByUserIdAsync(string userId, CancellationToken ct = default);
    Task<Result<PagedResult<AttachmentDto>>> GetPagedAsync(int page, int pageSize, CancellationToken ct = default);
    Task<Result<AttachmentDto>> CreateAsync(CreateAttachmentDto dto, CancellationToken ct = default);
    Task<Result<AttachmentDto>> UpdateLastUsedAsync(string id, CancellationToken ct = default);
    Task<Result<bool>> DeleteAsync(string id, CancellationToken ct = default);
    Task<Result<IReadOnlyList<AttachmentDto>>> GetByTypeAsync(MimeType type, CancellationToken ct = default);
    Task<Result<AttachmentDto>> GetByBucketPathAsync(string bucketPath, CancellationToken ct = default);
}
