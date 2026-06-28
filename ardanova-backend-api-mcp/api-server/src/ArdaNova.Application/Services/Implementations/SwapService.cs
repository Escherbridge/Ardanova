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

    public async Task<Result<SwapResultDto>> ExecuteSwapAsync(
        string userId,
        SwapRequestDto request,
        CancellationToken ct = default)
    {
        // Re-run preview validation to get computed values
        var previewResult = await GetSwapPreviewAsync(
            userId,
            request.SourceConfigId,
            request.TargetConfigId,
            request.SourceTokenAmount,
            ct);

        if (!previewResult.IsSuccess)
            return Result<SwapResultDto>.Failure(previewResult.Error ?? "Swap preview failed");

        var preview = previewResult.Value!;

        // Load source balance (validated in preview, but re-fetch for mutation)
        var sourceBalance = await _tokenBalanceRepo.FindOneAsync(
            b => b.userId == userId
              && b.projectTokenConfigId == request.SourceConfigId
              && b.isLiquid
              && b.holderClass == TokenHolderClass.CONTRIBUTOR,
            ct);

        if (sourceBalance == null)
            return Result<SwapResultDto>.Failure("Source balance not found");

        // Re-validate balance sufficiency to prevent race-condition double-spend
        if (sourceBalance.balance - sourceBalance.lockedBalance < request.SourceTokenAmount)
            return Result<SwapResultDto>.Failure("Insufficient liquid balance for swap");

        // Find or create target balance
        var targetBalance = await _tokenBalanceRepo.FindOneAsync(
            b => b.userId == userId
              && b.projectTokenConfigId == request.TargetConfigId
              && b.isLiquid
              && b.holderClass == TokenHolderClass.CONTRIBUTOR,
            ct);

        await _unitOfWork.BeginTransactionAsync(ct);
        try
        {
            // Debit source
            sourceBalance.balance -= request.SourceTokenAmount;
            sourceBalance.updatedAt = DateTime.UtcNow;
            await _tokenBalanceRepo.UpdateAsync(sourceBalance, ct);

            // Credit target (create if not exists)
            if (targetBalance == null)
            {
                targetBalance = new TokenBalance
                {
                    id = Guid.NewGuid().ToString(),
                    userId = userId,
                    projectTokenConfigId = request.TargetConfigId,
                    isPlatformToken = false,
                    holderClass = TokenHolderClass.CONTRIBUTOR,
                    isLiquid = true,
                    balance = preview.TargetTokenAmount,
                    lockedBalance = 0,
                    updatedAt = DateTime.UtcNow,
                };
                await _tokenBalanceRepo.AddAsync(targetBalance, ct);
            }
            else
            {
                targetBalance.balance += preview.TargetTokenAmount;
                targetBalance.updatedAt = DateTime.UtcNow;
                await _tokenBalanceRepo.UpdateAsync(targetBalance, ct);
            }

            // Persist swap record using ShareSwap entity
            var swapRecord = new ShareSwap
            {
                id = Guid.NewGuid().ToString(),
                userId = userId,
                fromShareId = request.SourceConfigId,
                toShareId = request.TargetConfigId,
                fromAmount = (decimal)request.SourceTokenAmount,
                toAmount = (decimal)preview.TargetTokenAmount,
                exchangeRate = (decimal)preview.ArdaRate,
                fee = 0m,
                status = SwapStatus.COMPLETED,
                createdAt = DateTime.UtcNow,
                completedAt = DateTime.UtcNow,
            };
            await _swapRepo.AddAsync(swapRecord, ct);

            await _unitOfWork.SaveChangesAsync(ct);
            await _unitOfWork.CommitTransactionAsync(ct);

            var result = new SwapResultDto
            {
                Id = swapRecord.id,
                UserId = userId,
                SourceConfigId = request.SourceConfigId,
                TargetConfigId = request.TargetConfigId,
                SourceTokenAmount = request.SourceTokenAmount,
                SourceUnitName = preview.SourceUnitName,
                SourceUsdValue = preview.SourceUsdValue,
                ArdaAmount = preview.ArdaAmount,
                TargetTokenAmount = preview.TargetTokenAmount,
                TargetUnitName = preview.TargetUnitName,
                TargetUsdValue = preview.TargetUsdValue,
                Status = SwapStatus.COMPLETED,
                CreatedAt = swapRecord.createdAt,
            };

            return Result<SwapResultDto>.Success(result);
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(ct);
            return Result<SwapResultDto>.Failure($"Swap execution failed: {ex.Message}");
        }
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
