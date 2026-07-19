---
type: spec
---
# Gated Commerce and AZOA Settlement

## Outcome

ArdaNova is the commercial system of record for project equity, ARDA exchange,
task bids, funding, and payout decisions. AZOA provides the shared-node avatar,
wallet, custody, and idempotent settlement boundary. A user-interface action
never moves value by itself: it creates a durable ArdaNova economic intent,
which is authorized, audited, and dispatched exactly once.

## Product journeys

### Contributor: bid to project-token award

`bid submitted -> bid accepted -> task assigned -> escrow funded -> quest run
created -> work/review -> release approved -> settlement pending -> settled`

- Accepting a bid creates or links a project task, reserves its agreed project
  token award, and provides a `/tasks/{taskId}/commerce` redirect. It does not
  award, unlock, or mint tokens.
- The commerce view shows the agreement, project gate, escrow state, avatar and
  wallet readiness, KYC requirement, quest state, and settlement receipt.
- Release creates one `EconomicSettlement` intent keyed by the escrow release.
  Its outbox calls AZOA with the stable allocation key; timeouts become
  local `AWAITING_RECONCILIATION`, never a new allocation request.
- A contributor award is eligible for exchange only after Gate 1 and a confirmed
  settlement. Investor and founder holdings remain locked until Gate 2.

### Supporter: funding to locked portfolio

`eligibility + disclosures -> checkout -> payment pending -> verified webhook
-> investment ledgered -> settlement pending -> allocated -> portfolio`

The browser never confirms an investment from URL parameters. It reads a
server-owned funding intent and its verified payment/settlement state. Funding
uses a payment-provider event id as its local inbox key and AZOA allocation key.

### Member: wallet and portfolio

The settings/portfolio experience shows separate facts: avatar linked, wallet
bound, KYC/eligibility, project-gate liquidity, and settlement state. Users can
add and verify an external payout wallet/address only through a caller-scoped,
verified flow. Shared-node custody means **no private-key or mnemonic export**;
portfolio, address, receipt, and transaction-history exports are permitted only
when their policy checks succeed.

### Exchange: project token <-> ARDA <-> project token

The exchange always has a quoted two-leg model:

`liquid source project token -> ARDA intermediary -> eligible target project token`

A quote contains fixed-scale source atoms, ARDA atoms, target atoms, rate
snapshots, the gate/liquidity decision, expiry, and a quote id. Accepting a quote
creates one `SwapOrder` and one settlement intent. The server rechecks ownership,
liquidity, gate, quote expiry, and balance at execution; the client never sends a
beneficiary or user id. It is not a free peer-to-peer market or AMM.

## ArdaNova-owned references and durable state

Keep AZOA references in the ArdaNova domain; do not copy keys, mnemonics, or
chain balances.

| Record | Required purpose |
| --- | --- |
| `User.azoaAvatarId`, `azoaWalletId`, `azoaWalletAddress` | Thin shared-node references and cached readiness only. |
| `EconomicSettlement` | One immutable economic event: type, owner/beneficiary, project/task/bid/escrow/payment references, asset refs, fixed-scale amounts, rate/terms snapshot, idempotency key, status, AZOA operation/receipt, failure code, timestamps. |
| `EconomicOutbox` | Transactional dispatch claim for a settlement; unique settlement id, attempt/lease/reconciliation metadata. |
| `FundingIntent` / payment inbox | Disclosure version, eligibility snapshot, payment-provider id, verified webhook state, and link to the settlement. |
| `SwapQuote` / `SwapOrder` | Fixed-scale quote snapshot, expiry, liquidity/gate result, and link to the settlement. |
| `TaskCommerceAgreement` | Bid, task, award, escrow, quest-run, and settlement references; immutable accepted terms. |

Amounts are decimal fixed-scale or integer minor/asset units plus explicit
currency/scale. Floating-point values are forbidden on a money or asset
settlement path. External event ids and semantic idempotency keys are unique in
the database.

`FundingIntent` persists a funder, project/token configuration, disclosure and
eligibility evidence, immutable terms hash/snapshot, provider payment/event
references, and at most one local settlement. `TaskCommerceAgreement` persists
one accepted award per task and bid, immutable accepted terms, a fixed-scale
award, and optional escrow/quest/settlement references. The settlement owns the
single outbox relationship in both cases. Funding's signed-success handler now
writes its immutable local decision and inert outbox row in one transaction,
but it does not dispatch or confirm value. The remaining decision writers and
all runtime activation still require a reviewed additive migration and
deployment evidence.

## Settlement state machine

`Draft -> Authorized -> PendingDispatch -> Submitted -> Confirmed`

Terminal alternatives are `Rejected`, `Cancelled`, and `Failed`. An ambiguous
AZOA/chain outcome enters local `AWAITING_RECONCILIATION`; it is visible as **pending
settlement** and can only be resolved by reconciliation, not a fresh broadcast.
The local business decision and its outbox row commit in one transaction. Network
calls are outside that transaction and are claimed by the outbox lease.

## Authorization and gates

- Every economic mutation derives its actor from authenticated claims. Route,
  body, and query identifiers are references to authorize, never identity.
- Policies distinguish member, project owner, finance operator, independent gate
  approver, and administrator. Gate 2/failure requires evidence, an immutable
  audit record, and independent approval or verifiable community decision.
- KYC/eligibility, disclosure consent, gate state, escrow status, wallet
  readiness, and asset liquidity are separate checks. A success in one never
  implies success in the others.
- `Azoa:Mode=Live` must fail startup/dispatch without a selected-node
  reconciliation capability attestation, scoped tenant key, and operator custody
  readiness evidence. Simulated mode is the only initial integration mode.

## Acceptance criteria

- Cross-user attempts to read balances, submit swaps, create escrows, or change
  another user’s funding/bid data fail closed.
- A duplicate payment webhook, release, refund, or dispatch creates exactly one
  local economic event and one AZOA allocation effect.
- A crashed/ambiguous dispatch reaches local `AWAITING_RECONCILIATION` and no retry
  re-broadcasts it.
- The task commerce view never labels a local `RELEASED` escrow as settled until
  the settlement receipt is confirmed.
- Project-token/ARDA exchange checks holder class and gate liquidity on quote and
  execution; contributor/Gate-1, investor/Gate-2, and founder/Gate-2 rules are
  covered by the same table-driven policy.
- The funding screen exposes pending/confirmed/failed/eligibility states from
  the server, not redirect parameters.
