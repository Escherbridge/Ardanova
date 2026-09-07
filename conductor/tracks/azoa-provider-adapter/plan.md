---
type: plan
---
# Track — AZOA Provider Adapter

> Contract: [`ARDANOVA-AZOA-INTEGRATION-CONTRACT.md`](../../ARDANOVA-AZOA-INTEGRATION-CONTRACT.md) §9.
> Depends on `azoa-user-onboarding` (`AzoaNodeClient`, AZOA user/wallet refs).
> **Reconciled 2026-09-07** against `f2e4bc5`: routes are `/api/action*` and
> `POST /api/allocation/{userId}`; ArdaNova C# names keep `Avatar`/`Quest`.
> Backend-only. Swap point: `ArdaNova.Infrastructure/DependencyInjection.cs:67`.

## 1. Adapter implementation

- [x] **[P0] `AzoaBackedAlgorandService : IAlgorandService`**
    - `ArdaNova.Infrastructure/Azoa/AzoaBackedAlgorandService.cs`
    - Reuse `AzoaNodeClient` (from the `azoa-user-onboarding` track)
    - `MintSoulboundASAAsync` → Grant/allocation Mint (soulbound: total=1, dec=0, frozen)
    - `CreateFungibleASAAsync` → `POST /api/nft/fungible-mint`
    - `TransferASAAsync` → `POST /api/allocation/{userId}` kind=Transfer
    - `GetASABalanceAsync` / `GetASAInfoAsync` → AZOA wallet/portfolio read (chain = source of truth)
    - `VerifyOwnershipAsync` → AZOA ownership read
    - `BuildARC19MetadataAsync` → keep local (delegate to existing shape builder)
    - `BurnASAAsync` / `ClawbackASAAsync` → explicit "deferred (AZOA H2)" Result

## 2. Feature flag + DI

- [x] **[P0] Provider selection config**
    - `Algorand:Provider` = `Legacy` | `Azoa` | `Simulated` (or reuse `Azoa:Mode`)
- [x] **[P0] Conditional registration**
    - Shipped at `ArdaNova.Infrastructure/DependencyInjection.cs:150-185`
      (`AlgorandProviderSelection.Parse` → Legacy | Azoa | Simulated)
    - Default `Legacy`/`Simulated` until validated; flip to `Azoa` after

## 3. Cutover / decommission (gated on validation)

- [ ] **[P1] Remove platform mnemonic + direct Algod/Indexer HttpClient from ArdaNova**
    - Only after `Azoa` path validated end-to-end
    - Retain ARC-19 metadata shape builder (domain)

## 4. Tests (.NET)

- [x] **[P0] `AzoaBackedAlgorandServiceTests`**
    - `tests/ArdaNova.Application.Tests/Services/AzoaBackedAlgorandServiceTests.cs`
      (or Infrastructure test project if one exists)
    - Each interface method routes to the expected AZOA call (mock node client)
    - `Burn`/`Clawback` return deferred result
    - Flag switch resolves correct implementation
- [ ] **[P0] ARC-19 metadata parity test**
    - Adapter `BuildARC19MetadataAsync` output == legacy output for same input

## 5. Verification

- [ ] **[P0] Build + test sweep (run ONCE at end)**
    - `dotnet build` + `dotnet test`
    - Existing `CredentialUtilityService` + tokenomics consumers compile unchanged
    - Manual: flip flag to `Azoa`, mint a soulbound credential against node (sim ok)
