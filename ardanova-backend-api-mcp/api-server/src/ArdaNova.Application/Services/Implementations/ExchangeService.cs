using System.Globalization;
using ArdaNova.Application.Common.Interfaces;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Interfaces;
using ArdaNova.Domain.Models.Entities;
using ArdaNova.Domain.Policies;
using AutoMapper;

namespace ArdaNova.Application.Services.Interfaces
{
    /// <summary>
    /// The rate ArdaNova DECLARES for one project's units. One hop, ARDA user-facing: the
    /// sponsor is told "N ARDA per unit", never a token price in dollars. The USD figure
    /// rides along only as the D16 at-the-time reference the node records on the receipt.
    /// </summary>
    /// <remarks>
    /// Amounts travel as exact decimal strings, invariant culture, because the node refuses a
    /// price it cannot represent exactly rather than rounding it (AZOA
    /// <c>DeclaredExchangeRate</c>). A double here would reintroduce the drift W5.3 removes.
    /// </remarks>
    public sealed record DeclaredProjectUnitRate(
        string ProjectTokenConfigId,
        string ArdaPerWholeProjectUnit,
        string UsdMajorUnitsPerWholeProjectUnit,
        int ProjectUnitDecimals,
        string Source,
        DateTimeOffset DeclaredAt,
        string? Version = null);

    /// <summary>Where a declared project-unit rate comes from. ArdaNova prices; the node records.</summary>
    public interface IProjectUnitRateSource
    {
        Task<Result<DeclaredProjectUnitRate>> GetDeclaredRateAsync(
            string projectTokenConfigId,
            CancellationToken ct = default);
    }

    /// <summary>One exchange, with every amount already decided by ArdaNova.</summary>
    /// <remarks>
    /// Both amounts are BASE units as <see cref="long"/> - the fixed-scale rule. The person the
    /// exchange is for is the route value on the node, never a field here.
    /// </remarks>
    public sealed record ArdaNovaExchangeCommand(
        string ChainType,
        bool IsBuy,
        string ProjectReference,
        string ProjectAssetId,
        long PlatformAmountBaseUnits,
        long ProjectUnitAmountBaseUnits,
        string ReservePartition,
        DeclaredProjectUnitRate Rate,
        DateTimeOffset? ReleaseNotBefore = null,
        string? Purpose = null,
        bool PreviewOnly = false);

    /// <summary>Application-owned projection of the node's exchange receipt.</summary>
    public sealed record ArdaNovaExchangeReceipt(
        string ReceiptReference,
        Guid? OperationId,
        string State,
        bool IsTerminal,
        bool RequiresReconciliation,
        bool Replayed,
        string? AmountSpentBaseUnits,
        string? AmountReceivedBaseUnits,
        string? ProjectCreditBaseUnits,
        string? SettlementReference,
        string? HoldReference,
        DateTimeOffset? HoldReleasesAt,
        string? FailureCode);

    /// <summary>
    /// Application-layer PORT onto the node's exchange door. The Infrastructure adapter is
    /// <c>AzoaExchangeClient</c> - the single adapter every ArdaNova caller uses for exchange
    /// operations, so no caller assembles its own node request.
    /// </summary>
    public interface IAzoaExchangeNode
    {
        /// <summary>Runs one exchange. Idempotent on <paramref name="idempotencyKey"/>.</summary>
        Task<Result<ArdaNovaExchangeReceipt>> ExchangeAsync(
            Guid avatarId,
            ArdaNovaExchangeCommand command,
            string idempotencyKey,
            CancellationToken ct = default);

        /// <summary>Reads the receipt for an exchange already submitted under this key.</summary>
        Task<Result<ArdaNovaExchangeReceipt>> GetReceiptAsync(
            string idempotencyKey,
            CancellationToken ct = default);
    }

    /// <summary>What the node reports a project's treasury account actually holds.</summary>
    public sealed record ProjectTreasuryBalance(
        string ProjectReference,
        string ChainType,
        string? Address,
        string? AssetId,
        long BalanceBaseUnits,
        DateTimeOffset ObservedAt);

    /// <summary>
    /// PORT for reading a project's node-side treasury balance (W3.7). ArdaNova's own rows are
    /// receipts; this read is the balance of record.
    /// </summary>
    public interface IAzoaProjectTreasuryReadModel
    {
        Task<Result<ProjectTreasuryBalance>> GetBalanceAsync(
            string chainType,
            string projectReference,
            string? assetId,
            CancellationToken ct = default);
    }

    /// <summary>What the node reports one avatar holds in one asset.</summary>
    public sealed record AvatarHolding(
        string AssetId,
        string? WalletAddress,
        long BalanceBaseUnits,
        DateTimeOffset ObservedAt);

    /// <summary>PORT for reading an avatar's custodial holdings. A person's ARDA lives here, not in a row.</summary>
    public interface IAzoaHoldingsReadModel
    {
        Task<Result<AvatarHolding>> GetHoldingAsync(
            Guid avatarId,
            string assetId,
            CancellationToken ct = default);
    }
}

namespace ArdaNova.Application.Services.Implementations
{
    /// <summary>Fixed-scale money. Exact minor units; no binary floating point on a value path.</summary>
    /// <remarks>
    /// Money is carried as an exact <c>decimal</c> and stored as a <c>long</c> count of minor
    /// units. Every narrowing conversion here is a <c>Try...</c> that can REFUSE, because the
    /// alternative - a cast that silently rounds or wraps - is how cents go missing.
    /// </remarks>
    public static class Money
    {
        /// <summary>Digits in the minor unit. ARDA carries two, so a person always thinks in dollars and cents.</summary>
        public const int MinorUnitDigits = 2;

        /// <summary>Minor units in one whole unit.</summary>
        public const decimal MinorUnitsPerWholeUnit = 100m;

        // The widest whole-unit amount that still SCALES without overflowing. Every conversion
        // below checks the INPUT against these before multiplying: scaling first and checking
        // after throws OverflowException, which is a crash where the contract promises a refusal.
        private static readonly decimal MaxWholeUnits = long.MaxValue / MinorUnitsPerWholeUnit;
        private static readonly decimal MinWholeUnits = long.MinValue / MinorUnitsPerWholeUnit;

        /// <summary>
        /// Exact conversion to minor units. REFUSES anything with sub-minor-unit precision rather
        /// than rounding it away - a silently dropped fraction of a cent is the defect this item
        /// exists to remove.
        /// </summary>
        public static bool TryToMinorUnits(decimal wholeUnits, out long minorUnits)
        {
            minorUnits = 0;
            if (wholeUnits < MinWholeUnits || wholeUnits > MaxWholeUnits)
                return false;

            var scaled = wholeUnits * MinorUnitsPerWholeUnit;
            if (scaled != decimal.Truncate(scaled))
                return false;

            minorUnits = (long)scaled;
            return true;
        }

        /// <summary>Minor units, rounding DOWN. Only for surfaces that deliberately drop the surplus.</summary>
        public static bool TryToMinorUnitsFloor(decimal wholeUnits, out long minorUnits)
        {
            minorUnits = 0;
            if (wholeUnits < MinWholeUnits || wholeUnits > MaxWholeUnits)
                return false;

            var scaled = decimal.Floor(wholeUnits * MinorUnitsPerWholeUnit);
            minorUnits = (long)scaled;
            return true;
        }

        /// <summary>Whole units, rounding DOWN. A partial unit is never a unit.</summary>
        public static bool TryToWholeUnitsFloor(decimal amount, out long wholeUnits)
        {
            wholeUnits = 0;
            var floored = decimal.Floor(amount);
            if (floored < long.MinValue || floored > long.MaxValue)
                return false;

            wholeUnits = (long)floored;
            return true;
        }

        /// <summary>Whole units back from minor units.</summary>
        public static decimal FromMinorUnits(long minorUnits) => minorUnits / MinorUnitsPerWholeUnit;

        /// <summary>Exact decimal string for the wire. Invariant culture; never a float round-trip.</summary>
        public static string ToExactString(decimal value) => value.ToString(CultureInfo.InvariantCulture);

        /// <summary>
        /// Lossy projection to <see cref="double"/>, for the pre-W5.3 surfaces Wave 8 still types
        /// that way. Only ever an OUTPUT: nothing reads a double back into money math.
        /// </summary>
        public static double ToDisplayDouble(decimal exact) => (double)exact;
    }

    /// <summary>
    /// ArdaNova's declared ARDA-per-project-unit rate, and the conversions built on it.
    /// </summary>
    /// <remarks>
    /// One hop, ARDA-first: project units price directly into ARDA. The USD figures on
    /// <see cref="ConversionPreviewDto"/> are a display projection of the exact decimal maths,
    /// kept only because <c>PayoutService</c> (Wave 8) still types them as <c>double</c>; they
    /// are never read back in.
    /// <para>
    /// The ARDA rate is BACKING-DERIVED, not pegged:
    /// <c>(indexFund + liquidReserve + operations) / ardaCirculatingSupply</c>. The only free
    /// variable is the genesis seed. It is refused unless it is POSITIVE, because every caller
    /// divides by it and a genesis treasury divides out to zero.
    /// </para>
    /// <para>
    /// A project unit has TWO prices, told apart by <see cref="DeclaredProjectUnitRate.Source"/>:
    /// the OPENING OFFER its founder named, which every sponsor in the primary raise pays, and
    /// the price its backers have set, which takes over only once the ask has been MET. Without
    /// the first, the first sponsorship of any project is unpriceable; without the handover rule,
    /// the price collapses the moment one sponsorship settles and later sponsors buy the same
    /// units for a fraction of what the first paid.
    /// </para>
    /// </remarks>
    public class ExchangeService : IExchangeService, IProjectUnitRateSource
    {
        /// <summary>What the node records for a price the project's own backers have set.</summary>
        public const string DeclaredRateSource = "ardanova:treasury-rate";

        /// <summary>
        /// What the node records while the primary raise is still open and the project is priced
        /// at its OPENING OFFER - the ask over the sponsor stake on offer. It rides the
        /// funding intent's <c>declaredRateSource</c> and the hashed terms snapshot, so the receipt
        /// says which of the two prices the sponsor was quoted rather than leaving it inferred.
        /// </summary>
        public const string OfferingRateSource = "ardanova:offering-price";

        private readonly IRepository<ProjectTokenConfig> _projectTokenConfigRepository;
        private readonly IRepository<PlatformTreasury> _treasuryRepository;
        private readonly IMapper _mapper;

        public ExchangeService(
            IRepository<ProjectTokenConfig> projectTokenConfigRepository,
            IRepository<PlatformTreasury> treasuryRepository,
            IMapper mapper)
        {
            _projectTokenConfigRepository = projectTokenConfigRepository;
            _treasuryRepository = treasuryRepository;
            _mapper = mapper;
        }

        public async Task<Result<double>> GetProjectTokenValueAsync(string projectTokenConfigId, CancellationToken ct = default)
        {
            var exact = await GetProjectUnitValueExactAsync(projectTokenConfigId, ct);
            return exact.IsSuccess
                ? Result<double>.Success(Money.ToDisplayDouble(exact.Value.UsdPerWholeUnit))
                : Result<double>.Failure(exact.Error ?? "Project token valuation failed");
        }

        public async Task<Result<double>> GetArdaValueAsync(CancellationToken ct = default)
        {
            var exact = await GetArdaValueExactAsync(ct);
            return exact.IsSuccess
                ? Result<double>.Success(Money.ToDisplayDouble(exact.Value))
                : Result<double>.Failure(exact.Error ?? "ARDA valuation failed");
        }

        /// <inheritdoc />
        public async Task<Result<DeclaredProjectUnitRate>> GetDeclaredRateAsync(
            string projectTokenConfigId,
            CancellationToken ct = default)
        {
            var config = await _projectTokenConfigRepository.GetByIdAsync(projectTokenConfigId, ct);
            if (config == null)
                return Result<DeclaredProjectUnitRate>.NotFound($"Project token configuration not found: {projectTokenConfigId}");

            var unitValue = await GetProjectUnitValueExactAsync(projectTokenConfigId, ct);
            if (!unitValue.IsSuccess)
                return Result<DeclaredProjectUnitRate>.Failure(unitValue.Error ?? "Project token valuation failed");

            var ardaValue = await GetArdaValueExactAsync(ct);
            if (!ardaValue.IsSuccess)
                return Result<DeclaredProjectUnitRate>.Failure(ardaValue.Error ?? "ARDA valuation failed");

            // One hop: how much ARDA one whole project unit costs. The sponsor never sees a
            // token price in dollars; the dollar figure only rides along as the D16 reference.
            var ardaPerUnit = unitValue.Value.UsdPerWholeUnit / ardaValue.Value;

            return Result<DeclaredProjectUnitRate>.Success(new DeclaredProjectUnitRate(
                ProjectTokenConfigId: projectTokenConfigId,
                ArdaPerWholeProjectUnit: Money.ToExactString(ardaPerUnit),
                UsdMajorUnitsPerWholeProjectUnit: Money.ToExactString(unitValue.Value.UsdPerWholeUnit),
                ProjectUnitDecimals: config.assetScale ?? 0,
                Source: unitValue.Value.Source,
                DeclaredAt: DateTimeOffset.UtcNow,
                Version: null));
        }

        public async Task<Result<ConversionPreviewDto>> CalculateConversionAsync(
            string projectTokenConfigId,
            int tokenAmount,
            CancellationToken ct = default)
        {
            if (tokenAmount <= 0)
            {
                return Result<ConversionPreviewDto>.Failure("Token amount must be positive");
            }

            var unitValue = await GetProjectUnitValueExactAsync(projectTokenConfigId, ct);
            if (!unitValue.IsSuccess)
            {
                return Result<ConversionPreviewDto>.Failure(
                    unitValue.Error ?? "Project token valuation failed");
            }

            var ardaValue = await GetArdaValueExactAsync(ct);
            if (!ardaValue.IsSuccess)
            {
                return Result<ConversionPreviewDto>.Failure(
                    ardaValue.Error ?? "ARDA valuation failed");
            }

            // project units -> ARDA in ONE hop. The dollar figures below are derived from these
            // exact decimals for display; they are never an input.
            var ardaPerUnit = unitValue.Value.UsdPerWholeUnit / ardaValue.Value;
            var ardaExact = tokenAmount * ardaPerUnit;

            if (!Money.TryToMinorUnitsFloor(ardaExact, out var ardaMinorUnits)
                || !Money.TryToWholeUnitsFloor(ardaExact, out var ardaWholeUnits))
            {
                return Result<ConversionPreviewDto>.Failure(
                    "The converted amount is outside the range this platform can represent exactly.");
            }

            var usdExact = tokenAmount * unitValue.Value.UsdPerWholeUnit;

            var preview = new ConversionPreviewDto
            {
                SourceTokenAmount = tokenAmount,
                ArdaPerProjectUnit = ardaPerUnit,
                ArdaAmountExact = ardaExact,
                ArdaMinorUnits = ardaMinorUnits,
                ProjectUnitValueUsdExact = unitValue.Value.UsdPerWholeUnit,
                ArdaValueUsdExact = ardaValue.Value,
                RateSource = unitValue.Value.Source,
                RateDeclaredAt = DateTimeOffset.UtcNow,

                // Pre-W5.3 display surface; Wave 8 removes these with PayoutService.
                ProjectTokenValueUsd = Money.ToDisplayDouble(unitValue.Value.UsdPerWholeUnit),
                ArdaValueUsd = Money.ToDisplayDouble(ardaValue.Value),
                UsdValue = Money.ToDisplayDouble(usdExact),
                ArdaAmount = ardaWholeUnits,
            };

            return Result<ConversionPreviewDto>.Success(preview);
        }

        public async Task<Result<TreasuryStatusDto>> GetTreasuryStatusAsync(CancellationToken ct = default)
        {
            var treasuries = await _treasuryRepository.GetAllAsync(ct);
            var treasury = treasuries.FirstOrDefault();

            if (treasury == null)
            {
                return Result<TreasuryStatusDto>.Failure("Platform treasury not found");
            }

            // The bucket split is a POLICY VIEW, not a balance. Without a chain read behind it
            // this status reports no balance at all rather than a database number that looks
            // like one. TreasuryService owns the chain-backed variant.
            var dto = _mapper.Map<TreasuryStatusDto>(treasury);
            return Result<TreasuryStatusDto>.Success(
                TreasuryPolicyView.WithoutChainBalance(dto, TreasuryPolicyView.NoChainReadOnThisRoute));
        }

        /// <summary>One project-unit price and which of the two prices produced it.</summary>
        /// <remarks>
        /// The origin travels WITH the number instead of being re-derived downstream, because the
        /// receipt has to be able to say which price the sponsor was quoted.
        /// </remarks>
        private readonly record struct ProjectUnitValuation(decimal UsdPerWholeUnit, string Source);

        /// <summary>Exact USD value of one whole project unit, and which price produced it.</summary>
        /// <remarks>
        /// <para>
        /// Two prices, one method, and BOTH numerators are dollars: <c>fundingGoal</c> and
        /// <c>fundingRaised</c> are read together through <see cref="ProjectFundingLedger"/>,
        /// which is the only place the pair is interpreted. Before that rule existed the goal
        /// carried the founder's ARDA figure and this method divided it into a field declared
        /// as dollars, so the first sponsor of every project paid about a hundred times the
        /// founder's price for a hundredth of the promised units.
        /// </para>
        /// <para>
        /// While the primary raise is OPEN the price is the founder's declared offering - the
        /// ask over THE STAKE ACTUALLY ON OFFER - and it does not move: every sponsor in the
        /// raise buys at the price the disclosure named, so sponsor #1 is not charged a fortune
        /// for what sponsor #2 gets cheaply. Only once the ask is MET does the price become the
        /// one the backers set, raised over the same denominator, which at that moment equals the
        /// offering and can only rise from there. The curve is continuous at the goal.
        /// </para>
        /// <para>
        /// The denominator is <see cref="ProjectRaiseOffering.TryUnitsOnOffer"/> - the declared
        /// SPONSOR partition - not the whole supply. Dividing the ask over the whole supply while
        /// the sponsorship path refuses to sell past the sponsor partition capped every raise at
        /// <c>(sponsor partition / total supply) x ask</c>, so a project that set aside anything
        /// for its workers could never reach its own goal and Gate 1 never cleared. Both halves
        /// now read one number, and selling the whole offer raises exactly the ask.
        /// </para>
        /// <para>
        /// Both success paths are provably positive: the units on offer are checked positive
        /// before dividing, and each branch checks its own numerator positive too.
        /// </para>
        /// </remarks>
        private async Task<Result<ProjectUnitValuation>> GetProjectUnitValueExactAsync(
            string projectTokenConfigId,
            CancellationToken ct)
        {
            var config = await _projectTokenConfigRepository.GetByIdAsync(projectTokenConfigId, ct);
            if (config == null)
            {
                return Result<ProjectUnitValuation>.Failure(
                    $"Project token configuration not found: {projectTokenConfigId}");
            }

            if (config.totalSupply <= 0)
            {
                return Result<ProjectUnitValuation>.Failure(
                    "Cannot calculate token value: total supply is zero or negative");
            }

            if (!ProjectFundingLedger.TryRead(config, out var funding))
            {
                return Result<ProjectUnitValuation>.Failure(
                    "This project's funding figures cannot be read as an exact amount, so no price can be quoted");
            }

            if (funding.RaisedIsBroken)
            {
                // Not an unsponsored project - a broken figure. Falling through to the opening
                // offer would price it anyway and hide that, so it is refused by name.
                return Result<ProjectUnitValuation>.Failure(
                    "This project's funding total is below zero, so no price can be quoted");
            }

            // The ask is priced over the stake that is FOR SALE. The sponsorship path refuses to
            // commit past this same partition, so pricing over any wider denominator sells out
            // the offer before the ask is met and the raise can never complete.
            if (!ProjectRaiseOffering.TryUnitsOnOffer(config, out var unitsOnOffer))
            {
                return Result<ProjectUnitValuation>.Failure(
                    "This project has not set aside any stake for sponsors, so it has no price");
            }

            // The declared offering holds for the WHOLE primary raise, not just its first
            // sponsorship. Handing sponsor #2 `raised / unitsOnOffer` while the raise is barely
            // begun would quote them a price orders of magnitude below what sponsor #1 paid.
            if (funding.RaiseIsOpen)
            {
                return Result<ProjectUnitValuation>.Success(
                    new ProjectUnitValuation(funding.GoalUsd / unitsOnOffer, OfferingRateSource));
            }

            if (!funding.NothingRaised)
            {
                return Result<ProjectUnitValuation>.Success(
                    new ProjectUnitValuation(funding.RaisedUsd / unitsOnOffer, DeclaredRateSource));
            }

            return Result<ProjectUnitValuation>.Failure(
                "This project has not set an amount to raise, so it has no opening price yet");
        }

        /// <summary>Exact ARDA value: total backing over circulating supply. Backing-derived, never pegged.</summary>
        /// <remarks>
        /// The computed rate is refused unless it is POSITIVE. Every caller DIVIDES BY this number,
        /// and a genesis treasury - three bucket balances still at 0 - divides out to zero, which
        /// made an unhandled <c>DivideByZeroException</c> on a value path (OI-34). The guard lives
        /// here, at the one place the rate is produced, rather than at each division site.
        /// <c>SwapService</c> refuses the identical computation in the same words.
        /// </remarks>
        private async Task<Result<decimal>> GetArdaValueExactAsync(CancellationToken ct)
        {
            var treasuries = await _treasuryRepository.GetAllAsync(ct);
            var treasury = treasuries.FirstOrDefault();

            if (treasury == null)
            {
                return Result<decimal>.Failure("Platform treasury not found");
            }

            if (treasury.ardaCirculatingSupply == 0)
            {
                return Result<decimal>.Failure("Cannot calculate ARDA value: circulating supply is zero");
            }

            if (!TryExactFromStoredAmount(treasury.indexFundBalance, out var indexFund)
                || !TryExactFromStoredAmount(treasury.liquidReserveBalance, out var liquidReserve)
                || !TryExactFromStoredAmount(treasury.operationsBalance, out var operations))
            {
                return Result<decimal>.Failure(
                    "The treasury's backing figures cannot be read as an exact amount, so no rate can be quoted");
            }

            var ardaValue = (indexFund + liquidReserve + operations) / treasury.ardaCirculatingSupply;

            if (ardaValue <= 0)
            {
                return Result<decimal>.Failure("ARDA has no backing, so no rate can be quoted");
            }

            return Result<decimal>.Success(ardaValue);
        }

        /// <summary>Exact decimal from a stored double, refusing what the cast would throw on.</summary>
        /// <remarks>
        /// These are database doubles - the same figures Wave 5 nulls out of the treasury status as
        /// too untrustworthy to SHOW, while they still price every rate declared here. A named
        /// refusal is an honest answer; an <c>OverflowException</c> on a value path is a 500.
        /// </remarks>
        private static bool TryExactFromStoredAmount(double stored, out decimal exact)
        {
            exact = 0m;
            if (!double.IsFinite(stored))
            {
                return false;
            }

            try
            {
                exact = (decimal)stored;
                return true;
            }
            catch (OverflowException)
            {
                return false;
            }
        }
    }
}
