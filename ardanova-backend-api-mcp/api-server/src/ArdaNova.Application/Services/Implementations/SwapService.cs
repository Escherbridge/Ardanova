using ArdaNova.Application.Common.Interfaces;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Interfaces;
using ArdaNova.Domain.Models.Entities;
using ArdaNova.Domain.Models.Enums;

namespace ArdaNova.Application.Services.Implementations;

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

        // Load source config
        var sourceConfig = await _configRepo.GetByIdAsync(sourceConfigId, ct);
        if (sourceConfig == null)
            return Result<SwapPreviewDto>.Failure($"Source token configuration not found: {sourceConfigId}");

        if (sourceConfig.status != ProjectTokenStatus.ACTIVE)
            return Result<SwapPreviewDto>.Failure("Source token configuration is not active");

        if (sourceConfig.totalSupply == 0)
            return Result<SwapPreviewDto>.Failure("Source token has zero total supply");

        // Load target config
        var targetConfig = await _configRepo.GetByIdAsync(targetConfigId, ct);
        if (targetConfig == null)
            return Result<SwapPreviewDto>.Failure($"Target token configuration not found: {targetConfigId}");

        if (targetConfig.status != ProjectTokenStatus.ACTIVE)
            return Result<SwapPreviewDto>.Failure("Target token configuration is not active");

        if (targetConfig.totalSupply == 0)
            return Result<SwapPreviewDto>.Failure("Target token has zero total supply");

        // Check source user balance
        var sourceBalance = await _tokenBalanceRepo.FindOneAsync(
            b => b.userId == userId
              && b.projectTokenConfigId == sourceConfigId
              && b.isLiquid
              && b.holderClass == TokenHolderClass.CONTRIBUTOR,
            ct);

        if (sourceBalance == null || sourceBalance.balance - sourceBalance.lockedBalance < sourceTokenAmount)
            return Result<SwapPreviewDto>.Failure("Insufficient liquid contributor balance for source tokens");

        // Load treasury for ARDA rate
        var treasuries = await _treasuryRepo.GetAllAsync(ct);
        var treasury = treasuries.FirstOrDefault();
        if (treasury == null)
            return Result<SwapPreviewDto>.Failure("Platform treasury not found");

        if (treasury.ardaCirculatingSupply == 0)
            return Result<SwapPreviewDto>.Failure("ARDA circulating supply is zero");

        // Compute rates using decimal for monetary precision
        var sourceTokenRate = (decimal)sourceConfig.fundingRaised / sourceConfig.totalSupply;
        var targetTokenRate = (decimal)targetConfig.fundingRaised / targetConfig.totalSupply;
        var totalBacking = (decimal)(treasury.indexFundBalance + treasury.liquidReserveBalance + treasury.operationsBalance);
        var ardaRate = totalBacking / (decimal)treasury.ardaCirculatingSupply;

        // Conversion chain: sourceTokens → USD → ARDA → targetTokens
        var sourceUsdValue = sourceTokenAmount * sourceTokenRate;
        var ardaAmount = sourceUsdValue / ardaRate;
        var targetUsdValue = ardaAmount * ardaRate;
        var targetTokenAmount = targetTokenRate > 0
            ? (int)Math.Floor(targetUsdValue / targetTokenRate)
            : 0;

        var preview = new SwapPreviewDto(
            SourceTokenAmount: sourceTokenAmount,
            SourceUnitName: sourceConfig.unitName,
            SourceUsdValue: sourceUsdValue,
            ArdaAmount: ardaAmount,
            TargetTokenAmount: targetTokenAmount,
            TargetUnitName: targetConfig.unitName,
            TargetUsdValue: targetUsdValue,
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

        // Build a lookup of configId → unitName
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
                    SourceTokenAmount = (int)s.fromAmount,
                    TargetUnitName = targetConfig?.unitName ?? s.toShareId,
                    TargetTokenAmount = (int)s.toAmount,
                    SourceUsdValue = s.fromAmount * sourceTokenRate,
                    TargetUsdValue = s.toAmount * targetTokenRate,
                    Status = s.status,
                    CreatedAt = s.createdAt,
                };
            })
            .ToList();

        return Result<IReadOnlyList<SwapHistoryDto>>.Success(history);
    }
}
