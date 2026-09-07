---
type: plan
---

# Track — AZOA User Onboarding

> Contract: [`ARDANOVA-AZOA-INTEGRATION-CONTRACT.md`](../../ARDANOVA-AZOA-INTEGRATION-CONTRACT.md) §2–§5.1, §11.
> Dependency root for `azoa-provider-adapter`, `azoa-action-authoring`,
> `treasury-reward-to-azoa-allocation`.
>
> **Reconciled 2026-09-07** against `f2e4bc5` (AZOA avatar→user / quest→action
> cutover). Boxes below now reflect what is on disk, not what was planned.
> Terminology rule and as-built divergences: see [`spec.md`](spec.md).
>
> Historical plan: current transport authority is split across credential-free
> public registration and explicit custody, value, and action keys. The canonical
> integration contract supersedes the single-key tasks below.

## 1. Schema (DBML-first)

- [x] **[P0] Add AZOA reference fields to `User` in DBML**
  - `ardanova-client/prisma/database-architecture.dbml:84-86` — `azoaAvatarId`
    (unique), `azoaWalletId`, `azoaWalletAddress`. Column names predate the
    rename and were deliberately left alone.
  - Regenerate with `npm run generate:prisma` then `npm run generate:csharp`
  - NEVER hand-edit `schema.prisma` or EF entities

## 2. AZOA node client (Infrastructure)

- [x] **[P0] Azoa settings + typed HttpClients**
  - `ArdaNova.Infrastructure/Azoa/AzoaSettings.cs` — `BaseUrl`, scoped keys, trusted `TenantId`, `Mode`
  - Split transports shipped: `AzoaPublicNodeClient` (no key,
    `RegistrationPath = "/api/user/register"`), `AzoaCustodialNodeClient`
    (`QuestPrefix = "/api/action"`), `AzoaQuestNodeClient`, `AzoaNodeClient`
  - Scoped keys come from the secret store and are never committed

## 3. User-linkage service (Application)

- [x] **[P0] `IAzoaAvatarService` + `AzoaAvatarService`**
  - `ArdaNova.Application/Services/{Interfaces,Implementations}/AzoaAvatarService.cs`
  - Idempotent create/resolve; persists `user.azoaAvatarId`
  - Registered in `ArdaNova.Application/DependencyInjection.cs`
  - Type name still says `Avatar`; the surface it calls is `/api/user/*`
- [~] **[P0] Readiness moved** — `IsTier2ReadyAsync` is no longer the gate.
  `IAzoaValueEligibilityService.RequireReadyAsync(userId, action, ct)` is, and it
  fails closed on identity + approved verification + account evidence.
  **That service is uncommitted** — see `conductor/RUNBOOK.md`.

## 4. API + DTOs (Backend)

- [x] **[P0] DTOs** — `ArdaNova.Application/DTOs/AzoaAvatarDtos.cs`
- [x] **[P0] Controller — but retired**
  - `ArdaNova.API/Controllers/AzoaAvatarController.cs` now returns **410 Gone**
    on `ensure` / `wallet` / `status`, pointing callers at
    `api/azoa/custodial-account` (`AzoaCustodialAccountController.cs`).

## 5. Frontend (thin proxy only)

- [x] **[P0] API client wrapper** — shipped as
  `ardanova-client/src/lib/api/ardanova/endpoints/azoa-custodial-account.ts`
  (not the planned `azoa-avatar.ts`)
- [x] **[P0] tRPC router (thin proxy → .NET)** — shipped as
  `ardanova-client/src/server/api/routers/azoa-custodial-account.ts`
- [~] **[P1] Identity/wallet/KYC status on profile or settings page**
  - `ardanova-client/src/app/settings/verification/page.tsx` and
    `src/components/azoa/custodial-account-card.tsx` exist; both are **modified
    and uncommitted** in the security-hardening pass.

## 6. Tests (.NET only for business logic)

- [x] **[P0] `AzoaAvatarServiceTests`**
  - `tests/ArdaNova.Application.Tests/Services/AzoaAvatarServiceTests.cs`
- [~] **[P0] Eligibility tests** —
  `tests/ArdaNova.Application.Tests/Services/AzoaValueEligibilityServiceTests.cs`
  exists but is **untracked**.

## 7. Verification

- [~] **[P0] Build + test sweep (run ONCE at end)**
  - Last recorded green (`f2e4bc5`): `dotnet build` 0 errors, 1050 tests
    (233 API + 817 Application); client `tsc` exit 0, 210 tests / 33 files.
  - ⚠ The client leg is green **only under `npm link`**. `npm ci` cannot
    reproduce it: `package.json` pins `azoa-sdk@^0.3.0`, which is unpublished.
    See `conductor/RUNBOOK.md` → "Open item 1".
