namespace ArdaNova.Infrastructure.Storage;

using ArdaNova.Application.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for registering storage services
/// </summary>
public static class StorageServiceExtensions
{
    /// <summary>
    /// Adds storage services to the service collection
    /// </summary>
    public static IServiceCollection AddStorageServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Manually bind from environment variables (flat format)
        // This supports both appsettings.json format (Storage:S3:AccessKeyId) and
        // environment variable format (S3_ACCESS_KEY)
        services.Configure<StorageConfiguration>(options =>
        {
            // Provider
            options.Provider = GetConfigValue(configuration, "STORAGE_PROVIDER", "Storage:Provider") ?? "Local";

            // S3 Configuration
            options.S3.AccessKeyId = GetConfigValue(configuration, "S3_ACCESS_KEY", "Storage:S3:AccessKeyId");
            options.S3.SecretAccessKey = GetConfigValue(configuration, "S3_SECRET_KEY", "Storage:S3:SecretAccessKey");
            options.S3.BucketName = GetConfigValue(configuration, "S3_BUCKET_NAME", "Storage:S3:BucketName") ?? "";
            options.S3.Region = GetConfigValue(configuration, "S3_REGION", "Storage:S3:Region") ?? "us-east-1";
            options.S3.ServiceUrl = GetConfigValue(configuration, "S3_SERVICE_URL", "Storage:S3:ServiceUrl");
            options.S3.PublicBaseUrl = GetConfigValue(configuration, "S3_PUBLIC_BASE_URL", "Storage:S3:PublicBaseUrl");

            var forcePathStyle = GetConfigValue(configuration, "S3_USE_PATH_STYLE", "Storage:S3:ForcePathStyle");
            if (bool.TryParse(forcePathStyle, out var pathStyle))
            {
                options.S3.ForcePathStyle = pathStyle;
            }

            // Local Configuration
            options.Local = new LocalStorageConfiguration
            {
                BasePath = GetConfigValue(configuration, "LOCAL_STORAGE_PATH", "Storage:Local:BasePath") ?? "./uploads",
                PublicBaseUrl = GetConfigValue(configuration, "LOCAL_STORAGE_BASE_URL", "Storage:Local:PublicBaseUrl") ?? "http://localhost:8080/files"
            };
        });

        // Determine provider from configuration
        var provider = GetConfigValue(configuration, "STORAGE_PROVIDER", "Storage:Provider") ?? "Local";

        // Register the appropriate storage provider
        switch (provider.ToUpperInvariant())
        {
            case "S3":
            case "AWS":
            case "MINIO":
            case "R2":
            case "SPACES":
                services.AddSingleton<IStorageService, S3StorageService>();
                break;

            case "LOCAL":
                // For development - uses local file system
                services.AddSingleton<IStorageService, LocalStorageService>();
                break;

            default:
                // Default to Local for development
                services.AddSingleton<IStorageService, LocalStorageService>();
                break;
        }

        return services;
    }

    /// <summary>
    /// Gets configuration value from either environment variable or appsettings path
    /// </summary>
    private static string? GetConfigValue(IConfiguration configuration, string envVarName, string settingsPath)
    {
        // First try environment variable
        var envValue = Environment.GetEnvironmentVariable(envVarName);
        if (!string.IsNullOrEmpty(envValue))
        {
            return envValue;
        }

        // Fall back to configuration path
        return configuration[settingsPath];
    }
}
