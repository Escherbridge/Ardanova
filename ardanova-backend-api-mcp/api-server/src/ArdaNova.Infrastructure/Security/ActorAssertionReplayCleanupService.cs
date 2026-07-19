namespace ArdaNova.Infrastructure.Security;

using ArdaNova.Application.Common.Security;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

/// <summary>Runs bounded actor assertion replay maintenance outside request execution.</summary>
public sealed class ActorAssertionReplayCleanupService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<ActorAssertionReplayCleanupService> _logger;

    public ActorAssertionReplayCleanupService(
        IServiceScopeFactory scopeFactory,
        TimeProvider timeProvider,
        ILogger<ActorAssertionReplayCleanupService> logger)
    {
        _scopeFactory = scopeFactory;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromSeconds(
            ActorAssertionReplayRetentionPolicy.CleanupIntervalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            await RunCycleSafelyAsync(stoppingToken);
            await Task.Delay(interval, _timeProvider, stoppingToken);
        }
    }

    private async Task RunCycleSafelyAsync(CancellationToken stoppingToken)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var cycle = scope.ServiceProvider.GetRequiredService<ActorAssertionReplayCleanupCycle>();
            var result = await cycle.RunAsync(stoppingToken);
            if (result.Removed > 0 || result.TimeBudgetReached)
            {
                _logger.LogInformation(
                    "Actor assertion replay cleanup removed {ReplayCount} records in {BatchCount} batches; time budget reached: {TimeBudgetReached}.",
                    result.Removed,
                    result.Batches,
                    result.TimeBudgetReached);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Normal hosted-service shutdown.
        }
        catch (Exception exception)
        {
            _logger.LogWarning(
                exception,
                "Actor assertion replay cleanup cycle failed; replay protection remains active.");
        }
    }
}
