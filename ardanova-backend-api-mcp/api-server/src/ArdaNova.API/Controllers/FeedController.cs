namespace ArdaNova.API.Controllers;

using ArdaNova.API.Middleware;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.Feed;
using Microsoft.AspNetCore.Mvc;

/// <summary>The one polymorphic feed: infinite ranked scroll, home or scoped; see Application/Feed/AGENTS.md §Paging.</summary>
/// <remarks>
/// Behind the service key (the global fallback policy) but not ActorAssertion: signed out, the viewer is anonymous
/// and gets the PUBLIC subset; signed in, the signed actor is the viewer. Never a query value.
/// </remarks>
[ApiController]
[Route("api/feed")]
public sealed class FeedController : ControllerBase
{
    private readonly IFeedService _feed;

    public FeedController(IFeedService feed)
    {
        _feed = feed;
    }

    /// <summary>One page; pass the previous page's nextCursor to continue. scope: home | project:{id} | guild:{id} | tag:{tag}.</summary>
    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] string? cursor = null,
        [FromQuery] int? limit = null,
        [FromQuery] string? scope = null,
        CancellationToken ct = default)
    {
        var result = await _feed.GetPageAsync(new FeedPageRequest(ViewerId, scope, cursor, limit), ct);
        return result.Type switch
        {
            ResultType.Success => Ok(result.Value),
            ResultType.NotFound => NotFound(new { error = result.Error }),
            _ => BadRequest(new { error = result.Error }),
        };
    }

    private string? ViewerId => ActorAssertionMiddleware.TryGetActorId(User, out var actorId) ? actorId : null;
}
