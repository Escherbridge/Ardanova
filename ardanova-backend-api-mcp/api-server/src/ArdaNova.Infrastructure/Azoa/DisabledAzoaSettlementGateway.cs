namespace ArdaNova.Infrastructure.Azoa;

using ArdaNova.Application.Services.Interfaces;

/// <summary>The gateway registered when no node was selected to settle: it refuses, and it says so.</summary>
/// <remarks>
/// It implements <see cref="ISelectedNodeSettlementCapability"/> deliberately. Readiness is a type
/// test (<c>gateway is ISelectedNodeSettlementCapability</c>), and a disabled gateway that simply
/// omitted the interface would pass that gate by accident of typing rather than by declaring what
/// it cannot do. Declaring the capability with every answer false makes the refusal the gate's
/// actual reason, so the readiness tests prove the gate rather than the class hierarchy.
/// </remarks>
public sealed class DisabledAzoaSettlementGateway : IAzoaSettlementGateway, ISelectedNodeSettlementCapability
{
    /// <inheritdoc/>
    public string SelectedNodeId => string.Empty;

    /// <inheritdoc/>
    public bool CanDispatch => false;

    /// <inheritdoc/>
    public bool CanReconcile => false;

    /// <inheritdoc/>
    public bool HasCurrentOperatorAttestation => false;

    /// <inheritdoc/>
    public bool SupportsCanonicalFundingSettlement => false;

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
