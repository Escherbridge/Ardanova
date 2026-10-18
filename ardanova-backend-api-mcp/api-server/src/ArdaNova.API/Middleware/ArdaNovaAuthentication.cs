using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ArdaNova.API.Middleware;

/// <summary>Default scheme that only answers challenge (401) and forbid (403); identity comes from the credential middlewares. See AGENTS.md §Authorization denials.</summary>
public sealed class ArdaNovaDenialAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "ArdaNova";
    public const string AuthenticationRequiredMessage = "Authentication is required.";
    public const string ActorRequiredMessage = "A verified actor assertion is required.";
    public const string ForbiddenMessage = "You do not have permission to perform this action.";

    /// <summary>Never authenticates, so it can never replace the principal the credential middlewares built.</summary>
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        => Task.FromResult(AuthenticateResult.NoResult());

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
        => ApiErrorEnvelope.WriteAsync(Context, StatusCodes.Status401Unauthorized, AuthenticationRequiredMessage);

    protected override Task HandleForbiddenAsync(AuthenticationProperties properties)
        => ApiErrorEnvelope.WriteAsync(Context, StatusCodes.Status403Forbidden, ForbiddenMessage);
}

/// <summary>Turns a policy failure caused only by a missing actor assertion into 401; every other outcome uses the default flow.</summary>
public sealed class ArdaNovaAuthorizationResultHandler : IAuthorizationMiddlewareResultHandler
{
    private readonly AuthorizationMiddlewareResultHandler _default = new();

    public Task HandleAsync(
        RequestDelegate next,
        HttpContext context,
        AuthorizationPolicy policy,
        PolicyAuthorizationResult authorizeResult)
    {
        if (authorizeResult.Forbidden && RequiresActorAssertion(policy) && !HasActorAssertion(context))
            return ApiErrorEnvelope.WriteAsync(
                context,
                StatusCodes.Status401Unauthorized,
                ArdaNovaDenialAuthenticationHandler.ActorRequiredMessage);

        return _default.HandleAsync(next, context, policy, authorizeResult);
    }

    private static bool RequiresActorAssertion(AuthorizationPolicy policy)
        => policy.Requirements.OfType<ClaimsAuthorizationRequirement>()
            .Any(requirement => requirement.ClaimType == ActorAssertionMiddleware.ClaimType);

    private static bool HasActorAssertion(HttpContext context)
        => context.User.HasClaim(claim => claim.Type == ActorAssertionMiddleware.ClaimType);
}

public static class ArdaNovaAuthenticationExtensions
{
    /// <summary>Registers the denial-only default scheme so Forbid()/Challenge() write 403/401 instead of throwing.</summary>
    public static IServiceCollection AddArdaNovaAuthentication(this IServiceCollection services)
    {
        services
            .AddAuthentication(ArdaNovaDenialAuthenticationHandler.SchemeName)
            .AddScheme<AuthenticationSchemeOptions, ArdaNovaDenialAuthenticationHandler>(
                ArdaNovaDenialAuthenticationHandler.SchemeName,
                configureOptions: null);
        services.AddSingleton<IAuthorizationMiddlewareResultHandler, ArdaNovaAuthorizationResultHandler>();
        return services;
    }
}
