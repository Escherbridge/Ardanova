namespace ArdaNova.Application.Services.Interfaces;

/// <summary>Gates sponsorship on a selected node that can actually settle and reconcile an exchange.</summary>
public interface IFundingSettlementReadiness
{
    /// <summary>True only when authorising a sponsorship can lead to an operable settlement path.</summary>
    bool IsReady { get; }

    /// <summary>Safe reason when sponsorship is unavailable; it names the leg that failed.</summary>
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

    /// <summary>
    /// Whether the gateway accepts the complete exchange settlement contract: a durable idempotency
    /// key, canonical token base units at a fixed scale, and the immutable terms the sponsor agreed
    /// to. A gateway that settles a currency amount does not satisfy it.
    /// </summary>
    bool SupportsCanonicalFundingSettlement { get; }
}

/// <summary>Describes whether the bounded hosted outbox loop was actually registered.</summary>
public interface ISettlementOutboxRuntimeCapability
{
    bool IsHostedDispatcherRegistered { get; }
}
