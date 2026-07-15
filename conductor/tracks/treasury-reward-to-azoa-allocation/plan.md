---
type: plan
---
# Track — Treasury / Reward → AZOA Allocation

> Contract: [`ARDANOVA-AZOA-INTEGRATION-CONTRACT.md`](../../ARDANOVA-AZOA-INTEGRATION-CONTRACT.md) §6, §7.
> Depends on `azoa-avatar-onboarding` + `azoa-provider-adapter`.
> ⚠️ Real-value flip gated on AZOA-side P7 (`quest-reconcile-retry-wiring`).
> Build + test on `Blockchain:Mode=Simulated` until then.

## 1. Allocation client (Infrastructure/Application)

- [x] **[P0] `IAzoaAllocationService` + implementation**
    - `ArdaNova.Application/Services/Interfaces/IAzoaAllocationService.cs`
    - `ArdaNova.Application/Services/Implementations/AzoaAllocationService.cs`
    - `AllocateAsync(avatarId, kind, amount, idempotencyKey, ...)` via `AzoaNodeClient`
    - `POST /api/allocation/{avatarId}` — avatarId from route value only (IDOR-safe)
    - `amount` passed as opaque string; map `200`→result (incl. `replayed`),
      `403 KYC_FORBIDDEN`→fail-closed error, `429`→rate-limit error
    - Register in `ArdaNova.Application/DependencyInjection.cs`

## 2. Idempotency key builder

- [x] **[P0] Stable per-event key helper**
    - `reward:{taskId}` (or `reward:{taskId}:{escrowReleaseId}`)
    - funding settlement → Stripe PaymentIntent id
    - `refund:{escrowId}`
    - NEVER random / timestamped

## 3. Trigger wiring (economics decided FIRST)

> **Current truth:** the allocation client and its unit tests exist, but no
> production funding, escrow, or refund trigger dispatches it yet. The durable
> inbox/outbox and user-facing settlement state now live in
> `gated-commerce-and-azoa-settlement`; this track remains in progress.

- [ ] **[P0] Funding settlement → allocation**
    - Hook tokenomics/Stripe settlement (consumer verifies webhook signature)
    - Decide asset + amount in ArdaNova, then call `AllocateAsync` (Mint/Transfer)
- [ ] **[P0] Task/bounty reward → allocation**
    - On escrow RELEASED: `AllocateAsync` kind=Transfer, key=`reward:{taskId}`
- [ ] **[P0] Refund branch → allocation**
    - On escrow REFUNDED: refund path, key=`refund:{escrowId}`

## 4. Reconcile obligations (consumer side)

- [ ] **[P0] Treat `AwaitingReconciliation` as non-terminal "pending settlement"**
    - No re-POST on timeout/ambiguous error; rely on idempotency + node reconcile
- [ ] **[P0] Real-value feature gate**
    - Default `Simulated`; flip to live only after AZOA P7 confirmed

## 5. API + frontend (thin proxy)

- [ ] **[P1] Status surfacing** — "reward sent / pending settlement / KYC required"
    - thin tRPC proxy → .NET; no economics in the frontend

## 6. Tests (.NET)

- [ ] **[P0] `AzoaAllocationServiceTests`**
    - `tests/ArdaNova.Application.Tests/Services/AzoaAllocationServiceTests.cs`
    - Stable key built per event; redelivery reuses key → `replayed:true`, no second move (mock node)
    - avatarId from route, never body; cross-owner → 404
    - amount opaque string; no economic computation on AZOA side
    - `403 KYC_FORBIDDEN` → fail-closed
    - `AwaitingReconciliation` never triggers re-POST

## 7. Verification

- [ ] **[P0] Build + test sweep (run ONCE at end)**
    - `dotnet build` + `dotnet test`; `npm run build`
    - Manual (Simulated): settle funding → allocation `replayed` on redelivery;
      release task escrow → reward allocation with `reward:{taskId}` key
