namespace ArdaNova.Application.Tests.Services;

using System.Linq.Expressions;
using System.Text.Json;
using ArdaNova.Application.Common.Interfaces;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Implementations;
using ArdaNova.Application.Services.Interfaces;
using ArdaNova.Domain.Models.Entities;
using ArdaNova.Domain.Models.Enums;
using FluentAssertions;
using Moq;

/// <summary>
/// Who may read what on a project's money page. The rule under test is that membership branches
/// the PAYLOAD: a non-member is answered with aggregate figures and no entry at all, so no
/// per-sponsor amount, release date, award size or node operation id ever leaves the server.
/// </summary>
/// <remarks>
/// The fixture deliberately CARRIES everything the non-member must not see - two settled
/// sponsorships with node operation ids, a fee receipt, a worker award, a buy-back and a treasury
/// receipt - and <see cref="Member_ReceivesTheItemisedMovements"/> proves it does. A withholding
/// test that passed over an empty fixture would be a test of the fixture, not of the rule.
/// </remarks>
public sealed class ProjectLedgerVisibilityTests
{
    private const string ProjectId = "project-1";
    private const string ConfigId = "config-1";
    private const string MemberId = "member-1";
    private const string StrangerId = "stranger-1";
    private const string SponsorOneId = "sponsor-1";
    private const string SponsorTwoId = "sponsor-2";
    private const string PendingSponsorId = "sponsor-3";
    private const string WorkerId = "worker-1";
    private const string RevokedWorkerId = "worker-2";

    private const string SponsorOneName = "Ada Sponsor";
    private const string SponsorshipOneOperationId = "azoa-op-sponsorship-one";
    private const string SponsorshipTwoOperationId = "azoa-op-sponsorship-two";
    private const string ServiceFeeOperationId = "azoa-op-service-fee";
    private const string BuyBackOperationId = "azoa-op-buy-back";
    private const string BuyBackReceiptReference = "azoa-receipt-buy-back";
    private const string ProtectionOperationId = "azoa-op-trust-protection";

    /// <summary>The release date on the first sponsorship's stake, as it is printed to a member.</summary>
    private const string ReleaseYear = "2099";

    private static readonly DateTime SettledAt = new(2026, 3, 1, 12, 0, 0, DateTimeKind.Utc);

    private readonly ProjectLedgerService _sut;

    public ProjectLedgerVisibilityTests()
    {
        var hierarchyAuthorization = new Mock<IHierarchyAuthorizationService>();
        hierarchyAuthorization
            .Setup(authorization => authorization.IsProjectMemberAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string userId, string projectId, CancellationToken token) =>
                userId == MemberId && projectId == ProjectId);

        var treasuryReadModel = new Mock<IProjectBuyBackReadModel>();
        treasuryReadModel
            .Setup(model => model.GetProjectStatusAsync(ProjectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<TreasuryStatusDto>.Success(new TreasuryStatusDto
            {
                Id = "treasury-1",
                IsChainBacked = true,
                ChainBalanceMinorUnits = 123456,
                BalanceObservedAt = new DateTimeOffset(SettledAt),
            }));

        _sut = new ProjectLedgerService(
            RepositoryOf(Config()).Object,
            RepositoryOf(SettledSponsorships().Concat(new[] { PendingSponsorship() }).ToArray()).Object,
            RepositoryOf(Settlements()).Object,
            RepositoryOf(Awards()).Object,
            RepositoryOf(BuyBacks()).Object,
            RepositoryOf(TreasuryReceipts()).Object,
            RepositoryOf(Pledges()).Object,
            RepositoryOf(Users()).Object,
            hierarchyAuthorization.Object,
            treasuryReadModel.Object);
    }

    /// <summary>
    /// The item's own acceptance test: a stranger's response carries no node operation id and no
    /// per-entry amount. Both are asserted against the SERIALIZED payload, because "the page does
    /// not render it" is not the same control as "the server did not send it".
    /// </summary>
    [Fact]
    public async Task NonMember_ReceivesNoEntriesNoAmountsAndNoOperationIds()
    {
        var result = await _sut.GetProjectMoneyAsync(ProjectId, StrangerId);

        result.IsSuccess.Should().BeTrue();
        var view = result.Value!;

        view.EntriesVisible.Should().BeFalse();
        view.Entries.Should().BeEmpty();
        view.EntriesHiddenNote.Should().Be(ProjectMoneyDisclosures.EntriesHiddenFromNonMembers);
        view.CounterpartyNamesVisible.Should().BeFalse();

        var payload = JsonSerializer.Serialize(view);

        // No entry was built, so the fields that carry a person's figure are absent outright
        // rather than present-and-blank.
        payload.Should().NotContain(nameof(ProjectMoneyEntry.AmountBaseUnits));
        payload.Should().NotContain(nameof(ProjectMoneyEntry.AmountText));
        payload.Should().NotContain(nameof(ProjectMoneyEntry.UsdReferenceMinorUnits));
        payload.Should().NotContain(nameof(ProjectMoneyEntry.VerifyReference));
        payload.Should().NotContain(nameof(ProjectMoneyEntry.PendingReason));
        payload.Should().NotContain(nameof(ProjectMoneyEntry.Headline));
        payload.Should().NotContain(nameof(ProjectMoneyEntry.Description));

        foreach (var operationId in SurfacedNodeOperationIds())
            payload.Should().NotContain(operationId);

        // The release date and the sponsor's name are the two values a cap-table reader wanted.
        payload.Should().NotContain(ReleaseYear);
        payload.Should().NotContain(SponsorOneName);
    }

    /// <summary>
    /// The mirror that keeps the test above honest: everything withheld from the stranger is
    /// really in the fixture, and a member really receives it.
    /// </summary>
    [Fact]
    public async Task Member_ReceivesTheItemisedMovements()
    {
        var result = await _sut.GetProjectMoneyAsync(ProjectId, MemberId);

        result.IsSuccess.Should().BeTrue();
        var view = result.Value!;

        view.EntriesVisible.Should().BeTrue();
        view.EntriesHiddenNote.Should().BeNull();
        view.CounterpartyNamesVisible.Should().BeTrue();
        view.CounterpartyNamesNote.Should().BeNull();

        var sponsorship = view.Entries.Single(entry => entry.Id == "sponsorship:intent-1");
        sponsorship.AmountBaseUnits.Should().Be(25000);
        sponsorship.VerifyReference.Should().Be(SponsorshipOneOperationId);
        sponsorship.CounterpartyName.Should().Be(SponsorOneName);

        var award = view.Entries.Single(entry => entry.Id == "award:award-1");
        award.AmountBaseUnits.Should().Be(400);

        var payload = JsonSerializer.Serialize(view);
        foreach (var operationId in SurfacedNodeOperationIds())
            payload.Should().Contain(operationId);

        payload.Should().Contain(ReleaseYear);
    }

    /// <summary>
    /// Withholding the entries is not withholding the project. A stranger still gets the figures
    /// that let them decide whether to sponsor: the balance of record and aggregate support.
    /// </summary>
    [Fact]
    public async Task NonMember_StillReceivesTheBalanceAndTheAggregate()
    {
        var result = await _sut.GetProjectMoneyAsync(ProjectId, StrangerId);

        var view = result.Value!;

        view.Balance.IsAvailable.Should().BeTrue();
        view.Balance.FundsBaseUnits.Should().Be(123456);
        view.BalanceProvenance.Should().Be(ProjectMoneyDisclosures.RowsExplainTheBalance);
        view.ProjectUnitLabel.Should().Be("SEED");
        view.Summary.SponsorshipTotalUsdText.Should().Be("350.00");
    }

    /// <summary>
    /// The aggregate counts only money that moved and stake still held: a sponsorship the node
    /// never settled is not support, a sponsor's own stake is not a contributor, and stake that
    /// came back is not someone still working.
    /// </summary>
    [Theory]
    [InlineData(StrangerId)]
    [InlineData(MemberId)]
    public async Task Summary_CountsSettledSponsorshipsAndLiveContributorStakeOnly(string actorId)
    {
        var result = await _sut.GetProjectMoneyAsync(ProjectId, actorId);

        var summary = result.Value!.Summary;

        summary.SponsorCount.Should().Be(2);
        summary.ContributorCount.Should().Be(1);
        summary.SponsorshipTotalUsdMinorUnits.Should().Be(35000);
    }

    /// <summary>An unverified caller is refused outright; visibility is never decided for nobody.</summary>
    [Fact]
    public async Task AnonymousCaller_IsRefused()
    {
        var result = await _sut.GetProjectMoneyAsync(ProjectId, string.Empty);

        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.Unauthorized);
    }

    /// <summary>Every node operation id this project's rows put on a member's entries.</summary>
    private static IEnumerable<string> SurfacedNodeOperationIds() =>
    [
        SponsorshipOneOperationId,
        SponsorshipTwoOperationId,
        ServiceFeeOperationId,
        BuyBackOperationId,
        ProtectionOperationId,
    ];

    private static ProjectTokenConfig Config() => new()
    {
        id = ConfigId,
        projectId = ProjectId,
        assetName = "Seed Units",
        unitName = "SEED",
        status = ProjectTokenStatus.ACTIVE,
    };

    private static FundingIntent[] SettledSponsorships() =>
    [
        new()
        {
            id = "intent-1",
            projectId = ProjectId,
            projectTokenConfigId = ConfigId,
            status = FundingIntentStatus.SETTLED,
            funderUserId = SponsorOneId,
            platformAssetCode = "ARDA",
            platformAmountBaseUnits = 25000,
            platformScale = 2,
            projectUnitAssetCode = "SEED",
            projectUnitQuantity = 500,
            usdReferenceMinorUnits = 25000,
            azoaOperationId = SponsorshipOneOperationId,
            settlementId = "settlement-1",
            releaseNotBefore = new DateTime(2099, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            settledAt = SettledAt,
            updatedAt = SettledAt,
            createdAt = SettledAt,
        },
        new()
        {
            id = "intent-2",
            projectId = ProjectId,
            projectTokenConfigId = ConfigId,
            status = FundingIntentStatus.SETTLED,
            funderUserId = SponsorTwoId,
            platformAssetCode = "ARDA",
            platformAmountBaseUnits = 10000,
            platformScale = 2,
            projectUnitAssetCode = "SEED",
            projectUnitQuantity = 200,
            usdReferenceMinorUnits = 10000,
            azoaOperationId = SponsorshipTwoOperationId,
            settledAt = SettledAt,
            updatedAt = SettledAt,
            createdAt = SettledAt,
        },
    ];

    /// <summary>An intent the node never settled: it moved nothing, so it is not support.</summary>
    private static FundingIntent PendingSponsorship() => new()
    {
        id = "intent-3",
        projectId = ProjectId,
        projectTokenConfigId = ConfigId,
        status = FundingIntentStatus.SETTLEMENT_PENDING,
        funderUserId = PendingSponsorId,
        platformAssetCode = "ARDA",
        platformAmountBaseUnits = 99999,
        platformScale = 2,
        projectUnitAssetCode = "SEED",
        projectUnitQuantity = 900,
        usdReferenceMinorUnits = 99999,
        updatedAt = SettledAt,
        createdAt = SettledAt,
    };

    private static EconomicSettlement[] Settlements() =>
    [
        new()
        {
            id = "settlement-1",
            kind = EconomicSettlementKind.EXCHANGE,
            status = EconomicSettlementStatus.CONFIRMED,
            beneficiaryUserId = SponsorOneId,
            projectId = ProjectId,
            assetCode = "ARDA",
            amount = 250m,
            scale = 2,
            azoaOperationId = ServiceFeeOperationId,
            azoaFeeBaseUnits = "500",
            createdAt = SettledAt,
            updatedAt = SettledAt,
        },
    ];

    private static TokenAllocation[] Awards() =>
    [
        new()
        {
            id = "award-1",
            projectTokenConfigId = ConfigId,
            recipientUserId = WorkerId,
            holderClass = TokenHolderClass.CONTRIBUTOR,
            status = AllocationStatus.DISTRIBUTED,
            tokenAmount = 400,
            distributedAt = SettledAt,
            workerHoldId = "hold-worker-1",
            workerHoldReleasesAt = new DateTime(2099, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            updatedAt = SettledAt,
        },
        new()
        {
            id = "award-2",
            projectTokenConfigId = ConfigId,
            recipientUserId = SponsorOneId,
            holderClass = TokenHolderClass.INVESTOR,
            status = AllocationStatus.DISTRIBUTED,
            tokenAmount = 500,
            distributedAt = SettledAt,
            holdId = "hold-investor-1",
            updatedAt = SettledAt,
        },
        new()
        {
            id = "award-3",
            projectTokenConfigId = ConfigId,
            recipientUserId = RevokedWorkerId,
            holderClass = TokenHolderClass.CONTRIBUTOR,
            status = AllocationStatus.REVOKED,
            tokenAmount = 150,
            distributedAt = SettledAt,
            burnedAt = SettledAt,
            updatedAt = SettledAt,
        },
    ];

    private static PayoutRequest[] BuyBacks() =>
    [
        new()
        {
            id = "payout-1",
            userId = SponsorTwoId,
            sourceProjectTokenConfigId = ConfigId,
            sourceTokenAmount = 100,
            ardaMinorUnits = 9000,
            usdReferenceMinorUnits = 9000,
            status = PayoutStatus.COMPLETED,
            holderClass = TokenHolderClass.INVESTOR,
            conversionTxHash = BuyBackReceiptReference,
            payoutTxHash = BuyBackOperationId,
            requestedAt = SettledAt,
            completedAt = SettledAt,
        },
    ];

    private static PlatformTreasuryTransaction[] TreasuryReceipts() =>
    [
        new()
        {
            id = "receipt-1",
            type = PlatformTreasuryTransactionType.TRUST_PROTECTION,
            relatedProjectId = ProjectId,
            amountMinorUnits = 1500,
            azoaOperationId = ProtectionOperationId,
            description = "Supporter protection paid",
            createdAt = SettledAt,
        },
    ];

    /// <summary>The pledge row keyed by its intent id; its consent flag is what names a sponsor.</summary>
    private static ProjectInvestment[] Pledges() =>
    [
        new()
        {
            id = "intent-1",
            projectTokenConfigId = ConfigId,
            userId = SponsorOneId,
            usdAmount = 250d,
            tokenAmount = 500,
            showBackerName = true,
            investedAt = SettledAt,
        },
    ];

    private static User[] Users() =>
    [
        new() { id = SponsorOneId, name = SponsorOneName },
        new() { id = SponsorTwoId, name = "Bo Sponsor" },
        new() { id = WorkerId, name = "Cy Worker" },
    ];

    /// <summary>A repository over a fixed set of rows that applies the caller's real predicate.</summary>
    private static Mock<IRepository<T>> RepositoryOf<T>(params T[] rows)
        where T : class
    {
        var repository = new Mock<IRepository<T>>();

        repository
            .Setup(store => store.FindAsync(
                It.IsAny<Expression<Func<T, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Expression<Func<T, bool>> predicate, CancellationToken token) =>
                (IReadOnlyList<T>)rows.Where(predicate.Compile()).ToList());

        repository
            .Setup(store => store.FindOneAsync(
                It.IsAny<Expression<Func<T, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Expression<Func<T, bool>> predicate, CancellationToken token) =>
                rows.FirstOrDefault(predicate.Compile()));

        return repository;
    }
}
