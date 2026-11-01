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
    private readonly FeedQualityPass _quality;
    private readonly ILogger<AdminFeedController> _logger;

    public AdminFeedController(FeedProjectorCycle projector, FeedQualityPass quality, ILogger<AdminFeedController> logger)
    {
        _projector = projector;
        _quality = quality;
        _logger = logger;
    }

    /// <summary>Projects every feedable row ever written. A dry run is the default; only dryRun=false writes.</summary>
    /// <remarks>Idempotent: an envelope already projected is counted as such (the unique index), never doubled.
    /// An executed backfill then re-scores every post, so freshly projected cards rank on real signals.</remarks>
    [HttpPost("backfill")]
    public async Task<IActionResult> Backfill([FromQuery] bool dryRun = true, CancellationToken ct = default)
    {
        // The signed operator is recorded before anything runs, so even a refused or failing run has an author.
        _logger.LogWarning(
            "Feed backfill ({Mode}) requested by operator {OperatorActorId}.",
            dryRun ? "dry run" : "execute",
            ActorId);

        var run = await _projector.BackfillAsync(dryRun, ct);
        if (run.Outcome == FeedProjectorOutcome.RunnerBusy)
            return Conflict(new { error = "The feed projector is in the middle of a cycle; try again in a few seconds." });

        FeedQualityRun? scored = null;
        if (!run.DryRun)
        {
            scored = await _quality.RunAsync(ct);
            _logger.LogWarning(
                "Feed backfill run by operator {OperatorActorId}: {Projected} projected, {Removed} removed, {Retracted} retracted, {Restored} restored, {Resynced} re-audienced, {Retagged} re-tagged from {Examined} source rows; {SourceReadsFailed} source reads and {CardsFailed} cards failed; {Rescored} posts re-scored.",
                ActorId,
                run.Projected,
                run.Removed,
                run.Retracted,
                run.Restored,
                run.Resynced,
                run.Retagged,
                run.SourcesExamined,
                run.SourceReadsFailed,
                run.CardsFailed,
                scored.Updated);
        }

        return Ok(FeedBackfillResponse.From(run, PreviewLimit, scored));
    }

    private string? ActorId => User.FindFirstValue(ClaimTypes.NameIdentifier);
}

/// <summary>A backfill result. For a dry run the counts say what WOULD change and Preview lists the first envelopes.</summary>
/// <remarks>Non-zero SourceReadsFailed / CardsFailed means the pass skipped part of the work; the logs name what.
/// ProjectedBySourceType is per source table (PROJECT_MEMBER and GUILD_MEMBER both make MEMBER_JOINED cards).</remarks>
public sealed record FeedBackfillResponse(
    bool DryRun,
    int SourcesExamined,
    int Projected,
    int AlreadyProjected,
    int TargetsGone,
    int Removed,
    int Retracted,
    int Restored,
    int Resynced,
    int Retagged,
    int SourceReadsFailed,
    int CardsFailed,
    int? Rescored,
    IReadOnlyDictionary<string, int> ProjectedByType,
    IReadOnlyDictionary<string, int> ProjectedBySourceType,
    IReadOnlyList<FeedEnvelopePreview> Preview)
{
    public static FeedBackfillResponse From(FeedProjectorRun run, int previewLimit, FeedQualityRun? scored = null) => new(
        run.DryRun,
        run.SourcesExamined,
        run.Projected,
        run.AlreadyProjected,
        run.TargetsGone,
        run.Removed,
        run.Retracted,
        run.Restored,
        run.Resynced,
        run.Retagged,
        run.SourceReadsFailed,
        run.CardsFailed,
        scored?.Outcome == FeedProjectorOutcome.Completed ? scored.Updated : null,
        run.PendingEnvelopes
            .GroupBy(envelope => envelope.Type.ToString())
            .ToDictionary(group => group.Key, group => group.Count()),
        run.PendingEnvelopes
            .GroupBy(envelope => envelope.Key.SourceType.ToString())
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
