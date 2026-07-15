namespace ArdaNova.Application.Tests.Services;

using ArdaNova.Application.Services.Interfaces;
using ArdaNova.Infrastructure.Azoa;
using FluentAssertions;
using Microsoft.Extensions.Options;

public class AzoaFundingSettlementReadinessTests
{
    [Fact]
    public void DisabledGateway_CannotEnableFundingCheckoutEvenWhenConfigurationIsSet()
    {
        var settings = Options.Create(new AzoaSettings
        {
            EnableFundingCheckout = true,
            SelectedSettlementNodeId = "node-1",
        });

        var readiness = new AzoaFundingSettlementReadiness(
            settings,
            new DisabledAzoaSettlementGateway());

        readiness.IsReady.Should().BeFalse();
    }

    [Fact]
    public void Readiness_RequiresExplicitEnablementMatchingNodeAndCurrentCapabilities()
    {
        var settings = Options.Create(new AzoaSettings
        {
            EnableFundingCheckout = true,
            SelectedSettlementNodeId = "node-1",
        });
        var capability = new CapableGateway("node-1", canDispatch: true, canReconcile: true, attested: true);

        new AzoaFundingSettlementReadiness(settings, capability).IsReady.Should().BeTrue();

        capability.HasCurrentOperatorAttestation = false;
        new AzoaFundingSettlementReadiness(settings, capability).IsReady.Should().BeFalse();

        capability.HasCurrentOperatorAttestation = true;
        capability.SelectedNodeId = "node-other";
        new AzoaFundingSettlementReadiness(settings, capability).IsReady.Should().BeFalse();
    }

    private sealed class CapableGateway : IAzoaSettlementGateway, ISelectedNodeSettlementCapability
    {
        public CapableGateway(string selectedNodeId, bool canDispatch, bool canReconcile, bool attested)
        {
            SelectedNodeId = selectedNodeId;
            CanDispatch = canDispatch;
            CanReconcile = canReconcile;
            HasCurrentOperatorAttestation = attested;
        }

        public string SelectedNodeId { get; set; }
        public bool CanDispatch { get; }
        public bool CanReconcile { get; }
        public bool HasCurrentOperatorAttestation { get; set; }

        public Task<AzoaSettlementGatewayResult> DispatchAsync(
            AzoaSettlementRequest request,
            CancellationToken ct = default)
            => Task.FromResult(AzoaSettlementGatewayResult.Retry("TEST", "Test gateway does not dispatch."));

        public Task<AzoaSettlementGatewayResult> ReconcileAsync(
            AzoaSettlementRequest request,
            CancellationToken ct = default)
            => Task.FromResult(AzoaSettlementGatewayResult.Retry("TEST", "Test gateway does not reconcile."));
    }
}
