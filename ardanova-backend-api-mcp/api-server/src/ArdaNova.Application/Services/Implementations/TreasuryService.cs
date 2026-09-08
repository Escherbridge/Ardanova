using System.Globalization;
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

    /// <summary>
    /// Presents the three buckets as what they now are: a POLICY VIEW of the one balance the
    /// node reported for this project's treasury account.
    /// </summary>
    /// <remarks>
    /// Cents are assigned exactly and the rounding remainder goes to operations, so the three
    /// views always sum back to the observed balance. A negative balance is impossible on a
    /// chain read, so it is reported as unavailable rather than split into negative buckets.
    /// </remarks>
    public static TreasuryStatusDto WithChainBalance(
        TreasuryStatusDto dto,
        long chainBalanceMinorUnits,
        string? treasuryAddress,
        DateTimeOffset observedAt)
    {
        if (chainBalanceMinorUnits < 0)
            return WithoutChainBalance(dto, NoChainReadConfigured);

        var balance = Money.FromMinorUnits(chainBalanceMinorUnits);
        Money.TryToMinorUnitsFloor(balance * (decimal)dto.IndexFundAllocationPct, out var indexMinorUnits);
        Money.TryToMinorUnitsFloor(balance * (decimal)dto.LiquidReserveAllocationPct, out var liquidMinorUnits);
        var operationsMinorUnits = chainBalanceMinorUnits - indexMinorUnits - liquidMinorUnits;

        return dto with
        {
            IndexFundBalance = Money.FromMinorUnits(indexMinorUnits),
            LiquidReserveBalance = Money.FromMinorUnits(liquidMinorUnits),
            OperationsBalance = Money.FromMinorUnits(operationsMinorUnits),
            IsChainBacked = true,
            ChainBalanceMinorUnits = chainBalanceMinorUnits,
            TreasuryAddress = treasuryAddress,
            BalanceObservedAt = observedAt,
            BalanceUnavailableReason = null,
        };
    }
}

/// <summary>What a project can pay for its own units right now, and on what terms.</summary>
/// <remarks>
/// Every figure here is a READ of the project's treasury account on the node plus the rate
/// ArdaNova declares. Nothing is a stored balance, and a figure that could not be read is null
/// with <see cref="UnavailableReason"/> set - never a zero that reads as "no funds".
/// </remarks>
/// <param name="TreasuryFundsMinorUnits">
/// What the project's treasury holds of the platform unit, in minor units. ARDA carries two
/// decimals, so its base units and its minor units are the same number.
/// </param>
/// <param name="MaxWholeUnitsFillableNow">Whole project units those funds could buy back at the declared rate.</param>
/// <param name="Disclosure">The sentence a seller is shown before they commit. Plain words, no market terms.</param>
public sealed record ProjectBuyBackFigures(
    string ProjectId,
    string? TreasuryAddress,
    bool IsChainBacked,
    long? TreasuryFundsMinorUnits,
    DateTimeOffset? ObservedAt,
    string? ArdaPerWholeProjectUnit,
    string? UsdMajorUnitsPerWholeProjectUnit,
    int ProjectUnitDecimals,
    long? MaxWholeUnitsFillableNow,
    string? RateSource,
    DateTimeOffset? RateDeclaredAt,
    string Disclosure,
    string? UnavailableReason);

/// <summary>The sentences a seller is shown. Constants so the wording cannot drift per caller.</summary>
public static class BuyBackDisclosures
{
    /// <summary>The whole promise, in one sentence a non-financial person can hold.</summary>
    public const string FilledFromProjectFunds =
        "Sell requests are filled from this project's own funds. When those funds are short, "
        + "your request is recorded and filled in turn.";

    /// <summary>Stamped when the project has no treasury account on the node yet.</summary>
    public const string NoTreasuryYet =
        "This project cannot buy units back yet: it has no funds account of its own.";

    /// <summary>Stamped when the platform unit has not been configured.</summary>
    public const string NoPlatformUnit =
        "This platform has no funds unit configured, so no buy-back figure can be quoted.";
}

/// <summary>
/// The project-scoped treasury reads: the balance of record, and what a buy-back would pay.
/// </summary>
/// <remarks>
/// A separate port rather than more members on <c>ITreasuryService</c>, whose interface file
/// belongs to another wave item. <c>TreasuryService</c> implements both, so the existing
/// <c>AddScoped&lt;ITreasuryService, TreasuryService&gt;</c> registration already produces an
/// instance that satisfies this port too.
/// </remarks>
public interface IProjectBuyBackReadModel
{
    /// <summary>The platform's policy split, applied to the balance the node reports for one project.</summary>
    Task<Result<TreasuryStatusDto>> GetProjectStatusAsync(string projectId, CancellationToken ct = default);

    /// <summary>The declared figures the node's buy-back policy is asked to honour for one project.</summary>
    Task<Result<ProjectBuyBackFigures>> GetBuyBackFiguresAsync(string projectId, CancellationToken ct = default);
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
public class TreasuryService : ITreasuryService, IProjectBuyBackReadModel
{
    private readonly IRepository<PlatformTreasury> _treasuryRepository;
    private readonly IRepository<PlatformTreasuryTransaction> _transactionRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IAzoaProjectTreasuryReadModel? _projectTreasuryReadModel;
    private readonly IRepository<ProjectTokenConfig>? _projectTokenConfigRepository;
    private readonly IProjectUnitRateSource? _projectUnitRateSource;

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
        IRepository<ProjectTokenConfig>? projectTokenConfigRepository = null,
        IProjectUnitRateSource? projectUnitRateSource = null)
    {
        _treasuryRepository = treasuryRepository;
        _transactionRepository = transactionRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _projectTreasuryReadModel = projectTreasuryReadModel;
        _projectTokenConfigRepository = projectTokenConfigRepository;
        _projectUnitRateSource = projectUnitRateSource;
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

    /// <inheritdoc />
    public async Task<Result<TreasuryStatusDto>> GetProjectStatusAsync(
        string projectId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(projectId))
            return Result<TreasuryStatusDto>.ValidationError("A project id is required.");

        var treasury = await GetOrCreateTreasuryAsync(ct);
        var dto = _mapper.Map<TreasuryStatusDto>(treasury);

        var read = await ReadProjectTreasuryAsync(projectId, treasury.ardaAssetId, ct);
        var projectRead = read.Value;
        if (!read.IsSuccess || projectRead is null)
        {
            // The reason travels with the status. A project whose treasury could not be read is
            // reported as unreadable, never as a treasury holding nothing.
            return Result<TreasuryStatusDto>.Success(
                TreasuryPolicyView.WithoutChainBalance(
                    dto, read.Error ?? TreasuryPolicyView.NoChainReadConfigured));
        }

        return Result<TreasuryStatusDto>.Success(
            TreasuryPolicyView.WithChainBalance(
                dto,
                projectRead.Balance.BalanceBaseUnits,
                projectRead.Balance.Address ?? projectRead.Config.treasuryAddress,
                projectRead.Balance.ObservedAt));
    }

    /// <inheritdoc />
    public async Task<Result<ProjectBuyBackFigures>> GetBuyBackFiguresAsync(
        string projectId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(projectId))
            return Result<ProjectBuyBackFigures>.ValidationError("A project id is required.");

        var treasury = await GetOrCreateTreasuryAsync(ct);

        var read = await ReadProjectTreasuryAsync(projectId, treasury.ardaAssetId, ct);
        var projectRead = read.Value;
        if (!read.IsSuccess || projectRead is null)
            return Result<ProjectBuyBackFigures>.Success(Unavailable(projectId, read.Error));

        var config = projectRead.Config;
        var balance = projectRead.Balance;

        if (_projectUnitRateSource is null)
        {
            return Result<ProjectBuyBackFigures>.Success(
                Unavailable(projectId, "No rate source is wired, so no buy-back price can be quoted.")
                with
                {
                    TreasuryAddress = balance.Address ?? config.treasuryAddress,
                    IsChainBacked = true,
                    TreasuryFundsMinorUnits = balance.BalanceBaseUnits,
                    ObservedAt = balance.ObservedAt,
                });
        }

        var rate = await _projectUnitRateSource.GetDeclaredRateAsync(config.id, ct);
        if (!rate.IsSuccess || rate.Value is null)
        {
            return Result<ProjectBuyBackFigures>.Success(
                Unavailable(projectId, rate.Error ?? "This project has no declared price yet.")
                with
                {
                    TreasuryAddress = balance.Address ?? config.treasuryAddress,
                    IsChainBacked = true,
                    TreasuryFundsMinorUnits = balance.BalanceBaseUnits,
                    ObservedAt = balance.ObservedAt,
                });
        }

        var declared = rate.Value;
        var fillable = MaxWholeUnitsFillable(balance.BalanceBaseUnits, declared.ArdaPerWholeProjectUnit);

        return Result<ProjectBuyBackFigures>.Success(new ProjectBuyBackFigures(
            ProjectId: projectId,
            TreasuryAddress: balance.Address ?? config.treasuryAddress,
            IsChainBacked: true,
            TreasuryFundsMinorUnits: balance.BalanceBaseUnits,
            ObservedAt: balance.ObservedAt,
            ArdaPerWholeProjectUnit: declared.ArdaPerWholeProjectUnit,
            UsdMajorUnitsPerWholeProjectUnit: declared.UsdMajorUnitsPerWholeProjectUnit,
            ProjectUnitDecimals: declared.ProjectUnitDecimals,
            MaxWholeUnitsFillableNow: fillable,
            RateSource: declared.Source,
            RateDeclaredAt: declared.DeclaredAt,
            Disclosure: BuyBackDisclosures.FilledFromProjectFunds,
            UnavailableReason: null));
    }

    /// <summary>Figures with nothing read, and the reason in the open. Never zeroes standing in for a read.</summary>
    private static ProjectBuyBackFigures Unavailable(string projectId, string? reason) =>
        new(
            ProjectId: projectId,
            TreasuryAddress: null,
            IsChainBacked: false,
            TreasuryFundsMinorUnits: null,
            ObservedAt: null,
            ArdaPerWholeProjectUnit: null,
            UsdMajorUnitsPerWholeProjectUnit: null,
            ProjectUnitDecimals: 0,
            MaxWholeUnitsFillableNow: null,
            RateSource: null,
            RateDeclaredAt: null,
            Disclosure: BuyBackDisclosures.FilledFromProjectFunds,
            UnavailableReason: reason ?? TreasuryPolicyView.NoChainReadConfigured);

    /// <summary>
    /// Whole project units the observed funds could buy back at the declared price, rounded
    /// DOWN. An unreadable or non-positive price quotes nothing rather than dividing by it.
    /// </summary>
    private static long? MaxWholeUnitsFillable(long treasuryFundsMinorUnits, string? ardaPerWholeUnitText)
    {
        if (treasuryFundsMinorUnits <= 0
            || !decimal.TryParse(
                ardaPerWholeUnitText,
                NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign,
                CultureInfo.InvariantCulture,
                out var ardaPerWholeUnit)
            || ardaPerWholeUnit <= 0m)
        {
            return null;
        }

        var funds = Money.FromMinorUnits(treasuryFundsMinorUnits);
        return Money.TryToWholeUnitsFloor(funds / ardaPerWholeUnit, out var wholeUnits) ? wholeUnits : null;
    }

    /// <summary>
    /// One project's node-side treasury and what it holds of the platform unit, or the reason
    /// no such read is possible. Never falls back to a stored number.
    /// </summary>
    private async Task<Result<ProjectTreasuryRead>> ReadProjectTreasuryAsync(
        string projectId, string? platformAssetId, CancellationToken ct)
    {
        if (_projectTreasuryReadModel is null || _projectTokenConfigRepository is null)
            return Result<ProjectTreasuryRead>.Failure(TreasuryPolicyView.NoChainReadConfigured);

        var config = await _projectTokenConfigRepository.FindOneAsync(row => row.projectId == projectId, ct);
        if (config is null)
        {
            return Result<ProjectTreasuryRead>.NotFound(
                $"No project token configuration exists for project {projectId}.");
        }

        if (string.IsNullOrWhiteSpace(config.treasuryChain) || string.IsNullOrWhiteSpace(config.treasuryReference))
            return Result<ProjectTreasuryRead>.Failure(BuyBackDisclosures.NoTreasuryYet);

        if (string.IsNullOrWhiteSpace(platformAssetId))
            return Result<ProjectTreasuryRead>.Failure(BuyBackDisclosures.NoPlatformUnit);

        // The project's FUNDS, not its own units: a buy-back is paid in the platform unit.
        var balance = await _projectTreasuryReadModel.GetBalanceAsync(
            config.treasuryChain!, config.treasuryReference!, platformAssetId, ct);

        return balance.IsSuccess && balance.Value is not null
            ? Result<ProjectTreasuryRead>.Success(new ProjectTreasuryRead(config, balance.Value))
            : Result<ProjectTreasuryRead>.Failure(
                balance.Error ?? "This project's funds could not be read right now.");
    }

    /// <summary>One project's configuration paired with what the node says its treasury holds.</summary>
    private sealed record ProjectTreasuryRead(ProjectTokenConfig Config, ProjectTreasuryBalance Balance);

}
