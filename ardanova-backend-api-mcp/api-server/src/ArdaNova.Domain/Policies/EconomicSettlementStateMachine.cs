namespace ArdaNova.Domain.Policies;

using ArdaNova.Domain.Models.Enums;

/// <summary>Defines legal local settlement transitions without treating provider acceptance as confirmation.</summary>
public static class EconomicSettlementStateMachine
{
    public static bool CanTransition(EconomicSettlementStatus current, EconomicSettlementStatus next) =>
        current switch
        {
            EconomicSettlementStatus.DRAFT => next is EconomicSettlementStatus.AUTHORIZED
                or EconomicSettlementStatus.REJECTED
                or EconomicSettlementStatus.CANCELLED,
            EconomicSettlementStatus.AUTHORIZED => next is EconomicSettlementStatus.PENDING_DISPATCH
                or EconomicSettlementStatus.REJECTED
                or EconomicSettlementStatus.CANCELLED
                or EconomicSettlementStatus.FAILED,
            EconomicSettlementStatus.PENDING_DISPATCH => next is EconomicSettlementStatus.SUBMITTED
                or EconomicSettlementStatus.AWAITING_RECONCILIATION
                or EconomicSettlementStatus.FAILED,
            EconomicSettlementStatus.SUBMITTED => next is EconomicSettlementStatus.CONFIRMED
                or EconomicSettlementStatus.AWAITING_RECONCILIATION
                or EconomicSettlementStatus.FAILED,
            EconomicSettlementStatus.AWAITING_RECONCILIATION => next is EconomicSettlementStatus.CONFIRMED
                or EconomicSettlementStatus.SUBMITTED
                or EconomicSettlementStatus.FAILED,
            _ => false,
        };
}
