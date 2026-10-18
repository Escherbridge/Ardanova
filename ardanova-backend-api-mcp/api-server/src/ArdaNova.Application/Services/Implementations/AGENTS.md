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

# Cash out

Cashing out is TWO steps, and the split is deliberate. A sale is a node exchange run
under a key derived from the `PayoutRequest` row id - which is why the row is written
*before* the node is called, so a request that dies mid-flight still leaves something
reconciliation can name. A withdrawal is a node-owned cash-out that ArdaNova READS BACK
rather than mirrors: duplicating it would make two records of one movement of real money,
the ledger-of-record shape D47 rejected for stake.

Units move only against a TERMINAL receipt. A timeout or a 409 is AMBIGUOUS - the
exchange may already have run - so the row parks with the units still LOCKED. Returning
them on a maybe is how one holding gets sold twice. Only an outright refusal (NotFound,
ValidationError, BadRequest, Forbidden, Unauthorized) unlocks, because those mean the node
read the request, said no, and created nothing.

A parked row is chased by an operator through `GET /api/Payouts/pending` (widened to
include PROCESSING) and `POST /api/Payouts/{id}/process`, which is a READ of the receipt
under the original key and can only ever observe the first submission. The person-facing
copy says exactly that: nothing on a timer finishes a parked sale.

The node's own refusal text NEVER reaches a person. `AzoaNodeClient` flattens a refusal to
sentences that name the node, so both `PayoutService.DescribeRefusal` and
`AzoaCashOutClient.Refused` re-word it from the refusal KIND. The node's text stays where
operators read it - the transport log.

A holding with no recorded `holderClass` is LISTED but never sellable. Every sellability
rule is keyed on the class, and defaulting one would decide a sale on an invented fact;
hiding the holding entirely would be worse than saying why it cannot be sold.

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

# Task assignment

`TaskService.AssignAsync` is the only write of `assignedToId` after creation. It refuses once an
accepted bid's agreement exists or escrow is no longer NONE: the agreement names the worker, and
swapping the assignee underneath it would pay one person for another's work.

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
