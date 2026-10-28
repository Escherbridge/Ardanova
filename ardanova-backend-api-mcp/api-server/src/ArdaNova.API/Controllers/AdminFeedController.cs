namespace ArdaNova.API.Controllers;

using System.Security.Claims;
using ArdaNova.API.Middleware;
using ArdaNova.Application.Feed;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

/// <summary>Operator-only feed backfill: the projector's own pass with no time window; see Application/Feed/AGENTS.md.</summary>
[ApiController]
[Route("api/admin/feed")]
[Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
[Authorize(Policy = AuthorizationPolicies.AdminApiKey)]
public sealed class AdminFeedController : ControllerBase
{
    /// <summary>The most would-be envelopes a dry run lists; the counts are always complete.</summary>
    public const int PreviewLimit = 100;

    private readonly FeedProjectorCycle _projector;
    private readonly ILogger<AdminFeedController> _logger;

    public AdminFeedController(FeedProjectorCycle projector, ILogger<AdminFeedController> logger)
    {
        _projector = projector;
        _logger = logger;
    }

    /// <summary>Projects every feedable row ever written. A dry run is the default; only dryRun=false writes.</summary>
    [HttpPost("backfill")]
    public async Task<IActionResult> Backfill([FromQuery] bool dryRun = true, CancellationToken ct = default)
    {
        var run = await _projector.BackfillAsync(dryRun, ct);
        if (run.Outcome == FeedProjectorOutcome.RunnerBusy)
            return Conflict(new { error = "The feed projector is in the middle of a cycle; try again in a few seconds." });

        if (!run.DryRun)
        {
            _logger.LogWarning(
                "Feed backfill run by operator {OperatorActorId}: {Projected} projected, {Removed} removed, {Resynced} re-audienced from {Examined} source rows.",
                ActorId,
                run.Projected,
                run.Removed,
                run.Resynced,
                run.SourcesExamined);
        }

        return Ok(FeedBackfillResponse.From(run, PreviewLimit));
    }

    private string? ActorId => User.FindFirstValue(ClaimTypes.NameIdentifier);
}

/// <summary>A backfill result. For a dry run the counts say what WOULD change and Preview lists the first envelopes.</summary>
public sealed record FeedBackfillResponse(
    bool DryRun,
    int SourcesExamined,
    int Projected,
    int AlreadyProjected,
    int TargetsGone,
    int Removed,
    int Resynced,
    IReadOnlyDictionary<string, int> ProjectedByType,
    IReadOnlyList<FeedEnvelopePreview> Preview)
{
    public static FeedBackfillResponse From(FeedProjectorRun run, int previewLimit) => new(
        run.DryRun,
        run.SourcesExamined,
        run.Projected,
        run.AlreadyProjected,
        run.TargetsGone,
        run.Removed,
        run.Resynced,
        run.PendingEnvelopes
            .GroupBy(envelope => envelope.Type.ToString())
            .ToDictionary(group => group.Key, group => group.Count()),
        run.PendingEnvelopes
            .Take(previewLimit)
            .Select(envelope => new FeedEnvelopePreview(
                envelope.Type.ToString(),
                envelope.Key.SourceType.ToString(),
                envelope.Key.SourceId,
                envelope.Visibility.ToString(),
                envelope.OccurredAt,
                envelope.Title,
                envelope.Targets.ProjectId,
                envelope.Targets.GuildId,
                envelope.Targets.OpportunityId,
                envelope.Targets.EventId))
            .ToList());
}

/// <summary>One envelope a dry run would write.</summary>
public sealed record FeedEnvelopePreview(
    string Type,
    string SourceType,
    string SourceId,
    string Visibility,
    DateTime OccurredAt,
    string? Title,
    string? ProjectId,
    string? GuildId,
    string? OpportunityId,
    string? EventId);
