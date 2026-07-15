using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ArdaNova.API.Middleware;

public class ApiKeyMiddleware
{
    private const string ApiKeyHeaderName = "X-Api-Key";
    private const string AdminApiKeyHeaderName = "X-Admin-Api-Key";
    public const string AdminRole = "Admin";
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
        // Stripe authenticates this one ingress route with its signed raw payload.
        // SignalR hubs handle their own API key validation via query parameters.
        if (context.Request.Path.StartsWithSegments("/health") ||
            context.Request.Path.StartsWithSegments("/swagger") ||
            context.Request.Path.StartsWithSegments("/hubs") ||
            string.Equals(
                context.Request.Path.Value,
                "/api/StripeWebhook/webhook",
                StringComparison.OrdinalIgnoreCase))
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

        if (!MatchesSecret(apiKey, extractedApiKey.ToString()))
        {
            _logger.LogWarning("Invalid API Key provided for {Path}", context.Request.Path);
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new { error = "Invalid API Key" });
            return;
        }

        context.User = CreatePrincipal(context.Request.Headers[AdminApiKeyHeaderName].ToString());

        await _next(context);
    }

    private ClaimsPrincipal CreatePrincipal(string suppliedAdminApiKey)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, "api-client"),
            new(ClaimTypes.Role, "Service")
        };

        var adminApiKey = GetAdminApiKey();
        if (!string.IsNullOrWhiteSpace(adminApiKey) && MatchesSecret(adminApiKey, suppliedAdminApiKey))
            claims.Add(new Claim(ClaimTypes.Role, AdminRole));

        return new ClaimsPrincipal(new ClaimsIdentity(claims, "ApiKey"));
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

    private string? GetAdminApiKey()
    {
        var adminApiKey = Environment.GetEnvironmentVariable("ADMIN_API_KEY");
        return string.IsNullOrWhiteSpace(adminApiKey)
            ? _configuration.GetValue<string>("AdminApiKey:Key")
            : adminApiKey;
    }

    private static bool MatchesSecret(string expected, string supplied)
        => CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(expected),
            Encoding.UTF8.GetBytes(supplied));
}

public static class AuthorizationPolicies
{
    public const string AdminApiKey = "AdminApiKey";
    public const string ActorAssertion = "ActorAssertion";
}

public static class ArdaNovaAuthorizationExtensions
{
    public static IServiceCollection AddArdaNovaAuthorization(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            options.AddPolicy(AuthorizationPolicies.AdminApiKey, policy =>
                policy.RequireAuthenticatedUser().RequireRole(ApiKeyMiddleware.AdminRole));
            options.AddPolicy(AuthorizationPolicies.ActorAssertion, policy =>
                policy.RequireAuthenticatedUser().RequireClaim(ActorAssertionMiddleware.ClaimType, "v2"));
        });

        return services;
    }
}

public static class ApiKeyMiddlewareExtensions
{
    public static IApplicationBuilder UseApiKeyAuthentication(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<ApiKeyMiddleware>();
    }
}
