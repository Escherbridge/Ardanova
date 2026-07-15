namespace ArdaNova.Application.Tests.Services;

using ArdaNova.Application.Services.Implementations;
using ArdaNova.Application.Services.Interfaces;
using ArdaNova.Domain.Models.Enums;
using ArdaNova.Domain.ValueObjects;
using ArdaNova.Infrastructure.Azoa;
using FluentAssertions;
using Moq;
using System.Net.Http;

public class EconomicOutboxDispatchServiceTests
{
    private readonly Mock<IEconomicOutboxLeaseStore> _store = new();
    private readonly Mock<IAzoaSettlementGateway> _gateway = new();
    private readonly EconomicOutboxDispatchService _sut;

    public EconomicOutboxDispatchServiceTests()
    {
        _sut = new EconomicOutboxDispatchService(_store.Object, _gateway.Object);
    }

    [Fact]
    public async Task DispatchOneAsync_DuplicateClaimOnlySendsTheSingleWinner()
    {
        var claim = Claim();
        _store.SetupSequence(store => store.TryClaimAsync(
                EconomicOutboxClaimKind.Dispatch,
                It.IsAny<DateTime>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(claim)
            .ReturnsAsync((EconomicOutboxLease?)null);
        _store.Setup(store => store.FinalizeAsync(
                claim,
                It.IsAny<EconomicOutboxFinalization>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _gateway.Setup(gateway => gateway.DispatchAsync(claim.Request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(AzoaSettlementGatewayResult.Accepted("op-1"));

        var winner = await _sut.DispatchOneAsync();
        var loser = await _sut.DispatchOneAsync();

        winner.Outcome.Should().Be(EconomicOutboxRecordedOutcome.Accepted);
        loser.Outcome.Should().Be(EconomicOutboxRecordedOutcome.NoWork);
        _gateway.Verify(gateway => gateway.DispatchAsync(claim.Request, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DispatchOneAsync_StaleLeaseDoesNotRecordGatewayOutcome()
    {
        var claim = Claim();
        _store.Setup(store => store.TryClaimAsync(
                EconomicOutboxClaimKind.Dispatch,
                It.IsAny<DateTime>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(claim);
        _store.Setup(store => store.FinalizeAsync(
                claim,
                It.IsAny<EconomicOutboxFinalization>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _gateway.Setup(gateway => gateway.DispatchAsync(claim.Request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(AzoaSettlementGatewayResult.Accepted("op-1"));

        var result = await _sut.DispatchOneAsync();

        result.Outcome.Should().Be(EconomicOutboxRecordedOutcome.StaleLease);
        _store.Verify(store => store.FinalizeAsync(
            claim,
            It.Is<EconomicOutboxFinalization>(item => item.Outcome == EconomicOutboxRecordedOutcome.Accepted),
            It.IsAny<DateTime>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DispatchOneAsync_UsesDurableIdempotencyKeyAndRecordsReplayAsAcceptedNotCompleted()
    {
        var claim = Claim();
        EconomicOutboxFinalization? finalization = null;
        _store.Setup(store => store.TryClaimAsync(
                EconomicOutboxClaimKind.Dispatch,
                It.IsAny<DateTime>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(claim);
        _store.Setup(store => store.FinalizeAsync(
                claim,
                It.IsAny<EconomicOutboxFinalization>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .Callback<EconomicOutboxLease, EconomicOutboxFinalization, DateTime, CancellationToken>((_, item, _, _) => finalization = item)
            .ReturnsAsync(true);
        _gateway.Setup(gateway => gateway.DispatchAsync(
                It.Is<AzoaSettlementRequest>(request => request.IdempotencyKey == "funding-intent:funding-1"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(AzoaSettlementGatewayResult.Accepted("op-replayed", "{\"accepted\":true}", true));

        var result = await _sut.DispatchOneAsync();

        result.Outcome.Should().Be(EconomicOutboxRecordedOutcome.Accepted);
        finalization.Should().NotBeNull();
        finalization!.Replayed.Should().BeTrue();
        finalization.Outcome.Should().NotBe(EconomicOutboxRecordedOutcome.Unknown);
        finalization.Outcome.Should().NotBe(EconomicOutboxRecordedOutcome.Retry);
    }

    [Fact]
    public async Task DispatchOneAsync_AmbiguousGatewayResultRequiresReconciliationWithoutAnOperationId()
    {
        var claim = Claim();
        EconomicOutboxFinalization? finalization = null;
        _store.Setup(store => store.TryClaimAsync(
                EconomicOutboxClaimKind.Dispatch,
                It.IsAny<DateTime>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(claim);
        _store.Setup(store => store.FinalizeAsync(
                claim,
                It.IsAny<EconomicOutboxFinalization>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .Callback<EconomicOutboxLease, EconomicOutboxFinalization, DateTime, CancellationToken>((_, item, _, _) => finalization = item)
            .ReturnsAsync(true);
        _gateway.Setup(gateway => gateway.DispatchAsync(claim.Request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(AzoaSettlementGatewayResult.Unknown("AZOA_TIMEOUT", "Timed out after request write."));

        var result = await _sut.DispatchOneAsync();

        result.Outcome.Should().Be(EconomicOutboxRecordedOutcome.Unknown);
        finalization.Should().NotBeNull();
        finalization!.Outcome.Should().Be(EconomicOutboxRecordedOutcome.Unknown);
        finalization.OperationId.Should().BeNull();
        finalization.FailureCode.Should().Be("AZOA_TIMEOUT");
    }

    [Fact]
    public async Task DispatchOneAsync_TransportExceptionRequiresReconciliationWithoutRethrowing()
    {
        var claim = Claim();
        EconomicOutboxFinalization? finalization = null;
        _store.Setup(store => store.TryClaimAsync(
                EconomicOutboxClaimKind.Dispatch,
                It.IsAny<DateTime>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(claim);
        _store.Setup(store => store.FinalizeAsync(
                claim,
                It.IsAny<EconomicOutboxFinalization>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .Callback<EconomicOutboxLease, EconomicOutboxFinalization, DateTime, CancellationToken>((_, item, _, _) => finalization = item)
            .ReturnsAsync(true);
        _gateway.Setup(gateway => gateway.DispatchAsync(claim.Request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Connection closed after request write."));

        var result = await _sut.DispatchOneAsync();

        result.Outcome.Should().Be(EconomicOutboxRecordedOutcome.Unknown);
        finalization.Should().NotBeNull();
        finalization!.Outcome.Should().Be(EconomicOutboxRecordedOutcome.Unknown);
        finalization.FailureCode.Should().Be("AZOA_TRANSPORT_UNCERTAIN");
    }

    [Fact]
    public async Task DispatchOneAsync_CallerCancellationBubblesAndDoesNotFinalize()
    {
        var claim = Claim();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        _store.Setup(store => store.TryClaimAsync(
                EconomicOutboxClaimKind.Dispatch,
                It.IsAny<DateTime>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(claim);
        _gateway.Setup(gateway => gateway.DispatchAsync(claim.Request, cancellation.Token))
            .ThrowsAsync(new OperationCanceledException(cancellation.Token));

        var action = () => _sut.DispatchOneAsync(cancellation.Token);

        await action.Should().ThrowAsync<OperationCanceledException>();
        _store.Verify(store => store.FinalizeAsync(
            It.IsAny<EconomicOutboxLease>(),
            It.IsAny<EconomicOutboxFinalization>(),
            It.IsAny<DateTime>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DispatchOneAsync_ExpiredLeaseDoesNotSend()
    {
        var claim = Claim() with { LeaseExpiresAt = DateTime.UtcNow.AddSeconds(-1) };
        _store.Setup(store => store.TryClaimAsync(
                EconomicOutboxClaimKind.Dispatch,
                It.IsAny<DateTime>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(claim);

        var result = await _sut.DispatchOneAsync();

        result.Outcome.Should().Be(EconomicOutboxRecordedOutcome.StaleLease);
        _gateway.Verify(gateway => gateway.DispatchAsync(claim.Request, It.IsAny<CancellationToken>()), Times.Never);
        _store.Verify(store => store.FinalizeAsync(
            It.IsAny<EconomicOutboxLease>(),
            It.IsAny<EconomicOutboxFinalization>(),
            It.IsAny<DateTime>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DispatchOneAsync_SettlementStateMismatchDoesNotSend()
    {
        var claim = Claim(status: EconomicSettlementStatus.SUBMITTED);
        _store.Setup(store => store.TryClaimAsync(
                EconomicOutboxClaimKind.Dispatch,
                It.IsAny<DateTime>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(claim);

        var result = await _sut.DispatchOneAsync();

        result.Outcome.Should().Be(EconomicOutboxRecordedOutcome.StaleLease);
        _gateway.Verify(gateway => gateway.DispatchAsync(claim.Request, It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ReconcileOneAsync_RetryRemainsOnTheReconciliationPath()
    {
        var claim = Claim(EconomicOutboxClaimKind.Reconciliation, EconomicSettlementStatus.AWAITING_RECONCILIATION);
        EconomicOutboxFinalization? finalization = null;
        _store.Setup(store => store.TryClaimAsync(
                EconomicOutboxClaimKind.Reconciliation,
                It.IsAny<DateTime>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(claim);
        _store.Setup(store => store.FinalizeAsync(
                claim,
                It.IsAny<EconomicOutboxFinalization>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .Callback<EconomicOutboxLease, EconomicOutboxFinalization, DateTime, CancellationToken>((_, item, _, _) => finalization = item)
            .ReturnsAsync(true);
        _gateway.Setup(gateway => gateway.ReconcileAsync(claim.Request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(AzoaSettlementGatewayResult.Retry("AZOA_RECONCILIATION_UNAVAILABLE"));

        var result = await _sut.ReconcileOneAsync();

        result.Outcome.Should().Be(EconomicOutboxRecordedOutcome.Retry);
        finalization!.Outcome.Should().Be(EconomicOutboxRecordedOutcome.Retry);
        finalization.AvailableAt.Should().BeAfter(DateTime.UtcNow.AddSeconds(-1));
    }

    private static EconomicOutboxLease Claim(
        EconomicOutboxClaimKind kind = EconomicOutboxClaimKind.Dispatch,
        EconomicSettlementStatus status = EconomicSettlementStatus.PENDING_DISPATCH)
        => new(
            "outbox-1",
            "settlement-1",
            "lease-1",
            1,
            DateTime.UtcNow.AddMinutes(2),
            status,
            7,
            kind,
            new AzoaSettlementRequest(
                "settlement-1",
                "funding-intent:funding-1",
                "user-1",
                "USD",
                Amount("1234", 2),
                "{\"amount\":\"12.34\"}"));

    private static FixedScaleAmount Amount(string expectedBaseUnits, int scale)
    {
        FixedScaleAmount.TryFromPositiveDecimal(12.34m, scale, out var amount).Should().BeTrue();
        amount.BaseUnits.Should().Be(expectedBaseUnits);
        return amount;
    }
}

public class EconomicOutboxDispatchWorkerTests
{
    private readonly Mock<IEconomicOutboxDispatchService> _dispatcher = new();

    [Fact]
    public async Task RunOnceAsync_DefaultOptionsAreDisabledAndDoNotClaimAnything()
    {
        var worker = new EconomicOutboxDispatchWorker(_dispatcher.Object, new EconomicOutboxDispatchOptions());

        var runs = await worker.RunOnceAsync();

        runs.Should().BeEmpty();
        _dispatcher.Verify(dispatcher => dispatcher.DispatchOneAsync(It.IsAny<CancellationToken>()), Times.Never);
        _dispatcher.Verify(dispatcher => dispatcher.ReconcileOneAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RunOnceAsync_ExplicitEnableUsesTheBoundedDispatchAndReconciliationSeams()
    {
        _dispatcher.Setup(dispatcher => dispatcher.DispatchOneAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EconomicOutboxDispatchRun(EconomicOutboxRecordedOutcome.NoWork));
        _dispatcher.Setup(dispatcher => dispatcher.ReconcileOneAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EconomicOutboxDispatchRun(EconomicOutboxRecordedOutcome.NoWork));
        var worker = new EconomicOutboxDispatchWorker(
            _dispatcher.Object,
            new EconomicOutboxDispatchOptions { Enabled = true, BatchSize = 1 });

        var runs = await worker.RunOnceAsync();

        runs.Should().HaveCount(2);
        _dispatcher.Verify(dispatcher => dispatcher.DispatchOneAsync(It.IsAny<CancellationToken>()), Times.Once);
        _dispatcher.Verify(dispatcher => dispatcher.ReconcileOneAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}

public class DisabledAzoaSettlementGatewayTests
{
    [Fact]
    public async Task DispatchAsync_DefaultGatewayFailsClosedWithoutSubmittingAnOperation()
    {
        var gateway = new DisabledAzoaSettlementGateway();

        var result = await gateway.DispatchAsync(new AzoaSettlementRequest(
            "settlement-1",
            "funding-intent:funding-1",
            "user-1",
            "USD",
            CreateAmount(),
            null));

        result.Outcome.Should().Be(AzoaSettlementGatewayOutcome.Retry);
        result.OperationId.Should().BeNull();
        result.Code.Should().Be("AZOA_SETTLEMENT_DISPATCH_DISABLED");
    }

    private static FixedScaleAmount CreateAmount()
    {
        FixedScaleAmount.TryFromPositiveDecimal(12.34m, 2, out var amount).Should().BeTrue();
        return amount;
    }
}
