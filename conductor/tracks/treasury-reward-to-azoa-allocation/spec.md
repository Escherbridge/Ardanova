---
type: spec
---

# Treasury / Reward → AZOA Allocation — Technical Specification

## Overview

Wire ArdaNova's **already-decided** value moves — project funding settlement and
task/bounty rewards — to the AZOA node's out-of-band value door,
`POST /api/allocation/{avatarId}`, with **stable idempotency keys** so an asset
moves exactly once. ArdaNova decides *how much* of *which* asset (economics stay
consumer-side, §1); AZOA performs the exactly-once, KYC-gated move (§6).

> Source contract: [`conductor/ARDANOVA-AZOA-INTEGRATION-CONTRACT.md`](../../ARDANOVA-AZOA-INTEGRATION-CONTRACT.md) §6, §7.

## ⚠️ Ordering constraint (real value)

AZOA-side `quest-reconcile-retry-wiring` (**P7**, the double-mint reconcile clause,
§7) is shipped on the reference node. Before enabling real value on another
deployment, verify its reconcile endpoints and recovery tests. Local and CI runs
remain on `Blockchain:Mode=Simulated` (deterministic `sim:` ids).

## Dependencies

- `azoa-avatar-onboarding` — avatar + wallet to receive the asset.
- `azoa-provider-adapter` — shares the `AzoaNodeClient` / transfer-mint plumbing.
- Track 09 (Tokenomics) — treasury split, equity math, funding-gate *valuation*,
  payout decisions (these PRODUCE the already-decided amounts; AZOA never computes
  them).
- Track 04/20 (Finance/Escrow) — task/bounty escrow records of truth stay in
  ArdaNova; release/refund decisions drive the allocation call.

## The allocation door (§6)

`POST /api/allocation/{avatarId}`:
- `{avatarId}` = the receiving AZOA avatar — **route only, IDOR-safe** (never a
  redirectable body field; cross-owner → 404).
- Headers: `X-Api-Key` (must hold `nft:mint`/`wallet:manage`),
  `Idempotency-Key` = a **stable per-event** key.
- Body: `kind: "Mint"` (`name`/`assetId`/`metadata`) or `kind: "Transfer"`
  (`assetRecordId`); `amount` is an **opaque string** (AZOA derives no economics).
- `200` → `walletId`, `walletAddress`, `walletProvisioned`, `operationId`,
  `replayed`. Same key again → original result, `replayed: true`, no second move.
- `403` if scope missing **or** target KYC ≠ `APPROVED` (`KYC_FORBIDDEN:` prefix,
  fail-closed); `429` financial rate-limit.

## Idempotency key strategy (the load-bearing detail)

Keys MUST be **stable and deterministic per economic event** — never random,
never timestamped:
- **Task/bounty reward** → `reward:{taskId}` (or `reward:{taskId}:{escrowReleaseId}`
  if a task can pay multiple times).
- **Funding settlement** → the payment provider's stable id (e.g. Stripe
  PaymentIntent id) so a webhook redelivery reuses the same key.
- **Refund branch** → `refund:{escrowId}`.

Idempotency is partitioned by API key on the node (`alloc:<apiKeyId>:<your-key>`);
absent header ⇒ node uses a deterministic content key — but ArdaNova MUST always
send an explicit stable key.

## Reconcile obligations (§7)

ArdaNova's consumer obligations:
1. Send a stable `Idempotency-Key` on **every** allocation (above).
2. Record an ambiguous outcome as local
   `EconomicSettlementStatus.AWAITING_RECONCILIATION`, render it as
   **non-terminal, non-error** "pending settlement," and **do not re-trigger**.
3. Never re-POST an allocation on a timeout/ambiguous error — rely on
   idempotency + the node's reconcile-before-retry.

## Trigger points (ArdaNova → allocation)

| Event | Source (records of truth) | Allocation call |
|---|---|---|
| Funding settled (fiat) | Tokenomics/Stripe webhook (consumer verifies signature, §1) | `kind: Mint`/`Transfer` of ProjectShare/decided asset, key = PaymentIntent id |
| Task/bounty accepted → escrow RELEASED | Escrow record (consumer decides amount) | `kind: Transfer`, key = `reward:{taskId}` |
| Task rejected → escrow REFUNDED | Escrow record | refund path, key = `refund:{escrowId}` |

> Webhook signature verification stays ArdaNova's job (§1, §3). AZOA holds no
> payment-provider secret.

## Backend-first

Economics + records of truth are .NET (tokenomics/escrow services produce the
decided amount, then call the AZOA allocation client). Frontend only reflects
status (e.g. "reward sent / pending settlement") via a thin proxy.

## Out of scope

- The reconcile wiring itself (AZOA P7) — consumed, not built here.
- In-DAG value moves via quest Tier-2 nodes (`azoa-quest-authoring`); this is the
  out-of-band door. (Both are exactly-once + KYC-gated, §6.)
- Treasury split / equity math (Track 09 — produces the amounts).

## Acceptance criteria

- Funding settlement and task reward each call `POST /api/allocation/{avatarId}`
  with a **stable** idempotency key; a redelivered event reuses the same key and
  the node returns `replayed: true` (no second move).
- `avatarId` is taken from the resolved avatar reference (route value), never a
  body field; cross-owner target → 404.
- `amount` is passed as an opaque string; no economic computation happens on the
  AZOA side.
- A `403 KYC_FORBIDDEN` surfaces as an actionable "must be KYC-approved" message;
  fail-closed (no value moves).
- Local `EconomicSettlementStatus.AWAITING_RECONCILIATION` is rendered as
  "pending settlement" and never triggers a re-POST.
- Real-value execution is flag-gated until the chosen node verifies shipped P7
  reconciliation plus its custody and fee-funding deploy checks.
