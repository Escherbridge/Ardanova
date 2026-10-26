---
type: tech-stack
---

# ArdaNova Tech Stack

## Overview
Four tiers on the value path (as-built 2026-10-26; the golden path ran end to end on
2026-10-25 — `conductor/tracks/mvp-tandem-demo_20261024/plan.md` §C2). ArdaNova owns tiers 1–2;
AZOA (repo `oasis-sleek`) is the settlement and custody node ArdaNova is a tenant of; chains sit
under AZOA. Nova AI is an optional drafting feature beside the value path, never on it.

| Tier | Framework | Owns |
|------|-----------|------|
| 1. `ardanova-client/` | Next.js 15.2.3, React 19, TypeScript 5.8, tRPC 11 | User experience only; tRPC routers are thin proxies |
| 2. `ardanova-backend-api-mcp/` | .NET 10 (net10.0), EF Core 10 on PostgreSQL, SignalR | BFF + MCP: business logic, records of truth, settlement outbox, the ONLY AZOA credentials |
| 3. AZOA node (`oasis-sleek`) | .NET 10 WebAPI on SurrealDB 3.x | Custody, KYC (Identomat), consent, token genesis, reserve partitions, exchange, holds, receipts, fiat on/off-ramp (Stripe) |
| 4. Chains (under AZOA) | `Simulated` (first-class), Algorand, Solana, Ethereum | Asset truth. Only `Simulated` has run the golden path; real chains are activation-gated |
| — Nova AI (side feature) | client sheet → .NET `NovaController` → OVERSEASGENAI partner API | Project-idea refinement; cannot fund, publish, approve, or move value |
| — `ardanova-ai-client/` | FastAPI (Python 3.11), CrewAI, LangGraph | Dormant: built, **not wired**, not on the Nova path |

### Value-path doctrine (the rules every tier obeys)
- **AZOA commits before ArdaNova does.** Any failure after a node call re-reads node state and
  replays the node's bytes (`ProjectTokenService.ResolveWorkerWindowAsync`,
  `ArdaNova.Application/Services/Implementations/ProjectTokenService.cs:1407`). Full machines:
  `conductor/ARDANOVA-AZOA-INTEGRATION-CONTRACT.md` §12.
- **Idempotency key + intent hash.** Every node command is keyed; the node hashes the whole
  intent, so time fields are decided once and stored at the precision Postgres keeps (whole
  seconds into `timestamp(3)`; `Services/Implementations/AGENTS.md` §award-window-instant).
- **Fail-closed economy.** `EconomyModeResolution` (`ArdaNova.Infrastructure/Azoa/AzoaSettings.cs:211`)
  derives `disabled | simulated | live`; `ThrowIfNotLiveInProduction` (`:327`, called at
  `ArdaNova.API/Program.cs:35` and `:123`) refuses to BOOT a Production host whose economy is
  anything but `live`. The released BFF runs under the Staging label with `economy=disabled`
  (`uxqa/RUNBOOK.md` "Study setup") — a Production label would refuse to boot it — and its value
  seams answer with fail-closed stand-ins (`DisabledAzoaSettlementGateway`,
  `DisabledFiatRailGateway`; `ArdaNova.Infrastructure/Azoa/AGENTS.md`) instead of pretending.
  Rationale: `ArdaNova.API/HealthChecks/AGENTS.md`.
- **KYC value gates never accept a manual approval** (round-4 decision A-4): value eligibility
  reads the node's Identomat-backed `kycStatus`, never ArdaNova's local `ManualKycProviderService`
  (`AzoaValueEligibilityService`, `Services/Implementations/AGENTS.md` "Value-action readiness";
  `mvp-tandem-demo_20261024/spec.md` "The one hard gate").
- **Absence is proof only in the node's typed shape.** A receipt 404 counts as "nothing
  happened" only with the node's `NOT_FOUND` envelope (`ArdaNova.Infrastructure/Azoa/AzoaNodeClient.cs:140-147`).

## Architecture

### Tier 1 — `ardanova-client/` (User Experience)
- **Framework**: Next.js 15.2.3 (App Router), React 19, TypeScript 5.8, TailwindCSS 4, tRPC 11
- **Pattern**: UI, routing, and session management only — thin proxies, no business logic
- **tRPC Routers**: Thin proxies that call the .NET API via `apiClient`
- **API Client**: `src/lib/api/` — typed HTTP client for the .NET backend
- **Auth**: NextAuth.js 5.0.0-beta.31 with Google OAuth (JWT strategy); Prisma 6.19.3 is used
  here ONLY for auth/session persistence — no other Prisma writes in this tier
- **Azoa read layer**: `src/lib/azoa-sdk/client.ts` wraps the published npm `azoa-sdk` (^0.5.0,
  published 2026-10-26 from `oasis-sleek/sdk/azoa-wallet`; `ardanova-client/package.json:63`) —
  a browser-safe, **read-only** singleton (no API key, configured from
  `NEXT_PUBLIC_AZOA_BASE_URL`) that reads wallets/portfolios/holons/action run-state into React
  Query. Its write methods (`mintNft`, `fungibleMint`, `transferNft`, `executeSwap`,
  `initiateBridge`, ...) are intentionally never called from here — see
  `ardanova-client/src/lib/azoa-sdk/client.ts` header comment. Any value-bearing write goes
  through the .NET API (Tier 2), which holds the Azoa credentials.
- **No business logic in Next.js** — the one other exception is user creation on sign-in (Prisma)

### Tier 2 — `ardanova-backend-api-mcp/` (API + MCP, business logic)
- **Location**: `ardanova-backend-api-mcp/api-server/`
- **Runtime**: .NET 10 (`net10.0`) across all projects (API, Application, Domain,
  Infrastructure, MCP)
- **Pattern**: Clean Architecture (Domain → Application → Infrastructure → API/MCP)
- **Role**: the backend-for-frontend — the HTTP API the client calls, **and** the MCP server.
  Owns business logic, authorization, EF Core 10 / PostgreSQL (via
  `Npgsql.EntityFrameworkCore.PostgreSQL`), SignalR, outbox, and is the **only** tier that holds
  Azoa credentials.
- **Services**: All business logic in `ArdaNova.Application/Services/`
- **Controllers**: REST API endpoints in `ArdaNova.API/Controllers/` (Swashbuckle 6.8.0 for
  OpenAPI)
- **MCP server**: `ArdaNova.MCP/` (SDK: `ModelContextProtocol` 0.2.0-preview.1), tools in
  `ArdaNova.MCP/Tools/`. The stdio host has **no trusted end-user principal**, so tools there are
  **discovery-only** — no value-, role-, credential-, or governance-mutating operations (see
  `ArdaNova.MCP/Tools/AGENTS.md`). Escrow mutation, credential lifecycle, vote-delegation, and
  referral reward-claim tools were removed for exactly this reason.
- **Tests**: `tests/ArdaNova.Application.Tests/` and `tests/ArdaNova.API.Tests/`

### Tier 3 — AZOA (settlement and custody node, repo `oasis-sleek`)
- **What it is**: a generic, self-hostable blockchain-operations node, maintained in a
  **separate repo** (.NET 10 WebAPI, SurrealDB 3.x as sole store). ArdaNova is a tenant on it,
  never its operator; ArdaNova's users are tenant-custodial child users (contract §11.2).
- **What it executes**: identity/KYC (Identomat hosted session), custodial accounts & managed
  wallets, consent grants, ARDA genesis (operator route) and per-project token genesis (consent
  lifecycle), reserve partitions (`on-ramp`, `investor`, `contributor`), project treasuries, the
  atomic two-leg reserve exchange, hold commands (`Open` / `ReleaseIntoHold` / `Refund`),
  receipts, the `Fiat.USD` rail (Stripe checkout + webhook in, Stripe Connect payouts out),
  credential issuance, and its DAG action engine.
- **Node-side rationale** (read-only from here): `oasis-sleek/Services/Hold/AGENTS.md`,
  `Services/Reserve/AGENTS.md`, `Services/Consent/AGENTS.md`, `Core/AGENTS.md`
  §consent-scope-coverage, `Providers/Blockchain/AGENTS.md`, `Services/Fiat/OnRamp/AGENTS.md`.
- **Called from**: server-side only, from `ArdaNova.Infrastructure/Azoa/` (Tier 2), via scoped
  typed clients — custody, value, issuance, and quest keys are separate secrets, enforced in
  Production. Never called from the browser for writes.
- **Contract**: `conductor/ARDANOVA-AZOA-INTEGRATION-CONTRACT.md` (byte-identical with Azoa's
  copy); transport rules: `ardanova-backend-api-mcp/api-server/src/ArdaNova.Infrastructure/Azoa/AGENTS.md`.
- **Custody and chain inspection**: custody follows the node operator; chain state is inspected
  through Azoa operator tooling — ArdaNova hosts no explorer. User-facing copy never says "Azoa".

### Tier 4 — Chains (under AZOA)
- **`Simulated` is a first-class chain** on a `Blockchain:Mode=Simulated` node: wallets, keys,
  reserve, project treasuries, fungible creation, recipient preparation and the atomic exchange
  all have Simulated modules (F13, F14, F16, F28; `oasis-sleek/Providers/Blockchain/AGENTS.md`
  §simulated-fungible-creation, §simulated-exchange). The node boot-guards Simulated out of
  Production, and Simulated custody needs literal `ASPNETCORE_ENVIRONMENT=Development`
  (ledger P7-09 verdict), so a QA cohort runs on a Development-labelled, cohort-scoped node.
- **Real chains are activation-gated.** DEX and exchange modules are advertised only where
  `{Dex|Exchange}:Activation:{Chain}:{Network}=true`, and a Railway host ignores every pin unless
  `AZOA:Activation:DeployedHostAcknowledged=true` (`Providers/Blockchain/AGENTS.md` §module
  activation). Algorand also needs a platform mnemonic for genesis and treasury keys.
- **ArdaNova never calls a chain.** `Algorand:Provider=Azoa` routes `IAlgorandService` through
  the node; the minted chain is whatever the node reports and is persisted to `config.assetChain`.

### Side feature — Nova AI
- **Live path**: topbar sheet (`ardanova-client/src/components/ai/nova-assistant.tsx`) → tRPC
  `nova.refineProject` → .NET `POST /api/nova/project-refinement` (`NovaController`) →
  `NovaOrchestrationService` → `IOgaiClient` → OVERSEASGENAI partner validate API, white-labelled
  as Nova (`ArdaNova.Infrastructure/Ogai/AGENTS.md`). `GET /api/nova/status` reports
  `off | local-preview | live`. Rollout is owner/lease-gated (ledger P2-11, CUT-FOR-MVP).
- Nova drafts only; it is not on the value path and holds no AZOA credential.

### Dormant — `ardanova-ai-client/` (Python orchestration, not wired)
- **Framework**: FastAPI (uvicorn, port 8081), Python 3.11 (`python:3.11-slim` in the Dockerfile)
- **Role**: operates Nova AI, the platform assistant — orchestrates Claude-backed agents via
  CrewAI and LangGraph workflows to turn a prompt into structured project data, optionally
  generating a pitch deck through Gamma.
- **Status: implemented but not wired into the product.** This is not a framing choice, it's
  three independent, verified gaps:
  1. The `ai-orchestrator` block in the root `docker-compose.dev.yml` is fully commented out —
     the service isn't started by the dev stack at all.
  2. `ardanova-client` never calls it — no reference to this service anywhere in
     `ardanova-client/src`.
  3. Even if it were called, `MCPClient.call_tool` / `list_tools` / `get_resource`
     (`src/mcp/client.py`) are unimplemented stubs, and the LangGraph workflow nodes that should
     kick off crews (`run_concept_crew`, `run_strategy_crew`, `run_positioning_crew`,
     `run_full_analysis`, `run_quick_analysis`) return hardcoded placeholder data instead of
     invoking the crews in `src/crews/` — the crews are built but never actually called.
  See `ardanova-ai-client/README.md` and `ardanova-ai-client/AGENTS.md` for the full current-gaps
  list (including a stale `MCP_TOOL_REGISTRY` and an unused parallel `src/agents/` abstraction).
- **AI**: Anthropic Claude (`anthropic`), CrewAI crews (`src/crews/`: `pitch_crew.py`,
  `project_analysis_crew.py`), LangGraph workflows (`src/workflows/`: `pitch_generation.py`,
  `project_analysis.py`)
- **MCP client**: `src/mcp/client.py` targets the .NET MCP server (Tier 2); default
  `mcp_server_url` is `http://localhost:8080` (`src/config/settings.py`) — target only, per the
  stub gap above
- **Pitch decks**: Gamma API via `src/core/tools/gamma_tool.py`
- **Real tree**: `src/{agents, api(main.py, routes.py), config, core(agent_builder, crew_builder,
  graph_builder, state, tools/), crews, mcp, orchestrator, workflows}`

### Request flow
Browser → `ardanova-client` (Tier 1, UX) → tRPC → .NET API (Tier 2, BFF) → PostgreSQL / AZOA
(Tier 3) → chain (Tier 4). Nova AI branches off at Tier 2 to the OGAI partner API; the Python
`ardanova-ai-client` is not on any request path (three verified gaps above).

### The money loop (as shipped 2026-10-25)
1. **Fiat in** — sponsor pays a Stripe hosted checkout at the node; the node's webhook issues
   ARDA from the platform unit's `on-ramp` partition (`AzoaFiatRailGateway`; genesis price
   `Azoa:OnRamp:UnitsPerUsdMinorUnit="100"` = 1 ARDA per $0.01, kept by owner decision P3-02).
2. **Project genesis** — creator's funding setup: `fungible-mint` → consent lifecycle
   (quote → accept-quote → continue → refresh) → treasury resolve → `investor` + `contributor`
   partitions (`ProjectTokenService.cs:29-32`).
3. **Sponsorship** — atomic two-leg reserve exchange (ARDA → treasury, project units → sponsor),
   settled through the settlement outbox (`PENDING_DISPATCH → LEASED → SUBMITTED/CONFIRMED`, or
   `AWAITING_RECONCILIATION` → receipt reconcile; a typed receipt 404 reopens under the same key
   with an attempt refund and a 5-reopen ceiling — `ArdaNova.Infrastructure/Outbox/AGENTS.md`).
   First sponsorship is priced at the opening offer (`ExchangeService`, L2.5).
4. **Work for stake** — bid accept opens an award hold on the `contributor` partition; approve
   runs `ReleaseIntoHold` into the worker's window (`Services/Implementations/AGENTS.md` "Worker awards").
5. **Out** — withdraw ARDA → bank via node-side Stripe Connect payouts (IN for MVP, built-untested,
   P5-02); sell units back to ARDA (IN for MVP by owner override, gated, P5-03). Paying a matured
   window to the worker's wallet needs a release mandate — not built (P4-04, due 2026-12-23).

### Released topology (Railway project `a876ca12`)
| Service | Source | Notes |
|---|---|---|
| Ardanova-Client | `ardanova-client/` (this repo, `main`) | `/api/health` reports `environment`, `commit`, `deploymentKey` (`ardanova-client/src/app/api/health/route.ts`) |
| Ardanova-BFF | `ardanova-backend-api-mcp/` (this repo, `main`) | Staging label, `economy=disabled` until the value flags land (ledger P7-08) |
| ardanova-postgress | Railway Postgres | `DATABASE_URL` reference variable |
| azoa-api, azoa-schema, SurrealDB, azoa-frontend | `oasis-sleek` | `azoa-schema` is the one-shot schema/runtime-user job run before `azoa-api` (`oasis-sleek/deploy/railway/DEPLOY.md`). Deploying azoa-api from `oasis-sleek` `main` waits on ledger P7-06 |

Client image build: `ardanova-client/Dockerfile` (`node:20-alpine`) upgrades the builder to npm 11
before `npm ci`. A lockfile written on Windows can miss Linux-only optional entries; regenerate it
on the builder's platform when it drifts (commit `e54e9c9`):
`podman run --rm -v <client>:/app -w /app node:20-alpine sh -c "npm i -g npm@^11 && npm install --package-lock-only"`.

### Local tandem stack
- `.omc/scripts/local-stack.ps1 -Action up -WithAzoa` (local-only, git-ignored) runs
  `oasis-sleek/tools/dev-node/dev-node-up.ps1`: SurrealDB :8020 → dev KYC vendor sidecar
  (`tools/dev-kyc-vendor`, :8031/:8032) → AZOA WebAPI :5000 → tenant + split keys → tenant KYB +
  KYC → ARDA genesis → `on-ramp` partition, then maps `.omc/state/dev-node-env.json` onto the BFF's
  `Azoa__*` env. BFF :5147 must answer `/health` with `"economy":"simulated"`; client :3000.
- Personas: cookie `ardanova-dev-persona` = `demo-creator | demo-sponsor | demo-freelancer`,
  honoured only under `DEV_AUTH_BYPASS` and only for the seed-owned `@local.ardanova.test`
  namespace (`ardanova-client/src/server/auth/index.ts:29-32`, `src/server/auth/AGENTS.md`).
- Runbook: `conductor/tracks/mvp-tandem-demo_20261024/DEMO-RUNBOOK.md`.

## Key Architecture Decisions

### Database Management
- **Source of Truth**: `ardanova-client/prisma/database-architecture.dbml`
- **Schema**: All database schema changes MUST be defined in the DBML file first
- **Generators**:
  1. `npm run generate:prisma` → Converts DBML to Prisma Schema (`schema.prisma`) and pushes to DB
  2. `npm run generate:csharp` → Converts DBML/Prisma schema to C# EF Core entities in `ArdaNova.Infrastructure`

### Communication
- **Client-Server**: tRPC 11 for type-safe API calls (Next.js frontend ↔ tRPC routers)
- **tRPC → .NET**: HTTP via `apiClient` (tRPC routers proxy to .NET API)
- **Real-time**: SignalR (`@microsoft/signalr` ^8.0.7 on the client) for WebSocket connections
  (notifications, chat, live updates)
- **AI-Agent**: MCP Protocol between the dormant `ardanova-ai-client` and the .NET MCP tools
  (Tier 2, discovery-only) — not wired; see "Request flow," above

### Authentication
- **Auth**: NextAuth.js (5.0.0-beta.31) with Google Provider
- **Strategy**: JWT with claims: `userId`, `email`, `role`, `userType`, `verificationLevel`
- **Auth is the ONE exception** where Next.js uses Prisma directly (for user creation on sign-in)

### KYC & Identity Verification
- **Value-bearing KYC lives on the AZOA node** (as-built 2026-10-25): Identomat hosted session,
  policy set by the node operator (contract §1.1). ArdaNova provisions the tenant-custodial
  account, hands the person to the hosted session, and reads back `kycStatus`.
  `AzoaValueEligibilityService` is the single value gate; it fails closed and has three
  outcomes — `Forbidden` (person has steps left), `Conflict` (readiness unconfirmable; node
  down), allowed (`Services/Implementations/AGENTS.md` "Value-action readiness").
- **Manual approval never opens a value door** (decision A-4). The local
  `ManualKycProviderService` / `IKycProviderService` strategy and the
  ANONYMOUS → VERIFIED → PRO → EXPERT levels remain for non-value gating only;
  `KYC_PROVIDER=veriff` throws at boot (`ArdaNova.API/Program.cs:39-44`).
- **Live-vendor blocker**: Identomat's trial company cannot create configs, so no released-env
  user can pass KYC until the vendor lifts it (ledger P1-02). Local dev uses the dev vendor
  sidecar; the provider is still `identomat`.

## Blockchain (Algorand, executed via Azoa)

### Algorand Integration
- **SDK**: `Algorand2` (NuGet, C# native) in the .NET backend (Tier 2)
- **Network**: Algorand TestNet → MainNet
- **Node**: AlgoNode (`https://testnet-api.algonode.cloud`)
- **Indexer**: AlgoNode Indexer (`https://testnet-idx.algonode.cloud`)
- **Credentials**: Soulbound ASAs (ARC-19 metadata, `defaultFrozen=true`, clawback-only)
- **Execution path**: `Algorand:Provider` config selects the `IAlgorandService` implementation
  (`ArdaNova.Infrastructure/Algorand/AlgorandProviderSelection.cs`): `Azoa` (routes through
  Tier 3, `AzoaBackedAlgorandService`), `Legacy` (direct Algorand SDK), or `Simulated`
  (fabricates proofs — unset defaults here, and Production rejects it). Azoa is the real
  financial-workflow execution path; Legacy/Simulated exist for environments without an Azoa
  tenant.
- **Config**: Environment variables (`ALGORAND_NETWORK`, `ALGORAND_NODE_URL`, `ALGORAND_INDEXER_URL`, `ALGORAND_PLATFORM_MNEMONIC`)

### Credential Architecture
- **Soulbound ASAs**: Non-transferable Algorand Standard Assets representing membership credentials
- **ARC-19 Metadata**: On-chain metadata with credential type, tier, grant method, scope (project/guild)
- **Graceful Degradation**: Credentials always granted off-chain first; blockchain minting is additive
- **Tier System**: Reuses `UserTier` enum (BRONZE → SILVER → GOLD → PLATINUM → DIAMOND)

### Tokenomics Architecture
> **D44-D51 correction:** the three lines below (Fiat Integration, Conversion Path) describe
> Track 09 as it shipped, not the target model. There is no USDCa leg; fiat is only the ARDA
> on/off-ramp, terminated inside AZOA's `Fiat.USD` rail (D45), and ARDA is user-facing, not
> abstracted away (D44). See `conductor/tracks/09-tokenomics/spec.md`'s header.
- **ARDA Token**: Platform-wide fungible ASA and ArdaNova's user-facing currency (D44)
- **Project Tokens**: Per-project fungible ASAs representing equity shares (fixed supply = 100%), one real ASA minted into a platform reserve per project (D47)
- **Exchange Model**: Deterministic treasury-backed rates (not market/AMM-based)
- **Fiat Integration**: AZOA `Fiat.USD` rail (Stripe-backed) for the ARDA on-ramp (in) and off-ramp (out) — never a per-project checkout (D45)
- **Equity Model**: Tasks assigned equity percentages; contributors earn project tokens on task completion
- **Conversion Path**: USD → ARDA → Project Tokens → ARDA → USD (custodial wallet abstracted from users; the ARDA balance and project-token holding are not)

### Key Blockchain Services (Tier 2, `ArdaNova.Application` / `ArdaNova.Infrastructure`)
All of these ultimately execute financial operations on Azoa (Tier 3) when
`Algorand:Provider=Azoa` — see "Execution path," above.

| Service | Location | Purpose | Azoa execution path |
|---------|----------|---------|---------|
| `IAlgorandService` | `ArdaNova.Infrastructure/Algorand/` | Algorand SDK wrapper (mint, burn, verify, fungible ASA ops) | `AzoaBackedAlgorandService` when `Algorand:Provider=Azoa` |
| `ICredentialUtilityService` | `ArdaNova.Application/Services/` | Orchestrates grant-and-mint, revoke-and-burn | via `IAlgorandService` above |
| `IMembershipCredentialService` | `ArdaNova.Application/Services/` | Off-chain credential CRUD | off-chain only |
| `IProjectTokenService` | `ArdaNova.Application/Services/` | Project equity token management | via `ArdaNova.Infrastructure/Azoa/AzoaProjectTokenNodeGateway` |
| `ITokenBalanceService` | `ArdaNova.Application/Services/` | Local holding mirror | no custodial balance read from the node yet — holdings reads are fail-closed stubs (`AzoaExchangeClient.cs:27-34`; ledger P5-01) |
| `IExchangeService` | `ArdaNova.Application/Services/` | Declares ARDA per project unit (opening offer while a raise is open) | via `ArdaNova.Infrastructure/Azoa/AzoaExchangeClient` |
| `IPayoutService` | `ArdaNova.Application/Services/` | Sell units (node exchange) + withdraw (node cash-out, read back) | via `AzoaExchangeClient` / `AzoaCashOutClient` |
| `IFundingIntentService` | `ArdaNova.Application/Services/` | Sponsorship intent → settlement → outbox | `AzoaSettlementGateway` (exchange + receipt) |
| `EconomicOutboxDispatchService` | `ArdaNova.Application/Services/Implementations/` | Lease, dispatch, reconcile, no-effect reopen | `ArdaNova.Infrastructure/Outbox/` (`EconomicOutboxLeaseStore`) |
| award holds | `ProjectTokenService` (`:1091` open, `:1333` release-into-hold) | Open, `ReleaseIntoHold`, refund | `ArdaNova.Infrastructure/Azoa/AzoaHoldClient` (`api/hold-commands`) |
| on-ramp | `ArdaNova.Infrastructure/Azoa/AzoaFiatRailGateway.cs` | Add funds; genesis price registration | node `Fiat.USD` rail |

## Frontend Design System

### Brand & Copy
- **All generated copy and UI text MUST follow** `documentation/BRAND_GUIDELINES.md`
- Lead with values and outcomes, not technical implementation
- Use plain language (e.g., "ownership shares" not "tokens", "member-led governance" not "DAO")
- See Brand Guidelines for full terminology mapping and audience framing

### Design Philosophy: Swiss Brutalism with Electric Accents
- **Clarity over decoration** — every element serves a purpose
- **Hierarchy through typography** — bold, confident type
- **Restrained color** — dark foundations with strategic accent pops
- **Sharp edges** — brutalist geometry, no rounded softness (border-radius: 0)

### Color System
| Role | Dark Mode | Usage |
|------|-----------|-------|
| Background | `#1a1d23` (Slate) | Primary surfaces |
| Foreground | `#ffffff` (White) | Primary text |
| Card | `#22262e` | Elevated surfaces |
| Muted | `#94a3b8` | Secondary text |
| Border | `#334155` | Dividers, outlines |
| Electric Cyan | `#00d4ff` | Primary actions, links, focus |
| Neon Green | `#00ff88` | Success, positive states |
| Hot Pink | `#ff0080` | Highlights, notifications |
| Electric Purple | `#8b5cf6` | Tertiary accent, charts |
| Warning Yellow | `#fbbf24` | Caution states |

### Typography
| Element | Font | Weight |
|---------|------|--------|
| Headlines | JetBrains Mono / System Mono | Bold (700) |
| Body | Inter / System Sans | Regular (400) |
| UI Labels | Inter / System Sans | Medium (500) |
| Code/Data | JetBrains Mono | Regular (400) |

### Layout
- **Grid**: 12-column Swiss grid system
- **Gutter**: 1.5rem (24px)
- **Max content width**: 1440px
- **Border radius**: 0 (sharp brutalist corners)

### Component Architecture
- **Modular, composable components** — build from small primitives up
- **Reuse over duplication** — extract shared patterns into `src/components/ui/`
- **Feature components** compose UI primitives for specific domains

```
src/components/
├── ui/              # Base primitives (button, card, badge, input, etc.)
├── layouts/         # Page layouts (authenticated, feed, etc.)
├── projects/        # Project-specific composed components
├── guilds/          # Guild-specific composed components
├── feed/            # Feed/social composed components
├── governance/      # Governance composed components
├── opportunities/   # Opportunity composed components
├── chats/           # Chat composed components
└── [feature]/       # New feature domains follow same pattern
```
