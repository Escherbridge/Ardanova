# Economic outbox persistence

`EconomicOutboxLeaseStore` uses EF conditional updates as the single-winner
claim and lease-finalization primitive. A dispatch claim requires its linked
settlement to remain `PENDING_DISPATCH`; a reconciliation claim requires
`AWAITING_RECONCILIATION`. Finalization guards the leased outbox, expected
settlement state, and settlement version in the same local transaction. It is
intentionally a persistence seam: it records only pending, submitted, or
reconciliation-required state, never an allocation, payout, or confirmed
settlement. The runtime gateway is disabled by default; a future transport must
preserve the stable idempotency key, canonical base-unit amount, and lease check
before enabling hosted execution.
