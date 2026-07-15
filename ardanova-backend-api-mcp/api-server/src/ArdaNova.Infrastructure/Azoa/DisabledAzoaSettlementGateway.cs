namespace ArdaNova.Infrastructure.Azoa;

using ArdaNova.Application.Services.Interfaces;

/// <inheritdoc/>
public sealed class DisabledAzoaSettlementGateway : IAzoaSettlementGateway
{
    /// <inheritdoc/>
    public Task<AzoaSettlementGatewayResult> DispatchAsync(
        AzoaSettlementRequest request,
        CancellationToken ct = default)
        => Task.FromResult(AzoaSettlementGatewayResult.Retry(
            "AZOA_SETTLEMENT_DISPATCH_DISABLED",
            "AZOA settlement dispatch is disabled until operator-reviewed live reconciliation and custody readiness are configured."));

    /// <inheritdoc/>
    public Task<AzoaSettlementGatewayResult> ReconcileAsync(
        AzoaSettlementRequest request,
        CancellationToken ct = default)
        => Task.FromResult(AzoaSettlementGatewayResult.Retry(
            "AZOA_SETTLEMENT_RECONCILIATION_DISABLED",
            "AZOA settlement reconciliation is disabled until an operator configures the production transport."));
}
