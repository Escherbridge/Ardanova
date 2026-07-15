namespace ArdaNova.Application.Services.Interfaces;

using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;

public interface IWalletVerificationService
{
    Task<Result<WalletVerificationChallengeDto>> IssueAsync(
        string actorId,
        string walletId,
        CancellationToken ct = default);

    Task<Result<WalletDto>> CompleteAsync(
        string actorId,
        string walletId,
        CompleteWalletVerificationDto request,
        CancellationToken ct = default);
}
