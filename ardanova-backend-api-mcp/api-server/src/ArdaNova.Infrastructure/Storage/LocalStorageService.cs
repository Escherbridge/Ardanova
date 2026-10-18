namespace ArdaNova.Infrastructure.Storage;

using ArdaNova.Application.Common.Results;
using ArdaNova.Application.Common.Security;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Interfaces;
using ArdaNova.Domain.Models.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

/// <summary>
/// Local file system implementation of IStorageService (development only). It is the provider
/// an unset STORAGE_PROVIDER selects, so it refuses every operation it cannot enforce rather
/// than falling back to an unchecked directory.
/// </summary>
public class LocalStorageService : IStorageService
{
    /// <summary>Refusal text for the presigned PUT the local disk cannot serve (OI-17).</summary>
    public const string PresignedUploadUnsupportedMessage =
        "Local file storage cannot issue a presigned upload URL. Set STORAGE_PROVIDER "
        + "(or Storage:Provider) to an S3-compatible provider to upload files.";

    private const string OutsideStorageMessage = "The path resolves outside local storage.";

    private const int CopyBufferBytes = 81920;

    private readonly ILogger<LocalStorageService> _logger;
    private readonly IReadOnlySet<string> _allowedExtensions;
    private readonly long _maximumFileSizeBytes;
    private readonly string _basePath;
    private readonly string _publicBaseUrl;
    private readonly string? _unavailableReason;

    public LocalStorageService(
        IOptions<StorageConfiguration> config,
        ILogger<LocalStorageService> logger)
    {
        var configuration = config.Value;
        _logger = logger;
        _allowedExtensions = NormalizeExtensions(configuration.AllowedExtensions);
        _maximumFileSizeBytes = configuration.MaxFileSizeBytes;

        var basePath = configuration.Local?.BasePath?.Trim();
        var publicBaseUrl = configuration.Local?.PublicBaseUrl?.Trim();
        _unavailableReason = ResolveUnavailableReason(
            basePath,
            publicBaseUrl,
            _allowedExtensions,
            _maximumFileSizeBytes);

        if (_unavailableReason is not null)
        {
            // Local is the provider a blank STORAGE_PROVIDER selects, so an unconfigured one
            // refuses rather than writing unchecked bytes into a fallback directory.
            _logger.LogError(
                "Local file storage is refusing every request: {Reason}",
                _unavailableReason);
            _basePath = string.Empty;
            _publicBaseUrl = string.Empty;
            return;
        }

        _basePath = Path.TrimEndingDirectorySeparator(Path.GetFullPath(basePath!));
        _publicBaseUrl = publicBaseUrl!;

        if (!Directory.Exists(_basePath))
        {
            Directory.CreateDirectory(_basePath);
        }
    }

    /// <summary>The local disk has no presigned PUT, so it refuses instead of returning a POST endpoint.</summary>
    public Task<Result<UploadResponseDto>> GetPresignedUploadUrlAsync(
        UploadRequestDto request,
        string userId,
        CancellationToken ct = default)
        => Task.FromResult(Result<UploadResponseDto>.Conflict(PresignedRefusal()));

    /// <summary>The same refusal as the single-file presign; a bulk request cannot borrow a capability.</summary>
    public Task<Result<BulkUploadResponseDto>> GetPresignedUploadUrlsAsync(
        BulkUploadRequestDto request,
        string userId,
        CancellationToken ct = default)
        => Task.FromResult(Result<BulkUploadResponseDto>.Conflict(PresignedRefusal()));

    public Task<Result<DownloadResponseDto>> GetPresignedDownloadUrlAsync(
        string bucketPath,
        string? fileName = null,
        int expirationMinutes = 60,
        CancellationToken ct = default)
    {
        if (_unavailableReason is not null)
        {
            return Task.FromResult(Result<DownloadResponseDto>.Conflict(UnavailableMessage()));
        }

        try
        {
            if (!TryResolveContainedPath(bucketPath, out var fullPath) || !File.Exists(fullPath))
            {
                return Task.FromResult(Result<DownloadResponseDto>.NotFound("File not found"));
            }

            return Task.FromResult(Result<DownloadResponseDto>.Success(new DownloadResponseDto
            {
                DownloadUrl = GetPublicUrl(bucketPath),
                FileName = fileName ?? Path.GetFileName(bucketPath),
                ExpiresAt = DateTime.UtcNow.AddMinutes(UploadContentPolicy.ClampDownloadMinutes(expirationMinutes))
            }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating download URL for {BucketPath}", bucketPath);
            return Task.FromResult(Result<DownloadResponseDto>.Failure("Failed to generate download URL."));
        }
    }

    /// <summary>
    /// The only path on which bytes reach the local disk, so the extension allow-list and the size
    /// limit are enforced here rather than at a caller.
    /// </summary>
    public async Task<Result<string>> UploadAsync(
        Stream fileStream,
        string fileName,
        string contentType,
        string? folder = null,
        CancellationToken ct = default)
    {
        if (_unavailableReason is not null)
        {
            return Result<string>.Conflict(UnavailableMessage());
        }

        var disallowedExtension = RefuseDisallowedExtension(fileName);
        if (disallowedExtension is not null)
        {
            return Result<string>.ValidationError(disallowedExtension);
        }

        var bucketPath = GenerateBucketPath(fileName, folder);
        if (!TryResolveContainedPath(bucketPath, out var fullPath))
        {
            return Result<string>.ValidationError(OutsideStorageMessage);
        }

        try
        {
            var directory = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var writtenBytes = await WriteCappedAsync(fileStream, fullPath, ct);
            if (writtenBytes > _maximumFileSizeBytes)
            {
                // The cap is measured on the bytes that actually arrive, not on a declared size,
                // so a caller that under-declares still cannot land an oversized file.
                File.Delete(fullPath);
                _logger.LogWarning(
                    "Refused an oversized local upload of {FileName} ({WrittenBytes} bytes seen, limit {LimitBytes})",
                    fileName,
                    writtenBytes,
                    _maximumFileSizeBytes);
                return Result<string>.ValidationError(SizeExceededMessage());
            }

            _logger.LogInformation("Successfully uploaded file to {FullPath}", fullPath);
            return Result<string>.Success(bucketPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file {FileName}", fileName);
            return Result<string>.Failure("Failed to upload file.");
        }
    }

    public Task<Result<bool>> DeleteAsync(string bucketPath, CancellationToken ct = default)
    {
        if (_unavailableReason is not null)
        {
            return Task.FromResult(Result<bool>.Conflict(UnavailableMessage()));
        }

        try
        {
            if (!TryResolveContainedPath(bucketPath, out var fullPath))
            {
                return Task.FromResult(Result<bool>.ValidationError(OutsideStorageMessage));
            }

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
            return Task.FromResult(Result<bool>.Failure("Failed to delete file."));
        }
    }

    public Task<Result<bool>> ExistsAsync(string bucketPath, CancellationToken ct = default)
    {
        if (_unavailableReason is not null)
        {
            return Task.FromResult(Result<bool>.Conflict(UnavailableMessage()));
        }

        try
        {
            // A path outside the storage root is not "missing", it is not addressable at all, and
            // answering false keeps this from becoming a probe for the surrounding disk.
            if (!TryResolveContainedPath(bucketPath, out var fullPath))
            {
                return Task.FromResult(Result<bool>.Success(false));
            }

            return Task.FromResult(Result<bool>.Success(File.Exists(fullPath)));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if file exists {BucketPath}", bucketPath);
            return Task.FromResult(Result<bool>.Failure("Failed to check file existence."));
        }
    }

    /// <summary>Empty when this service may not serve the path; callers must treat empty as no URL.</summary>
    public string GetPublicUrl(string bucketPath)
    {
        if (_unavailableReason is not null || !TryResolveContainedPath(bucketPath, out _))
        {
            return string.Empty;
        }

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

    public async Task<Result<string>> CopyAsync(
        string sourcePath,
        string destinationPath,
        CancellationToken ct = default)
    {
        if (_unavailableReason is not null)
        {
            return Result<string>.Conflict(UnavailableMessage());
        }

        if (!TryResolveContainedPath(sourcePath, out var sourceFullPath)
            || !TryResolveContainedPath(destinationPath, out var destinationFullPath))
        {
            return Result<string>.ValidationError(OutsideStorageMessage);
        }

        var disallowedExtension = RefuseDisallowedExtension(destinationPath);
        if (disallowedExtension is not null)
        {
            return Result<string>.ValidationError(disallowedExtension);
        }

        try
        {
            if (!File.Exists(sourceFullPath))
            {
                return Result<string>.NotFound("Source file not found");
            }

            var destinationDirectory = Path.GetDirectoryName(destinationFullPath);
            if (!string.IsNullOrEmpty(destinationDirectory) && !Directory.Exists(destinationDirectory))
            {
                Directory.CreateDirectory(destinationDirectory);
            }

            await using var sourceStream = File.OpenRead(sourceFullPath);
            var writtenBytes = await WriteCappedAsync(sourceStream, destinationFullPath, ct);
            if (writtenBytes > _maximumFileSizeBytes)
            {
                File.Delete(destinationFullPath);
                return Result<string>.ValidationError(SizeExceededMessage());
            }

            _logger.LogInformation("Successfully copied file from {Source} to {Destination}",
                sourcePath, destinationPath);
            return Result<string>.Success(destinationPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error copying file from {Source} to {Destination}",
                sourcePath, destinationPath);
            return Result<string>.Failure("Failed to copy file.");
        }
    }

    /// <summary>Copies until the cap is passed; the returned count exceeds it when the source did.</summary>
    private async Task<long> WriteCappedAsync(Stream source, string fullPath, CancellationToken ct)
    {
        var buffer = new byte[CopyBufferBytes];
        var writtenBytes = 0L;

        await using var destination = File.Create(fullPath);
        int read;
        while ((read = await source.ReadAsync(buffer.AsMemory(), ct)) != 0)
        {
            writtenBytes += read;
            if (writtenBytes > _maximumFileSizeBytes)
            {
                return writtenBytes;
            }

            await destination.WriteAsync(buffer.AsMemory(0, read), ct);
        }

        return writtenBytes;
    }

    /// <summary>Null when the extension is on the configured allow-list; the refusal text otherwise.</summary>
    private string? RefuseDisallowedExtension(string fileName)
    {
        var extension = Path.GetExtension(fileName);
        return string.IsNullOrEmpty(extension) || !_allowedExtensions.Contains(extension)
            ? $"File extension '{extension}' is not allowed"
            : null;
    }

    /// <summary>True only for a relative path that resolves inside the configured storage root.</summary>
    private bool TryResolveContainedPath(string relativePath, out string fullPath)
    {
        fullPath = string.Empty;
        if (string.IsNullOrWhiteSpace(relativePath) || Path.IsPathRooted(relativePath))
        {
            return false;
        }

        string candidate;
        try
        {
            candidate = Path.GetFullPath(Path.Combine(_basePath, relativePath));
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return false;
        }

        var root = _basePath + Path.DirectorySeparatorChar;
        if (!candidate.StartsWith(root, StringComparison.Ordinal))
        {
            return false;
        }

        fullPath = candidate;
        return true;
    }

    private string PresignedRefusal()
        => _unavailableReason is null ? PresignedUploadUnsupportedMessage : UnavailableMessage();

    private string UnavailableMessage()
        => $"Local file storage is not available: {_unavailableReason}.";

    private string SizeExceededMessage()
        => $"File size exceeds maximum allowed size of {_maximumFileSizeBytes / 1024 / 1024}MB";

    /// <summary>
    /// Blank configuration resolves to the narrowest setting - refuse - never to "allow anything".
    /// An empty allow-list means S3StorageService permits every extension; local storage must not
    /// inherit that reading, because it is the provider an unset STORAGE_PROVIDER selects.
    /// </summary>
    private static string? ResolveUnavailableReason(
        string? basePath,
        string? publicBaseUrl,
        IReadOnlySet<string> allowedExtensions,
        long maximumFileSizeBytes)
    {
        if (string.IsNullOrWhiteSpace(basePath))
            return "no base path is configured (LOCAL_STORAGE_PATH or Storage:Local:BasePath)";

        if (string.IsNullOrWhiteSpace(publicBaseUrl))
            return "no public base URL is configured (LOCAL_STORAGE_BASE_URL or Storage:Local:PublicBaseUrl)";

        if (allowedExtensions.Count == 0)
            return "no upload extension allow-list is configured (Storage:AllowedExtensions)";

        if (maximumFileSizeBytes <= 0)
            return "no maximum file size is configured (STORAGE_MAX_FILE_BYTES or Storage:MaxFileSizeBytes)";

        return null;
    }

    /// <summary>Dot-prefixed, case-insensitive allow-list; blank and whitespace entries are dropped.</summary>
    private static IReadOnlySet<string> NormalizeExtensions(IEnumerable<string>? configured)
    {
        var normalized = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var extension in configured ?? Array.Empty<string>())
        {
            var trimmed = extension?.Trim();
            if (string.IsNullOrEmpty(trimmed))
            {
                continue;
            }

            normalized.Add(trimmed.StartsWith('.') ? trimmed : "." + trimmed);
        }

        return normalized;
    }

    private static string GenerateBucketPath(string fileName, string? folder = null)
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
