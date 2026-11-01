namespace ArdaNova.API.Tests.Flows;

using System.Globalization;
using System.Net;
using ArdaNova.API.Controllers;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Implementations;
using ArdaNova.Application.Services.Interfaces;
using ArdaNova.Application.Tests.Support;
using ArdaNova.Domain.Models.Enums;
using ArdaNova.Infrastructure.Data;
using ArdaNova.Infrastructure.Outbox;

/// <summary>
/// Round 5 on the same real-Postgres harness: the worker's terminal states read the node first (H1),
/// every send carries <c>Admit-Before</c> (H2), the completion takes the outbox lock first (M1), the
/// request path's lost-answer writes are compare-and-sets (M2), the promoter follows a released DRAFT
/// (M3), and the two guards round 4 left unpinned (L1).
/// </summary>
public sealed partial class SponsorshipVoidFlowTests
{
    /// <summary>The small dispatch ceiling the worker runs with here, so "to the ceiling" is two passes.</summary>
    private const int SmallCeiling = 2;

    private static readonly TimeSpan PastTheProofWindow =
        EconomicOutboxDispatchService.NoEffectQuietWindow + TimeSpan.FromMinutes(1);

    // -- H1: a terminal "nothing was paid" is the node's to prove --------------------------------

    [PostgresFact]
    public async Task ALostAnswerThenRefusalsToTheDispatchCeiling_TheNodesAcceptanceCompletesIt_NeverFailedOrRefunded()
    {
        var key = NewKey();
        await LoseTheAnswerToASentExchangeAsync(key);
        BeneficiaryHasNoUsableAvatar();
        await AgeAsync(key, PastTheProofWindow);
        _node.OnReceipt(AcceptedUnderTheKey);

        var outcomes = await RunWorkerToTheCeilingAsync(key);

        outcomes.Should().Equal(EconomicOutboxRecordedOutcome.Retry, EconomicOutboxRecordedOutcome.Accepted);
        var (intent, settlement, outbox) = await RowsAsync(key);
        settlement.status.Should().Be(EconomicSettlementStatus.SUBMITTED, "the node holds the exchange the lost answer was for");
        settlement.azoaOperationId.Should().NotBeNull();
        outbox.status.Should().Be(EconomicOutboxStatus.SUBMITTED);
        _alerts.Raised.Should().BeEmpty("nothing is owed back: the sponsor's exchange landed");
        _node.ExchangeCalls.Should().Be(1, "every worker attempt was refused before it sent");
        _node.ReceiptCalls.Should().Be(1, "the close asked the node first");
        intent.status.Should().Be(FundingIntentStatus.DRAFT, "the request that released it never came back");

        // M3: the closed tab's promoter records the stake from the SUBMITTED settlement - no browser, no resend.
        await AgeAsync(key, TimeSpan.FromMinutes(3));
        (await RunPromoterOnceAsync()).Promoted.Should().Be(1);
        (await RowsAsync(key)).Intent.status.Should().Be(FundingIntentStatus.SETTLED);
        (await InvestmentsAsync()).Should().Be(1);
        _node.ExchangeCalls.Should().Be(1);
    }

    [PostgresFact]
    public async Task ALostAnswerThenRefusalsToTheCeiling_UntilTheNodeIsAttested_StaysOpenWithAMayHaveMovedAlert_EvenOnANotFound()
    {
        // The shipped default: Azoa:Exchange:NodeEnforcesAdmitBefore=false (or a node rolled back past 06abcd6c).
        var key = NewKey();
        await LoseTheAnswerToASentExchangeAsync(key);
        BeneficiaryHasNoUsableAvatar();
        await AgeAsync(key, PastTheProofWindow);

        var outcomes = await RunWorkerToTheCeilingAsync(key);

        outcomes[^1].Should().Be(EconomicOutboxRecordedOutcome.Unknown);
        var (_, settlement, outbox) = await RowsAsync(key);
        settlement.status.Should().Be(EconomicSettlementStatus.AWAITING_RECONCILIATION,
            "a stalled send could still be admitted, so the NOT_FOUND proves nothing");
        outbox.status.Should().Be(EconomicOutboxStatus.AWAITING_RECONCILIATION);
        _node.ReceiptCalls.Should().Be(1);
        var alert = _alerts.Raised.Should().ContainSingle().Subject;
        alert.AlertCode.Should().Be(EconomicOperatorAlertCodes.ReconciliationAttentionRequired);
        alert.Severity.Should().Be(OperatorAlertSeverity.Critical);
        alert.Summary.Should().Contain("Value MAY have moved").And.Contain("not attested to refuse late admissions");
        alert.RequiredAction.Should().Contain("Do NOT refund");
    }

    [PostgresFact]
    public async Task ALostAnswerThenRefusalsToTheCeiling_OnAnEnforcingNode_ClosesAsUnpaidOnTheNotFoundPastTheWindow()
    {
        _admission = EnforcingNode;
        var key = NewKey();
        await LoseTheAnswerToASentExchangeAsync(key);
        BeneficiaryHasNoUsableAvatar();
        await AgeAsync(key, PastTheProofWindow);

        var outcomes = await RunWorkerToTheCeilingAsync(key);

        outcomes[^1].Should().Be(EconomicOutboxRecordedOutcome.Retry);
        var (_, settlement, outbox) = await RowsAsync(key);
        settlement.status.Should().Be(EconomicSettlementStatus.FAILED);
        outbox.status.Should().Be(EconomicOutboxStatus.FAILED);
        _node.ReceiptCalls.Should().Be(1, "closed only after the node's own NOT_FOUND");
        var alert = _alerts.Raised.Should().ContainSingle().Subject;
        alert.AlertCode.Should().Be(EconomicOperatorAlertCodes.DispatchAttemptCeiling);
        alert.Summary.Should().Contain("The node holds no exchange under this key");
        alert.RequiredAction.Should().Contain("do NOT credit or return anything", "an exchange that never landed debited nobody");
    }

    [PostgresFact]
    public async Task ACeilingInsideTheProofWindow_DefersTheClose_ThenClosesOnceTheNotFoundIsProof()
    {
        _admission = EnforcingNode;
        var key = NewKey();
        await LoseTheAnswerToASentExchangeAsync(key);
        BeneficiaryHasNoUsableAvatar();

        await RunWorkerToTheCeilingAsync(key);

        var (_, deferred, deferredOutbox) = await RowsAsync(key);
        deferred.status.Should().Be(EconomicSettlementStatus.PENDING_DISPATCH, "a NOT_FOUND inside the window proves nothing");
        deferredOutbox.status.Should().Be(EconomicOutboxStatus.PENDING);
        deferredOutbox.availableAt.Should().BeCloseTo(
            deferred.submittedAt!.Value.Add(EconomicOutboxDispatchService.NoEffectQuietWindow), TimeSpan.FromSeconds(1));
        _alerts.Raised.Should().BeEmpty();

        await AgeAsync(key, PastTheProofWindow);
        (await RunOutboxWorkerOnceAsync(maximumAttempts: SmallCeiling)).Should().Be(EconomicOutboxRecordedOutcome.Retry);

        (await RowsAsync(key)).Settlement.status.Should().Be(EconomicSettlementStatus.FAILED);
        _alerts.Raised.Should().ContainSingle(alert => alert.AlertCode == EconomicOperatorAlertCodes.DispatchAttemptCeiling);
        _node.ReceiptCalls.Should().Be(2);
        _node.ExchangeCalls.Should().Be(1);
    }

    [PostgresFact]
    public async Task AReceiptReadThatStartsInsideTheWindowButAnswersAfterIt_IsNoProof_AndTheCloseDefers()
    {
        _admission = EnforcingNode;
        var key = NewKey();
        await LoseTheAnswerToASentExchangeAsync(key);
        BeneficiaryHasNoUsableAvatar();
        await MakeOutboxDueAsync(key);
        (await RunOutboxWorkerOnceAsync(SmallCeiling)).Should().Be(EconomicOutboxRecordedOutcome.Retry);

        // The ceiling's read STARTS 4 s before the send's window ends and ANSWERS 6 s later - after it.
        await SeedAsync(key, (_, settlement, outbox) =>
        {
            settlement.submittedAt = DateTime.UtcNow - EnforcingNode.QuietWindow + TimeSpan.FromSeconds(4);
            outbox.availableAt = DateTime.UtcNow.AddSeconds(-1);
        });
        _node.ReceiptDelay = TimeSpan.FromSeconds(6);

        (await RunOutboxWorkerOnceAsync(SmallCeiling)).Should().Be(EconomicOutboxRecordedOutcome.Retry);

        var (_, settlement, outbox) = await RowsAsync(key);
        settlement.status.Should().Be(EconomicSettlementStatus.PENDING_DISPATCH,
            "a NOT_FOUND is only as late as the moment its read began");
        outbox.status.Should().Be(EconomicOutboxStatus.PENDING);
        _alerts.Raised.Should().BeEmpty();
        _node.ReceiptCalls.Should().Be(1);
    }

    [PostgresFact]
    public async Task ACeilingWhoseReceiptCannotBeRead_StaysOpenWithAMayHaveMovedAlert_AndStillCompletesLater()
    {
        var key = NewKey();
        await LoseTheAnswerToASentExchangeAsync(key);
        BeneficiaryHasNoUsableAvatar();
        await AgeAsync(key, PastTheProofWindow);
        _node.OnReceipt(ReceiptUnreadable);

        var outcomes = await RunWorkerToTheCeilingAsync(key);

        outcomes[^1].Should().Be(EconomicOutboxRecordedOutcome.Unknown);
        var (_, settlement, outbox) = await RowsAsync(key);
        settlement.status.Should().Be(EconomicSettlementStatus.AWAITING_RECONCILIATION, "never FAILED on an unreadable receipt");
        outbox.status.Should().Be(EconomicOutboxStatus.AWAITING_RECONCILIATION);
        var alert = _alerts.Raised.Should().ContainSingle().Subject;
        alert.AlertCode.Should().Be(EconomicOperatorAlertCodes.ReconciliationAttentionRequired);
        alert.Summary.Should().Contain("Value MAY have moved");
        alert.RequiredAction.Should().Contain("Do NOT refund");

        // The node answers later: the settlement was left open, so it can still complete and record the stake.
        _node.OnReceipt(AcceptedUnderTheKey);
        await MakeOutboxDueAsync(key);
        (await RunReconciliationWorkerOnceAsync()).Should().Be(EconomicOutboxRecordedOutcome.Accepted);
        await AgeAsync(key, TimeSpan.FromMinutes(3));
        (await RunPromoterOnceAsync()).Promoted.Should().Be(1);
        (await RowsAsync(key)).Intent.status.Should().Be(FundingIntentStatus.SETTLED);
        _node.ExchangeCalls.Should().Be(1);
    }

    [PostgresFact]
    public async Task ALostAnswerThenAReadinessHoldPastItsWindow_TheRefundDueReadsTheNodeFirst_AndOwesNothingBack()
    {
        var key = NewKey();
        await LoseTheAnswerToASentExchangeAsync(key);
        BeneficiaryIs(Result<AzoaCustodialAccountStatusDto>.Forbidden(ValueEligibilityMessages.SetupIncomplete("receive settled project value")));
        await AgeAsync(key, PastTheProofWindow);
        _node.OnReceipt(AcceptedUnderTheKey);

        // The smallest holding window (12 h) and a ceiling of 2: the refund falls due on attempt 3 + 2 + 1 = 6.
        var outcomes = await RunWorkerToTheCeilingAsync(
            key, attempts: 6, readinessHoldingWindow: EconomicOutboxDispatchService.ReadinessHoldingBackoff);

        outcomes[^1].Should().Be(EconomicOutboxRecordedOutcome.Accepted);
        outcomes.Take(outcomes.Count - 1).Should().AllBeEquivalentTo(EconomicOutboxRecordedOutcome.Retry);
        (await RowsAsync(key)).Settlement.status.Should().Be(EconomicSettlementStatus.SUBMITTED);
        _alerts.Raised.Should().NotContain(alert => alert.AlertCode == EconomicOperatorAlertCodes.ReadinessHoldingRefundDue);
        _node.ExchangeCalls.Should().Be(1);
    }

    [PostgresFact]
    public async Task ATakeoverOfADispatchLeaseThatExpiredMidAttempt_StampsItsSend_AndTheCloseWaitsForItsWindow()
    {
        var key = NewKey();
        await LoseTheAnswerToASentExchangeAsync(key);
        BeneficiaryHasNoUsableAvatar();
        await AgeAsync(key, TimeSpan.FromMinutes(30));
        await MakeOutboxDueAsync(key);
        // A node that enforces Admit-Before and a 10-minute quiet window: longer than the 5-minute lease.
        var admission = new ExchangeAdmissionPolicy { QuietWindow = TimeSpan.FromMinutes(10), NodeEnforcesAdmitBefore = true };

        // A worker claims and dies mid-attempt: whether its send left is unknown, and nothing recorded it.
        await using (var crashed = NewContext())
            (await new EconomicOutboxLeaseStore(crashed).TryClaimAsync(
                EconomicOutboxClaimKind.Dispatch, DateTime.UtcNow, TimeSpan.FromMinutes(5))).Should().NotBeNull();
        await SeedAsync(key, (_, _, outbox) =>
        {
            outbox.lastAttemptAt -= TimeSpan.FromMinutes(6);
            outbox.leaseExpiresAt -= TimeSpan.FromMinutes(6);
        });
        var crashedAttemptAt = (await RowsAsync(key)).Outbox.lastAttemptAt!.Value;

        await using (var context = NewContext())
            (await NewWorker(context, maximumAttempts: 1, readinessHoldingWindow: null, admission).DispatchOneAsync())
                .Outcome.Should().Be(EconomicOutboxRecordedOutcome.Retry);

        var (_, settlement, outbox) = await RowsAsync(key);
        settlement.submittedAt.Should().BeCloseTo(crashedAttemptAt, TimeSpan.FromMilliseconds(1),
            "the expired attempt's stamp became the settlement's unanswered-send stamp");
        settlement.status.Should().Be(EconomicSettlementStatus.PENDING_DISPATCH, "that send's window is still open");
        outbox.availableAt.Should().BeCloseTo(crashedAttemptAt.Add(admission.ProofWindow), TimeSpan.FromSeconds(1));
        _alerts.Raised.Should().BeEmpty();
    }

    // -- H2: Admit-Before on both send paths -------------------------------------------------------

    [PostgresFact]
    public async Task EveryExchangeSendCarriesAdmitBefore_TheRequestFromItsReleaseStamp_TheWorkerFromItsClaimStamp()
    {
        var key = NewKey();
        _node.OnExchange(() => Task.FromResult(NodeRejectedTheRequest()));
        (await CreateAsync(key)).Status.Should().Be(200, "a retryable refusal hands the settlement to the worker");
        var releasedAt = (await RowsAsync(key)).Settlement.submittedAt!.Value;

        _node.OnExchange(() => Task.FromResult(AcceptedUnderTheKey()));
        await MakeOutboxDueAsync(key);
        (await RunOutboxWorkerOnceAsync()).Should().Be(EconomicOutboxRecordedOutcome.Accepted);
        var claimedAt = (await RowsAsync(key)).Outbox.lastAttemptAt!.Value;

        _node.AdmitBefore.Should().HaveCount(2).And.AllSatisfy(header =>
            header.Should().MatchRegex(@"^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}Z$", "strict UTC, whole seconds"));
        ParseUtc(_node.AdmitBefore[0]!).Should().Be(WholeSecond(releasedAt).AddMinutes(4), "the release stamp + W_admit");
        ParseUtc(_node.AdmitBefore[1]!).Should().Be(WholeSecond(claimedAt).AddMinutes(4), "the claim stamp + W_admit");
    }

    [PostgresFact]
    public async Task TheNodesAdmissionDeadlineRefusal_IsARetryForThatAttempt_NeitherNothingTakenNorAnAmbiguousConflict()
    {
        var key = NewKey();
        _node.OnExchange(() => Task.FromResult(AdmissionDeadlinePassed()));

        var create = await CreateAsync(key);

        create.Status.Should().Be(200, "followed as pending; the key is kept");
        var (intent, settlement, outbox) = await RowsAsync(key);
        intent.status.Should().Be(FundingIntentStatus.SETTLEMENT_PENDING);
        intent.holdingReasonDetail.Should().NotContainAny("node", "deadline", "admitted", "dispatch");
        settlement.status.Should().Be(EconomicSettlementStatus.PENDING_DISPATCH,
            "THIS attempt admitted nothing - not the generic 409 that may already have run");
        settlement.failureCode.Should().Be(EconomicSettlementFailureCodes.AdmissionDeadlinePassed);
        outbox.status.Should().Be(EconomicOutboxStatus.PENDING);
        outbox.reconciliationRequiredAt.Should().BeNull();
        (await VoidAsync(key)).Body!.Outcome.Should().Be(
            FundingIntentVoidOutcomes.Revealed, "one refused attempt is no proof the key is unused");

        _node.OnExchange(() => Task.FromResult(AcceptedUnderTheKey()));
        await MakeOutboxDueAsync(key);
        (await RunOutboxWorkerOnceAsync()).Should().Be(EconomicOutboxRecordedOutcome.Accepted);
        _node.ExchangeCalls.Should().Be(2);
        _node.AdmitBefore.Should().AllSatisfy(header => header.Should().NotBeNull());
    }

    // -- M1: one lock order --------------------------------------------------------------------------

    [PostgresFact]
    public async Task ACompletionRacingAWorkersFinalizeOfTheSameSettlement_NeverDeadlocks_TheWorkerLosesCleanly()
    {
        var key = NewKey();
        await LoseTheAnswerToASentExchangeAsync(key);
        await MakeOutboxDueAsync(key);
        EconomicOutboxLease? lease;
        await using (var claim = NewContext())
            lease = await new EconomicOutboxLeaseStore(claim).TryClaimAsync(
                EconomicOutboxClaimKind.Dispatch, DateTime.UtcNow, TimeSpan.FromMinutes(5));
        lease.Should().NotBeNull();
        _node.OnReceipt(AcceptedUnderTheKey);

        Task<bool>? workerFinalize = null;
        var racing = new HookedLeaseStore
        {
            AfterConfirmOpen = async () =>
            {
                // The worker records its own (replayed) acceptance while the completion holds its locks.
                workerFinalize = Task.Run(async () =>
                {
                    await using var worker = NewContext();
                    return await new EconomicOutboxLeaseStore(worker).FinalizeAsync(
                        lease!,
                        new EconomicOutboxFinalization(
                            EconomicOutboxRecordedOutcome.Accepted, DateTime.UtcNow, Guid.NewGuid().ToString("D"), null, true, null, null),
                        DateTime.UtcNow);
                });
                await WaitUntilAnotherSessionWaitsOnALockAsync();
            },
        };

        var resumed = await CreateAsync(key, racing);

        resumed.Status.Should().Be(200);
        (await workerFinalize!).Should().BeFalse("the worker's compare-and-set lost to the completion, without a deadlock");
        var (intent, settlement, outbox) = await RowsAsync(key);
        intent.status.Should().Be(FundingIntentStatus.SETTLED);
        settlement.status.Should().Be(EconomicSettlementStatus.CONFIRMED);
        outbox.status.Should().Be(EconomicOutboxStatus.COMPLETED);
        (await InvestmentsAsync()).Should().Be(1);
    }

    // -- M2: the request path never writes a whole row over a worker -------------------------------

    public static TheoryData<HttpStatusCode, string> RequestAnswersAfterTheWorkerSubmitted() => new()
    {
        // Unknown -> ParkForReconciliationAsync.
        { HttpStatusCode.ServiceUnavailable, """{"isError":true,"code":"DEPENDENCY_UNAVAILABLE","message":"Try again shortly."}""" },
        // A retryable refusal -> LeaveToOutboxAsync.
        { HttpStatusCode.BadRequest, """{"isError":true,"code":"INVALID_REQUEST","message":"Rejected."}""" },
    };

    [PostgresTheory]
    [MemberData(nameof(RequestAnswersAfterTheWorkerSubmitted))]
    public async Task AResumedDraftsSendAnsweredAfterTheWorkerSubmittedIt_NeverWritesOverTheWorkersSubmitted(
        HttpStatusCode requestAnswer,
        string body)
    {
        var key = NewKey();
        await LeaveADormantDraftAsync(key);
        // The dormant outbox is long overdue: a worker may claim it the instant the resume releases it.
        await AgeAsync(key, TimeSpan.FromMinutes(3));
        var exchangeCalls = 0;
        _node.OnExchange(async () =>
        {
            if (++exchangeCalls > 1)
                return AcceptedUnderTheKey();

            // The resume's send is in flight; the worker claims the released settlement and its own send lands.
            (await RunOutboxWorkerOnceAsync()).Should().Be(EconomicOutboxRecordedOutcome.Accepted);
            return (requestAnswer, body);
        });

        var resumed = await CreateAsync(key);

        resumed.Status.Should().Be(200);
        var (intent, settlement, outbox) = await RowsAsync(key);
        settlement.status.Should().Be(EconomicSettlementStatus.SUBMITTED, "the worker's acceptance stands");
        settlement.azoaOperationId.Should().NotBeNull();
        outbox.status.Should().Be(EconomicOutboxStatus.SUBMITTED);
        intent.status.Should().Be(FundingIntentStatus.SETTLEMENT_PENDING, "the request follows the worker it lost to");

        (await PollAsync(intent.id)).Status.Should().Be(nameof(FundingIntentStatus.SETTLED));
        (await InvestmentsAsync()).Should().Be(1);
        _node.ExchangeCalls.Should().Be(2);
    }

    // -- M3: a released DRAFT is followed without the sponsor ----------------------------------------

    [PostgresFact]
    public async Task ALostAnswerTheSponsorNeverResends_IsFinishedByThePromoterFromTheNodesReceipt_WithoutAnotherSend()
    {
        var key = NewKey();
        await LoseTheAnswerToASentExchangeAsync(key);
        _node.OnReceipt(AcceptedUnderTheKey);

        (await RunPromoterOnceAsync()).Examined.Should().Be(0, "inside the handover delay the request may still be finishing");

        await AgeAsync(key, TimeSpan.FromMinutes(3));
        (await RunPromoterOnceAsync()).Promoted.Should().Be(1);

        (await RowsAsync(key)).Intent.status.Should().Be(FundingIntentStatus.SETTLED);
        (await InvestmentsAsync()).Should().Be(1);
        _node.ExchangeCalls.Should().Be(1, "followed by a READ; a poll never sends");
        _node.ReceiptCalls.Should().Be(1);
    }

    [PostgresFact]
    public async Task ADormantDraftThatWasNeverReleased_IsNeverReleasedOrSentByThePromoter()
    {
        var key = NewKey();
        await LeaveADormantDraftAsync(key);
        await AgeAsync(key, TimeSpan.FromMinutes(3));

        (await RunPromoterOnceAsync()).Examined.Should().Be(0);

        var (intent, settlement, _) = await RowsAsync(key);
        intent.status.Should().Be(FundingIntentStatus.DRAFT);
        settlement.status.Should().Be(EconomicSettlementStatus.AUTHORIZED, "only the sponsor's own request releases");
        _node.ExchangeCalls.Should().Be(0);
    }

    // -- L1: the two guards round 4 left unpinned ----------------------------------------------------

    [PostgresFact]
    public async Task AVoidOfAnOlderBuildsHold_UntilTheNodeIsAttested_NeverVoidsOnANotFound_TheKeyIsKept()
    {
        var key = NewKey();
        BeneficiaryIs(Result<AzoaCustodialAccountStatusDto>.Forbidden(ValueEligibilityMessages.SetupIncomplete("receive settled project value")));
        (await CreateAsync(key)).Code.Should().Be(MoneyRefusalCodes.Finished);
        await SeedAsync(key, (_, settlement, _) => settlement.submittedAt = DateTime.UtcNow.AddMinutes(-1));

        await AgeAsync(key, PastTheProofWindow + TimeSpan.FromMinutes(10));
        var answer = await VoidAsync(key);

        answer.Body!.Outcome.Should().Be(FundingIntentVoidOutcomes.Revealed,
            "without an attested admission deadline a NOT_FOUND never releases a key");
        _node.ReceiptCalls.Should().Be(1, "the node was asked; its NOT_FOUND was simply not proof");
    }

    [PostgresFact]
    public async Task AVoidOfAnOlderBuildsHoldWhoseSendIsInsideTheWindow_OnAnEnforcingNode_RevealsUntilTheNotFoundIsProof()
    {
        _admission = EnforcingNode;
        var key = NewKey();
        BeneficiaryIs(Result<AzoaCustodialAccountStatusDto>.Forbidden(ValueEligibilityMessages.SetupIncomplete("receive settled project value")));
        (await CreateAsync(key)).Code.Should().Be(MoneyRefusalCodes.Finished);
        // An older build's hold: it kept the release stamp of a send that may have left a minute ago.
        await SeedAsync(key, (_, settlement, _) => settlement.submittedAt = DateTime.UtcNow.AddMinutes(-1));

        var early = await VoidAsync(key);

        early.Body!.Outcome.Should().Be(FundingIntentVoidOutcomes.Revealed, "a NOT_FOUND read inside the window is not proof");
        _node.ReceiptCalls.Should().Be(1);

        await AgeAsync(key, PastTheProofWindow);
        (await VoidAsync(key)).Body!.Outcome.Should().Be(FundingIntentVoidOutcomes.Voided);
    }

    [PostgresFact]
    public async Task AHoldWhoseSettlementAWorkerMovedSinceTheRelease_DoesNotLand_AndTheSponsorshipFollowsTheWorker()
    {
        var key = NewKey();
        BeneficiaryIs(Result<AzoaCustodialAccountStatusDto>.Forbidden(ValueEligibilityMessages.SetupIncomplete("receive settled project value")));
        // Between the request's release and its hold, a worker claims, is refused, and puts the outbox back
        // unleased - so only the settlement's VERSION says the row moved.
        var leases = new HookedLeaseStore
        {
            BeforeCancelUnleased = async () =>
            {
                await using var worker = NewContext();
                var store = new EconomicOutboxLeaseStore(worker);
                var at = DateTime.UtcNow.AddMinutes(5);
                var lease = await store.TryClaimAsync(EconomicOutboxClaimKind.Dispatch, at, TimeSpan.FromMinutes(5));
                lease.Should().NotBeNull();
                (await store.FinalizeAsync(
                    lease!,
                    new EconomicOutboxFinalization(
                        EconomicOutboxRecordedOutcome.Retry, at.AddMinutes(1), null, null, null,
                        "AZOA_SETTLEMENT_REJECTED", "The node rejected this settlement request, so no value moved."),
                    at)).Should().BeTrue();
            },
        };

        var create = await CreateAsync(key, leases);

        create.Status.Should().Be(200);
        var (intent, settlement, outbox) = await RowsAsync(key);
        intent.status.Should().Be(FundingIntentStatus.SETTLEMENT_PENDING);
        intent.heldAt.Should().BeNull();
        settlement.status.Should().Be(EconomicSettlementStatus.PENDING_DISPATCH, "the hold's version compare-and-set lost");
        settlement.failureCode.Should().Be("AZOA_SETTLEMENT_REJECTED", "the worker's write stands");
        outbox.status.Should().Be(EconomicOutboxStatus.PENDING);
    }

    // -- Harness ------------------------------------------------------------------------------------

    /// <summary>Readiness reads fine, but the account carries no usable node avatar: the gateway refuses before sending.</summary>
    private void BeneficiaryHasNoUsableAvatar()
        => BeneficiaryIs(Result<AzoaCustodialAccountStatusDto>.Success(new()
        {
            AvatarId = "not-a-node-avatar",
            WalletId = "wallet-void",
            WalletAddress = "managed-account-address",
            IdentityReady = true,
            KycReady = true,
            KycStatus = AzoaKycStatus.Approved,
            WalletReady = true,
            Ready = true,
        }));

    /// <summary>Runs the worker <paramref name="attempts"/> times at a ceiling of <see cref="SmallCeiling"/>, the outbox due each time.</summary>
    private async Task<List<EconomicOutboxRecordedOutcome>> RunWorkerToTheCeilingAsync(
        string key,
        int attempts = SmallCeiling,
        TimeSpan? readinessHoldingWindow = null)
    {
        var outcomes = new List<EconomicOutboxRecordedOutcome>();
        for (var attempt = 0; attempt < attempts; attempt++)
        {
            await MakeOutboxDueAsync(key);
            outcomes.Add(await RunOutboxWorkerOnceAsync(SmallCeiling, readinessHoldingWindow));
        }

        return outcomes;
    }

    private EconomicOutboxDispatchService NewWorker(
        ArdaNovaDbContext context,
        int? maximumAttempts,
        TimeSpan? readinessHoldingWindow,
        ExchangeAdmissionPolicy admission)
    {
        var readiness = Readiness();
        return new EconomicOutboxDispatchService(
            new EconomicOutboxLeaseStore(context),
            NewGateway(readiness),
            readiness,
            maximumAttempts: maximumAttempts,
            operatorAlerts: _alerts,
            readinessHoldingWindow: readinessHoldingWindow,
            admission: admission);
    }

    /// <summary>Blocks until some other session in this test database is waiting on a row lock.</summary>
    private async Task WaitUntilAnotherSessionWaitsOnALockAsync()
    {
        var deadline = DateTime.UtcNow.AddSeconds(15);
        while (DateTime.UtcNow < deadline)
        {
            await using var command = _dataSource!.CreateCommand(
                "SELECT count(*) FROM pg_stat_activity WHERE datname = current_database() AND wait_event_type = 'Lock'");
            if ((long)(await command.ExecuteScalarAsync())! > 0)
                return;
            await Task.Delay(25);
        }

        throw new TimeoutException("The racing worker never queued behind the completion's lock.");
    }

    private static DateTimeOffset ParseUtc(string header)
        => DateTimeOffset.Parse(header, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);

    private static DateTimeOffset WholeSecond(DateTime stamp)
        => new(stamp.Ticks - (stamp.Ticks % TimeSpan.TicksPerSecond), TimeSpan.Zero);

    private static (HttpStatusCode, string) NodeRejectedTheRequest()
        => (HttpStatusCode.BadRequest, """{"isError":true,"code":"INVALID_REQUEST","message":"Rejected."}""");

    /// <summary>The node's exact refusal (oasis-sleek feat/exchange-admit-before): 409, branched on its code.</summary>
    private static (HttpStatusCode, string) AdmissionDeadlinePassed()
        => (HttpStatusCode.Conflict,
            """{"isError":true,"message":"This request arrived after its deadline and nothing was admitted.","result":null,"code":"ADMISSION_DEADLINE_PASSED","detail":null}""");
}
