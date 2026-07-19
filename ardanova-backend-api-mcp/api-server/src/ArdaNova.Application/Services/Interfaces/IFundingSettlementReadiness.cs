namespace ArdaNova.Application.Services.Interfaces;

/// <summary>Gates payment collection on an enabled, selected-node settlement and reconciliation capability.</summary>
public interface IFundingSettlementReadiness
{
    /// <summary>True only when collecting payment can lead to an operable settlement path.</summary>
    bool IsReady { get; }

    /// <summary>Safe operator-facing reason when payment collection is unavailable.</summary>
    string UnavailableReason { get; }
}

/// <summary>Implemented only by a selected-node gateway that can dispatch and reconcile settlements.</summary>
public interface ISelectedNodeSettlementCapability
{
    /// <summary>The selected node identifier covered by this capability.</summary>
    string SelectedNodeId { get; }

    /// <summary>Whether the gateway can submit a durable settlement.</summary>
    bool CanDispatch { get; }

    /// <summary>Whether the gateway can reconcile an ambiguous settlement.</summary>
    bool CanReconcile { get; }

    /// <summary>Whether operator-reviewed capability attestation remains valid for this node.</summary>
    bool HasCurrentOperatorAttestation { get; }

    /// <summary>Whether the gateway accepts the complete immutable funding asset contract.</summary>
    bool SupportsCanonicalFundingSettlement { get; }
}

/// <summary>Describes whether the bounded hosted outbox loop was actually registered.</summary>
public interface ISettlementOutboxRuntimeCapability
{
    bool IsHostedDispatcherRegistered { get; }
}
