namespace ArdaNova.Application.Services.Interfaces;

/// <summary>Runs one lease-protected dispatch or reconciliation attempt without completing value settlement.</summary>
public interface IEconomicOutboxDispatchService
{
    /// <summary>Claims and dispatches one pending economic decision.</summary>
    Task<EconomicOutboxDispatchRun> DispatchOneAsync(CancellationToken ct = default);

    /// <summary>Claims and reconciles one ambiguous economic decision.</summary>
    Task<EconomicOutboxDispatchRun> ReconcileOneAsync(CancellationToken ct = default);
}
