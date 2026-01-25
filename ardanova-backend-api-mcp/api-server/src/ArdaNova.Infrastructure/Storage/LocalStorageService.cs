namespace ArdaNova.Infrastructure.Storage;

using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Interfaces;
using ArdaNova.Domain.Models.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

/// <summary>
/// Local file system implementation of IStorageService (for development)
/// </summary>
public class LocalStorageService : IStorageService
{
    private readonly StorageConfiguration _config;
    private readonly ILogger<LocalStorageService> _logger;
    private readonly string _basePath;
    private readonly string _publicBaseUrl;

    public LocalStorageService(
        IOptions<StorageConfiguration> config,
        ILogger<LocalStorageService> logger)
    {
        _config = config.Value;
        _logger = logger;
        _basePath = _config.Local?.BasePath ?? "./uploads";
        _publicBaseUrl = _config.Local?.PublicBaseUrl ?? "http://localhost:8080/uploads";

        // Ensure base directory exists
        if (!Directory.Exists(_basePath))
        {
            Directory.CreateDirectory(_basePath);
        }
    }

    public Task<Result<UploadResponseDto>> GetPresignedUploadUrlAsync(
        UploadRequestDto request,
        string userId,
        CancellationToken ct = default)
    {
        try
        {
            // For local storage, we return a direct upload URL
            var bucketPath = GenerateBucketPath(request.FileName, request.Folder, userId);
            var fullPath = Path.Combine(_basePath, bucketPath);

            // Ensure directory exists
            var directory = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // In local mode, the "upload URL" is actually just the path
            // The client should use the direct upload endpoint instead
            return Task.FromResult(Result<UploadResponseDto>.Success(new UploadResponseDto
            {
                UploadUrl = $"{_publicBaseUrl.TrimEnd('/')}/api/attachments/upload",
                BucketPath = bucketPath,
                PublicUrl = GetPublicUrl(bucketPath),
                ExpiresAt = DateTime.UtcNow.AddMinutes(_config.DefaultExpirationMinutes),
                Headers = new Dictionary<string, string>
                {
                    ["Content-Type"] = request.ContentType
                }
            }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating upload path for {FileName}", request.FileName);
            return Task.FromResult(Result<UploadResponseDto>.Failure($"Failed to generate upload path: {ex.Message}"));
        }
    }

    public Task<Result<DownloadResponseDto>> GetPresignedDownloadUrlAsync(
        string bucketPath,
        string? fileName = null,
        int expirationMinutes = 60,
        CancellationToken ct = default)
    {
        try
        {
            var fullPath = Path.Combine(_basePath, bucketPath);
            if (!File.Exists(fullPath))
            {
                return Task.FromResult(Result<DownloadResponseDto>.NotFound("File not found"));
            }

            return Task.FromResult(Result<DownloadResponseDto>.Success(new DownloadResponseDto
            {
                DownloadUrl = GetPublicUrl(bucketPath),
                FileName = fileName ?? Path.GetFileName(bucketPath),
                ExpiresAt = DateTime.UtcNow.AddMinutes(expirationMinutes)
            }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating download URL for {BucketPath}", bucketPath);
            return Task.FromResult(Result<DownloadResponseDto>.Failure($"Failed to generate download URL: {ex.Message}"));
        }
    }

    public async Task<Result<string>> UploadAsync(
        Stream fileStream,
        string fileName,
        string contentType,
        string? folder = null,
        CancellationToken ct = default)
    {
        try
        {
            var bucketPath = GenerateBucketPath(fileName, folder);
            var fullPath = Path.Combine(_basePath, bucketPath);

            var directory = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            await using var fileStreamOut = File.Create(fullPath);
            await fileStream.CopyToAsync(fileStreamOut, ct);

            _logger.LogInformation("Successfully uploaded file to {FullPath}", fullPath);
            return Result<string>.Success(bucketPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file {FileName}", fileName);
            return Result<string>.Failure($"Failed to upload file: {ex.Message}");
        }
    }

    public Task<Result<bool>> DeleteAsync(string bucketPath, CancellationToken ct = default)
    {
        try
        {
            var fullPath = Path.Combine(_basePath, bucketPath);
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
                _logger.LogInformation("Successfully deleted file {FullPath}", fullPath);
            }

            return Task.FromResult(Result<bool>.Success(true));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file {BucketPath}", bucketPath);
            return Task.FromResult(Result<bool>.Failure($"Failed to delete file: {ex.Message}"));
        }
    }

    public Task<Result<bool>> ExistsAsync(string bucketPath, CancellationToken ct = default)
    {
        try
        {
            var fullPath = Path.Combine(_basePath, bucketPath);
            return Task.FromResult(Result<bool>.Success(File.Exists(fullPath)));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if file exists {BucketPath}", bucketPath);
            return Task.FromResult(Result<bool>.Failure($"Failed to check file existence: {ex.Message}"));
        }
    }

    public string GetPublicUrl(string bucketPath)
    {
        return $"{_publicBaseUrl.TrimEnd('/')}/{bucketPath.Replace('\\', '/')}";
    }

    public MimeType GetMimeType(string contentType)
    {
        if (string.IsNullOrEmpty(contentType))
            return MimeType.OTHER;

        var lowerType = contentType.ToLowerInvariant();

        return lowerType switch
        {
            var t when t.StartsWith("image/") => MimeType.IMAGE,
            var t when t.StartsWith("video/") => MimeType.VIDEO,
            var t when t.StartsWith("audio/") => MimeType.AUDIO,
            var t when t.StartsWith("application/pdf") => MimeType.DOCUMENT,
            var t when t.StartsWith("application/msword") => MimeType.DOCUMENT,
            var t when t.StartsWith("application/vnd.openxmlformats") => MimeType.DOCUMENT,
            var t when t.StartsWith("text/") => MimeType.DOCUMENT,
            var t when t.Contains("zip") || t.Contains("tar") || t.Contains("rar") || t.Contains("7z") => MimeType.ARCHIVE,
            _ => MimeType.OTHER
        };
    }

    public async Task<Result<BulkUploadResponseDto>> GetPresignedUploadUrlsAsync(
        BulkUploadRequestDto request,
        string userId,
        CancellationToken ct = default)
    {
        var uploads = new List<UploadResponseDto>();

        foreach (var file in request.Files)
        {
            var result = await GetPresignedUploadUrlAsync(file, userId, ct);
            if (result.IsSuccess)
            {
                uploads.Add(result.Value!);
            }
        }

        return Result<BulkUploadResponseDto>.Success(new BulkUploadResponseDto
        {
            Uploads = uploads
        });
    }

    public async Task<Result<string>> CopyAsync(
        string sourcePath,
        string destinationPath,
        CancellationToken ct = default)
    {
        try
        {
            var sourceFullPath = Path.Combine(_basePath, sourcePath);
            var destFullPath = Path.Combine(_basePath, destinationPath);

            if (!File.Exists(sourceFullPath))
            {
                return Result<string>.NotFound("Source file not found");
            }

            var destDirectory = Path.GetDirectoryName(destFullPath);
            if (!string.IsNullOrEmpty(destDirectory) && !Directory.Exists(destDirectory))
            {
                Directory.CreateDirectory(destDirectory);
            }

            await using var sourceStream = File.OpenRead(sourceFullPath);
            await using var destStream = File.Create(destFullPath);
            await sourceStream.CopyToAsync(destStream, ct);

            _logger.LogInformation("Successfully copied file from {Source} to {Destination}",
                sourcePath, destinationPath);
            return Result<string>.Success(destinationPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error copying file from {Source} to {Destination}",
                sourcePath, destinationPath);
            return Result<string>.Failure($"Failed to copy file: {ex.Message}");
        }
    }

    private static string GenerateBucketPath(string fileName, string? folder = null, string? userId = null)
    {
        var sanitizedFileName = SanitizeFileName(fileName);
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd");
        var extension = Path.GetExtension(fileName);
        var nameWithoutExt = Path.GetFileNameWithoutExtension(sanitizedFileName);

        var parts = new List<string>();

        if (!string.IsNullOrEmpty(folder))
        {
            parts.Add(folder.Trim('/').Replace('/', Path.DirectorySeparatorChar));
        }

        if (!string.IsNullOrEmpty(userId))
        {
            parts.Add($"user_{userId[..Math.Min(8, userId.Length)]}");
        }

        parts.Add(timestamp);
        parts.Add($"{nameWithoutExt}_{uniqueId}{extension}");

        return Path.Combine(parts.ToArray());
    }

    private static string SanitizeFileName(string fileName)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var sanitized = string.Join("_", fileName.Split(invalid, StringSplitOptions.RemoveEmptyEntries));
        return sanitized.Replace(" ", "_").ToLowerInvariant();
    }
}
