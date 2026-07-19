namespace ArdaNova.Application.Tests.Services;

using ArdaNova.Application.Common.Interfaces;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Implementations;
using ArdaNova.Application.Services.Interfaces;
using ArdaNova.Domain.Models.Entities;
using ArdaNova.Domain.Models.Enums;
using FluentAssertions;
using Moq;
using Stripe;

public class FundingIntentServiceTests
{
    private readonly Mock<IRepository<User>> _users = new();
    private readonly Mock<IRepository<Project>> _projects = new();
    private readonly Mock<IRepository<ProjectTokenConfig>> _configs = new();
    private readonly Mock<IRepository<FundingIntent>> _intents = new();
    private readonly Mock<IRepository<EconomicSettlement>> _settlements = new();
    private readonly Mock<IRepository<EconomicOutbox>> _outboxes = new();
    private readonly Mock<IStripeCheckoutGateway> _checkout = new();
    private readonly Mock<IFundingSettlementReadiness> _settlementReadiness = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly FundingIntentService _sut;

    public FundingIntentServiceTests()
    {
        _settlementReadiness.SetupGet(readiness => readiness.IsReady).Returns(true);
        _sut = new FundingIntentService(
            _users.Object,
            _projects.Object,
            _configs.Object,
            _intents.Object,
            _settlements.Object,
            _outboxes.Object,
            _checkout.Object,
            _settlementReadiness.Object,
            _unitOfWork.Object);
    }

    [Fact]
    public async Task CreateCheckoutAsync_RejectsBeforePersistenceOrStripeWhenSettlementIsNotOperationallyReady()
    {
        _settlementReadiness.SetupGet(readiness => readiness.IsReady).Returns(false);
        _settlementReadiness.SetupGet(readiness => readiness.UnavailableReason)
            .Returns("Settlement readiness is disabled for this test.");

        var result = await _sut.CreateCheckoutAsync(
            new CreateFundingIntentDto
            {
                ProjectTokenConfigId = "config-1",
                Amount = "12.34",
                DisclosureVersion = "funding-disclosure-v1",
            },
            "user-1",
            "9e68f25a-589c-472a-ab2d-0b4161c5ab89");

        result.Type.Should().Be(ResultType.Conflict);
        result.Error.Should().Be("Settlement readiness is disabled for this test.");
        _users.Verify(repository => repository.GetByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _intents.Verify(repository => repository.AddAsync(It.IsAny<FundingIntent>(), It.IsAny<CancellationToken>()), Times.Never);
        _checkout.Verify(gateway => gateway.CreateAsync(It.IsAny<StripeCheckoutRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateCheckoutAsync_CommitsImmutableIntentBeforeCallingStripe()
    {
        var fixture = ArrangeEligibleFunding();
        FundingIntent? savedIntent = null;
        _intents.Setup(repository => repository.AddAsync(It.IsAny<FundingIntent>(), It.IsAny<CancellationToken>()))
            .Callback<FundingIntent, CancellationToken>((intent, _) => savedIntent = intent)
            .ReturnsAsync((FundingIntent intent, CancellationToken _) => intent);
        _intents.Setup(repository => repository.UpdateAsync(It.IsAny<FundingIntent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _unitOfWork.SetupSequence(work => work.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1).ReturnsAsync(1);
        _checkout.Setup(gateway => gateway.CreateAsync(It.IsAny<StripeCheckoutRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StripeCheckoutSession("cs_123", "https://checkout.stripe.test/cs_123"));

        var result = await _sut.CreateCheckoutAsync(
            new CreateFundingIntentDto
            {
                ProjectTokenConfigId = fixture.Config.id,
                Amount = "12.34",
                DisclosureVersion = "funding-disclosure-v1",
            },
            fixture.Actor.id,
            "9e68f25a-589c-472a-ab2d-0b4161c5ab89");

        result.IsSuccess.Should().BeTrue();
        result.Value!.IntentId.Should().Be(savedIntent!.id);
        result.Value.CheckoutUrl.Should().Be("https://checkout.stripe.test/cs_123");
        savedIntent.status.Should().Be(FundingIntentStatus.AWAITING_PAYMENT);
        savedIntent.amount.Should().Be(12.34m);
        savedIntent.idempotencyKey.Should().Be("9e68f25a-589c-472a-ab2d-0b4161c5ab89");
        savedIntent.eligibilitySnapshot.Should().Contain("FUNDING");
        savedIntent.termsSnapshot.Should().Contain("12.34");
        _unitOfWork.Verify(work => work.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _checkout.Verify(gateway => gateway.CreateAsync(
            It.Is<StripeCheckoutRequest>(request => request.AmountInMinorUnits == 1234 && request.IdempotencyKey == savedIntent.semanticKey),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateCheckoutAsync_RejectsClosedFundingBeforeWritesOrProviderCall()
    {
        var fixture = ArrangeEligibleFunding();
        fixture.Config.gateStatus = ProjectGateStatus.ACTIVE;

        var result = await _sut.CreateCheckoutAsync(
            new CreateFundingIntentDto
            {
                ProjectTokenConfigId = fixture.Config.id,
                Amount = "1.00",
                DisclosureVersion = "funding-disclosure-v1",
            },
            fixture.Actor.id,
            "9e68f25a-589c-472a-ab2d-0b4161c5ab89");

        result.Type.Should().Be(ResultType.ValidationError);
        _intents.Verify(repository => repository.AddAsync(It.IsAny<FundingIntent>(), It.IsAny<CancellationToken>()), Times.Never);
        _checkout.Verify(gateway => gateway.CreateAsync(It.IsAny<StripeCheckoutRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateCheckoutAsync_RejectsUnsupportedTokenScaleBeforeWritesOrProviderCall()
    {
        var fixture = ArrangeEligibleFunding();
        fixture.Config.assetScale = 19;

        var result = await _sut.CreateCheckoutAsync(
            new CreateFundingIntentDto
            {
                ProjectTokenConfigId = fixture.Config.id,
                Amount = "1.00",
                DisclosureVersion = "funding-disclosure-v1",
            },
            fixture.Actor.id,
            "9e68f25a-589c-472a-ab2d-0b4161c5ab89");

        result.Type.Should().Be(ResultType.ValidationError);
        result.Error.Should().Contain("unsupported asset scale");
        _intents.Verify(repository => repository.AddAsync(It.IsAny<FundingIntent>(), It.IsAny<CancellationToken>()), Times.Never);
        _checkout.Verify(gateway => gateway.CreateAsync(It.IsAny<StripeCheckoutRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateCheckoutAsync_RejectsMissingAssetIdBeforeWritesOrProviderCall()
    {
        var fixture = ArrangeEligibleFunding();
        fixture.Config.assetId = null;

        var result = await _sut.CreateCheckoutAsync(
            new CreateFundingIntentDto
            {
                ProjectTokenConfigId = fixture.Config.id,
                Amount = "1.00",
                DisclosureVersion = "funding-disclosure-v1",
            },
            fixture.Actor.id,
            "9e68f25a-589c-472a-ab2d-0b4161c5ab89");

        result.Type.Should().Be(ResultType.ValidationError);
        result.Error.Should().Contain("asset id");
        _intents.Verify(repository => repository.AddAsync(It.IsAny<FundingIntent>(), It.IsAny<CancellationToken>()), Times.Never);
        _checkout.Verify(gateway => gateway.CreateAsync(It.IsAny<StripeCheckoutRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateCheckoutAsync_ExactReplayUsesExistingProviderSessionWithoutCreatingAnother()
    {
        var fixture = ArrangeEligibleFunding();
        var existing = new FundingIntent
        {
            id = "funding-1",
            funderUserId = fixture.Actor.id,
            idempotencyKey = "9e68f25a-589c-472a-ab2d-0b4161c5ab89",
            projectTokenConfigId = fixture.Config.id,
            amount = 12.34m,
            disclosureVersion = "funding-disclosure-v1",
            status = FundingIntentStatus.AWAITING_PAYMENT,
            providerCheckoutSessionId = "cs_123",
            termsSnapshot = "{\"projectTokenConfigId\":\"config-1\",\"assetId\":\"asset-123\",\"tokenScale\":6}",
        };
        _intents.Setup(repository => repository.FindOneAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<FundingIntent, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);
        _checkout.Setup(gateway => gateway.GetUrlAsync("cs_123", It.IsAny<CancellationToken>()))
            .ReturnsAsync("https://checkout.stripe.test/cs_123");

        var result = await _sut.CreateCheckoutAsync(
            new CreateFundingIntentDto
            {
                ProjectTokenConfigId = fixture.Config.id,
                Amount = "12.34",
                DisclosureVersion = "funding-disclosure-v1",
            },
            fixture.Actor.id,
            existing.idempotencyKey);

        result.IsSuccess.Should().BeTrue();
        result.Value!.IntentId.Should().Be(existing.id);
        _checkout.Verify(gateway => gateway.CreateAsync(It.IsAny<StripeCheckoutRequest>(), It.IsAny<CancellationToken>()), Times.Never);
        _intents.Verify(repository => repository.AddAsync(It.IsAny<FundingIntent>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RecordPaymentSucceededAsync_AtomicallyCreatesPendingSettlementAndOutbox()
    {
        var intent = new FundingIntent
        {
            id = "funding-1",
            currencyCode = "usd",
            amount = 12.34m,
            scale = 2,
            status = FundingIntentStatus.AWAITING_PAYMENT,
        };
        _intents.Setup(repository => repository.GetByIdAsync(intent.id, It.IsAny<CancellationToken>())).ReturnsAsync(intent);
        _intents.Setup(repository => repository.UpdateAsync(intent, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        EconomicSettlement? savedSettlement = null;
        EconomicOutbox? savedOutbox = null;
        _settlements.Setup(repository => repository.AddAsync(It.IsAny<EconomicSettlement>(), It.IsAny<CancellationToken>()))
            .Callback<EconomicSettlement, CancellationToken>((settlement, _) => savedSettlement = settlement)
            .ReturnsAsync((EconomicSettlement settlement, CancellationToken _) => settlement);
        _outboxes.Setup(repository => repository.AddAsync(It.IsAny<EconomicOutbox>(), It.IsAny<CancellationToken>()))
            .Callback<EconomicOutbox, CancellationToken>((outbox, _) => savedOutbox = outbox)
            .ReturnsAsync((EconomicOutbox outbox, CancellationToken _) => outbox);
        _unitOfWork.Setup(work => work.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        var paymentIntent = new PaymentIntent
        {
            Id = "pi_123",
            Amount = 1234,
            Currency = "usd",
            Metadata = new Dictionary<string, string> { ["fundingIntentId"] = intent.id },
        };

        var result = await _sut.RecordPaymentSucceededAsync("evt_123", paymentIntent);

        result.IsSuccess.Should().BeTrue();
        intent.status.Should().Be(FundingIntentStatus.SETTLEMENT_PENDING);
        intent.providerPaymentIntentId.Should().Be("pi_123");
        intent.verifiedProviderEventId.Should().Be("evt_123");
        intent.settlementId.Should().Be(savedSettlement!.id);
        savedSettlement.kind.Should().Be(EconomicSettlementKind.FUNDING_ALLOCATION);
        savedSettlement.status.Should().Be(EconomicSettlementStatus.PENDING_DISPATCH);
        savedSettlement.idempotencyKey.Should().Be($"funding-intent:{intent.id}");
        savedSettlement.externalEventId.Should().Be("evt_123");
        savedSettlement.amount.Should().Be(12.34m);
        savedSettlement.scale.Should().Be(2);
        savedOutbox!.settlementId.Should().Be(savedSettlement.id);
        savedOutbox.status.Should().Be(EconomicOutboxStatus.PENDING);
        savedOutbox.attemptCount.Should().Be(0);
        intent.status.Should().NotBe(FundingIntentStatus.SETTLED);
        savedSettlement.status.Should().NotBe(EconomicSettlementStatus.CONFIRMED);
        savedOutbox.status.Should().NotBe(EconomicOutboxStatus.COMPLETED);
        _unitOfWork.Verify(work => work.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(work => work.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(work => work.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RecordPaymentSucceededAsync_ExactReplayReusesPendingDecisionWithoutWrites()
    {
        var intent = new FundingIntent
        {
            id = "funding-1",
            funderUserId = "user-1",
            projectId = "project-1",
            currencyCode = "usd",
            amount = 12.34m,
            scale = 2,
            termsSnapshot = "{\"amount\":\"12.34\"}",
            status = FundingIntentStatus.SETTLEMENT_PENDING,
            providerPaymentIntentId = "pi_123",
            verifiedProviderEventId = "evt_123",
            settlementId = "settlement-1",
        };
        var settlement = new EconomicSettlement
        {
            id = intent.settlementId,
            kind = EconomicSettlementKind.FUNDING_ALLOCATION,
            status = EconomicSettlementStatus.PENDING_DISPATCH,
            idempotencyKey = $"funding-intent:{intent.id}",
            externalEventId = intent.verifiedProviderEventId,
            beneficiaryUserId = intent.funderUserId,
            projectId = intent.projectId,
            assetCode = "USD",
            amount = intent.amount,
            scale = intent.scale,
            termsSnapshot = intent.termsSnapshot,
        };
        _intents.Setup(repository => repository.GetByIdAsync(intent.id, It.IsAny<CancellationToken>())).ReturnsAsync(intent);
        _settlements.Setup(repository => repository.FindOneAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<EconomicSettlement, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(settlement);
        _outboxes.Setup(repository => repository.FindOneAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<EconomicOutbox, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EconomicOutbox { id = "outbox-1", settlementId = settlement.id, status = EconomicOutboxStatus.PENDING });

        var result = await _sut.RecordPaymentSucceededAsync("evt_123", PaymentIntentFor(intent));

        result.IsSuccess.Should().BeTrue();
        _settlements.Verify(repository => repository.AddAsync(It.IsAny<EconomicSettlement>(), It.IsAny<CancellationToken>()), Times.Never);
        _outboxes.Verify(repository => repository.AddAsync(It.IsAny<EconomicOutbox>(), It.IsAny<CancellationToken>()), Times.Never);
        _intents.Verify(repository => repository.UpdateAsync(It.IsAny<FundingIntent>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(work => work.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RecordPaymentSucceededAsync_DivergentSettlementFailsClosed()
    {
        var intent = new FundingIntent
        {
            id = "funding-1",
            funderUserId = "user-1",
            projectId = "project-1",
            currencyCode = "usd",
            amount = 12.34m,
            scale = 2,
            termsSnapshot = "{\"amount\":\"12.34\"}",
            status = FundingIntentStatus.SETTLEMENT_PENDING,
            providerPaymentIntentId = "pi_123",
            verifiedProviderEventId = "evt_123",
            settlementId = "settlement-1",
        };
        _intents.Setup(repository => repository.GetByIdAsync(intent.id, It.IsAny<CancellationToken>())).ReturnsAsync(intent);
        _settlements.Setup(repository => repository.FindOneAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<EconomicSettlement, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EconomicSettlement
            {
                id = intent.settlementId,
                kind = EconomicSettlementKind.FUNDING_ALLOCATION,
                idempotencyKey = $"funding-intent:{intent.id}",
                externalEventId = intent.verifiedProviderEventId,
                beneficiaryUserId = intent.funderUserId,
                projectId = intent.projectId,
                assetCode = "USD",
                amount = 99m,
                scale = intent.scale,
                termsSnapshot = intent.termsSnapshot,
            });

        var result = await _sut.RecordPaymentSucceededAsync("evt_123", PaymentIntentFor(intent));

        result.Type.Should().Be(ResultType.Conflict);
        _outboxes.Verify(repository => repository.AddAsync(It.IsAny<EconomicOutbox>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(work => work.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RecordPaymentSucceededAsync_SameEventWithDifferentProviderPaymentFailsClosed()
    {
        var intent = new FundingIntent
        {
            id = "funding-1",
            currencyCode = "usd",
            amount = 12.34m,
            status = FundingIntentStatus.SETTLEMENT_PENDING,
            providerPaymentIntentId = "pi_123",
            verifiedProviderEventId = "evt_123",
        };
        _intents.Setup(repository => repository.GetByIdAsync(intent.id, It.IsAny<CancellationToken>())).ReturnsAsync(intent);
        var differentPayment = PaymentIntentFor(intent);
        differentPayment.Id = "pi_other";

        var result = await _sut.RecordPaymentSucceededAsync("evt_123", differentPayment);

        result.Type.Should().Be(ResultType.Conflict);
        _settlements.Verify(repository => repository.FindOneAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<EconomicSettlement, bool>>>(),
            It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(work => work.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RecordPaymentSucceededAsync_ConcurrentExactCreateRecoversWithoutASecondDecision()
    {
        var initial = new FundingIntent
        {
            id = "funding-1",
            funderUserId = "user-1",
            projectId = "project-1",
            currencyCode = "usd",
            amount = 12.34m,
            scale = 2,
            termsSnapshot = "{\"amount\":\"12.34\"}",
            status = FundingIntentStatus.AWAITING_PAYMENT,
        };
        var persisted = new FundingIntent
        {
            id = initial.id,
            funderUserId = initial.funderUserId,
            projectId = initial.projectId,
            currencyCode = initial.currencyCode,
            amount = initial.amount,
            scale = initial.scale,
            termsSnapshot = initial.termsSnapshot,
            status = FundingIntentStatus.SETTLEMENT_PENDING,
            providerPaymentIntentId = "pi_123",
            verifiedProviderEventId = "evt_123",
            settlementId = "settlement-1",
        };
        var settlement = new EconomicSettlement
        {
            id = persisted.settlementId,
            kind = EconomicSettlementKind.FUNDING_ALLOCATION,
            status = EconomicSettlementStatus.PENDING_DISPATCH,
            idempotencyKey = $"funding-intent:{persisted.id}",
            externalEventId = persisted.verifiedProviderEventId,
            beneficiaryUserId = persisted.funderUserId,
            projectId = persisted.projectId,
            assetCode = "USD",
            amount = persisted.amount,
            scale = persisted.scale,
            termsSnapshot = persisted.termsSnapshot,
        };
        _intents.SetupSequence(repository => repository.GetByIdAsync(initial.id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(initial)
            .ReturnsAsync(persisted);
        _unitOfWork.Setup(work => work.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Microsoft.EntityFrameworkCore.DbUpdateException("unique conflict"));
        _settlements.Setup(repository => repository.FindOneAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<EconomicSettlement, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(settlement);
        _outboxes.Setup(repository => repository.FindOneAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<EconomicOutbox, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EconomicOutbox { id = "outbox-1", settlementId = settlement.id, status = EconomicOutboxStatus.PENDING });

        var result = await _sut.RecordPaymentSucceededAsync("evt_123", PaymentIntentFor(initial));

        result.IsSuccess.Should().BeTrue();
        _unitOfWork.Verify(work => work.RollbackTransactionAsync(CancellationToken.None), Times.Once);
        _unitOfWork.Verify(work => work.ClearTrackedChanges(), Times.Once);
        _settlements.Verify(repository => repository.AddAsync(It.IsAny<EconomicSettlement>(), It.IsAny<CancellationToken>()), Times.Once);
        _outboxes.Verify(repository => repository.AddAsync(It.IsAny<EconomicOutbox>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetStatusAsync_ForeignActorCannotReadFundingIntent()
    {
        var intent = new FundingIntent { id = "funding-1", funderUserId = "owner" };
        _intents.Setup(repository => repository.GetByIdAsync(intent.id, It.IsAny<CancellationToken>())).ReturnsAsync(intent);

        var result = await _sut.GetStatusAsync(intent.id, "foreign");

        result.Type.Should().Be(ResultType.Forbidden);
    }

    private (User Actor, Project Project, ProjectTokenConfig Config) ArrangeEligibleFunding()
    {
        var actor = new User { id = "user-1" };
        var project = new Project { id = "project-1", commerceEnabled = true, status = ProjectStatus.PUBLISHED };
        var config = new ProjectTokenConfig
        {
            id = "config-1",
            projectId = project.id,
            assetId = "asset-123",
            assetName = "Project Token",
            unitName = "PROJ",
            assetScale = 6,
            gateStatus = ProjectGateStatus.FUNDING,
        };
        _users.Setup(repository => repository.GetByIdAsync(actor.id, It.IsAny<CancellationToken>())).ReturnsAsync(actor);
        _configs.Setup(repository => repository.GetByIdAsync(config.id, It.IsAny<CancellationToken>())).ReturnsAsync(config);
        _projects.Setup(repository => repository.GetByIdAsync(project.id, It.IsAny<CancellationToken>())).ReturnsAsync(project);
        _intents.Setup(repository => repository.FindOneAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<FundingIntent, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((FundingIntent?)null);
        return (actor, project, config);
    }

    private static PaymentIntent PaymentIntentFor(FundingIntent intent)
        => new()
        {
            Id = "pi_123",
            Amount = checked((long)(intent.amount * 100m)),
            Currency = intent.currencyCode,
            Metadata = new Dictionary<string, string> { ["fundingIntentId"] = intent.id },
        };
}
