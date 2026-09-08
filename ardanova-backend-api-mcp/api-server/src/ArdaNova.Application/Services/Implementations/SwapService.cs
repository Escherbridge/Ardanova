using ArdaNova.Application.Common.Interfaces;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Interfaces;
using ArdaNova.Domain.Models.Entities;
using ArdaNova.Domain.Models.Enums;

namespace ArdaNova.Application.Services.Implementations;

/// <summary>
/// Previews and history for swapping one project's units for another's, priced through ARDA.
/// </summary>
/// <remarks>
/// Every figure is decimal. Where a unit count has to become a whole number it is FLOORED and
/// then range-checked: a count that will not fit the wire type is refused, never silently
/// narrowed. Executing a swap stays refused until a buy-back liquidity policy exists (W8.2).
/// </remarks>
public class SwapService : ISwapService
{
    private readonly IRepository<TokenBalance> _tokenBalanceRepo;
    private readonly IRepository<ProjectTokenConfig> _configRepo;
    private readonly IRepository<PlatformTreasury> _treasuryRepo;
    private readonly IRepository<ShareSwap> _swapRepo;
    private readonly IUnitOfWork _unitOfWork;

    public SwapService(
        IRepository<TokenBalance> tokenBalanceRepo,
        IRepository<ProjectTokenConfig> configRepo,
        IRepository<PlatformTreasury> treasuryRepo,
        IRepository<ShareSwap> swapRepo,
        IUnitOfWork unitOfWork)
    {
        _tokenBalanceRepo = tokenBalanceRepo;
        _configRepo = configRepo;
        _treasuryRepo = treasuryRepo;
        _swapRepo = swapRepo;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<SwapPreviewDto>> GetSwapPreviewAsync(
        string userId,
        string sourceConfigId,
        string targetConfigId,
        int sourceTokenAmount,
        CancellationToken ct = default)
    {
        if (sourceTokenAmount <= 0)
            return Result<SwapPreviewDto>.Failure("Source token amount must be positive");

        if (sourceConfigId == targetConfigId)
            return Result<SwapPreviewDto>.Failure("Source and target tokens must be different");

        var sourceConfig = await _configRepo.GetByIdAsync(sourceConfigId, ct);
        if (sourceConfig == null)
            return Result<SwapPreviewDto>.Failure($"Source token configuration not found: {sourceConfigId}");

        if (sourceConfig.status != ProjectTokenStatus.ACTIVE)
            return Result<SwapPreviewDto>.Failure("Source token configuration is not active");

        if (sourceConfig.totalSupply == 0)
            return Result<SwapPreviewDto>.Failure("Source token has zero total supply");

        var targetConfig = await _configRepo.GetByIdAsync(targetConfigId, ct);
        if (targetConfig == null)
            return Result<SwapPreviewDto>.Failure($"Target token configuration not found: {targetConfigId}");

        if (targetConfig.status != ProjectTokenStatus.ACTIVE)
            return Result<SwapPreviewDto>.Failure("Target token configuration is not active");

        if (targetConfig.totalSupply == 0)
            return Result<SwapPreviewDto>.Failure("Target token has zero total supply");

        var sourceBalance = await _tokenBalanceRepo.FindOneAsync(
            b => b.userId == userId
              && b.projectTokenConfigId == sourceConfigId
              && b.isLiquid
              && b.holderClass == TokenHolderClass.CONTRIBUTOR,
            ct);

        if (sourceBalance == null || sourceBalance.balance - sourceBalance.lockedBalance < sourceTokenAmount)
            return Result<SwapPreviewDto>.Failure("Insufficient liquid contributor balance for source tokens");

        var treasuries = await _treasuryRepo.GetAllAsync(ct);
        var treasury = treasuries.FirstOrDefault();
        if (treasury == null)
            return Result<SwapPreviewDto>.Failure("Platform treasury not found");

        if (treasury.ardaCirculatingSupply == 0)
            return Result<SwapPreviewDto>.Failure("ARDA circulating supply is zero");

        var sourceTokenRate = (decimal)sourceConfig.fundingRaised / sourceConfig.totalSupply;
        var targetTokenRate = (decimal)targetConfig.fundingRaised / targetConfig.totalSupply;
        var totalBacking = (decimal)treasury.indexFundBalance
            + (decimal)treasury.liquidReserveBalance
            + (decimal)treasury.operationsBalance;
        var ardaRate = totalBacking / treasury.ardaCirculatingSupply;

        if (ardaRate <= 0)
            return Result<SwapPreviewDto>.Failure("ARDA has no backing, so no rate can be quoted");

        if (targetTokenRate <= 0)
            return Result<SwapPreviewDto>.Failure("Target token has no valuation, so no rate can be quoted");

        // One hop each way, ARDA in the middle: source units -> ARDA -> target units.
        var ardaPerSourceUnit = sourceTokenRate / ardaRate;
        var ardaPerTargetUnit = targetTokenRate / ardaRate;
        var ardaAmount = sourceTokenAmount * ardaPerSourceUnit;

        // A partial unit is not a unit; the surplus is deliberately dropped here.
        if (!Money.TryToWholeUnitsFloor(ardaAmount / ardaPerTargetUnit, out var targetUnits))
            return Result<SwapPreviewDto>.Failure(
                "The swap result is outside the range this platform can represent exactly.");

        if (targetUnits > int.MaxValue)
            return Result<SwapPreviewDto>.Failure(
                "The swap result is larger than a single quote can carry. Split it into smaller swaps.");

        var targetTokenAmount = (int)targetUnits;

        var preview = new SwapPreviewDto(
            SourceTokenAmount: sourceTokenAmount,
            SourceUnitName: sourceConfig.unitName,
            SourceUsdValue: sourceTokenAmount * sourceTokenRate,
            ArdaAmount: ardaAmount,
            TargetTokenAmount: targetTokenAmount,
            TargetUnitName: targetConfig.unitName,
            TargetUsdValue: targetTokenAmount * targetTokenRate,
            SourceTokenRate: sourceTokenRate,
            TargetTokenRate: targetTokenRate,
            ArdaRate: ardaRate
        );

        return Result<SwapPreviewDto>.Success(preview);
    }

    public Task<Result<SwapResultDto>> ExecuteSwapAsync(
        string userId,
        SwapRequestDto request,
        CancellationToken ct = default)
    {
        _ = userId;
        _ = request;
        _ = ct;

        return Task.FromResult(Result<SwapResultDto>.Failure(
            "Project share exchange is unavailable until secure provider settlement, reconciliation, and account eligibility checks are available."));
    }

    public async Task<Result<IReadOnlyList<SwapHistoryDto>>> GetSwapHistoryAsync(
        string userId,
        CancellationToken ct = default)
    {
        var swaps = await _swapRepo.FindAsync(s => s.userId == userId, ct);

        var configIds = swaps
            .SelectMany(s => new[] { s.fromShareId, s.toShareId })
            .Distinct()
            .ToList();

        var configList = await _configRepo.FindAsync(c => configIds.Contains(c.id), ct);
        var configs = configList.ToDictionary(c => c.id);

        var history = swaps
            .OrderByDescending(s => s.createdAt)
            .Select(s =>
            {
                var sourceConfig = configs.GetValueOrDefault(s.fromShareId);
                var targetConfig = configs.GetValueOrDefault(s.toShareId);

                var sourceTokenRate = sourceConfig != null && sourceConfig.totalSupply > 0
                    ? (decimal)sourceConfig.fundingRaised / sourceConfig.totalSupply
                    : 0m;
                var targetTokenRate = targetConfig != null && targetConfig.totalSupply > 0
                    ? (decimal)targetConfig.fundingRaised / targetConfig.totalSupply
                    : 0m;

                return new SwapHistoryDto
                {
                    Id = s.id,
                    SourceUnitName = sourceConfig?.unitName ?? s.fromShareId,
                    SourceTokenAmount = ToWholeUnitsForDisplay(s.fromAmount),
                    TargetUnitName = targetConfig?.unitName ?? s.toShareId,
                    TargetTokenAmount = ToWholeUnitsForDisplay(s.toAmount),
                    SourceUsdValue = s.fromAmount * sourceTokenRate,
                    TargetUsdValue = s.toAmount * targetTokenRate,
                    Status = s.status,
                    CreatedAt = s.createdAt,
                };
            })
            .ToList();

        return Result<IReadOnlyList<SwapHistoryDto>>.Success(history);
    }

    /// <summary>
    /// Whole units for a history row, floored and CLAMPED rather than narrowed.
    /// </summary>
    /// <remarks>
    /// A plain <c>(int)</c> cast of a large decimal wraps to a nonsense - often negative -
    /// count. The exact figures stay on the USD fields beside it; this one is a label.
    /// </remarks>
    private static int ToWholeUnitsForDisplay(decimal amount)
    {
        var floored = decimal.Floor(amount);
        if (floored >= int.MaxValue)
            return int.MaxValue;
        if (floored <= int.MinValue)
            return int.MinValue;

        return (int)floored;
    }
}
