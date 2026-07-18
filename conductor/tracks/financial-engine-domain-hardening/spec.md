---
type: spec
---
# Financial Engine Domain Hardening

## Outcome

ArdaNova has one immutable, consumer-owned financial vocabulary for its dual
project model: a project utility token and separately governed equity or
redemption rights. It records the eligibility decision that authorized a
funding or award decision and identifies every asset by its canonical chain
reference and scale. AZOA remains the custody, idempotency, and settlement
orchestration boundary; it receives already-decided asset references and
base-unit amounts only.

This track is a prerequisite for the gated-commerce journeys. It does not
activate checkout, payouts, token exchange, quest rewards, or AZOA dispatch.

## Domain contract

| Record | Immutable facts | Explicitly not implied |
| --- | --- | --- |
| `AssetDefinition` | stable id, canonical chain/network or fiat namespace identity, asset id, symbol, display name, and scale 0--18 | a price, balance, liquidity, or right to redeem |
| `ProjectTokenPolicy` | utility purpose, issuance/allocation rules, gate/liquidity policy, and referenced project asset | equity, revenue share, ownership, or guaranteed return |
| `EquityOrRedemptionRightPolicy` | the distinct right class, jurisdiction/disclosure version, eligibility policy, cap/vesting or redemption terms, and effective period | custody, payment confirmation, or token liquidity |
| `EligibilityDecision` | subject, policy version, evidence references/digests, decision, decider, expiry/review time, and recorded reason code | an investment, allocation, KYC approval for another purpose, or a settlement receipt |

`AssetDefinition` is append-only after use by a funding intent, task commerce
agreement, quote, order, or settlement. Corrections create a successor with a
new stable id; historic records retain their original reference. An asset's
scale is an explicit integer in the inclusive range 0--18. No default scale,
floating-point asset amount, or inferred chain identity is permitted.

## Boundaries and invariants

- Project-token utility, equity/redemption rights, eligibility, payment
  verification, gate liquidity, and AZOA settlement are separate facts. Passing
  one check does not pass another.
- A funding intent snapshots distinct payment and award asset definitions; a task
  agreement snapshots its exact award asset definition. Both persist the exact
  policy version, and eligibility decision used when it was authorized. Later
  policy changes cannot rewrite that evidence.
- A settlement with more than one asset uses ordered immutable settlement legs;
  its legacy single amount is never used to collapse payment and award facts.
- Fixed-scale base units are the sole amount representation across new
  financial-engine contracts. UI decimal input is parsed at its named asset
  scale and rejected when it would round, overflow, or become non-positive.
- A project-token/ARDA quote identifies both project assets and the ARDA asset
  explicitly. Its two legs are utility-asset -> ARDA -> utility-asset; it may
  not be represented as an equity-right conversion.
- Equity/redemption decisions require their own policy and eligibility evidence.
  A utility-token holding, avatar link, task completion, payment redirect, or
  project gate alone never grants such a right.
- The consumer is the system of record for all vocabulary in this track. AZOA
  receives opaque identifiers and already-decided amounts, never valuation,
  rights classification, or eligibility calculation.

## Integration points

This track extends, but does not duplicate,
[`gated-commerce-and-azoa-settlement`](../gated-commerce-and-azoa-settlement/spec.md):

- `FundingIntent` stores distinct payment/award asset references, a canonical
  award base-unit string, utility-policy and/or rights-policy references, plus
  the applicable `EligibilityDecision` snapshot.
- `EconomicSettlementLeg`, `TaskCommerceAgreement`, `SwapQuote`, and `SwapOrder`
  use canonical asset ids and fixed-scale base units. A quote has exactly three
  explicit legs: project utility source, ARDA intermediary, and project utility
  target. Task awards default to a project utility-token policy; any separate
  ownership/right award needs its explicit policy and decision.
- The existing staged asset-scale migration remains the only permissible
  database cutover path. Do not apply a default or use `prisma db push` against
  a shared environment.

## Acceptance criteria

- Every new value-bearing record references an immutable asset definition with
  a canonical chain/network asset id and scale 0--18.
- Project utility and equity/redemption policy are distinct types, versioned,
  and cannot be silently converted into one another.
- Eligibility is a durable, auditable decision snapshot rather than a boolean
  copied from request input.
- Legacy `double` funding values have a reviewed, additive, reversible
  fixed-scale migration and have no remaining value-path readers or writers.
- Concurrent authorization and policy updates cannot produce a funding intent
  or agreement with mixed asset/policy/eligibility versions.
- Checkout, dispatch, allocation, payout, exchange, and task reward remain
  disabled until the gated-commerce track's migration, node-attestation,
  reconciliation, and end-to-end evidence gates are met.
