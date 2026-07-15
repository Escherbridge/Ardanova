namespace ArdaNova.Application.Services.Interfaces;

/// <summary>Provides compare-and-set outbox leasing and finalization for economic dispatch.</summary>
public interface IEconomicOutboxLeaseStore
{
    /// <summary>Claims one due outbox row or returns no work when another worker owns it.</summary>
    Task<EconomicOutboxLease?> TryClaimAsync(
        EconomicOutboxClaimKind kind,
        DateTime now,
        TimeSpan leaseDuration,
        CancellationToken ct = default);

    /// <summary>Atomically records an outcome only while the supplied lease remains current.</summary>
    Task<bool> FinalizeAsync(
        EconomicOutboxLease lease,
        EconomicOutboxFinalization finalization,
        DateTime now,
        CancellationToken ct = default);
}
