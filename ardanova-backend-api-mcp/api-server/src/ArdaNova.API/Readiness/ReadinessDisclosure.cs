using ArdaNova.API.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace ArdaNova.API.Readiness;

/// <summary>Decides how much of /ready an unauthenticated probe may read. See AGENTS.md §Disclosure.</summary>
public static class ReadinessDisclosure
{
    /// <summary>The per-check breakdown is for callers holding the service key; probes only need the status code.</summary>
    public static bool ShowsDetails(HttpContext context, IConfiguration configuration)
    {
        var apiKey = ApiKeyMiddleware.GetApiKey(configuration);
        var supplied = context.Request.Headers["X-Api-Key"].ToString();
        return ApiKeyMiddleware.IsStrongApiKey(apiKey)
            && supplied.Length > 0
            && ApiKeyMiddleware.MatchesSecret(apiKey, supplied);
    }

    /// <summary>The body an anonymous probe receives: status only.</summary>
    public static object Summary(bool ready, DateTime timestamp)
        => new { status = ready ? "ready" : "not_ready", timestamp, service = "ardanova-api" };
}
