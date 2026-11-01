// Generated from database-architecture.dbml by generate:csharp - do not edit; member notes go in Enums/AGENTS.md
namespace ArdaNova.Domain.Models.Enums;

public enum EconomicOutboxStatus
{
    PENDING,
    LEASED,
    SUBMITTED,
    AWAITING_RECONCILIATION,
    COMPLETED,
    FAILED,
    CANCELLED
}
