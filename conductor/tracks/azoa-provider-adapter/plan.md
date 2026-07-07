# Track — AZOA Provider Adapter

> Contract: [`ARDANOVA-AZOA-INTEGRATION-CONTRACT.md`](../../ARDANOVA-AZOA-INTEGRATION-CONTRACT.md) §9.
> Depends on `azoa-avatar-onboarding` (`AzoaNodeClient`, avatar/wallet refs).
> Backend-only. Swap point: `ArdaNova.Infrastructure/DependencyInjection.cs:67`.

## 1. Adapter implementation

- [ ] **[P0] `AzoaBackedAlgorandService : IAlgorandService`**
    - `ArdaNova.Infrastructure/Azoa/AzoaBackedAlgorandService.cs`
    - Reuse `AzoaNodeClient` (from avatar-onboarding track)
    - `MintSoulboundASAAsync` → Grant/allocation Mint (soulbound: total=1, dec=0, frozen)
    - `CreateFungibleASAAsync` → `POST /api/nft/fungible-mint`
    - `TransferASAAsync` → `POST /api/allocation/{avatarId}` kind=Transfer
    - `GetASABalanceAsync` / `GetASAInfoAsync` → AZOA wallet/portfolio read (chain = source of truth)
    - `VerifyOwnershipAsync` → AZOA ownership read
    - `BuildARC19MetadataAsync` → keep local (delegate to existing shape builder)
    - `BurnASAAsync` / `ClawbackASAAsync` → explicit "deferred (AZOA H2)" Result

## 2. Feature flag + DI

- [ ] **[P0] Provider selection config**
    - `Algorand:Provider` = `Legacy` | `Azoa` | `Simulated` (or reuse `Azoa:Mode`)
- [ ] **[P0] Conditional registration**
    - Replace single line at `ArdaNova.Infrastructure/DependencyInjection.cs:67`
    - Default `Legacy`/`Simulated` until validated; flip to `Azoa` after

## 3. Cutover / decommission (gated on validation)

- [ ] **[P1] Remove platform mnemonic + direct Algod/Indexer HttpClient from ArdaNova**
    - Only after `Azoa` path validated end-to-end
    - Retain ARC-19 metadata shape builder (domain)

## 4. Tests (.NET)

- [ ] **[P0] `AzoaBackedAlgorandServiceTests`**
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
