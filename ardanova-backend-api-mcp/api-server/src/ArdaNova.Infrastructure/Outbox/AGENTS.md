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
