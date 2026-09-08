---
type: plan
---
# Track — Treasury / Reward → AZOA Allocation

> Contract: [`ARDANOVA-AZOA-INTEGRATION-CONTRACT.md`](../../ARDANOVA-AZOA-INTEGRATION-CONTRACT.md) §6, §7.
> Depends on `azoa-user-onboarding` + `azoa-provider-adapter`.
> P7 (`action-reconcile-retry-wiring`) is shipped in the reference implementation.
> Gate real value on deployment verification; keep local/CI on `Blockchain:Mode=Simulated`.
> **D47 (2026-09-07): funding and rewards are `IssueUnits` from the platform reserve, never a
> per-reward `Mint`.** A stake is one real per-project ASA minted once into the reserve at
> project creation (`azoa-provider-adapter`'s `CreateFungibleASAAsync`); every allocation this
> track dispatches after that moves already-issued units out of the reserve. Section 3's
> `AllocateAsync (Mint/Transfer)` reference below describes the AS-BUILT client surface —
> `AllocationKind.Mint`-per-reward is the pattern D47 rejects on both callers (funding and task
> reward); the corrected call is `AllocateAsync(..., kind: Transfer, ...)` moving units out of
> the platform reserve, never `kind: Mint` increasing supply per event.

## 1. Allocation client (Infrastructure/Application)

- [x] **[P0] `IAzoaAllocationService` + implementation**
    - `ArdaNova.Application/Services/Interfaces/IAzoaAllocationService.cs`
    - `ArdaNova.Application/Services/Implementations/AzoaAllocationService.cs`
    - `AllocateAsync(avatarId, kind, amount, idempotencyKey, ...)` via `AzoaNodeClient`
      (parameter name still `avatarId`; the AZOA route segment is now `{userId}`)
    - `POST /api/allocation/{userId}` — the id is a route value only (IDOR-safe)
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
    - Hook the ARDA on-ramp settlement (AZOA `Fiat.USD` rail, D45), not a project-scoped Stripe
      webhook — funding a project is spend-ARDA-for-project-token, never fiat-for-project-token
    - Decide asset + amount in ArdaNova, then call `AllocateAsync` kind=Transfer — `IssueUnits`
      from the platform reserve (D47); `kind=Mint` per funding event is rejected
- [ ] **[P0] Task/bounty reward → allocation**
    - On escrow RELEASED: `AllocateAsync` kind=Transfer, key=`reward:{taskId}` — `IssueUnits`
      from the reserve's contributor partition, never a per-reward `Mint` (D47)
- [ ] **[P0] Refund branch → allocation**
    - On escrow REFUNDED: refund path, key=`refund:{escrowId}`

## 4. Reconcile obligations (consumer side)

- [ ] **[P0] Treat local `AWAITING_RECONCILIATION` as non-terminal "pending settlement"**
    - No re-POST on timeout/ambiguous error; rely on idempotency + node reconcile
- [ ] **[P0] Real-value feature gate**
    - Default `Simulated`; flip to live only after the chosen node verifies P7,
      custody, and fee funding

## 5. API + frontend (thin proxy)

- [ ] **[P1] Status surfacing** — "reward sent / pending settlement / KYC required"
    - thin tRPC proxy → .NET; no economics in the frontend

## 6. Tests (.NET)

- [x] **[P0] `AzoaAllocationServiceTests`**
    - `tests/ArdaNova.Application.Tests/Services/AzoaAllocationServiceTests.cs`
    - Stable key built per event; redelivery reuses key → `replayed:true`, no second move (mock node)
    - the AZOA user id comes from the route, never the body; cross-owner → 404
    - amount opaque string; no economic computation on AZOA side
    - `403 KYC_FORBIDDEN` → fail-closed
    - local `AWAITING_RECONCILIATION` never triggers re-POST

## 7. Verification

- [ ] **[P0] Build + test sweep (run ONCE at end)**
    - `dotnet build` + `dotnet test`; `npm run build`
    - Manual (Simulated): settle funding → allocation `replayed` on redelivery;
      release task escrow → reward allocation with `reward:{taskId}` key
