namespace ArdaNova.Infrastructure.Storage;

/// <summary>
/// Configuration options for cloud storage
/// </summary>
public class StorageConfiguration
{
    public const string SectionName = "Storage";

    /// <summary>
    /// The cloud storage provider (S3, Azure, GCS, Local)
    /// </summary>
    public string Provider { get; set; } = "S3";

    /// <summary>
    /// S3 configuration
    /// </summary>
    public S3Configuration S3 { get; set; } = new();

    /// <summary>
    /// Azure Blob configuration (future)
    /// </summary>
    public AzureBlobConfiguration? AzureBlob { get; set; }

    /// <summary>
    /// Local storage configuration (for development)
    /// </summary>
    public LocalStorageConfiguration? Local { get; set; }

    /// <summary>
    /// Default presigned URL expiration in minutes
    /// </summary>
    public int DefaultExpirationMinutes { get; set; } = 60;

    /// <summary>
    /// Maximum file size in bytes (default 100MB)
    /// </summary>
    public long MaxFileSizeBytes { get; set; } = 104_857_600;

    /// <summary>
    /// Allowed file extensions (empty = all allowed)
    /// </summary>
    public List<string> AllowedExtensions { get; set; } = new();
}

/// <summary>
/// AWS S3 specific configuration
/// </summary>
public class S3Configuration
{
    /// <summary>
    /// AWS Region (e.g., "us-east-1")
    /// </summary>
    public string Region { get; set; } = "us-east-1";

    /// <summary>
    /// S3 Bucket name
    /// </summary>
    public string BucketName { get; set; } = string.Empty;

    /// <summary>
    /// AWS Access Key ID (optional if using IAM roles)
    /// </summary>
    public string? AccessKeyId { get; set; }

    /// <summary>
    /// AWS Secret Access Key (optional if using IAM roles)
    /// </summary>
    public string? SecretAccessKey { get; set; }

    /// <summary>
    /// Custom endpoint URL (for S3-compatible services like MinIO, Cloudflare R2)
    /// </summary>
    public string? ServiceUrl { get; set; }

    /// <summary>
    /// Whether to use path-style addressing (required for some S3-compatible services)
    /// </summary>
    public bool ForcePathStyle { get; set; } = false;

    /// <summary>
    /// Public base URL for the bucket (for CDN or custom domain)
    /// </summary>
    public string? PublicBaseUrl { get; set; }
}

/// <summary>
/// Azure Blob Storage configuration (future implementation)
/// </summary>
public class AzureBlobConfiguration
{
    public string ConnectionString { get; set; } = string.Empty;
    public string ContainerName { get; set; } = string.Empty;
    public string? PublicBaseUrl { get; set; }
}

/// <summary>
/// Local file system storage configuration (for development)
/// </summary>
public class LocalStorageConfiguration
{
    public string BasePath { get; set; } = "./uploads";
    public string PublicBaseUrl { get; set; } = "http://localhost:8080/uploads";
}
