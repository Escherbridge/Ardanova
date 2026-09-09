# Economic outbox persistence

`EconomicOutboxLeaseStore` uses EF conditional updates as the single-winner
claim, renewal, and lease-finalization primitive. A dispatch claim requires its linked
settlement to remain `PENDING_DISPATCH`; a reconciliation claim requires
`AWAITING_RECONCILIATION`. Renewal and finalization guard the leased outbox,
expected settlement state, and settlement version; finalization does so in one
local transaction. It is
intentionally a persistence seam: it records only pending, submitted, or
reconciliation-required state, never an allocation, payout, or confirmed
settlement. The runtime gateway is disabled by default; a future transport must
preserve the stable idempotency key, canonical base-unit amount, and lease check
before enabling hosted execution. Gateway calls use a linked deadline shorter
than the renewed lease; a local deadline enters reconciliation while caller
cancellation still propagates. The hosted loop is registered only when
`Azoa:EnableSettlementOutboxWorker=true`; each cycle is batch-bounded and uses a
fresh dependency-injection scope. Funding readiness requires both that actual
registration and a selected gateway attesting the complete canonical funding
contract.

# Timed chasers

Two hosted services in this directory exist so an operator's read is a FALLBACK rather than
the only path a stuck row can move.

`SettlementPendingPromoterService` promotes settlement rows that fell pending (OI-88b).
`ParkedPayoutReconcilerService` chases parked unit sales under the ORIGINAL key, which is a
read of a receipt and can only ever observe the first submission (OI-86b) — see
`Application/Services/Implementations/AGENTS.md`, "Cash out", for why a parked row keeps its
units LOCKED.

Both register their options singleton ALWAYS and their hosted service unless their own flag
is explicitly `false`, so a blank or missing setting resolves to "the chaser runs" rather than
to silence. Both are registered from `Infrastructure/DependencyInjection.cs`; without those
two lines the services compile, are tested, and never run — which is the recorded defect
class in RUNBOOK.md §2, a claim in prose that is not a control in code.

# Dispatch ceiling

`EconomicOutboxDispatchService` bounds one settlement's attempts with a single counter
(`_maximumAttempts`, `DefaultMaximumAttempts = 10`, overridable at construction), but the
counter ends in TWO OPPOSITE outcomes depending on which claim kind ran out, because a
refusal and an unreadable result are not the same fact:

- **`DispatchCeiling`** fires only after every DISPATCH attempt was refused in a way the
  gateway itself called safely retryable — its own word that no value moved. It is
  therefore the one refusal this seam may make TERMINAL:
  `EconomicOutboxLeaseStore.FinalizeAsync`'s `isDispatchCeiling` check drives both the
  outbox row and the settlement to `FAILED`, a Critical operator alert names the reserved
  amount that must be returned by hand, and the row never retries again.
- **`ReconciliationCeiling`** fires when the node's OUTCOME is still unreadable after
  every RECONCILIATION attempt. It does the opposite on purpose: the settlement stays
  `AWAITING_RECONCILIATION` (an `Unknown` outcome, never `FAILED`), because value MAY
  already have moved and closing the row would hide that fact from whoever looks next.
  An operator must reconcile it against the node by hand before anyone is told it
  succeeded or failed.

This is a different ceiling from the READINESS holding window (`ReadinessRefundDue`),
which ends a settlement that never became dispatchable at all — see
`Application/Services/Implementations/AGENTS.md`, "Reward dispatch hygiene", for why a
readiness refusal must never be spent against this transport ceiling. All three paths —
dispatch, reconciliation, and readiness — alert through the same
`EconomicOperatorAlertCodes` / `OperatorAlertService` seam rather than a bespoke channel
per ceiling, so an operator has one place to watch, not three.