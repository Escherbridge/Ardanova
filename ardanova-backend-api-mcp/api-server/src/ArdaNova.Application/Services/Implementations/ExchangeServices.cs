namespace ArdaNova.Application.Services.Implementations;

using ArdaNova.Application.Common.Interfaces;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Interfaces;
using ArdaNova.Domain.Models.Entities;
using ArdaNova.Domain.Models.Enums;
using AutoMapper;

public class TokenSwapService : ITokenSwapService
{
    private readonly IRepository<ShareSwap> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public TokenSwapService(IRepository<ShareSwap> repository, IUnitOfWork unitOfWork, IMapper mapper)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<Result<TokenSwapDto>> GetByIdAsync(string id, CancellationToken ct = default)
    {
        var swap = await _repository.GetByIdAsync(id, ct);
        if (swap is null)
            return Result<TokenSwapDto>.NotFound($"TokenSwap with id {id} not found");
        return Result<TokenSwapDto>.Success(_mapper.Map<TokenSwapDto>(swap));
    }

    public async Task<Result<IReadOnlyList<TokenSwapDto>>> GetByUserIdAsync(string userId, CancellationToken ct = default)
    {
        var swaps = await _repository.FindAsync(s => s.userId == userId, ct);
        var ordered = swaps.OrderByDescending(s => s.createdAt).ToList();
        return Result<IReadOnlyList<TokenSwapDto>>.Success(_mapper.Map<IReadOnlyList<TokenSwapDto>>(ordered));
    }

    public async Task<Result<PagedResult<TokenSwapDto>>> GetByUserIdPagedAsync(string userId, int page, int pageSize, CancellationToken ct = default)
    {
        var result = await _repository.GetPagedAsync(page, pageSize, s => s.userId == userId, ct);
        return Result<PagedResult<TokenSwapDto>>.Success(result.Map(_mapper.Map<TokenSwapDto>));
    }

    public async Task<Result<TokenSwapDto>> CreateAsync(CreateTokenSwapDto dto, CancellationToken ct = default)
    {
        var swap = new ShareSwap
        {
            id = Guid.NewGuid().ToString(),
            userId = dto.UserId,
            fromShareId = dto.FromShareId,
            toShareId = dto.ToShareId,
            fromAmount = dto.FromAmount,
            toAmount = dto.ToAmount,
            exchangeRate = dto.ExchangeRate,
            fee = dto.Fee,
            status = SwapStatus.PENDING,
            createdAt = DateTime.UtcNow
        };

        await _repository.AddAsync(swap, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<TokenSwapDto>.Success(_mapper.Map<TokenSwapDto>(swap));
    }

    public async Task<Result<TokenSwapDto>> StartProcessingAsync(string id, CancellationToken ct = default)
    {
        var swap = await _repository.GetByIdAsync(id, ct);
        if (swap is null)
            return Result<TokenSwapDto>.NotFound($"TokenSwap with id {id} not found");

        if (swap.status != SwapStatus.PENDING)
            return Result<TokenSwapDto>.ValidationError($"Cannot process swap in status {swap.status}");

        swap.status = SwapStatus.PROCESSING;
        await _repository.UpdateAsync(swap, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<TokenSwapDto>.Success(_mapper.Map<TokenSwapDto>(swap));
    }

    public async Task<Result<TokenSwapDto>> CompleteAsync(string id, CompleteSwapDto dto, CancellationToken ct = default)
    {
        var swap = await _repository.GetByIdAsync(id, ct);
        if (swap is null)
            return Result<TokenSwapDto>.NotFound($"TokenSwap with id {id} not found");

        if (swap.status != SwapStatus.PROCESSING && swap.status != SwapStatus.PENDING)
            return Result<TokenSwapDto>.ValidationError($"Cannot complete swap in status {swap.status}");

        swap.status = SwapStatus.COMPLETED;
        swap.txHash = dto.TxHash;
        swap.completedAt = DateTime.UtcNow;
        await _repository.UpdateAsync(swap, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<TokenSwapDto>.Success(_mapper.Map<TokenSwapDto>(swap));
    }

    public async Task<Result<TokenSwapDto>> FailAsync(string id, CancellationToken ct = default)
    {
        var swap = await _repository.GetByIdAsync(id, ct);
        if (swap is null)
            return Result<TokenSwapDto>.NotFound($"TokenSwap with id {id} not found");

        swap.status = SwapStatus.FAILED;
        await _repository.UpdateAsync(swap, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<TokenSwapDto>.Success(_mapper.Map<TokenSwapDto>(swap));
    }

    public async Task<Result<TokenSwapDto>> CancelAsync(string id, CancellationToken ct = default)
    {
        var swap = await _repository.GetByIdAsync(id, ct);
        if (swap is null)
            return Result<TokenSwapDto>.NotFound($"TokenSwap with id {id} not found");

        if (swap.status != SwapStatus.PENDING)
            return Result<TokenSwapDto>.ValidationError($"Can only cancel pending swaps");

        swap.status = SwapStatus.CANCELLED;
        await _repository.UpdateAsync(swap, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<TokenSwapDto>.Success(_mapper.Map<TokenSwapDto>(swap));
    }
}

public class LiquidityPoolService : ILiquidityPoolService
{
    private readonly IRepository<LiquidityPool> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public LiquidityPoolService(IRepository<LiquidityPool> repository, IUnitOfWork unitOfWork, IMapper mapper)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<Result<LiquidityPoolDto>> GetByIdAsync(string id, CancellationToken ct = default)
    {
        var pool = await _repository.GetByIdAsync(id, ct);
        if (pool is null)
            return Result<LiquidityPoolDto>.NotFound($"LiquidityPool with id {id} not found");
        return Result<LiquidityPoolDto>.Success(_mapper.Map<LiquidityPoolDto>(pool));
    }

    public async Task<Result<IReadOnlyList<LiquidityPoolDto>>> GetAllAsync(CancellationToken ct = default)
    {
        var pools = await _repository.GetAllAsync(ct);
        return Result<IReadOnlyList<LiquidityPoolDto>>.Success(_mapper.Map<IReadOnlyList<LiquidityPoolDto>>(pools));
    }

    public async Task<Result<IReadOnlyList<LiquidityPoolDto>>> GetActivePoolsAsync(CancellationToken ct = default)
    {
        var pools = await _repository.FindAsync(p => p.isActive, ct);
        return Result<IReadOnlyList<LiquidityPoolDto>>.Success(_mapper.Map<IReadOnlyList<LiquidityPoolDto>>(pools));
    }

    public async Task<Result<LiquidityPoolDto>> GetByTokenPairAsync(string token1Id, string token2Id, CancellationToken ct = default)
    {
        var pool = await _repository.FindOneAsync(p =>
            (p.token1Id == token1Id && p.token2Id == token2Id) ||
            (p.token1Id == token2Id && p.token2Id == token1Id), ct);

        if (pool is null)
            return Result<LiquidityPoolDto>.NotFound($"LiquidityPool for token pair not found");
        return Result<LiquidityPoolDto>.Success(_mapper.Map<LiquidityPoolDto>(pool));
    }

    public async Task<Result<LiquidityPoolDto>> CreateAsync(CreateLiquidityPoolDto dto, CancellationToken ct = default)
    {
        var exists = await _repository.ExistsAsync(p =>
            (p.token1Id == dto.Token1Id && p.token2Id == dto.Token2Id) ||
            (p.token1Id == dto.Token2Id && p.token2Id == dto.Token1Id), ct);

        if (exists)
            return Result<LiquidityPoolDto>.ValidationError("Pool already exists for this token pair");

        var pool = new LiquidityPool
        {
            id = Guid.NewGuid().ToString(),
            share1Id = dto.Share1Id,
            share2Id = dto.Share2Id,
            reserve1 = 0,
            reserve2 = 0,
            totalShares = 0,
            feePercent = dto.FeePercent,
            isActive = true,
            createdAt = DateTime.UtcNow,
            updatedAt = DateTime.UtcNow
        };

        await _repository.AddAsync(pool, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<LiquidityPoolDto>.Success(_mapper.Map<LiquidityPoolDto>(pool));
    }

    public async Task<Result<LiquidityPoolDto>> AddLiquidityAsync(string id, AddLiquidityDto dto, CancellationToken ct = default)
    {
        var pool = await _repository.GetByIdAsync(id, ct);
        if (pool is null)
            return Result<LiquidityPoolDto>.NotFound($"LiquidityPool with id {id} not found");

        pool.reserve1 += dto.Amount1;
        pool.reserve2 += dto.Amount2;
        pool.totalShares += dto.Shares;
        pool.updatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(pool, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<LiquidityPoolDto>.Success(_mapper.Map<LiquidityPoolDto>(pool));
    }

    public async Task<Result<LiquidityPoolDto>> RemoveLiquidityAsync(string id, RemoveLiquidityDto dto, CancellationToken ct = default)
    {
        var pool = await _repository.GetByIdAsync(id, ct);
        if (pool is null)
            return Result<LiquidityPoolDto>.NotFound($"LiquidityPool with id {id} not found");

        pool.reserve1 -= dto.Amount1;
        pool.reserve2 -= dto.Amount2;
        pool.totalShares -= dto.Shares;
        pool.updatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(pool, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<LiquidityPoolDto>.Success(_mapper.Map<LiquidityPoolDto>(pool));
    }

    public async Task<Result<LiquidityPoolDto>> ActivateAsync(string id, CancellationToken ct = default)
    {
        var pool = await _repository.GetByIdAsync(id, ct);
        if (pool is null)
            return Result<LiquidityPoolDto>.NotFound($"LiquidityPool with id {id} not found");

        pool.isActive = true;
        pool.updatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(pool, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<LiquidityPoolDto>.Success(_mapper.Map<LiquidityPoolDto>(pool));
    }

    public async Task<Result<LiquidityPoolDto>> DeactivateAsync(string id, CancellationToken ct = default)
    {
        var pool = await _repository.GetByIdAsync(id, ct);
        if (pool is null)
            return Result<LiquidityPoolDto>.NotFound($"LiquidityPool with id {id} not found");

        pool.isActive = false;
        pool.updatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(pool, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<LiquidityPoolDto>.Success(_mapper.Map<LiquidityPoolDto>(pool));
    }
}

public class LiquidityProviderService : ILiquidityProviderService
{
    private readonly IRepository<LiquidityProvider> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public LiquidityProviderService(IRepository<LiquidityProvider> repository, IUnitOfWork unitOfWork, IMapper mapper)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<Result<LiquidityProviderDto>> GetByIdAsync(string id, CancellationToken ct = default)
    {
        var provider = await _repository.GetByIdAsync(id, ct);
        if (provider is null)
            return Result<LiquidityProviderDto>.NotFound($"LiquidityProvider with id {id} not found");
        return Result<LiquidityProviderDto>.Success(_mapper.Map<LiquidityProviderDto>(provider));
    }

    public async Task<Result<IReadOnlyList<LiquidityProviderDto>>> GetByPoolIdAsync(string poolId, CancellationToken ct = default)
    {
        var providers = await _repository.FindAsync(p => p.poolId == poolId, ct);
        return Result<IReadOnlyList<LiquidityProviderDto>>.Success(_mapper.Map<IReadOnlyList<LiquidityProviderDto>>(providers));
    }

    public async Task<Result<IReadOnlyList<LiquidityProviderDto>>> GetByUserIdAsync(string userId, CancellationToken ct = default)
    {
        var providers = await _repository.FindAsync(p => p.userId == userId, ct);
        return Result<IReadOnlyList<LiquidityProviderDto>>.Success(_mapper.Map<IReadOnlyList<LiquidityProviderDto>>(providers));
    }

    public async Task<Result<LiquidityProviderDto>> GetByPoolAndUserAsync(string poolId, string userId, CancellationToken ct = default)
    {
        var provider = await _repository.FindOneAsync(p => p.poolId == poolId && p.userId == userId, ct);
        if (provider is null)
            return Result<LiquidityProviderDto>.NotFound($"LiquidityProvider not found for pool {poolId} and user {userId}");
        return Result<LiquidityProviderDto>.Success(_mapper.Map<LiquidityProviderDto>(provider));
    }

    public async Task<Result<LiquidityProviderDto>> CreateAsync(CreateLiquidityProviderDto dto, CancellationToken ct = default)
    {
        var exists = await _repository.ExistsAsync(p => p.poolId == dto.PoolId && p.userId == dto.UserId, ct);
        if (exists)
            return Result<LiquidityProviderDto>.ValidationError("Provider position already exists for this pool and user");

        var provider = new LiquidityProvider
        {
            id = Guid.NewGuid().ToString(),
            poolId = dto.PoolId,
            userId = dto.UserId,
            shares = dto.Shares,
            share1In = dto.Share1In,
            share2In = dto.Share2In,
            createdAt = DateTime.UtcNow,
            updatedAt = DateTime.UtcNow
        };

        await _repository.AddAsync(provider, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<LiquidityProviderDto>.Success(_mapper.Map<LiquidityProviderDto>(provider));
    }

    public async Task<Result<LiquidityProviderDto>> AddLiquidityAsync(string id, decimal shares, decimal share1, decimal share2, CancellationToken ct = default)
    {
        var provider = await _repository.GetByIdAsync(id, ct);
        if (provider is null)
            return Result<LiquidityProviderDto>.NotFound($"LiquidityProvider with id {id} not found");

        provider.shares += shares;
        provider.share1In += share1;
        provider.share2In += share2;
        provider.updatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(provider, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<LiquidityProviderDto>.Success(_mapper.Map<LiquidityProviderDto>(provider));
    }

    public async Task<Result<LiquidityProviderDto>> RemoveLiquidityAsync(string id, decimal shares, decimal share1, decimal share2, CancellationToken ct = default)
    {
        var provider = await _repository.GetByIdAsync(id, ct);
        if (provider is null)
            return Result<LiquidityProviderDto>.NotFound($"LiquidityProvider with id {id} not found");

        provider.shares -= shares;
        provider.share1In -= share1;
        provider.share2In -= share2;
        provider.updatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(provider, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<LiquidityProviderDto>.Success(_mapper.Map<LiquidityProviderDto>(provider));
    }

    public async Task<Result<bool>> DeleteAsync(string id, CancellationToken ct = default)
    {
        var provider = await _repository.GetByIdAsync(id, ct);
        if (provider is null)
            return Result<bool>.NotFound($"LiquidityProvider with id {id} not found");

        await _repository.DeleteAsync(provider, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<bool>.Success(true);
    }
}
