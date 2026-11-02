# Opportunity role conversion

Project-wizard roles cross the opportunity boundary as typed allocation and
application-state fields. `OpportunityService` maps the open flag to the
existing lifecycle status and stores optional equity metadata in a versioned
compensation envelope, avoiding a schema-only field that older deployments
cannot read. API responses decode the envelope back into typed fields.

# Value-action readiness

`AzoaValueEligibilityService` is the single provider-authoritative check for
value-bearing actions. It fails closed unless the account has a stable avatar,
approved identity verification, and a managed account id and address in
addition to every aggregate readiness signal. Draft project creation and
ordinary unpaid collaboration deliberately do not call it; callers add it only
after actor authorization and before an action can promise, move, or receive
value.

The gate has THREE outcomes, not two, and the difference is load-bearing.
`Forbidden` means the account was read and this person genuinely has setup steps
left. `Conflict` means readiness could not be confirmed at all — AZOA unreachable,
a malformed answer, a node outage — and says nothing about the person. Flattening
Conflict into Forbidden blames someone who already finished setup for the platform
being down and sends them back to a flow they completed. Callers propagate the
outcome with `ValueEligibilityFailure.Propagate` (services) or
`ValueEligibilityFailure.StatusCode` (controllers, 409 vs 403); the outbox emits
`AZOA_ELIGIBILITY_UNCONFIRMED` rather than `AZOA_ACCOUNT_NOT_READY` so an operator
triages the platform, not the recipient. Never restate the rule inline.

Funding rechecks the project owner after external payment confirmation and
immediately before a funding-allocation dispatch. An incomplete owner leaves
the payment in a durable pending settlement/outbox and retries the leased
outbox without gateway submission; it never synthesizes a refund.

# Worker awards

An award is the platform's promise that a person will be paid for a piece of work,
and it becomes real in two steps, each of which opens a hold on the shared node.
Accepting a bid opens the FIRST hold, drawn from the project's declared contributor
allowance: the units stop being spendable by anyone else the moment someone is asked
to do the work. Approving the completed work continues that hold into a SECOND one -
the worker's own window - so the units are theirs and restricted at the same time.

`TokenAllocation`'s hold columns are a MIRROR, never a source. The node's reserve
ledger decides whether an allowance can still afford an award; those columns exist so
a person can be shown their own award without asking the node on every page load.
`TokenAllocation.IsNodeHeld` (a non-null `holdId`) is the single predicate every payout
path reads, and it is why a historical reservation - every row that predates this
design - can never be paid: nothing was ever committed for it.

`workerHoldReleasesAt` is the end of the worker's window, and the reason it exists is
worth saying to the worker in their own words: the award is theirs, and it is held
until that date so they are not taxed on it before they can realise it. It is NOT a
chargeback or refund window. Reward units are drawn from the platform reserve and were
never bought with a card, so there is nothing to reverse - describing it as a refund
window would be both false and frightening.

The hold id is the idempotency anchor for a whole award: it is derived from the
accepted bid, so a redelivered acceptance addresses the hold it already opened rather
than opening a second one. The unique index on `holdId` is what makes that true under
concurrency; without it two simultaneous acceptances of one bid could each write their
own allocation row against a single node commitment.

## §award-window-instant (F31)

The node keys the approve's `ReleaseIntoHold` on the request key AND the hash of the whole
intent, `nextReleaseNotBefore` included, to the 100 ns tick; the same key with a different
instant is refused. `ProjectTokenService.ResolveWorkerWindowAsync` therefore (1) adopts the
instant the node already committed when the worker window exists - a local failure AFTER the
node's commit (found live: the first-credit `CreditAsync` bug below) or a lost response must
replay the node's bytes - else (2) decides the instant once, truncated to whole seconds
because `workerHoldReleasesAt` is `timestamp(3)`, and saves it before the call. The node's
hash check, not this service, is what proves an adopted window is this award's.

`TokenBalanceService.CreditAsync` must not call `UpdateAsync` on a balance it just added:
`DbSet.Update` re-marks it Modified and the first credit becomes a 0-row UPDATE
(`DbUpdateConcurrencyException`). Pinned in `TokenBalanceServiceTrackingTests`.

A guild bid cannot enter commerce yet (`TaskCommerceService.GuildSplitUnsupported`): a
hold has exactly one beneficiary and a guild bid records no split, so accepting one
would hold the whole award for whichever member happened to submit it. This is a
fail-closed stub, unblocked by a declared split at bid time.

# Reward dispatch hygiene

A readiness refusal is not a failing dispatch. Nothing was submitted and the
reservation still stands at the node, so it must never be spent against the transport
ceiling - that is how a reward is marked FAILED twenty minutes after accept while its
hold stays live and unrefunded. The dispatcher gives readiness its own schedule: two
fast attempts for a service that blinked, then a flat 12-hour HOLDING cadence, an
operator alert every seven days, and a refund ceiling that ends the hold instead of
retrying forever. The failure code stays the readiness code because those four codes
are the platform's HOLDING marker and `FundingIntentService` reads the same four off
the settlement row. `EconomicOperatorAlertCodes` are the alerting counterpart of
`EconomicSettlementFailureCodes`: the failure code decides the row's next state, the
alert code decides who gets woken.

# Cash out

Cashing out is TWO steps, and the split is deliberate. A sale is a node exchange run
under a key derived from the `PayoutRequest` row id - which is why the row is written
_before_ the node is called, so a request that dies mid-flight still leaves something
reconciliation can name. A withdrawal is a node-owned cash-out that ArdaNova READS BACK
rather than mirrors: duplicating it would make two records of one movement of real money,
the ledger-of-record shape D47 rejected for stake.

Units move only against terminal node evidence. HTTP refusals alone do not unlock: a
receipt read can lose permission after a send, and an absent receipt can precede a late send.
Only a terminal failed receipt or the immutable admission proof below returns units.

## Quoted sales and immutable admission proof (P5-39a)

`CreateSaleQuoteAsync` persists subject, exact exchange command, quantity, proceeds,
denomination, and expiry. `AcceptSaleQuoteAsync` serializes admissions per subject;
(user, requestKey) and quoteId are unique. Replays resolve the original sale before expiry or
readiness checks and never reprice it. Unadmitted requests use a separate subject/key/quote
bound PayoutSaleVoid tombstone. Void and admission share the same subject lock: missing means
a durable void; an existing admission is revealed, never cancelled blindly. Expired or missing
quotes can therefore resolve without fabricated sale rows or an endlessly saved browser key.

A new sale commits one `exchangeAdmitBefore` before any node call. It is immutable across
attempts. Only the node's typed NOT_FOUND, from a read that started after the deadline plus
clock skew and margin, proves no admission is possible; NodeEnforcesAdmitBefore must also
be attested. Legacy null deadlines and any observed node receipt/operation stay unresolved.
Revoked consent, timeouts, plain 404s and HTTP refusal codes never prove absence.
Cancellation obeys the same proof.

Terminalization takes the subject advisory lock and then the payout row lock, refreshes
state, and changes balances and payout state in one transaction. It rechecks absence against
the fresh row. Two reconcilers, cancellation and a late answer cannot return or debit the
same units twice. Test composition may omit the store; production registers it and quoted
admission refuses a missing store. Node calls remain outside accounting transactions.

New sale JSON carries large amounts as decimal strings. Each quote and sale persist their
ARDA denomination; `PayoutSaleDenomination` defines its two-decimal base-unit contract.

## Claimed-account continuity and handoff

Minimal claimed status intentionally withholds KYC and wallet fields. Preserve local mirrors
and never persist its Unknown as a new compliance verdict. A full, correctly bound claimed
response may refresh the wallet while the node user id stays stable. Value actions check live
grant scopes and expiry; paused dispatch keeps its idempotency key and reservations.

Handoff derives email from the verified server user and builds a fixed return path from the
configured application origin. Responses carry metadata only. Tokens and hosted URLs stay
in the node's protected operator delivery stub; delivery acknowledgement never means email
was sent. Node and BFF gates default off. Bank setup is a first-party node-hosted action.

A holding with no recorded `holderClass` is LISTED but never sellable. Every sellability
rule is keyed on the class, and defaulting one would decide a sale on an invented fact;
hiding the holding entirely would be worse than saying why it cannot be sold.

# Sponsorship request keys

`FundingIntentService.CreateCheckoutAsync` answers a repeated `X-Idempotency-Key` by REPLAYING
the intent that key already admitted, and the replay lookup runs before the sponsor's readiness
gate and the project's fundability check. An intent this key admitted must finish (or report its
final state) even if the person went under review or the project's gate closed since the first
call; checking readiness first would answer an admitted request's retry with "finish your setup".
It also means every refusal after the lookup is pre-admission.

Which 409s are final is said in a machine code, not left to a status
(`ArdaNova.API/Controllers/AGENTS.md` §Money refusal codes): a hold (settlement and outbox
CANCELLED, nothing taken), a HOLDING or closed replay, and a key reused for a different
sponsorship carry `INTENT_FINISHED` / `REQUEST_KEY_IN_USE` - which the BFF maps to `finished`, a
code no caller releases on; a voided key carries `REQUEST_VOIDED`. The attempt-ceiling 409 ("being
completed; check back shortly") carries none, because that sponsorship may still settle.

# Sponsorship void

`FundingIntentService.VoidRequestAsync` (`POST /api/funding-intents/requests/{key}/void`) is the
only server answer a sponsor's browser may drop its saved key on. It voids only what it can prove
dead and reveals everything else:

| Under (funderUserId, key)                                                                               | Void does                                                                                                | Answer                                                                                         |
| ------------------------------------------------------------------------------------------------------- | -------------------------------------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------- |
| no intent                                                                                               | writes a `FundingIntentVoid` tombstone, then re-reads                                                    | `voided` (no id) - or decides the intent a racing create committed                             |
| CANCELLED (a void's own)                                                                                | nothing                                                                                                  | `voided`                                                                                       |
| DRAFT, settlement AUTHORIZED                                                                            | CAS AUTHORIZED -> CANCELLED (outbox PENDING+unleased -> CANCELLED) AND intent CANCELLED, one transaction | `voided`; a lost CAS re-reads and decides again                                                |
| DRAFT, settlement CANCELLED by `VOIDED_BY_SPONSOR` (a pre-fix two-commit void that half-landed)         | repairs the intent to CANCELLED                                                                          | `voided`                                                                                       |
| DRAFT, settlement released                                                                              | nothing - the request that released it may be asking the node now                                        | `revealed`                                                                                     |
| HOLDING / FAILED / REJECTED, settlement closed, no node operation, never released (`VOIDED_BY_SPONSOR`) | nothing                                                                                                  | `voided`                                                                                       |
| HOLDING / FAILED / REJECTED, settlement closed, no node operation, released                             | reads the node's receipt by idempotency key (§Node proof)                                                | `voided` only on a believable NOT_FOUND; else `revealed` (a Landed receipt also logs Critical) |
| SETTLEMENT_PENDING (after the advance a poll makes), SETTLED, a recorded node operation, anything else  | nothing                                                                                                  | `revealed`                                                                                     |

**AUTHORIZED is the void's window.** A sponsorship's settlement is created AUTHORIZED, which the
outbox worker never takes (it leases PENDING_DISPATCH only), and only `ReleaseAndDispatchAsync`
moves it to PENDING_DISPATCH - by compare-and-set (`IEconomicOutboxLeaseStore.TryReleaseForDispatchAsync`),
immediately before the request's own node call. So "nothing has been or can be dispatched" is a
row state, and the void's cancel and the release are one compare-and-set race with one winner. This
is also the transition `EconomicSettlementStateMachine` allows (AUTHORIZED -> CANCELLED, never
PENDING_DISPATCH -> CANCELLED). Consequence: a request that dies after its commit and before its
release leaves a dormant DRAFT that nothing dispatches until its sponsor tries again (a replay
releases it the same way) or cancels it.

**The tombstone handshake.** A tombstone lives in its own table, so no unique index serialises it
with an intent. Instead each side writes its own marker and only then reads the other's: the void
commits the tombstone and then reads for an intent; the create commits its intent and then
(`ReleaseAndDispatchAsync`) reads for a tombstone. Under READ COMMITTED at least one of the two sees
the other, so a void can never answer `voided` while that create dispatches. A create that sees the
tombstone cancels its own still-AUTHORIZED settlement and answers `REQUEST_VOIDED`; a later create
with the key is refused before readiness. Proven on real Postgres by
`ArdaNova.API.Tests/Flows/SponsorshipVoidFlowTests`.

**Never under a lease (M-5).** `TryHoldAsync` cancels a lapsed settlement through
`TryCancelUnleasedAsync`: PENDING_DISPATCH at the expected version AND its outbox PENDING with no
lease, outbox row first (`FinalizeAsync`'s lock order). A worker holding the lease may be mid-dispatch,
so the hold does not land: a request-path lapse leaves the intent SETTLEMENT_PENDING following that
worker, and a poll's lapse falls through to the ordinary pending checks. A lease proves only that no
WORKER is sending; it is blind to a request-path send, which never takes one - that is what §Node proof
is for.

**No whole-row write over a worker (round 5 M2).** The request path's two lost-answer writes are
compare-and-sets too: `ParkForReconciliationAsync` (`TryParkUnleasedForReconciliationAsync`) and
`LeaveToOutboxAsync` (`TryLeaveUnleasedToWorkerAsync`) land only on PENDING_DISPATCH at the version this
request released and an unleased PENDING outbox; a lost CAS means a worker took the row (it may have
SUBMITTED it), and the intent just follows it (`FollowPendingAsync`). Every CAS the service makes is
mirrored onto the tracked rows AS PERSISTED (`IUnitOfWork.MarkPersisted`): the old "a later save repeats
it" mirror re-wrote the release's PENDING_DISPATCH over a worker's SUBMITTED on the next intent save.
`MarkPersisted` accepts exactly the columns that CAS wrote (`FundingIntentService`'s `*Columns` lists, kept in
step with the store's `ExecuteUpdate` setters); any other pending change stays pending, and an empty or
unknown column list throws (round 5b MED-2 - a whole-row accept silently dropped other pending changes).

**One lock order, outbox first (round 5 M1).** `FinalizeAsync`, `CancelBothAsync` and the park/leave
CASes update the outbox row before the settlement. `CompleteAsync` used to CAS the settlement first and
write the outbox at `SaveChanges` - the reverse - so it deadlocked against a worker finalizing the same
settlement. `TryConfirmOpenAsync` now takes `SELECT ... FOR UPDATE` on the outbox row before its
settlement CAS, inside `CompleteAsync`'s transaction, so the worker queues behind it and loses cleanly.

**One transaction per cancel (round 4 #2).** Every cancel - the void's AUTHORIZED cancel and the
hold - commits its compare-and-set and the intent write that names it together
(`CancelInOneTransactionAsync`; the store cancels inside a savepoint of the ambient transaction).
`ct` is the request's abort token, so with two commits an abort between them left DRAFT + CANCELLED:
the void then answered `revealed intent=DRAFT` forever, and a same-key replay DISPATCHED the cancelled
settlement while `CompleteAsync`'s full-row update rewrote CANCELLED as CONFIRMED. Now:
`ResumeDraftAsync` releases ONLY an AUTHORIZED settlement (a `VOIDED_BY_SPONSOR` cancel is repaired and
answered `REQUEST_VOIDED`); `DecideVoidAsync` voids that same half state; and `CompleteAsync` confirms
through `TryConfirmOpenAsync` - a compare-and-set from PENDING_DISPATCH/SUBMITTED/AWAITING_RECONCILIATION/
CONFIRMED only, the open states `EconomicSettlementStateMachine` lets reach CONFIRMED. A node acceptance
for a closed settlement is logged Critical and answered with an uncoded 409 (the key stays).

# Node proof

A local label is not proof. "CANCELLED, `azoaOperationId` null" means nothing landed only if no send
ever reached the node - but the operation id is written only AFTER the node answers, so a lost answer
leaves the label and not the reality. Every "nothing was taken" on a settlement that was RELEASED
(PENDING_DISPATCH or later) is therefore decided by THE AUTHORITY: the node's receipt, read by
idempotency key through `IAzoaSettlementGateway.ReconcileAsync` (`ReadNodeReceiptAsync`):

| Receipt                                                                                                                                                                    | Verdict    | Hold / fail / void                                                  | Resume of a lost send |
| -------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ---------- | ------------------------------------------------------------------- | --------------------- |
| Accepted with an operation id                                                                                                                                              | `Landed`   | complete the sponsorship (record the stake) - never hold or fail it | complete it           |
| the node's NOT_FOUND envelope, from a read STARTED past every send's quiet window, on a node attested to enforce `Admit-Before` - or for a key nothing was ever sent under | `Absent`   | hold (a lapse), fail (a closed settlement), void                    | hand to the worker    |
| any NOT_FOUND while enforcement is not attested, NOT_FOUND inside the window, 5xx, timeout, non-terminal, unconfigured                                                     | `Unproven` | follow it as SETTLEMENT_PENDING; never "nothing taken"              | hand to the worker    |

**The quiet window and the admission deadline (round 5 H2).** The node admits an exchange only at step
13 of `ReserveExchangeManager.ExchangeAsync` (oasis-sleek), after governance, rate, readiness and account
reads, and its receipt answers NOT_FOUND until then - so a 404 read while an earlier send is still in
flight is not proof. Every send therefore starts only after a durable stamp - `EconomicSettlement.submittedAt`
(the release CAS, `TryReleaseForDispatchAsync`, BEFORE the request-path send) or `EconomicOutbox.lastAttemptAt`
(every worker claim) - and carries the header `Admit-Before: <stamp + W_admit>` (RFC 3339, UTC, whole
seconds, rounded down; `ExchangeAdmissionPolicy`). Exactly two callers send: `DispatchJustReleasedAsync`
(request path) and `EconomicOutboxDispatchService` (worker, from its claim's `LastAttemptAt`). The node
refuses to admit past it with 409 `ADMISSION_DEADLINE_PASSED` (nothing admitted), which the gateway maps
by CODE - never by the 409, which on that route also means "may already have run" - to a Retry
(`AZOA_ADMISSION_DEADLINE_PASSED`). It is a PER-ATTEMPT answer: the node's early refusal does not re-read a
concurrent same-key admission that has not committed yet, so it is never proof the key is unused while
another attempt may be in flight - it follows the same window rule. The gateway also never sends a request
whose deadline has already passed. A replay of an already-admitted key ignores the deadline and returns the
original receipt. `LatestSendStamp` reads both stamps before the receipt; a send that starts later must take
a lease or move the row, which the hold's version-and-unleased CAS then loses to.

| Setting (`Azoa:Exchange:*`) | Default | Meaning                                                                                     |
| --------------------------- | ------- | ------------------------------------------------------------------------------------------- |
| `AdmitWindowSeconds`        | 240     | W_admit: how long after its stamp a send may be admitted                                    |
| `QuietWindowSeconds`        | 300     | W: how long after the latest stamp a NOT_FOUND is proof (enforcing node only)               |
| `MaximumClockSkewSeconds`   | 30      | the BFF-vs-SurrealDB-host clock difference (and BFF replica-to-replica) the proof tolerates |
| `NodeEnforcesAdmitBefore`   | false   | operator attestation that the deployed node enforces the header                             |

**The clock assumption.** The DECIDING clock is the node's DATABASE, not its process: the admission
write carries `IF time::now() > $deadline THROW` inside the same transaction as its CREATE (oasis-sleek
`SurrealHoldCommandStore.Exchange.cs`), so no admission can commit past the deadline and there is no
check-then-commit gap; the node-process check is only an early refusal. The skew budget is therefore the
BFF clock against the SurrealDB HOST clock (`time::now()`), and since one BFF replica may stamp while another
reads, BFF replica-to-replica skew counts too; all are NTP-synced on Railway and real skew is sub-second.
Boot fails (`AzoaExchangeAdmissionSettings`) unless **W >= W_admit + skew + 1 s**: an admission can commit no
later than stamp + W_admit + skew on the BFF clock, so a NOT_FOUND from a read STARTED at stamp + W follows
every possible admission. The extra second (`MinimumMargin`) covers the request path computing its deadline
from an unrounded clock while the stamp is stored as TIMESTAMP(3). The brief's "1 minute allowance" is the gap
W - W_admit (300 - 240 s); it must exceed the assumed skew, so the default skew is 30 s. A NEW attempt
re-stamps (a new claim), so the latest stamp bounds them all.

**The read's START is the read's time (round 5b HIGH-2).** A receipt read may take most of the gateway's
4.5-minute deadline - far more than the window's spare margin - so every NOT_FOUND is judged at the instant
its read BEGAN (`DecideClosingAsync`'s `readStartedAt`; `ReadNodeReceiptAsync` judges before it reads; the
outbox reopen judges at its reconcile claim), never at the instant the answer arrived.

**No proof at all until the node enforces it (round 5b HIGH-1).** Without an enforced deadline a stalled
send stamped at S can be admitted at S + 7 min, after a NOT_FOUND read at S + 5 min - so while
`NodeEnforcesAdmitBefore` is false (the default, or a node rolled back) a NOT_FOUND after an unanswered send
is NEVER "nothing was taken" (`ExchangeAdmissionPolicy.AbsenceIsBelievable`): the worker's close stays OPEN
with the Critical "value MAY have moved - do NOT refund" alert, a hold/fail does not land, and the void
answers `revealed` (the key is kept). A key nothing was ever sent under is still believed at once. Only two
things still act on a NOT_FOUND past the 5-minute `ProofWindow` without the flag, because neither claims
anything was untaken: the outbox's no-effect reopen (a same-key resend, which the node replays and so cannot
double-pay) and the poll's decision to READ the receipt at all (a landed exchange still completes). The
header is harmless before enforcement (an older node ignores it). Set the flag only after the node build that
enforces it is deployed (oasis-sleek `feat/exchange-admit-before`, 06abcd6c) and every send stamped before it
is past W. **Rollback order:** before rolling the node back past 06abcd6c, set
`Azoa:Exchange:NodeEnforcesAdmitBefore=false` and redeploy the BFF FIRST (`conductor/RUNBOOK.md` §8).

**The worker's terminal states read the node first (round 5 H1).** `DispatchCeiling` (~10 refused
attempts) and `ReadinessRefundDue` (past the 14-day hold) used to close a settlement FAILED and tell an
operator "nothing was paid out, return the funds" on lease evidence alone - blind to a request-path send
whose answer was lost (base-client 30 s abort -> DRAFT + PENDING_DISPATCH, every worker attempt refused
before sending). Now `EconomicOutboxLease.SubmittedAt` (the latest stamp of a send whose answer was never
recorded) gates both: with one, `DecideClosingAsync` reads the receipt by key - Accepted -> finalized
Accepted (SUBMITTED; the promoter records the stake); on an enforcing node, NOT_FOUND from a read started past
the window -> the terminal state as before, and inside it -> stays dispatchable and decides again at the
window's end; anything else, including every NOT_FOUND while enforcement is not attested -> a non-terminal
ReconciliationCeiling (AWAITING_RECONCILIATION, stays in `TryConfirmOpenAsync`'s open set) with a Critical
"value MAY have moved - reconcile, do not refund" alert that names why. Two worker sends are unrecorded and
stamp `submittedAt` (GREATEST, no version bump): a dispatch lease that EXPIRED mid-attempt (the takeover claim
stamps that attempt's `lastAttemptAt`) and a dispatch that ended `Unknown` (`FinalizeAsync` stamps the lease's
`LastAttemptAt`, round 5b MED-1) - so no close of a reward or refund that was ever sent can say "never sent".
An exchange that never landed debited nobody, so its close alert says to refund NOTHING.

**P6-03: never rotate the value key while a settlement is open.** Receipts are scoped to the calling API
key (the node's `ForRequest(tenant, apiKeyId, key)`), so after a value-key rotation a receipt read is a
typed NOT_FOUND for an exchange that exists - and this section, the outbox reopen and the worker's close
now all trust a NOT_FOUND. Precondition until P6-03 (tenant-scoped receipt reads) closes: rotate only
when no settlement is PENDING_DISPATCH or AWAITING_RECONCILIATION. `AzoaExchangeProofBootCheck` logs the
key's fingerprint and the open count at boot, and logs Critical when `Azoa:ValueKeyFingerprint` is pinned
and no longer matches while any is open.

**The one exemption: released-and-proven-unsent within THIS call.** `DispatchJustReleasedAsync` runs
only right after this call's own release. The gateway raises a readiness refusal before it sends, no
worker can have claimed in between (the hold's CAS checks the version and an unleased PENDING outbox),
and the settlement was AUTHORIZED until this call - so that hold needs no node read, and its CAS also
clears `submittedAt` (nothing was submitted), which lets a later void believe a NOT_FOUND at once.

**Who asks.** `ResumeDraftAsync` ("Try again" on a DRAFT whose settlement an earlier request released)
never re-sends without a lease: it reads the receipt, completes on `Landed`, and otherwise sets the
intent SETTLEMENT_PENDING for the leased outbox worker, which re-sends under the same key (the node
replays a landed one). The sponsor may never come back, so `GetStatusAsync` - the poll and the promoter
(`SettlementPendingPromoter` now also scans DRAFT intents whose settlement left AUTHORIZED) - follows such
a DRAFT the same read-only way (`FollowDormantReleaseAsync`, round 5 M3), once its release is older than
`OutboxHandoverDelay`; it never releases an AUTHORIZED settlement or sends.

**Sponsor-facing copy names no upstream wording (round 5 L2).** A pending intent reads a fixed
"still being completed" sentence (`PendingMessage`), a hold reads a sentence keyed on its readiness
CODE (`HoldingMessage`), and a node-proven failure a fixed "nothing was taken" - never the gateway's or
the worker's text, which may say "remains pending" after the hold CANCELLED the settlement. `AdvancePendingAsync` (status poll, promoter, replay, void) sends a lapse or a
closed settlement through `SettleOnNodeProofAsync`, which skips the read entirely inside the window (a
404 there would prove nothing) and asks at most once per `NodeProofReadInterval` per intent. The void's
HOLDING/FAILED/REJECTED branch (`DecideRecordedAsNothingTakenAsync`) reads it every time - it is
user-initiated - and is pinned against its local guards too: a recorded `azoaOperationId` reveals
without asking.

**Known boundary.** A settlement released by a build older than this one carries no `submittedAt`, so a
404 for it is believed at once. That matters only for a send still in flight across the deploy itself.

The key covers the request BYTES the caller first sent - `requestedPlatformBaseUnits`, the config
id and the disclosure version - so a caller resending a recomputed amount after a rate move gets a
permanent `REQUEST_KEY_IN_USE`. The Next.js invest page therefore persists the exact wire fields
and resends them verbatim (`ardanova-client/src/lib/commerce/AGENTS.md` §Persisted request keys).

# Project money

The balance is the node's; the rows only explain it. `ProjectLedgerService` reports what the
project's custodial treasury account holds as the node reports it, and every entry beneath it is
DERIVED from the row that already recorded the event - the sponsorship (`FundingIntent`), the
award (`TokenAllocation`), the buy-back (`PayoutRequest`) and the treasury receipt
(`PlatformTreasuryTransaction`). Nothing accumulates a running total into a column. A stored
per-project balance is the database-number-as-money this platform rejected: it can drift from the
account it claims to describe, and the account is the one that pays.

Each event has exactly one source of record on the page. The four row sources above are disjoint
by construction, so a single movement of money is never told twice under two names. This is why
`TreasuryTransaction` stays retired rather than being revived as a convenience ledger - see
`Domain/Models/Entities/AGENTS.md`, "Treasury". When a new kind of event needs to appear here,
it gets its existing row as its source, not a new table that re-states one.

Names come from the caller's verified identity, never from anything the client sends. The route
passes the actor id off the verified claim, and the service resolves people from it; a project id
in the path decides only WHICH project, never WHO is asking.

# Operator alerts

`WebhookOperatorAlertService` is an ADDITIVE channel: it posts to `OperatorAlerts:WebhookUrl`
when one is configured and otherwise logs at Critical. A blank URL — from `appsettings.json`
or from an env var, which REPLACES the C# default rather than leaving it — resolves to "no
channel, log Critical" explicitly in code, which is the narrowest setting and the shape a
deploy produces most easily. That resolution is the control; the empty key in
`appsettings.json` is only discoverability and cannot widen anything.

# Opportunity visibility

`OpportunityService` answers every list, search and detail read through `VisibleOpportunities(viewer)`,
which composes `ProjectQueryFilters.VisibleTo` instead of re-deriving it. A role attached to a DRAFT
project is as private as the draft, and a role whose own status is DRAFT is visible only to its poster
and the project's team (creator or members). `GetByIdAsync` stays unscoped because the controller's
management and apply checks need the row itself; any public route uses `GetVisibleByIdAsync`.
`TaskService` does the same for task lists and search (`VisibleTasks`); single-task and per-project
reads are gated in `TasksController` by the project visibility seam.

# Hiring and applications

Both application kinds are a one-step state machine: PENDING moves once, to ACCEPTED, REJECTED or
WITHDRAWN, and every later move is refused with `ApplicationNoLongerPending`. A reviewer may only
accept or reject an opportunity application; withdrawing belongs to the applicant (the project
withdraw route is applicant-only for the same reason). Accepting seats the applicant as a
`ProjectMember` in the same `SaveChangesAsync`, so the status and the membership commit together and
the person hired can be assigned work straight away (task assignment requires membership). A project
application seats a CONTRIBUTOR because its role title is applicant-written; an opportunity seats the
manager-chosen `projectRole`, never FOUNDER. A project application is refused from the creator, an
existing member, or a user who already has a PENDING one.

## Hiring an existing member

An accepted opportunity application must leave the applicant holding the offered role. When the
applicant is already a member ranked _below_ the offer, accepting promotes their existing row in the
same save (one membership per user per project; the reviewer already passed `RefuseRoleOffer`, so the
promotion is never above the reviewer's own rank). When the applicant already holds the offered rank
or a higher one (the creator always ranks as FOUNDER), both `ApplyAsync` and the ACCEPT are refused
with `ApplicantAlreadyHoldsRole` and the application stays PENDING. Before this, the accept returned
ACCEPTED while the member silently kept their old role, and a stale lower-role accept could have
demoted them.

# Project delete

The database restricts deleting a project that still has children, and `ExceptionHandlingMiddleware`
hides that foreign-key error behind a generic 400. `ProjectService.DeleteAsync` therefore asks
`IProjectDeletionBlockers` first and refuses with a 409 (`DeletionBlockedMessage`) naming what is left.
`ProjectsController.Delete` answers with `ProjectDeletionBlockedDto`
(`{ error, code: "PROJECT_HAS_DEPENDENTS", blockedBy: ["Epic", ...] }`).

Which tables block is derived, not listed by hand: `Infrastructure/Repositories/ProjectDeletionBlockers`
takes every _required_ foreign key to `Project` from the EF model (Prisma emits `ON DELETE RESTRICT`
for required relations and `SET NULL` for optional ones) and runs one `EXISTS` per table in a single
`UNION ALL` query. `ProjectDeletionFlowTests` pins that set to the `RESTRICT` constraints parsed from
`ardanova-client/prisma/migrations`, so a new dependent table added through the DBML turns the test red
until it is covered and labelled (`ProjectDeletionBlockerLabels`). Loop 4 review found the earlier
task-or-milestone check missed root-level epics, sprints, features and PBIs (all `RESTRICT`, none needing
a milestone) and every non-plan dependent (members, followers, comments, treasury...), so the opaque 400
was still reachable.

A project with history should be moved to CANCELLED instead. Deleting stays meant for an empty project,
for example the create page rolling back a project whose creation failed halfway.

# Task assignment

`assignedToId` has exactly **two writers** after creation (SR-03 phase 3, Lane K1). Anything else that
assigns a task is a bug; route it through one of these.

1. **`ITaskClaimStore.TryClaimAsync`** (`Infrastructure/Tasks/TaskClaimStore`): anyone taking an open
   task for themselves. `TasksController.Assign` sends a self-assign here, through
   `TaskService.ClaimAsync`, when ANY of these holds:

   ```csharp
   var selfClaim = assigneeId == ActorId
                   && (!canManage || dto.Claim || task.AssignedToId is null);
   ```

   - a non-manager (always; a member has no other way to take work);
   - the one-tap "Pick up" (`AssignTaskDto.Claim = true`, sent by the BFF's `task.claim`), a manager's
     included. The creator always ranks as a manager (`HierarchyAuthorizationService.CanManageProjectAsync`)
     and the feed offers them "Pick up" too; before this, their tap went to `AssignAsync` and silently took
     a task a member had claimed since the card rendered, and the asker never heard;
   - a manager assigning themselves a task that reads as open (still the conditional write, so a claim
     landing between the controller's read and the write is a 409, not an overwrite).

   Only a manager's deliberate self-assign of a task someone holds, without `Claim`, is a takeover and
   stays on `AssignAsync`. The conditional write is ONE statement:

   ```sql
   UPDATE "ProjectTask" SET "assignedToId" = @me, "updatedAt" = @now
   WHERE id = @id AND "assignedToId" IS NULL AND status <> 'COMPLETED' AND "escrowStatus" = 'NONE'
     AND NOT EXISTS (SELECT 1 FROM "TaskCommerceAgreement" WHERE "taskId" = id)
   ```

   The WHERE is `Tasks/TaskClaimRule`'s blocking facts; `TaskClaimStoreAgreementTests` seeds every fact
   row on real Postgres and proves the SQL accepts exactly what the rule calls Claimable or
   PaidNeedsReadiness, so the two cannot drift. Two racing claims both reach the UPDATE; the loser
   waits on the row lock, Postgres re-checks the WHERE against the winner's row, and it writes 0 rows.
   On 0 rows `ClaimAsync` re-reads the task and answers with `TaskClaimRule.Evaluate` as a 409 carrying
   a `TaskClaimCodes` code (`TASK_TAKEN`, `TASK_DONE`, `TASK_UNDER_AGREEMENT`); `TasksController`
   returns `{ error, code }`. A loser that finds the task already its own (a double tap) gets 200.
   Membership and the paid-task readiness check stay in `TasksController.Assign`, before the write.
   The asker's "picked up" notification is staged inside the claim's transaction, so a lost race
   writes none and a won one writes exactly one (§Come-back notifications). The UPDATE bypasses EF's
   change tracker, so `ClaimAsync` clears the tracked rows before it reads the task back.

2. **`TaskService.AssignAsync`**: a manager assigning someone else or no one, a manager's deliberate
   takeover of a held task, and a member releasing their own task. It writes no notification. It refuses
   once an accepted bid's agreement exists or escrow is no longer NONE (409,
   `TASK_UNDER_AGREEMENT`): the agreement names the worker, and swapping the assignee underneath it
   would pay one person for another's work. **This path is still check-then-write** and can
   overwrite a claim that landed between its read and its save. Accepted for now; trigger to
   revisit: the first report of a claim being silently overwritten (then give managers a
   compare-and-set on the assignee they saw).

`TaskCommerceService` sets `assignedToId` only on the task it creates from an accepted bid (creation,
not reassignment).

# Help wanted

A HELP_WANTED post is a person's ask for help on one task (SR-03 phase 3 plan §2). `PostService`
rules, in order (`ResolveAskAsync`):

- `taskId` is required on HELP_WANTED (400) and refused on every other type (400; the database CHECK
  `Post_taskId_help_wanted_only` backs it).
- **Audience is PUBLIC or PROJECT_MEMBERS only** (400 `AskOnAGroup` for any `guildId`, 400 `AskAudience`
  for GUILD_MEMBERS or PRIVATE), on create and on every edit (`UpdateAsync`). Reason: removal below loads
  the ask through the read gate as the manager, and a manager is always the creator or a `ProjectMember`,
  so these two are exactly the audiences every manager can read. A PRIVATE or group ask would be
  invisible to them (404), and the ask would hold its task forever once its author left.
- The task must be on a project the author can see, else 404 (a draft's task reads as missing). The
  post's `projectId`, when sent, must be the task's (400); when omitted it becomes the task's.
- The author must be a member (creator or `ProjectMember`, `IHierarchyAuthorizationService.IsProjectMemberAsync`), else 403.
- A COMPLETED task cannot be asked about (400).
- **One ask per task** is the unique index on `Post.taskId`. A second insert's 23505 is the answer,
  never a 500: the service clears the tracked insert, reads the ask that holds the task and returns a
  409 with code `HELP_WANTED_EXISTS` and `Result.Reference` = that ask's id. `PostsController`
  writes `PostConflictDto` `{ error, code, conflictId }` (the error envelope keeps the extra fields
  and adds `statusCode`, `traceId`); the composer reads `conflictId` to offer "Edit your ask".
- **Removing an ask:** `DELETE /api/posts/{id}` admits the author or a manager of the ask's project
  (`CanManageProjectAsync`), so an ask whose author left cannot hold its task forever. Every other
  type keeps author-only. Deleting any post also deletes its notifications (`StageForgetPostAsync`):
  the database cascades `Notification.postId`, but EF models that FK as client-set-null, so a tracked
  notification would otherwise survive pointing at nothing.

`TaskDto.HelpWanted { postId, askerId, askerName }` and `TaskDto.HasAgreement` are filled by
`TaskService`'s enrichment (one read each per page). Task reads carry no viewer, so `HelpWanted` only
shows an ask whose own audience is everyone (PUBLIC, not retracted); the task itself is already gated
by its project's visibility. `TaskDto.ToClaimFacts()` hands `TaskClaimRule` the facts it reads.

# Come-back notifications

SR-03 phase 3 §4 allows exactly two in-app return loops; nothing else in the backend writes
notifications. Both are **staged** by `NotificationService` (added or updated through the repository,
not saved) so they commit in the caller's own save or transaction, and neither ever notifies the
person who acted.

| Event                                                                 | Writer                                             | Type          | Title                           | Message                           |
| --------------------------------------------------------------------- | -------------------------------------------------- | ------------- | ------------------------------- | --------------------------------- |
| Someone (a manager too) claims a task with a live HELP_WANTED         | `TaskService.ClaimAsync` → `StageAskPickedUpAsync` | TASK_ASSIGNED | `Picked up`                     | `{name} took {task}`              |
| A reply on a post (any type, including a feed card naming the person) | `PostService.AddCommentAsync` → `StageReplyAsync`  | COMMENT_REPLY | `New reply` / `{n} new replies` | `{name}: “{first 80 characters}”` |

- **Only to someone who can still read the post.** Both writers ask the one post read gate
  (`PostReadFilters.ReadableBy`) for the RECIPIENT before staging; a "no" stages nothing. An author
  removed from a draft project gets no reply notification (the bell would otherwise quote a post they
  can no longer open), and an asker who lost the task's project gets no "picked up" (an ask lives on
  its task's project, so the post gate covers the task too).
- **Collapsed per post.** A reply finds the author's one unread COMMENT_REPLY for `(userId, postId)`
  (index `Notification_userId_postId_idx`) and folds into it with ONE conditional statement that
  writes only the collapse fields (count +1 in `data.replyCount`, latest replier and excerpt, and
  `createdAt` moved to now so the bell, newest first, lifts it):

  ```sql
  UPDATE "Notification" SET title, message, data, "createdAt"
  WHERE id = @id AND NOT "isRead" AND data = @foldSeen   -- compare-and-set
  ```

  0 rows means someone got there first, and the reply opens a fresh row instead. A concurrent
  mark-read wins (the old whole-row `Update` rewrote `isRead = false` and resurrected a notification
  the person had just read). Two replies at the same instant both read the same fold; the second's
  compare-and-set misses and it opens its own row, so the counts still add up (two rows, accepted,
  plan §10). Reading a row ends its fold; the next reply opens a new row. The fold writes at once, not
  at the caller's save, so `PostService.AddCommentAsync` wraps the reply and its notification in one
  transaction.

- `data` is JSON (`postId`, `replyCount`, `latestReplierId` / `postId`, `taskId`, `claimerId`);
  `NotificationDto` has no `postId` field, so clients read it from `data`. `actionUrl` is
  `/tasks/{id}` for a claim and `/projects/{slug}` (Feed tab) for a reply on a project post.
- The excerpt collapses whitespace and cuts on whole text elements, never mid-emoji. A person with no
  name on file is `Someone`.
- A manager's assign of someone else, or deliberate takeover, does not notify the asker (only a claim
  "picks up" an ask; a manager's own "Pick up" IS a claim, §Task assignment). Trigger to revisit: an
  asker whose task a manager handed to someone never hears about it.

# Hashtags and follow (Lane L)

**§Project hashtags.** `ProjectService.ResolveHashtagsAsync` (ProjectServices.cs) normalizes/dedupes/caps submitted
hashtags (`Hashtags.MaxPerPost`=10) then classifies them via `ITaxonomyService.ResolveTagsAsync`: SECTOR hits →
`categories` (deduped, first-seen), the first PROJECT_TYPE hit → `projectType` (absent → `TEMPORARY`), everything
else → free `tags`. This only runs when `CreateProjectDto.Hashtags`/`UpdateProjectDto.Hashtags` is non-null — a full
replacement, never a merge with the legacy Categories/ProjectType/Tags fields, which still work unchanged when
Hashtags is omitted. `ProjectHashtagDisplay.From` is the pure reverse for `ProjectDto.Hashtags` (edit-form
round-trip); it does not re-expose free tags (those stay in `Tags` only).

**Carried over (final-review H1).** On `UpdateAsync`, a stored `categories` or `tags` token that `Hashtags.Normalize`
rejects (a legacy custom category such as `Community Garden`, free text with a space) is appended to the resolved
value instead of being dropped (`NotSpellableAsHashtag` + `JoinTokens`). The edit form cannot show such a token as a
hashtag, so the replacement never had a say over it; the form shows it as a fixed chip
(client `components/projects/AGENTS.md` §Edit form). Proof: `ProjectHashtagResolutionFlowTests
.EditingHashtags_CarriesOverACustomCategoryAndFreeTextNoHashtagCanSpell` (real PG).

**§Project follow.** `ProjectFollowService` (new) is the only writer of `ProjectFollow`. Visibility
(`IProjectVisibility`) is checked before every write — a hidden/draft project 404s the same as a missing one.
`FollowAsync` has no exists-then-insert race: the pre-check is a shortcut only, and a genuine `23505` on save is
caught and answered idempotently (proven under real concurrent calls). `UnfollowAsync` is one `ExecuteDeleteAsync`,
not find-then-delete: a double-tap unfollow used to make the second request's tracked DELETE hit 0 rows, throw
`DbUpdateConcurrencyException` and 500; now it deletes nothing and answers success. There is no follower-list route and no read
of another user's follow state — `GET .../follow` only ever answers for the signed actor.

**§Memberships (SR-03 P3 final join).** `GET /api/projects/mine` (`IProjectVisibility.GetMembershipsAsync`) and
`GET /api/guilds/mine` (`IGuildService.GetMembershipsAsync`) list what the signed actor belongs to: created or a
`ProjectMember` row; owned or a `GuildMember` row. That is exactly who `PostsController.ActorMayPostToAsync` lets
write into a project or guild, so the composer's attach picker (client `components/feed/composer/AGENTS.md`) never
offers a subject the post would be refused on. Actor-only, no userId parameter; the project read still composes
`ProjectQueryFilters.VisibleTo`, and an admin reads only their own team. Proof: `MembershipListFlowTests` (real PG).

Slim and bounded (final-review M4): each answers a membership row, not the full DTO - `ProjectMembershipDto` (id,
title, slug, summary, status, categories; `status` lets the composer say a public note on a DRAFT waits for publish)
and `GuildMembershipDto` (id, name, slug) - projected in SQL so the 20k-character text columns are never read. At
most `MembershipLists.Cap` (200) rows: the most recently updated are chosen (`updatedAt` desc, id as tiebreak), then
sorted by title/name for the picker. Trigger to revisit: someone on more than 200 teams cannot attach the rest.

# Project member authority

`ProjectMemberRolePolicy` is the rank rule `ProjectsController` applies after its manager gate
(FOUNDER > LEADER > CORE_CONTRIBUTOR > CONTRIBUTOR > OBSERVER; the project's creator always ranks as
FOUNDER even without a membership row). The manager gate alone let any CORE_CONTRIBUTOR delete the
founder's project, seat anyone as FOUNDER, or raise their own role, so:

- A membership is created only with the member's consent: an invitation they accept or an
  application a manager accepts. The direct `POST members` route is reserved for the creator seating
  their own FOUNDER row during project creation (and admins); `InvitedById` is the signed actor.
- FOUNDER is never grantable by a role change, the founder's row is never changed or removed (admins
  excepted), nobody changes their own role, and a manager acts only on members ranked below them and
  grants only roles below their own.
- Anyone except the founder may remove themself (leave). Deleting the project is founder-only.

`ProjectMemberService.DeleteAsync` unassigns the removed person's unfinished tasks in that project in
the same save, so a former member cannot keep moving work they no longer belong to; completed tasks
keep their assignee as history.

Hiring and invitations are two more ways to give someone a role, so both follow the same rule,
through `ProjectMemberRolePolicy.RefuseRoleOffer`: FOUNDER is never offered, and an actor offers
only roles below their own rank. Before this rule, a CORE_CONTRIBUTOR could post a LEADER role,
accept an application from a sock-puppet account, and then be demoted by the LEADER they had just
seated. `OpportunitiesController` calls `IOpportunityService.RefuseRoleOfferAsync` in three places:
on create, on a role change, and again on ACCEPT. The ACCEPT check uses the **reviewer's** rank, so a
role the founder posted cannot be granted by a lower-ranked co-manager who happens to review it.
`ProjectsController.CreateInvitation` applies the same policy to the invited role. An unknown role
string now returns 400; before, `Enum.Parse` crashed.

A project TEAM_POSITION always records the seat it fills. A position posted without a role is stored
as CONTRIBUTOR, the seat `SeatedRole` would assign anyway. A GUILD_POSITION type on a project is
stored as PROJECT_ROLE. Without these defaults, positions posted from the project's Team Positions
tab had `projectRole = null`, so the tab never showed them.

# Hierarchy read visibility

Reads of epics, sprints, features and product backlog items (by id, by project and by parent) all
call `ProjectVisibilityExtensions.IsVisibleAsync` before returning anything. This is the same
"404 before 403" rule as in `TasksController`: if the viewer cannot see the project, the item (or its
parent) returns the same 404 body as a missing item. An empty 200 list would also reveal that the
hidden project exists. The tRPC `publicProcedure` attaches the signed-in actor, so a draft project's
own team can still read its backlog.

Writes follow the same rule. The epic, sprint and feature write routes, and the creates that load a
parent milestone, epic or sprint, load the item through a `GetVisible*Async` helper in the
controller. A hidden item answers with the missing item's 404 before any ancestry or permission
check runs. `ProjectsController` applies the same rule through `HiddenOrRefusedAsync`. This covers
project update, publish and delete, adding, changing and removing members, and resource writes. A
refusal on a hidden project becomes the missing project's 404, so a 403 only ever confirms a project
the caller can already see.

# Exchange pricing

`ExchangeService` declares ARDA per project unit as `usdPerUnit / usdPerArda` (L2.5, OI-89b +
OI-34). While a project's raise is open its unit price is the OPENING OFFER, `fundingGoal /
investorPartitionSupply`, marked `ardanova:offering-price` (`OfferingRateSource`) so the funding
intent's `declaredRateSource` and terms snapshot say which price the sponsor got. L2.5's text says
`/ totalSupply`; the code divides by the sponsor partition instead because that is what the funding
tab's `describeOfferPerUnit` shows ($15,000 / 60,000 = $0.25) and the only stake the sponsorship
path will sell, so the full offer raises exactly the ask.

`usdPerArda` is the backing ratio `(indexFund + liquidReserve + operations) / ardaCirculatingSupply`
once ARDA has been issued. A treasury with circulating supply below zero, or a ratio of zero or
less, refuses by name. A treasury that has issued nothing (`== 0`, the genesis state after a reset)
has no ratio to take, so it quotes the on-ramp's declared genesis seed through `IArdaGenesisPrice`.
Infrastructure's `OnRampArdaGenesisPrice` reads the same `ArdaOnRampOptions.UnitsPerUsdMinorUnit` the
price-schedule registrar declares to the node (default "100": 100 base units per cent, i.e. 1 ARDA =
$0.01 at 2dp — DECIDED 2026-10-26, mvp-readiness ledger P3-02), and is
only bound when the on-ramp is enabled with an asset. With no declared seed, a genesis treasury
refuses with "Cannot calculate ARDA value: circulating supply is zero" rather than dividing.

**Untrustworthy backing figures (recorded, not fixed here).** The three bucket balances and
`ardaCirculatingSupply` are database doubles and longs on the `PlatformTreasury` singleton. Nothing
in this repo keeps them in step with the node: an on-ramp purchase issues ARDA node-side and leaves
this row at genesis. Wave 5 nulls these same figures out of the treasury status as too
untrustworthy to SHOW, yet they price every rate ArdaNova declares to the node. The genesis-seed
fallback is honest only while the row truly reflects "nothing issued"; once something writes a
non-zero supply against stale balances, the ratio prices sponsorships off figures nobody
reconciles. The control that catches the result is the node-side rate consistency band, OI-79
(launch-readiness L4.6). The real fix is a node-read backing figure, not a better guard here.

# Posts and feed envelopes

`Post` holds two kinds of row: posts a person wrote (`sourceType` NULL) and feed envelopes the projector
writes (`sourceType` set; see `Application/Feed/AGENTS.md`). An envelope names a person as `authorId`
(the update's writer, the member who joined), so every by-author read would otherwise show system cards
as if that person had written them. `PostService` therefore:

- **Excludes envelopes from person-post reads** (`WrittenByAPerson`): project field notes, both by-author
  reads (profiles) and the legacy viewer-less feed. Single-post reads and engagement (like, bookmark,
  share, comment) still resolve envelopes - the feed reuses those tables - through the same
  `LoadVisibleAsync` gate, so a draft project's cards stay as private as its notes.
- **Refuses edit and delete of an envelope** (`MirroredActivityRefusal`, 403) even by its named author:
  an edit could widen a PROJECT_MEMBERS card to PUBLIC, and a delete would be undone by the next cycle.
- **Refuses a reply on a DISCUSSION envelope**: that card deep-links its native thread (metadata
  `thread`), and a reply here would start a second one.
- **Settles a double tap with the unique index (P8-18).** Like and bookmark are check-then-insert; the
  `(postId, userId)` unique indexes (migration `20261027002000_post_engagement_uniqueness`) catch the race,
  and a 23505 on save clears the tracked changes and answers success (the like re-read with the count the
  winning request stored), the same shape as the ballot and guild-review guards. The losing save's counter
  bump rolls back with its insert, so a double tap counts once. Known gap: two DIFFERENT people liking at
  the same instant still write `likesCount` as an absolute `N+1` (both rows land, the counter moves once);
  an atomic `likesCount = likesCount + 1` or a recount is the fix, not done here.
- **Settles a double UNtap too.** Unlike and unbookmark are check-then-delete; when both taps found the row,
  the second DELETE hits zero rows and EF throws `DbUpdateConcurrencyException`. That is the state asked for,
  so it clears the tracked changes and answers success (unlike re-reads the post); the losing save's counter
  decrement rolled back with its delete, so the count drops once. Pinned by
  `FeedProjectorFlowTests.ARacingDoubleUntap_Succeeds_AndLeavesNothingBehind` on real Postgres.

# Taxonomy

`TaxonomyService` (SR-03 Phase 1 Lane C) answers `GET /api/taxonomy` off the `TaxonomyTerm` table
Phase 0 seeded: a shared vocabulary spanning every problem domain the platform hosts (construction
to hospitality), not just tech. It is a suggestion list and filter facet, NOT a stored constraint -
`Project.categories`, `Guild.specialties`, `Opportunity.skills` and `UserSkill.skill` keep storing
plain comma-separated or scalar text, and a custom value under 50 characters still works beside any
taxonomy slug. A `parentSlug` on a SKILL comes from the `Parent` navigation inside the `Select`
projection, never an `Include` - EF Core still emits one LEFT JOIN, so a SKILL's sector arrives as a
slug and never leaks the internal id.

**P8-15 (fixed here).** `ProjectQueryFilters.HasCategory` replaces `categories.Contains(x)`, a
SUBSTRING match that let a filter on `BUSINESS` return `SMALL_BUSINESS` too - worse once the
vocabulary is 26 sectors deep instead of 10. The fix pads both the column and the needle with
commas (`","+categories+","` vs `","+category+","`) before calling `.Contains`, so only a whole
comma-delimited token can match; the concatenation is inlined into the predicate expression (not a
C#-side helper call) specifically so EF Core keeps translating it to SQL instead of pulling every
row into memory to filter client-side.

**Normalization.** `ProjectService.NormalizeCategory` delegates to
`ProjectQueryFilters.CanonicalizeCategory` (`Trim()`, then rewrite to the matching
`ProjectCategory` enum name when one matches case-insensitively) and is the one transform shared by
`ValidateCategories`, both category-write sites (`CreateAsync`, `UpdateAsync`), `HasCategory` and
every filter entry point (`ProjectQueryFilters.Build`, `GetVisibleByCategoryAsync`). Validation
accepted a known code case-insensitively before this fix but storage only trimmed it, so `business`
landed in the column as `business` and the whole-token `BUSINESS` filter — itself case-sensitive —
never matched its own row (fixed alongside P8-15). Canonicalizing in one place means the string
that passed validation, the string the comma-joined column stores, and the needle `HasCategory`
later comma-pads are all byte-for-byte the same value regardless of the caller's casing. A custom
value (not a known code) passes through trimmed only, as typed. `ValidateCategories` also refuses
any single category value containing a comma - the column uses commas as its only delimiter, so an
unrefused one (e.g. `"Solar, Wind"`) would silently become two stored categories.

The actual payout INSERT selects the owner's frozen quote only while expiresAt is later than
clock_timestamp(), the immutable admission deadline remains future, and no request tombstone
exists. This fences expiry at the write itself even when a process pauses after prechecks.
A failed insertion rolls back the balance reservation. Cash-out void is likewise a proxy of
the node's typed void-or-reveal result; a refusal keeps the saved request key.
