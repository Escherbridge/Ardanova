using ArdaNova.API.Middleware;
using ArdaNova.Application.Common.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace ArdaNova.API.Filters;

/// <summary>The record-level rule an action must pass before its service runs.</summary>
public enum ResourceRule
{
    /// <summary>The verified actor organizes the event.</summary>
    EventOrganizer,
    /// <summary>The viewer (anonymous allowed) may see the event; otherwise it does not exist.</summary>
    EventViewer,
    /// <summary>The verified actor manages the project that owns the product.</summary>
    ProductManager,
    /// <summary>The verified actor manages the named project.</summary>
    ProjectCatalogManager,
    /// <summary>The verified actor is the referrer or the referred user.</summary>
    ReferralParticipant,
    /// <summary>The verified actor created or belongs to the named project.</summary>
    ProjectAffiliate,
    /// <summary>The verified actor owns or belongs to the named guild.</summary>
    GuildAffiliate,
}

/// <summary>Which record an action touches (read like a subject binding) and the rule that guards it; an optional record is checked only when the request names one.</summary>
public sealed record ResourceGuard(ResourceRule Rule, ActorSubjectBinding Record, bool Optional = false)
{
    public bool RequiresActor => Rule != ResourceRule.EventViewer;
}

/// <summary>Looks the record up through <see cref="IResourceAuthority"/> and refuses with 401/403/404 before the action runs. See Filters/AGENTS.md §Resource guards.</summary>
public sealed class ResourceAuthorizationFilter(ResourceGuard guard) : IAsyncActionFilter
{
    public ResourceGuard Guard { get; } = guard;

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var http = context.HttpContext;
        var actorId = ViewerAuthority.ActorId(http.User);
        if (Guard.RequiresActor && actorId is null)
        {
            context.Result = Envelope(http, StatusCodes.Status401Unauthorized, ArdaNovaDenialAuthenticationHandler.ActorRequiredMessage);
            return;
        }

        if (!ViewerAuthority.IsAdministrator(http.User))
        {
            var recordId = Guard.Record.ReadSubject(context.ActionArguments);
            if (Guard.Optional && string.IsNullOrEmpty(recordId))
            {
                await next();
                return;
            }

            var access = string.IsNullOrEmpty(recordId)
                ? ResourceAccess.NotFound
                : await CheckAsync(http.RequestServices.GetRequiredService<IResourceAuthority>(), actorId, recordId, http.RequestAborted);

            if (access != ResourceAccess.Allowed)
            {
                context.Result = access == ResourceAccess.NotFound
                    ? Envelope(http, StatusCodes.Status404NotFound, ErrorEnvelopeResultFilter.NotFoundMessage)
                    : Envelope(http, StatusCodes.Status403Forbidden, ArdaNovaDenialAuthenticationHandler.ForbiddenMessage);
                return;
            }
        }

        await next();
    }

    private Task<ResourceAccess> CheckAsync(IResourceAuthority authority, string? actorId, string recordId, CancellationToken ct)
        => Guard.Rule switch
        {
            ResourceRule.EventOrganizer => authority.CanManageEventAsync(actorId!, recordId, ct),
            ResourceRule.EventViewer => authority.CanViewEventAsync(actorId, recordId, ct),
            ResourceRule.ProductManager => authority.CanManageProductAsync(actorId!, recordId, ct),
            ResourceRule.ProjectCatalogManager => authority.CanManageProjectCatalogAsync(actorId!, recordId, ct),
            ResourceRule.ReferralParticipant => authority.CanReadReferralAsync(actorId!, recordId, ct),
            ResourceRule.ProjectAffiliate => authority.CanAffiliateWithProjectAsync(actorId!, recordId, ct),
            ResourceRule.GuildAffiliate => authority.CanAffiliateWithGuildAsync(actorId!, recordId, ct),
            _ => Task.FromResult(ResourceAccess.Denied),
        };

    private static ObjectResult Envelope(HttpContext context, int statusCode, string error)
        => new(ApiErrorEnvelope.Create(context, statusCode, error)) { StatusCode = statusCode };
}
