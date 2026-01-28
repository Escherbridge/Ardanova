namespace ArdaNova.Application.Services.Interfaces;

using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;

public interface ITokenSwapService
{
    Task<Result<TokenSwapDto>> GetByIdAsync(string id, CancellationToken ct = default);
    Task<Result<IReadOnlyList<TokenSwapDto>>> GetByUserIdAsync(string userId, CancellationToken ct = default);
    Task<Result<PagedResult<TokenSwapDto>>> GetByUserIdPagedAsync(string userId, int page, int pageSize, CancellationToken ct = default);
    Task<Result<TokenSwapDto>> CreateAsync(CreateTokenSwapDto dto, CancellationToken ct = default);
    Task<Result<TokenSwapDto>> StartProcessingAsync(string id, CancellationToken ct = default);
    Task<Result<TokenSwapDto>> CompleteAsync(string id, CompleteSwapDto dto, CancellationToken ct = default);
    Task<Result<TokenSwapDto>> FailAsync(string id, CancellationToken ct = default);
    Task<Result<TokenSwapDto>> CancelAsync(string id, CancellationToken ct = default);
}

public interface ILiquidityPoolService
{
    Task<Result<LiquidityPoolDto>> GetByIdAsync(string id, CancellationToken ct = default);
    Task<Result<IReadOnlyList<LiquidityPoolDto>>> GetAllAsync(CancellationToken ct = default);
    Task<Result<IReadOnlyList<LiquidityPoolDto>>> GetActivePoolsAsync(CancellationToken ct = default);
    Task<Result<LiquidityPoolDto>> GetByTokenPairAsync(string share1Id, string share2Id, CancellationToken ct = default);
    Task<Result<LiquidityPoolDto>> CreateAsync(CreateLiquidityPoolDto dto, CancellationToken ct = default);
    Task<Result<LiquidityPoolDto>> AddLiquidityAsync(string id, AddLiquidityDto dto, CancellationToken ct = default);
    Task<Result<LiquidityPoolDto>> RemoveLiquidityAsync(string id, RemoveLiquidityDto dto, CancellationToken ct = default);
    Task<Result<LiquidityPoolDto>> ActivateAsync(string id, CancellationToken ct = default);
    Task<Result<LiquidityPoolDto>> DeactivateAsync(string id, CancellationToken ct = default);
}

public interface ILiquidityProviderService
{
    Task<Result<LiquidityProviderDto>> GetByIdAsync(string id, CancellationToken ct = default);
    Task<Result<IReadOnlyList<LiquidityProviderDto>>> GetByPoolIdAsync(string poolId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<LiquidityProviderDto>>> GetByUserIdAsync(string userId, CancellationToken ct = default);
    Task<Result<LiquidityProviderDto>> GetByPoolAndUserAsync(string poolId, string userId, CancellationToken ct = default);
    Task<Result<LiquidityProviderDto>> CreateAsync(CreateLiquidityProviderDto dto, CancellationToken ct = default);
    Task<Result<LiquidityProviderDto>> AddLiquidityAsync(string id, decimal shares, decimal share1, decimal share2, CancellationToken ct = default);
    Task<Result<LiquidityProviderDto>> RemoveLiquidityAsync(string id, decimal shares, decimal share1, decimal share2, CancellationToken ct = default);
    Task<Result<bool>> DeleteAsync(string id, CancellationToken ct = default);
}
