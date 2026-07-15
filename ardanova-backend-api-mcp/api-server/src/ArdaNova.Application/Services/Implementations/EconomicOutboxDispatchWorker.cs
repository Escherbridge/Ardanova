namespace ArdaNova.Application.Services.Implementations;

using ArdaNova.Application.Services.Interfaces;

/// <summary>Explicitly invoked, disabled-by-default worker facade for economic outbox maintenance.</summary>
public sealed class EconomicOutboxDispatchWorker
{
    private readonly IEconomicOutboxDispatchService _dispatcher;
    private readonly EconomicOutboxDispatchOptions _options;

    public EconomicOutboxDispatchWorker(
        IEconomicOutboxDispatchService dispatcher,
        EconomicOutboxDispatchOptions options)
    {
        _dispatcher = dispatcher;
        _options = options;
    }

    /// <summary>Runs bounded dispatch and reconciliation attempts only when an operator enabled the worker.</summary>
    public async Task<IReadOnlyList<EconomicOutboxDispatchRun>> RunOnceAsync(CancellationToken ct = default)
    {
        if (!_options.Enabled)
            return [];

        var runs = new List<EconomicOutboxDispatchRun>();
        for (var i = 0; i < _options.BatchSize; i++)
        {
            var run = await _dispatcher.DispatchOneAsync(ct);
            runs.Add(run);
            if (run.Outcome == EconomicOutboxRecordedOutcome.NoWork)
                break;
        }

        for (var i = 0; i < _options.BatchSize; i++)
        {
            var run = await _dispatcher.ReconcileOneAsync(ct);
            runs.Add(run);
            if (run.Outcome == EconomicOutboxRecordedOutcome.NoWork)
                break;
        }

        return runs;
    }
}

/// <summary>Operator-controlled worker limits; disabled is the safe default.</summary>
public sealed class EconomicOutboxDispatchOptions
{
    public bool Enabled { get; init; }
    public int BatchSize { get; init; } = 10;
}
