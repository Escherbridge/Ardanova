namespace ArdaNova.Application.Tests.Services;

using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
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
    private readonly Mock<IAzoaValueEligibilityService> _valueEligibility = new();
    private readonly EconomicOutboxDispatchService _sut;

    public EconomicOutboxDispatchServiceTests()
    {
        _store.Setup(store => store.TryRenewAsync(
                It.IsAny<EconomicOutboxLease>(),
                It.IsAny<DateTime>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((EconomicOutboxLease lease, DateTime renewalTime, TimeSpan renewalDuration, CancellationToken _) =>
                lease with { LeaseExpiresAt = renewalTime.Add(renewalDuration) });
        _valueEligibility.Setup(service => service.RequireReadyAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<AzoaCustodialAccountStatusDto>.Success(ReadyAccount()));
        _sut = new EconomicOutboxDispatchService(_store.Object, _gateway.Object, _valueEligibility.Object);
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
        _store.Verify(store => store.TryClaimAsync(
            EconomicOutboxClaimKind.Dispatch,
            It.IsAny<DateTime>(),
            It.Is<TimeSpan>(duration => duration == TimeSpan.FromMinutes(5)),
            It.IsAny<CancellationToken>()), Times.Exactly(2));
        _store.Verify(store => store.TryRenewAsync(
            It.IsAny<EconomicOutboxLease>(),
            It.IsAny<DateTime>(),
            It.IsAny<TimeSpan>(),
            It.IsAny<CancellationToken>()), Times.Exactly(2));
        _gateway.Verify(gateway => gateway.DispatchAsync(claim.Request, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DispatchOneAsync_WhenBeneficiaryAccountIsIncomplete_RetriesWithoutCallingSettlementGateway()
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
            .ReturnsAsync(true);
        _valueEligibility.Setup(service => service.RequireReadyAsync(
                "user-1",
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<AzoaCustodialAccountStatusDto>.Forbidden("Account setup is incomplete."));

        var result = await _sut.DispatchOneAsync();

        result.Outcome.Should().Be(EconomicOutboxRecordedOutcome.Retry);
        _store.Verify(store => store.FinalizeAsync(
            claim,
            It.Is<EconomicOutboxFinalization>(item =>
                item.Outcome == EconomicOutboxRecordedOutcome.Retry
                && item.FailureCode == "AZOA_ACCOUNT_NOT_READY"),
            It.IsAny<DateTime>(),
            It.IsAny<CancellationToken>()), Times.Once);
        _gateway.Verify(gateway => gateway.DispatchAsync(
            It.IsAny<AzoaSettlementRequest>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DispatchOneAsync_FundingAllocationRenewsBeforeBothReadinessChecksAndGateway()
    {
        var claim = Claim(
            settlementKind: EconomicSettlementKind.FUNDING_ALLOCATION,
            projectOwnerUserId: "project-owner");
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
            .ReturnsAsync(true);
        _gateway.Setup(gateway => gateway.DispatchAsync(claim.Request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(AzoaSettlementGatewayResult.Accepted("op-1"));

        var result = await _sut.DispatchOneAsync();

        result.Outcome.Should().Be(EconomicOutboxRecordedOutcome.Accepted);
        _store.Verify(store => store.TryRenewAsync(
            It.IsAny<EconomicOutboxLease>(),
            It.IsAny<DateTime>(),
            It.IsAny<TimeSpan>(),
            It.IsAny<CancellationToken>()), Times.Exactly(3));
        _gateway.Verify(gateway => gateway.DispatchAsync(claim.Request, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DispatchOneAsync_WhenRenewalFailsBeforeGateway_DoesNotCallGatewayOrFinalize()
    {
        var claim = Claim();
        _store.Setup(store => store.TryClaimAsync(
                EconomicOutboxClaimKind.Dispatch,
                It.IsAny<DateTime>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(claim);
        _store.SetupSequence(store => store.TryRenewAsync(
                It.IsAny<EconomicOutboxLease>(),
                It.IsAny<DateTime>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(claim)
            .ReturnsAsync((EconomicOutboxLease?)null);

        var result = await _sut.DispatchOneAsync();

        result.Outcome.Should().Be(EconomicOutboxRecordedOutcome.StaleLease);
        _valueEligibility.Verify(service => service.RequireReadyAsync(
            "user-1",
            "receive settled project value",
            It.IsAny<CancellationToken>()), Times.Once);
        _store.Verify(store => store.TryRenewAsync(
            It.IsAny<EconomicOutboxLease>(),
            It.IsAny<DateTime>(),
            It.IsAny<TimeSpan>(),
            It.IsAny<CancellationToken>()), Times.Exactly(2));
        _gateway.Verify(gateway => gateway.DispatchAsync(
            It.IsAny<AzoaSettlementRequest>(),
            It.IsAny<CancellationToken>()), Times.Never);
        _store.Verify(store => store.FinalizeAsync(
            It.IsAny<EconomicOutboxLease>(),
            It.IsAny<EconomicOutboxFinalization>(),
            It.IsAny<DateTime>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DispatchOneAsync_FundingAllocationWithIncompleteProjectOwner_RetriesWithoutCallingSettlementGateway()
    {
        var claim = Claim(
            settlementKind: EconomicSettlementKind.FUNDING_ALLOCATION,
            projectOwnerUserId: "project-owner");
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
            .ReturnsAsync(true);
        _valueEligibility.Setup(service => service.RequireReadyAsync(
                "project-owner",
                "receive project funding",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<AzoaCustodialAccountStatusDto>.Forbidden("Account setup is incomplete."));

        var result = await _sut.DispatchOneAsync();

        result.Outcome.Should().Be(EconomicOutboxRecordedOutcome.Retry);
        _valueEligibility.Verify(service => service.RequireReadyAsync(
            "user-1",
            "receive settled project value",
            It.IsAny<CancellationToken>()), Times.Once);
        _valueEligibility.Verify(service => service.RequireReadyAsync(
            "project-owner",
            "receive project funding",
            It.IsAny<CancellationToken>()), Times.Once);
        _store.Verify(store => store.TryRenewAsync(
            It.IsAny<EconomicOutboxLease>(),
            It.IsAny<DateTime>(),
            It.IsAny<TimeSpan>(),
            It.IsAny<CancellationToken>()), Times.Exactly(2));
        _store.Verify(store => store.FinalizeAsync(
            claim,
            It.Is<EconomicOutboxFinalization>(item =>
                item.Outcome == EconomicOutboxRecordedOutcome.Retry
                && item.FailureCode == "AZOA_PROJECT_OWNER_NOT_READY"),
            It.IsAny<DateTime>(),
            It.IsAny<CancellationToken>()), Times.Once);
        _gateway.Verify(gateway => gateway.DispatchAsync(
            It.IsAny<AzoaSettlementRequest>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DispatchOneAsync_FundingAllocationWithoutProjectOwner_RetriesWithoutCallingSettlementGateway()
    {
        var claim = Claim(settlementKind: EconomicSettlementKind.FUNDING_ALLOCATION);
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
            .ReturnsAsync(true);

        var result = await _sut.DispatchOneAsync();

        result.Outcome.Should().Be(EconomicOutboxRecordedOutcome.Retry);
        _store.Verify(store => store.FinalizeAsync(
            claim,
            It.Is<EconomicOutboxFinalization>(item =>
                item.Outcome == EconomicOutboxRecordedOutcome.Retry
                && item.FailureCode == "AZOA_PROJECT_OWNER_UNAVAILABLE"),
            It.IsAny<DateTime>(),
            It.IsAny<CancellationToken>()), Times.Once);
        _valueEligibility.Verify(service => service.RequireReadyAsync(
            "user-1",
            "receive settled project value",
            It.IsAny<CancellationToken>()), Times.Once);
        _gateway.Verify(gateway => gateway.DispatchAsync(
            It.IsAny<AzoaSettlementRequest>(),
            It.IsAny<CancellationToken>()), Times.Never);
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
    public async Task DispatchOneAsync_LocallyCanceledGatewayTokenRecordsUnknownAndFinalizes()
    {
        var claim = Claim();
        CancellationToken observedGatewayToken = default;
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
            .ReturnsAsync(true);
        _gateway.Setup(gateway => gateway.DispatchAsync(claim.Request, It.IsAny<CancellationToken>()))
            .Returns((AzoaSettlementRequest _, CancellationToken gatewayToken) =>
            {
                observedGatewayToken = gatewayToken;
                return WaitForCancellationAsync(gatewayToken);
            });
        var sut = new EconomicOutboxDispatchService(
            _store.Object,
            _gateway.Object,
            _valueEligibility.Object,
            TimeSpan.FromMilliseconds(10));

        var result = await sut.DispatchOneAsync();

        result.Outcome.Should().Be(EconomicOutboxRecordedOutcome.Unknown);
        observedGatewayToken.IsCancellationRequested.Should().BeTrue();
        _gateway.Verify(gateway => gateway.DispatchAsync(claim.Request, It.IsAny<CancellationToken>()), Times.Once);
        _store.Verify(store => store.FinalizeAsync(
            claim,
            It.Is<EconomicOutboxFinalization>(item =>
                item.Outcome == EconomicOutboxRecordedOutcome.Unknown
                && item.FailureCode == "AZOA_TRANSPORT_UNCERTAIN"),
            It.IsAny<DateTime>(),
            It.IsAny<CancellationToken>()), Times.Once);
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
        _gateway.Setup(gateway => gateway.DispatchAsync(claim.Request, It.IsAny<CancellationToken>()))
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
    public async Task DispatchOneAsync_CallerCancellationDuringGatewayWaitPropagatesAndDoesNotFinalize()
    {
        var claim = Claim();
        var gatewayStarted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var neverCompletes = new TaskCompletionSource<AzoaSettlementGatewayResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        _store.Setup(store => store.TryClaimAsync(
                EconomicOutboxClaimKind.Dispatch,
                It.IsAny<DateTime>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(claim);
        _gateway.Setup(gateway => gateway.DispatchAsync(claim.Request, It.IsAny<CancellationToken>()))
            .Returns((AzoaSettlementRequest request, CancellationToken gatewayToken) =>
            {
                gatewayStarted.TrySetResult(true);
                return neverCompletes.Task;
            });
        using var cancellation = new CancellationTokenSource();

        var dispatch = _sut.DispatchOneAsync(cancellation.Token);
        await gatewayStarted.Task.WaitAsync(TimeSpan.FromSeconds(1));
        cancellation.Cancel();
        Func<Task> action = async () => await dispatch;

        await action.Should().ThrowAsync<OperationCanceledException>();
        _gateway.Verify(gateway => gateway.DispatchAsync(claim.Request, It.IsAny<CancellationToken>()), Times.Once);
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

    // --- Readiness holding (gotcha 29) ------------------------------------------------------
    //
    // A readiness refusal is NOT a failing dispatch: nothing was submitted and the reservation
    // still stands at the node. The four tests below pin the three properties that keep those
    // two things apart - its own slow cadence, its exemption from the transport ceiling, and a
    // definite end that hands a person a refund instead of retrying forever.

    [Fact]
    public async Task DispatchOneAsync_HeldOnReadiness_UsesTheHoldingCadenceNotTheTransientLadder()
    {
        var claim = HeldOnReadiness(attemptCount: 5);
        var before = DateTime.UtcNow;

        var result = await _sut.DispatchOneAsync();

        result.Outcome.Should().Be(EconomicOutboxRecordedOutcome.Retry);
        _store.Verify(store => store.FinalizeAsync(
            claim,
            It.Is<EconomicOutboxFinalization>(item =>
                item.Outcome == EconomicOutboxRecordedOutcome.Retry
                && item.FailureCode == EconomicSettlementReadinessCodes.BeneficiaryNotReady
                && item.AvailableAt >= before.AddHours(11)),
            It.IsAny<DateTime>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// The regression that keeps gotcha 29 shut. Attempt 12 is past the ordinary dispatch ceiling
    /// of <see cref="EconomicOutboxDispatchService.DefaultMaximumAttempts"/>, and spending a
    /// readiness refusal against that ceiling is exactly how a reward gets marked FAILED while its
    /// hold stays live and unrefunded.
    /// </summary>
    [Fact]
    public async Task DispatchOneAsync_HeldOnReadinessPastTheTransportCeiling_StaysHeldAndIsNeverFailed()
    {
        var claim = HeldOnReadiness(attemptCount: 12);

        var result = await _sut.DispatchOneAsync();

        result.Outcome.Should().Be(EconomicOutboxRecordedOutcome.Retry);
        _store.Verify(store => store.FinalizeAsync(
            claim,
            It.Is<EconomicOutboxFinalization>(item =>
                item.FailureCode == EconomicSettlementReadinessCodes.BeneficiaryNotReady
                && item.FailureCode != EconomicSettlementFailureCodes.DispatchAttemptCeiling),
            It.IsAny<DateTime>(),
            It.IsAny<CancellationToken>()), Times.Once);
        _gateway.Verify(gateway => gateway.DispatchAsync(
            It.IsAny<AzoaSettlementRequest>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DispatchOneAsync_HeldOnReadinessPastTheHoldingWindow_EndsTheHoldAndOwesTheAmountBack()
    {
        var alerts = new Mock<IOperatorAlertService>();
        var sut = NewServiceWithAlerts(alerts);
        var claim = HeldOnReadiness(attemptCount: 41);

        var result = await sut.DispatchOneAsync();

        result.Outcome.Should().Be(EconomicOutboxRecordedOutcome.Retry);
        _store.Verify(store => store.FinalizeAsync(
            claim,
            It.Is<EconomicOutboxFinalization>(item =>
                item.FailureCode == EconomicSettlementFailureCodes.DispatchAttemptCeiling),
            It.IsAny<DateTime>(),
            It.IsAny<CancellationToken>()), Times.Once);
        alerts.Verify(service => service.RaiseAsync(
            It.Is<EconomicOperatorAlert>(alert =>
                alert.AlertCode == EconomicOperatorAlertCodes.ReadinessHoldingRefundDue
                && alert.Severity == OperatorAlertSeverity.Critical),
            It.IsAny<CancellationToken>()), Times.Once);
        alerts.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(EconomicOutboxDispatchService.ReadinessHoldingEntryAttempts, 1)]
    [InlineData(EconomicOutboxDispatchService.ReadinessHoldingEntryAttempts + 1, 0)]
    public async Task DispatchOneAsync_HeldOnReadiness_AlertsOnlyOnTheHoldingInterval(
        int attemptCount,
        int expectedAlerts)
    {
        var alerts = new Mock<IOperatorAlertService>();
        var sut = NewServiceWithAlerts(alerts);
        HeldOnReadiness(attemptCount);

        await sut.DispatchOneAsync();

        alerts.Verify(service => service.RaiseAsync(
            It.Is<EconomicOperatorAlert>(alert =>
                alert.AlertCode == EconomicOperatorAlertCodes.ReadinessHoldingStalled
                && alert.Severity == OperatorAlertSeverity.Warning),
            It.IsAny<CancellationToken>()), Times.Exactly(expectedAlerts));
    }

    /// <summary>Claims one settlement whose beneficiary is genuinely not set up yet.</summary>
    private EconomicOutboxLease HeldOnReadiness(int attemptCount)
    {
        var claim = Claim() with { AttemptCount = attemptCount };
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
            .ReturnsAsync(true);
        _valueEligibility.Setup(service => service.RequireReadyAsync(
                "user-1",
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<AzoaCustodialAccountStatusDto>.Forbidden("Account setup is incomplete."));
        return claim;
    }

    private EconomicOutboxDispatchService NewServiceWithAlerts(Mock<IOperatorAlertService> alerts)
        => new(
            _store.Object,
            _gateway.Object,
            _valueEligibility.Object,
            operatorAlerts: alerts.Object);

    private static EconomicOutboxLease Claim(
        EconomicOutboxClaimKind kind = EconomicOutboxClaimKind.Dispatch,
        EconomicSettlementStatus status = EconomicSettlementStatus.PENDING_DISPATCH,
        EconomicSettlementKind settlementKind = EconomicSettlementKind.TASK_REWARD,
        string? projectOwnerUserId = null)
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
                "{\"amount\":\"12.34\"}"),
            settlementKind,
            projectOwnerUserId);

    private static FixedScaleAmount Amount(string expectedBaseUnits, int scale)
    {
        FixedScaleAmount.TryFromPositiveDecimal(12.34m, scale, out var amount).Should().BeTrue();
        amount.BaseUnits.Should().Be(expectedBaseUnits);
        return amount;
    }

    private static async Task<AzoaSettlementGatewayResult> WaitForCancellationAsync(CancellationToken ct)
    {
        await Task.Delay(Timeout.InfiniteTimeSpan, ct);
        return AzoaSettlementGatewayResult.Accepted("unreachable");
    }

    private static AzoaCustodialAccountStatusDto ReadyAccount()
        => new()
        {
            AvatarId = Guid.NewGuid().ToString(),
            WalletId = "wallet-1",
            WalletAddress = "managed-account-address",
            IdentityReady = true,
            KycReady = true,
            KycStatus = AzoaKycStatus.Approved,
            WalletReady = true,
            Ready = true,
        };
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
