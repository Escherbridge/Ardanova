// Generated from database-architecture.dbml by generate:csharp - do not edit; member notes go in Enums/AGENTS.md
namespace ArdaNova.Domain.Models.Enums;

public enum EconomicSettlementStatus
{
    DRAFT,
    AUTHORIZED,
    PENDING_DISPATCH,
    SUBMITTED,
    AWAITING_RECONCILIATION,
    CONFIRMED,
    REJECTED,
    CANCELLED,
    FAILED
}
