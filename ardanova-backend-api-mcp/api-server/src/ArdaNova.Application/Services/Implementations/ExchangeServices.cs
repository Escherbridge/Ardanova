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
    private readonly IRepository<TokenSwap> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public TokenSwapService(IRepository<TokenSwap> repository, IUnitOfWork unitOfWork, IMapper mapper)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<Result<TokenSwapDto>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var swap = await _repository.GetByIdAsync(id, ct);
        if (swap is null)
            return Result<TokenSwapDto>.NotFound($"TokenSwap with id {id} not found");
        return Result<TokenSwapDto>.Success(_mapper.Map<TokenSwapDto>(swap));
    }

    public async Task<Result<IReadOnlyList<TokenSwapDto>>> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
    {
        var swaps = await _repository.FindAsync(s => s.UserId == userId, ct);
        var ordered = swaps.OrderByDescending(s => s.CreatedAt).ToList();
        return Result<IReadOnlyList<TokenSwapDto>>.Success(_mapper.Map<IReadOnlyList<TokenSwapDto>>(ordered));
    }

    public async Task<Result<PagedResult<TokenSwapDto>>> GetByUserIdPagedAsync(Guid userId, int page, int pageSize, CancellationToken ct = default)
    {
        var result = await _repository.GetPagedAsync(page, pageSize, s => s.UserId == userId, ct);
        return Result<PagedResult<TokenSwapDto>>.Success(result.Map(_mapper.Map<TokenSwapDto>));
    }

    public async Task<Result<TokenSwapDto>> CreateAsync(CreateTokenSwapDto dto, CancellationToken ct = default)
    {
        var swap = TokenSwap.Create(
            dto.UserId,
            dto.FromTokenId,
            dto.ToTokenId,
            dto.FromAmount,
            dto.ToAmount,
            dto.ExchangeRate,
            dto.Fee
        );

        await _repository.AddAsync(swap, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<TokenSwapDto>.Success(_mapper.Map<TokenSwapDto>(swap));
    }

    public async Task<Result<TokenSwapDto>> StartProcessingAsync(Guid id, CancellationToken ct = default)
    {
        var swap = await _repository.GetByIdAsync(id, ct);
        if (swap is null)
            return Result<TokenSwapDto>.NotFound($"TokenSwap with id {id} not found");

        if (swap.Status != SwapStatus.PENDING)
            return Result<TokenSwapDto>.ValidationError($"Cannot process swap in status {swap.Status}");

        swap.StartProcessing();
        await _repository.UpdateAsync(swap, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<TokenSwapDto>.Success(_mapper.Map<TokenSwapDto>(swap));
    }

    public async Task<Result<TokenSwapDto>> CompleteAsync(Guid id, CompleteSwapDto dto, CancellationToken ct = default)
    {
        var swap = await _repository.GetByIdAsync(id, ct);
        if (swap is null)
            return Result<TokenSwapDto>.NotFound($"TokenSwap with id {id} not found");

        if (swap.Status != SwapStatus.PROCESSING && swap.Status != SwapStatus.PENDING)
            return Result<TokenSwapDto>.ValidationError($"Cannot complete swap in status {swap.Status}");

        swap.Complete(dto.TxHash);
        await _repository.UpdateAsync(swap, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<TokenSwapDto>.Success(_mapper.Map<TokenSwapDto>(swap));
    }

    public async Task<Result<TokenSwapDto>> FailAsync(Guid id, CancellationToken ct = default)
    {
        var swap = await _repository.GetByIdAsync(id, ct);
        if (swap is null)
            return Result<TokenSwapDto>.NotFound($"TokenSwap with id {id} not found");

        swap.Fail();
        await _repository.UpdateAsync(swap, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<TokenSwapDto>.Success(_mapper.Map<TokenSwapDto>(swap));
    }

    public async Task<Result<TokenSwapDto>> CancelAsync(Guid id, CancellationToken ct = default)
    {
        var swap = await _repository.GetByIdAsync(id, ct);
        if (swap is null)
            return Result<TokenSwapDto>.NotFound($"TokenSwap with id {id} not found");

        if (swap.Status != SwapStatus.PENDING)
            return Result<TokenSwapDto>.ValidationError($"Can only cancel pending swaps");

        swap.Cancel();
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

    public async Task<Result<LiquidityPoolDto>> GetByIdAsync(Guid id, CancellationToken ct = default)
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
        var pools = await _repository.FindAsync(p => p.IsActive, ct);
        return Result<IReadOnlyList<LiquidityPoolDto>>.Success(_mapper.Map<IReadOnlyList<LiquidityPoolDto>>(pools));
    }

    public async Task<Result<LiquidityPoolDto>> GetByTokenPairAsync(Guid token1Id, Guid token2Id, CancellationToken ct = default)
    {
        var pool = await _repository.FindOneAsync(p =>
            (p.Token1Id == token1Id && p.Token2Id == token2Id) ||
            (p.Token1Id == token2Id && p.Token2Id == token1Id), ct);

        if (pool is null)
            return Result<LiquidityPoolDto>.NotFound($"LiquidityPool for token pair not found");
        return Result<LiquidityPoolDto>.Success(_mapper.Map<LiquidityPoolDto>(pool));
    }

    public async Task<Result<LiquidityPoolDto>> CreateAsync(CreateLiquidityPoolDto dto, CancellationToken ct = default)
    {
        var exists = await _repository.ExistsAsync(p =>
            (p.Token1Id == dto.Token1Id && p.Token2Id == dto.Token2Id) ||
            (p.Token1Id == dto.Token2Id && p.Token2Id == dto.Token1Id), ct);

        if (exists)
            return Result<LiquidityPoolDto>.ValidationError("Pool already exists for this token pair");

        var pool = LiquidityPool.Create(dto.Token1Id, dto.Token2Id, dto.FeePercent);
        await _repository.AddAsync(pool, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<LiquidityPoolDto>.Success(_mapper.Map<LiquidityPoolDto>(pool));
    }

    public async Task<Result<LiquidityPoolDto>> AddLiquidityAsync(Guid id, AddLiquidityDto dto, CancellationToken ct = default)
    {
        var pool = await _repository.GetByIdAsync(id, ct);
        if (pool is null)
            return Result<LiquidityPoolDto>.NotFound($"LiquidityPool with id {id} not found");

        pool.AddLiquidity(dto.Amount1, dto.Amount2, dto.Shares);
        await _repository.UpdateAsync(pool, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<LiquidityPoolDto>.Success(_mapper.Map<LiquidityPoolDto>(pool));
    }

    public async Task<Result<LiquidityPoolDto>> RemoveLiquidityAsync(Guid id, RemoveLiquidityDto dto, CancellationToken ct = default)
    {
        var pool = await _repository.GetByIdAsync(id, ct);
        if (pool is null)
            return Result<LiquidityPoolDto>.NotFound($"LiquidityPool with id {id} not found");

        pool.RemoveLiquidity(dto.Amount1, dto.Amount2, dto.Shares);
        await _repository.UpdateAsync(pool, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<LiquidityPoolDto>.Success(_mapper.Map<LiquidityPoolDto>(pool));
    }

    public async Task<Result<LiquidityPoolDto>> ActivateAsync(Guid id, CancellationToken ct = default)
    {
        var pool = await _repository.GetByIdAsync(id, ct);
        if (pool is null)
            return Result<LiquidityPoolDto>.NotFound($"LiquidityPool with id {id} not found");

        pool.Activate();
        await _repository.UpdateAsync(pool, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<LiquidityPoolDto>.Success(_mapper.Map<LiquidityPoolDto>(pool));
    }

    public async Task<Result<LiquidityPoolDto>> DeactivateAsync(Guid id, CancellationToken ct = default)
    {
        var pool = await _repository.GetByIdAsync(id, ct);
        if (pool is null)
            return Result<LiquidityPoolDto>.NotFound($"LiquidityPool with id {id} not found");

        pool.Deactivate();
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

    public async Task<Result<LiquidityProviderDto>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var provider = await _repository.GetByIdAsync(id, ct);
        if (provider is null)
            return Result<LiquidityProviderDto>.NotFound($"LiquidityProvider with id {id} not found");
        return Result<LiquidityProviderDto>.Success(_mapper.Map<LiquidityProviderDto>(provider));
    }

    public async Task<Result<IReadOnlyList<LiquidityProviderDto>>> GetByPoolIdAsync(Guid poolId, CancellationToken ct = default)
    {
        var providers = await _repository.FindAsync(p => p.PoolId == poolId, ct);
        return Result<IReadOnlyList<LiquidityProviderDto>>.Success(_mapper.Map<IReadOnlyList<LiquidityProviderDto>>(providers));
    }

    public async Task<Result<IReadOnlyList<LiquidityProviderDto>>> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
    {
        var providers = await _repository.FindAsync(p => p.UserId == userId, ct);
        return Result<IReadOnlyList<LiquidityProviderDto>>.Success(_mapper.Map<IReadOnlyList<LiquidityProviderDto>>(providers));
    }

    public async Task<Result<LiquidityProviderDto>> GetByPoolAndUserAsync(Guid poolId, Guid userId, CancellationToken ct = default)
    {
        var provider = await _repository.FindOneAsync(p => p.PoolId == poolId && p.UserId == userId, ct);
        if (provider is null)
            return Result<LiquidityProviderDto>.NotFound($"LiquidityProvider not found for pool {poolId} and user {userId}");
        return Result<LiquidityProviderDto>.Success(_mapper.Map<LiquidityProviderDto>(provider));
    }

    public async Task<Result<LiquidityProviderDto>> CreateAsync(CreateLiquidityProviderDto dto, CancellationToken ct = default)
    {
        var exists = await _repository.ExistsAsync(p => p.PoolId == dto.PoolId && p.UserId == dto.UserId, ct);
        if (exists)
            return Result<LiquidityProviderDto>.ValidationError("Provider position already exists for this pool and user");

        var provider = LiquidityProvider.Create(dto.PoolId, dto.UserId, dto.Shares, dto.Token1In, dto.Token2In);
        await _repository.AddAsync(provider, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<LiquidityProviderDto>.Success(_mapper.Map<LiquidityProviderDto>(provider));
    }

    public async Task<Result<LiquidityProviderDto>> AddLiquidityAsync(Guid id, decimal shares, decimal token1, decimal token2, CancellationToken ct = default)
    {
        var provider = await _repository.GetByIdAsync(id, ct);
        if (provider is null)
            return Result<LiquidityProviderDto>.NotFound($"LiquidityProvider with id {id} not found");

        provider.AddLiquidity(shares, token1, token2);
        await _repository.UpdateAsync(provider, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<LiquidityProviderDto>.Success(_mapper.Map<LiquidityProviderDto>(provider));
    }

    public async Task<Result<LiquidityProviderDto>> RemoveLiquidityAsync(Guid id, decimal shares, decimal token1, decimal token2, CancellationToken ct = default)
    {
        var provider = await _repository.GetByIdAsync(id, ct);
        if (provider is null)
            return Result<LiquidityProviderDto>.NotFound($"LiquidityProvider with id {id} not found");

        provider.RemoveLiquidity(shares, token1, token2);
        await _repository.UpdateAsync(provider, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<LiquidityProviderDto>.Success(_mapper.Map<LiquidityProviderDto>(provider));
    }

    public async Task<Result<bool>> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var provider = await _repository.GetByIdAsync(id, ct);
        if (provider is null)
            return Result<bool>.NotFound($"LiquidityProvider with id {id} not found");

        await _repository.DeleteAsync(provider, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<bool>.Success(true);
    }
}
