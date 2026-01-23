namespace ArdaNova.API.Middleware;

public class ApiKeyMiddleware
{
    private const string ApiKeyHeaderName = "X-Api-Key";
    private readonly RequestDelegate _next;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ApiKeyMiddleware> _logger;

    public ApiKeyMiddleware(
        RequestDelegate next,
        IConfiguration configuration,
        ILogger<ApiKeyMiddleware> logger)
    {
        _next = next;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip API key check for health and Swagger endpoints
        if (context.Request.Path.StartsWithSegments("/health") ||
            context.Request.Path.StartsWithSegments("/swagger"))
        {
            await _next(context);
            return;
        }

        if (!context.Request.Headers.TryGetValue(ApiKeyHeaderName, out var extractedApiKey))
        {
            _logger.LogWarning("API Key was not provided for {Path}", context.Request.Path);
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new { error = "API Key was not provided" });
            return;
        }

        // Get API key - prefer API_KEY env var, fallback to config
        var apiKey = GetApiKey();

        if (string.IsNullOrEmpty(apiKey))
        {
            _logger.LogError("API Key is not configured");
            context.Response.StatusCode = 500;
            await context.Response.WriteAsJsonAsync(new { error = "API Key is not configured" });
            return;
        }

        if (!apiKey.Equals(extractedApiKey))
        {
            _logger.LogWarning("Invalid API Key provided for {Path}", context.Request.Path);
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new { error = "Invalid API Key" });
            return;
        }

        await _next(context);
    }

    private string? GetApiKey()
    {
        // First try API_KEY environment variable (shared .env file)
        var apiKey = Environment.GetEnvironmentVariable("API_KEY");

        if (!string.IsNullOrEmpty(apiKey))
        {
            return apiKey;
        }

        // Fallback to traditional .NET config (ApiKey:Key in appsettings.json)
        return _configuration.GetValue<string>("ApiKey:Key");
    }
}

public static class ApiKeyMiddlewareExtensions
{
    public static IApplicationBuilder UseApiKeyAuthentication(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<ApiKeyMiddleware>();
    }
}
