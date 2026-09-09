using ArdaNova.Application.Common.Interfaces;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Interfaces;
using ArdaNova.Domain.Models.Entities;
using ArdaNova.Domain.Models.Enums;
using ArdaNova.Domain.Policies;

namespace ArdaNova.Application.Services.Implementations;

/// <summary>
/// Previews and history for swapping one project's units for another's, priced through ARDA.
/// </summary>
/// <remarks>
/// Every figure is decimal. Where a unit count has to become a whole number it is FLOORED and
/// then range-checked: a count that will not fit the wire type is refused, never silently
/// narrowed. Executing a swap stays refused until a buy-back liquidity policy exists (W8.2).
/// <para>
/// A preview quotes only units the holder may actually move: the project's gate AND the worker's
/// own restricted window, both read live through <see cref="UnitWindow"/>. It deliberately does
/// NOT select on the stored <c>isLiquid</c> column - that column is stamped when units are
/// credited and when a gate moves, while the window ends on a date nothing revisits, so quoting
/// against it offers a person units they were told are held until a date still ahead.
/// </para>
/// </remarks>
public class SwapService : ISwapService
{
    /// <summary>Why a preview is refused when the awards behind a holding cannot be read.</summary>
    /// <remarks>
    /// Fail CLOSED: without the awards there is no way to tell a free unit from one still inside
    /// the worker's window, and quoting the difference away is how a held award gets spent.
    /// </remarks>
    public const string NoWindowReadable =
        "Your units could not be checked against the date they are held until, so nothing can be "
        + "quoted. Please try again shortly.";

    /// <summary>Why moving units straight from one project into another is refused today.</summary>
    public const string SwapNotAvailable =
        "Moving units straight from one project into another is not available yet, so nothing was "
        + "moved. You can sell units you are free to move and put the funds into another project.";

    private readonly IRepository<TokenBalance> _tokenBalanceRepo;
    private readonly IRepository<ProjectTokenConfig> _configRepo;
    private readonly IRepository<PlatformTreasury> _treasuryRepo;
    private readonly IRepository<ShareSwap> _swapRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IRepository<TokenAllocation>? _allocationRepo;

    /// <param name="allocationRepo">
    /// The awards a holder's restricted window is read off. Optional in the constructor only so
    /// the type still constructs without a container; the container always supplies it through the
    /// open generic repository registration, and its absence REFUSES a preview rather than
    /// quoting one that ignores the window.
    /// </param>
    public SwapService(
        IRepository<TokenBalance> tokenBalanceRepo,
        IRepository<ProjectTokenConfig> configRepo,
        IRepository<PlatformTreasury> treasuryRepo,
        IRepository<ShareSwap> swapRepo,
        IUnitOfWork unitOfWork,
        IRepository<TokenAllocation>? allocationRepo = null)
    {
        _tokenBalanceRepo = tokenBalanceRepo;
        _configRepo = configRepo;
        _treasuryRepo = treasuryRepo;
        _swapRepo = swapRepo;
        _unitOfWork = unitOfWork;
        _allocationRepo = allocationRepo;
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

        // Found by IDENTITY, then judged live. Selecting on the stored isLiquid column would let a
        // stamp decide a spend: it is written from the gate alone at credit time and at gate
        // activation, and it is never rewritten when a worker's window ends.
        var sourceBalance = await _tokenBalanceRepo.FindOneAsync(
            b => b.userId == userId
              && b.projectTokenConfigId == sourceConfigId
              && b.holderClass == TokenHolderClass.CONTRIBUTOR,
            ct);

        if (sourceBalance == null)
            return Result<SwapPreviewDto>.Failure("You do not hold any units in this project.");

        if (!TokenLiquidityPolicy.IsLiquid(TokenHolderClass.CONTRIBUTOR, sourceConfig.gateStatus))
            return Result<SwapPreviewDto>.Failure(DescribeIlliquidity(sourceConfig.gateStatus));

        if (_allocationRepo is null)
            return Result<SwapPreviewDto>.Failure(NoWindowReadable);

        // The worker's own window is part of the balance and must not be movable. The ceiling is
        // what is free AFTER it, never the number the balance row happens to show.
        var restriction = await UnitWindow.ReadAsync(
            _allocationRepo, userId, sourceConfigId, DateTimeOffset.UtcNow, ct);

        var movableUnits = sourceBalance.balance - sourceBalance.lockedBalance - restriction.RestrictedUnits;
        if (movableUnits < sourceTokenAmount)
            return Result<SwapPreviewDto>.Failure(DescribeShortfall(movableUnits, restriction));

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

        // Settlement, reconciliation and the account eligibility gate all exist now - they are
        // what a unit SALE runs through. What does not exist is a buy-back liquidity policy for
        // trading one project's units for another's (W8.2), so the refusal names that and points
        // at the route that does work, rather than at conditions that have since been met.
        return Task.FromResult(Result<SwapResultDto>.Failure(SwapNotAvailable));
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

    /// <summary>Why this project's units cannot be moved at all yet, in the holder's own terms.</summary>
    private static string DescribeIlliquidity(ProjectGateStatus gateStatus) => gateStatus switch
    {
        ProjectGateStatus.FUNDING =>
            "This project has not reached its funding goal yet, so its units cannot be moved.",
        ProjectGateStatus.FAILED =>
            "This project ended, so its units cannot be moved.",
        _ => "These units cannot be moved at the moment.",
    };

    /// <summary>Why the requested amount is more than this person can move.</summary>
    /// <remarks>The same shape the sale seam refuses with, in the verb this one is about.</remarks>
    private static string DescribeShortfall(long movableUnits, UnitWindowRestriction restriction)
    {
        var free = Math.Max(0L, movableUnits);

        if (restriction.RestrictedUnits > 0 && restriction.RestrictedUntil is { } until)
        {
            return $"You can move {free:N0} units now. Another {restriction.RestrictedUnits:N0} "
                + $"are yours but held until {until.UtcDateTime:d MMMM yyyy}.";
        }

        if (restriction.RestrictedUnits > 0)
        {
            return $"You can move {free:N0} units now. Another {restriction.RestrictedUnits:N0} "
                + "are yours but still held.";
        }

        return $"You can move {free:N0} units now.";
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
