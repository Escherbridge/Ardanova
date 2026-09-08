namespace ArdaNova.Application.Tests.Services;

using System.Linq.Expressions;
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
/// Sponsorship is an exchange, so these tests are about money: what was locked, what the node was
/// asked for exactly once, and what a refusal leaves behind.
/// </summary>
public class FundingIntentServiceTests
{
    private const string ActorId = "user-1";
    private const string AvatarId = "6f27b1a4-6d34-4f6b-9d6c-a3d2f9c9c111";
    private const string IdempotencyKey = "9e68f25a-589c-472a-ab2d-0b4161c5ab89";
    private const string Disclosure = "funding-disclosure-v1";

    private readonly Mock<IRepository<User>> _users = new();
    private readonly Mock<IRepository<Project>> _projects = new();
    private readonly Mock<IRepository<ProjectTokenConfig>> _configs = new();
    private readonly Mock<IRepository<FundingIntent>> _intents = new();
    private readonly Mock<IRepository<EconomicSettlement>> _settlements = new();
    private readonly Mock<IRepository<EconomicOutbox>> _outboxes = new();
    private readonly Mock<IRepository<ProjectInvestment>> _investments = new();
    private readonly Mock<IRepository<PlatformTreasuryTransaction>> _treasuryReceipts = new();
    private readonly Mock<IRepository<PlatformTreasury>> _treasuries = new();
    private readonly Mock<IAzoaSettlementGateway> _gateway = new();
    private readonly Mock<IProjectUnitRateSource> _rateSource = new();
    private readonly Mock<IProjectGateService> _projectGate = new();
    private readonly Mock<IFundingSettlementReadiness> _settlementReadiness = new();
    private readonly Mock<IAzoaValueEligibilityService> _valueEligibility = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly FundingIntentService _sut;

    public FundingIntentServiceTests()
    {
        _settlementReadiness.SetupGet(readiness => readiness.IsReady).Returns(true);
        _settlementReadiness.SetupGet(readiness => readiness.UnavailableReason).Returns(string.Empty);
        _valueEligibility
            .Setup(service => service.RequireReadyAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<AzoaCustodialAccountStatusDto>.Success(ReadyAccountStatus()));
        _treasuries
            .Setup(repository => repository.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { new PlatformTreasury { id = "treasury-1", ardaAssetId = "arda-asset" } });
        _projectGate
            .Setup(service => service.EvaluateGate1Async(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<GateTransitionResultDto>.Success(new GateTransitionResultDto()));

        _sut = new FundingIntentService(
            _users.Object,
            _projects.Object,
            _configs.Object,
            _intents.Object,
            _settlements.Object,
            _outboxes.Object,
            _investments.Object,
            _treasuryReceipts.Object,
            _treasuries.Object,
            _gateway.Object,
            _rateSource.Object,
            _projectGate.Object,
            _settlementReadiness.Object,
            _valueEligibility.Object,
            _unitOfWork.Object);
    }

    [Fact]
    public async Task CreateCheckoutAsync_RefusesBeforeAnyRowWhenThisDeploymentCannotSettle()
    {
        _settlementReadiness.SetupGet(readiness => readiness.IsReady).Returns(false);
        _settlementReadiness.SetupGet(readiness => readiness.UnavailableReason)
            .Returns("Sponsorship is not switched on for this deployment. (no settlement node is selected)");

        var result = await _sut.CreateCheckoutAsync(Request("12.00"), ActorId, IdempotencyKey);

        result.Type.Should().Be(ResultType.Conflict);
        result.Error.Should().Contain("no settlement node is selected");
        _users.Verify(repository => repository.GetByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        VerifyNothingWasSettled();
    }

    [Fact]
    public async Task CreateCheckoutAsync_RefusesAProjectThatHasNoMintedAssetToSell()
    {
        var fixture = ArrangeEligibleFunding();
        fixture.Config.assetId = null;

        var result = await _sut.CreateCheckoutAsync(Request("12.00"), ActorId, IdempotencyKey);

        result.Type.Should().Be(ResultType.ValidationError);
        result.Error.Should().Be("This project has no units to offer yet");
        VerifyNothingWasSettled();
    }

    [Fact]
    public async Task CreateCheckoutAsync_RefusesAProjectWhoseNodeTreasuryIsNotResolved()
    {
        var fixture = ArrangeEligibleFunding();
        fixture.Config.treasuryReference = null;

        var result = await _sut.CreateCheckoutAsync(Request("12.00"), ActorId, IdempotencyKey);

        result.Type.Should().Be(ResultType.ValidationError);
        VerifyNothingWasSettled();
    }

    [Fact]
    public async Task CreateCheckoutAsync_IncompleteSponsorNeverReachesTheProjectOrTheNode()
    {
        ArrangeEligibleFunding();
        _valueEligibility
            .Setup(service => service.RequireReadyAsync(ActorId, "fund a project", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<AzoaCustodialAccountStatusDto>.Forbidden("Complete your account setup before funding."));

        var result = await _sut.CreateCheckoutAsync(Request("12.00"), ActorId, IdempotencyKey);

        result.Type.Should().Be(ResultType.Forbidden);
        _configs.Verify(repository => repository.GetByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        VerifyNothingWasSettled();
    }

    [Fact]
    public async Task CreateCheckoutAsync_IncompleteProjectOwnerNeverReachesTheNode()
    {
        var fixture = ArrangeEligibleFunding();
        _valueEligibility
            .Setup(service => service.RequireReadyAsync(
                fixture.Project.createdById,
                "receive project funding",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<AzoaCustodialAccountStatusDto>.Forbidden("Recipient account setup is incomplete."));

        var result = await _sut.CreateCheckoutAsync(Request("12.00"), ActorId, IdempotencyKey);

        result.Type.Should().Be(ResultType.Forbidden);
        VerifyNothingWasSettled();
    }

    [Fact]
    public async Task CreateCheckoutAsync_RefusesAZeroRateRatherThanIssuingUnboundedUnits()
    {
        ArrangeEligibleFunding();
        ArrangeRate(platformPerUnit: "0", referencePerUnit: "0");

        var result = await _sut.CreateCheckoutAsync(Request("12.00"), ActorId, IdempotencyKey);

        result.Type.Should().Be(ResultType.ValidationError);
        result.Error.Should().Be("This project has no price yet, so nothing was taken.");
        VerifyNothingWasSettled();
    }

    [Fact]
    public async Task CreateCheckoutAsync_RefusesAnAmountThatDoesNotBuyAWholeUnit()
    {
        ArrangeEligibleFunding();
        ArrangeRate(platformPerUnit: "3.00", referencePerUnit: "0.03");

        var result = await _sut.CreateCheckoutAsync(Request("2.00"), ActorId, IdempotencyKey);

        result.Type.Should().Be(ResultType.ValidationError);
        result.Error.Should().Be("That amount does not buy a whole unit of this project.");
        VerifyNothingWasSettled();
    }

    [Fact]
    public async Task CreateCheckoutAsync_LocksWholeUnitsTheDeclaredRateAndTheOneIdempotencyKey()
    {
        var fixture = ArrangeEligibleFunding();
        ArrangeRate(platformPerUnit: "3.00", referencePerUnit: "0.03");
        var captured = CaptureRows();
        ArrangeAcceptedDispatch(captured);

        var result = await _sut.CreateCheckoutAsync(Request("10.00"), ActorId, IdempotencyKey);

        result.IsSuccess.Should().BeTrue();

        var intent = captured.Intent!;
        // Three whole units at 3.00 each. The 1.00 that buys no whole unit stays with the sponsor.
        intent.projectUnitQuantity.Should().Be(3);
        intent.platformAmountBaseUnits.Should().Be(900);
        intent.requestedPlatformBaseUnits.Should().Be(1000);
        intent.platformAssetCode.Should().Be("ARDA");
        intent.projectUnitAssetCode.Should().Be(fixture.Config.unitName);
        intent.projectAssetId.Should().Be(fixture.Config.assetId);
        intent.declaredRateArdaPerUnit.Should().Be("3.00");
        intent.declaredRateSource.Should().Be("test:rate");
        intent.usdReferenceMinorUnits.Should().Be(9);
        intent.exchangeIdempotencyKey.Should().Be($"{AvatarId}:funding-intent:{intent.id}");

        // The settlement carries the SAME key, so the outbox worker can only ever read back this
        // service's operation rather than creating a second one.
        var settlement = captured.Settlement!;
        settlement.idempotencyKey.Should().Be(intent.exchangeIdempotencyKey);
        settlement.kind.Should().Be(EconomicSettlementKind.EXCHANGE);
        settlement.assetCode.Should().Be(fixture.Config.unitName);
        settlement.amount.Should().Be(3m);
        settlement.scale.Should().Be(2);

        captured.DispatchRequest!.IdempotencyKey.Should().Be(intent.exchangeIdempotencyKey);
        captured.DispatchRequest.AssetCode.Should().Be(fixture.Config.unitName);
        captured.DispatchRequest.Amount.BaseUnits.Should().Be("300");
        captured.DispatchRequest.TermsSnapshot.Should().Contain("\"settlementLeg\":\"projectUnit\"");
        captured.DispatchRequest.TermsSnapshot.Should().Contain("\"projectUnitAmountBaseUnits\":\"300\"");
    }

    [Fact]
    public async Task CreateCheckoutAsync_NeverSettlesInACurrencyAmount()
    {
        ArrangeEligibleFunding();
        ArrangeRate(platformPerUnit: "3.00", referencePerUnit: "0.03");
        var captured = CaptureRows();
        ArrangeAcceptedDispatch(captured);

        await _sut.CreateCheckoutAsync(Request("9.00"), ActorId, IdempotencyKey);

        captured.Settlement!.assetCode.Should().NotBe("USD");
        captured.DispatchRequest!.AssetCode.Should().NotBe("USD");
        captured.DispatchRequest.TermsSnapshot.Should().NotContain("\"currencyCode\"");
    }

    [Fact]
    public async Task CreateCheckoutAsync_ConfirmedExchangeRecordsStakeReceiptAndGateInOneTransaction()
    {
        var fixture = ArrangeEligibleFunding();
        ArrangeRate(platformPerUnit: "3.00", referencePerUnit: "0.03");
        var captured = CaptureRows();
        ArrangeAcceptedDispatch(captured, new DateTimeOffset(2026, 10, 8, 0, 0, 0, TimeSpan.Zero));

        var result = await _sut.CreateCheckoutAsync(Request("9.00"), ActorId, IdempotencyKey);

        result.IsSuccess.Should().BeTrue();
        result.Value!.IntentId.Should().Be(captured.Intent!.id);

        captured.Intent.status.Should().Be(FundingIntentStatus.SETTLED);
        captured.Intent.azoaOperationId.Should().Be("op-1");
        captured.Intent.holdReference.Should().Be("hold-1");
        captured.Intent.releaseNotBefore.Should().Be(new DateTime(2026, 10, 8, 0, 0, 0, DateTimeKind.Utc));
        captured.Settlement!.status.Should().Be(EconomicSettlementStatus.CONFIRMED);
        captured.Outbox!.status.Should().Be(EconomicOutboxStatus.COMPLETED);

        // The stake carries the intent's own id, so one sponsorship can only ever produce one.
        captured.Investment!.id.Should().Be(captured.Intent.id);
        captured.Investment.userId.Should().Be(ActorId);
        captured.Investment.tokenAmount.Should().Be(3);

        // A receipt of the credit the exchange already made, not a bucket split.
        captured.TreasuryReceipt!.type.Should().Be(PlatformTreasuryTransactionType.FUNDING_INFLOW);
        captured.TreasuryReceipt.azoaOperationId.Should().Be("op-1");
        captured.TreasuryReceipt.azoaIdempotencyKey.Should().Be(captured.Intent.exchangeIdempotencyKey);
        captured.TreasuryReceipt.chainAssetId.Should().Be("arda-asset");
        captured.TreasuryReceipt.projectTreasuryAddress.Should().Be(fixture.Config.treasuryAddress);
        captured.TreasuryReceipt.balanceAfterMinorUnits.Should().BeNull();

        fixture.Config.fundingRaised.Should().BeApproximately(0.09d, 1e-9);
        _projectGate.Verify(
            service => service.EvaluateGate1Async(fixture.Config.id, It.IsAny<CancellationToken>()),
            Times.Once);
        // Two commits, and only two: one that authorises before any value moves, and one that
        // records everything the confirmed exchange means. The stake, the receipt, the gate and the
        // settled intent are all inside the second.
        _unitOfWork.Verify(work => work.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
        _unitOfWork.Verify(work => work.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateCheckoutAsync_ReadinessLapseHoldsTheSponsorshipInsteadOfRetryingForever()
    {
        ArrangeEligibleFunding();
        ArrangeRate(platformPerUnit: "3.00", referencePerUnit: "0.03");
        var captured = CaptureRows();
        ArrangeDispatch(captured, AzoaSettlementGatewayResult.Retry(
            "AZOA_ACCOUNT_NOT_READY",
            "The recipient's account setup could not be confirmed, so nothing was settled."));

        var result = await _sut.CreateCheckoutAsync(Request("9.00"), ActorId, IdempotencyKey);

        result.Type.Should().Be(ResultType.Conflict);
        result.Error.Should().EndWith("Nothing was taken.");
        captured.Intent!.status.Should().Be(FundingIntentStatus.HOLDING);
        captured.Intent.heldAt.Should().NotBeNull();
        captured.Intent.holdingReasonCode.Should().Be("AZOA_ACCOUNT_NOT_READY");

        // Terminal on both rows: nothing left for a worker to pick up and retry.
        captured.Settlement!.status.Should().Be(EconomicSettlementStatus.CANCELLED);
        captured.Outbox!.status.Should().Be(EconomicOutboxStatus.CANCELLED);
        _investments.Verify(
            repository => repository.AddAsync(It.IsAny<ProjectInvestment>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _projectGate.Verify(
            service => service.EvaluateGate1Async(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CreateCheckoutAsync_AmbiguousNodeAnswerRecordsNoStakeAndWaitsForReconciliation()
    {
        ArrangeEligibleFunding();
        ArrangeRate(platformPerUnit: "3.00", referencePerUnit: "0.03");
        var captured = CaptureRows();
        ArrangeDispatch(captured, AzoaSettlementGatewayResult.Unknown(
            "AZOA_TRANSPORT_UNCERTAIN",
            "The node returned no durable outcome."));

        var result = await _sut.CreateCheckoutAsync(Request("9.00"), ActorId, IdempotencyKey);

        result.IsSuccess.Should().BeTrue();
        captured.Intent!.status.Should().Be(FundingIntentStatus.SETTLEMENT_PENDING);
        captured.Settlement!.status.Should().Be(EconomicSettlementStatus.AWAITING_RECONCILIATION);
        captured.Outbox!.status.Should().Be(EconomicOutboxStatus.AWAITING_RECONCILIATION);
        _investments.Verify(
            repository => repository.AddAsync(It.IsAny<ProjectInvestment>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _treasuryReceipts.Verify(
            repository => repository.AddAsync(It.IsAny<PlatformTreasuryTransaction>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CreateCheckoutAsync_ReplayOfADifferentAmountUnderTheSameKeyIsRefused()
    {
        var fixture = ArrangeEligibleFunding();
        var settled = SettledIntent(fixture, requestedBaseUnits: 900);
        _intents
            .Setup(repository => repository.FindOneAsync(
                It.IsAny<Expression<Func<FundingIntent, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(settled);

        var result = await _sut.CreateCheckoutAsync(Request("12.00"), ActorId, IdempotencyKey);

        result.Type.Should().Be(ResultType.Conflict);
        result.Error.Should().Be("That key was already used for a different sponsorship");
        VerifyNothingWasSettled();
    }

    [Fact]
    public async Task CreateCheckoutAsync_ReplayOfASettledSponsorshipReturnsItWithoutASecondNodeCall()
    {
        var fixture = ArrangeEligibleFunding();
        var settled = SettledIntent(fixture, requestedBaseUnits: 900);
        _intents
            .Setup(repository => repository.FindOneAsync(
                It.IsAny<Expression<Func<FundingIntent, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(settled);

        var result = await _sut.CreateCheckoutAsync(Request("9.00"), ActorId, IdempotencyKey);

        result.IsSuccess.Should().BeTrue();
        result.Value!.IntentId.Should().Be(settled.id);
        _gateway.Verify(
            gateway => gateway.DispatchAsync(It.IsAny<AzoaSettlementRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetStatusAsync_ForeignActorCannotReadSponsorship()
    {
        var intent = new FundingIntent { id = "funding-1", funderUserId = "owner" };
        _intents.Setup(repository => repository.GetByIdAsync(intent.id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(intent);

        var result = await _sut.GetStatusAsync(intent.id, "foreign");

        result.Type.Should().Be(ResultType.Forbidden);
    }

    [Fact]
    public async Task GetStatusAsync_PromotesASponsorshipTheOutboxWorkerAlreadyConfirmed()
    {
        var fixture = ArrangeEligibleFunding();
        var (intent, settlement, outbox) = PendingSponsorship(fixture);
        // SUBMITTED is the outbox worker's word for "the gateway accepted it"; it never writes
        // CONFIRMED, so a promotion that only recognised CONFIRMED would never fire.
        settlement.status = EconomicSettlementStatus.SUBMITTED;
        settlement.azoaOperationId = "op-9";
        ProjectInvestment? recorded = null;
        _investments
            .Setup(repository => repository.AddAsync(It.IsAny<ProjectInvestment>(), It.IsAny<CancellationToken>()))
            .Callback<ProjectInvestment, CancellationToken>((row, _) => recorded = row)
            .ReturnsAsync((ProjectInvestment row, CancellationToken _) => row);

        var result = await _sut.GetStatusAsync(intent.id, ActorId);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be(nameof(FundingIntentStatus.SETTLED));
        result.Value.CurrencyCode.Should().Be("ARDA");
        result.Value.Amount.Should().Be("9.00");
        intent.status.Should().Be(FundingIntentStatus.SETTLED);
        recorded.Should().NotBeNull();
        recorded!.id.Should().Be(intent.id);
        outbox.status.Should().Be(EconomicOutboxStatus.COMPLETED);
    }

    [Fact]
    public async Task GetStatusAsync_TurnsAReadinessLapseLeftOnAQueuedSettlementIntoHolding()
    {
        var fixture = ArrangeEligibleFunding();
        var (intent, settlement, outbox) = PendingSponsorship(fixture);
        settlement.status = EconomicSettlementStatus.PENDING_DISPATCH;
        settlement.failureCode = "AZOA_PROJECT_OWNER_NOT_READY";
        settlement.failureDetail = "Funding settlement remains pending until the project owner completes account setup.";

        var result = await _sut.GetStatusAsync(intent.id, ActorId);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be(nameof(FundingIntentStatus.HOLDING));
        settlement.status.Should().Be(EconomicSettlementStatus.CANCELLED);
        outbox.status.Should().Be(EconomicOutboxStatus.CANCELLED);
        _gateway.Verify(
            gateway => gateway.DispatchAsync(It.IsAny<AzoaSettlementRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetStatusAsync_ReconcilesAnAmbiguousSponsorshipButNeverDispatchesFromARead()
    {
        var fixture = ArrangeEligibleFunding();
        var (intent, settlement, _) = PendingSponsorship(fixture);
        settlement.status = EconomicSettlementStatus.AWAITING_RECONCILIATION;
        _gateway
            .Setup(gateway => gateway.ReconcileAsync(It.IsAny<AzoaSettlementRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(AzoaSettlementGatewayResult.Unknown("AZOA_SETTLEMENT_AWAITING_NODE"));

        var result = await _sut.GetStatusAsync(intent.id, ActorId);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be(nameof(FundingIntentStatus.SETTLEMENT_PENDING));
        intent.settlementAttemptCount.Should().Be(1);
        _gateway.Verify(
            gateway => gateway.ReconcileAsync(It.IsAny<AzoaSettlementRequest>(), It.IsAny<CancellationToken>()),
            Times.Once);
        _gateway.Verify(
            gateway => gateway.DispatchAsync(It.IsAny<AzoaSettlementRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // -- Fixtures -----------------------------------------------------------

    private sealed record Fixture(User Actor, Project Project, ProjectTokenConfig Config);

    private sealed class CapturedRows
    {
        public FundingIntent? Intent { get; set; }
        public EconomicSettlement? Settlement { get; set; }
        public EconomicOutbox? Outbox { get; set; }
        public ProjectInvestment? Investment { get; set; }
        public PlatformTreasuryTransaction? TreasuryReceipt { get; set; }
        public AzoaSettlementRequest? DispatchRequest { get; set; }
    }

    private Fixture ArrangeEligibleFunding()
    {
        var actor = new User { id = ActorId };
        var project = new Project
        {
            id = "project-1",
            slug = "a-real-project",
            createdById = "project-owner",
            commerceEnabled = true,
            status = ProjectStatus.PUBLISHED,
        };
        var config = new ProjectTokenConfig
        {
            id = "config-1",
            projectId = project.id,
            assetId = "asset-123",
            assetName = "Project Units",
            unitName = "PROJ",
            assetScale = 2,
            status = ProjectTokenStatus.ACTIVE,
            gateStatus = ProjectGateStatus.FUNDING,
            totalSupply = 1_000,
            fundingGoal = 1_000d,
            fundingRaised = 0d,
            assetChain = "Algorand",
            treasuryChain = "Algorand",
            treasuryReference = "project-1",
            treasuryAddress = "TREASURY-ADDRESS",
            treasuryResolvedAt = DateTime.UtcNow,
            partitionsDeclaredAt = DateTime.UtcNow,
        };

        _users.Setup(repository => repository.GetByIdAsync(actor.id, It.IsAny<CancellationToken>())).ReturnsAsync(actor);
        _configs.Setup(repository => repository.GetByIdAsync(config.id, It.IsAny<CancellationToken>())).ReturnsAsync(config);
        _projects.Setup(repository => repository.GetByIdAsync(project.id, It.IsAny<CancellationToken>())).ReturnsAsync(project);
        _intents
            .Setup(repository => repository.FindOneAsync(
                It.IsAny<Expression<Func<FundingIntent, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((FundingIntent?)null);
        ArrangeRate(platformPerUnit: "3.00", referencePerUnit: "0.03");
        return new Fixture(actor, project, config);
    }

    private void ArrangeRate(string platformPerUnit, string referencePerUnit)
        => _rateSource
            .Setup(source => source.GetDeclaredRateAsync("config-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<DeclaredProjectUnitRate>.Success(new DeclaredProjectUnitRate(
                ProjectTokenConfigId: "config-1",
                ArdaPerWholeProjectUnit: platformPerUnit,
                UsdMajorUnitsPerWholeProjectUnit: referencePerUnit,
                ProjectUnitDecimals: 2,
                Source: "test:rate",
                DeclaredAt: DateTimeOffset.UtcNow,
                Version: null)));

    private CapturedRows CaptureRows()
    {
        var captured = new CapturedRows();
        _intents
            .Setup(repository => repository.AddAsync(It.IsAny<FundingIntent>(), It.IsAny<CancellationToken>()))
            .Callback<FundingIntent, CancellationToken>((row, _) => captured.Intent = row)
            .ReturnsAsync((FundingIntent row, CancellationToken _) => row);
        _settlements
            .Setup(repository => repository.AddAsync(It.IsAny<EconomicSettlement>(), It.IsAny<CancellationToken>()))
            .Callback<EconomicSettlement, CancellationToken>((row, _) => captured.Settlement = row)
            .ReturnsAsync((EconomicSettlement row, CancellationToken _) => row);
        _outboxes
            .Setup(repository => repository.AddAsync(It.IsAny<EconomicOutbox>(), It.IsAny<CancellationToken>()))
            .Callback<EconomicOutbox, CancellationToken>((row, _) => captured.Outbox = row)
            .ReturnsAsync((EconomicOutbox row, CancellationToken _) => row);
        _investments
            .Setup(repository => repository.AddAsync(It.IsAny<ProjectInvestment>(), It.IsAny<CancellationToken>()))
            .Callback<ProjectInvestment, CancellationToken>((row, _) => captured.Investment = row)
            .ReturnsAsync((ProjectInvestment row, CancellationToken _) => row);
        _treasuryReceipts
            .Setup(repository => repository.AddAsync(
                It.IsAny<PlatformTreasuryTransaction>(),
                It.IsAny<CancellationToken>()))
            .Callback<PlatformTreasuryTransaction, CancellationToken>((row, _) => captured.TreasuryReceipt = row)
            .ReturnsAsync((PlatformTreasuryTransaction row, CancellationToken _) => row);
        return captured;
    }

    /// <summary>One setup for the node call: it records what was asked and answers with one outcome.</summary>
    private void ArrangeDispatch(CapturedRows captured, AzoaSettlementGatewayResult outcome)
        => _gateway
            .Setup(gateway => gateway.DispatchAsync(It.IsAny<AzoaSettlementRequest>(), It.IsAny<CancellationToken>()))
            .Callback<AzoaSettlementRequest, CancellationToken>((request, _) => captured.DispatchRequest = request)
            .ReturnsAsync(outcome);

    private void ArrangeAcceptedDispatch(CapturedRows captured, DateTimeOffset? holdReleasesAt = null)
    {
        var receipt = new EconomicSettlementReceipt(
            OperationId: "op-1",
            ReceiptReference: "receipt-1",
            State: "Confirmed",
            Replayed: false,
            GrossBaseUnits: "300",
            FeeBaseUnits: "0",
            NetBaseUnits: "300",
            Scale: 2,
            SettlementReference: "settle-1",
            HoldReference: "hold-1",
            HoldReleasesAt: holdReleasesAt);
        ArrangeDispatch(captured, AzoaSettlementGatewayResult.Accepted("op-1", receipt.ToJson()));
    }

    /// <summary>A sponsorship already committed and sent, waiting on the node.</summary>
    private (FundingIntent Intent, EconomicSettlement Settlement, EconomicOutbox Outbox) PendingSponsorship(
        Fixture fixture)
    {
        var intent = SettledIntent(fixture, requestedBaseUnits: 900);
        intent.status = FundingIntentStatus.SETTLEMENT_PENDING;
        intent.settledAt = null;
        intent.azoaOperationId = null;
        var settlement = new EconomicSettlement
        {
            id = "settlement-1",
            kind = EconomicSettlementKind.EXCHANGE,
            status = EconomicSettlementStatus.AWAITING_RECONCILIATION,
            idempotencyKey = intent.exchangeIdempotencyKey!,
            beneficiaryUserId = intent.funderUserId,
            projectId = intent.projectId,
            assetCode = intent.projectUnitAssetCode,
            amount = intent.projectUnitQuantity,
            scale = intent.projectUnitScale,
            termsSnapshot = intent.termsSnapshot,
            version = 1,
        };
        var outbox = new EconomicOutbox
        {
            id = "outbox-1",
            settlementId = settlement.id,
            status = EconomicOutboxStatus.AWAITING_RECONCILIATION,
        };
        intent.settlementId = settlement.id;

        _intents.Setup(repository => repository.GetByIdAsync(intent.id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(intent);
        _settlements.Setup(repository => repository.GetByIdAsync(settlement.id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(settlement);
        _outboxes
            .Setup(repository => repository.FindOneAsync(
                It.IsAny<Expression<Func<EconomicOutbox, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(outbox);
        return (intent, settlement, outbox);
    }

    private static FundingIntent SettledIntent(Fixture fixture, long requestedBaseUnits)
    {
        var id = "funding-1";
        return new FundingIntent
        {
            id = id,
            semanticKey = $"funding:{ActorId}:{IdempotencyKey}",
            idempotencyKey = IdempotencyKey,
            status = FundingIntentStatus.SETTLED,
            funderUserId = ActorId,
            projectId = fixture.Project.id,
            projectTokenConfigId = fixture.Config.id,
            disclosureVersion = Disclosure,
            platformAssetCode = "ARDA",
            platformAmountBaseUnits = 900,
            platformScale = 2,
            requestedPlatformBaseUnits = requestedBaseUnits,
            projectUnitAssetCode = fixture.Config.unitName,
            projectUnitQuantity = 3,
            projectUnitScale = 2,
            projectAssetId = fixture.Config.assetId!,
            usdReferenceMinorUnits = 9,
            declaredRateArdaPerUnit = "3.00",
            declaredRateUsdPerUnit = "0.03",
            declaredRateSource = "test:rate",
            declaredRateDeclaredAt = DateTime.UtcNow,
            eligibilitySnapshot = "{}",
            termsSnapshot = "{}",
            termsHash = "hash",
            exchangeIdempotencyKey = $"{AvatarId}:funding-intent:{id}",
            settledAt = DateTime.UtcNow,
            createdAt = DateTime.UtcNow,
            updatedAt = DateTime.UtcNow,
        };
    }

    private static CreateFundingIntentDto Request(string amount) => new()
    {
        ProjectTokenConfigId = "config-1",
        Amount = amount,
        DisclosureVersion = Disclosure,
    };

    private static AzoaCustodialAccountStatusDto ReadyAccountStatus() => new()
    {
        AvatarId = AvatarId,
        WalletId = "wallet-1",
        WalletAddress = "wallet-address-1",
        KycStatus = AzoaKycStatus.Approved,
        IdentityReady = true,
        KycReady = true,
        WalletReady = true,
        Ready = true,
    };

    private void VerifyNothingWasSettled()
    {
        _intents.Verify(
            repository => repository.AddAsync(It.IsAny<FundingIntent>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _gateway.Verify(
            gateway => gateway.DispatchAsync(It.IsAny<AzoaSettlementRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _investments.Verify(
            repository => repository.AddAsync(It.IsAny<ProjectInvestment>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
