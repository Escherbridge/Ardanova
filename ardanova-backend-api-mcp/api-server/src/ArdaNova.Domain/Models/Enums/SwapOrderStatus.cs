namespace ArdaNova.Domain.Models.Enums;

public enum SwapOrderStatus
{
    DRAFT,
    AUTHORIZED,
    PENDING_SETTLEMENT,
    CONFIRMED,
    REJECTED,
    CANCELLED,
    FAILED
}
