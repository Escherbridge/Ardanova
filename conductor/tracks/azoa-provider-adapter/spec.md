# AZOA Provider Adapter — Technical Specification

## Overview

Replace ArdaNova's self-custodial Algorand signer with an adapter that talks to
the shared AZOA node, **behind the existing `IAlgorandService` interface** so the
rest of the codebase doesn't change. Custody moves off ArdaNova (to the node
operator, §2). The cutover is **feature-flagged** so the legacy/simulated path
coexists during migration.

> Source contract: [`conductor/ARDANOVA-AZOA-INTEGRATION-CONTRACT.md`](../../ARDANOVA-AZOA-INTEGRATION-CONTRACT.md) §9.

## Current state (verified)

- `IAlgorandService`
  ([`ArdaNova.Application/Services/Interfaces/IAlgorandService.cs`](../../../ardanova-backend-api-mcp/api-server/src/ArdaNova.Application/Services/Interfaces/IAlgorandService.cs))
  defines the full surface: `MintSoulboundASAAsync`, `BurnASAAsync`,
  `GetASAInfoAsync`, `VerifyOwnershipAsync`, `BuildARC19MetadataAsync`,
  `CreateFungibleASAAsync`, `TransferASAAsync`, `GetASABalanceAsync`,
  `ClawbackASAAsync`.
- `AlgorandService`
  ([`ArdaNova.Infrastructure/Algorand/AlgorandService.cs`](../../../ardanova-backend-api-mcp/api-server/src/ArdaNova.Infrastructure/Algorand/AlgorandService.cs))
  is the self-contained custodial signer (platform mnemonic signs everything,
  direct Algod/Indexer HttpClient).
- Registered at
  [`ArdaNova.Infrastructure/DependencyInjection.cs:67`](../../../ardanova-backend-api-mcp/api-server/src/ArdaNova.Infrastructure/DependencyInjection.cs#L67)
  via `services.AddHttpClient<IAlgorandService, AlgorandService>()` — this single
  registration line is the swap point.
- Consumers today: `CredentialUtilityService` (soulbound credentials) and the
  tokenomics services (Track 09).

## Architecture

### The adapter

Introduce `AzoaBackedAlgorandService : IAlgorandService` in
`ArdaNova.Infrastructure/Azoa/`, implementing the same surface by calling the
AZOA node (reusing `AzoaNodeClient` from the `azoa-avatar-onboarding` track).

| `IAlgorandService` method | AZOA call | Notes |
|---|---|---|
| `MintSoulboundASAAsync` | `Grant` quest node (soulbound) **or** `POST /api/allocation` kind=`Mint` | soulbound: total=1, decimals=0, frozen |
| `CreateFungibleASAAsync` | `POST /api/nft/fungible-mint` (dedicated endpoint, §11.3) **or** `FungibleTokenCreate` quest node | total/decimals are consumer-authoritative; AZOA derives no economics |
| `TransferASAAsync` | `POST /api/allocation/{avatarId}` kind=`Transfer` | |
| `GetASABalanceAsync` / `GetASAInfoAsync` | AZOA wallet/portfolio read | **chain is source of truth; AZOA stores no balance** |
| `VerifyOwnershipAsync` | AZOA NFT/holon ownership read | |
| `BuildARC19MetadataAsync` | **stays local** (ArdaNova keeps the ARC-19 metadata *shape*; domain) | passed to AZOA as opaque shape |
| `BurnASAAsync` / `ClawbackASAAsync` | **deferred** — soulbound clawback-revoke is AZOA H2 (mint shipped; revoke follow-up) | return a clear "not yet supported via node" result; legacy path still covers if needed |

### Feature flag / cutover

- `Algorand:Provider` (or reuse `Azoa:Mode`) selects the implementation:
  - `Legacy` → `AlgorandService` (current mnemonic signer)
  - `Azoa` → `AzoaBackedAlgorandService`
  - (`Simulated` remains valid for dev — deterministic `sim:` ids)
- DI chooses the registration based on config (replace the single line at
  `DependencyInjection.cs:67`). Default stays `Legacy`/`Simulated` until the
  node path is validated, then flips to `Azoa`.

### Decommission (only after cutover validated)

- Remove the platform mnemonic + direct Algod/Indexer `HttpClient` usage from
  ArdaNova. ArdaNova retains only the ARC-19 metadata *shape* (domain), passed to
  AZOA. Custody now follows the node operator (§2).

## Backend-only

This track is entirely .NET (Infrastructure + DI + tests). No frontend changes —
all current `IAlgorandService` consumers keep working through the interface.

## Out of scope

- Avatar/wallet linkage (`azoa-avatar-onboarding` — dependency).
- Quest definitions (`azoa-quest-authoring`).
- Treasury/reward allocation orchestration (`treasury-reward-to-azoa-allocation`),
  though it relies on this adapter's transfer/mint paths.
- KMS/custody hardening (node operator concern).

## Acceptance criteria

- `AzoaBackedAlgorandService` satisfies the full `IAlgorandService` surface; all
  existing consumers compile and run unchanged.
- Config flag switches Legacy ↔ Azoa ↔ Simulated with no code change at call
  sites.
- Mint/transfer/balance/ownership/info paths route to the AZOA node when flagged
  `Azoa`; `Burn`/`Clawback` return an explicit "deferred (H2)" result.
- With the flag on `Azoa`, no ArdaNova code path uses the platform mnemonic or
  direct Algod/Indexer calls.
- `BuildARC19MetadataAsync` output is byte-equivalent to the legacy implementation
  (metadata shape preserved).
