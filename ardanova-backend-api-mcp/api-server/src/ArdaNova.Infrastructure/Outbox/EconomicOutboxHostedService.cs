namespace ArdaNova.Infrastructure.Outbox;

using ArdaNova.Application.Services.Implementations;
using ArdaNova.Application.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

public sealed record SettlementOutboxRuntimeCapability(bool IsHostedDispatcherRegistered)
    : ISettlementOutboxRuntimeCapability;

/// <summary>Runs bounded outbox cycles only when explicitly registered by configuration.</summary>
public sealed class EconomicOutboxHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly EconomicOutboxDispatchOptions _options;
    private readonly ILogger<EconomicOutboxHostedService> _logger;

    public EconomicOutboxHostedService(
        IServiceScopeFactory scopeFactory,
        EconomicOutboxDispatchOptions options,
        ILogger<EconomicOutboxHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(_options.Interval);
        do
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var worker = scope.ServiceProvider.GetRequiredService<EconomicOutboxDispatchWorker>();
                await worker.RunOnceAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "The bounded economic outbox cycle failed; it will retry on the next interval.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
