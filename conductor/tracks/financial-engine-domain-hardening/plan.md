---
type: plan
---
# Track — Financial Engine Domain Hardening

> Depends on [Gated Commerce and AZOA Settlement](../gated-commerce-and-azoa-settlement/spec.md).
> This is an ArdaNova-owned domain track. It does not authorize value movement.

## 1. Contract and model inventory [P0]

- [ ] Map every current project token, equity, investment, funding, task award,
  quote, order, and settlement field to a canonical asset/policy/eligibility
  owner. Reject ambiguous or implicit mappings.
- [ ] Specify DBML-first `AssetDefinition`, `ProjectTokenPolicy`,
  `EquityOrRedemptionRightPolicy`, and `EligibilityDecision` with immutable ids,
  versioning/effective windows, actor/audit fields, and foreign-key/index plan.
- [ ] Document which current `ProjectTokenConfig`, `ProjectEquity`, and funding
  fields become compatibility projections, and the deletion criteria for each.

## 2. Fixed-scale migration [P0]

- [ ] Replace funding/project-token `double` values on value paths with
  `FixedScaleAmount` or canonical base-unit strings and explicit scales 0--18.
- [ ] Create and review a non-production additive Prisma migration: nullable
  columns first, authoritative manifest backfill, readback/reconciliation, then
  no-default `NOT NULL` hardening. Never use `prisma db push` on a shared DB.
- [ ] Add a migration preflight fingerprint and rollback evidence that prove no
  historical asset or agreement changed its semantic amount.

## 3. Immutable authorization snapshots [P0]

- [ ] Make funding-intent creation atomically pin the relevant asset definition,
  utility/rights policy, eligibility decision, disclosure version, and terms
  hash. Same idempotency key must replay only identical immutable facts.
- [ ] Make task-agreement acceptance pin the utility-asset/policy and award
  amount. A separate equity/right award requires the corresponding policy and
  eligibility decision; it must not reuse the utility path.
- [ ] Make quote/order creation pin both project assets, ARDA intermediary,
  rates, liquidity/gate result, expiry, and all policy versions before execution.

## 4. API and UI contracts [P1]

- [ ] Return server-owned readiness facts separately: wallet proof, KYC,
  eligibility decision, utility liquidity, rights status, payment verification,
  and settlement state. Do not collapse them into an "eligible" flag.
- [ ] Ensure investment, portfolio, task-commerce, and swap UI copy clearly
  distinguishes utility tokens from ownership/redemption rights and never
  labels payment redirects or pending settlements as confirmed.

## 5. Verification [P0]

- [ ] Unit tests: scale boundaries, canonical asset identity, policy separation,
  eligibility expiry, immutable replay, and rejected mixed-version snapshots.
- [ ] Integration tests: duplicate and concurrent funding/task/quote
  authorization, additive migration/backfill/readback, and policy successor
  behavior with historic facts preserved.
- [ ] End-to-end simulation: eligible funding -> pending settlement -> confirmed
  utility portfolio; accepted task -> agreement -> pending reward; quote ->
  order -> pending reconciliation. Keep real dispatch disabled.

## Launch gate

No item in this track opens checkout, provider dispatch, AZOA allocation,
payout, exchange execution, or task reward. Those paths stay fail-closed until
the existing gated-commerce track has reviewed migrations, selected-node
attestation, durable reconciliation, live concurrency evidence, and full
end-to-end acceptance evidence.
