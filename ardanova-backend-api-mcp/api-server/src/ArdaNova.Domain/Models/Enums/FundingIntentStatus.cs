namespace ArdaNova.Domain.Models.Enums;

public enum FundingIntentStatus
{
    DRAFT,
    AWAITING_PAYMENT,
    PAYMENT_VERIFIED,
    SETTLEMENT_PENDING,
    SETTLED,
    REJECTED,
    CANCELLED,
    FAILED
}
