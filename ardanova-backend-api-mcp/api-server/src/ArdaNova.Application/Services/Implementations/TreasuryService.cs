using ArdaNova.Application.Common.Interfaces;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Interfaces;
using ArdaNova.Domain.Models.Entities;
using ArdaNova.Domain.Models.Enums;
using AutoMapper;

namespace ArdaNova.Application.Services.Implementations;

/// <summary>
/// Projects a treasury status as what it is: a POLICY VIEW over one balance, plus whether a
/// chain read actually backed it.
/// </summary>
/// <remarks>
/// The three buckets are null unless a chain read produced them. A null cannot be rendered as
/// "$0.00", so nobody can mistake an unavailable read for an empty treasury - which is exactly
/// how a database number came to look like money in the first place.
/// </remarks>
public static class TreasuryPolicyView
{
    /// <summary>Reason stamped when the caller's route has no project to read a treasury for.</summary>
    public const string NoChainReadOnThisRoute =
        "This status is the platform's policy configuration only. A project's balance of record "
        + "is its treasury account on the node; ask for that project's treasury to see it.";

    /// <summary>Reason stamped when no node read-model is wired at all.</summary>
    public const string NoChainReadConfigured =
        "No treasury balance read is wired to the node, so no balance is reported. Unblocked by "
        + "an API-key-reachable project-treasury read on AZOA (W3.7 exposes only an operator route).";

    /// <summary>Strips every bucket balance and says why, leaving the policy percentages intact.</summary>
    public static TreasuryStatusDto WithoutChainBalance(TreasuryStatusDto dto, string reason) =>
        dto with
        {
            IndexFundBalance = null,
            LiquidReserveBalance = null,
            OperationsBalance = null,
            IsChainBacked = false,
            ChainBalanceMinorUnits = null,
            TreasuryAddress = null,
            BalanceObservedAt = null,
            BalanceUnavailableReason = reason,
        };

    // There is deliberately no "WithChainBalance" projection yet. The split can only be a view
    // of a balance once something can actually read one, and no route on this service names a
    // project to read. Writing the projection now would leave a helper nothing calls - a
    // control that exists in prose and not in the code path. It lands with the project-scoped
    // treasury route (W8.2 / W10.1), which owns this file next.
}

/// <summary>
/// The treasury READ-MODEL and its receipts. ArdaNova records what moved; the node holds it.
/// </summary>
/// <remarks>
/// Every row this service writes is a receipt (<c>PlatformTreasuryTransaction</c>), carrying an
/// exact minor-unit amount and, where one exists, the AZOA operation it attests to. The bucket
/// balances persisted on <c>PlatformTreasury</c> remain the ARDA backing inputs the rate is
/// derived from; they are NOT a project's money, and <see cref="GetStatusAsync"/> refuses to
/// present them as a chain balance.
/// <para>
/// <see cref="ReconcileAsync"/> is the control: it compares ArdaNova's receipts against the
/// node's treasury balance and FAILS when it cannot read the node, because a reconciliation
/// that reconciled nothing must never report success.
/// </para>
/// </remarks>
public class TreasuryService : ITreasuryService
{
    private readonly IRepository<PlatformTreasury> _treasuryRepository;
    private readonly IRepository<PlatformTreasuryTransaction> _transactionRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IAzoaProjectTreasuryReadModel? _projectTreasuryReadModel;
    private readonly IRepository<ProjectTokenConfig>? _projectTokenConfigRepository;

    /// <param name="projectTreasuryReadModel">
    /// The node read-model. Optional in the constructor only so the type still constructs
    /// without a container; when it is absent every balance-of-record read REFUSES rather than
    /// falling back to a database number.
    /// </param>
    public TreasuryService(
        IRepository<PlatformTreasury> treasuryRepository,
        IRepository<PlatformTreasuryTransaction> transactionRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IAzoaProjectTreasuryReadModel? projectTreasuryReadModel = null,
        IRepository<ProjectTokenConfig>? projectTokenConfigRepository = null)
    {
        _treasuryRepository = treasuryRepository;
        _transactionRepository = transactionRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _projectTreasuryReadModel = projectTreasuryReadModel;
        _projectTokenConfigRepository = projectTokenConfigRepository;
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

        // Fix the scale at the door. An amount that cannot be stated exactly in cents is
        // refused, never rounded into the treasury.
        if (!Money.TryToMinorUnits((decimal)usdAmount, out var inflowMinorUnits) || inflowMinorUnits <= 0)
        {
            return Result<bool>.ValidationError(
                "Funding amount must be an exact number of cents.");
        }

        var treasury = await GetOrCreateTreasuryAsync(ct);
        var inflow = Money.FromMinorUnits(inflowMinorUnits);

        var indexPct = (decimal)treasury.indexFundAllocationPct;
        var liquidPct = (decimal)treasury.liquidReserveAllocationPct;
        var operationsPct = (decimal)treasury.operationsAllocationPct;

        // The split is the policy view of one inflow. Cents are assigned exactly and the
        // rounding remainder goes to operations, so the three receipts always sum to the inflow.
        Money.TryToMinorUnitsFloor(inflow * indexPct, out var indexMinorUnits);
        Money.TryToMinorUnitsFloor(inflow * liquidPct, out var liquidMinorUnits);
        var operationsMinorUnits = inflowMinorUnits - indexMinorUnits - liquidMinorUnits;

        var indexAmount = Money.FromMinorUnits(indexMinorUnits);
        var liquidAmount = Money.FromMinorUnits(liquidMinorUnits);
        var operationsAmount = Money.FromMinorUnits(operationsMinorUnits);

        var indexBalance = (decimal)treasury.indexFundBalance + indexAmount;
        var liquidBalance = (decimal)treasury.liquidReserveBalance + liquidAmount;
        var operationsBalance = (decimal)treasury.operationsBalance + operationsAmount;

        treasury.indexFundBalance = Money.ToDisplayDouble(indexBalance);
        treasury.liquidReserveBalance = Money.ToDisplayDouble(liquidBalance);
        treasury.operationsBalance = Money.ToDisplayDouble(operationsBalance);
        treasury.totalInflows = Money.ToDisplayDouble((decimal)treasury.totalInflows + inflow);
        treasury.updatedAt = DateTime.UtcNow;

        await _treasuryRepository.UpdateAsync(treasury, ct);

        var projectTreasuryAddress = await TryResolveTreasuryAddressAsync(projectId, ct);
        var now = DateTime.UtcNow;

        var receipts = new[]
        {
            NewReceipt(
                PlatformTreasuryTransactionType.ALLOCATION_INDEX,
                indexMinorUnits,
                indexBalance,
                projectId != null ? $"Index fund allocation from project {projectId}" : "Index fund allocation",
                projectId,
                projectTreasuryAddress,
                toBucket: "index",
                createdAt: now),
            NewReceipt(
                PlatformTreasuryTransactionType.ALLOCATION_LIQUID,
                liquidMinorUnits,
                liquidBalance,
                projectId != null ? $"Liquid reserve allocation from project {projectId}" : "Liquid reserve allocation",
                projectId,
                projectTreasuryAddress,
                toBucket: "liquid",
                createdAt: now),
            NewReceipt(
                PlatformTreasuryTransactionType.ALLOCATION_OPS,
                operationsMinorUnits,
                operationsBalance,
                projectId != null ? $"Operations allocation from project {projectId}" : "Operations allocation",
                projectId,
                projectTreasuryAddress,
                toBucket: "operations",
                createdAt: now),
        };

        foreach (var receipt in receipts)
        {
            await _transactionRepository.AddAsync(receipt, ct);
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

        var indexBalance = (decimal)treasury.indexFundBalance;
        var monthlyReturn = indexBalance * (decimal)treasury.indexFundAnnualReturn / 12m;
        var profitShare = monthlyReturn * (decimal)treasury.platformProfitSharePct;

        // Whole cents only: the remainder stays in the index fund rather than evaporating.
        if (!Money.TryToMinorUnitsFloor(monthlyReturn, out var monthlyReturnMinorUnits)
            || !Money.TryToMinorUnitsFloor(profitShare, out var profitShareMinorUnits))
        {
            return Result<bool>.Failure(
                "The index fund return is outside the range this platform can represent exactly.");
        }

        var monthlyReturnExact = Money.FromMinorUnits(monthlyReturnMinorUnits);
        var profitShareExact = Money.FromMinorUnits(profitShareMinorUnits);

        var newIndexBalance = indexBalance + (monthlyReturnExact - profitShareExact);
        var newOperationsBalance = (decimal)treasury.operationsBalance + profitShareExact;

        treasury.indexFundBalance = Money.ToDisplayDouble(newIndexBalance);
        treasury.operationsBalance = Money.ToDisplayDouble(newOperationsBalance);
        treasury.updatedAt = DateTime.UtcNow;

        await _treasuryRepository.UpdateAsync(treasury, ct);

        var now = DateTime.UtcNow;

        await _transactionRepository.AddAsync(
            NewReceipt(
                PlatformTreasuryTransactionType.INDEX_RETURN,
                monthlyReturnMinorUnits,
                newIndexBalance,
                $"Monthly index fund return applied ({treasury.indexFundAnnualReturn:P2} annual)",
                relatedProjectId: null,
                projectTreasuryAddress: null,
                toBucket: "index",
                createdAt: now),
            ct);

        await _transactionRepository.AddAsync(
            NewReceipt(
                PlatformTreasuryTransactionType.PROFIT_SHARE,
                profitShareMinorUnits,
                newOperationsBalance,
                $"Platform profit share from index returns ({treasury.platformProfitSharePct:P2})",
                relatedProjectId: null,
                projectTreasuryAddress: null,
                toBucket: "operations",
                createdAt: now),
            ct);

        await _unitOfWork.SaveChangesAsync(ct);

        return Result<bool>.Success(true);
    }

    public async Task<Result<double>> RebalanceIfNeededAsync(double requiredLiquid, CancellationToken ct = default)
    {
        var treasury = await GetOrCreateTreasuryAsync(ct);

        var liquidBalance = (decimal)treasury.liquidReserveBalance;
        var required = (decimal)requiredLiquid;

        if (liquidBalance >= required)
        {
            return Result<double>.Success(0);
        }

        var indexBalance = (decimal)treasury.indexFundBalance;
        var deficit = required - liquidBalance;
        var transferAmount = Math.Min(deficit, indexBalance);

        // A transfer is only ever whole cents; a sub-cent deficit is not a rebalance.
        if (!Money.TryToMinorUnitsFloor(transferAmount, out var transferMinorUnits) || transferMinorUnits <= 0)
        {
            return Result<double>.Failure("Insufficient index fund balance to rebalance");
        }

        var transferExact = Money.FromMinorUnits(transferMinorUnits);
        var newLiquidBalance = liquidBalance + transferExact;

        treasury.indexFundBalance = Money.ToDisplayDouble(indexBalance - transferExact);
        treasury.liquidReserveBalance = Money.ToDisplayDouble(newLiquidBalance);
        treasury.totalRebalanceTransfers = Money.ToDisplayDouble(
            (decimal)treasury.totalRebalanceTransfers + transferExact);
        treasury.lastRebalanceAt = DateTime.UtcNow;
        treasury.updatedAt = DateTime.UtcNow;

        await _treasuryRepository.UpdateAsync(treasury, ct);

        var receipt = NewReceipt(
            PlatformTreasuryTransactionType.REBALANCE,
            transferMinorUnits,
            newLiquidBalance,
            $"Rebalanced {transferExact:N2} from index fund to liquid reserve (deficit: {deficit:N2})",
            relatedProjectId: null,
            projectTreasuryAddress: null,
            toBucket: "liquid",
            createdAt: DateTime.UtcNow);
        receipt.fromBucket = "index";

        await _transactionRepository.AddAsync(receipt, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return Result<double>.Success(Money.ToDisplayDouble(transferExact));
    }

    /// <summary>
    /// Compares ArdaNova's receipts against what the node says each project's treasury holds.
    /// </summary>
    /// <remarks>
    /// This REFUSES when no node read-model is wired. A reconciliation whose only source is the
    /// same database it is checking proves nothing, and reporting success for it is how a
    /// database number came to be trusted as money.
    /// </remarks>
    public async Task<Result<bool>> ReconcileAsync(CancellationToken ct = default)
    {
        if (_projectTreasuryReadModel is null || _projectTokenConfigRepository is null)
        {
            return Result<bool>.Failure(
                "Treasury reconciliation is unavailable: no node treasury read is wired, and a "
                + "reconciliation against ArdaNova's own rows proves nothing. "
                + TreasuryPolicyView.NoChainReadConfigured);
        }

        var treasury = await GetOrCreateTreasuryAsync(ct);

        var configs = await _projectTokenConfigRepository.FindAsync(
            config => config.treasuryReference != null && config.treasuryChain != null, ct);

        var drifted = new List<string>();
        var unreadable = new List<string>();

        foreach (var config in configs)
        {
            var balance = await _projectTreasuryReadModel.GetBalanceAsync(
                config.treasuryChain!, config.treasuryReference!, config.assetId, ct);

            if (!balance.IsSuccess || balance.Value is null)
            {
                unreadable.Add(config.projectId);
                continue;
            }

            var receipts = await _transactionRepository.FindAsync(
                row => row.relatedProjectId == config.projectId, ct);
            var recordedMinorUnits = receipts
                .Where(row => row.type == PlatformTreasuryTransactionType.FUNDING_INFLOW)
                .Sum(row => row.amountMinorUnits);

            if (recordedMinorUnits != balance.Value.BalanceBaseUnits)
                drifted.Add(config.projectId);
        }

        if (unreadable.Count > 0)
        {
            return Result<bool>.Failure(
                $"Treasury reconciliation could not read {unreadable.Count} project treasuries on the node; "
                + "no reconciliation is recorded.");
        }

        treasury.lastReconciliationAt = DateTime.UtcNow;
        treasury.updatedAt = DateTime.UtcNow;
        await _treasuryRepository.UpdateAsync(treasury, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return drifted.Count == 0
            ? Result<bool>.Success(true)
            : Result<bool>.Conflict(
                $"Treasury reconciliation found {drifted.Count} projects whose receipts disagree with the node.");
    }

    public async Task<Result<TreasuryStatusDto>> GetStatusAsync(CancellationToken ct = default)
    {
        var treasury = await GetOrCreateTreasuryAsync(ct);
        var dto = _mapper.Map<TreasuryStatusDto>(treasury);

        // No project is named on this route, so there is no single treasury account to read.
        // The percentages are real configuration; the balances are not reported at all.
        return Result<TreasuryStatusDto>.Success(
            TreasuryPolicyView.WithoutChainBalance(
                dto,
                _projectTreasuryReadModel is null
                    ? TreasuryPolicyView.NoChainReadConfigured
                    : TreasuryPolicyView.NoChainReadOnThisRoute));
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

    /// <summary>Builds a receipt whose exact figure is the minor-unit amount; the double is its projection.</summary>
    private static PlatformTreasuryTransaction NewReceipt(
        PlatformTreasuryTransactionType type,
        long amountMinorUnits,
        decimal balanceAfter,
        string description,
        string? relatedProjectId,
        string? projectTreasuryAddress,
        string? toBucket,
        DateTime createdAt)
    {
        Money.TryToMinorUnitsFloor(balanceAfter, out var balanceAfterMinorUnits);

        return new PlatformTreasuryTransaction
        {
            id = Guid.NewGuid().ToString(),
            type = type,
            amountMinorUnits = amountMinorUnits,
            amount = Money.ToDisplayDouble(Money.FromMinorUnits(amountMinorUnits)),
            balanceAfterMinorUnits = balanceAfterMinorUnits,
            balanceAfter = Money.ToDisplayDouble(Money.FromMinorUnits(balanceAfterMinorUnits)),
            description = description,
            relatedProjectId = relatedProjectId,
            projectTreasuryAddress = projectTreasuryAddress,
            toBucket = toBucket,
            createdAt = createdAt,
        };
    }

    /// <summary>The project's node-side treasury address, when W5.2's provisioning recorded one.</summary>
    private async Task<string?> TryResolveTreasuryAddressAsync(string? projectId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(projectId) || _projectTokenConfigRepository is null)
            return null;

        var config = await _projectTokenConfigRepository.FindOneAsync(
            row => row.projectId == projectId, ct);
        return config?.treasuryAddress;
    }

    private async Task<PlatformTreasury> GetOrCreateTreasuryAsync(CancellationToken ct = default)
    {
        var treasuries = await _treasuryRepository.GetAllAsync(ct);
        var treasury = treasuries.FirstOrDefault();

        if (treasury == null)
        {
            // Default treasury: policy percentages only. Balances start at zero because nothing
            // has been credited, and the genesis seed is the node's to set, not ArdaNova's.
            treasury = new PlatformTreasury
            {
                id = Guid.NewGuid().ToString(),
                indexFundBalance = 0,
                liquidReserveBalance = 0,
                operationsBalance = 0,
                ardaTotalSupply = 0,
                ardaCirculatingSupply = 0,
                indexFundAllocationPct = 0.60,
                liquidReserveAllocationPct = 0.30,
                operationsAllocationPct = 0.10,
                indexFundAnnualReturn = 0.12,
                platformProfitSharePct = 0.20,
                trustProtectionRate = 0.30,
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
