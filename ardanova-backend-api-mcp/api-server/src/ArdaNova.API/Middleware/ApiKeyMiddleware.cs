using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using ArdaNova.Application.Common.Security;
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
    internal const int MinimumApiKeyBytes = GeneratedSecretValidator.MinimumUtf8Bytes;
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
        // SignalR hubs validate their dedicated service and actor headers during connection setup.
        if (context.Request.Path.StartsWithSegments("/health") ||
            context.Request.Path.StartsWithSegments("/ready") ||
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
        var apiKey = GetApiKey(_configuration);

        if (!IsStrongApiKey(apiKey))
        {
            _logger.LogError(
                "API Key is not configured as a generated secret with at least {MinimumBytes} bytes",
                MinimumApiKeyBytes);
            context.Response.StatusCode = 500;
            await context.Response.WriteAsJsonAsync(new { error = "API Key is not configured securely" });
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

        var adminApiKey = GetAdminApiKey(_configuration);
        if (IsStrongApiKey(adminApiKey) && MatchesSecret(adminApiKey, suppliedAdminApiKey))
            claims.Add(new Claim(ClaimTypes.Role, AdminRole));

        return new ClaimsPrincipal(new ClaimsIdentity(claims, "ApiKey"));
    }

    internal static string? GetApiKey(IConfiguration configuration)
    {
        // First try API_KEY environment variable (shared .env file)
        var apiKey = Environment.GetEnvironmentVariable("API_KEY");

        if (!string.IsNullOrEmpty(apiKey))
        {
            return apiKey;
        }

        // Fallback to traditional .NET config (ApiKey:Key in appsettings.json)
        return configuration.GetValue<string>("ApiKey:Key") ?? configuration["API_KEY"];
    }

    internal static bool IsStrongApiKey([NotNullWhen(true)] string? apiKey)
        => IsStrongSecret(apiKey);

    internal static bool IsStrongSecret([NotNullWhen(true)] string? secret)
        => GeneratedSecretValidator.IsValid(secret);

    internal static string? GetAdminApiKey(IConfiguration configuration)
    {
        var adminApiKey = Environment.GetEnvironmentVariable("ADMIN_API_KEY");
        return string.IsNullOrWhiteSpace(adminApiKey)
            ? configuration.GetValue<string>("AdminApiKey:Key")
            : adminApiKey;
    }

    internal static bool MatchesSecret(string expected, string supplied)
    {
        var expectedHash = SHA256.HashData(Encoding.UTF8.GetBytes(expected));
        var suppliedHash = SHA256.HashData(Encoding.UTF8.GetBytes(supplied));
        return CryptographicOperations.FixedTimeEquals(expectedHash, suppliedHash);
    }
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
            options.FallbackPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();
            options.AddPolicy(AuthorizationPolicies.AdminApiKey, policy =>
                policy.RequireAuthenticatedUser().RequireRole(ApiKeyMiddleware.AdminRole));
            options.AddPolicy(AuthorizationPolicies.ActorAssertion, policy =>
                policy.RequireAssertion(context =>
                    ActorAssertionMiddleware.TryGetActorId(context.User, out _)));
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
