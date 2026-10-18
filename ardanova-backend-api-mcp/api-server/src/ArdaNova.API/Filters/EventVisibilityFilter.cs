using ArdaNova.API.Middleware;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.Common.Security;
using ArdaNova.Application.DTOs;
using ArdaNova.Domain.Models.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace ArdaNova.API.Filters;

/// <summary>Drops events the viewer may not see and blanks meeting links they may not use, on every EventDto response. See Filters/AGENTS.md §Event visibility.</summary>
public sealed class EventVisibilityFilter : IAsyncResultFilter
{
    public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
    {
        if (context.Result is ObjectResult { Value: { } value, StatusCode: null or < 400 } result
            && !ViewerAuthority.IsAdministrator(context.HttpContext.User))
        {
            IReadOnlyCollection<EventDto>? events = value switch
            {
                EventDto single => [single],
                PagedResult<EventDto> page => page.Items,
                IEnumerable<EventDto> list => list as IReadOnlyCollection<EventDto> ?? list.ToList(),
                _ => null,
            };

            if (events is not null)
            {
                var viewerId = ViewerAuthority.ActorId(context.HttpContext.User);
                var relations = await RelationsAsync(context.HttpContext, viewerId, events);
                var shown = events
                    .Where(e => EventVisibilityPolicy.CanSee(Facts(e), viewerId, relations))
                    .Select(e => EventVisibilityPolicy.CanSeeMeetingLink(Facts(e), viewerId, relations) ? e : e with { MeetingUrl = null })
                    .ToList();

                context.Result = value switch
                {
                    EventDto when shown.Count == 0 => new ObjectResult(
                        ApiErrorEnvelope.Create(context.HttpContext, StatusCodes.Status404NotFound, ErrorEnvelopeResultFilter.NotFoundMessage))
                    {
                        StatusCode = StatusCodes.Status404NotFound,
                    },
                    EventDto => Replace(result, shown[0]),
                    // Counts stay the repository's; see AGENTS.md for why a page may come back short.
                    PagedResult<EventDto> page => Replace(result, new PagedResult<EventDto>(shown, page.TotalCount, page.Page, page.PageSize)),
                    _ => Replace(result, shown),
                };
            }
        }

        await next();
    }

    private static async Task<EventViewerRelations> RelationsAsync(HttpContext http, string? viewerId, IReadOnlyCollection<EventDto> events)
        => viewerId is null || !events.Any(e => NeedsRelations(e, viewerId))
            ? EventViewerRelations.None
            : await http.RequestServices.GetRequiredService<IResourceAuthority>()
                .GetEventViewerRelationsAsync(viewerId, events.Select(e => e.Id).ToList(), http.RequestAborted);

    /// <summary>Only a hidden event or a meeting link the viewer does not host needs the membership lookup.</summary>
    private static bool NeedsRelations(EventDto e, string viewerId)
        => !string.Equals(e.OrganizerId, viewerId, StringComparison.Ordinal)
            && (e.Visibility != EventVisibility.PUBLIC || e.MeetingUrl is not null);

    private static EventVisibilityFacts Facts(EventDto e)
        => new(e.Id, e.Visibility, e.OrganizerId, e.ProjectId, e.GuildId);

    private static ObjectResult Replace(ObjectResult original, object value)
    {
        original.Value = value;
        original.DeclaredType = null;
        return original;
    }
}
