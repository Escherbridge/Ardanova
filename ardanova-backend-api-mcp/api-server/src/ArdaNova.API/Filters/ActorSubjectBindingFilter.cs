using System.Security.Claims;
using ArdaNova.API.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ArdaNova.API.Filters;

/// <summary>Who the caller is, as the credential middlewares proved it.</summary>
public static class ViewerAuthority
{
    /// <summary>The admin service key, or a verified actor whose signed role is ADMIN.</summary>
    public static bool IsAdministrator(ClaimsPrincipal principal)
        => principal.IsInRole(ApiKeyMiddleware.AdminRole)
            || (ActorAssertionMiddleware.TryGetActorId(principal, out _)
                && principal.IsInRole(ArdaNovaAuthorizationExtensions.ActorAdministratorRole));

    public static string? ActorId(ClaimsPrincipal principal)
        => ActorAssertionMiddleware.TryGetActorId(principal, out var actorId) ? actorId : null;
}

/// <summary>Refuses a request whose named user is not the verified actor, before the action runs.</summary>
public sealed class ActorSubjectBindingFilter(ActorSubjectBinding binding) : IActionFilter
{
    public ActorSubjectBinding Binding { get; } = binding;

    public void OnActionExecuting(ActionExecutingContext context)
    {
        var user = context.HttpContext.User;
        var actorId = ViewerAuthority.ActorId(user);
        if (actorId is null)
        {
            context.Result = Envelope(context.HttpContext, StatusCodes.Status401Unauthorized, ArdaNovaDenialAuthenticationHandler.ActorRequiredMessage);
            return;
        }

        if (ViewerAuthority.IsAdministrator(user))
            return;

        if (!string.Equals(Binding.ReadSubject(context.ActionArguments), actorId, StringComparison.Ordinal))
            context.Result = Envelope(context.HttpContext, StatusCodes.Status403Forbidden, ArdaNovaDenialAuthenticationHandler.ForbiddenMessage);
    }

    public void OnActionExecuted(ActionExecutedContext context)
    {
    }

    private static ObjectResult Envelope(HttpContext context, int statusCode, string error)
        => new(ApiErrorEnvelope.Create(context, statusCode, error)) { StatusCode = statusCode };
}
