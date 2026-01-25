namespace ArdaNova.Infrastructure.Storage;

using System.Net;
using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Interfaces;
using ArdaNova.Domain.Models.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

/// <summary>
/// AWS S3 implementation of IStorageService
/// Supports S3-compatible services (AWS S3, MinIO, Cloudflare R2, DigitalOcean Spaces)
/// </summary>
public class S3StorageService : IStorageService, IDisposable
{
    private readonly IAmazonS3 _s3Client;
    private readonly StorageConfiguration _config;
    private readonly ILogger<S3StorageService> _logger;
    private readonly string _bucketName;

    public S3StorageService(
        IOptions<StorageConfiguration> config,
        ILogger<S3StorageService> logger)
    {
        _config = config.Value;
        _logger = logger;
        _bucketName = _config.S3.BucketName;

        var s3Config = new AmazonS3Config
        {
            RegionEndpoint = RegionEndpoint.GetBySystemName(_config.S3.Region),
            ForcePathStyle = _config.S3.ForcePathStyle
        };

        // Custom endpoint for S3-compatible services
        if (!string.IsNullOrEmpty(_config.S3.ServiceUrl))
        {
            s3Config.ServiceURL = _config.S3.ServiceUrl;
        }

        // Use explicit credentials if provided, otherwise rely on IAM/environment
        if (!string.IsNullOrEmpty(_config.S3.AccessKeyId) && !string.IsNullOrEmpty(_config.S3.SecretAccessKey))
        {
            _s3Client = new AmazonS3Client(
                _config.S3.AccessKeyId,
                _config.S3.SecretAccessKey,
                s3Config);
        }
        else
        {
            _s3Client = new AmazonS3Client(s3Config);
        }
    }

    public async Task<Result<UploadResponseDto>> GetPresignedUploadUrlAsync(
        UploadRequestDto request,
        string userId,
        CancellationToken ct = default)
    {
        try
        {
            // Validate file size
            if (request.FileSize.HasValue && request.FileSize.Value > _config.MaxFileSizeBytes)
            {
                return Result<UploadResponseDto>.ValidationError(
                    $"File size exceeds maximum allowed size of {_config.MaxFileSizeBytes / 1024 / 1024}MB");
            }

            // Validate extension if restrictions exist
            if (_config.AllowedExtensions.Count > 0)
            {
                var extension = Path.GetExtension(request.FileName)?.ToLowerInvariant();
                if (string.IsNullOrEmpty(extension) || !_config.AllowedExtensions.Contains(extension))
                {
                    return Result<UploadResponseDto>.ValidationError(
                        $"File extension '{extension}' is not allowed");
                }
            }

            // Generate unique path
            var bucketPath = GenerateBucketPath(request.FileName, request.Folder, userId);
            var expiresAt = DateTime.UtcNow.AddMinutes(_config.DefaultExpirationMinutes);

            var presignRequest = new GetPreSignedUrlRequest
            {
                BucketName = _bucketName,
                Key = bucketPath,
                Verb = HttpVerb.PUT,
                Expires = expiresAt,
                ContentType = request.ContentType
            };

            var uploadUrl = await _s3Client.GetPreSignedURLAsync(presignRequest);

            return Result<UploadResponseDto>.Success(new UploadResponseDto
            {
                UploadUrl = uploadUrl,
                BucketPath = bucketPath,
                PublicUrl = GetPublicUrl(bucketPath),
                ExpiresAt = expiresAt,
                Headers = new Dictionary<string, string>
                {
                    ["Content-Type"] = request.ContentType
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating presigned upload URL for {FileName}", request.FileName);
            return Result<UploadResponseDto>.Failure($"Failed to generate upload URL: {ex.Message}");
        }
    }

    public async Task<Result<DownloadResponseDto>> GetPresignedDownloadUrlAsync(
        string bucketPath,
        string? fileName = null,
        int expirationMinutes = 60,
        CancellationToken ct = default)
    {
        try
        {
            var expiresAt = DateTime.UtcNow.AddMinutes(expirationMinutes);

            var presignRequest = new GetPreSignedUrlRequest
            {
                BucketName = _bucketName,
                Key = bucketPath,
                Verb = HttpVerb.GET,
                Expires = expiresAt
            };

            // Set content-disposition if filename provided
            if (!string.IsNullOrEmpty(fileName))
            {
                presignRequest.ResponseHeaderOverrides = new ResponseHeaderOverrides
                {
                    ContentDisposition = $"attachment; filename=\"{fileName}\""
                };
            }

            var downloadUrl = await _s3Client.GetPreSignedURLAsync(presignRequest);

            return Result<DownloadResponseDto>.Success(new DownloadResponseDto
            {
                DownloadUrl = downloadUrl,
                FileName = fileName ?? Path.GetFileName(bucketPath),
                ExpiresAt = expiresAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating presigned download URL for {BucketPath}", bucketPath);
            return Result<DownloadResponseDto>.Failure($"Failed to generate download URL: {ex.Message}");
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

            var putRequest = new PutObjectRequest
            {
                BucketName = _bucketName,
                Key = bucketPath,
                InputStream = fileStream,
                ContentType = contentType
            };

            var response = await _s3Client.PutObjectAsync(putRequest, ct);

            if (response.HttpStatusCode == HttpStatusCode.OK)
            {
                _logger.LogInformation("Successfully uploaded file to {BucketPath}", bucketPath);
                return Result<string>.Success(bucketPath);
            }

            return Result<string>.Failure($"Upload failed with status {response.HttpStatusCode}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file {FileName}", fileName);
            return Result<string>.Failure($"Failed to upload file: {ex.Message}");
        }
    }

    public async Task<Result<bool>> DeleteAsync(string bucketPath, CancellationToken ct = default)
    {
        try
        {
            var deleteRequest = new DeleteObjectRequest
            {
                BucketName = _bucketName,
                Key = bucketPath
            };

            var response = await _s3Client.DeleteObjectAsync(deleteRequest, ct);

            if (response.HttpStatusCode == HttpStatusCode.NoContent ||
                response.HttpStatusCode == HttpStatusCode.OK)
            {
                _logger.LogInformation("Successfully deleted file {BucketPath}", bucketPath);
                return Result<bool>.Success(true);
            }

            return Result<bool>.Failure($"Delete failed with status {response.HttpStatusCode}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file {BucketPath}", bucketPath);
            return Result<bool>.Failure($"Failed to delete file: {ex.Message}");
        }
    }

    public async Task<Result<bool>> ExistsAsync(string bucketPath, CancellationToken ct = default)
    {
        try
        {
            var request = new GetObjectMetadataRequest
            {
                BucketName = _bucketName,
                Key = bucketPath
            };

            await _s3Client.GetObjectMetadataAsync(request, ct);
            return Result<bool>.Success(true);
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return Result<bool>.Success(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if file exists {BucketPath}", bucketPath);
            return Result<bool>.Failure($"Failed to check file existence: {ex.Message}");
        }
    }

    public string GetPublicUrl(string bucketPath)
    {
        if (!string.IsNullOrEmpty(_config.S3.PublicBaseUrl))
        {
            return $"{_config.S3.PublicBaseUrl.TrimEnd('/')}/{bucketPath}";
        }

        // Default S3 URL format
        return $"https://{_bucketName}.s3.{_config.S3.Region}.amazonaws.com/{bucketPath}";
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
        var errors = new List<string>();

        foreach (var file in request.Files)
        {
            var result = await GetPresignedUploadUrlAsync(file, userId, ct);
            if (result.IsSuccess)
            {
                uploads.Add(result.Value!);
            }
            else
            {
                errors.Add($"{file.FileName}: {result.Error}");
            }
        }

        if (errors.Count > 0 && uploads.Count == 0)
        {
            return Result<BulkUploadResponseDto>.Failure(string.Join("; ", errors));
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
            var copyRequest = new CopyObjectRequest
            {
                SourceBucket = _bucketName,
                SourceKey = sourcePath,
                DestinationBucket = _bucketName,
                DestinationKey = destinationPath
            };

            var response = await _s3Client.CopyObjectAsync(copyRequest, ct);

            if (response.HttpStatusCode == HttpStatusCode.OK)
            {
                _logger.LogInformation("Successfully copied file from {Source} to {Destination}",
                    sourcePath, destinationPath);
                return Result<string>.Success(destinationPath);
            }

            return Result<string>.Failure($"Copy failed with status {response.HttpStatusCode}");
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
            parts.Add(folder.Trim('/'));
        }

        if (!string.IsNullOrEmpty(userId))
        {
            parts.Add($"user_{userId[..8]}");
        }

        parts.Add(timestamp);
        parts.Add($"{nameWithoutExt}_{uniqueId}{extension}");

        return string.Join("/", parts);
    }

    private static string SanitizeFileName(string fileName)
    {
        // Remove invalid characters and replace spaces
        var invalid = Path.GetInvalidFileNameChars();
        var sanitized = string.Join("_", fileName.Split(invalid, StringSplitOptions.RemoveEmptyEntries));
        return sanitized.Replace(" ", "_").ToLowerInvariant();
    }

    public void Dispose()
    {
        _s3Client?.Dispose();
    }
}
