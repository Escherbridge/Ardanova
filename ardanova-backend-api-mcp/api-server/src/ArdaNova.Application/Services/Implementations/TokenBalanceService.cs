using System.Linq.Expressions;
using ArdaNova.Application.Common.Interfaces;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Interfaces;
using ArdaNova.Domain.Models.Entities;
using ArdaNova.Domain.Models.Enums;
using AutoMapper;

namespace ArdaNova.Application.Services.Implementations;

public class TokenBalanceService : ITokenBalanceService
{
    private readonly IRepository<TokenBalance> _tokenBalanceRepository;
    private readonly IRepository<ProjectTokenConfig> _projectTokenConfigRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public TokenBalanceService(
        IRepository<TokenBalance> tokenBalanceRepository,
        IRepository<ProjectTokenConfig> projectTokenConfigRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper)
    {
        _tokenBalanceRepository = tokenBalanceRepository;
        _projectTokenConfigRepository = projectTokenConfigRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<Result<TokenBalanceDto>> GetBalanceAsync(
        string userId,
        string projectTokenConfigId,
        TokenHolderClass holderClass,
        CancellationToken ct = default)
    {
        Expression<Func<TokenBalance, bool>> predicate = tb =>
            tb.userId == userId &&
            tb.projectTokenConfigId == projectTokenConfigId &&
            tb.holderClass == holderClass;

        var balance = await _tokenBalanceRepository.FindOneAsync(predicate, ct);

        if (balance == null)
        {
            return Result<TokenBalanceDto>.Failure($"Token balance not found for user {userId}, config {projectTokenConfigId}, class {holderClass}");
        }

        var dto = _mapper.Map<TokenBalanceDto>(balance);
        return Result<TokenBalanceDto>.Success(dto);
    }

    public async Task<Result<TokenBalanceDto>> GetArdaBalanceAsync(string userId, CancellationToken ct = default)
    {
        Expression<Func<TokenBalance, bool>> balancePredicate = tb =>
            tb.userId == userId &&
            tb.isPlatformToken == true;

        var balance = await _tokenBalanceRepository.FindOneAsync(balancePredicate, ct);

        if (balance == null)
        {
            return Result<TokenBalanceDto>.Failure($"ARDA balance not found for user {userId}");
        }

        var dto = _mapper.Map<TokenBalanceDto>(balance);
        return Result<TokenBalanceDto>.Success(dto);
    }

    public async Task<Result<UserPortfolioDto>> GetPortfolioAsync(string userId, CancellationToken ct = default)
    {
        Expression<Func<TokenBalance, bool>> predicate = tb => tb.userId == userId;
        var balances = await _tokenBalanceRepository.FindAsync(predicate, ct);

        var dto = new UserPortfolioDto
        {
            UserId = userId,
            Holdings = _mapper.Map<List<TokenBalanceDto>>(balances),
            ArdaBalance = null,
            TotalLiquidValueUsd = 0,
            TotalLockedValueUsd = 0,
            TotalPortfolioValueUsd = 0
        };
        return Result<UserPortfolioDto>.Success(dto);
    }

    public async Task<Result<TokenBalanceDto>> CreditAsync(
        string userId,
        string projectTokenConfigId,
        int amount,
        TokenHolderClass holderClass,
        CancellationToken ct = default)
    {
        if (amount <= 0)
        {
            return Result<TokenBalanceDto>.Failure("Credit amount must be positive");
        }

        // Find or create balance
        Expression<Func<TokenBalance, bool>> predicate = tb =>
            tb.userId == userId &&
            tb.projectTokenConfigId == projectTokenConfigId &&
            tb.holderClass == holderClass;

        var balance = await _tokenBalanceRepository.FindOneAsync(predicate, ct);

        if (balance == null)
        {
            balance = new TokenBalance
            {
                id = Guid.NewGuid().ToString(),
                userId = userId,
                projectTokenConfigId = projectTokenConfigId,
                holderClass = holderClass,
                balance = 0,
                lockedBalance = 0,
                isLiquid = false,
                isPlatformToken = false,
                updatedAt = DateTime.UtcNow
            };
            await _tokenBalanceRepository.AddAsync(balance, ct);
        }

        // Increment balance
        balance.balance += amount;
        balance.updatedAt = DateTime.UtcNow;

        // Determine liquidity
        var liquidResult = await IsBalanceLiquidAsync(userId, projectTokenConfigId, holderClass, ct);
        if (liquidResult.IsSuccess)
        {
            balance.isLiquid = liquidResult.Value;
        }

        await _tokenBalanceRepository.UpdateAsync(balance, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        var dto = _mapper.Map<TokenBalanceDto>(balance);
        return Result<TokenBalanceDto>.Success(dto);
    }

    public async Task<Result<TokenBalanceDto>> DebitAsync(
        string userId,
        string projectTokenConfigId,
        int amount,
        TokenHolderClass holderClass,
        CancellationToken ct = default)
    {
        if (amount <= 0)
        {
            return Result<TokenBalanceDto>.Failure("Debit amount must be positive");
        }

        Expression<Func<TokenBalance, bool>> predicate = tb =>
            tb.userId == userId &&
            tb.projectTokenConfigId == projectTokenConfigId &&
            tb.holderClass == holderClass;

        var balance = await _tokenBalanceRepository.FindOneAsync(predicate, ct);

        if (balance == null)
        {
            return Result<TokenBalanceDto>.Failure($"Token balance not found for user {userId}");
        }

        if (balance.balance < amount)
        {
            return Result<TokenBalanceDto>.Failure($"Insufficient balance. Available: {balance.balance}, Required: {amount}");
        }

        balance.balance -= amount;
        balance.updatedAt = DateTime.UtcNow;

        await _tokenBalanceRepository.UpdateAsync(balance, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        var dto = _mapper.Map<TokenBalanceDto>(balance);
        return Result<TokenBalanceDto>.Success(dto);
    }

    public async Task<Result<TokenBalanceDto>> LockAsync(
        string userId,
        string projectTokenConfigId,
        int amount,
        TokenHolderClass holderClass,
        CancellationToken ct = default)
    {
        if (amount <= 0)
        {
            return Result<TokenBalanceDto>.Failure("Lock amount must be positive");
        }

        Expression<Func<TokenBalance, bool>> predicate = tb =>
            tb.userId == userId &&
            tb.projectTokenConfigId == projectTokenConfigId &&
            tb.holderClass == holderClass;

        var balance = await _tokenBalanceRepository.FindOneAsync(predicate, ct);

        if (balance == null)
        {
            return Result<TokenBalanceDto>.Failure($"Token balance not found for user {userId}");
        }

        var availableBalance = balance.balance - balance.lockedBalance;
        if (availableBalance < amount)
        {
            return Result<TokenBalanceDto>.Failure($"Insufficient unlocked balance. Available: {availableBalance}, Required: {amount}");
        }

        balance.lockedBalance += amount;
        balance.updatedAt = DateTime.UtcNow;

        await _tokenBalanceRepository.UpdateAsync(balance, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        var dto = _mapper.Map<TokenBalanceDto>(balance);
        return Result<TokenBalanceDto>.Success(dto);
    }

    public async Task<Result<TokenBalanceDto>> UnlockAsync(
        string userId,
        string projectTokenConfigId,
        int amount,
        TokenHolderClass holderClass,
        CancellationToken ct = default)
    {
        if (amount <= 0)
        {
            return Result<TokenBalanceDto>.Failure("Unlock amount must be positive");
        }

        Expression<Func<TokenBalance, bool>> predicate = tb =>
            tb.userId == userId &&
            tb.projectTokenConfigId == projectTokenConfigId &&
            tb.holderClass == holderClass;

        var balance = await _tokenBalanceRepository.FindOneAsync(predicate, ct);

        if (balance == null)
        {
            return Result<TokenBalanceDto>.Failure($"Token balance not found for user {userId}");
        }

        if (balance.lockedBalance < amount)
        {
            return Result<TokenBalanceDto>.Failure($"Insufficient locked balance. Locked: {balance.lockedBalance}, Requested: {amount}");
        }

        balance.lockedBalance -= amount;
        balance.updatedAt = DateTime.UtcNow;

        await _tokenBalanceRepository.UpdateAsync(balance, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        var dto = _mapper.Map<TokenBalanceDto>(balance);
        return Result<TokenBalanceDto>.Success(dto);
    }

    public async Task<Result<bool>> IsBalanceLiquidAsync(
        string userId,
        string projectTokenConfigId,
        TokenHolderClass holderClass,
        CancellationToken ct = default)
    {
        // Check if this is a platform token balance
        Expression<Func<TokenBalance, bool>> balancePredicate = tb =>
            tb.userId == userId &&
            tb.projectTokenConfigId == projectTokenConfigId &&
            tb.holderClass == holderClass;

        var balance = await _tokenBalanceRepository.FindOneAsync(balancePredicate, ct);
        if (balance != null && balance.isPlatformToken)
        {
            return Result<bool>.Success(true);
        }

        // Get the project token config to check gate status
        var config = await _projectTokenConfigRepository.GetByIdAsync(projectTokenConfigId, ct);
        if (config == null)
        {
            return Result<bool>.Failure("Project token configuration not found");
        }

        // According to the business rules:
        // - CONTRIBUTOR: liquid when gate is ACTIVE or SUCCEEDED
        // - INVESTOR/FOUNDER: liquid only when SUCCEEDED
        // - FOUNDER tokens are burned if project FAILED

        bool isLiquid = (holderClass, config.gateStatus) switch
        {
            (TokenHolderClass.CONTRIBUTOR, ProjectGateStatus.ACTIVE) => true,
            (TokenHolderClass.CONTRIBUTOR, ProjectGateStatus.SUCCEEDED) => true,
            (TokenHolderClass.INVESTOR, ProjectGateStatus.SUCCEEDED) => true,
            (TokenHolderClass.FOUNDER, ProjectGateStatus.SUCCEEDED) => true,
            _ => false
        };

        return Result<bool>.Success(isLiquid);
    }
}
