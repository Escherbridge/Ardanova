namespace ArdaNova.Domain.Models.Enums;

public enum EconomicSettlementKind
{
    TASK_REWARD,
    FUNDING_ALLOCATION,
    ESCROW_REFUND,
    SWAP_SETTLEMENT,

    /// <summary>A sponsor's ARDA exchanged for project units in one node operation.</summary>
    EXCHANGE,

    /// <summary>Units already held on the node released to their beneficiary when the window elapses.</summary>
    HOLD_RELEASE
}
