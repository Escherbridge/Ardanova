using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ArdaNova.API.Middleware;

/// <summary>
/// Default scheme that answers challenge with 401 and forbid with 403; it authenticates no one,
/// because identity comes from the API-key and actor-assertion middleware (see Middleware/AGENTS.md §Denial status codes).
/// </summary>
public sealed class StatusCodeAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "ArdaNovaStatusCodes";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        => Task.FromResult(AuthenticateResult.NoResult());

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
        => WriteDenialAsync(StatusCodes.Status401Unauthorized, "Authentication is required.");

    protected override Task HandleForbiddenAsync(AuthenticationProperties properties)
        => WriteDenialAsync(StatusCodes.Status403Forbidden, "This action is not permitted for the current caller.");

    private Task WriteDenialAsync(int statusCode, string error)
    {
        if (Response.HasStarted) return Task.CompletedTask;
        Response.StatusCode = statusCode;
        return Response.WriteAsJsonAsync(new { error, statusCode });
    }
}

public static class StatusCodeAuthenticationExtensions
{
    /// <summary>Registers the denial-only default scheme so Forbid()/Challenge() and policy failures map to 403/401.</summary>
    public static IServiceCollection AddArdaNovaDenialStatusCodes(this IServiceCollection services)
    {
        services.AddAuthentication(StatusCodeAuthenticationHandler.SchemeName)
            .AddScheme<AuthenticationSchemeOptions, StatusCodeAuthenticationHandler>(
                StatusCodeAuthenticationHandler.SchemeName, configureOptions: null);
        return services;
    }
}
