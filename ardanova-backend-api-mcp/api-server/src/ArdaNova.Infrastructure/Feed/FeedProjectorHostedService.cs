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
                var run = await cycle.RunAsync(sweepDue, stoppingToken);
                if (run.Swept)
                    lastSweptAt = _clock.GetUtcNow();
                if (run.ChangedAnything || run.TargetsGone > 0)
                {
                    _logger.LogInformation(
                        "Feed projector: {Projected} projected, {Removed} removed, {Resynced} re-audienced, {TargetsGone} skipped (target gone) from {Examined} source rows.",
                        run.Projected,
                        run.Removed,
                        run.Resynced,
                        run.TargetsGone,
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

/// <summary>Registration for the feed projector, so one call site decides whether it runs.</summary>
public static class FeedProjectorRegistration
{
    /// <summary>
    /// Registers the projector's cycle and stores always (the admin backfill needs them) and its timer only
    /// when <c>Feed:Projector:Enabled</c> resolves true. Without this call the feed has no system cards.
    /// </summary>
    public static IServiceCollection AddFeedProjector(
        this IServiceCollection services,
        IConfiguration configuration,
        string? environmentName)
    {
        var options = FeedProjectorOptions.FromConfiguration(configuration, environmentName);
        services.AddSingleton(options);
        services.AddScoped<IFeedSourceReader, FeedSourceReader>();
        services.AddScoped<IFeedEnvelopeStore, FeedEnvelopeStore>();
        services.AddScoped<FeedProjectorCycle>();
        if (options.Enabled)
            services.AddHostedService<FeedProjectorHostedService>();

        return services;
    }
}
