namespace ArdaNova.Domain.Models.Enums;

public enum WalletProvider
{
    PERA,
    DEFLY,
    ALGOSIGNER,
    WALLETCONNECT,
    OTHER
}

public enum SwapStatus
{
    PENDING,
    PROCESSING,
    COMPLETED,
    FAILED,
    CANCELLED
}
