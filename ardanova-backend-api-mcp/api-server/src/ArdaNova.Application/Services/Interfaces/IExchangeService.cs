namespace ArdaNova.Application.Services.Interfaces;

using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;

public interface ITokenSwapService
{
    Task<Result<TokenSwapDto>> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Result<IReadOnlyList<TokenSwapDto>>> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<Result<PagedResult<TokenSwapDto>>> GetByUserIdPagedAsync(Guid userId, int page, int pageSize, CancellationToken ct = default);
    Task<Result<TokenSwapDto>> CreateAsync(CreateTokenSwapDto dto, CancellationToken ct = default);
    Task<Result<TokenSwapDto>> StartProcessingAsync(Guid id, CancellationToken ct = default);
    Task<Result<TokenSwapDto>> CompleteAsync(Guid id, CompleteSwapDto dto, CancellationToken ct = default);
    Task<Result<TokenSwapDto>> FailAsync(Guid id, CancellationToken ct = default);
    Task<Result<TokenSwapDto>> CancelAsync(Guid id, CancellationToken ct = default);
}

public interface ILiquidityPoolService
{
    Task<Result<LiquidityPoolDto>> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Result<IReadOnlyList<LiquidityPoolDto>>> GetAllAsync(CancellationToken ct = default);
    Task<Result<IReadOnlyList<LiquidityPoolDto>>> GetActivePoolsAsync(CancellationToken ct = default);
    Task<Result<LiquidityPoolDto>> GetByTokenPairAsync(Guid token1Id, Guid token2Id, CancellationToken ct = default);
    Task<Result<LiquidityPoolDto>> CreateAsync(CreateLiquidityPoolDto dto, CancellationToken ct = default);
    Task<Result<LiquidityPoolDto>> AddLiquidityAsync(Guid id, AddLiquidityDto dto, CancellationToken ct = default);
    Task<Result<LiquidityPoolDto>> RemoveLiquidityAsync(Guid id, RemoveLiquidityDto dto, CancellationToken ct = default);
    Task<Result<LiquidityPoolDto>> ActivateAsync(Guid id, CancellationToken ct = default);
    Task<Result<LiquidityPoolDto>> DeactivateAsync(Guid id, CancellationToken ct = default);
}

public interface ILiquidityProviderService
{
    Task<Result<LiquidityProviderDto>> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Result<IReadOnlyList<LiquidityProviderDto>>> GetByPoolIdAsync(Guid poolId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<LiquidityProviderDto>>> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<Result<LiquidityProviderDto>> GetByPoolAndUserAsync(Guid poolId, Guid userId, CancellationToken ct = default);
    Task<Result<LiquidityProviderDto>> CreateAsync(CreateLiquidityProviderDto dto, CancellationToken ct = default);
    Task<Result<LiquidityProviderDto>> AddLiquidityAsync(Guid id, decimal shares, decimal token1, decimal token2, CancellationToken ct = default);
    Task<Result<LiquidityProviderDto>> RemoveLiquidityAsync(Guid id, decimal shares, decimal token1, decimal token2, CancellationToken ct = default);
    Task<Result<bool>> DeleteAsync(Guid id, CancellationToken ct = default);
}
