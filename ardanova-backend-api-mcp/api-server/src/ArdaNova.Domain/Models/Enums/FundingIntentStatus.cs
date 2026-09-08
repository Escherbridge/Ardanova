namespace ArdaNova.Domain.Models.Enums;

/// <summary>Lifecycle of one sponsorship: platform units in, project units out.</summary>
/// <remarks>
/// There is no card-payment state here any more. A sponsor already holds platform units when
/// they press Support, so the intent goes straight from DRAFT to the node. See
/// <c>src/ArdaNova.Application/Services/Implementations/AGENTS.md</c> section sponsorship.
/// </remarks>
public enum FundingIntentStatus
{
    /// <summary>Locked terms committed; nothing has been asked of the node yet.</summary>
    DRAFT,

    /// <summary>Sent to the node, which has not yet given a durable answer.</summary>
    SETTLEMENT_PENDING,

    /// <summary>The node moved the value and ArdaNova recorded the stake.</summary>
    SETTLED,

    /// <summary>
    /// Readiness lapsed between the click and the confirmation, so nothing was taken and the
    /// sponsor keeps their platform units. Terminal: a lapse must never become an endless retry.
    /// </summary>
    HOLDING,

    /// <summary>Refused by policy before any node call.</summary>
    REJECTED,

    /// <summary>Withdrawn by the sponsor before settlement.</summary>
    CANCELLED,

    /// <summary>The node refused, or the platform gave up; no value moved.</summary>
    FAILED
}
