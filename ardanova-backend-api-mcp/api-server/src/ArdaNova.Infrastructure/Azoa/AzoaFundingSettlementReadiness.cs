namespace ArdaNova.Infrastructure.Azoa;

using ArdaNova.Application.Services.Interfaces;
using Microsoft.Extensions.Options;

/// <summary>Fail-closed funding gate backed by the selected-node gateway capability.</summary>
public sealed class AzoaFundingSettlementReadiness : IFundingSettlementReadiness
{
    private const string DisabledReason = "Funding checkout is unavailable until a selected AZOA node has enabled, attested settlement dispatch and reconciliation.";

    private readonly AzoaSettings _settings;
    private readonly IAzoaSettlementGateway _gateway;

    public AzoaFundingSettlementReadiness(
        IOptions<AzoaSettings> settings,
        IAzoaSettlementGateway gateway)
    {
        _settings = settings.Value;
        _gateway = gateway;
    }

    /// <inheritdoc/>
    public bool IsReady
        => _settings.EnableFundingCheckout
            && !string.IsNullOrWhiteSpace(_settings.SelectedSettlementNodeId)
            && _gateway is ISelectedNodeSettlementCapability capability
            && string.Equals(
                capability.SelectedNodeId,
                _settings.SelectedSettlementNodeId,
                StringComparison.Ordinal)
            && capability.CanDispatch
            && capability.CanReconcile
            && capability.HasCurrentOperatorAttestation;

    /// <inheritdoc/>
    public string UnavailableReason => DisabledReason;
}
