namespace ArdaNova.Application.Services.Interfaces;

/// <summary>Submits and reconciles immutable ArdaNova economic decisions at the AZOA boundary.</summary>
public interface IAzoaSettlementGateway
{
    /// <summary>Submits one claimed settlement using its durable idempotency key.</summary>
    Task<AzoaSettlementGatewayResult> DispatchAsync(
        AzoaSettlementRequest request,
        CancellationToken ct = default);

    /// <summary>Looks up an ambiguous settlement without creating a new economic effect.</summary>
    Task<AzoaSettlementGatewayResult> ReconcileAsync(
        AzoaSettlementRequest request,
        CancellationToken ct = default);
}
