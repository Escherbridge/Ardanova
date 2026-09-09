using System.Globalization;
using ArdaNova.Application.Common.Interfaces;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.Services.Interfaces;
using ArdaNova.Domain.Models.Entities;
using ArdaNova.Domain.Models.Enums;

namespace ArdaNova.Application.Services.Implementations;

/// <summary>Which way value moved, read from the project's side of the exchange.</summary>
public enum ProjectMoneyDirection
{
    /// <summary>The project received it.</summary>
    In,

    /// <summary>The project gave it up.</summary>
    Out,
}

/// <summary>What an entry is counted in.</summary>
public enum ProjectMoneyDenomination
{
    /// <summary>Spendable funds - the platform unit, or the currency reference a receipt was written in.</summary>
    Funds,

    /// <summary>A share of this project itself.</summary>
    Stake,
}

/// <summary>One thing that happened to a project's funds or stake, in words a sponsor reads.</summary>
/// <remarks>
/// Every entry is DERIVED from the row that already records the event; nothing here is a second
/// ledger.
/// </remarks>
/// <param name="AmountBaseUnits">The exact figure. <paramref name="AmountText"/> is its display projection.</param>
/// <param name="UnitLabel">What the amount is counted in, printed beside it so two denominations never blur.</param>
/// <param name="UsdReferenceMinorUnits">The D16 currency reference AT THE TIME, in exact cents. Never re-derived from today's rate.</param>
/// <param name="VerifyReference">The node operation this entry attests to. Shown only behind a "verify" affordance.</param>
public sealed record ProjectMoneyEntry(
    string Id,
    DateTimeOffset OccurredAt,
    ProjectMoneyDirection Direction,
    ProjectMoneyDenomination Denomination,
    string Headline,
    string Description,
    string AmountText,
    long AmountBaseUnits,
    int AmountScale,
    string UnitLabel,
    string? ExchangedForText,
    long? UsdReferenceMinorUnits,
    string? UsdReferenceText,
    bool IsSettled,
    string? PendingReason,
    string? CounterpartyName,
    string? VerifyReference);

/// <summary>What the project actually holds, as the node reported it. Never a sum of ArdaNova's rows.</summary>
/// <param name="FundsBaseUnits">Null when the node could not be read; a null is NOT a zero.</param>
public sealed record ProjectMoneyBalance(
    bool IsAvailable,
    long? FundsBaseUnits,
    string? FundsText,
    int FundsScale,
    string FundsUnitLabel,
    DateTimeOffset? ObservedAt,
    string? UnavailableReason);

/// <summary>What a project raised and who stands behind it, with nobody's own figure in it.</summary>
/// <remarks>
/// This is the WHOLE payload a non-member receives, so every field on it must be safe to publish
/// to any signed-in stranger. Headcounts and one project-level total; never a per-person amount.
/// </remarks>
/// <param name="SponsorshipTotalUsdMinorUnits">Exact cents. The currency reference is the one figure every sponsorship carries at the same scale.</param>
public sealed record ProjectMoneySummary(
    int SponsorCount,
    int ContributorCount,
    long SponsorshipTotalUsdMinorUnits,
    string SponsorshipTotalUsdText);

/// <summary>A project's money page: the balance of record, and the entries that explain it.</summary>
/// <param name="Summary">Aggregate support. Always present - it is all a non-member is sent.</param>
/// <param name="Entries">Itemised movements. EMPTY for a non-member, because none were built for them.</param>
/// <param name="EntriesHiddenNote">Why there are no entries, when there are none. Null when the reader may see them.</param>
/// <param name="CounterpartyNamesNote">Why names are missing, when they are. Null when the reader may see them.</param>
public sealed record ProjectMoneyView(
    string ProjectId,
    string? ProjectUnitLabel,
    ProjectMoneyBalance Balance,
    string BalanceProvenance,
    ProjectMoneySummary Summary,
    IReadOnlyList<ProjectMoneyEntry> Entries,
    bool EntriesVisible,
    string? EntriesHiddenNote,
    bool CounterpartyNamesVisible,
    string? CounterpartyNamesNote);

/// <summary>The sentences this view is allowed to say. Constants so the wording cannot drift per caller.</summary>
public static class ProjectMoneyDisclosures
{
    /// <summary>The whole discipline of this page, in one sentence.</summary>
    public const string RowsExplainTheBalance =
        "These entries explain the balance above. They are not added up to produce it: the "
        + "balance is read from this project's own funds account.";

    /// <summary>Stamped when the caller may not see who a counterparty was.</summary>
    public const string NamesHiddenFromNonMembers =
        "People's names are shown to project members only, and only when the person chose to be named.";

    /// <summary>Stamped when the reader is not on the project, so no itemised movement is sent at all.</summary>
    public const string EntriesHiddenFromNonMembers =
        "Individual movements are shown to project members only. Everyone else sees how much "
        + "support this project has, not who gave what.";

    /// <summary>Stamped when no project funding has been set up at all.</summary>
    public const string NoFundingSetUp =
        "This project has not set up funding yet, so it has no money to show.";
}

/// <summary>The per-project money view: the project's balance, and the entries that explain it.</summary>
/// <remarks>
/// A separate port rather than more members on an existing service interface, whose file belongs
/// to another wave item. The controller composes the implementation directly until a container
/// registration exists for it.
/// </remarks>
public interface IProjectLedgerService
{
    /// <summary>
    /// One project's money, as <paramref name="actorId"/> is allowed to read it. The caller
    /// supplies only the verified actor; this service decides membership itself, and membership
    /// decides the PAYLOAD: a non-member is answered with aggregate figures and no entries.
    /// </summary>
    Task<Result<ProjectMoneyView>> GetProjectMoneyAsync(
        string projectId,
        string actorId,
        CancellationToken ct = default);
}

/// <summary>Builds the money view by DERIVING entries from the rows that already record each event.</summary>
/// <remarks>
/// <para>
/// <b>The balance is not the entries.</b> <see cref="ReadBalanceAsync"/> takes no entries and is
/// called before any entry is read; the only source of a balance is the node treasury read-model.
/// A page that summed its own rows would be the database-as-money this platform rejected.
/// </para>
/// <para>
/// <b>Nothing is counted twice.</b> Each event has exactly one source of record here: a
/// sponsorship is its <see cref="FundingIntent"/>, a buy-back is its <see cref="PayoutRequest"/>,
/// a worker's award is its <see cref="TokenAllocation"/>. The treasury receipt rows those paths
/// also write are skipped by <see cref="IsCoveredByItsOwnSourceOfRecord"/> for exactly that reason.
/// </para>
/// <para>
/// <b>Membership branches the payload.</b> A reader who is not on the project is answered with
/// <see cref="ProjectMoneySummary"/> and an EMPTY entry list, and the itemised movements are
/// never built for them: no per-sponsor amount, release date, award size or node operation id
/// leaves this method for a renderer to withhold. Gating only the name field, as this service
/// once did, published the whole cap table to any signed-in stranger.
/// </para>
/// </remarks>
public class ProjectLedgerService : IProjectLedgerService
{
    /// <summary>Platform unit scale. ARDA carries two decimals, so a person thinks in whole currency.</summary>
    private const int PlatformFallbackScale = Money.MinorUnitDigits;

    /// <summary>The platform unit as a person sees it named across the site.</summary>
    private const string PlatformUnitLabel = "ARDA";

    /// <summary>What a counterparty is called when the reader may not see a name.</summary>
    private const string SponsorRole = "A sponsor";

    private const string WorkerRole = "A worker";

    private const string SupporterRole = "A supporter";

    /// <summary>The placeholder every entry sentence carries until a counterparty is resolved into it.</summary>
    private const string CounterpartyPlaceholder = "{counterparty}";

    private readonly IRepository<ProjectTokenConfig> _projectTokenConfigRepository;
    private readonly IRepository<FundingIntent> _fundingIntentRepository;
    private readonly IRepository<EconomicSettlement> _settlementRepository;
    private readonly IRepository<TokenAllocation> _allocationRepository;
    private readonly IRepository<PayoutRequest> _payoutRepository;
    private readonly IRepository<PlatformTreasuryTransaction> _treasuryReceiptRepository;
    private readonly IRepository<ProjectInvestment> _investmentRepository;
    private readonly IRepository<User> _userRepository;
    private readonly IHierarchyAuthorizationService _hierarchyAuthorization;
    private readonly IProjectBuyBackReadModel? _projectTreasuryReadModel;

    /// <param name="projectTreasuryReadModel">
    /// The node read of the project's own funds account. Optional in the constructor only so the
    /// type still constructs without a container; when it is absent the balance REFUSES rather
    /// than falling back to a figure derived from the entries.
    /// </param>
    public ProjectLedgerService(
        IRepository<ProjectTokenConfig> projectTokenConfigRepository,
        IRepository<FundingIntent> fundingIntentRepository,
        IRepository<EconomicSettlement> settlementRepository,
        IRepository<TokenAllocation> allocationRepository,
        IRepository<PayoutRequest> payoutRepository,
        IRepository<PlatformTreasuryTransaction> treasuryReceiptRepository,
        IRepository<ProjectInvestment> investmentRepository,
        IRepository<User> userRepository,
        IHierarchyAuthorizationService hierarchyAuthorization,
        IProjectBuyBackReadModel? projectTreasuryReadModel = null)
    {
        _projectTokenConfigRepository = projectTokenConfigRepository;
        _fundingIntentRepository = fundingIntentRepository;
        _settlementRepository = settlementRepository;
        _allocationRepository = allocationRepository;
        _payoutRepository = payoutRepository;
        _treasuryReceiptRepository = treasuryReceiptRepository;
        _investmentRepository = investmentRepository;
        _userRepository = userRepository;
        _hierarchyAuthorization = hierarchyAuthorization;
        _projectTreasuryReadModel = projectTreasuryReadModel;
    }

    /// <inheritdoc />
    public async Task<Result<ProjectMoneyView>> GetProjectMoneyAsync(
        string projectId,
        string actorId,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(projectId))
            return Result<ProjectMoneyView>.ValidationError("A project id is required.");

        if (string.IsNullOrWhiteSpace(actorId))
            return Result<ProjectMoneyView>.Unauthorized("A verified signed-in person is required.");

        var config = await _projectTokenConfigRepository.FindOneAsync(
            row => row.projectId == projectId, ct);
        if (config is null)
            return Result<ProjectMoneyView>.NotFound(ProjectMoneyDisclosures.NoFundingSetUp);

        // The balance is read FIRST and from one place only. Nothing below can contribute to it.
        var balance = await ReadBalanceAsync(projectId, ct);

        // Membership is decided HERE, against the verified actor the controller passed. A caller
        // never tells this service what it may see.
        var isMember = await _hierarchyAuthorization.IsProjectMemberAsync(actorId, projectId, ct);

        var sponsorships = await ReadSettledSponsorshipsAsync(projectId, ct);
        var awards = await ReadDistributedAwardsAsync(config, ct);
        var summary = BuildSummary(sponsorships, awards);

        var projectUnitLabel = string.IsNullOrWhiteSpace(config.unitName) ? null : config.unitName;

        // Membership branches the PAYLOAD, not a name field. A non-member is answered with the
        // aggregate and no entry is built for them at all, so no per-sponsor amount, release date,
        // award size or node operation id exists in the returned object for a renderer to hide.
        // This is the branch the sibling backer list takes (ProjectTokensController.GetInvestors).
        if (!isMember)
        {
            return Result<ProjectMoneyView>.Success(new ProjectMoneyView(
                ProjectId: projectId,
                ProjectUnitLabel: projectUnitLabel,
                Balance: balance,
                BalanceProvenance: ProjectMoneyDisclosures.RowsExplainTheBalance,
                Summary: summary,
                Entries: Array.Empty<ProjectMoneyEntry>(),
                EntriesVisible: false,
                EntriesHiddenNote: ProjectMoneyDisclosures.EntriesHiddenFromNonMembers,
                CounterpartyNamesVisible: false,
                CounterpartyNamesNote: ProjectMoneyDisclosures.NamesHiddenFromNonMembers));
        }

        var entries = new List<ProjectMoneyEntry>();
        var counterparties = new CounterpartyNames(mayShowNames: true);

        await AddSponsorshipsAsync(entries, counterparties, sponsorships, ct);
        AddWorkerAwards(entries, counterparties, config, awards);
        await AddBuyBacksAsync(entries, counterparties, config, ct);
        await AddRemainingTreasuryReceiptsAsync(entries, projectId, ct);

        await counterparties.ResolveAsync(_userRepository, ct);

        var ordered = entries
            .Select(counterparties.Apply)
            .OrderByDescending(entry => entry.OccurredAt)
            .ThenBy(entry => entry.Id, StringComparer.Ordinal)
            .ToList();

        return Result<ProjectMoneyView>.Success(new ProjectMoneyView(
            ProjectId: projectId,
            ProjectUnitLabel: projectUnitLabel,
            Balance: balance,
            BalanceProvenance: ProjectMoneyDisclosures.RowsExplainTheBalance,
            Summary: summary,
            Entries: ordered,
            EntriesVisible: true,
            EntriesHiddenNote: null,
            CounterpartyNamesVisible: true,
            CounterpartyNamesNote: null));
    }

    /// <summary>Settled sponsorships for this project. Only a SETTLED intent moved any money.</summary>
    private Task<IReadOnlyList<FundingIntent>> ReadSettledSponsorshipsAsync(
        string projectId,
        CancellationToken ct) =>
        _fundingIntentRepository.FindAsync(
            intent => intent.projectId == projectId && intent.status == FundingIntentStatus.SETTLED,
            ct);

    /// <summary>Stake the project committed to a named person, still held or since returned.</summary>
    private Task<IReadOnlyList<TokenAllocation>> ReadDistributedAwardsAsync(
        ProjectTokenConfig config,
        CancellationToken ct) =>
        _allocationRepository.FindAsync(
            allocation => allocation.projectTokenConfigId == config.id
                && allocation.recipientUserId != null
                && allocation.distributedAt != null,
            ct);

    /// <summary>Two headcounts and one total: proof of support that names nobody and prices nobody.</summary>
    private static ProjectMoneySummary BuildSummary(
        IReadOnlyList<FundingIntent> sponsorships,
        IReadOnlyList<TokenAllocation> awards)
    {
        var sponsorCount = sponsorships
            .Select(intent => intent.funderUserId)
            .Where(userId => !string.IsNullOrWhiteSpace(userId))
            .Distinct(StringComparer.Ordinal)
            .Count();

        // The currency reference is the one figure every sponsorship carries at the same scale, so
        // a total of it is exact. Platform base units may differ in scale row to row and are NOT
        // summed here: a total that blurred two scales would be a wrong number, not a coarse one.
        var totalUsdMinorUnits = sponsorships.Sum(intent => intent.usdReferenceMinorUnits);

        var contributorCount = awards
            .Where(award => award.holderClass != TokenHolderClass.INVESTOR
                && award.status is not (AllocationStatus.REVOKED or AllocationStatus.BURNED))
            .Select(award => award.recipientUserId)
            .Where(userId => !string.IsNullOrWhiteSpace(userId))
            .Distinct(StringComparer.Ordinal)
            .Count();

        return new ProjectMoneySummary(
            SponsorCount: sponsorCount,
            ContributorCount: contributorCount,
            SponsorshipTotalUsdMinorUnits: totalUsdMinorUnits,
            SponsorshipTotalUsdText: FormatAmount(totalUsdMinorUnits, Money.MinorUnitDigits));
    }

    /// <summary>
    /// The project's balance of record, from the node. Takes no entries and returns no figure it
    /// derived from one - that is what makes "the rows do not constitute the balance" a control.
    /// </summary>
    private async Task<ProjectMoneyBalance> ReadBalanceAsync(string projectId, CancellationToken ct)
    {
        if (_projectTreasuryReadModel is null)
            return UnreadableBalance(TreasuryPolicyView.NoChainReadConfigured);

        var status = await _projectTreasuryReadModel.GetProjectStatusAsync(projectId, ct);
        if (!status.IsSuccess || status.Value is null)
            return UnreadableBalance(status.Error ?? TreasuryPolicyView.NoChainReadConfigured);

        var read = status.Value;
        if (!read.IsChainBacked || read.ChainBalanceMinorUnits is not { } observed)
        {
            return UnreadableBalance(
                read.BalanceUnavailableReason ?? TreasuryPolicyView.NoChainReadConfigured);
        }

        return new ProjectMoneyBalance(
            IsAvailable: true,
            FundsBaseUnits: observed,
            FundsText: FormatAmount(observed, PlatformFallbackScale),
            FundsScale: PlatformFallbackScale,
            FundsUnitLabel: PlatformUnitLabel,
            ObservedAt: read.BalanceObservedAt,
            UnavailableReason: null);
    }

    /// <summary>A balance that could not be read, with the reason in the open. Never a zero standing in for a read.</summary>
    private static ProjectMoneyBalance UnreadableBalance(string reason) =>
        new(
            IsAvailable: false,
            FundsBaseUnits: null,
            FundsText: null,
            FundsScale: PlatformFallbackScale,
            FundsUnitLabel: PlatformUnitLabel,
            ObservedAt: null,
            UnavailableReason: reason);

    /// <summary>Settled sponsorships, the stake each bought, and the fee taken before the funds landed.</summary>
    /// <remarks>
    /// Only SETTLED intents are money. A HOLDING, REJECTED or CANCELLED intent left the sponsor's
    /// funds where they were, so it never touched this project and must not appear as if it had.
    /// </remarks>
    private async Task AddSponsorshipsAsync(
        List<ProjectMoneyEntry> entries,
        CounterpartyNames counterparties,
        IReadOnlyList<FundingIntent> settled,
        CancellationToken ct)
    {
        if (settled.Count == 0)
            return;

        // A sponsor's name appears only when their own per-pledge consent opted in. The pledge
        // carries the intent's id as its key, so the consent flag is a lookup rather than a guess.
        var intentIds = settled.Select(intent => intent.id).ToList();
        var pledges = await _investmentRepository.FindAsync(
            pledge => intentIds.Contains(pledge.id), ct);
        var consentedIntentIds = pledges
            .Where(pledge => pledge.showBackerName)
            .Select(pledge => pledge.id)
            .ToHashSet(StringComparer.Ordinal);

        var settlementIds = settled
            .Where(intent => !string.IsNullOrWhiteSpace(intent.settlementId))
            .Select(intent => intent.settlementId!)
            .Distinct()
            .ToList();
        var settlementById = settlementIds.Count == 0
            ? new Dictionary<string, EconomicSettlement>(StringComparer.Ordinal)
            : (await _settlementRepository.FindAsync(row => settlementIds.Contains(row.id), ct))
                .ToDictionary(row => row.id, StringComparer.Ordinal);

        foreach (var intent in settled)
        {
            var occurredAt = AsUtc(intent.settledAt ?? intent.updatedAt);
            var unitsText = $"{intent.projectUnitQuantity} {intent.projectUnitAssetCode}";
            var consented = consentedIntentIds.Contains(intent.id);

            var fundsEntryId = $"sponsorship:{intent.id}";
            entries.Add(new ProjectMoneyEntry(
                Id: fundsEntryId,
                OccurredAt: occurredAt,
                Direction: ProjectMoneyDirection.In,
                Denomination: ProjectMoneyDenomination.Funds,
                Headline: "Sponsorship received",
                Description: $"{CounterpartyPlaceholder} put funds into this project and received {unitsText}.",
                AmountText: FormatAmount(intent.platformAmountBaseUnits, intent.platformScale),
                AmountBaseUnits: intent.platformAmountBaseUnits,
                AmountScale: intent.platformScale,
                UnitLabel: intent.platformAssetCode,
                ExchangedForText: unitsText,
                UsdReferenceMinorUnits: intent.usdReferenceMinorUnits,
                UsdReferenceText: FormatAmount(intent.usdReferenceMinorUnits, Money.MinorUnitDigits),
                IsSettled: true,
                PendingReason: null,
                CounterpartyName: null,
                VerifyReference: intent.azoaOperationId));
            counterparties.Claim(fundsEntryId, intent.funderUserId, SponsorRole, consented);

            // The same exchange from the stake side: a share of the project left it.
            var stakeEntryId = $"sponsorship-stake:{intent.id}";
            entries.Add(new ProjectMoneyEntry(
                Id: stakeEntryId,
                OccurredAt: occurredAt,
                Direction: ProjectMoneyDirection.Out,
                Denomination: ProjectMoneyDenomination.Stake,
                Headline: "Stake to a sponsor",
                Description: $"{CounterpartyPlaceholder} received a share of this project for their sponsorship.",
                AmountText: intent.projectUnitQuantity.ToString(CultureInfo.InvariantCulture),
                AmountBaseUnits: intent.projectUnitQuantity,
                AmountScale: 0,
                UnitLabel: intent.projectUnitAssetCode,
                ExchangedForText: null,
                UsdReferenceMinorUnits: null,
                UsdReferenceText: null,
                IsSettled: true,
                PendingReason: intent.releaseNotBefore is { } releaseAt && releaseAt > DateTime.UtcNow
                    ? $"Theirs to move from {FormatDate(releaseAt)}."
                    : null,
                CounterpartyName: null,
                VerifyReference: intent.azoaOperationId));
            counterparties.Claim(stakeEntryId, intent.funderUserId, SponsorRole, consented);

            if (!settlementById.TryGetValue(intent.settlementId ?? string.Empty, out var settlement))
                continue;

            if (!TryReadBaseUnits(settlement.azoaFeeBaseUnits, out var feeBaseUnits) || feeBaseUnits <= 0)
                continue;

            entries.Add(new ProjectMoneyEntry(
                Id: $"sponsorship-fee:{intent.id}",
                OccurredAt: occurredAt,
                Direction: ProjectMoneyDirection.Out,
                Denomination: ProjectMoneyDenomination.Funds,
                Headline: "Service fee",
                Description: "A service fee was taken from this sponsorship before it reached the project.",
                AmountText: FormatAmount(feeBaseUnits, intent.platformScale),
                AmountBaseUnits: feeBaseUnits,
                AmountScale: intent.platformScale,
                UnitLabel: intent.platformAssetCode,
                ExchangedForText: null,
                UsdReferenceMinorUnits: null,
                UsdReferenceText: null,
                IsSettled: true,
                PendingReason: null,
                CounterpartyName: null,
                VerifyReference: settlement.azoaOperationId ?? intent.azoaOperationId));
        }
    }

    /// <summary>Stake committed to the people doing the work, and any of it that came back.</summary>
    private static void AddWorkerAwards(
        List<ProjectMoneyEntry> entries,
        CounterpartyNames counterparties,
        ProjectTokenConfig config,
        IReadOnlyList<TokenAllocation> awards)
    {
        var unitLabel = StakeUnitLabel(config);

        foreach (var award in awards)
        {
            var returned = award.status is AllocationStatus.REVOKED or AllocationStatus.BURNED;
            var occurredAt = AsUtc(returned
                ? award.burnedAt ?? award.updatedAt
                : award.distributedAt!.Value);
            var role = award.holderClass == TokenHolderClass.INVESTOR ? SponsorRole : WorkerRole;
            var entryId = $"award:{award.id}";

            entries.Add(new ProjectMoneyEntry(
                Id: entryId,
                OccurredAt: occurredAt,
                Direction: returned ? ProjectMoneyDirection.In : ProjectMoneyDirection.Out,
                Denomination: ProjectMoneyDenomination.Stake,
                Headline: returned ? "Stake returned to the project" : "Stake for work done",
                Description: returned
                    ? $"A share committed to {CounterpartyPlaceholder} came back to the project."
                    : $"{CounterpartyPlaceholder} received a share of this project for their work.",
                AmountText: award.tokenAmount.ToString(CultureInfo.InvariantCulture),
                AmountBaseUnits: award.tokenAmount,
                AmountScale: 0,
                UnitLabel: unitLabel,
                ExchangedForText: null,
                UsdReferenceMinorUnits: null,
                UsdReferenceText: null,
                // Only what the node reported counts as settled: a distribution date with no hold
                // reference is ArdaNova's own row, not proof the node committed anything.
                IsSettled: returned || award.IsNodeHeld,
                PendingReason: PendingAwardReason(award, returned),
                CounterpartyName: null,
                VerifyReference: award.workerHoldId ?? award.holdId));

            counterparties.Claim(entryId, award.recipientUserId, role, consented: true);
        }
    }

    /// <summary>Why an award is not yet the recipient's to move, in the words they were shown.</summary>
    private static string? PendingAwardReason(TokenAllocation award, bool returned)
    {
        if (returned)
            return null;

        if (!award.IsNodeHeld)
            return "Recorded here; the funds account has not confirmed it yet.";

        return award.workerHoldReleasesAt is { } releasesAt && releasesAt > DateTime.UtcNow
            ? $"Theirs to move from {FormatDate(releasesAt)}."
            : null;
    }

    /// <summary>Units sold back to the project, and buy-backs that reversed.</summary>
    /// <remarks>
    /// Only a request the node answered with a figure is money: <c>ardaMinorUnits</c> is what the
    /// node said it paid. A request still in flight has moved nothing and is not shown as if it had.
    /// </remarks>
    private async Task AddBuyBacksAsync(
        List<ProjectMoneyEntry> entries,
        CounterpartyNames counterparties,
        ProjectTokenConfig config,
        CancellationToken ct)
    {
        var requests = await _payoutRepository.FindAsync(
            request => request.sourceProjectTokenConfigId == config.id
                && request.ardaMinorUnits != null,
            ct);

        var unitLabel = StakeUnitLabel(config);

        foreach (var request in requests)
        {
            var reversed = request.status is PayoutStatus.FAILED or PayoutStatus.CANCELLED;
            var paid = request.ardaMinorUnits!.Value;
            var occurredAt = AsUtc(request.completedAt ?? request.processedAt ?? request.requestedAt);
            var unitsText = $"{request.sourceTokenAmount} {unitLabel}";
            var entryId = $"buy-back:{request.id}";

            entries.Add(new ProjectMoneyEntry(
                Id: entryId,
                OccurredAt: occurredAt,
                Direction: reversed ? ProjectMoneyDirection.In : ProjectMoneyDirection.Out,
                Denomination: ProjectMoneyDenomination.Funds,
                Headline: reversed ? "Buy-back reversed" : "Stake bought back",
                Description: reversed
                    ? $"A buy-back for {CounterpartyPlaceholder} could not be completed, and the funds stayed with the project."
                    : $"{CounterpartyPlaceholder} sold {unitsText} back to the project.",
                AmountText: FormatAmount(paid, PlatformFallbackScale),
                AmountBaseUnits: paid,
                AmountScale: PlatformFallbackScale,
                UnitLabel: PlatformUnitLabel,
                ExchangedForText: reversed ? null : unitsText,
                UsdReferenceMinorUnits: request.usdReferenceMinorUnits,
                UsdReferenceText: request.usdReferenceMinorUnits is { } reference
                    ? FormatAmount(reference, Money.MinorUnitDigits)
                    : null,
                IsSettled: request.IsNodeSettled,
                PendingReason: request.IsNodeSettled
                    ? null
                    : "Recorded here; the funds account has not confirmed it yet.",
                CounterpartyName: null,
                VerifyReference: request.SellOperationId ?? request.SellReceiptReference));

            counterparties.Claim(entryId, request.userId, SupporterRole, consented: false);
        }
    }

    /// <summary>Node-confirmed treasury receipts whose event has no other source of record on this page.</summary>
    private async Task AddRemainingTreasuryReceiptsAsync(
        List<ProjectMoneyEntry> entries,
        string projectId,
        CancellationToken ct)
    {
        var receipts = await _treasuryReceiptRepository.FindAsync(
            receipt => receipt.relatedProjectId == projectId && receipt.azoaOperationId != null,
            ct);

        foreach (var receipt in receipts)
        {
            if (IsCoveredByItsOwnSourceOfRecord(receipt.type))
                continue;

            var headline = HeadlineFor(receipt.type);

            entries.Add(new ProjectMoneyEntry(
                Id: $"receipt:{receipt.id}",
                OccurredAt: AsUtc(receipt.createdAt),
                Direction: ProjectMoneyDirection.Out,
                Denomination: ProjectMoneyDenomination.Funds,
                Headline: headline,
                Description: string.IsNullOrWhiteSpace(receipt.description) ? headline : receipt.description!,
                // Receipt amounts are written in the currency reference, not the platform unit, so
                // the label says so rather than letting two denominations blur together.
                AmountText: FormatAmount(receipt.amountMinorUnits, Money.MinorUnitDigits),
                AmountBaseUnits: receipt.amountMinorUnits,
                AmountScale: Money.MinorUnitDigits,
                UnitLabel: "USD",
                ExchangedForText: null,
                UsdReferenceMinorUnits: receipt.amountMinorUnits,
                UsdReferenceText: FormatAmount(receipt.amountMinorUnits, Money.MinorUnitDigits),
                IsSettled: receipt.IsChainReceipt,
                PendingReason: null,
                CounterpartyName: null,
                VerifyReference: receipt.azoaOperationId));
        }
    }

    /// <summary>True for receipt kinds whose event is already on this page from its own source of record.</summary>
    /// <remarks>
    /// A sponsorship is its <see cref="FundingIntent"/> and a buy-back is its
    /// <see cref="PayoutRequest"/>; the three ALLOCATION_ kinds are ArdaNova's policy split of one
    /// inflow, so showing them would state the same money three more times.
    /// </remarks>
    private static bool IsCoveredByItsOwnSourceOfRecord(PlatformTreasuryTransactionType type) =>
        type is PlatformTreasuryTransactionType.FUNDING_INFLOW
            or PlatformTreasuryTransactionType.ALLOCATION_INDEX
            or PlatformTreasuryTransactionType.ALLOCATION_LIQUID
            or PlatformTreasuryTransactionType.ALLOCATION_OPS
            or PlatformTreasuryTransactionType.PAYOUT_DEBIT;

    private static string HeadlineFor(PlatformTreasuryTransactionType type) => type switch
    {
        PlatformTreasuryTransactionType.TRUST_PROTECTION => "Supporter protection paid",
        PlatformTreasuryTransactionType.FOUNDER_BURN => "Founder stake written off",
        PlatformTreasuryTransactionType.INDEX_RETURN => "Return on held funds",
        PlatformTreasuryTransactionType.PROFIT_SHARE => "Platform share of returns",
        PlatformTreasuryTransactionType.REBALANCE => "Funds moved between reserves",
        _ => "Recorded movement",
    };

    /// <summary>What a share of this project is called; never the word "token".</summary>
    private static string StakeUnitLabel(ProjectTokenConfig config) =>
        string.IsNullOrWhiteSpace(config.unitName) ? "units" : config.unitName;

    /// <summary>Exact display of a base-unit figure at its own scale. Invariant culture; never a float.</summary>
    private static string FormatAmount(long baseUnits, int scale)
    {
        var safeScale = scale < 0 ? 0 : scale > 18 ? 18 : scale;
        var value = (decimal)baseUnits / PowerOfTen(safeScale);
        return value.ToString(
            "F" + safeScale.ToString(CultureInfo.InvariantCulture),
            CultureInfo.InvariantCulture);
    }

    private static decimal PowerOfTen(int exponent)
    {
        var result = 1m;
        for (var step = 0; step < exponent; step++)
            result *= 10m;
        return result;
    }

    /// <summary>A date a person reads, not a timestamp.</summary>
    private static string FormatDate(DateTime value) =>
        value.ToString("d MMMM yyyy", CultureInfo.InvariantCulture);

    /// <summary>Stored money times are UTC; say so rather than letting the server's zone decide.</summary>
    private static DateTimeOffset AsUtc(DateTime value) =>
        new(DateTime.SpecifyKind(value, DateTimeKind.Utc));

    /// <summary>The node's base-unit strings are exact integers; anything else is not a figure.</summary>
    private static bool TryReadBaseUnits(string? text, out long baseUnits) =>
        long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out baseUnits);

    /// <summary>
    /// Collects who each entry was with, then resolves display names ONCE - and only for a reader
    /// allowed to see them.
    /// </summary>
    /// <remarks>
    /// The role ("A sponsor") is what a non-member sees, and it is also what a member sees when the
    /// person did not consent to being named. No internal user id ever leaves this class.
    /// </remarks>
    private sealed class CounterpartyNames
    {
        private readonly bool _mayShowNames;
        private readonly Dictionary<string, ClaimedCounterparty> _byEntryId = new(StringComparer.Ordinal);
        private Dictionary<string, string?> _displayNameByUserId = new(StringComparer.Ordinal);

        public CounterpartyNames(bool mayShowNames) => _mayShowNames = mayShowNames;

        /// <summary>Records who an entry was with, and whether they agreed to be named.</summary>
        public void Claim(string entryId, string? userId, string role, bool consented) =>
            _byEntryId[entryId] = new ClaimedCounterparty(userId, role, consented);

        /// <summary>Loads display names in one query, and only when the reader may see any.</summary>
        public async Task ResolveAsync(IRepository<User> userRepository, CancellationToken ct)
        {
            if (!_mayShowNames)
                return;

            var userIds = _byEntryId.Values
                .Where(claim => claim.Consented && !string.IsNullOrWhiteSpace(claim.UserId))
                .Select(claim => claim.UserId!)
                .Distinct()
                .ToList();
            if (userIds.Count == 0)
                return;

            var users = await userRepository.FindAsync(user => userIds.Contains(user.id), ct);
            _displayNameByUserId = users.ToDictionary(
                user => user.id, user => (string?)user.name, StringComparer.Ordinal);
        }

        /// <summary>Substitutes the counterparty into the entry's sentence and its name field.</summary>
        public ProjectMoneyEntry Apply(ProjectMoneyEntry entry)
        {
            var name = ResolveName(entry.Id, out var role);

            return entry with
            {
                CounterpartyName = name,
                Description = entry.Description.Replace(
                    CounterpartyPlaceholder, name ?? role, StringComparison.Ordinal),
            };
        }

        private string? ResolveName(string entryId, out string role)
        {
            role = SupporterRole;
            if (!_byEntryId.TryGetValue(entryId, out var claim))
                return null;

            role = claim.Role;
            if (!_mayShowNames || !claim.Consented || string.IsNullOrWhiteSpace(claim.UserId))
                return null;

            return _displayNameByUserId.TryGetValue(claim.UserId!, out var name)
                && !string.IsNullOrWhiteSpace(name)
                    ? name
                    : null;
        }

        private sealed record ClaimedCounterparty(string? UserId, string Role, bool Consented);
    }
}
