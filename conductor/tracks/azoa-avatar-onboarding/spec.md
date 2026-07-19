---
type: spec
---

# AZOA Avatar Onboarding — Technical Specification

> Historical track: credential and custody details are superseded by the
> canonical integration contract and the tenant custodial-account flow. Public
> self-registration is credential-free; managed onboarding uses a dedicated
> custody key and a trusted deploy-time tenant binding.

## Overview

Link every ArdaNova user to a **self-sovereign AZOA avatar** on the shared/managed
AZOA node, and provide the wallet-bound readiness check that every value-bearing
(Tier-2) operation depends on. This is the dependency root for the other three
AZOA tracks — adapter, quest-authoring, and allocation all assume an avatar
exists and (for value moves) has a wallet bound.

Per the locked contract decisions (§11), ArdaNova:

- integrates a **shared/managed AZOA node** — the node operator custodies keys;
  ArdaNova owns **no** KMS/B3 or fee-funding/P3 concern;
- uses **self-register + self-run** avatars — **no fleet map, no `tenant:provision`,
  no acting-as/consent-delegation path**. §4.1 of the contract does NOT apply.

> Source contract: [`conductor/ARDANOVA-AZOA-INTEGRATION-CONTRACT.md`](../../ARDANOVA-AZOA-INTEGRATION-CONTRACT.md) §2, §3, §4, §5.1.

## Dependencies

- **AZOA shared node** — endpoint URL and capability-specific credentials available.
- Track 01 (Auth) — `User` identity, NextAuth session, JWT claims.
- Track 08 (KYC) — `verificationLevel` / KYC status, consumed by the value-gate
  (the gate itself lives on the AZOA node; ArdaNova only reads/passes status).

## Architecture

### Where the link lives (consumer side)

ArdaNova stores a **thin reference** from its `User` to the AZOA avatar — it does
NOT store keys, balances, or wallet secrets (those live on the node). The
reference is the minimum needed to address the avatar on subsequent API calls.

| Field                                | Owner                           | Purpose                                                          |
| ------------------------------------ | ------------------------------- | ---------------------------------------------------------------- |
| `azoaAvatarId`                       | ArdaNova `User` (new)           | The avatar's id on the AZOA node. Null until linked.             |
| `azoaWalletId` / `azoaWalletAddress` | ArdaNova `User` (new, nullable) | Cached wallet reference once bound. Chain stays source of truth. |
| keys / mnemonic                      | **AZOA node only**              | Never stored or seen by ArdaNova.                                |

> DBML-first: add the `User` fields in
> `ardanova-client/prisma/database-architecture.dbml`, then
> `npm run generate:prisma` + `npm run generate:csharp`. Never edit
> `schema.prisma` or EF entities by hand.

### Onboarding flow (self-sovereign)

1. **Avatar create** — on first need (sign-up completion or first blockchain
   touchpoint), ArdaNova calls the AZOA node as a self-registering consumer to
   create/resolve an avatar **for the calling user as itself**. No
   `tenant:provision`; the user is its own avatar.
2. **Persist reference** — store `azoaAvatarId` on the `User`. Idempotent: a
   second call returns the existing avatar, never a duplicate.
3. **Wallet generate (pre-KYC OK)** — `POST /api/wallet/generate` may be called
   without KYC (contract §11.4). Cache `azoaWalletId` / `azoaWalletAddress`.
4. **Wallet-bound check before Tier-2** — before any value-bearing call, assert
   the avatar has a wallet bound. This is the consumer-side mirror of AZOA's
   `ChainCapabilityGate` (fail-closed): no wallet → block the action with a
   clear "wallet not ready" error, never a silent no-op.

### KYC posture (read-only on consumer)

ArdaNova does NOT enforce the KYC value-gate — the AZOA node does, fail-closed,
returning `KYC_FORBIDDEN:` → 403 on value seams (§6, §11.4). ArdaNova's job is to
(a) surface KYC status in the UI so a user knows they must be `APPROVED` to
transact, and (b) translate a `403 KYC_FORBIDDEN` from the node into an
actionable client message. Wallet creation stays ungated.

## Backend (.NET) responsibilities

- `IAzoaAvatarService` + implementation in `ArdaNova.Application/Services/`:
  - `EnsureAvatarAsync(userId)` → idempotent create/resolve, persists `azoaAvatarId`.
  - `EnsureWalletAsync(userId)` → calls AZOA wallet generate, caches refs.
  - `IsTier2ReadyAsync(userId)` → wallet-bound check (does NOT check KYC; that's
    the node's gate, surfaced via 403 at call time).
- AZOA node clients: credential-free public registration plus separate custody,
  value, and quest transports in `ArdaNova.Infrastructure/Azoa/`.
- Config: `Azoa:BaseUrl`, `Azoa:CustodyApiKey`, `Azoa:ValueApiKey`,
  `Azoa:QuestApiKey`, trusted `Azoa:TenantId`, and `Azoa:Mode`
  (Live | Simulated) — Simulated still available for dev even though the node
  is live.

## Frontend (Next.js) responsibilities

- tRPC router `azoaAvatar` is a **thin proxy** → `apiClient` → .NET (no Prisma,
  no business logic), per the architecture rule.
- Surface avatar/wallet status + KYC status on the profile/settings page so a
  user can see "wallet ready / KYC required to transact."

## Out of scope (explicitly)

- Fleet mapping, `tenant:provision`, acting-as / consent-delegation (locked out
  by §11.2).
- Key custody, KMS, fee-funding (node operator's concern, §2).
- The provider-adapter swap (`azoa-provider-adapter` track).
- Quest definitions (`azoa-quest-authoring` track).

## Acceptance criteria

- A new user can be linked to exactly one AZOA avatar; re-running the link is a
  no-op (idempotent), no duplicate avatar.
- A wallet can be generated for a user pre-KYC and its reference cached.
- `IsTier2ReadyAsync` returns false with a clear reason when no wallet is bound,
  true once bound.
- No keys/mnemonic/balances are persisted on the ArdaNova side.
- A node `403 KYC_FORBIDDEN` surfaces to the client as an actionable message.
