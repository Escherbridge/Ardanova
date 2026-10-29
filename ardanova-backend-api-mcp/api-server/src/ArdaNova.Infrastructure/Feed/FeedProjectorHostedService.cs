namespace ArdaNova.Infrastructure.Feed;

using ArdaNova.Application.Feed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

/// <summary>Runs the feed projector on a timer, one cycle per fresh scope; the sweep rides along when due.</summary>
public sealed class FeedProjectorHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly FeedProjectorOptions _options;
    private readonly TimeProvider _clock;
    private readonly ILogger<FeedProjectorHostedService> _logger;

    /// <summary>Lives as long as the process: the cycle is Scoped, so the high-water mark is kept here.</summary>
    private readonly FeedProjectorWatermark _watermark = new();

    public FeedProjectorHostedService(
        IServiceScopeFactory scopeFactory,
        FeedProjectorOptions options,
        TimeProvider clock,
        ILogger<FeedProjectorHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options;
        _clock = clock;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(_options.Interval);
        // Null so the first cycle after a start sweeps: deletions made while the API was down are caught at once.
        DateTimeOffset? lastSweptAt = null;
        do
        {
            try
            {
                var sweepDue = lastSweptAt is not { } swept || _clock.GetUtcNow() - swept >= _options.SweepInterval;
                await using var scope = _scopeFactory.CreateAsyncScope();
                var cycle = scope.ServiceProvider.GetRequiredService<FeedProjectorCycle>();
                var run = await cycle.RunAsync(sweepDue, _watermark, stoppingToken);
                if (run.Swept)
                    lastSweptAt = _clock.GetUtcNow();
                if (run.ChangedAnything || run.TargetsGone > 0 || run.HadFailures)
                {
                    _logger.Log(
                        run.HadFailures ? LogLevel.Warning : LogLevel.Information,
                        "Feed projector: {Projected} projected, {Removed} removed, {Resynced} re-audienced, {TargetsGone} skipped (target gone), {SourceReadsFailed} source reads and {CardsFailed} cards failed, from {Examined} source rows.",
                        run.Projected,
                        run.Removed,
                        run.Resynced,
                        run.TargetsGone,
                        run.SourceReadsFailed,
                        run.CardsFailed,
                        run.SourcesExamined);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception failure)
            {
                _logger.LogError(failure, "The feed projector cycle failed; it will retry on the next interval.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}

/// <summary>Says, at Critical, that a malformed Feed:Projector setting left the projector off; see Feed/AGENTS.md.</summary>
public sealed class FeedProjectorMisconfiguredAlarm : IHostedService
{
    private readonly string _problem;
    private readonly ILogger<FeedProjectorMisconfiguredAlarm> _logger;

    public FeedProjectorMisconfiguredAlarm(string problem, ILogger<FeedProjectorMisconfiguredAlarm> logger)
    {
        _problem = problem;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogCritical(
            "The feed projector is OFF because its configuration is malformed: {Problem} The API started without it; fix the setting and restart.",
            _problem);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

/// <summary>Registration for the feed projector, so one call site decides whether it runs.</summary>
public static class FeedProjectorRegistration
{
    /// <summary>
    /// Registers the projector's cycle and stores always (the admin backfill needs them) and its timer only when
    /// <c>Feed:Projector:Enabled</c> is explicitly true. A malformed setting never fails startup: the timer stays
    /// off and <see cref="FeedProjectorMisconfiguredAlarm"/> logs it at Critical.
    /// </summary>
    public static IServiceCollection AddFeedProjector(this IServiceCollection services, IConfiguration configuration)
    {
        if (!FeedProjectorOptions.TryFromConfiguration(configuration, out var options, out var problem))
        {
            services.AddHostedService(provider => new FeedProjectorMisconfiguredAlarm(
                problem!,
                provider.GetRequiredService<ILogger<FeedProjectorMisconfiguredAlarm>>()));
        }

        services.AddSingleton(options);
        services.AddScoped<IFeedSourceReader, FeedSourceReader>();
        services.AddScoped<IFeedEnvelopeStore, FeedEnvelopeStore>();
        services.AddScoped<FeedProjectorCycle>();
        if (options.Enabled)
            services.AddHostedService<FeedProjectorHostedService>();

        return services;
    }
}
