# Track — AZOA Avatar Onboarding

> Contract: [`ARDANOVA-AZOA-INTEGRATION-CONTRACT.md`](../../ARDANOVA-AZOA-INTEGRATION-CONTRACT.md) §2–§5.1, §11.
> Dependency root for `azoa-provider-adapter`, `azoa-quest-authoring`,
> `treasury-reward-to-azoa-allocation`.

## 1. Schema (DBML-first)

- [ ] **[P0] Add avatar reference fields to `User` in DBML**
    - `ardanova-client/prisma/database-architecture.dbml`
    - `azoaAvatarId` (text, nullable, unique), `azoaWalletId` (text, nullable),
      `azoaWalletAddress` (text, nullable)
    - Run `npm run generate:prisma` then `npm run generate:csharp`
    - NEVER hand-edit `schema.prisma` or EF entities

## 2. AZOA node client (Infrastructure)

- [ ] **[P0] Add Azoa settings + typed HttpClient**
    - `ArdaNova.Infrastructure/Azoa/AzoaSettings.cs` — `BaseUrl`, `TenantApiKey`, `Mode`
    - `ArdaNova.Infrastructure/Azoa/AzoaNodeClient.cs` — sets `X-Api-Key` header
    - Register via `AddHttpClient` in `ArdaNova.Infrastructure/DependencyInjection.cs`
    - Bind config section `Azoa`; tenant key read from secret store (`AZOA__TenantApiKey`), never committed

## 3. Avatar service (Application)

- [ ] **[P0] `IAzoaAvatarService` interface**
    - `ArdaNova.Application/Services/Interfaces/IAzoaAvatarService.cs`
    - `EnsureAvatarAsync`, `EnsureWalletAsync`, `IsTier2ReadyAsync`
- [ ] **[P0] `AzoaAvatarService` implementation**
    - `ArdaNova.Application/Services/Implementations/AzoaAvatarService.cs`
    - Idempotent create/resolve; persist `azoaAvatarId`
    - Wallet generate (pre-KYC allowed); cache wallet refs
    - `IsTier2ReadyAsync` = wallet-bound check only (KYC gate is node-side)
    - Register in `ArdaNova.Application/DependencyInjection.cs`

## 4. API + DTOs (Backend)

- [ ] **[P0] DTOs**
    - `ArdaNova.Application/DTOs/AzoaAvatarDtos.cs` — avatar/wallet status DTO
- [ ] **[P0] Controller**
    - `ArdaNova.API/Controllers/AzoaAvatarController.cs`
    - `POST /api/azoa/avatar/ensure`, `POST /api/azoa/avatar/wallet`,
      `GET /api/azoa/avatar/status` (IDOR-safe: target = session claim, never body)

## 5. Frontend (thin proxy only)

- [ ] **[P0] API client wrapper**
    - `ardanova-client/src/lib/api/ardanova/endpoints/azoa-avatar.ts`
- [ ] **[P0] tRPC router (thin proxy → .NET)**
    - `ardanova-client/src/server/api/routers/azoaAvatar.ts`; wire into `root.ts`
    - No Prisma, no business logic
- [ ] **[P1] Avatar/wallet/KYC status on profile or settings page**
    - Show "wallet ready" + "KYC required to transact" states
    - Translate `403 KYC_FORBIDDEN` into an actionable message

## 6. Tests (.NET only for business logic)

- [ ] **[P0] `AzoaAvatarServiceTests`**
    - `tests/ArdaNova.Application.Tests/Services/AzoaAvatarServiceTests.cs`
    - Idempotent ensure (no duplicate avatar)
    - Wallet generate succeeds pre-KYC
    - `IsTier2ReadyAsync` false-with-reason when no wallet, true once bound
    - No keys/balances persisted

## 7. Verification

- [ ] **[P0] Build + test sweep (run ONCE at end)**
    - `dotnet build` + `dotnet test` (backend)
    - `npm run build` (frontend)
    - Manual: sign up → avatar linked → generate wallet pre-KYC → status reflects ready
