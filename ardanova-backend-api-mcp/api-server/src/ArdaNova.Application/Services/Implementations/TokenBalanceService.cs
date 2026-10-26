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

/// <summary>Units a holder's awards still hold back, and the date that ends the last of them.</summary>
public sealed record UnitWindowRestriction(long RestrictedUnits, DateTimeOffset? RestrictedUntil)
{
    /// <summary>Nothing is held back.</summary>
    public static UnitWindowRestriction Free { get; } = new(0, null);
}

/// <summary>
/// Whether a holding may move, and the ONLY release date that may be shown for it.
/// </summary>
/// <param name="IsLiquid">True when at least some of the holding may move right now.</param>
/// <param name="DatedRelease">
/// The date the hold ends, or null when nothing dates it - including when the project's gate is
/// part of the hold, because a gate moves on an outcome and not on a date.
/// </param>
public readonly record struct HoldingLiquidity(bool IsLiquid, DateTimeOffset? DatedRelease)
{
    /// <summary>Nothing holds this holding back, so there is no date to give.</summary>
    public static HoldingLiquidity Free { get; } = new(true, null);
}

/// <summary>
/// The ONE rule for the worker's own restricted window: what it holds back, and until when.
/// </summary>
/// <remarks>
/// The window is the second hold an approved award is continued into. It decides whether a
/// person's units may move YET; <see cref="TokenLiquidityPolicy"/> decides whether that holder
/// class may EVER move them at this gate. Units are free only when both agree.
/// <para>
/// Every seam that spends or displays units reads this rule rather than a stored flag, because a
/// flag is stamped once and a window ends on a date: a column written at approval is false
/// forever, and a column written at gate activation is true too early. Telling a worker "you can
/// move this from 14 March" and then reading their units as liquid today is the platform not
/// knowing its own rules.
/// </para>
/// </remarks>
public static class UnitWindow
{
    /// <summary>True once an award's own window has ended - or never opened at all.</summary>
    /// <remarks>
    /// An award that is held with NO release date is restricted rather than free: a promise
    /// nobody can date is one nobody may spend.
    /// </remarks>
    public static bool HasElapsed(TokenAllocation award, DateTimeOffset now)
    {
        if (!award.IsAwaitingWorkerWindow)
            return true;

        return award.workerHoldReleasesAt is { } releasesAt && AsUtc(releasesAt) <= now;
    }

    /// <summary>Whether ONE award may move: the gate says ever, the window says yet.</summary>
    public static bool IsAwardLiquid(
        TokenAllocation award, ProjectGateStatus gateStatus, DateTimeOffset now)
        => TokenLiquidityPolicy.IsLiquid(award.holderClass, gateStatus) && HasElapsed(award, now);

    /// <summary>
    /// The date a hold ENDS, or null when something UNDATED is also holding these units.
    /// </summary>
    /// <remarks>
    /// <c>workerHoldReleasesAt</c> is stamped at award approval and knows nothing about the
    /// project's gate, so a worker on a still-FUNDING project has a real date sitting on units
    /// the gate ALSO refuses to release. Showing that date would promise a release the gate does
    /// not honour: the window ends on 14 March, the gate may never move at all, and "Available
    /// from 14 March" would be a date this platform cannot keep. A date is only publishable when
    /// the window is the WHOLE of the remaining hold - which is exactly what
    /// <see cref="TokenLiquidityPolicy.IsLiquid"/> being true means. This is the same rule
    /// <c>PayoutService.DescribeIlliquidity</c> states in the holder's own words; both read it
    /// here so there is one rule rather than two that can drift.
    /// </remarks>
    public static DateTimeOffset? DatedReleaseFor(
        TokenHolderClass holderClass,
        ProjectGateStatus gateStatus,
        UnitWindowRestriction restriction)
        => TokenLiquidityPolicy.IsLiquid(holderClass, gateStatus)
            ? restriction.RestrictedUntil
            : null;

    /// <summary>What a holder's awards still hold back, summed over the awards themselves.</summary>
    public static UnitWindowRestriction Restrict(IEnumerable<TokenAllocation> awards, DateTimeOffset now)
    {
        var restrictedUnits = 0L;
        DateTimeOffset? until = null;

        foreach (var award in awards)
        {
            if (!award.IsAwaitingWorkerWindow || HasElapsed(award, now))
                continue;

            restrictedUnits += award.tokenAmount;

            if (award.workerHoldReleasesAt is not { } releasesAt)
                continue;

            var moment = AsUtc(releasesAt);
            if (until is null || moment > until)
                until = moment;
        }

        return restrictedUnits == 0 && until is null
            ? UnitWindowRestriction.Free
            : new UnitWindowRestriction(restrictedUnits, until);
    }

    /// <summary>Reads one holder's window for one project's units straight off their awards.</summary>
    public static async Task<UnitWindowRestriction> ReadAsync(
        IRepository<TokenAllocation> awards,
        string userId,
        string projectTokenConfigId,
        DateTimeOffset now,
        CancellationToken ct)
    {
        var held = await awards.FindAsync(
            award => award.recipientUserId == userId
                && award.projectTokenConfigId == projectTokenConfigId
                && award.workerHoldId != null,
            ct);

        return Restrict(held, now);
    }

    /// <summary>Reads a stored timestamp as the UTC instant it always was.</summary>
    internal static DateTimeOffset AsUtc(DateTime value)
        => new(DateTime.SpecifyKind(value, DateTimeKind.Utc));
}

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
    private readonly IRepository<TokenAllocation>? _allocationRepository;

    /// <param name="holdingsReadModel">
    /// The node read-model. Optional in the constructor only so the type still constructs without
    /// a container; when it is absent an ARDA read REFUSES instead of falling back to a row.
    /// </param>
    /// <param name="allocationRepository">
    /// The awards the worker's restricted window is read off. Optional in the constructor only so
    /// the type still constructs without a container; the container always supplies it through the
    /// open generic repository registration, so a deployment never takes the "no awards readable"
    /// branch - a hand-built instance does.
    /// </param>
    public TokenBalanceService(
        IRepository<TokenBalance> tokenBalanceRepository,
        IRepository<ProjectTokenConfig> projectTokenConfigRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IAzoaHoldingsReadModel? holdingsReadModel = null,
        IRepository<User>? userRepository = null,
        IRepository<PlatformTreasury>? treasuryRepository = null,
        IRepository<TokenAllocation>? allocationRepository = null)
    {
        _tokenBalanceRepository = tokenBalanceRepository;
        _projectTokenConfigRepository = projectTokenConfigRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _holdingsReadModel = holdingsReadModel;
        _userRepository = userRepository;
        _treasuryRepository = treasuryRepository;
        _allocationRepository = allocationRepository;
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
        return Result<TokenBalanceDto>.Success(await WithLiveLiquidityAsync(dto, ct));
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

        // isPlatformToken rows are ArdaNova bookkeeping, never a person's ARDA; the ARDA leg
        // below is the only place a platform balance comes from.
        var holdings = _mapper.Map<List<TokenBalanceDto>>(
            balances.Where(row => !row.isPlatformToken).ToList());
        for (var index = 0; index < holdings.Count; index++)
            holdings[index] = await WithLiveLiquidityAsync(holdings[index], ct);

        var dto = new UserPortfolioDto
        {
            UserId = userId,
            Holdings = holdings,
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
        var isNew = balance == null;

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

        // A stamp of the answer as it stood when the units arrived, kept so a stored row is
        // readable on its own. It is NOT the authority: the window it folds in ends on a date
        // nothing revisits, so every read restates it through WithLiveLiquidityAsync and every
        // spend seam subtracts the window itself.
        var liquidResult = await IsBalanceLiquidAsync(userId, projectTokenConfigId, holderClass, ct);
        if (liquidResult.IsSuccess)
        {
            balance.isLiquid = liquidResult.Value;
        }

        // Update() on a just-Added row re-marks it Modified, so the first credit became an
        // UPDATE of a row that did not exist (0 rows -> DbUpdateConcurrencyException).
        if (!isNew)
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

    /// <summary>Whether any of this holding may move right now: the gate AND the worker's window.</summary>
    /// <remarks>
    /// Two rules, both required. <see cref="TokenLiquidityPolicy"/> answers whether this holder
    /// class may ever move units at this gate; <see cref="UnitWindow"/> answers whether the
    /// person's own awards still hold them back. A holding whose EVERY unit is still inside the
    /// window is not liquid however wide the gate is open - which is what stops a card reading
    /// "liquid" beside a sentence promising the units from a future date.
    /// <para>
    /// This is a display and eligibility answer, not the spend ceiling. How MANY units may move is
    /// a subtraction, not a boolean, and every spend seam does it against
    /// <see cref="UnitWindowRestriction.RestrictedUnits"/> rather than against this flag.
    /// </para>
    /// </remarks>
    public async Task<Result<bool>> IsBalanceLiquidAsync(
        string userId,
        string projectTokenConfigId,
        TokenHolderClass holderClass,
        CancellationToken ct = default)
    {
        var reading = await ReadLiquidityAsync(userId, projectTokenConfigId, holderClass, ct);
        return reading.IsSuccess
            ? Result<bool>.Success(reading.Value.IsLiquid)
            : Result<bool>.Failure(reading.Error!);
    }

    /// <summary>
    /// One holding's liquidity AND the only release date that may be published for it.
    /// </summary>
    /// <remarks>
    /// Both answers come out of one reading because they are one decision. Computing "is it
    /// held?" here and "until when?" somewhere else is how a held holding came to be stamped
    /// with a window date it could not keep: the gate refused the units, and a second, separate
    /// window read then dated the refusal anyway. <see cref="UnitWindow.DatedReleaseFor"/> is the
    /// rule, and it is applied at the same counter that decides the flag.
    /// </remarks>
    private async Task<Result<HoldingLiquidity>> ReadLiquidityAsync(
        string userId,
        string projectTokenConfigId,
        TokenHolderClass holderClass,
        CancellationToken ct)
    {
        Expression<Func<TokenBalance, bool>> balancePredicate = tb =>
            tb.userId == userId &&
            tb.projectTokenConfigId == projectTokenConfigId &&
            tb.holderClass == holderClass;

        var balance = await _tokenBalanceRepository.FindOneAsync(balancePredicate, ct);
        if (balance != null && balance.isPlatformToken)
        {
            return Result<HoldingLiquidity>.Success(HoldingLiquidity.Free);
        }

        var config = await _projectTokenConfigRepository.GetByIdAsync(projectTokenConfigId, ct);
        if (config == null)
        {
            return Result<HoldingLiquidity>.Failure("Project token configuration not found");
        }

        var restriction = await ReadWindowAsync(userId, projectTokenConfigId, ct);

        // The date is decided by the SAME predicate that decides the flag, so a gate hold can
        // never leave here carrying a window date.
        var datedRelease = UnitWindow.DatedReleaseFor(holderClass, config.gateStatus, restriction);

        if (!TokenLiquidityPolicy.IsLiquid(holderClass, config.gateStatus))
        {
            return Result<HoldingLiquidity>.Success(new HoldingLiquidity(false, datedRelease));
        }

        if (restriction.RestrictedUnits <= 0)
        {
            return Result<HoldingLiquidity>.Success(HoldingLiquidity.Free);
        }

        // Some of the holding may already be free while the rest sits in a dated window. The gate
        // is open on this branch, so the date is one the platform can keep either way.
        return Result<HoldingLiquidity>.Success(new HoldingLiquidity(
            restriction.RestrictedUnits < (balance?.balance ?? 0),
            datedRelease));
    }

    /// <summary>This holder's restricted window for one project's units.</summary>
    private async Task<UnitWindowRestriction> ReadWindowAsync(
        string userId, string projectTokenConfigId, CancellationToken ct)
        => _allocationRepository is null
            ? UnitWindowRestriction.Free
            : await UnitWindow.ReadAsync(
                _allocationRepository, userId, projectTokenConfigId, DateTimeOffset.UtcNow, ct);

    /// <summary>
    /// Restates a holding's liquidity from the live gate and window rather than the stored stamp.
    /// </summary>
    /// <remarks>
    /// The column is written when units are credited and when a gate moves; the window ends on a
    /// date nothing revisits, so the stamp goes stale in both directions. Where the gate cannot be
    /// read the stamp STANDS - nothing here invents an answer it does not have.
    /// </remarks>
    private async Task<TokenBalanceDto> WithLiveLiquidityAsync(TokenBalanceDto dto, CancellationToken ct)
    {
        if (dto.IsPlatformToken
            || string.IsNullOrWhiteSpace(dto.ProjectTokenConfigId)
            || string.IsNullOrWhiteSpace(dto.UserId)
            || dto.HolderClass is not { } holderClass)
        {
            return dto;
        }

        var reading = await ReadLiquidityAsync(
            dto.UserId, dto.ProjectTokenConfigId!, holderClass, ct);

        if (!reading.IsSuccess)
            return dto;

        // The date the window ends is what a person actually needs to be told, and it already
        // exists on their own awards - it was simply never carried to the wire, which is why the
        // badge read "not available yet" with nothing after it. It is null whenever the hold
        // carries no date AND whenever the gate is part of the hold: a promise nobody can date is
        // one nobody may spend, and a date the gate does not honour is worse than none.
        return dto with
        {
            IsLiquid = reading.Value.IsLiquid,
            RestrictedUntil = reading.Value.DatedRelease,
        };
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
