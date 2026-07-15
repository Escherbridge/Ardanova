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
