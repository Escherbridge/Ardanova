namespace ArdaNova.Infrastructure.Azoa;

using ArdaNova.Application.Services.Interfaces;
using Microsoft.Extensions.Options;

/// <summary>Fail-closed sponsorship gate backed by the selected-node gateway capability.</summary>
/// <remarks>
/// Half of the checkout gate. This half answers "can this deployment settle a sponsorship at all";
/// the sponsor's own uniform readiness and the project's minted asset are checked per request in
/// <c>FundingIntentService</c>. Both must pass before an intent row exists.
/// <para>
/// <see cref="UnavailableReason"/> names the leg that actually failed. One constant for every
/// refusal told an operator only that something was off, which is how a missing attestation and an
/// unregistered outbox looked identical for the whole of Wave 5.
/// </para>
/// </remarks>
public sealed class AzoaFundingSettlementReadiness : IFundingSettlementReadiness
{
    /// <summary>What a sponsor is told. Operators read the specific leg; people read this.</summary>
    private const string ConsumerReason =
        "Sponsorship is not switched on for this deployment.";

    private readonly AzoaSettings _settings;
    private readonly IAzoaSettlementGateway _gateway;
    private readonly ISettlementOutboxRuntimeCapability _outboxRuntime;

    public AzoaFundingSettlementReadiness(
        IOptions<AzoaSettings> settings,
        IAzoaSettlementGateway gateway,
        ISettlementOutboxRuntimeCapability outboxRuntime)
    {
        _settings = settings.Value;
        _gateway = gateway;
        _outboxRuntime = outboxRuntime;
    }

    /// <inheritdoc/>
    public bool IsReady => FailingLeg() is null;

    /// <inheritdoc/>
    public string UnavailableReason
    {
        get
        {
            var leg = FailingLeg();
            return leg is null ? string.Empty : $"{ConsumerReason} ({leg})";
        }
    }

    /// <summary>Returns the first unmet requirement, or null when every leg holds.</summary>
    /// <remarks>
    /// Order matters only for the message. Every leg is a hard requirement: the operator opt-in,
    /// a registered outbox loop (nothing reconciles an ambiguous exchange without it), a selected
    /// node, and a gateway that declares - not merely happens to have - the matching capability.
    /// </remarks>
    private string? FailingLeg()
    {
        if (!_settings.EnableFundingCheckout)
            return "Azoa:EnableFundingCheckout is off";
        if (!_outboxRuntime.IsHostedDispatcherRegistered)
            return "the settlement outbox loop is not registered";
        if (string.IsNullOrWhiteSpace(_settings.SelectedSettlementNodeId))
            return "no settlement node is selected";
        if (_gateway is not ISelectedNodeSettlementCapability capability)
            return "the settlement gateway declares no node capability";
        if (!string.Equals(capability.SelectedNodeId, _settings.SelectedSettlementNodeId, StringComparison.Ordinal))
            return "the gateway serves a different node than the one selected";
        if (!capability.CanDispatch)
            return "the gateway cannot dispatch";
        if (!capability.CanReconcile)
            return "the gateway cannot reconcile";
        if (!capability.HasCurrentOperatorAttestation)
            return "the operator attestation has expired";
        return capability.SupportsCanonicalFundingSettlement
            ? null
            : "the gateway does not accept the exchange settlement contract";
    }
}
