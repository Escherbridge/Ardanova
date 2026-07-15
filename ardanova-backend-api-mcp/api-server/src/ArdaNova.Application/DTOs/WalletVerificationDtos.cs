namespace ArdaNova.Application.DTOs;

public sealed record WalletVerificationChallengeDto
{
    public string ChallengeId { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public string Chain { get; init; } = string.Empty;
    public string Network { get; init; } = string.Empty;
    public DateTime ExpiresAt { get; init; }
}

public sealed record CompleteWalletVerificationDto
{
    public string ChallengeId { get; init; } = string.Empty;
    public string Nonce { get; init; } = string.Empty;
    public string Signature { get; init; } = string.Empty;
}
