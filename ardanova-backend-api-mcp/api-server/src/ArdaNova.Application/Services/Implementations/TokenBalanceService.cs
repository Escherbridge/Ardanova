using System.Linq.Expressions;
using ArdaNova.Application.Common.Interfaces;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Interfaces;
using ArdaNova.Domain.Models.Entities;
using ArdaNova.Domain.Models.Enums;
using ArdaNova.Domain.Policies;
using AutoMapper;

namespace ArdaNova.Application.Services.Implementations;

/// <summary>
/// Holdings, as a read-model of what the chain says a person has.
/// </summary>
/// <remarks>
/// A person's ARDA is the balance of their custodial wallet on the node, NOT an
/// <c>isPlatformToken</c> row: that row was never credited by anything, which is why an ARDA
/// balance read zero forever. <see cref="GetArdaBalanceAsync"/> now reads the chain and refuses
/// when it cannot, rather than returning a database row that looks like an answer.
/// <para>
/// Project-unit rows are still ArdaNova's own bookkeeping until the node holds them (W7.2).
/// They carry their provenance - <c>assetId</c>, <c>walletAddress</c>, <c>observedAt</c> - so a
/// caller can always tell a chain-observed holding from an internal one.
/// </para>
/// </remarks>
public class TokenBalanceService : ITokenBalanceService
{
    /// <summary>Why an ARDA balance is unavailable when nothing is wired to read the chain.</summary>
    public const string NoHoldingsReadConfigured =
        "No custodial holdings read is wired to the node, so this balance cannot be reported. "
        + "Unblocked by an API-key-reachable holdings read on AZOA (today only the wallet owner's "
        + "own principal can call GET /api/wallet/{id}/portfolio).";

    private readonly IRepository<TokenBalance> _tokenBalanceRepository;
    private readonly IRepository<ProjectTokenConfig> _projectTokenConfigRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IAzoaHoldingsReadModel? _holdingsReadModel;
    private readonly IRepository<User>? _userRepository;
    private readonly IRepository<PlatformTreasury>? _treasuryRepository;

    /// <param name="holdingsReadModel">
    /// The node read-model. Optional in the constructor only so the type still constructs without
    /// a container; when it is absent an ARDA read REFUSES instead of falling back to a row.
    /// </param>
    public TokenBalanceService(
        IRepository<TokenBalance> tokenBalanceRepository,
        IRepository<ProjectTokenConfig> projectTokenConfigRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IAzoaHoldingsReadModel? holdingsReadModel = null,
        IRepository<User>? userRepository = null,
        IRepository<PlatformTreasury>? treasuryRepository = null)
    {
        _tokenBalanceRepository = tokenBalanceRepository;
        _projectTokenConfigRepository = projectTokenConfigRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _holdingsReadModel = holdingsReadModel;
        _userRepository = userRepository;
        _treasuryRepository = treasuryRepository;
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

    /// <summary>
    /// The person's ARDA, read from their custodial wallet on the node.
    /// </summary>
    /// <remarks>
    /// Fails closed at every step that cannot be answered truthfully: no custodial avatar, no
    /// ARDA asset registered yet, or no holdings read wired. None of those is "zero".
    /// </remarks>
    public async Task<Result<TokenBalanceDto>> GetArdaBalanceAsync(string userId, CancellationToken ct = default)
    {
        var holding = await ReadArdaHoldingAsync(userId, ct);
        if (!holding.IsSuccess || holding.Value is null)
        {
            return Result<TokenBalanceDto>.Failure(holding.Error ?? NoHoldingsReadConfigured);
        }

        return Result<TokenBalanceDto>.Success(ToArdaDto(userId, holding.Value));
    }

    public async Task<Result<UserPortfolioDto>> GetPortfolioAsync(string userId, CancellationToken ct = default)
    {
        Expression<Func<TokenBalance, bool>> predicate = tb => tb.userId == userId;
        var balances = await _tokenBalanceRepository.FindAsync(predicate, ct);

        var holding = await ReadArdaHoldingAsync(userId, ct);

        var dto = new UserPortfolioDto
        {
            UserId = userId,
            // isPlatformToken rows are ArdaNova bookkeeping, never a person's ARDA; the ARDA
            // leg below is the only place a platform balance comes from.
            Holdings = _mapper.Map<List<TokenBalanceDto>>(
                balances.Where(row => !row.isPlatformToken).ToList()),
            ArdaBalance = holding.IsSuccess && holding.Value is not null
                ? ToArdaDto(userId, holding.Value)
                : null,
            ArdaBalanceUnavailableReason = holding.IsSuccess ? null : holding.Error,
            TotalLiquidValueUsd = null,
            TotalLockedValueUsd = null,
            TotalPortfolioValueUsd = null,
            ValuationUnavailableReason =
                "Portfolio valuation needs a chain-backed treasury read; see TreasuryService.",
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

        // checked: base units are exact, so an overflow must throw rather than wrap a holding
        // around into a negative number.
        try
        {
            balance.balance = checked(balance.balance + amount);
        }
        catch (OverflowException)
        {
            return Result<TokenBalanceDto>.Failure("Credit would exceed the range this platform can hold.");
        }

        balance.updatedAt = DateTime.UtcNow;

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
        Expression<Func<TokenBalance, bool>> balancePredicate = tb =>
            tb.userId == userId &&
            tb.projectTokenConfigId == projectTokenConfigId &&
            tb.holderClass == holderClass;

        var balance = await _tokenBalanceRepository.FindOneAsync(balancePredicate, ct);
        if (balance != null && balance.isPlatformToken)
        {
            return Result<bool>.Success(true);
        }

        var config = await _projectTokenConfigRepository.GetByIdAsync(projectTokenConfigId, ct);
        if (config == null)
        {
            return Result<bool>.Failure("Project token configuration not found");
        }

        return Result<bool>.Success(TokenLiquidityPolicy.IsLiquid(holderClass, config.gateStatus));
    }

    /// <summary>Reads the person's ARDA holding from the node, or says exactly why it cannot.</summary>
    private async Task<Result<AvatarHolding>> ReadArdaHoldingAsync(string userId, CancellationToken ct)
    {
        if (_holdingsReadModel is null || _userRepository is null || _treasuryRepository is null)
        {
            return Result<AvatarHolding>.Failure(NoHoldingsReadConfigured);
        }

        var user = await _userRepository.GetByIdAsync(userId, ct);
        if (user is null)
        {
            return Result<AvatarHolding>.NotFound($"User not found: {userId}");
        }

        if (!Guid.TryParse(user.azoaAvatarId, out var avatarId) || avatarId == Guid.Empty)
        {
            return Result<AvatarHolding>.Failure(
                "This account has no custodial avatar on the node yet, so it holds nothing to report.");
        }

        var treasuries = await _treasuryRepository.GetAllAsync(ct);
        var ardaAssetId = treasuries.FirstOrDefault()?.ardaAssetId;
        if (string.IsNullOrWhiteSpace(ardaAssetId))
        {
            return Result<AvatarHolding>.Failure(
                "The platform unit has not been created on the node yet, so no balance exists to read.");
        }

        return await _holdingsReadModel.GetHoldingAsync(avatarId, ardaAssetId, ct);
    }

    /// <summary>Projects a chain holding onto the balance DTO, stamped with its provenance.</summary>
    private static TokenBalanceDto ToArdaDto(string userId, AvatarHolding holding) =>
        new()
        {
            Id = $"arda:{userId}",
            UserId = userId,
            ProjectTokenConfigId = null,
            IsPlatformToken = true,
            HolderClass = null,
            IsLiquid = true,
            Balance = holding.BalanceBaseUnits,
            LockedBalance = 0,
            AssetId = holding.AssetId,
            WalletAddress = holding.WalletAddress,
            ObservedAt = holding.ObservedAt.UtcDateTime,
            UpdatedAt = holding.ObservedAt.UtcDateTime,
        };
}
