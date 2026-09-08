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
