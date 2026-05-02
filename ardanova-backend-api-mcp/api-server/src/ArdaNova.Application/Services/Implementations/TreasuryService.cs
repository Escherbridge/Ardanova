using ArdaNova.Application.Common.Interfaces;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Interfaces;
using ArdaNova.Domain.Models.Entities;
using ArdaNova.Domain.Models.Enums;
using AutoMapper;

namespace ArdaNova.Application.Services.Implementations;

public class TreasuryService : ITreasuryService
{
    private readonly IRepository<PlatformTreasury> _treasuryRepository;
    private readonly IRepository<PlatformTreasuryTransaction> _transactionRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public TreasuryService(
        IRepository<PlatformTreasury> treasuryRepository,
        IRepository<PlatformTreasuryTransaction> transactionRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper)
    {
        _treasuryRepository = treasuryRepository;
        _transactionRepository = transactionRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<Result<bool>> ProcessFundingInflowAsync(
        double usdAmount,
        string? projectId,
        CancellationToken ct = default)
    {
        if (usdAmount <= 0)
        {
            return Result<bool>.Failure("Funding amount must be positive");
        }

        var treasury = await GetOrCreateTreasuryAsync(ct);

        // Split funding according to allocation percentages
        var indexAmount = usdAmount * treasury.indexFundAllocationPct;
        var liquidAmount = usdAmount * treasury.liquidReserveAllocationPct;
        var opsAmount = usdAmount * treasury.operationsAllocationPct;

        // Update treasury balances
        treasury.indexFundBalance += indexAmount;
        treasury.liquidReserveBalance += liquidAmount;
        treasury.operationsBalance += opsAmount;
        treasury.totalInflows += usdAmount;
        treasury.updatedAt = DateTime.UtcNow;

        await _treasuryRepository.UpdateAsync(treasury, ct);

        // Create transaction records for each allocation
        var transactions = new[]
        {
            new PlatformTreasuryTransaction
            {
                id = Guid.NewGuid().ToString(),
                type = PlatformTreasuryTransactionType.ALLOCATION_INDEX,
                amount = indexAmount,
                balanceAfter = treasury.indexFundBalance,
                description = projectId != null ? $"Index fund allocation from project {projectId}" : "Index fund allocation",
                relatedProjectId = projectId,
                createdAt = DateTime.UtcNow
            },
            new PlatformTreasuryTransaction
            {
                id = Guid.NewGuid().ToString(),
                type = PlatformTreasuryTransactionType.ALLOCATION_LIQUID,
                amount = liquidAmount,
                balanceAfter = treasury.liquidReserveBalance,
                description = projectId != null ? $"Liquid reserve allocation from project {projectId}" : "Liquid reserve allocation",
                relatedProjectId = projectId,
                createdAt = DateTime.UtcNow
            },
            new PlatformTreasuryTransaction
            {
                id = Guid.NewGuid().ToString(),
                type = PlatformTreasuryTransactionType.ALLOCATION_OPS,
                amount = opsAmount,
                balanceAfter = treasury.operationsBalance,
                description = projectId != null ? $"Operations allocation from project {projectId}" : "Operations allocation",
                relatedProjectId = projectId,
                createdAt = DateTime.UtcNow
            }
        };

        foreach (var transaction in transactions)
        {
            await _transactionRepository.AddAsync(transaction, ct);
        }

        await _unitOfWork.SaveChangesAsync(ct);

        return Result<bool>.Success(true);
    }

    public async Task<Result<bool>> ApplyIndexFundReturnAsync(CancellationToken ct = default)
    {
        var treasury = await GetOrCreateTreasuryAsync(ct);

        if (treasury.indexFundBalance == 0)
        {
            return Result<bool>.Failure("Index fund balance is zero, no returns to apply");
        }

        // Calculate monthly return (annual return / 12)
        var monthlyReturn = treasury.indexFundBalance * (treasury.indexFundAnnualReturn / 12);

        // Calculate profit share for platform operations
        var profitShare = monthlyReturn * treasury.platformProfitSharePct;

        // Apply returns to index fund (net of profit share)
        treasury.indexFundBalance += (monthlyReturn - profitShare);

        // Transfer profit share to operations
        treasury.operationsBalance += profitShare;
        treasury.updatedAt = DateTime.UtcNow;

        await _treasuryRepository.UpdateAsync(treasury, ct);

        // Log transactions
        var indexReturnTransaction = new PlatformTreasuryTransaction
        {
            id = Guid.NewGuid().ToString(),
            type = PlatformTreasuryTransactionType.INDEX_RETURN,
            amount = monthlyReturn,
            balanceAfter = treasury.indexFundBalance,
            description = $"Monthly index fund return applied ({treasury.indexFundAnnualReturn:P2} annual)",
            createdAt = DateTime.UtcNow
        };

        var profitShareTransaction = new PlatformTreasuryTransaction
        {
            id = Guid.NewGuid().ToString(),
            type = PlatformTreasuryTransactionType.PROFIT_SHARE,
            amount = profitShare,
            balanceAfter = treasury.operationsBalance,
            description = $"Platform profit share from index returns ({treasury.platformProfitSharePct:P2})",
            createdAt = DateTime.UtcNow
        };

        await _transactionRepository.AddAsync(indexReturnTransaction, ct);
        await _transactionRepository.AddAsync(profitShareTransaction, ct);

        await _unitOfWork.SaveChangesAsync(ct);

        return Result<bool>.Success(true);
    }

    public async Task<Result<double>> RebalanceIfNeededAsync(double requiredLiquid, CancellationToken ct = default)
    {
        var treasury = await GetOrCreateTreasuryAsync(ct);

        // Check if rebalancing is needed
        if (treasury.liquidReserveBalance >= requiredLiquid)
        {
            // No rebalancing needed
            return Result<double>.Success(0);
        }

        // Calculate deficit
        var deficit = requiredLiquid - treasury.liquidReserveBalance;

        // Transfer from index fund (up to available balance)
        var transferAmount = Math.Min(deficit, treasury.indexFundBalance);

        if (transferAmount == 0)
        {
            return Result<double>.Failure("Insufficient index fund balance to rebalance");
        }

        // Execute transfer
        treasury.indexFundBalance -= transferAmount;
        treasury.liquidReserveBalance += transferAmount;
        treasury.totalRebalanceTransfers += transferAmount;
        treasury.updatedAt = DateTime.UtcNow;

        await _treasuryRepository.UpdateAsync(treasury, ct);

        // Log rebalance transaction
        var transaction = new PlatformTreasuryTransaction
        {
            id = Guid.NewGuid().ToString(),
            type = PlatformTreasuryTransactionType.REBALANCE,
            amount = transferAmount,
            balanceAfter = treasury.liquidReserveBalance,
            description = $"Rebalanced ${transferAmount:N2} from index fund to liquid reserve (deficit: ${deficit:N2})",
            fromBucket = "index",
            toBucket = "liquid",
            createdAt = DateTime.UtcNow
        };

        await _transactionRepository.AddAsync(transaction, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return Result<double>.Success(transferAmount);
    }

    public async Task<Result<bool>> ReconcileAsync(CancellationToken ct = default)
    {
        var treasury = await GetOrCreateTreasuryAsync(ct);

        var totalBalance = treasury.indexFundBalance + treasury.liquidReserveBalance + treasury.operationsBalance;

        if (totalBalance <= 0)
        {
            return Result<bool>.Failure("Cannot reconcile: total treasury balance is zero or negative");
        }

        treasury.lastReconciliationAt = DateTime.UtcNow;
        treasury.updatedAt = DateTime.UtcNow;
        await _treasuryRepository.UpdateAsync(treasury, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return Result<bool>.Success(true);
    }

    public async Task<Result<TreasuryStatusDto>> GetStatusAsync(CancellationToken ct = default)
    {
        var treasury = await GetOrCreateTreasuryAsync(ct);
        var dto = _mapper.Map<TreasuryStatusDto>(treasury);
        return Result<TreasuryStatusDto>.Success(dto);
    }

    public async Task<Result<IReadOnlyList<PlatformTreasuryTransactionDto>>> GetTransactionHistoryAsync(int limit = 50, CancellationToken ct = default)
    {
        var allTransactions = await _transactionRepository.GetAllAsync(ct);

        var recentTransactions = allTransactions
            .OrderByDescending(t => t.createdAt)
            .Take(limit)
            .ToList();

        var dtos = _mapper.Map<List<PlatformTreasuryTransactionDto>>(recentTransactions);
        return Result<IReadOnlyList<PlatformTreasuryTransactionDto>>.Success(dtos);
    }

    private async Task<PlatformTreasury> GetOrCreateTreasuryAsync(CancellationToken ct = default)
    {
        var treasuries = await _treasuryRepository.GetAllAsync(ct);
        var treasury = treasuries.FirstOrDefault();

        if (treasury == null)
        {
            // Create default treasury
            treasury = new PlatformTreasury
            {
                id = Guid.NewGuid().ToString(),
                indexFundBalance = 0,
                liquidReserveBalance = 0,
                operationsBalance = 0,
                ardaTotalSupply = 0,
                ardaCirculatingSupply = 0,
                indexFundAllocationPct = 0.60, // 60%
                liquidReserveAllocationPct = 0.30, // 30%
                operationsAllocationPct = 0.10, // 10%
                indexFundAnnualReturn = 0.12, // 12% annual
                platformProfitSharePct = 0.20, // 20%
                trustProtectionRate = 0.30, // 30%
                totalInflows = 0,
                totalPayouts = 0,
                totalRebalanceTransfers = 0,
                lastReconciliationAt = null,
                updatedAt = DateTime.UtcNow
            };

            await _treasuryRepository.AddAsync(treasury, ct);
            await _unitOfWork.SaveChangesAsync(ct);
        }

        return treasury;
    }
}
