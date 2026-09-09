namespace ArdaNova.Application.Services.Implementations;

using System.Data.Common;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ArdaNova.Application.Common.Interfaces;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Interfaces;
using ArdaNova.Domain.Models.Entities;
using ArdaNova.Domain.Models.Enums;
using ArdaNova.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Sponsorship is an exchange: the sponsor's platform units become real project units on the node.
/// </summary>
/// <remarks>
/// The shape of this service is one sentence long - lock the terms, ask the node once, record what
/// the node did. The reasoning lives in
/// <c>src/ArdaNova.Application/Services/Implementations/AGENTS.md</c> section sponsorship.
/// <para>
/// There is no card here. The fiat rail terminates on the node and only ever buys platform units,
/// so by the time a person presses Support they already hold them and a sponsorship never touches a
/// currency amount. The old "USD"-cents settlement is gone, and the card-webhook entry points below
/// refuse rather than linger.
/// </para>
/// <para>
/// One idempotency key, <c>{avatarId}:funding-intent:{intentId}</c>, is written onto the settlement
/// row as well as the intent. The synchronous attempt this service makes and the outbox worker that
/// covers for it therefore address the SAME node operation, so a second attempt can only ever read
/// back the first one's receipt.
/// </para>
/// </remarks>
public sealed class FundingIntentService : IFundingIntentService
{
    /// <summary>
    /// How long freshly issued stake sits in the node's hold before it is the sponsor's to move
    /// (D14 / Q5). Declared here rather than read from configuration because no operator setting
    /// exists yet; W6.5 owns <c>AzoaSettings</c> and can lift it there without changing this file's
    /// behaviour. Surfaced to the sponsor as "yours after N days", never as a hold or a chain word.
    /// </summary>
    public static readonly TimeSpan StakeHoldWindow = TimeSpan.FromDays(30);

    /// <summary>Attempts this service makes on one intent before it leaves the work to the outbox worker.</summary>
    private const int MaximumIntentAttempts = 5;

    /// <summary>
    /// How long the outbox waits before covering for the synchronous attempt. Without it the worker
    /// and the request race for the same settlement; with it the worker only ever picks up work the
    /// request failed to finish.
    /// </summary>
    private static readonly TimeSpan OutboxHandoverDelay = TimeSpan.FromMinutes(2);

    private const string FundAction = "fund a project";
    private const string ReceiveAction = "receive project funding";
    private const int TermsSchemaVersion = 2;
    private const string ProjectUnitLeg = "projectUnit";
    private const string ExchangePurpose = "sponsorship";

    /// <summary>
    /// Currency codes a project unit may never be symbolised by. It mirrors the gateway's own
    /// refusal so a sponsor is told at authorization time, instead of watching a settlement die
    /// later for a reason no page can explain.
    /// </summary>
    private static readonly HashSet<string> CurrencyCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        "USD", "EUR", "GBP", "CAD", "AUD", "NZD", "CHF", "JPY", "SEK", "NOK", "DKK",
    };

    private readonly IRepository<User> _userRepository;
    private readonly IRepository<Project> _projectRepository;
    private readonly IRepository<ProjectTokenConfig> _tokenConfigRepository;
    private readonly IRepository<FundingIntent> _intentRepository;
    private readonly IRepository<EconomicSettlement> _settlementRepository;
    private readonly IRepository<EconomicOutbox> _outboxRepository;
    private readonly IRepository<ProjectInvestment> _investmentRepository;
    private readonly IRepository<PlatformTreasuryTransaction> _treasuryReceiptRepository;
    private readonly IRepository<PlatformTreasury> _treasuryRepository;
    private readonly IAzoaSettlementGateway _settlementGateway;
    private readonly IProjectUnitRateSource _rateSource;
    private readonly IProjectGateService _projectGate;
    private readonly IFundingSettlementReadiness _settlementReadiness;
    private readonly IAzoaValueEligibilityService _valueEligibility;
    private readonly IUnitOfWork _unitOfWork;

    public FundingIntentService(
        IRepository<User> userRepository,
        IRepository<Project> projectRepository,
        IRepository<ProjectTokenConfig> tokenConfigRepository,
        IRepository<FundingIntent> intentRepository,
        IRepository<EconomicSettlement> settlementRepository,
        IRepository<EconomicOutbox> outboxRepository,
        IRepository<ProjectInvestment> investmentRepository,
        IRepository<PlatformTreasuryTransaction> treasuryReceiptRepository,
        IRepository<PlatformTreasury> treasuryRepository,
        IAzoaSettlementGateway settlementGateway,
        IProjectUnitRateSource rateSource,
        IProjectGateService projectGate,
        IFundingSettlementReadiness settlementReadiness,
        IAzoaValueEligibilityService valueEligibility,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _projectRepository = projectRepository;
        _tokenConfigRepository = tokenConfigRepository;
        _intentRepository = intentRepository;
        _settlementRepository = settlementRepository;
        _outboxRepository = outboxRepository;
        _investmentRepository = investmentRepository;
        _treasuryReceiptRepository = treasuryReceiptRepository;
        _treasuryRepository = treasuryRepository;
        _settlementGateway = settlementGateway;
        _rateSource = rateSource;
        _projectGate = projectGate;
        _settlementReadiness = settlementReadiness;
        _valueEligibility = valueEligibility;
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc/>
    public async Task<Result<FundingCheckoutDto>> CreateCheckoutAsync(
        CreateFundingIntentDto request,
        string actorId,
        string idempotencyKey,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(actorId))
            return Result<FundingCheckoutDto>.Unauthorized("A sponsor is required");
        if (!Guid.TryParse(idempotencyKey, out _))
            return Result<FundingCheckoutDto>.ValidationError("A canonical idempotency key is required");
        if (!TryParsePlatformAmount(request.Amount, out var requestedBaseUnits))
            return Result<FundingCheckoutDto>.ValidationError(
                "The amount must be a positive figure with at most two decimal places");
        if (string.IsNullOrWhiteSpace(request.DisclosureVersion) || request.DisclosureVersion.Length > 100)
            return Result<FundingCheckoutDto>.ValidationError("A disclosure version is required");

        // Half the checkout gate: this deployment can settle at all. The other half - this person
        // and this project - is below, and neither half is skippable.
        if (!_settlementReadiness.IsReady)
            return Result<FundingCheckoutDto>.Conflict(_settlementReadiness.UnavailableReason);

        var actor = await _userRepository.GetByIdAsync(actorId, ct);
        if (actor is null)
            return Result<FundingCheckoutDto>.NotFound("Sponsor not found");

        var funderReadiness = await _valueEligibility.RequireReadyAsync(actorId, FundAction, ct);
        if (!funderReadiness.IsSuccess)
            return ValueEligibilityFailure.Propagate<AzoaCustodialAccountStatusDto, FundingCheckoutDto>(
                funderReadiness);

        var avatarId = funderReadiness.Value?.AvatarId;
        if (string.IsNullOrWhiteSpace(avatarId))
            return Result<FundingCheckoutDto>.Conflict(
                "Your account has no node reference yet, so nothing was taken.");

        // Replay is answered BEFORE fundability is re-checked. A project whose gate moved on since
        // the first call must still be able to finish the sponsorship it already accepted.
        var existing = await _intentRepository.FindOneAsync(
            intent => intent.funderUserId == actorId && intent.idempotencyKey == idempotencyKey,
            ct);
        if (existing is not null)
            return await ReplayAsync(existing, request, requestedBaseUnits, actorId, ct);

        var fundable = await LoadFundableAsync(request.ProjectTokenConfigId, ct);
        if (!fundable.IsSuccess)
            return Propagate<FundableProject, FundingCheckoutDto>(fundable);

        var (config, project, assetScale) = (fundable.Value!.Config, fundable.Value.Project, fundable.Value.AssetScale);

        if (!string.Equals(project.createdById, actorId, StringComparison.Ordinal))
        {
            var recipientReadiness = await _valueEligibility.RequireReadyAsync(
                project.createdById,
                ReceiveAction,
                ct);
            if (!recipientReadiness.IsSuccess)
                return ValueEligibilityFailure.Propagate<AzoaCustodialAccountStatusDto, FundingCheckoutDto>(
                    recipientReadiness,
                    "This project cannot receive sponsorship until its owner completes account setup.");
        }

        var declaredRate = await _rateSource.GetDeclaredRateAsync(config.id, ct);
        if (!declaredRate.IsSuccess)
            return Result<FundingCheckoutDto>.Conflict(
                "We could not price this project right now, so nothing was taken.");

        if (!TryQuote(requestedBaseUnits, declaredRate.Value!, assetScale, out var quote, out var quoteError))
            return Result<FundingCheckoutDto>.ValidationError(quoteError!);

        var intent = BuildIntent(
            actorId,
            avatarId!,
            idempotencyKey,
            request,
            project,
            config,
            assetScale,
            requestedBaseUnits,
            quote,
            declaredRate.Value!);
        var settlement = BuildExchangeSettlement(intent);
        var outbox = BuildOutbox(settlement.id, intent.createdAt);
        intent.settlementId = settlement.id;

        try
        {
            await _unitOfWork.BeginTransactionAsync(ct);
            await _intentRepository.AddAsync(intent, ct);
            await _settlementRepository.AddAsync(settlement, ct);
            await _outboxRepository.AddAsync(outbox, ct);
            await _unitOfWork.SaveChangesAsync(ct);
            await _unitOfWork.CommitTransactionAsync(ct);
        }
        catch (DbUpdateException failure) when (IsUniquenessViolation(failure))
        {
            await _unitOfWork.RollbackTransactionAsync(CancellationToken.None);
            _unitOfWork.ClearTrackedChanges();
            var concurrent = await _intentRepository.FindOneAsync(
                item => item.funderUserId == actorId && item.idempotencyKey == idempotencyKey,
                ct);
            return concurrent is null
                ? throw new InvalidOperationException(
                    "A sponsorship uniqueness conflict did not expose a durable intent.")
                : await ReplayAsync(concurrent, request, requestedBaseUnits, actorId, ct);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(CancellationToken.None);
            throw;
        }

        return await DispatchAndApplyAsync(intent, settlement, outbox, config, project, ct);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// A read that can finish work. The node may confirm after the request that started it has
    /// gone, so the poll the success page already makes is what promotes the intent. It is
    /// idempotent and bounded, and it never dispatches: dispatch belongs to the outbox worker.
    /// </remarks>
    public async Task<Result<FundingIntentStatusDto>> GetStatusAsync(
        string intentId,
        string actorId,
        CancellationToken ct = default)
    {
        var intent = await _intentRepository.GetByIdAsync(intentId, ct);
        if (intent is null)
            return Result<FundingIntentStatusDto>.NotFound("Sponsorship not found");
        if (!string.Equals(intent.funderUserId, actorId, StringComparison.Ordinal))
            return Result<FundingIntentStatusDto>.Forbidden("You cannot view this sponsorship");

        if (intent.status == FundingIntentStatus.SETTLEMENT_PENDING)
            await AdvancePendingAsync(intent, ct);

        return Result<FundingIntentStatusDto>.Success(ToStatus(intent));
    }

    // -- Authorization-time loading and pricing ------------------------------

    /// <summary>A project that may take a sponsorship right now, with the precision its units carry.</summary>
    private sealed record FundableProject(ProjectTokenConfig Config, Project Project, int AssetScale);

    /// <summary>Loads the project and its token configuration only when both can actually take a sponsorship.</summary>
    /// <summary>SQLSTATE for a unique-constraint violation - the only replayable write failure.</summary>
    private const string UniqueViolationSqlState = "23505";

    /// <summary>
    /// True only when the write failed on a uniqueness constraint, which is the one failure a
    /// concurrent request with the same idempotency key can cause. Every other
    /// <see cref="DbUpdateException"/> - an undefined column, a foreign-key or check violation, a
    /// bad cast - means the write was WRONG, not raced, and must surface as itself. Reading them
    /// all as a race is what let a malformed value present as "a sponsorship uniqueness conflict
    /// did not expose a durable intent" (OI-36). <c>SqlState</c> is provider-agnostic
    /// (<see cref="DbException"/>), so this needs no Npgsql reference in the Application layer.
    /// </summary>
    private static bool IsUniquenessViolation(DbUpdateException failure)
        => failure.InnerException is DbException { SqlState: UniqueViolationSqlState };

    private async Task<Result<FundableProject>> LoadFundableAsync(
        string projectTokenConfigId,
        CancellationToken ct)
    {
        var config = await _tokenConfigRepository.GetByIdAsync(projectTokenConfigId, ct);
        if (config is null)
            return Result<FundableProject>.NotFound("Project token configuration not found");

        // The gate the item names: a project with no minted asset has nothing to sell, so this
        // refuses before any row exists rather than discovering it at the node.
        if (string.IsNullOrWhiteSpace(config.assetId))
            return Result<FundableProject>.ValidationError(
                "This project has no units to offer yet");
        if (config.assetScale is not int assetScale || !FixedScaleAmount.IsSupportedScale(assetScale))
            return Result<FundableProject>.ValidationError(
                "This project's units have an unsupported precision");
        if (!config.IsFundable)
            return Result<FundableProject>.ValidationError(
                "This project is not set up to receive sponsorship yet");
        if (config.gateStatus != ProjectGateStatus.FUNDING)
            return Result<FundableProject>.ValidationError(
                "Sponsorship is not open for this project");
        if (CurrencyCodes.Contains(config.unitName.Trim()))
            return Result<FundableProject>.ValidationError(
                "This project's unit symbol collides with a currency code and cannot be exchanged");
        if (string.IsNullOrWhiteSpace(config.treasuryReference) || string.IsNullOrWhiteSpace(config.assetChain))
            return Result<FundableProject>.ValidationError(
                "This project's account on the node is not ready to receive sponsorship");

        var project = await _projectRepository.GetByIdAsync(config.projectId, ct);
        if (project is null)
            return Result<FundableProject>.NotFound("Project not found");
        if (!project.commerceEnabled)
            return Result<FundableProject>.ValidationError(
                "Sponsorship is not enabled for this project");
        if (project.status is ProjectStatus.DRAFT or ProjectStatus.CANCELLED)
            return Result<FundableProject>.ValidationError(
                "This project is not eligible for sponsorship");

        return Result<FundableProject>.Success(new FundableProject(config, project, assetScale));
    }

    /// <summary>What one sponsorship buys, priced once and never re-derived after authorization.</summary>
    private readonly record struct SponsorshipQuote(
        long Quantity,
        long PlatformBaseUnits,
        long UsdReferenceMinorUnits,
        string ProjectUnitBaseUnits);

    /// <summary>Turns "this much of my balance" into whole project units and the exact amount actually spent.</summary>
    /// <remarks>
    /// Two refusals here are money controls, not validation niceties. A rate of zero would make the
    /// quantity unbounded - a person would receive the whole project for nothing - so a non-positive
    /// rate is refused outright; the offering price a project has before its first sponsorship is
    /// <see cref="IProjectUnitRateSource"/>'s to supply. And the charge is the rounded-UP cost of
    /// the WHOLE units bought, never the whole amount offered, so the remainder below one unit stays
    /// with the sponsor instead of evaporating.
    /// </remarks>
    private static bool TryQuote(
        long requestedBaseUnits,
        DeclaredProjectUnitRate rate,
        int assetScale,
        out SponsorshipQuote quote,
        out string? error)
    {
        quote = default;
        error = null;

        if (!TryParseExact(rate.ArdaPerWholeProjectUnit, out var platformPerUnit) || platformPerUnit <= 0)
        {
            error = "This project has no price yet, so nothing was taken.";
            return false;
        }

        if (!TryParseExact(rate.UsdMajorUnitsPerWholeProjectUnit, out var referencePerUnit) || referencePerUnit < 0)
        {
            error = "This project has no price yet, so nothing was taken.";
            return false;
        }

        var offered = Money.FromMinorUnits(requestedBaseUnits);
        if (!Money.TryToWholeUnitsFloor(offered / platformPerUnit, out var quantity)
            || quantity <= 0
            || quantity > int.MaxValue)
        {
            error = quantity > int.MaxValue
                ? "That number of units is outside the range this project can issue."
                : "That amount does not buy a whole unit of this project.";
            return false;
        }

        if (!TryToMinorUnitsCeiling(quantity * platformPerUnit, out var chargedBaseUnits)
            || chargedBaseUnits <= 0
            || chargedBaseUnits > requestedBaseUnits)
        {
            error = "That amount cannot be exchanged exactly, so nothing was taken.";
            return false;
        }

        if (!Money.TryToMinorUnitsFloor(quantity * referencePerUnit, out var referenceMinorUnits)
            || referenceMinorUnits < 0)
        {
            error = "That amount cannot be exchanged exactly, so nothing was taken.";
            return false;
        }

        if (!FixedScaleAmount.TryFromPositiveDecimal(quantity, assetScale, out var projectUnits))
        {
            error = "That number of units is outside the range this project can issue.";
            return false;
        }

        quote = new SponsorshipQuote(quantity, chargedBaseUnits, referenceMinorUnits, projectUnits.BaseUnits);
        return true;
    }

    /// <summary>Builds the intent with every locked field written exactly once.</summary>
    private static FundingIntent BuildIntent(
        string actorId,
        string avatarId,
        string idempotencyKey,
        CreateFundingIntentDto request,
        Project project,
        ProjectTokenConfig config,
        int assetScale,
        long requestedBaseUnits,
        SponsorshipQuote quote,
        DeclaredProjectUnitRate rate)
    {
        var now = DateTime.UtcNow;
        var intentId = Guid.NewGuid().ToString("D");
        var releaseNotBefore = now.Add(StakeHoldWindow);
        var intent = new FundingIntent
        {
            id = intentId,
            semanticKey = $"funding:{actorId}:{idempotencyKey}",
            idempotencyKey = idempotencyKey,
            status = FundingIntentStatus.DRAFT,
            funderUserId = actorId,
            projectId = project.id,
            projectTokenConfigId = config.id,
            disclosureVersion = request.DisclosureVersion,
            platformAssetCode = ProjectFundingUnits.Arda,
            platformAmountBaseUnits = quote.PlatformBaseUnits,
            platformScale = Money.MinorUnitDigits,
            requestedPlatformBaseUnits = requestedBaseUnits,
            projectUnitAssetCode = config.unitName,
            projectUnitQuantity = quote.Quantity,
            projectUnitScale = assetScale,
            projectAssetId = config.assetId!,
            usdReferenceMinorUnits = quote.UsdReferenceMinorUnits,
            declaredRateArdaPerUnit = rate.ArdaPerWholeProjectUnit,
            declaredRateUsdPerUnit = rate.UsdMajorUnitsPerWholeProjectUnit,
            declaredRateSource = rate.Source,
            declaredRateDeclaredAt = rate.DeclaredAt.UtcDateTime,
            declaredRateVersion = rate.Version,
            eligibilitySnapshot = CreateEligibilitySnapshot(project, config),
            exchangeIdempotencyKey = ExchangeIdempotencyKey(avatarId, intentId),
            releaseNotBefore = releaseNotBefore,
            settlementAttemptCount = 0,
            createdAt = now,
            updatedAt = now,
        };

        intent.termsSnapshot = CreateTermsSnapshot(intent, config, quote, rate, releaseNotBefore);
        intent.termsHash = Hash(intent.termsSnapshot);
        return intent;
    }

    /// <summary>The one key a sponsorship is ever settled under, everywhere.</summary>
    private static string ExchangeIdempotencyKey(string avatarId, string intentId)
        => $"{avatarId}:funding-intent:{intentId}";

    // -- The node call and its outcomes --------------------------------------

    private async Task<Result<FundingCheckoutDto>> DispatchAndApplyAsync(
        FundingIntent intent,
        EconomicSettlement settlement,
        EconomicOutbox outbox,
        ProjectTokenConfig config,
        Project project,
        CancellationToken ct)
    {
        intent.settlementAttemptCount += 1;
        var outcome = await _settlementGateway.DispatchAsync(BuildSettlementRequest(intent), ct);
        return outcome.Outcome switch
        {
            AzoaSettlementGatewayOutcome.Accepted => await CompleteAsync(
                intent, settlement, outbox, config, project, outcome, ct),
            AzoaSettlementGatewayOutcome.Unknown => await ParkForReconciliationAsync(
                intent, settlement, outbox, project, outcome, ct),
            _ when IsReadinessLapse(outcome.Code) => await HoldAsync(
                intent, settlement, outbox, outcome, ct),
            _ => await LeaveToOutboxAsync(intent, settlement, outbox, project, outcome, ct),
        };
    }

    /// <summary>
    /// Everything the confirmed exchange means, in one transaction: the stake, the treasury receipt,
    /// the funding gate and the settled intent. A partial write here would be a person who paid and
    /// owns nothing, so there is exactly one commit.
    /// </summary>
    private async Task<Result<FundingCheckoutDto>> CompleteAsync(
        FundingIntent intent,
        EconomicSettlement settlement,
        EconomicOutbox outbox,
        ProjectTokenConfig config,
        Project project,
        AzoaSettlementGatewayResult outcome,
        CancellationToken ct)
    {
        var receipt = EconomicSettlementReceipt.FromJson(outcome.Receipt);
        var now = DateTime.UtcNow;
        var operationId = outcome.OperationId!;
        var platformAssetId = await ResolvePlatformAssetIdAsync(ct);

        try
        {
            await _unitOfWork.BeginTransactionAsync(ct);

            settlement.status = EconomicSettlementStatus.CONFIRMED;
            settlement.azoaOperationId = operationId;
            settlement.azoaReceipt = outcome.Receipt;
            settlement.azoaReplayed = outcome.Replayed;
            settlement.azoaGrossBaseUnits = receipt?.GrossBaseUnits;
            settlement.azoaFeeBaseUnits = receipt?.FeeBaseUnits;
            settlement.azoaNetBaseUnits = receipt?.NetBaseUnits;
            settlement.submittedAt ??= now;
            settlement.confirmedAt = now;
            settlement.failureCode = null;
            settlement.failureDetail = null;
            settlement.updatedAt = now;
            settlement.version += 1;
            await _settlementRepository.UpdateAsync(settlement, ct);

            outbox.status = EconomicOutboxStatus.COMPLETED;
            outbox.dispatchedAt ??= now;
            outbox.updatedAt = now;
            await _outboxRepository.UpdateAsync(outbox, ct);

            intent.status = FundingIntentStatus.SETTLED;
            intent.azoaOperationId = operationId;
            intent.holdReference = receipt?.HoldReference;
            // The node's own release time wins when it gives one. The declared window is what
            // ArdaNova asked for, and only one of the two can be the promise a person is shown.
            intent.releaseNotBefore = receipt?.HoldReleasesAt?.UtcDateTime ?? intent.releaseNotBefore;
            intent.settledAt = now;
            intent.holdingReasonCode = null;
            intent.holdingReasonDetail = null;
            intent.updatedAt = now;
            await _intentRepository.UpdateAsync(intent, ct);

            // One sponsorship, one stake. The investment carries the intent's own id, so a second
            // promotion of the same intent cannot mint a second row even if two callers race.
            var existingInvestment = await _investmentRepository.GetByIdAsync(intent.id, ct);
            if (existingInvestment is null)
            {
                await _investmentRepository.AddAsync(BuildInvestment(intent, now), ct);
                await _treasuryReceiptRepository.AddAsync(
                    BuildTreasuryReceipt(intent, config, receipt, operationId, platformAssetId, now),
                    ct);

                // The gate reads whole units, so the raise moves by the same exact reference the
                // disclosure showed - never by a figure re-derived from a rate that has since moved.
                config.fundingRaised = Money.ToDisplayDouble(
                    (decimal)config.fundingRaised + Money.FromMinorUnits(intent.usdReferenceMinorUnits));
                config.updatedAt = now;
                await _tokenConfigRepository.UpdateAsync(config, ct);
            }

            await _unitOfWork.SaveChangesAsync(ct);

            // Gate 1 is the gate service's, not a second copy of it. This sponsorship may be the one
            // that clears the goal, and it has to clear inside the same transaction that recorded
            // the money that cleared it.
            await _projectGate.EvaluateGate1Async(config.id, ct);
            await _unitOfWork.SaveChangesAsync(ct);
            await _unitOfWork.CommitTransactionAsync(ct);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(CancellationToken.None);
            throw;
        }

        return Result<FundingCheckoutDto>.Success(BuildCheckout(intent, project));
    }

    /// <summary>The node did not finish: keep the row and hand the work to the reconciliation loop.</summary>
    private async Task<Result<FundingCheckoutDto>> ParkForReconciliationAsync(
        FundingIntent intent,
        EconomicSettlement settlement,
        EconomicOutbox outbox,
        Project project,
        AzoaSettlementGatewayResult outcome,
        CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        settlement.status = EconomicSettlementStatus.AWAITING_RECONCILIATION;
        settlement.failureCode = outcome.Code;
        settlement.failureDetail = outcome.Detail;
        settlement.submittedAt ??= now;
        settlement.updatedAt = now;
        settlement.version += 1;
        await _settlementRepository.UpdateAsync(settlement, ct);

        outbox.status = EconomicOutboxStatus.AWAITING_RECONCILIATION;
        outbox.reconciliationRequiredAt = now;
        outbox.availableAt = now.Add(OutboxHandoverDelay);
        outbox.failureCode = outcome.Code;
        outbox.failureDetail = outcome.Detail;
        outbox.updatedAt = now;
        await _outboxRepository.UpdateAsync(outbox, ct);

        intent.status = FundingIntentStatus.SETTLEMENT_PENDING;
        intent.holdingReasonCode = outcome.Code;
        intent.holdingReasonDetail = outcome.Detail;
        intent.updatedAt = now;
        await _intentRepository.UpdateAsync(intent, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return Result<FundingCheckoutDto>.Success(BuildCheckout(intent, project));
    }

    /// <summary>A refusal the gateway calls safely retryable: leave it queued, bounded by the worker's ceiling.</summary>
    private async Task<Result<FundingCheckoutDto>> LeaveToOutboxAsync(
        FundingIntent intent,
        EconomicSettlement settlement,
        EconomicOutbox outbox,
        Project project,
        AzoaSettlementGatewayResult outcome,
        CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        settlement.failureCode = outcome.Code;
        settlement.failureDetail = outcome.Detail;
        settlement.updatedAt = now;
        await _settlementRepository.UpdateAsync(settlement, ct);

        outbox.availableAt = now.Add(OutboxHandoverDelay);
        outbox.failureCode = outcome.Code;
        outbox.failureDetail = outcome.Detail;
        outbox.updatedAt = now;
        await _outboxRepository.UpdateAsync(outbox, ct);

        intent.status = FundingIntentStatus.SETTLEMENT_PENDING;
        intent.holdingReasonCode = outcome.Code;
        intent.holdingReasonDetail = outcome.Detail;
        intent.updatedAt = now;
        await _intentRepository.UpdateAsync(intent, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return Result<FundingCheckoutDto>.Success(BuildCheckout(intent, project));
    }

    /// <summary>
    /// Readiness lapsed and no value moved: park the sponsorship and stop. The sponsor's balance was
    /// never debited, which is what returning their units means when the exchange refuses before it
    /// touches the node - and the settlement is cancelled here so nothing retries it forever.
    /// </summary>
    private async Task<Result<FundingCheckoutDto>> HoldAsync(
        FundingIntent intent,
        EconomicSettlement settlement,
        EconomicOutbox outbox,
        AzoaSettlementGatewayResult outcome,
        CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        settlement.status = EconomicSettlementStatus.CANCELLED;
        settlement.failureCode = outcome.Code;
        settlement.failureDetail = outcome.Detail;
        settlement.updatedAt = now;
        settlement.version += 1;
        await _settlementRepository.UpdateAsync(settlement, ct);

        outbox.status = EconomicOutboxStatus.CANCELLED;
        outbox.failureCode = outcome.Code;
        outbox.failureDetail = outcome.Detail;
        outbox.updatedAt = now;
        await _outboxRepository.UpdateAsync(outbox, ct);

        intent.status = FundingIntentStatus.HOLDING;
        intent.holdingReasonCode = outcome.Code;
        intent.holdingReasonDetail = HoldingMessage(outcome.Detail);
        intent.heldAt = now;
        intent.updatedAt = now;
        await _intentRepository.UpdateAsync(intent, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return Result<FundingCheckoutDto>.Conflict(intent.holdingReasonDetail!);
    }

    // -- Finishing work a previous request started ---------------------------

    /// <summary>Promotes, parks or fails a pending sponsorship from what is now durably known.</summary>
    /// <remarks>
    /// The branches are the ways a node call ends. A settlement the outbox worker accepted is
    /// SUBMITTED, not CONFIRMED - the worker only knows the gateway said yes - so both count as
    /// confirmed here. A readiness code left on a still-queued settlement is the lapse gotcha 29
    /// describes; it becomes HOLDING here instead of being retried until a ceiling.
    /// </remarks>
    private async Task AdvancePendingAsync(FundingIntent intent, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(intent.settlementId))
            return;

        var settlement = await _settlementRepository.GetByIdAsync(intent.settlementId, ct);
        if (settlement is null)
            return;

        var outbox = await _outboxRepository.FindOneAsync(item => item.settlementId == settlement.id, ct);
        if (outbox is null)
            return;

        if (IsNodeConfirmed(settlement))
        {
            await PromoteConfirmedAsync(intent, settlement, outbox, ct);
            return;
        }

        if (IsReadinessLapse(settlement.failureCode))
        {
            await HoldAsync(
                intent,
                settlement,
                outbox,
                AzoaSettlementGatewayResult.Retry(settlement.failureCode!, settlement.failureDetail),
                ct);
            return;
        }

        if (settlement.status is EconomicSettlementStatus.FAILED
            or EconomicSettlementStatus.REJECTED
            or EconomicSettlementStatus.CANCELLED)
        {
            await FailAsync(intent, settlement, ct);
            return;
        }

        if (settlement.status != EconomicSettlementStatus.AWAITING_RECONCILIATION
            || intent.settlementAttemptCount >= MaximumIntentAttempts)
        {
            return;
        }

        // A reconcile is a READ of an operation that already exists. It can never create a second
        // economic effect, which is why a poll may make it and a dispatch may not.
        intent.settlementAttemptCount += 1;
        var outcome = await _settlementGateway.ReconcileAsync(BuildSettlementRequest(intent), ct);
        if (outcome.Outcome != AzoaSettlementGatewayOutcome.Accepted)
        {
            intent.updatedAt = DateTime.UtcNow;
            await _intentRepository.UpdateAsync(intent, ct);
            await _unitOfWork.SaveChangesAsync(ct);
            return;
        }

        var config = await _tokenConfigRepository.GetByIdAsync(intent.projectTokenConfigId, ct);
        var project = await _projectRepository.GetByIdAsync(intent.projectId, ct);
        if (config is null || project is null)
            return;

        await CompleteAsync(intent, settlement, outbox, config, project, outcome, ct);
    }

    /// <summary>Completes an intent whose settlement someone else already confirmed at the node.</summary>
    private async Task PromoteConfirmedAsync(
        FundingIntent intent,
        EconomicSettlement settlement,
        EconomicOutbox outbox,
        CancellationToken ct)
    {
        var config = await _tokenConfigRepository.GetByIdAsync(intent.projectTokenConfigId, ct);
        var project = await _projectRepository.GetByIdAsync(intent.projectId, ct);
        if (config is null || project is null)
            return;

        await CompleteAsync(
            intent,
            settlement,
            outbox,
            config,
            project,
            AzoaSettlementGatewayResult.Accepted(
                settlement.azoaOperationId!,
                settlement.azoaReceipt,
                settlement.azoaReplayed ?? false),
            ct);
    }

    private async Task FailAsync(FundingIntent intent, EconomicSettlement settlement, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        intent.status = FundingIntentStatus.FAILED;
        intent.holdingReasonCode = settlement.failureCode;
        intent.holdingReasonDetail = settlement.failureDetail
            ?? "This sponsorship could not be completed, and nothing was taken.";
        intent.updatedAt = now;
        await _intentRepository.UpdateAsync(intent, ct);
        await _unitOfWork.SaveChangesAsync(ct);
    }

    /// <summary>Replays an idempotent request against the intent it already created.</summary>
    private async Task<Result<FundingCheckoutDto>> ReplayAsync(
        FundingIntent intent,
        CreateFundingIntentDto request,
        long requestedBaseUnits,
        string actorId,
        CancellationToken ct)
    {
        // The key covers the REQUEST, so the request is what has to match. The quote may differ on a
        // later call because the rate moved; that is not the caller sending different terms.
        if (!string.Equals(intent.funderUserId, actorId, StringComparison.Ordinal)
            || !string.Equals(intent.projectTokenConfigId, request.ProjectTokenConfigId, StringComparison.Ordinal)
            || intent.requestedPlatformBaseUnits != requestedBaseUnits
            || !string.Equals(intent.disclosureVersion, request.DisclosureVersion, StringComparison.Ordinal))
        {
            return Result<FundingCheckoutDto>.Conflict("That key was already used for a different sponsorship");
        }

        var project = await _projectRepository.GetByIdAsync(intent.projectId, ct);
        if (project is null)
            return Result<FundingCheckoutDto>.NotFound("Project not found");

        switch (intent.status)
        {
            case FundingIntentStatus.SETTLED:
                return Result<FundingCheckoutDto>.Success(BuildCheckout(intent, project));

            case FundingIntentStatus.SETTLEMENT_PENDING:
                await AdvancePendingAsync(intent, ct);
                return intent.status == FundingIntentStatus.HOLDING
                    ? Result<FundingCheckoutDto>.Conflict(intent.holdingReasonDetail!)
                    : Result<FundingCheckoutDto>.Success(BuildCheckout(intent, project));

            case FundingIntentStatus.DRAFT:
                return await ResumeDraftAsync(intent, project, ct);

            case FundingIntentStatus.HOLDING:
                return Result<FundingCheckoutDto>.Conflict(
                    intent.holdingReasonDetail ?? HoldingMessage(null));

            default:
                return Result<FundingCheckoutDto>.Conflict(
                    intent.holdingReasonDetail ?? "This sponsorship is closed and nothing was taken.");
        }
    }

    /// <summary>Finishes an intent that was committed but never reached the node.</summary>
    private async Task<Result<FundingCheckoutDto>> ResumeDraftAsync(
        FundingIntent intent,
        Project project,
        CancellationToken ct)
    {
        var settlement = string.IsNullOrWhiteSpace(intent.settlementId)
            ? null
            : await _settlementRepository.GetByIdAsync(intent.settlementId, ct);
        var outbox = settlement is null
            ? null
            : await _outboxRepository.FindOneAsync(item => item.settlementId == settlement.id, ct);
        var config = await _tokenConfigRepository.GetByIdAsync(intent.projectTokenConfigId, ct);
        if (settlement is null || outbox is null || config is null)
            return Result<FundingCheckoutDto>.Conflict("This sponsorship has no settlement to complete.");
        if (intent.settlementAttemptCount >= MaximumIntentAttempts)
            return Result<FundingCheckoutDto>.Conflict("This sponsorship is being completed; check back shortly.");

        return await DispatchAndApplyAsync(intent, settlement, outbox, config, project, ct);
    }

    // -- Rows the confirmed exchange produces --------------------------------

    private static EconomicSettlement BuildExchangeSettlement(FundingIntent intent)
        => new()
        {
            id = Guid.NewGuid().ToString("D"),
            kind = EconomicSettlementKind.EXCHANGE,
            status = EconomicSettlementStatus.PENDING_DISPATCH,
            idempotencyKey = intent.exchangeIdempotencyKey!,
            beneficiaryUserId = intent.funderUserId,
            authorizedByUserId = intent.funderUserId,
            projectId = intent.projectId,
            assetCode = intent.projectUnitAssetCode,
            amount = intent.projectUnitQuantity,
            scale = intent.projectUnitScale,
            termsSnapshot = intent.termsSnapshot,
            version = 1,
            authorizedAt = intent.createdAt,
            createdAt = intent.createdAt,
            updatedAt = intent.createdAt,
        };

    private static EconomicOutbox BuildOutbox(string settlementId, DateTime now)
        => new()
        {
            id = Guid.NewGuid().ToString("D"),
            settlementId = settlementId,
            status = EconomicOutboxStatus.PENDING,
            payloadVersion = 1,
            attemptCount = 0,
            availableAt = now.Add(OutboxHandoverDelay),
            createdAt = now,
            updatedAt = now,
        };

    private static ProjectInvestment BuildInvestment(FundingIntent intent, DateTime now)
        => new()
        {
            // The intent's own id: one sponsorship can only ever produce one stake, and the primary
            // key is what enforces that rather than a check the promotion path could forget.
            id = intent.id,
            projectTokenConfigId = intent.projectTokenConfigId,
            userId = intent.funderUserId,
            usdAmount = Money.ToDisplayDouble(Money.FromMinorUnits(intent.usdReferenceMinorUnits)),
            tokenAmount = checked((int)intent.projectUnitQuantity),
            investedAt = now,
            protectionEligible = true,
            protectionPaidOut = false,
        };

    /// <summary>A receipt for the credit the exchange already made to the project's node-side treasury.</summary>
    /// <remarks>
    /// It attests; it does not compute. No bucket split runs here and no balance is invented - the
    /// project's balance of record is its treasury account on the node, so
    /// <c>balanceAfterMinorUnits</c> stays null. <c>amountMinorUnits</c> is the currency reference
    /// the disclosure showed, in exact cents, which is also the figure the funding gate counts; the
    /// platform units actually credited are named in the description and counted on the settlement.
    /// </remarks>
    private static PlatformTreasuryTransaction BuildTreasuryReceipt(
        FundingIntent intent,
        ProjectTokenConfig config,
        EconomicSettlementReceipt? receipt,
        string operationId,
        string? platformAssetId,
        DateTime now)
        => new()
        {
            id = Guid.NewGuid().ToString("D"),
            type = PlatformTreasuryTransactionType.FUNDING_INFLOW,
            amountMinorUnits = intent.usdReferenceMinorUnits,
            amount = Money.ToDisplayDouble(Money.FromMinorUnits(intent.usdReferenceMinorUnits)),
            balanceAfter = 0,
            balanceAfterMinorUnits = null,
            toBucket = "project-treasury",
            relatedProjectId = intent.projectId,
            description =
                $"Sponsorship credit of {WholeUnits(intent.platformAmountBaseUnits, intent.platformScale)} "
                + $"{intent.platformAssetCode} to project {intent.projectId} for "
                + $"{intent.projectUnitQuantity} {intent.projectUnitAssetCode}",
            azoaOperationId = operationId,
            azoaIdempotencyKey = intent.exchangeIdempotencyKey,
            azoaReceiptReference = receipt?.ReceiptReference,
            chainAssetId = platformAssetId,
            projectTreasuryAddress = config.treasuryAddress,
            createdAt = now,
        };

    /// <summary>The platform asset the treasury credit was counted in; null until genesis has run.</summary>
    private async Task<string?> ResolvePlatformAssetIdAsync(CancellationToken ct)
    {
        var treasuries = await _treasuryRepository.GetAllAsync(ct);
        return treasuries.FirstOrDefault()?.ardaAssetId;
    }

    private static AzoaSettlementRequest BuildSettlementRequest(FundingIntent intent)
    {
        if (!FixedScaleAmount.TryFromPositiveDecimal(
                intent.projectUnitQuantity,
                intent.projectUnitScale,
                out var amount))
        {
            throw new InvalidOperationException(
                "A durable sponsorship carries a project-unit quantity the node cannot express.");
        }

        return new AzoaSettlementRequest(
            intent.settlementId!,
            intent.exchangeIdempotencyKey!,
            intent.funderUserId,
            intent.projectUnitAssetCode,
            amount,
            intent.termsSnapshot);
    }

    // -- Snapshots, projections and small conversions ------------------------

    /// <summary>The exchange terms the node is asked to honour, written once and never edited.</summary>
    private static string CreateTermsSnapshot(
        FundingIntent intent,
        ProjectTokenConfig config,
        SponsorshipQuote quote,
        DeclaredProjectUnitRate rate,
        DateTime releaseNotBefore)
        => JsonSerializer.Serialize(new
        {
            schemaVersion = TermsSchemaVersion,
            settlementLeg = ProjectUnitLeg,
            chainType = config.assetChain,
            isBuy = true,
            projectReference = config.treasuryReference,
            projectAssetId = config.assetId,
            reservePartition = ProjectTokenService.InvestorPartitionName,
            platformAssetCode = intent.platformAssetCode,
            platformAmountBaseUnits = quote.PlatformBaseUnits.ToString(CultureInfo.InvariantCulture),
            projectUnitAssetCode = intent.projectUnitAssetCode,
            projectUnitAmountBaseUnits = quote.ProjectUnitBaseUnits,
            projectUnitQuantity = quote.Quantity,
            releaseNotBefore = new DateTimeOffset(releaseNotBefore, TimeSpan.Zero),
            purpose = ExchangePurpose,
            previewOnly = false,
            usdReferenceMinorUnits = quote.UsdReferenceMinorUnits.ToString(CultureInfo.InvariantCulture),
            disclosureVersion = intent.disclosureVersion,
            declaredRate = new
            {
                projectTokenConfigId = rate.ProjectTokenConfigId,
                ardaPerWholeProjectUnit = rate.ArdaPerWholeProjectUnit,
                usdMajorUnitsPerWholeProjectUnit = rate.UsdMajorUnitsPerWholeProjectUnit,
                projectUnitDecimals = rate.ProjectUnitDecimals,
                source = rate.Source,
                declaredAt = rate.DeclaredAt,
                version = rate.Version,
            },
        });

    private static string CreateEligibilitySnapshot(Project project, ProjectTokenConfig config)
        => JsonSerializer.Serialize(new
        {
            schemaVersion = TermsSchemaVersion,
            projectId = project.id,
            projectStatus = project.status.ToString(),
            commerceEnabled = project.commerceEnabled,
            projectTokenConfigId = config.id,
            gateStatus = config.gateStatus.ToString(),
            assetId = config.assetId,
            treasuryAddress = config.treasuryAddress,
        });

    /// <summary>Where the sponsor watches their sponsorship finish. No provider redirect exists any more.</summary>
    private static FundingCheckoutDto BuildCheckout(FundingIntent intent, Project project)
        => new()
        {
            IntentId = intent.id,
            CheckoutUrl = $"/projects/{project.slug}/invest/success?intent={intent.id}",
        };

    private static FundingIntentStatusDto ToStatus(FundingIntent intent)
        => new()
        {
            IntentId = intent.id,
            ProjectTokenConfigId = intent.projectTokenConfigId,
            // The DTO still carries the card rail's field names; the values are the exchange's - the
            // platform unit spent, and the moment the node confirmed it.
            CurrencyCode = intent.platformAssetCode,
            Amount = WholeUnits(intent.platformAmountBaseUnits, intent.platformScale),
            Status = intent.status.ToString(),
            PaymentVerifiedAt = intent.settledAt,
        };

    private static bool IsNodeConfirmed(EconomicSettlement settlement)
        => !string.IsNullOrWhiteSpace(settlement.azoaOperationId)
            && settlement.status is EconomicSettlementStatus.SUBMITTED or EconomicSettlementStatus.CONFIRMED;

    /// <summary>
    /// Whether a refusal means a person's account readiness lapsed between the click and the
    /// confirmation. Those are the ONLY refusals that park a sponsorship instead of failing it,
    /// because nothing was taken and the sponsor's balance is untouched.
    /// </summary>
    /// <remarks>
    /// Deliberately delegated to <see cref="EconomicSettlementReadinessCodes"/> rather than kept
    /// as a second copy of the same four strings: the dispatcher WRITES these codes onto the
    /// settlement row and this reads them back off it, so a rename in one place and not the other
    /// silently drops sponsorships out of HOLDING and fails them instead.
    /// </remarks>
    private static bool IsReadinessLapse(string? code)
        => EconomicSettlementReadinessCodes.IsReadinessLapse(code);

    private static string HoldingMessage(string? detail)
        => string.IsNullOrWhiteSpace(detail)
            ? "This sponsorship is on hold until account setup is complete. Nothing was taken."
            : $"{detail} Nothing was taken.";

    /// <summary>Propagates a typed failure across result payload types without inventing a status.</summary>
    private static Result<TOut> Propagate<TIn, TOut>(Result<TIn> source)
        => source.Type switch
        {
            ResultType.NotFound => Result<TOut>.NotFound(source.Error!),
            ResultType.ValidationError => Result<TOut>.ValidationError(source.Error!),
            ResultType.Conflict => Result<TOut>.Conflict(source.Error!),
            ResultType.Forbidden => Result<TOut>.Forbidden(source.Error!),
            ResultType.Unauthorized => Result<TOut>.Unauthorized(source.Error!),
            ResultType.BadRequest => Result<TOut>.BadRequest(source.Error!),
            _ => Result<TOut>.Failure(source.Error ?? "The request could not be completed"),
        };

    /// <summary>Parses a person-facing amount of the platform unit into exact base units.</summary>
    private static bool TryParsePlatformAmount(string? value, out long baseUnits)
    {
        baseUnits = 0;
        return TryParseExact(value, out var parsed)
            && parsed > 0
            && Money.TryToMinorUnits(parsed, out baseUnits)
            && baseUnits > 0;
    }

    private static bool TryParseExact(string? value, out decimal parsed)
        => decimal.TryParse(
            value,
            NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign,
            CultureInfo.InvariantCulture,
            out parsed);

    /// <summary>Minor units, rounding UP. The platform never under-charges for units it issued.</summary>
    private static bool TryToMinorUnitsCeiling(decimal wholeUnits, out long minorUnits)
    {
        minorUnits = 0;
        if (wholeUnits < 0 || wholeUnits > long.MaxValue / Money.MinorUnitsPerWholeUnit)
            return false;

        minorUnits = (long)decimal.Ceiling(wholeUnits * Money.MinorUnitsPerWholeUnit);
        return true;
    }

    /// <summary>Exact whole-unit text for a base-unit count. Display only; never an input.</summary>
    private static string WholeUnits(long baseUnits, int scale)
    {
        var divisor = 1m;
        for (var index = 0; index < scale; index++)
            divisor *= 10m;

        return (baseUnits / divisor).ToString(
            "F" + scale.ToString(CultureInfo.InvariantCulture),
            CultureInfo.InvariantCulture);
    }

    private static string Hash(string value)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
}
