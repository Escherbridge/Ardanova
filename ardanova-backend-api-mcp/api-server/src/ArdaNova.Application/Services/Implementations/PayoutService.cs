using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using ArdaNova.Application.Common.Interfaces;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Interfaces;
using ArdaNova.Domain.Models.Entities;
using ArdaNova.Domain.Models.Enums;
using ArdaNova.Domain.Policies;
using ArdaNova.Domain.ValueObjects;
using AutoMapper;

namespace ArdaNova.Application.Services.Interfaces
{
    // The ports below are declared next to their only consumer, for the same reason
    // IAzoaExchangeNode is declared next to ExchangeService: one adapter, one caller, and no
    // interface file that exists only to be found.

    /// <summary>What a person can turn into money today, and why the rest is not ready yet.</summary>
    /// <remarks>Every amount is minor units of <see cref="CurrencyCode"/>. Nothing here is a unit count.</remarks>
    public sealed record CashOutAvailability(
        bool Available,
        bool DestinationReady,
        long CashableMinorUnits,
        long OnHoldMinorUnits,
        DateTimeOffset? AvailableFrom,
        long MinimumAmountMinorUnits,
        long MaximumAmountMinorUnits,
        string CurrencyCode,
        string StatusMessage);

    /// <summary>Where a person's money goes, and whether it can receive any yet.</summary>
    public sealed record CashOutDestination(
        bool Available,
        bool Opened,
        bool ReadyToReceive,
        bool DetailsSubmitted,
        int OutstandingRequirementCount,
        string? SetupUrl,
        DateTimeOffset? SetupUrlExpiresAt,
        string StatusMessage);

    /// <summary>One cash-out as the person sees it. No provider names, no chain words.</summary>
    public sealed record CashOutRecord(
        string Id,
        string Status,
        string StatusMessage,
        long AmountMinorUnits,
        string CurrencyCode,
        DateTimeOffset RequestedAt,
        DateTimeOffset? PaidAt,
        DateTimeOffset? ReversedAt);

    /// <summary>What a sale would pay, and whether it may go ahead. Moves nothing.</summary>
    /// <remarks>
    /// Produced by the SAME gates and the SAME arithmetic that a real sale runs, so a quote a
    /// person is shown and the sale they then confirm can never disagree.
    /// </remarks>
    public sealed record UnitSaleQuote(
        string ProjectTokenConfigId,
        string UnitName,
        int SourceUnitAmount,
        long PlatformMinorUnits,
        long UsdReferenceMinorUnits,
        long SellableUnitCount,
        DateTimeOffset? RestrictedUntil,
        bool CanSell,
        string StatusMessage);

    /// <summary>One project's units as they stand for one holder: sellable now, or held until a date.</summary>
    /// <remarks>
    /// <see cref="HolderClass"/> is nullable because a balance row can carry none, and every
    /// sellability rule is keyed on it. Such a holding is still LISTED - hiding somebody's units
    /// is worse than telling them why they cannot be sold - but it is never sellable, and no
    /// class is invented for it.
    /// </remarks>
    public sealed record SellableUnits(
        string ProjectTokenConfigId,
        string ProjectId,
        string UnitName,
        TokenHolderClass? HolderClass,
        long HeldUnits,
        long CommittedUnits,
        long RestrictedUnits,
        long SellableUnitCount,
        DateTimeOffset? RestrictedUntil,
        bool CanSell,
        string StatusMessage);

    /// <summary>
    /// Application-layer PORT onto the node's cash-out door - the money half of the off-ramp.
    /// </summary>
    /// <remarks>
    /// The Infrastructure adapter calls the node's fiat payout routes. ArdaNova never holds a bank
    /// credential, never terminates a payment webhook, and never names a payment provider in
    /// anything a person can read.
    /// <para>
    /// The subject is the ROUTE value on the node, never a body field: a caller-supplied
    /// destination would be a way to redirect somebody else's money into your own bank.
    /// </para>
    /// </remarks>
    public interface IAzoaCashOutNode
    {
        /// <summary>What this person can cash out today, and what is still held.</summary>
        Task<Result<CashOutAvailability>> GetAvailabilityAsync(
            Guid avatarId, CancellationToken ct = default);

        /// <summary>Reads where this person's money goes. Never creates anything.</summary>
        Task<Result<CashOutDestination>> GetDestinationAsync(
            Guid avatarId, CancellationToken ct = default);

        /// <summary>Starts or resumes destination setup and returns a fresh hosted link.</summary>
        Task<Result<CashOutDestination>> BeginDestinationSetupAsync(
            Guid avatarId, CancellationToken ct = default);

        /// <summary>Claims one cash-out. Idempotent on <paramref name="requestKey"/>.</summary>
        Task<Result<CashOutRecord>> RequestAsync(
            Guid avatarId, string requestKey, long amountMinorUnits, CancellationToken ct = default);

        /// <summary>This person's own cash-out history, newest first.</summary>
        Task<Result<IReadOnlyList<CashOutRecord>>> ListAsync(
            Guid avatarId, int limit, CancellationToken ct = default);
    }

    /// <summary>
    /// The rest of cashing out, implemented by <c>PayoutService</c> alongside
    /// <see cref="IPayoutService"/>: what a person may sell, and turning what they hold into money.
    /// </summary>
    /// <remarks>
    /// Kept apart from <see cref="IPayoutService"/> because the two halves answer different
    /// questions - one is about units, the other is about money - and a caller that needs only the
    /// money half should not be handed the unit-sale surface with it.
    /// </remarks>
    public interface ICashOutService
    {
        /// <summary>What this person may sell right now, per project, with held units dated.</summary>
        Task<Result<IReadOnlyList<SellableUnits>>> GetSellableUnitsAsync(
            string userId, CancellationToken ct = default);

        /// <summary>What one sale would pay this person, checked against the same gates it will face.</summary>
        Task<Result<UnitSaleQuote>> QuoteSaleAsync(
            string userId,
            string projectTokenConfigId,
            TokenHolderClass holderClass,
            int sourceUnitAmount,
            CancellationToken ct = default);

        /// <summary>What this person can turn into money today.</summary>
        Task<Result<CashOutAvailability>> GetAvailabilityAsync(
            string userId, CancellationToken ct = default);

        /// <summary>Where this person's money goes, and whether it is ready.</summary>
        Task<Result<CashOutDestination>> GetDestinationAsync(
            string userId, CancellationToken ct = default);

        /// <summary>Starts or resumes setting up where this person's money goes.</summary>
        Task<Result<CashOutDestination>> BeginDestinationSetupAsync(
            string userId, CancellationToken ct = default);

        /// <summary>Sends money to this person's own destination. Idempotent on the request key.</summary>
        Task<Result<CashOutRecord>> WithdrawAsync(
            string userId, string requestKey, long amountMinorUnits, CancellationToken ct = default);

        /// <summary>This person's own withdrawals, newest first.</summary>
        Task<Result<IReadOnlyList<CashOutRecord>>> GetWithdrawalsAsync(
            string userId, CancellationToken ct = default);
    }
}

namespace ArdaNova.Application.Services.Implementations
{
    /// <summary>
    /// Cashing out, in two steps a person can read: sell units for platform funds, then send those
    /// funds to their own bank.
    /// </summary>
    /// <remarks>
    /// The reasoning lives in
    /// <c>src/ArdaNova.Application/Services/Implementations/AGENTS.md</c> section cash out.
    /// <para>
    /// <b>Every amount here is exact.</b> Unit counts are whole numbers, money is a
    /// <see cref="decimal"/> narrowed through <see cref="Money"/>'s refusing conversions, and the
    /// two <c>double</c>-typed fields written onto the row are display projections nothing reads
    /// back. A cast that silently rounds is how cents go missing, so there are none.
    /// </para>
    /// <para>
    /// <b>Nothing is credited that the node did not do.</b> A sale is one exchange call under a key
    /// derived from the row id; units are unlocked and debited only against a TERMINAL receipt. An
    /// ambiguous answer parks the row in <see cref="PayoutStatus.PROCESSING"/> with the units still
    /// locked, because releasing them on a maybe is how one holding gets sold twice.
    /// </para>
    /// </remarks>
    public class PayoutService : IPayoutService, ICashOutService
    {
        /// <summary>Person-facing name of step one, used in every readiness refusal.</summary>
        public const string SellAction = "sell your units";

        /// <summary>Person-facing name of step two, used in every readiness refusal.</summary>
        public const string WithdrawAction = "send money to your bank";

        /// <summary>Why selling is refused when nothing is wired to the node's exchange.</summary>
        public const string NoExchangeConfigured =
            "Selling units is not switched on for this deployment, so nothing was moved.";

        /// <summary>Why cashing out is refused when nothing is wired to the node's off-ramp.</summary>
        public const string NoCashOutConfigured =
            "Sending money to a bank account is not switched on for this deployment, so nothing was moved.";

        /// <summary>What the node records as the reason these units moved.</summary>
        private const string ExchangePurpose = "unit-sale";

        /// <summary>Longest history the withdrawals read asks the node for.</summary>
        private const int WithdrawalHistoryLimit = 25;

        /// <summary>Longest request key the node's own alphabet accepts.</summary>
        private const int MaximumRequestKeyLength = 128;

        private readonly IRepository<PayoutRequest> _payoutRepository;
        private readonly IRepository<ProjectTokenConfig> _projectTokenConfigRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ITokenBalanceService _tokenBalanceService;
        private readonly IAzoaExchangeNode? _exchangeNode;
        private readonly IProjectUnitRateSource? _rateSource;
        private readonly IAzoaValueEligibilityService _valueEligibility;
        private readonly IAzoaCashOutNode? _cashOutNode;
        private readonly IRepository<User>? _userRepository;
        private readonly IRepository<TokenBalance>? _tokenBalanceRepository;
        private readonly IRepository<TokenAllocation>? _allocationRepository;
        private readonly TimeProvider _timeProvider;

        /// <param name="exchangeService">
        /// Kept in the signature, and deliberately unused: pricing a sale now goes through
        /// <see cref="IProjectUnitRateSource"/>, which returns the same exact decimals the node is
        /// asked to honour. Two pricing paths for one sale is how a quote and a receipt disagree.
        /// </param>
        /// <param name="exchangeNode">
        /// The node's exchange door. Optional in the constructor only so the type still constructs
        /// without a container; when it is absent a sale REFUSES rather than crediting anything.
        /// </param>
        /// <param name="cashOutNode">
        /// The node's off-ramp. Absent means this deployment cannot send money, which is a refusal
        /// with a named reason, never a silent success.
        /// </param>
        public PayoutService(
            IRepository<PayoutRequest> payoutRepository,
            IRepository<ProjectTokenConfig> projectTokenConfigRepository,
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ITokenBalanceService tokenBalanceService,
            IExchangeService exchangeService,
            IAzoaValueEligibilityService valueEligibility,
            IAzoaExchangeNode? exchangeNode = null,
            IProjectUnitRateSource? rateSource = null,
            IAzoaCashOutNode? cashOutNode = null,
            IRepository<User>? userRepository = null,
            IRepository<TokenBalance>? tokenBalanceRepository = null,
            IRepository<TokenAllocation>? allocationRepository = null,
            TimeProvider? timeProvider = null)
        {
            _payoutRepository = payoutRepository;
            _projectTokenConfigRepository = projectTokenConfigRepository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _tokenBalanceService = tokenBalanceService;
            _ = exchangeService;
            _exchangeNode = exchangeNode;
            _rateSource = rateSource;
            _valueEligibility = valueEligibility
                ?? throw new ArgumentNullException(nameof(valueEligibility));
            _cashOutNode = cashOutNode;
            _userRepository = userRepository;
            _tokenBalanceRepository = tokenBalanceRepository;
            _allocationRepository = allocationRepository;
            _timeProvider = timeProvider ?? TimeProvider.System;
        }

        // -- Step one: sell units for platform funds -----------------------------

        /// <summary>Sells project units back for platform funds, through the node's exchange.</summary>
        /// <remarks>
        /// The order is deliberate: lock, record, ask the node, apply what it answered. The row
        /// exists BEFORE the node call because the exchange key is derived from its id, so a
        /// request that dies mid-flight still leaves something reconciliation can name.
        /// </remarks>
        public async Task<Result<PayoutRequestDto>> RequestPayoutAsync(
            string userId, CreatePayoutRequestDto dto, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return Result<PayoutRequestDto>.Unauthorized(ValueEligibilityMessages.SubjectUnknown);

            if (dto is null || dto.SourceTokenAmount <= 0)
                return Result<PayoutRequestDto>.ValidationError("Choose how many units to sell.");

            var assessment = await AssessSaleAsync(
                userId, dto.SourceProjectTokenConfigId, dto.HolderClass, dto.SourceTokenAmount, ct);
            if (!assessment.IsSuccess || assessment.Value is null)
                return Carry<SaleAssessment, PayoutRequestDto>(assessment);

            var config = assessment.Value.Config;
            var rate = assessment.Value.Rate;
            var priced = assessment.Value.Priced;
            var avatarId = assessment.Value.AvatarId;

            // A retry of a sale that is STILL OPEN resumes it under the row it already has. The
            // node derives this seller's place in its buy-back queue from the exchange key, and
            // that key is derived from this row's id, so starting a second row would take a
            // SECOND place while the abandoned one goes on holding capacity until it expires -
            // making the next attempt strictly harder rather than easier (OI-44 c). One open sale
            // of the same units is one place in line. The units are already set aside against
            // that row, so nothing is locked a second time either.
            var resumable = await FindResumableSaleAsync(userId, dto, ct);
            if (resumable is not null)
                return await SubmitSaleAsync(resumable, config, rate, priced, avatarId, ct);

            var lockResult = await _tokenBalanceService.LockAsync(
                userId, dto.SourceProjectTokenConfigId, dto.SourceTokenAmount, dto.HolderClass, ct);
            if (!lockResult.IsSuccess)
            {
                return Result<PayoutRequestDto>.Conflict(
                    "Those units could not be set aside for the sale, so nothing was moved.");
            }

            // The lock only knows the balance, not the worker's hold. Two sales started at the
            // same moment can each pass the hold check above and TOGETHER reach into held units,
            // so the ceiling is re-checked once the lock is actually held - the only point at
            // which the answer cannot change underneath us.
            if (lockResult.Value is { } locked
                && locked.Balance - locked.LockedBalance < assessment.Value.Restriction.RestrictedUnits)
            {
                await _tokenBalanceService.UnlockAsync(
                    userId, dto.SourceProjectTokenConfigId, dto.SourceTokenAmount, dto.HolderClass, ct);

                return Result<PayoutRequestDto>.Conflict(
                    "Another sale of these units was already going through, so this one was not "
                    + "started. Nothing was moved.");
            }

            var payoutRequest = new PayoutRequest
            {
                id = Guid.NewGuid().ToString(),
                userId = userId,
                sourceProjectTokenConfigId = dto.SourceProjectTokenConfigId,
                sourceTokenAmount = dto.SourceTokenAmount,
                holderClass = dto.HolderClass,
                gateStatusAtRequest = config.gateStatus,
                status = PayoutStatus.PENDING,
                requestedAt = _timeProvider.GetUtcNow().UtcDateTime,
                ardaMinorUnits = priced.PlatformMinorUnits,
                usdReferenceMinorUnits = priced.UsdReferenceMinorUnits,
                ardaTokenAmount = ToWholeUnitsForDisplay(priced.PlatformMinorUnits),
                usdAmount = Money.ToDisplayDouble(Money.FromMinorUnits(priced.UsdReferenceMinorUnits)),
            };

            await _payoutRepository.AddAsync(payoutRequest, ct);
            await _unitOfWork.SaveChangesAsync(ct);

            return await SubmitSaleAsync(payoutRequest, config, rate, priced, avatarId, ct);
        }

        /// <summary>Sends one sale to the node under ITS OWN row's key, and applies the answer.</summary>
        /// <remarks>
        /// The key is the row's and nothing else's, so submitting the same row twice reaches the
        /// node as ONE request: a settled exchange replays its receipt, and a sale waiting in the
        /// node's buy-back queue re-reads the place it already holds instead of taking another.
        /// That is the whole of "a retry keeps its place", and it only works while the row - and
        /// therefore the key - is stable across attempts.
        /// </remarks>
        private async Task<Result<PayoutRequestDto>> SubmitSaleAsync(
            PayoutRequest payoutRequest,
            ProjectTokenConfig config,
            DeclaredProjectUnitRate rate,
            PricedSale priced,
            Guid avatarId,
            CancellationToken ct)
        {
            var command = new ArdaNovaExchangeCommand(
                ChainType: config.assetChain!,
                IsBuy: false,
                ProjectReference: config.treasuryReference!,
                ProjectAssetId: config.assetId!,
                PlatformAmountBaseUnits: priced.PlatformMinorUnits,
                ProjectUnitAmountBaseUnits: priced.ProjectUnitBaseUnits,
                ReservePartition: priced.ReservePartition,
                Rate: rate,
                ReleaseNotBefore: null,
                Purpose: ExchangePurpose,
                PreviewOnly: false);

            var receipt = await _exchangeNode!.ExchangeAsync(
                avatarId, command, payoutRequest.ExchangeIdempotencyKey, ct);

            return await ApplyExchangeOutcomeAsync(payoutRequest, receipt, ct);
        }

        /// <summary>
        /// The open sale this request is a RETRY of, or null when it is a new one. Matched on the
        /// units being sold rather than on the clock, because a seller who submits the same sale
        /// again is asking for the place they already hold - never for a second one.
        /// </summary>
        /// <remarks>
        /// A row the node has already answered for is deliberately excluded: its units have left,
        /// and <see cref="ProcessPayoutAsync"/> is the read that settles it. The oldest match wins,
        /// because that is the one holding the earliest place in the node's queue.
        /// </remarks>
        private async Task<PayoutRequest?> FindResumableSaleAsync(
            string userId, CreatePayoutRequestDto dto, CancellationToken ct)
        {
            var configId = dto.SourceProjectTokenConfigId;
            var unitAmount = dto.SourceTokenAmount;
            var holderClass = dto.HolderClass;

            Expression<Func<PayoutRequest, bool>> predicate = pr =>
                pr.userId == userId
                && pr.sourceProjectTokenConfigId == configId
                && pr.sourceTokenAmount == unitAmount
                && pr.holderClass == holderClass
                && (pr.status == PayoutStatus.PENDING || pr.status == PayoutStatus.PROCESSING);

            var open = await _payoutRepository.FindAsync(predicate, ct);

            // IsNodeSettled reads a column through a computed property, so the exclusion happens
            // here rather than in the predicate the database has to translate.
            return open
                .Where(candidate => !candidate.IsNodeSettled)
                .OrderBy(candidate => candidate.requestedAt)
                .FirstOrDefault();
        }

        /// <summary>Chases a sale the node's answer left ambiguous, and settles the row on what it finds.</summary>
        /// <remarks>
        /// It creates no economic effect: the receipt route is a READ under the key the sale was
        /// submitted with, so a second call can only ever observe the first one's operation.
        /// </remarks>
        public async Task<Result<PayoutRequestDto>> ProcessPayoutAsync(
            string payoutRequestId, CancellationToken ct = default)
        {
            var payoutRequest = await _payoutRepository.GetByIdAsync(payoutRequestId, ct);
            if (payoutRequest is null)
                return Result<PayoutRequestDto>.NotFound("That sale could not be found.");

            if (payoutRequest.status is not (PayoutStatus.PENDING or PayoutStatus.PROCESSING))
                return Result<PayoutRequestDto>.Success(_mapper.Map<PayoutRequestDto>(payoutRequest));

            if (_exchangeNode is null)
                return Result<PayoutRequestDto>.Failure(NoExchangeConfigured);

            var receipt = await _exchangeNode.GetReceiptAsync(payoutRequest.ExchangeIdempotencyKey, ct);
            return await ApplyExchangeOutcomeAsync(payoutRequest, receipt, ct);
        }

        /// <summary>Puts back a sale the node never settled, and returns the units.</summary>
        public async Task<Result<PayoutRequestDto>> CancelPayoutAsync(
            string payoutRequestId, CancellationToken ct = default)
        {
            var payoutRequest = await _payoutRepository.GetByIdAsync(payoutRequestId, ct);
            if (payoutRequest is null)
                return Result<PayoutRequestDto>.NotFound("That sale could not be found.");

            if (payoutRequest.status != PayoutStatus.PENDING)
            {
                return Result<PayoutRequestDto>.Conflict(
                    "This sale is no longer waiting, so it cannot be called off.");
            }

            // A row the node already answered for is NOT cancellable, whatever its status column
            // says: the units have left, and unlocking them here would let them be sold twice.
            if (payoutRequest.IsNodeSettled)
            {
                return Result<PayoutRequestDto>.Conflict(
                    "This sale already went through, so it cannot be called off.");
            }

            await _unitOfWork.BeginTransactionAsync(ct);
            try
            {
                var unlockResult = await _tokenBalanceService.UnlockAsync(
                    payoutRequest.userId,
                    payoutRequest.sourceProjectTokenConfigId ?? string.Empty,
                    payoutRequest.sourceTokenAmount,
                    payoutRequest.holderClass,
                    ct);

                if (!unlockResult.IsSuccess)
                {
                    await _unitOfWork.RollbackTransactionAsync(CancellationToken.None);
                    _unitOfWork.ClearTrackedChanges();
                    return Result<PayoutRequestDto>.Failure(
                        "Your units could not be released, so the sale was left as it was.");
                }

                payoutRequest.status = PayoutStatus.CANCELLED;
                payoutRequest.completedAt = _timeProvider.GetUtcNow().UtcDateTime;
                await _payoutRepository.UpdateAsync(payoutRequest, ct);
                await _unitOfWork.SaveChangesAsync(ct);
                var resultDto = _mapper.Map<PayoutRequestDto>(payoutRequest);
                await _unitOfWork.CommitTransactionAsync(ct);
                return Result<PayoutRequestDto>.Success(resultDto);
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync(CancellationToken.None);
                _unitOfWork.ClearTrackedChanges();
                throw;
            }
        }

        public async Task<Result<IReadOnlyList<PayoutRequestDto>>> GetPayoutsByUserAsync(
            string userId, CancellationToken ct = default)
        {
            Expression<Func<PayoutRequest, bool>> predicate = pr => pr.userId == userId;
            var payouts = await _payoutRepository.FindAsync(predicate, ct);

            var dtos = _mapper.Map<List<PayoutRequestDto>>(payouts);
            return Result<IReadOnlyList<PayoutRequestDto>>.Success(dtos);
        }

        /// <summary>Every sale an operator still has to account for: waiting, or parked mid-flight.</summary>
        /// <remarks>
        /// Parked rows are included because they are the ones a person is waiting on. They are also
        /// chased automatically - see <c>ParkedPayoutReconcilerService</c> - so this list is what an
        /// operator looks at, not the only thing that ever settles a row.
        /// </remarks>
        public async Task<Result<IReadOnlyList<PayoutRequestDto>>> GetPendingPayoutsAsync(
            CancellationToken ct = default)
        {
            Expression<Func<PayoutRequest, bool>> predicate = pr =>
                pr.status == PayoutStatus.PENDING || pr.status == PayoutStatus.PROCESSING;
            var payouts = await _payoutRepository.FindAsync(predicate, ct);

            var dtos = _mapper.Map<List<PayoutRequestDto>>(payouts);
            return Result<IReadOnlyList<PayoutRequestDto>>.Success(dtos);
        }

        // -- What a person may sell ----------------------------------------------

        /// <inheritdoc />
        /// <remarks>
        /// A quote runs the whole gate, so what the page says and what the sale does are the same
        /// decision. A gate that says no is an ANSWER - a quote with <c>CanSell</c> false and the
        /// reason in words - while a platform that cannot work the price out is a failure.
        /// </remarks>
        public async Task<Result<UnitSaleQuote>> QuoteSaleAsync(
            string userId,
            string projectTokenConfigId,
            TokenHolderClass holderClass,
            int sourceUnitAmount,
            CancellationToken ct = default)
        {
            if (sourceUnitAmount <= 0)
                return Result<UnitSaleQuote>.ValidationError("Choose how many units to sell.");

            var assessment = await AssessSaleAsync(
                userId, projectTokenConfigId, holderClass, sourceUnitAmount, ct);

            if (assessment.IsSuccess && assessment.Value is { } ready)
            {
                return Result<UnitSaleQuote>.Success(new UnitSaleQuote(
                    ProjectTokenConfigId: projectTokenConfigId,
                    UnitName: ready.Config.unitName,
                    SourceUnitAmount: sourceUnitAmount,
                    PlatformMinorUnits: ready.Priced.PlatformMinorUnits,
                    UsdReferenceMinorUnits: ready.Priced.UsdReferenceMinorUnits,
                    SellableUnitCount: ready.SellableUnitCount,
                    RestrictedUntil: ready.Restriction.RestrictedUntil,
                    CanSell: true,
                    StatusMessage: "Ready to sell."));
            }

            var refusedByGate = assessment.Type
                is ResultType.Forbidden or ResultType.ValidationError or ResultType.NotFound;
            if (!refusedByGate)
                return Carry<SaleAssessment, UnitSaleQuote>(assessment);

            return Result<UnitSaleQuote>.Success(new UnitSaleQuote(
                ProjectTokenConfigId: projectTokenConfigId,
                UnitName: string.Empty,
                SourceUnitAmount: sourceUnitAmount,
                PlatformMinorUnits: 0,
                UsdReferenceMinorUnits: 0,
                SellableUnitCount: 0,
                RestrictedUntil: null,
                CanSell: false,
                StatusMessage: assessment.Error ?? "These units cannot be sold at the moment."));
        }

        /// <inheritdoc />
        public async Task<Result<IReadOnlyList<SellableUnits>>> GetSellableUnitsAsync(
            string userId, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return Result<IReadOnlyList<SellableUnits>>.Unauthorized(ValueEligibilityMessages.SubjectUnknown);

            if (_tokenBalanceRepository is null)
            {
                return Result<IReadOnlyList<SellableUnits>>.Failure(
                    "Your units could not be read on this deployment.");
            }

            var rows = await _tokenBalanceRepository.FindAsync(
                balance => balance.userId == userId && !balance.isPlatformToken, ct);

            var holdings = new List<SellableUnits>();
            foreach (var row in rows.Where(row =>
                row.balance > 0 && !string.IsNullOrWhiteSpace(row.projectTokenConfigId)))
            {
                var configId = row.projectTokenConfigId!;
                var config = await _projectTokenConfigRepository.GetByIdAsync(configId, ct);
                if (config is null)
                    continue;

                var restriction = await ReadRestrictionAsync(userId, configId, ct);
                var free = Math.Max(0L, row.balance - row.lockedBalance - restriction.RestrictedUnits);

                // Every sellability rule below is keyed on the holder class. A row that carries
                // none cannot have them applied, and defaulting one would decide a sale on an
                // invented fact - so the holding is shown, and refused.
                if (row.holderClass is not { } holderClass)
                {
                    holdings.Add(new SellableUnits(
                        ProjectTokenConfigId: configId,
                        ProjectId: config.projectId,
                        UnitName: config.unitName,
                        HolderClass: null,
                        HeldUnits: row.balance,
                        CommittedUnits: row.lockedBalance,
                        RestrictedUnits: restriction.RestrictedUnits,
                        SellableUnitCount: 0,
                        RestrictedUntil: restriction.RestrictedUntil,
                        CanSell: false,
                        StatusMessage:
                            "These units are not recorded against how you came by them yet, so they "
                            + "cannot be sold. Support can put that right."));
                    continue;
                }

                var liquidity = await _tokenBalanceService.IsBalanceLiquidAsync(
                    userId, configId, holderClass, ct);
                var liquid = liquidity.IsSuccess && liquidity.Value;
                var blocked = DescribeSellability(config, holderClass);
                var canSell = liquid && blocked is null && free > 0;

                holdings.Add(new SellableUnits(
                    ProjectTokenConfigId: configId,
                    ProjectId: config.projectId,
                    UnitName: config.unitName,
                    HolderClass: holderClass,
                    HeldUnits: row.balance,
                    CommittedUnits: row.lockedBalance,
                    RestrictedUnits: restriction.RestrictedUnits,
                    SellableUnitCount: canSell ? free : 0,
                    RestrictedUntil: restriction.RestrictedUntil,
                    CanSell: canSell,
                    StatusMessage: DescribeHolding(
                        liquid, blocked, config.gateStatus, holderClass, free, restriction)));
            }

            return Result<IReadOnlyList<SellableUnits>>.Success(holdings);
        }

        // -- Step two: send the funds to a bank -----------------------------------

        /// <inheritdoc />
        public async Task<Result<CashOutAvailability>> GetAvailabilityAsync(
            string userId, CancellationToken ct = default)
        {
            var avatar = await ResolveAvatarAsync<CashOutAvailability>(userId, ct);
            if (avatar.Refusal is not null)
                return avatar.Refusal;

            return await _cashOutNode!.GetAvailabilityAsync(avatar.AvatarId, ct);
        }

        /// <inheritdoc />
        public async Task<Result<CashOutDestination>> GetDestinationAsync(
            string userId, CancellationToken ct = default)
        {
            var avatar = await ResolveAvatarAsync<CashOutDestination>(userId, ct);
            if (avatar.Refusal is not null)
                return avatar.Refusal;

            return await _cashOutNode!.GetDestinationAsync(avatar.AvatarId, ct);
        }

        /// <inheritdoc />
        /// <remarks>
        /// Setup happens HERE, at the first cash-out, rather than at sign-up: it is the one moment
        /// paperwork is expected, and asking earlier turns joining in into an application.
        /// </remarks>
        public async Task<Result<CashOutDestination>> BeginDestinationSetupAsync(
            string userId, CancellationToken ct = default)
        {
            var avatar = await ResolveAvatarAsync<CashOutDestination>(userId, ct, WithdrawAction);
            if (avatar.Refusal is not null)
                return avatar.Refusal;

            return await _cashOutNode!.BeginDestinationSetupAsync(avatar.AvatarId, ct);
        }

        /// <inheritdoc />
        public async Task<Result<CashOutRecord>> WithdrawAsync(
            string userId, string requestKey, long amountMinorUnits, CancellationToken ct = default)
        {
            if (!IsAcceptableRequestKey(requestKey))
            {
                return Result<CashOutRecord>.ValidationError(
                    "This request could not be identified, so nothing was sent. Please try again.");
            }

            if (amountMinorUnits <= 0)
                return Result<CashOutRecord>.ValidationError("Enter how much you want to send.");

            var avatar = await ResolveAvatarAsync<CashOutRecord>(userId, ct, WithdrawAction);
            if (avatar.Refusal is not null)
                return avatar.Refusal;

            return await _cashOutNode!.RequestAsync(
                avatar.AvatarId, requestKey.Trim(), amountMinorUnits, ct);
        }

        /// <inheritdoc />
        public async Task<Result<IReadOnlyList<CashOutRecord>>> GetWithdrawalsAsync(
            string userId, CancellationToken ct = default)
        {
            var avatar = await ResolveAvatarAsync<IReadOnlyList<CashOutRecord>>(userId, ct);
            if (avatar.Refusal is not null)
                return avatar.Refusal;

            return await _cashOutNode!.ListAsync(avatar.AvatarId, WithdrawalHistoryLimit, ct);
        }

        // -- Applying what the node answered --------------------------------------

        /// <summary>Settles the row on the node's answer, moving units ONLY against a terminal receipt.</summary>
        /// <remarks>
        /// Four outcomes, kept apart on purpose.
        /// <list type="bullet">
        /// <item>A TERMINAL receipt with no failure code debits the units and completes the row.</item>
        /// <item>A receipt that is not terminal, or asks for reconciliation, parks the row in
        /// PROCESSING with the units still locked.</item>
        /// <item>A DECLINE - the node read the request, said no, and created nothing - returns the
        /// units and fails the row.</item>
        /// <item>Anything else is AMBIGUOUS: a timeout, an unreachable node, a conflict. The
        /// exchange may already have run, so the row parks with the units still locked. Returning
        /// them on a maybe is exactly how one holding gets sold twice.</item>
        /// </list>
        /// </remarks>
        private async Task<Result<PayoutRequestDto>> ApplyExchangeOutcomeAsync(
            PayoutRequest payoutRequest,
            Result<ArdaNovaExchangeReceipt> receipt,
            CancellationToken ct)
        {
            var now = _timeProvider.GetUtcNow().UtcDateTime;

            if (receipt.IsSuccess && receipt.Value is { } settled)
            {
                payoutRequest.SellReceiptReference = settled.ReceiptReference;
                payoutRequest.SellOperationId = settled.OperationId?.ToString();
                payoutRequest.processedAt ??= now;

                if (!settled.IsTerminal || settled.RequiresReconciliation)
                {
                    payoutRequest.status = PayoutStatus.PROCESSING;
                    payoutRequest.failureReason = null;
                    await _payoutRepository.UpdateAsync(payoutRequest, ct);
                    await _unitOfWork.SaveChangesAsync(ct);

                    return Result<PayoutRequestDto>.Success(_mapper.Map<PayoutRequestDto>(payoutRequest));
                }

                if (!string.IsNullOrWhiteSpace(settled.FailureCode))
                {
                    return await FailSaleAsync(
                        payoutRequest,
                        "This sale did not go through, so your units are back where they were.",
                        now,
                        ct);
                }

                return await CompleteSaleAsync(payoutRequest, settled, now, ct);
            }

            // The node DECLINED - it read the request, said no, and created nothing. Only these
            // outcomes are safe to unlock on. A transport failure or a conflict is AMBIGUOUS: the
            // exchange may already have run, and returning the units on a maybe is how the same
            // holding gets sold twice.
            //
            // BadRequest is load-bearing here, not incidental: the node answers a refusal that
            // provably moved nothing (REQUEST_DECLINED, e.g. a buy-back the operator has not
            // opened) with HTTP 422, and the transport maps 422 to BadRequest. Dropping it from
            // this set puts the seller's units back under an indefinite lock over a plain "no".
            var declined = receipt.Type
                is ResultType.NotFound
                or ResultType.ValidationError
                or ResultType.BadRequest
                or ResultType.Forbidden
                or ResultType.Unauthorized;

            if (declined)
                return await FailSaleAsync(payoutRequest, DescribeRefusal(receipt.Type), now, ct);

            var unresolved = DescribeRefusal(receipt.Type);

            if (payoutRequest.status != PayoutStatus.PROCESSING)
            {
                payoutRequest.status = PayoutStatus.PROCESSING;
                payoutRequest.processedAt ??= now;
                payoutRequest.failureReason = unresolved;
                await _payoutRepository.UpdateAsync(payoutRequest, ct);
                await _unitOfWork.SaveChangesAsync(ct);
            }

            return Result<PayoutRequestDto>.Conflict(unresolved);
        }

        /// <summary>ArdaNova's own words for a refusal the shared node returned.</summary>
        /// <remarks>
        /// The node's transport flattens its own wording to sentences that NAME THE NODE, and no
        /// person selling their work should read a platform's internal name. The refusal KIND
        /// still carries the meaning, so it is translated here rather than passed through, and the
        /// node's own text stays where operators read it: its transport log.
        /// </remarks>
        private static string DescribeRefusal(ResultType type) => type switch
        {
            ResultType.NotFound =>
                "This sale did not go through, so your units are back where they were.",
            ResultType.Forbidden or ResultType.Unauthorized =>
                "Your account is not set up for this yet, so nothing was moved. "
                + "Finish account setup and try again.",
            // Covers both readings of a decline - wrong terms, and "not being bought back right
            // now" - because the seller acts on the same fact either way: nothing is locked.
            ResultType.ValidationError or ResultType.BadRequest =>
                "This sale was not accepted, so your units are back where they were. "
                + "You can try again later.",
            // The words promise only what is true either way: the sale is visible and it will be
            // finished. A parked row is chased on a timer by ParkedPayoutReconcilerService where
            // that loop is registered, and settled by an operator's read of the same receipt where
            // it is not - so the sentence does not have to know which.
            ResultType.Conflict =>
                "We could not confirm this sale just now, so your units stay set aside for it. "
                + "Support can see it and will finish it.",
            _ =>
                "We could not reach the part of the platform that moves units, so your units stay "
                + "set aside for this sale. Support can see it and will finish it.",
        };

        /// <summary>Debits the sold units and completes the row, in one transaction.</summary>
        private async Task<Result<PayoutRequestDto>> CompleteSaleAsync(
            PayoutRequest payoutRequest,
            ArdaNovaExchangeReceipt settled,
            DateTime now,
            CancellationToken ct)
        {
            await _unitOfWork.BeginTransactionAsync(ct);
            try
            {
                var configId = payoutRequest.sourceProjectTokenConfigId ?? string.Empty;

                var unlockResult = await _tokenBalanceService.UnlockAsync(
                    payoutRequest.userId, configId, payoutRequest.sourceTokenAmount,
                    payoutRequest.holderClass, ct);
                var debitResult = unlockResult.IsSuccess
                    ? await _tokenBalanceService.DebitAsync(
                        payoutRequest.userId, configId, payoutRequest.sourceTokenAmount,
                        payoutRequest.holderClass, ct)
                    : null;

                if (!unlockResult.IsSuccess || debitResult is null || !debitResult.IsSuccess)
                {
                    await _unitOfWork.RollbackTransactionAsync(CancellationToken.None);
                    _unitOfWork.ClearTrackedChanges();
                    return Result<PayoutRequestDto>.Conflict(
                        "Your sale went through, but your units could not be updated here. "
                        + "Support has the record and nothing was lost.");
                }

                // What the node actually paid wins over what ArdaNova quoted. A receipt that landed
                // a different amount is the record; keeping the quote is how a row comes to
                // disagree with the money.
                if (TryReadBaseUnits(settled.AmountReceivedBaseUnits, out var received))
                {
                    payoutRequest.ardaMinorUnits = received;
                    payoutRequest.ardaTokenAmount = ToWholeUnitsForDisplay(received);
                }

                payoutRequest.status = PayoutStatus.COMPLETED;
                payoutRequest.failureReason = null;
                payoutRequest.completedAt = now;
                await _payoutRepository.UpdateAsync(payoutRequest, ct);
                await _unitOfWork.SaveChangesAsync(ct);

                var dto = _mapper.Map<PayoutRequestDto>(payoutRequest);
                await _unitOfWork.CommitTransactionAsync(ct);
                return Result<PayoutRequestDto>.Success(dto);
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync(CancellationToken.None);
                _unitOfWork.ClearTrackedChanges();
                throw;
            }
        }

        /// <summary>Returns the units and records why the sale did not happen.</summary>
        private async Task<Result<PayoutRequestDto>> FailSaleAsync(
            PayoutRequest payoutRequest, string reason, DateTime now, CancellationToken ct)
        {
            await _unitOfWork.BeginTransactionAsync(ct);
            try
            {
                await _tokenBalanceService.UnlockAsync(
                    payoutRequest.userId,
                    payoutRequest.sourceProjectTokenConfigId ?? string.Empty,
                    payoutRequest.sourceTokenAmount,
                    payoutRequest.holderClass,
                    ct);

                payoutRequest.status = PayoutStatus.FAILED;
                payoutRequest.failureReason = reason;
                payoutRequest.completedAt = now;
                await _payoutRepository.UpdateAsync(payoutRequest, ct);
                await _unitOfWork.SaveChangesAsync(ct);
                await _unitOfWork.CommitTransactionAsync(ct);
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync(CancellationToken.None);
                _unitOfWork.ClearTrackedChanges();
                throw;
            }

            return Result<PayoutRequestDto>.Failure(reason);
        }

        // -- The gate every sale passes, quoted or executed -------------------------

        /// <summary>Everything a sale needs, once every gate it has to pass has been checked.</summary>
        private sealed record SaleAssessment(
            ProjectTokenConfig Config,
            DeclaredProjectUnitRate Rate,
            PricedSale Priced,
            long SellableUnitCount,
            UnitWindowRestriction Restriction,
            Guid AvatarId);

        /// <summary>
        /// The ONE place a sale is decided: readiness, provisioning, liquidity, the worker's hold,
        /// the amount, and the exact price. Both the quote and the sale itself run through here, so
        /// a person cannot be shown one answer and given another.
        /// </summary>
        /// <remarks>
        /// The refusal TYPE carries meaning downstream. <see cref="ResultType.Forbidden"/> and
        /// <see cref="ResultType.ValidationError"/> mean "these units, this amount, not now" - an
        /// answer a page states in words. Everything else means the platform could not decide,
        /// which is never reported to a person as a rule they broke.
        /// </remarks>
        private async Task<Result<SaleAssessment>> AssessSaleAsync(
            string userId,
            string projectTokenConfigId,
            TokenHolderClass holderClass,
            int sourceUnitAmount,
            CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(projectTokenConfigId))
                return Result<SaleAssessment>.ValidationError("Choose which project's units to sell.");

            if (_exchangeNode is null || _rateSource is null)
                return Result<SaleAssessment>.Failure(NoExchangeConfigured);

            var readiness = await _valueEligibility.RequireReadyAsync(userId, SellAction, ct);
            if (!readiness.IsSuccess)
                return ValueEligibilityFailure.Propagate<AzoaCustodialAccountStatusDto, SaleAssessment>(readiness);

            if (!Guid.TryParse(readiness.Value?.AvatarId, out var avatarId) || avatarId == Guid.Empty)
            {
                return Result<SaleAssessment>.Conflict(
                    "Your account is still being set up, so nothing was moved. Please try again shortly.");
            }

            var config = await _projectTokenConfigRepository.GetByIdAsync(projectTokenConfigId, ct);
            if (config is null)
                return Result<SaleAssessment>.NotFound("That project's units could not be found.");

            var blocked = DescribeSellability(config, holderClass);
            if (blocked is not null)
                return Result<SaleAssessment>.Forbidden(blocked);

            // Held units are the worker's own restricted window. They are part of the balance and
            // must not be sellable, so the ceiling is what is free AFTER the hold, never the number
            // the balance row happens to show. The window is read BEFORE the liquidity gate so that
            // a holding the gate would allow but the window still holds is refused with its DATE
            // rather than with a sentence about the project.
            var restriction = await ReadRestrictionAsync(userId, projectTokenConfigId, ct);

            var liquidity = await _tokenBalanceService.IsBalanceLiquidAsync(
                userId, projectTokenConfigId, holderClass, ct);
            if (!liquidity.IsSuccess || !liquidity.Value)
            {
                return Result<SaleAssessment>.Forbidden(
                    DescribeIlliquidity(config.gateStatus, holderClass, restriction));
            }

            var available = await ReadSellableUnitCountAsync(
                userId, projectTokenConfigId, holderClass, restriction, ct);
            if (!available.IsSuccess)
                return Result<SaleAssessment>.NotFound(available.Error ?? "Your units could not be read.");

            if (available.Value < sourceUnitAmount)
                return Result<SaleAssessment>.ValidationError(DescribeShortfall(available.Value, restriction));

            var rate = await _rateSource.GetDeclaredRateAsync(projectTokenConfigId, ct);
            if (!rate.IsSuccess || rate.Value is null)
            {
                return Result<SaleAssessment>.Failure(
                    "These units cannot be priced right now, so nothing was moved. Please try again shortly.");
            }

            var priced = QuoteSale(sourceUnitAmount, config, holderClass, rate.Value);
            if (!priced.IsSuccess || priced.Value is null)
                return Carry<PricedSale, SaleAssessment>(priced);

            return Result<SaleAssessment>.Success(new SaleAssessment(
                config, rate.Value, priced.Value, available.Value, restriction, avatarId));
        }

        /// <summary>Re-wraps a refusal onto another result type, keeping BOTH its wording and its kind.</summary>
        /// <remarks>Flattening a refusal to a plain failure is how a "not yet" becomes a "server error".</remarks>
        private static Result<TOut> Carry<TIn, TOut>(Result<TIn> source)
        {
            var error = source.Error ?? "This could not be completed, so nothing was moved.";
            return source.Type switch
            {
                ResultType.NotFound => Result<TOut>.NotFound(error),
                ResultType.Forbidden => Result<TOut>.Forbidden(error),
                ResultType.Unauthorized => Result<TOut>.Unauthorized(error),
                ResultType.Conflict => Result<TOut>.Conflict(error),
                ResultType.ValidationError => Result<TOut>.ValidationError(error),
                ResultType.BadRequest => Result<TOut>.BadRequest(error),
                _ => Result<TOut>.Failure(error),
            };
        }

        // -- Pricing, in exact numbers --------------------------------------------

        /// <summary>One priced sale: both legs in base units, plus the allowance it returns to.</summary>
        private sealed record PricedSale(
            long ProjectUnitBaseUnits,
            long PlatformMinorUnits,
            long UsdReferenceMinorUnits,
            string ReservePartition);

        /// <summary>Prices a sale exactly, refusing anything that will not fit rather than narrowing it.</summary>
        private static Result<PricedSale> QuoteSale(
            int sourceTokenAmount,
            ProjectTokenConfig config,
            TokenHolderClass holderClass,
            DeclaredProjectUnitRate rate)
        {
            if (!FixedScaleAmount.IsSupportedScale(config.assetScale))
                return Result<PricedSale>.Failure("These units are not set up to be sold yet.");

            if (!FixedScaleAmount.TryFromPositiveDecimal(
                    sourceTokenAmount, config.assetScale!.Value, out var unitAmount)
                || !long.TryParse(
                    unitAmount.BaseUnits,
                    NumberStyles.None,
                    CultureInfo.InvariantCulture,
                    out var unitBaseUnits))
            {
                return Result<PricedSale>.Failure(
                    "That many units is outside the range this platform can move exactly.");
            }

            if (!decimal.TryParse(
                    rate.ArdaPerWholeProjectUnit,
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out var platformPerUnit)
                || platformPerUnit <= 0)
            {
                return Result<PricedSale>.Failure("These units have no price today, so nothing was moved.");
            }

            // Floored, never rounded up: the surplus is deliberately dropped rather than paid out
            // of a balance nobody funded.
            var platformExact = sourceTokenAmount * platformPerUnit;
            if (!Money.TryToMinorUnitsFloor(platformExact, out var platformMinorUnits))
            {
                return Result<PricedSale>.Failure(
                    "That sale is outside the range this platform can represent exactly.");
            }

            if (platformMinorUnits <= 0)
            {
                return Result<PricedSale>.ValidationError(
                    "That many units is worth less than the smallest amount we can pay. Sell more at once.");
            }

            // The dollar figure is a reference the node stamps on the receipt, never an input to
            // the move. An unreadable one records as absent rather than blocking a valid sale.
            var usdMinorUnits = decimal.TryParse(
                    rate.UsdMajorUnitsPerWholeProjectUnit,
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out var usdPerUnit)
                && Money.TryToMinorUnitsFloor(sourceTokenAmount * usdPerUnit, out var usdFloor)
                ? usdFloor
                : 0L;

            return Result<PricedSale>.Success(new PricedSale(
                ProjectUnitBaseUnits: unitBaseUnits,
                PlatformMinorUnits: platformMinorUnits,
                UsdReferenceMinorUnits: usdMinorUnits,
                ReservePartition: ReservePartitionFor(holderClass)));
        }

        /// <summary>Whole platform units for a label, floored and CLAMPED rather than narrowed.</summary>
        /// <remarks>A plain cast of a large count wraps to a nonsense - often negative - number.</remarks>
        private static int ToWholeUnitsForDisplay(long minorUnits)
        {
            var whole = minorUnits / (long)Money.MinorUnitsPerWholeUnit;
            return whole > int.MaxValue ? int.MaxValue : (int)whole;
        }

        /// <summary>Reads a base-unit string the node returned, refusing anything that is not one.</summary>
        private static bool TryReadBaseUnits(string? value, out long baseUnits)
            => long.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out baseUnits)
                && baseUnits >= 0;

        // -- Sellability, in plain words -------------------------------------------

        /// <summary>Reads the worker's own restricted window off the awards behind a holding.</summary>
        /// <remarks>
        /// The rule itself lives in <see cref="UnitWindow"/> so the sale seam, the swap seam, the
        /// gate transitions and the holdings read all decide a window the same way. Duplicating it
        /// here is how one of them comes to disagree with the date a person was given.
        /// </remarks>
        private async Task<UnitWindowRestriction> ReadRestrictionAsync(
            string userId, string projectTokenConfigId, CancellationToken ct)
            => _allocationRepository is null
                ? UnitWindowRestriction.Free
                : await UnitWindow.ReadAsync(
                    _allocationRepository,
                    userId,
                    projectTokenConfigId,
                    _timeProvider.GetUtcNow(),
                    ct);

        /// <summary>Units free to sell: held, minus what is committed, minus what is restricted.</summary>
        private async Task<Result<long>> ReadSellableUnitCountAsync(
            string userId,
            string projectTokenConfigId,
            TokenHolderClass holderClass,
            UnitWindowRestriction restriction,
            CancellationToken ct)
        {
            var balance = await _tokenBalanceService.GetBalanceAsync(
                userId, projectTokenConfigId, holderClass, ct);
            if (!balance.IsSuccess || balance.Value is null)
                return Result<long>.Failure("You do not hold any units in this project.");

            return Result<long>.Success(
                Math.Max(0L, balance.Value.AvailableBalance - restriction.RestrictedUnits));
        }

        /// <summary>Why these units cannot be sold at all, or null when they can.</summary>
        private static string? DescribeSellability(ProjectTokenConfig config, TokenHolderClass holderClass)
        {
            if (!config.IsNodeProvisioned
                || string.IsNullOrWhiteSpace(config.assetChain)
                || string.IsNullOrWhiteSpace(config.treasuryReference)
                || string.IsNullOrWhiteSpace(config.assetId))
            {
                return "This project's units are not set up to be sold yet.";
            }

            // A founder holding sits in no declared allowance, so there is nothing for the node to
            // draw a buy-back against. Refused by name rather than charged to somebody else's
            // allowance; unblocked by a declared founder partition on the project's units.
            if (holderClass == TokenHolderClass.FOUNDER)
                return "Founder units cannot be sold back yet.";

            return null;
        }

        /// <summary>Which declared allowance a sale returns units to.</summary>
        /// <remarks>
        /// The allowance a holding came OUT of is the one it goes back into; sending contributor
        /// units back through the investor allowance would let one side of a project's declared
        /// supply quietly fund the other.
        /// </remarks>
        private static string ReservePartitionFor(TokenHolderClass holderClass)
            => holderClass == TokenHolderClass.INVESTOR
                ? ProjectTokenService.InvestorPartitionName
                : ProjectTokenService.ContributorPartitionName;

        /// <summary>Why a holding is not liquid yet, in the holder's own terms.</summary>
        /// <remarks>
        /// The worker's own window wins over the project's gate when it is the thing holding the
        /// units, because it is the fact the person was already told: "yours, held until 14 March"
        /// is an answer they can act on, and "these units cannot be sold at the moment" is not.
        /// </remarks>
        private static string DescribeIlliquidity(
            ProjectGateStatus gateStatus,
            TokenHolderClass holderClass,
            UnitWindowRestriction restriction)
        {
            if (restriction.RestrictedUnits > 0
                && TokenLiquidityPolicy.IsLiquid(holderClass, gateStatus))
            {
                return restriction.RestrictedUntil is { } until
                    ? $"These units are yours, held until {until.UtcDateTime:d MMMM yyyy}."
                    : "These units are yours, but they are still held.";
            }

            return (holderClass, gateStatus) switch
            {
                (TokenHolderClass.CONTRIBUTOR, ProjectGateStatus.FUNDING) =>
                    "This project has not reached its funding goal yet, so its units cannot be sold.",
                (TokenHolderClass.INVESTOR, ProjectGateStatus.ACTIVE) =>
                    "This project has not delivered yet. Your units can be sold once it does.",
                (TokenHolderClass.FOUNDER, ProjectGateStatus.ACTIVE) =>
                    "Founder units can be sold once the project's success is confirmed.",
                (TokenHolderClass.FOUNDER, ProjectGateStatus.FAILED) =>
                    "Founder units were written off when this project ended.",
                _ => "These units cannot be sold at the moment.",
            };
        }

        /// <summary>Why the requested amount is more than this person can sell.</summary>
        private static string DescribeShortfall(long available, UnitWindowRestriction restriction)
        {
            if (restriction.RestrictedUnits > 0 && restriction.RestrictedUntil is { } until)
            {
                return $"You can sell {available:N0} units now. Another {restriction.RestrictedUnits:N0} "
                    + $"are yours but held until {until.UtcDateTime:d MMMM yyyy}.";
            }

            if (restriction.RestrictedUnits > 0)
            {
                return $"You can sell {available:N0} units now. Another {restriction.RestrictedUnits:N0} "
                    + "are yours but still held.";
            }

            return $"You can sell {available:N0} units now.";
        }

        /// <summary>One holding's status line, in the words a person reads on the page.</summary>
        private static string DescribeHolding(
            bool liquid,
            string? blocked,
            ProjectGateStatus gateStatus,
            TokenHolderClass holderClass,
            long free,
            UnitWindowRestriction restriction)
        {
            if (blocked is not null)
                return blocked;

            if (!liquid)
                return DescribeIlliquidity(gateStatus, holderClass, restriction);

            if (free > 0 && restriction.RestrictedUnits > 0 && restriction.RestrictedUntil is { } until)
            {
                return $"{free:N0} ready to sell. {restriction.RestrictedUnits:N0} more are yours, "
                    + $"held until {until.UtcDateTime:d MMMM yyyy}.";
            }

            if (free > 0)
                return $"{free:N0} ready to sell.";

            if (restriction.RestrictedUntil is { } holdEnds)
                return $"These units are yours, held until {holdEnds.UtcDateTime:d MMMM yyyy}.";

            return "These units are not ready to sell yet.";
        }

        // -- Shared plumbing --------------------------------------------------------

        /// <summary>An account resolved for a money action, or the refusal that stopped it.</summary>
        private readonly record struct ResolvedAvatar<T>(Guid AvatarId, Result<T>? Refusal);

        /// <summary>Resolves the node account a money action runs against, refusing rather than guessing.</summary>
        /// <param name="action">
        /// Named only when the action needs the full readiness gate. Reads pass null, so a person
        /// mid-setup still sees an honest "not ready yet" page instead of a forbidden one.
        /// </param>
        private async Task<ResolvedAvatar<T>> ResolveAvatarAsync<T>(
            string userId, CancellationToken ct, string? action = null)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return new ResolvedAvatar<T>(
                    Guid.Empty, Result<T>.Unauthorized(ValueEligibilityMessages.SubjectUnknown));
            }

            if (_cashOutNode is null)
                return new ResolvedAvatar<T>(Guid.Empty, Result<T>.Failure(NoCashOutConfigured));

            if (action is not null)
            {
                var readiness = await _valueEligibility.RequireReadyAsync(userId, action, ct);
                if (!readiness.IsSuccess)
                {
                    return new ResolvedAvatar<T>(
                        Guid.Empty,
                        ValueEligibilityFailure.Propagate<AzoaCustodialAccountStatusDto, T>(readiness));
                }

                if (Guid.TryParse(readiness.Value?.AvatarId, out var readyAvatarId)
                    && readyAvatarId != Guid.Empty)
                {
                    return new ResolvedAvatar<T>(readyAvatarId, null);
                }
            }

            if (_userRepository is null)
                return new ResolvedAvatar<T>(Guid.Empty, Result<T>.Failure(NoCashOutConfigured));

            var user = await _userRepository.GetByIdAsync(userId, ct);
            if (user is null)
                return new ResolvedAvatar<T>(Guid.Empty, Result<T>.NotFound("That account could not be found."));

            if (!Guid.TryParse(user.azoaAvatarId, out var avatarId) || avatarId == Guid.Empty)
            {
                return new ResolvedAvatar<T>(
                    Guid.Empty,
                    Result<T>.Conflict("Your account is still being set up, so there is nothing to send yet."));
            }

            return new ResolvedAvatar<T>(avatarId, null);
        }

        /// <summary>The node's own key alphabet, applied before a key can reach a request body.</summary>
        private static bool IsAcceptableRequestKey(string? key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return false;

            var trimmed = key.Trim();
            if (trimmed.Length > MaximumRequestKeyLength)
                return false;

            foreach (var character in trimmed)
            {
                if (!char.IsAsciiLetterOrDigit(character) && character is not ('-' or '_' or ':' or '.'))
                    return false;
            }

            return true;
        }
    }
}
