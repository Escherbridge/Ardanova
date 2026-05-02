namespace ArdaNova.Application.Services.Interfaces;

using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Domain.Models.Enums;

public interface ITokenBalanceService
{
    Task<Result<TokenBalanceDto>> GetBalanceAsync(string userId, string projectTokenConfigId, TokenHolderClass holderClass, CancellationToken ct = default);
    Task<Result<TokenBalanceDto>> GetArdaBalanceAsync(string userId, CancellationToken ct = default);
    Task<Result<UserPortfolioDto>> GetPortfolioAsync(string userId, CancellationToken ct = default);

    Task<Result<TokenBalanceDto>> CreditAsync(string userId, string projectTokenConfigId, int amount, TokenHolderClass holderClass, CancellationToken ct = default);
    Task<Result<TokenBalanceDto>> DebitAsync(string userId, string projectTokenConfigId, int amount, TokenHolderClass holderClass, CancellationToken ct = default);

    Task<Result<TokenBalanceDto>> LockAsync(string userId, string projectTokenConfigId, int amount, TokenHolderClass holderClass, CancellationToken ct = default);
    Task<Result<TokenBalanceDto>> UnlockAsync(string userId, string projectTokenConfigId, int amount, TokenHolderClass holderClass, CancellationToken ct = default);

    Task<Result<bool>> IsBalanceLiquidAsync(string userId, string projectTokenConfigId, TokenHolderClass holderClass, CancellationToken ct = default);
}
