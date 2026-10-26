# ArdaNova Platform Architecture

This document provides a detailed technical architecture for the ArdaNova platform, covering the repository structure, service boundaries, data flow, and integration patterns.

---

## Architecture at a glance (as-built 2026-10-26)

ArdaNova is **four tiers on the value path**, with AZOA slotted in as the settlement and custody
node. The full creator → sponsor → worker loop ran end to end on 2026-10-25 against a real AZOA
node (`conductor/tracks/mvp-tandem-demo_20261024/plan.md` §C2, findings F1–F31). Remaining work
is tracked row by row in `conductor/tracks/mvp-readiness-loop_20261026/DECISIONS-LEDGER.md`.

| Tier | Repo / path | Runtime | Owns | Never does |
|---|---|---|---|---|
| 1. Client | `ardanova-client/` | Next.js 15, tRPC 11, NextAuth v5 | UX, session | business logic, value writes, AZOA credentials |
| 2. BFF | `ardanova-backend-api-mcp/` | .NET 10, EF Core 10 on PostgreSQL, SignalR | business logic, records of truth, settlement outbox, the ONLY AZOA credentials | custody, KYC decisions, chain calls |
| 3. AZOA node | `oasis-sleek` (separate repo) | .NET 10 WebAPI, SurrealDB 3.x | custody, KYC (Identomat), consent, genesis, reserve partitions, exchange, holds, receipts, fiat rail (Stripe) | economics: valuation, splits, prices are declared by the consumer |
| 4. Chains | under AZOA | `Simulated` (first-class), Algorand, Solana, Ethereum | asset truth | — |

**Side features, not on the value path.** Nova AI refines project ideas through the BFF to the
OVERSEASGENAI partner API (`ArdaNova.Infrastructure/Ogai/AGENTS.md`); it cannot fund, publish,
approve or move value. The Python `ardanova-ai-client/` is built but dormant (no caller, its MCP
client is a stub — `ardanova-ai-client/AGENTS.md`). The MCP host is discovery-only
(`ArdaNova.MCP/Tools/AGENTS.md`).

```
 Browser
   │
   ▼
┌─────────────────────────┐   tRPC thin proxies → apiClient (API_KEY + signed actor assertion)
│ 1  ardanova-client       │──────────────────────────────┐
│    Next.js 15 (UX only)  │                              │
│    read-only azoa-sdk     │                              ▼
└─────────────────────────┘          ┌───────────────────────────────────────┐
                                     │ 2  ardanova-backend-api-mcp (.NET 10)  │──► PostgreSQL
   Nova sheet ─► NovaController ─►   │    business logic · records of truth   │    (EF Core)
   OGAI partner API (side feature)   │    settlement outbox · economy mode    │
                                     │    ONLY holder of AZOA credentials     │
                                     └───────────────────┬───────────────────┘
                     scoped typed clients: custody · value · issuance · quest keys
                                                         ▼
                                     ┌───────────────────────────────────────┐
                                     │ 3  AZOA node (oasis-sleek)             │──► SurrealDB
                                     │    custody · KYC · consent · genesis   │◄── Stripe
                                     │    exchange · holds · receipts         │    (checkout,
                                     └───────────────────┬───────────────────┘     webhook,
                                                         ▼                         Connect)
                                     ┌───────────────────────────────────────┐
                                     │ 4  chains: Simulated · Algorand · …    │
                                     └───────────────────────────────────────┘
```

### Trust boundaries

- **Only `ArdaNova.Infrastructure/Azoa/` holds AZOA credentials** — custody, value, quest and
  issuance keys, rejected in Production if any two share a secret
  (`ArdaNova.Infrastructure/Azoa/AGENTS.md`). The client's AZOA layer is read-only and keyless.
- **AZOA receives already-decided amounts.** ArdaNova declares rates and terms; the node
  never computes economics, and ArdaNova never sends it a fiat amount
  (`AzoaSettlementGateway.cs:57`, contract §0 rule 3).
- **ArdaNova never terminates a payment.** Card, webhook secret and Connect accounts live in the
  node's `Fiat.USD` rail; ArdaNova's one on-ramp adapter is `AzoaFiatRailGateway`.
- **Node text never reaches a person.** Transport maps node refusals to stable messages
  (`AzoaNodeClient`); operators read the original in the transport log.

### Failure doctrine (why the code is shaped the way it is)

1. **The node commits before ArdaNova does.** Any local failure after a node call re-reads node
   state and replays the node's bytes — `ProjectTokenService.ResolveWorkerWindowAsync`
   (`ArdaNova.Application/Services/Implementations/ProjectTokenService.cs:1407`), found live as F31.
2. **Idempotency key + intent hash.** Every node command is keyed and the node hashes the whole
   intent, so time fields are decided once and stored at database precision (whole seconds into
   `timestamp(3)`; `Services/Implementations/AGENTS.md` §award-window-instant).
3. **Absence is proof only in the node's typed shape** — a 404 with the `NOT_FOUND` envelope;
   any other 404 is an unknown outcome (`AzoaNodeClient.cs:140-147`).
4. **Fail-closed economy modes.** `EconomyModeResolution` (`ArdaNova.Infrastructure/Azoa/AzoaSettings.cs:211`)
   derives `disabled | simulated | live` from config and the registered provider;
   `ThrowIfNotLiveInProduction` refuses to boot a Production host that is not `live`
   (`ArdaNova.API/Program.cs:35`, `:123`). `/health` carries only the economy word; `/ready`
   projects each leg (`ArdaNova.API/HealthChecks/AGENTS.md`).
5. **KYC value gates never accept manual approval** (decision A-4). `AzoaValueEligibilityService`
   reads the node's Identomat-backed status and distinguishes `Forbidden` (person) from `Conflict`
   (platform) (`Services/Implementations/AGENTS.md` "Value-action readiness").
6. **Ceilings never hide moved value.** A refusal the gateway calls safe may end `FAILED`; an
   unreadable outcome parks `AWAITING_RECONCILIATION` with a Critical alert, never `FAILED`
   (`ArdaNova.Infrastructure/Outbox/AGENTS.md` "Dispatch ceiling").

### The money loop (as shipped)

| Step | ArdaNova (BFF) | AZOA node | Evidence |
|---|---|---|---|
| Fiat in | `AzoaFiatRailGateway` starts checkout; declares the genesis price once (`UnitsPerUsdMinorUnit="100"` = 1 ARDA per $0.01, ledger P3-02) | Stripe hosted checkout → webhook → on-ramp lot → `IssueUnits` from the platform `on-ramp` partition | plan.md Step-3 milestone, F19, F21 |
| Project genesis | `ProjectTokenService` drives `fungible-mint` through the consent lifecycle, then treasury resolve and `investor` / `contributor` partitions | durable creation (`AwaitingConsent` → quote → accept → continue → `Completed`), reserve + project treasury | F13–F17; contract §12.3.1 |
| Sponsorship | `FundingIntentService` → settlement → outbox → `AzoaSettlementGateway`; opening-offer price while the raise is open (`ExchangeService`) | atomic two-leg reserve exchange (ARDA → treasury, units → sponsor), receipt by idempotency key | STEP 3 SETTLED; contract §12.1 |
| Award | bid accept → `AzoaHoldClient.OpenAsync` (`open:{holdId}`) | reserve-funded hold on the `contributor` partition | F20 |
| Approve | `ReleaseIntoHold` (`release-into-hold:{holdId}:{nextHoldId}`), window instant persisted first | child hold = the worker's window; `ReplayConflict` on byte-different replay | F31; contract §12.2 |
| Out | withdraw ARDA → bank (P5-02, IN, built-untested); sell units → ARDA (P5-03, IN, gated) | cash-out + Stripe Connect payouts; buy-back exchange | `oasis-sleek/Services/Fiat/OffRamp/AGENTS.md` |
| Window matures | — | release mandate needed; **not built** (P4-04, due 2026-12-23) | F20 "step-5 boundary" |

**Settlement outbox in one line:** settlement `PENDING_DISPATCH` → outbox `LEASED` (5-min lease)
→ `SUBMITTED`/`CONFIRMED` on a terminal receipt, or `AWAITING_RECONCILIATION` on any ambiguity →
receipt reconcile → a typed receipt 404, past a 5-minute quiet window, reopens the row under the
SAME key with that claim's attempt refunded, capped at 5 reopens (owner decision F29). Full
machine: `conductor/ARDANOVA-AZOA-INTEGRATION-CONTRACT.md` §12.1.

### Released topology (Railway project `a876ca12`)

| Service | Source | State (2026-10-26) |
|---|---|---|
| Ardanova-Client | `ardanova-client/` | Live at `e54e9c9`; `/api/health` reports `environment`, `commit`, `deploymentKey` (`src/app/api/health/route.ts`) |
| Ardanova-BFF | `ardanova-backend-api-mcp/` | Staging label, `economy=disabled` until the value flags are set (ledger P7-08) |
| ardanova-postgress | Railway Postgres | Prod migration is human-gated (P7-10) |
| azoa-api · azoa-schema · SurrealDB · azoa-frontend | `oasis-sleek` | `azoa-schema` is the one-shot schema job deployed before `azoa-api` (`oasis-sleek/deploy/railway/DEPLOY.md`); deploy-from-`main` waits on P7-06 |
| Aradnova-AI | `ardanova-ai-client/` | dormant |

`azoa-sdk` 0.5.0 is published on npm (`ardanova-client/package.json:63`). The client Dockerfile
(`node:20-alpine`) upgrades to npm 11 before `npm ci`; when the Windows-written lockfile drifts
from the Linux builder, regenerate it in-container (commit `e54e9c9` message has the command).
A QA cohort with value needs a Development-labelled, cohort-scoped node: the node boot-guards
Simulated out of Production (ledger P7-09 verdict).

### Local tandem stack

`.omc/scripts/local-stack.ps1 -Action up -WithAzoa` (local-only; `.omc/` is git-ignored) runs
`oasis-sleek/tools/dev-node/dev-node-up.ps1` — SurrealDB :8020, dev KYC vendor sidecar
(`tools/dev-kyc-vendor`, :8031/:8032), AZOA WebAPI :5000, tenant + split keys, tenant KYB + KYC,
ARDA genesis, `on-ramp` partition — and maps `oasis-sleek/.omc/state/dev-node-env.json` onto the
BFF's `Azoa__*` env. Success: BFF `:5147/health` → `"economy":"simulated"`; client `:3000`.
Persona switching uses the `ardanova-dev-persona` cookie, honoured only under `DEV_AUTH_BYPASS`
and only for the seed-owned `@local.ardanova.test` namespace
(`ardanova-client/src/server/auth/index.ts:29-32`). Never wait on `dev-node-up.ps1`'s stdout
(spawned servers hold the pipe open; F12). Runbook:
`conductor/tracks/mvp-tandem-demo_20261024/DEMO-RUNBOOK.md`.

### Component status (2026-10-26)

| Component | Status | Notes |
| --- | --- | --- |
| `ardanova-client` | ✅ Live | tRPC thin proxies; read-only AZOA layer |
| `ardanova-backend-api-mcp` | ✅ Live (Staging, economy disabled) | 42 controllers; MCP host discovery-only |
| AZOA integration (.NET) | ✅ Golden path driven live (Simulated) | custody, KYC, genesis, exchange settlement, holds; see `ArdaNova.Infrastructure/Azoa/AGENTS.md` |
| Custodial balance read | ⛔ Missing | holdings reads are fail-closed stubs (P5-01) |
| Hold release mandate | ⛔ Missing | due 2026-12-23 (P4-04) |
| Task escrow components | ⛔ Cut for MVP | the hold rail is the MVP escrow (P4-05) |
| Live Identomat | ⏸ Vendor-gated | trial company cannot create configs (P1-02) |
| Nova AI refinement | ⏸ Owner/lease-gated | via OGAI partner API (P2-11) |
| `ardanova-ai-client` | 💤 Dormant | built, not wired |
| Event bus · SignalR · storage · generator | ✅ Complete | unchanged |

Older narrative below this section predates the AZOA slot-in in places; where it disagrees,
this section and the integration contract win.

---

## Table of Contents

1. [System Overview](#system-overview)
2. [Repository Structure](#repository-structure)
3. [Development Scripts](#development-scripts)
4. [Application Architecture](#application-architecture)
5. [Event-Driven Architecture](#event-driven-architecture)
6. [Real-Time Communication](#real-time-communication)
7. [File Storage](#file-storage)
8. [Data Flow](#data-flow)
9. [Database Design](#database-design)
10. [Code Generation & EF Core](#code-generation--ef-core)
11. [API Layer](#api-layer)
12. [Authentication & Authorization](#authentication--authorization)
13. [AI & MCP Integration](#ai--mcp-integration)
14. [Ledger Integration](#ledger-integration)
15. [Infrastructure](#infrastructure)
16. [Security](#security)

---

## System Overview

### High-Level Architecture

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                              CLIENT LAYER                                    │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  ┌────────────────────────────────────────────────────────────────────────┐ │
│  │                     ARDANOVA-CLIENT (Next.js 15)                       │ │
│  │  ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌──────────┐    │ │
│  │  │ Projects │ │Governance│ │  Studio  │ │   Swap   │ │  Chats   │    │ │
│  │  │Dashboard │ │          │ │  (Gamma) │ │          │ │          │    │ │
│  │  └────┬─────┘ └────┬─────┘ └────┬─────┘ └────┬─────┘ └────┬─────┘    │ │
│  │       └────────────┴────────────┴─────┬──────┴────────────┘          │ │
│  │                                       │                               │ │
│  │       ┌───────────────────────────────┼───────────────────────────┐  │ │
│  │       │                               │                           │  │ │
│  │       ▼                               ▼                           ▼  │ │
│  │  ┌─────────┐                   ┌─────────────┐              ┌─────────┐│ │
│  │  │  tRPC   │                   │  SignalR    │              │ Storage ││ │
│  │  │ Client  │                   │  WebSocket  │              │  Client ││ │
│  │  └────┬────┘                   └──────┬──────┘              └────┬────┘│ │
│  │       │                                                           │     │ │
│  │       │          ┌──────────────────────────┐                    │     │ │
│  │       │          │ azoa-sdk (READ-ONLY)      │                    │     │ │
│  │       │          │ wallets/portfolios/holons │                    │     │ │
│  │       │          │ no API key, browser-safe  │                    │     │ │
│  │       │          └─────────────┬────────────┘                    │     │ │
│  └───────┼────────────────────────┼───────────────┼───────────────────┼────┘ │
│          │                        │                │                   │     │
└──────────┼────────────────────────┼────────────────┼───────────────────┼─────┘
           │                        │ read-only       │                   │
           ▼                        │ AZOA reads      ▼                   ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                           ARDANOVA-BACKEND-API-MCP                          │
│                       (.NET 10, the backend-for-frontend)                   │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐    │
│  │ Controllers  │  │  Event Bus   │  │  SignalR Hub │  │   Storage    │    │
│  │ (42 REST)    │  │ (In-Memory)  │  │ (WebSocket)  │  │   Service    │    │
│  └──────┬───────┘  └──────┬───────┘  └──────┬───────┘  └──────┬───────┘    │
│         │                 │                 │                 │             │
│         └─────────────────┴─────────────────┴─────────────────┘             │
│                                    │                                         │
│                         ┌──────────▼──────────┐    ┌───────────────────┐   │
│                         │    Application      │    │   ArdaNova.MCP     │   │
│                         │     Services        │    │ 42 tools, DISCOVERY-│   │
│                         │                     │    │ ONLY (no trusted   │   │
│                         └──────────┬──────────┘    │ end-user principal)│   │
│                                    │                └─────────┬──────────┘   │
│                         ┌──────────▼──────────┐               │              │
│                         │   Infrastructure    │               │ MCP          │
│                         │ Repository Pattern  │               │              │
│                         │ Azoa/ (ONLY tier     │               ▼              │
│                         │ holding AZOA creds)  │     ┌────────────────────┐ │
│                         └──────────┬──────────┘     │ ardanova-ai-client  │ │
│                                    │                 │ (FastAPI, Nova AI)  │ │
└────────────────────────────────────┼─────────────────  NOT YET WIRED ──────┘─┘
                                     │
           ┌─────────────────────────┼─────────────────────────┐
           │                         │                         │
           ▼                         ▼                         ▼
    ┌──────────────┐         ┌──────────────┐         ┌──────────────┐
    │  PostgreSQL  │         │     S3       │         │     AZOA      │
    │  (Database)  │         │   Storage    │         │  (external)   │
    └──────────────┘         └──────────────┘         └──────┬───────┘
                                                               │ settles to
                                                               ▼
                                                        ┌──────────────┐
                                                        │   Algorand    │
                                                        │ (underlying   │
                                                        │    chain)     │
                                                        └──────────────┘
```

### Core Principles

1. **Simplicity** - Single platform with all features consolidated
2. **Type Safety** - End-to-end TypeScript with tRPC
3. **Clean Architecture** - Separation of concerns in backend
4. **Event-Driven** - Domain events for loose coupling
5. **Real-Time** - WebSocket for instant updates
6. **AI-First** - Native AI integration via MCP protocol (discovery-only until end-user
   authorization reaches the stdio host)
7. **Decentralization** - Progressive ownership via Cooperatives / DAOs
8. **Credential isolation** - Only `ArdaNova.Infrastructure/Azoa/` ever holds an AZOA credential;
   the client's AZOA reads are unauthenticated/read-only and MCP tools never see one

---

## Repository Structure

```
ardanova/
│
├── ardanova-client/                    # Main Next.js application (owns the UX)
│   ├── src/
│   │   ├── app/                        # Next.js 15 App Router
│   │   │   ├── auth/, login/, sign-up/, signup/  # Authentication pages
│   │   │   ├── dashboard/              # User dashboard
│   │   │   ├── projects/               # Project listing & details
│   │   │   ├── governance/             # Governance UI
│   │   │   ├── studio/                 # Pitch builder (Gamma-backed)
│   │   │   ├── swap/                   # Token swap UI
│   │   │   ├── opportunities/, tasks/  # Task/opportunity marketplace
│   │   │   ├── guilds/, people/        # Guild & social surfaces
│   │   │   ├── credentials/, portfolio/, events/, chats/, admin/, settings/
│   │   │   ├── (no explorer/ — RETIRED, not planned; AZOA is the chain boundary)
│   │   │   └── api/                    # Next.js API routes
│   │   │
│   │   ├── components/                 # Shared UI components
│   │   ├── lib/                        # Utilities and helpers
│   │   │   ├── api/                    # apiClient for .NET backend (lib/api/ardanova/)
│   │   │   ├── azoa-sdk/               # READ-ONLY AZOA client (npm `azoa-sdk`, no API key)
│   │   │   ├── chat/, commerce/, contracts/, credentials/, studio/, vocabulary/
│   │   │   └── websocket/               # Real-time WebSocket hooks
│   │   │
│   │   ├── server/                     # Server-side code
│   │   │   ├── api/routers/            # tRPC routers (thin proxies to apiClient)
│   │   │   ├── auth/                   # NextAuth v5 configuration
│   │   │   └── db.ts                   # Prisma client (auth/session only)
│   │   │
│   │   └── trpc/                       # tRPC client
│   │
│   ├── prisma/                         # Database schema & migrations
│   └── public/                         # Static assets
│
├── ardanova-backend-api-mcp/           # .NET 10 Backend + MCP Server (the BFF)
│   ├── ardanova.sln                    # Solution file
│   └── api-server/
│       └── src/
│           ├── ArdaNova.Domain/        # Entities, Enums (99+ entity files)
│           ├── ArdaNova.Application/   # Services, DTOs (52+ service implementations)
│           │   ├── Common/             # Result<T>, PagedResult<T>
│           │   ├── DTOs/               # DTO files
│           │   ├── Azoa/               # Quest authoring DTOs (server-side only)
│           │   ├── Mappings/           # AutoMapper profiles
│           │   └── Services/           # Business logic
│           ├── ArdaNova.Infrastructure/# DbContext, Repositories, external gateways
│           │   ├── Data/               # EF Core DbContext
│           │   ├── Repositories/       # Generic Repository
│           │   ├── Storage/            # S3/Local storage ✅
│           │   ├── Algorand/           # Legacy/simulated on-chain signer (IAlgorandService)
│           │   └── Azoa/               # ONLY tier holding AZOA creds — custody/value/quest/
│           │                           # issuance clients, settlement + fiat on-ramp gateways
│           │                           # (see `Azoa/AGENTS.md`, "Financial workflows via AZOA")
│           ├── ArdaNova.API/           # Controllers, Middleware
│           │   ├── Controllers/        # 42 REST controllers ✅
│           │   ├── EventBus/           # Domain event bus ✅
│           │   ├── WebSocket/          # SignalR hub ✅
│           │   └── Middleware/         # API key, exception handling
│           └── ArdaNova.MCP/           # 42 MCP Tools, DISCOVERY-ONLY (see Tools/AGENTS.md)
│
├── ardanova-ai-client/                 # Python FastAPI service operating Nova AI
│   ├── src/
│   │   ├── api/                        # FastAPI app (main.py) + routes.py (/api/agent/*)
│   │   ├── orchestrator/               # Agent orchestration
│   │   ├── agents/                     # Agent definitions
│   │   ├── core/                       # agent_builder, crew_builder, graph_builder, state,
│   │   │                               # tools/ (mcp_tool.py, gamma_tool.py)
│   │   ├── crews/                      # CrewAI crews (pitch, project analysis) — built
│   │   ├── workflows/                  # LangGraph workflows — nodes still return placeholders
│   │   ├── mcp/                        # MCP client — call_tool/list_tools are stubs
│   │   └── config/                     # Settings (port 8081)
│   ├── AGENTS.md                       # Boundary rules (no AZOA creds, MCP discovery-only)
│   ├── requirements.txt
│   └── Dockerfile
│
├── ardanova-game-sdk/                  # Game SDK (TODO)
│   ├── game-sdk-unity/                 # C# SDK for Unity
│   └── game-sdk-godot/                 # C# SDK for Godot
│
├── contracts/                          # Automated agreements ✅ STUBBED
│   ├── governance/                     # Cooperative governance
│   ├── tokens/                         # Equity share factories
│   ├── ico/                            # Fundraising lifecycle
│   ├── exchange/                       # DEX/swap contracts
│   └── escrow/                         # Task payment escrow
│
├── documentation/                      # All documentation
│   ├── ARCHITECTURE.md                 # This file
│   └── ROADMAP.md                      # Development roadmap
│
├── scripts/                            # Development scripts ✅ NEW
│   ├── dev-up.sh                       # Bash: Start dev environment
│   ├── dev-down.sh                     # Bash: Tear down environment
│   ├── dev-up.ps1                      # PowerShell: Start dev environment
│   └── dev-down.ps1                    # PowerShell: Tear down environment
│
├── uploads/                            # Local file storage (dev only)
│
├── docker-compose.yml                  # Production compose
├── docker-compose.dev.yml              # Development compose (hot-reload) ✅ NEW
├── .env.example                        # Environment template
└── README.md                           # Project overview
```

---

## Development Scripts

### Overview

Development scripts support both Docker and Podman, with automatic runtime detection.

```
scripts/
├── dev-up.sh       # Bash - Start development environment
├── dev-down.sh     # Bash - Tear down environment
├── dev-up.ps1      # PowerShell - Start development environment
└── dev-down.ps1    # PowerShell - Tear down environment
```

### Usage

**Start Development Environment (Hot-Reload):**

```bash
# Bash (Linux/Mac/Git Bash)
./scripts/dev-up.sh

# PowerShell (Windows)
.\scripts\dev-up.ps1
```

**Tear Down Environment:**

```bash
# Bash - Basic tear down
./scripts/dev-down.sh

# Bash - Remove volumes, images, and prune
./scripts/dev-down.sh -a

# PowerShell
.\scripts\dev-down.ps1 -All
```

### Script Options

| Script     | Flag               | Description            |
| ---------- | ------------------ | ---------------------- |
| `dev-up`   | `-p, --prod`       | Use production compose |
| `dev-up`   | `-b, --build`      | Force rebuild images   |
| `dev-up`   | `-f, --foreground` | Run in foreground      |
| `dev-up`   | `-s, --service`    | Start specific service |
| `dev-down` | `-v, --volumes`    | Remove volumes         |
| `dev-down` | `-i, --images`     | Remove images          |
| `dev-down` | `-p, --prune`      | Prune unused resources |
| `dev-down` | `-a, --all`        | Remove everything      |

### Docker Compose Files

| File                     | Purpose                     |
| ------------------------ | --------------------------- |
| `docker-compose.yml`     | Production deployment       |
| `docker-compose.dev.yml` | Development with hot-reload |

### Hot-Reload Development

The development compose file (`docker-compose.dev.yml`) enables hot-reload:

- **.NET Backend**: Uses `dotnet watch` - code changes auto-reload
- **Next.js Client**: Uses `next dev` - changes auto-reload
- **Python AI Client**: Uses `uvicorn --reload`

**No rebuild needed for code changes in development mode.**

### Container Runtime Detection

Scripts automatically detect available runtime:

1. Check for `podman` first
2. Fall back to `docker`
3. Detect compose variant (`podman-compose`, `podman compose`, `docker-compose`, `docker compose`)

---

## Event-Driven Architecture

### Event Bus Implementation

The backend uses an in-memory event bus for domain event publishing and handling.

```
┌─────────────────────────────────────────────────────────────────┐
│                      EVENT BUS ARCHITECTURE                      │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  ┌─────────────┐        ┌─────────────┐        ┌─────────────┐  │
│  │  Service    │ ─────> │  Event Bus  │ ─────> │   Handler   │  │
│  │ (Publisher) │        │ (In-Memory) │        │ (Subscriber)│  │
│  └─────────────┘        └─────────────┘        └─────────────┘  │
│                                │                                 │
│                                ▼                                 │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │                     EVENT TYPES                           │   │
│  ├──────────────────────────────────────────────────────────┤   │
│  │                                                           │   │
│  │  PROJECT EVENTS:                                          │   │
│  │  • ProjectCreatedEvent                                    │   │
│  │  • ProjectUpdatedEvent                                    │   │
│  │  • ProjectStatusChangedEvent                              │   │
│  │  • ProjectDeletedEvent                                    │   │
│  │  • ProjectTaskCompletedEvent                              │   │
│  │  • ProjectMemberAddedEvent                                │   │
│  │  • ProjectMemberRemovedEvent                              │   │
│  │                                                           │   │
│  │  USER EVENTS:                                             │   │
│  │  • UserCreatedEvent                                       │   │
│  │  • UserUpdatedEvent                                       │   │
│  │  • UserVerifiedEvent                                      │   │
│  │  • UserDeletedEvent                                       │   │
│  │                                                           │   │
│  │  NOTIFICATION EVENTS:                                     │   │
│  │  • NotificationCreatedEvent                               │   │
│  │  • NotificationReadEvent                                  │   │
│  │  • NotificationsMarkedAllReadEvent                        │   │
│  │                                                           │   │
│  │  ACTIVITY EVENTS:                                         │   │
│  │  • ActivityLoggedEvent                                    │   │
│  │                                                           │   │
│  └──────────────────────────────────────────────────────────┘   │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

### Event Bus Interfaces

```csharp
// IDomainEvent - Base interface for all events
public interface IDomainEvent
{
    Guid EventId { get; }
    string EventType { get; }
    DateTime OccurredAt { get; }
}

// IEventBus - Event pub/sub interface
public interface IEventBus
{
    Task PublishAsync<TEvent>(TEvent @event, CancellationToken ct = default)
        where TEvent : IDomainEvent;

    IDisposable Subscribe<TEvent>(Func<TEvent, Task> handler)
        where TEvent : IDomainEvent;
}

// IEventHandler<T> - Handler interface
public interface IEventHandler<in TEvent> where TEvent : IDomainEvent
{
    Task HandleAsync(TEvent @event, CancellationToken ct = default);
}
```

---

## Real-Time Communication

### SignalR WebSocket Hub

The platform uses SignalR for real-time updates with group-based message routing.

```
┌─────────────────────────────────────────────────────────────────┐
│                      SIGNALR ARCHITECTURE                        │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  ┌─────────────────────────────────────────────────────────┐    │
│  │                    ARDANOVA HUB                          │    │
│  │                  /hubs/ardanova                          │    │
│  ├─────────────────────────────────────────────────────────┤    │
│  │                                                          │    │
│  │  GROUP SUBSCRIPTIONS:                                    │    │
│  │  ├── user:{userId}      User-specific events            │    │
│  │  ├── project:{projectId} Project-specific events        │    │
│  │  ├── agency:{agencyId}   Agency-specific events         │    │
│  │  └── all                 Broadcast to all clients       │    │
│  │                                                          │    │
│  │  HUB METHODS (Client → Server):                          │    │
│  │  ├── SubscribeToProject(projectId)                      │    │
│  │  ├── UnsubscribeFromProject(projectId)                  │    │
│  │  ├── SubscribeToAgency(agencyId)                        │    │
│  │  ├── UnsubscribeFromAgency(agencyId)                    │    │
│  │  ├── SubscribeToUser(userId) // Own user only           │    │
│  │  └── SubscribeToAll()                                   │    │
│  │                                                          │    │
│  │  CLIENT METHODS (Server → Client):                       │    │
│  │  ├── ReceiveEvent(eventType, payload)                   │    │
│  │  ├── UserCreated(user), UserUpdated(user)               │    │
│  │  ├── ProjectCreated(project), ProjectUpdated(project)   │    │
│  │  ├── ProjectStatusChanged(data)                         │    │
│  │  ├── TaskCompleted(task)                                │    │
│  │  ├── NotificationReceived(notification)                 │    │
│  │  └── ActivityLogged(activity)                           │    │
│  │                                                          │    │
│  └─────────────────────────────────────────────────────────┘    │
│                              │                                   │
│                              ▼                                   │
│  ┌─────────────────────────────────────────────────────────┐    │
│  │                    HUB HANDLERS                          │    │
│  │             (Event Bus → SignalR Bridge)                 │    │
│  ├─────────────────────────────────────────────────────────┤    │
│  │  ├── ProjectEventHubHandler (7 event handlers)          │    │
│  │  ├── UserEventHubHandler (4 event handlers)             │    │
│  │  ├── NotificationHubHandler (3 event handlers)          │    │
│  │  └── ActivityHubHandler (1 event handler)               │    │
│  └─────────────────────────────────────────────────────────┘    │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

### Client-Side WebSocket Hooks

```typescript
// use-realtime.ts - Real-time subscription hook
export function useRealtime<T>(eventType: string, handler: (data: T) => void) {
  useEffect(() => {
    const connection = getSignalRConnection();
    connection.on(eventType, handler);
    return () => connection.off(eventType, handler);
  }, [eventType, handler]);
}

// use-event-subscription.ts - Event subscription hook
export function useEventSubscription(projectId: string) {
  useEffect(() => {
    const connection = getSignalRConnection();
    connection.invoke("SubscribeToProject", projectId);
    return () => connection.invoke("UnsubscribeFromProject", projectId);
  }, [projectId]);
}
```

---

## File Storage

### Storage Architecture

The platform uses S3-compatible storage for file uploads with database metadata tracking.

```
┌─────────────────────────────────────────────────────────────────┐
│                      STORAGE ARCHITECTURE                        │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  ┌─────────────────────────────────────────────────────────┐    │
│  │                   IStorageService                        │    │
│  │              (Cloud Storage Abstraction)                 │    │
│  ├─────────────────────────────────────────────────────────┤    │
│  │  • GetPresignedUploadUrlAsync(request, userId)          │    │
│  │  • GetPresignedDownloadUrlAsync(bucketPath)             │    │
│  │  • UploadAsync(stream, fileName, contentType)           │    │
│  │  • DeleteAsync(bucketPath)                              │    │
│  │  • ExistsAsync(bucketPath)                              │    │
│  │  • GetPublicUrl(bucketPath)                             │    │
│  │  • GetMimeType(contentType)                             │    │
│  │  • CopyAsync(source, destination)                       │    │
│  └─────────────────────────────────────────────────────────┘    │
│                              │                                   │
│              ┌───────────────┴───────────────┐                  │
│              ▼                               ▼                   │
│  ┌───────────────────────┐      ┌───────────────────────┐       │
│  │   S3StorageService    │      │  LocalStorageService  │       │
│  │  (Production/Cloud)   │      │   (Development)       │       │
│  ├───────────────────────┤      ├───────────────────────┤       │
│  │  • AWS S3             │      │  • Local filesystem   │       │
│  │  • MinIO              │      │  • ./uploads folder   │       │
│  │  • Cloudflare R2      │      │  • Direct URLs        │       │
│  │  • DigitalOcean Spaces│      │                       │       │
│  └───────────────────────┘      └───────────────────────┘       │
│                                                                  │
│  ┌─────────────────────────────────────────────────────────┐    │
│  │                  IAttachmentService                      │    │
│  │              (Database Metadata CRUD)                    │    │
│  ├─────────────────────────────────────────────────────────┤    │
│  │  • GetByIdAsync(id)                                     │    │
│  │  • GetByUserIdAsync(userId)                             │    │
│  │  • CreateAsync(dto)                                     │    │
│  │  • UpdateLastUsedAsync(id)                              │    │
│  │  • DeleteAsync(id)                                      │    │
│  │  • GetByTypeAsync(mimeType)                             │    │
│  │  • GetByBucketPathAsync(path)                           │    │
│  └─────────────────────────────────────────────────────────┘    │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

### Storage Configuration

The storage service supports both `appsettings.json` format and flat environment variables.

**Environment Variables (Recommended for Docker/Production):**

| Environment Variable     | Description                    | Default                       |
| ------------------------ | ------------------------------ | ----------------------------- |
| `STORAGE_PROVIDER`       | Storage provider (S3, Local)   | `Local`                       |
| `S3_ACCESS_KEY`          | AWS Access Key ID              | -                             |
| `S3_SECRET_KEY`          | AWS Secret Access Key          | -                             |
| `S3_BUCKET_NAME`         | S3 bucket name                 | -                             |
| `S3_REGION`              | AWS region                     | `us-east-1`                   |
| `S3_SERVICE_URL`         | Custom S3 endpoint (MinIO, R2) | -                             |
| `S3_USE_PATH_STYLE`      | Path-style addressing          | `false`                       |
| `S3_PUBLIC_BASE_URL`     | CDN/custom public URL          | -                             |
| `LOCAL_STORAGE_PATH`     | Local storage path             | `./uploads`                   |
| `LOCAL_STORAGE_BASE_URL` | Local public URL               | `http://localhost:8080/files` |

**Configuration Binding:**

The `StorageServiceExtensions` uses a custom binding approach to support both formats:

```csharp
// Reads from environment variable first, falls back to appsettings path
private static string? GetConfigValue(IConfiguration configuration, string envVarName, string settingsPath)
{
    var envValue = Environment.GetEnvironmentVariable(envVarName);
    if (!string.IsNullOrEmpty(envValue))
        return envValue;
    return configuration[settingsPath];
}
```

**appsettings.json Format:**

```json
{
  "Storage": {
    "Provider": "S3",
    "DefaultExpirationMinutes": 60,
    "MaxFileSizeBytes": 104857600,
    "AllowedExtensions": [],
    "S3": {
      "Region": "us-east-1",
      "BucketName": "ardanova-uploads",
      "AccessKeyId": "",
      "SecretAccessKey": "",
      "ServiceUrl": "",
      "ForcePathStyle": false,
      "PublicBaseUrl": ""
    }
  }
}
```

### Upload Flow

```
┌──────────┐    ┌──────────┐    ┌──────────┐    ┌──────────┐
│  Client  │    │   API    │    │  Storage │    │    S3    │
│  (Web)   │    │Controller│    │  Service │    │  Bucket  │
└────┬─────┘    └────┬─────┘    └────┬─────┘    └────┬─────┘
     │               │               │               │
     │ POST /upload-url              │               │
     │──────────────>│               │               │
     │               │ GetPresignedUploadUrl         │
     │               │──────────────>│               │
     │               │               │ Generate URL  │
     │               │               │──────────────>│
     │               │               │<──────────────│
     │ { uploadUrl, bucketPath }     │               │
     │<──────────────│               │               │
     │               │               │               │
     │ PUT to presigned URL          │               │
     │───────────────────────────────────────────────>
     │               │               │               │
     │ POST /attachments (register)  │               │
     │──────────────>│               │               │
     │               │ CreateAsync   │               │
     │               │──────────────>│               │
     │ { attachment record }         │               │
     │<──────────────│               │               │
```

---

## Application Architecture

### Client Application (ardanova-client)

The main platform is a Next.js 15 application using the App Router pattern.

```
┌─────────────────────────────────────────────────────────────────┐
│                    ARDANOVA-CLIENT ARCHITECTURE                  │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  ┌─────────────────────────────────────────────────────────┐    │
│  │                     APP ROUTES                           │    │
│  │  /auth, /dashboard, /projects, /dao, /studio,           │    │
│  │  /exchange, /agent, /marketplace (explorer retired)      │    │
│  └────────────────────────┬────────────────────────────────┘    │
│                           │                                      │
│          ┌────────────────┼────────────────┐                    │
│          ▼                ▼                ▼                    │
│  ┌─────────────┐   ┌─────────────┐   ┌─────────────┐           │
│  │ tRPC Client │   │  SignalR    │   │   Storage   │           │
│  │(React Query)│   │  Hooks      │   │   Client    │           │
│  └──────┬──────┘   └──────┬──────┘   └──────┬──────┘           │
│         │                 │                 │                    │
│         └─────────────────┴─────────────────┘                    │
│                           │                                      │
│                           ▼                                      │
│  ┌─────────────────────────────────────────────────────────┐    │
│  │                    API CLIENT                            │    │
│  │  lib/api/ardanova/                                       │    │
│  │  • Base HTTP client with API key auth                   │    │
│  │  • Typed endpoints for all resources                    │    │
│  │  • Error handling & retries                             │    │
│  └─────────────────────────────────────────────────────────┘    │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

### Backend Architecture (.NET 10)

```
┌─────────────────────────────────────────────────────────────────┐
│                    .NET BACKEND ARCHITECTURE                     │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  ┌─────────────────────────────────────────────────────────┐    │
│  │                    ArdaNova.API                          │    │
│  │  • 14 REST Controllers                                  │    │
│  │  • API Key Middleware                                   │    │
│  │  • Exception Handling Middleware                        │    │
│  │  • SignalR Hub (ArdaNovaHub)                           │    │
│  │  • Event Bus (InMemoryEventBus)                        │    │
│  └─────────────────────────────────────────────────────────┘    │
│                              │                                   │
│  ┌───────────────────────────┼───────────────────────────┐      │
│  │                           │                           │      │
│  ▼                           ▼                           ▼      │
│  ┌──────────────┐   ┌──────────────┐   ┌──────────────┐        │
│  │ArdaNova.MCP  │   │ArdaNova.     │   │ArdaNova.     │        │
│  │(40+ Tools)   │   │Application   │   │Infrastructure│        │
│  └──────────────┘   └──────────────┘   └──────────────┘        │
│                              │                  │               │
│                     ┌────────┴────────┐   ┌────┴────────┐      │
│                     │ 28+ Services    │   │  DbContext  │      │
│                     │ Result<T>       │   │  Repository │      │
│                     │ AutoMapper      │   │  S3 Storage │      │
│                     └─────────────────┘   └─────────────┘      │
│                                                  │               │
│  ┌─────────────────────────────────────────────────────────┐    │
│  │                    ArdaNova.Domain                       │    │
│  │  60+ Entities, Enums, Value Objects                     │    │
│  └─────────────────────────────────────────────────────────┘    │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

---

## Data Flow

### Request Flow: Client → tRPC → API → Database

```
┌──────────┐    ┌──────────┐    ┌──────────┐    ┌──────────┐    ┌──────────┐
│  React   │───>│  tRPC    │───>│  tRPC    │───>│   API    │───>│  .NET    │
│Component │    │  Client  │    │  Server  │    │  Client  │    │  Backend │
└──────────┘    └──────────┘    └──────────┘    └──────────┘    └────┬─────┘
                                                                      │
                                                        ┌─────────────┤
                                                        ▼             ▼
                                                 ┌──────────┐  ┌──────────┐
                                                 │PostgreSQL│  │    S3    │
                                                 └──────────┘  └──────────┘
```

This is the path for everything that is **not** a financial workflow (reads/writes on
projects, guilds, events, social graph, etc.). tRPC routers never talk to Prisma for this
data — they proxy to `apiClient`, which calls the .NET API.

### Financial Request Flow: Client → tRPC → API → AZOA → Algorand

```
┌──────────┐    ┌──────────┐    ┌──────────┐    ┌──────────────────┐    ┌──────────┐    ┌──────────┐
│  React   │───>│  tRPC    │───>│   API    │───>│  ArdaNova.        │───>│   AZOA    │───>│ Algorand │
│Component │    │ (server) │    │  Client  │    │  Infrastructure/  │    │(external) │    │ (chain)  │
└──────────┘    └──────────┘    └──────────┘    │  Azoa/ (scoped    │    └──────────┘    └──────────┘
                                                 │  typed clients)   │
                                                 └──────────────────┘
```

Value-bearing operations (custody, minting, allocations, cash-out, quests, …) never reach
Prisma or Algorand directly from the client — they cross into AZOA only from
`ArdaNova.Infrastructure/Azoa/`, which is the only code that holds an AZOA credential. See
[Financial workflows via AZOA](#financial-workflows-via-azoa). The client's own `azoa-sdk`
layer bypasses the .NET API for **reads only** (wallets/portfolios/holons/action run-state),
using the signed-in user's own bearer token — never the tenant key — so it cannot originate a
write even if a write method were called.

### Real-Time Event Flow

```
┌──────────┐    ┌──────────┐    ┌──────────┐    ┌──────────┐    ┌──────────┐
│  Service │───>│ Event    │───>│   Hub    │───>│ SignalR  │───>│  Client  │
│  Action  │    │   Bus    │    │ Handler  │    │WebSocket │    │  (React) │
└──────────┘    └──────────┘    └──────────┘    └──────────┘    └──────────┘
     │                                                                │
     │  ProjectService.CreateAsync()                                  │
     │       │                                                        │
     │       ├── Publish(ProjectCreatedEvent)                        │
     │       │         │                                              │
     │       │         └── ProjectEventHubHandler.Handle()           │
     │       │                  │                                     │
     │       │                  └── Hub.Clients.Group().ProjectCreated()
     │       │                                   │                    │
     │       │                                   └────────────────────┘
     │       │                                      ReceiveEvent()
```

---

## Database Design

### Schema Overview

The database schema is organized into 10 modules with 70+ entities:

1. **Authentication & User Core** - Users, accounts, sessions, verification
2. **Gamification & Reputation** - XP, achievements, streaks, leaderboards
3. **Project Management & Governance** - Projects, tasks, roadmaps, proposals
4. **Guild Module** - Guilds, members, bids, reviews
5. **Marketplace & Shop** - Shops, products, invoices, sales
6. **Finance & Tokenomics** - ProjectShares (economic rights), treasury, escrow, ICO, liquidity
   6a. **Dual-Asset Model** - MembershipCredential (governance rights) + ProjectShare (economic rights) separation
7. **Engagement & Communication** - Posts, comments, chat, attachments
8. **Events Module** - Events, attendees, co-hosts, reminders
9. **Social & Follow Module** - User/Project/Guild following

### Key Schema Relationships

| Parent               | Child                     | Relationship                            |
| -------------------- | ------------------------- | --------------------------------------- |
| Project              | Roadmap                   | 1:1                                     |
| Roadmap              | RoadmapPhase              | 1:N                                     |
| RoadmapPhase         | Epic                      | 1:N                                     |
| Epic                 | PBI                       | 1:N                                     |
| PBI                  | BacklogItem               | 1:N                                     |
| BacklogItem          | ProjectTask               | 1:N                                     |
| Sprint               | ProjectTask               | N:M (via SprintItem)                    |
| Project              | ProjectMember             | 1:N                                     |
| Project              | ProjectInvitation         | 1:N                                     |
| Project              | ProjectMembershipRequest  | 1:N                                     |
| Project              | ProjectFollow             | 1:N                                     |
| Project              | Proposal                  | 1:N                                     |
| Project              | ProjectToken              | 1:1                                     |
| Project              | MembershipCredential      | 1:N                                     |
| Guild                | MembershipCredential      | 1:N                                     |
| Project              | Event                     | 1:N                                     |
| ProjectToken         | ICO                       | 1:1                                     |
| MembershipCredential | User                      | N:1                                     |
| MembershipCredential | Project                   | N:1                                     |
| MembershipCredential | Proposal                  | N:1 (optional, if granted via DAO vote) |
| Guild                | GuildMember               | 1:N                                     |
| Guild                | GuildInvitation           | 1:N                                     |
| Guild                | GuildApplication          | 1:N                                     |
| Guild                | GuildFollow               | 1:N                                     |
| Guild                | Event                     | 1:N                                     |
| Event                | EventAttendee             | 1:N                                     |
| Event                | EventCoHost               | 1:N                                     |
| Event                | EventReminder             | 1:N                                     |
| User                 | UserFollow (as follower)  | 1:N                                     |
| User                 | UserFollow (as following) | 1:N                                     |
| User                 | XPEvent                   | 1:N                                     |
| User                 | UserAchievement           | 1:N                                     |
| User                 | Attachment                | 1:N                                     |

### Membership Management Models

#### Project Membership

```
┌─────────────────────────────────────────────────────────────────┐
│                  PROJECT MEMBERSHIP FLOW                         │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  INVITATION FLOW (Project → User):                              │
│  ┌──────────────┐    ┌──────────────┐    ┌──────────────┐      │
│  │   Project    │───>│  Invitation  │───>│    User      │      │
│  │   Leader     │    │   Created    │    │   Invited    │      │
│  └──────────────┘    └──────────────┘    └──────┬───────┘      │
│                                                  │               │
│                           ┌──────────────────────┼──────────┐   │
│                           │                      │          │   │
│                           ▼                      ▼          ▼   │
│                    ┌──────────┐          ┌──────────┐ ┌────────┐│
│                    │ ACCEPTED │          │ DECLINED │ │EXPIRED ││
│                    └────┬─────┘          └──────────┘ └────────┘│
│                         │                                       │
│                         ▼                                       │
│                  ┌──────────────┐                               │
│                  │ProjectMember │                               │
│                  │   Created    │                               │
│                  └──────────────┘                               │
│                                                                  │
│  APPLICATION FLOW (User → Project):                             │
│  ┌──────────────┐    ┌──────────────┐    ┌──────────────┐      │
│  │    User      │───>│ Application  │───>│   Project    │      │
│  │   Applies    │    │   Created    │    │   Leader     │      │
│  └──────────────┘    └──────────────┘    └──────┬───────┘      │
│                                                  │               │
│                           ┌──────────────────────┼──────────┐   │
│                           │                      │          │   │
│                           ▼                      ▼          ▼   │
│                    ┌──────────┐          ┌──────────┐ ┌────────┐│
│                    │ APPROVED │          │ REJECTED │ │WITHDRAWN│
│                    └────┬─────┘          └──────────┘ └────────┘│
│                         │                                       │
│                         ▼                                       │
│                  ┌──────────────┐                               │
│                  │ProjectMember │                               │
│                  │   Created    │                               │
│                  └──────────────┘                               │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

**ProjectInvitation Table:**
| Field | Type | Description |
|-------|------|-------------|
| id | string | Primary key (cuid) |
| projectId | string | FK to Project |
| invitedById | string | FK to User (inviter) |
| invitedUserId | string? | FK to User (if registered) |
| invitedEmail | string? | Email (for non-registered users) |
| role | ProjectRole | Role being offered |
| message | text? | Personal message |
| status | InvitationStatus | PENDING, ACCEPTED, DECLINED, EXPIRED, CANCELLED |
| token | string? | Unique invitation token |
| expiresAt | datetime? | Expiration timestamp |
| createdAt | datetime | Created timestamp |
| respondedAt | datetime? | When user responded |

**ProjectMembershipRequest Table:**
| Field | Type | Description |
|-------|------|-------------|
| id | string | Primary key (cuid) |
| projectId | string | FK to Project |
| userId | string | FK to User (applicant) |
| requestedRole | ProjectRole | Desired role |
| message | text | Application message |
| skills | text? | Relevant skills |
| motivation | text? | Why they want to join |
| portfolio | text? | Portfolio links |
| status | MembershipRequestStatus | PENDING, APPROVED, REJECTED, WITHDRAWN |
| reviewedById | string? | FK to User (reviewer) |
| reviewMessage | text? | Feedback from reviewer |
| createdAt | datetime | Created timestamp |
| reviewedAt | datetime? | When reviewed |

#### Guild Membership

**GuildInvitation Table:**
| Field | Type | Description |
|-------|------|-------------|
| id | string | Primary key (cuid) |
| guildId | string | FK to Guild |
| invitedById | string | FK to User (inviter) |
| invitedUserId | string? | FK to User (if registered) |
| invitedEmail | string? | Email (for non-registered users) |
| role | GuildMemberRole | OWNER, ADMIN, MANAGER, MEMBER, APPRENTICE |
| message | text? | Personal message |
| status | InvitationStatus | PENDING, ACCEPTED, DECLINED, EXPIRED, CANCELLED |
| token | string? | Unique invitation token |
| expiresAt | datetime? | Expiration timestamp |
| createdAt | datetime | Created timestamp |
| respondedAt | datetime? | When user responded |

**GuildApplication Table:**
| Field | Type | Description |
|-------|------|-------------|
| id | string | Primary key (cuid) |
| guildId | string | FK to Guild |
| userId | string | FK to User (applicant) |
| requestedRole | GuildMemberRole | Desired role |
| message | text | Application message |
| skills | text? | Relevant skills |
| experience | text? | Work experience |
| portfolio | text? | Portfolio links |
| availability | string? | Time availability |
| status | MembershipRequestStatus | PENDING, APPROVED, REJECTED, WITHDRAWN |
| reviewedById | string? | FK to User (reviewer) |
| reviewMessage | text? | Feedback from reviewer |
| appliedAt | datetime | Created timestamp |
| reviewedAt | datetime? | When reviewed |

### Events System

```
┌─────────────────────────────────────────────────────────────────┐
│                      EVENTS ARCHITECTURE                         │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  ┌─────────────────────────────────────────────────────────┐    │
│  │                        EVENT                             │    │
│  │  • title, slug, description                             │    │
│  │  • type: MEETING, WORKSHOP, WEBINAR, TOWN_HALL,        │    │
│  │          CRITIQUE, HACKATHON, SOCIAL, OTHER            │    │
│  │  • visibility: PUBLIC, PROJECT_MEMBERS, GUILD_MEMBERS, │    │
│  │                 INVITE_ONLY                             │    │
│  │  • status: DRAFT, SCHEDULED, LIVE, COMPLETED, CANCELLED│    │
│  │  • location, locationUrl (physical)                     │    │
│  │  • isOnline, meetingUrl (virtual)                       │    │
│  │  • timezone, startDate, endDate                         │    │
│  │  • maxAttendees, attendeesCount                         │    │
│  │  • organizerId → User                                   │    │
│  │  • projectId → Project (optional)                       │    │
│  │  • guildId → Guild (optional)                          │    │
│  └─────────────────────────────────────────────────────────┘    │
│                              │                                   │
│         ┌────────────────────┼────────────────────┐             │
│         ▼                    ▼                    ▼             │
│  ┌──────────────┐    ┌──────────────┐    ┌──────────────┐      │
│  │EventAttendee │    │ EventCoHost  │    │EventReminder │      │
│  ├──────────────┤    ├──────────────┤    ├──────────────┤      │
│  │ eventId      │    │ eventId      │    │ eventId      │      │
│  │ userId       │    │ userId       │    │ userId       │      │
│  │ status:      │    │ addedAt      │    │ remindAt     │      │
│  │  INVITED     │    └──────────────┘    │ isSent       │      │
│  │  GOING       │                        │ sentAt       │      │
│  │  MAYBE       │                        └──────────────┘      │
│  │  NOT_GOING   │                                               │
│  │  ATTENDED    │                                               │
│  │ rsvpAt       │                                               │
│  │ attendedAt   │                                               │
│  │ notes        │                                               │
│  └──────────────┘                                               │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

**Event Types:**
| Type | Description |
|------|-------------|
| MEETING | General meetings, stand-ups |
| WORKSHOP | Hands-on learning sessions |
| WEBINAR | Presentation-style online events |
| TOWN_HALL | Community-wide discussions |
| CRITIQUE | Design/code review sessions |
| HACKATHON | Coding competitions |
| SOCIAL | Casual gatherings |
| OTHER | Custom event types |

### Social & Following System

```
┌─────────────────────────────────────────────────────────────────┐
│                    SOCIAL FOLLOW SYSTEM                          │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  USER FOLLOW (User → User):                                     │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │ UserFollow                                                │   │
│  │ • followerId → User (who is following)                   │   │
│  │ • followingId → User (who is being followed)             │   │
│  │ • createdAt                                               │   │
│  └──────────────────────────────────────────────────────────┘   │
│                                                                  │
│  PROJECT FOLLOW (User → Project):                               │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │ ProjectFollow                                             │   │
│  │ • userId → User                                           │   │
│  │ • projectId → Project                                     │   │
│  │ • notifyUpdates: boolean (project news)                  │   │
│  │ • notifyMilestones: boolean (milestone completions)      │   │
│  │ • notifyProposals: boolean (governance proposals)        │   │
│  │ • createdAt                                               │   │
│  └──────────────────────────────────────────────────────────┘   │
│                                                                  │
│  GUILD FOLLOW (User → Guild):                                   │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │ GuildFollow                                               │   │
│  │ • userId → User                                           │   │
│  │ • guildId → Guild                                         │   │
│  │ • notifyUpdates: boolean (guild news)                    │   │
│  │ • notifyEvents: boolean (new events)                     │   │
│  │ • notifyProjects: boolean (new project assignments)      │   │
│  │ • createdAt                                               │   │
│  └──────────────────────────────────────────────────────────┘   │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

### Dual-Asset Model (Governance + Economic)

ArdaNova uses a dual-asset architecture to separate governance rights from economic rights:

```
┌─────────────────────────────────────────────────────────────────┐
│                    DUAL-ASSET MODEL                              │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  GOVERNANCE ASSET: MembershipCredential (Soulbound)              │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │ MembershipCredential                                      │   │
│  │ • projectId → Project (nullable — project OR guild)      │   │
│  │ • guildId → Guild (nullable — project OR guild)          │   │
│  │ • userId → User                                           │   │
│  │ • assetId (Algorand ASA for soulbound credential)        │   │
│  │ • tier: BRONZE, SILVER, GOLD, PLATINUM, DIAMOND          │   │
│  │ • status: ACTIVE, REVOKED, SUSPENDED                     │   │
│  │ • isTransferable: false (always soulbound)               │   │
│  │ • grantedVia: FOUNDER, DAO_VOTE,                         │   │
│  │   CONTRIBUTION_THRESHOLD, APPLICATION_APPROVED,          │   │
│  │   GAME_SDK_THRESHOLD                                     │   │
│  │ • grantedByProposalId → Proposal (if via DAO vote)       │   │
│  │ • metadataUri (ARC-19 metadata pointer)                  │   │
│  │ • Unique constraints: (projectId, userId), (guildId, userId) │
│  │                                                           │   │
│  │ GRANTS: 1 vote in governance (equal for all members)     │   │
│  │ CANNOT: Be bought, sold, or transferred                  │   │
│  └──────────────────────────────────────────────────────────┘   │
│                                                                  │
│  ECONOMIC ASSET: ProjectShare (Fungible ASA)                    │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │ ProjectShare                                              │   │
│  │ • projectId → Project (1:1)                               │   │
│  │ • assetId (Algorand ASA for fungible token)              │   │
│  │ • totalSupply, decimals, symbol                          │   │
│  │ • allocation (founders, contributors, treasury, etc.)    │   │
│  │ • vestingConfig                                           │   │
│  │                                                           │   │
│  │ GRANTS: Proportional revenue dividends, staking rewards  │   │
│  │ CAN: Be purchased (ICO), earned (tasks/games), traded   │   │
│  │ DOES NOT: Grant governance voting power                  │   │
│  └──────────────────────────────────────────────────────────┘   │
│                                                                  │
│  RELATIONSHIP TO ProjectMember:                                  │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │ ProjectMember                                             │   │
│  │ • shareBalance: decimal (economic rights, proportional)  │   │
│  │ • votingPower: decimal (0 or 1, from MembershipCredential) │   │
│  │                                                           │   │
│  │ votingPower = 1 if user has ACTIVE MembershipCredential  │   │
│  │ votingPower = 0 otherwise                                │   │
│  │ shareBalance is independent of votingPower               │   │
│  └──────────────────────────────────────────────────────────┘   │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

**Membership Grant Flow:**

```
┌──────────────────────────────────────────────────────────────────┐
│                MEMBERSHIP CREDENTIAL GRANT FLOW                   │
├──────────────────────────────────────────────────────────────────┤
│                                                                   │
│  PATH 1: FOUNDER                                                 │
│  Project Created ──► MembershipCredential auto-issued for creator│
│                                                                   │
│  PATH 2: DAO VOTE                                                │
│  Proposal Created ──► Members Vote ──► If Passed ──► Credential Issued │
│                                                                   │
│  PATH 3: CONTRIBUTION THRESHOLD                                  │
│  Tasks Completed ──► Threshold Met ──► Credential Auto-Issued   │
│                                                                   │
│  PATH 4: APPLICATION APPROVED                                    │
│  User Applies ──► Project Reviews ──► Approved ──► Credential Issued │
│                                                                   │
│  PATH 5: GAME SDK THRESHOLD                                     │
│  Player Earns ──► Play-to-Earn Level Met ──► Credential Auto-Issued │
│                                                                   │
│  REVOCATION (requires DAO vote):                                 │
│  Proposal Created ──► 66% Quorum ──► 75% Approve ──► Credential Revoked│
│                                                                   │
└──────────────────────────────────────────────────────────────────┘
```

### Cooperative Trust Structure

ArdaNova operates as a cooperative trust entity with the following properties:

| Property               | Description                                                        |
| ---------------------- | ------------------------------------------------------------------ |
| Entity Type            | Cooperative Trust (pass-through taxation)                          |
| Securities             | Project shares are registered securities (Reg D / Reg CF / Reg A+) |
| Governance Instruments | MembershipCredentials are non-security governance instruments      |
| Revenue                | Distributed via DAO-governed treasury                              |
| Compliance             | Trust charter enforces cooperative principles                      |
| Ownership              | Platform owned by members via $ARDA token                          |

### Trending System

The platform uses an analytics-driven trending system for content discovery.

**Trending Fields (on Project, Guild, Post):**
| Field | Type | Description |
|-------|------|-------------|
| isTrending | boolean | Quick filter flag (set by analytics job) |
| trendingScore | decimal(18,8) | Calculated score for ranking |
| trendingRank | int? | Display position (1st, 2nd, 3rd, etc.) |
| trendingAt | datetime? | When item became trending |

**Trending Score Factors:**

- **Views** - Page/profile views
- **Engagement** - Likes, comments, shares
- **Recency** - Time decay for freshness
- **Growth Rate** - Velocity of engagement
- **Quality Signals** - Completion rate, verification status

**Analytics Job Workflow:**

```
┌──────────────┐    ┌──────────────┐    ┌──────────────┐
│  Calculate   │───>│   Rank Top   │───>│  Update DB   │
│   Scores     │    │   N Items    │    │   Fields     │
└──────────────┘    └──────────────┘    └──────────────┘
       │                   │                   │
       ▼                   ▼                   ▼
  Views + Likes +    Set trendingRank    isTrending = true
  Comments + Shares   1, 2, 3, ...       trendingScore = X
  + Recency Factor                       trendingAt = now()
```

### New Enums

**InvitationStatus:**
| Value | Description |
|-------|-------------|
| PENDING | Awaiting response |
| ACCEPTED | User accepted |
| DECLINED | User declined |
| EXPIRED | Invitation expired |
| CANCELLED | Inviter cancelled |

**MembershipRequestStatus:**
| Value | Description |
|-------|-------------|
| PENDING | Awaiting review |
| APPROVED | Application approved |
| REJECTED | Application rejected |
| WITHDRAWN | User withdrew application |

**GuildMemberRole:**
| Value | Level | Description |
|-------|-------|-------------|
| OWNER | 100 | Guild owner (creator) |
| ADMIN | 80 | Full administrative access |
| MANAGER | 60 | Project/member management |
| MEMBER | 40 | Standard member |
| APPRENTICE | 20 | New/learning member |

**EventStatus:**
| Value | Description |
|-------|-------------|
| DRAFT | Not yet published |
| SCHEDULED | Published, upcoming |
| LIVE | Currently happening |
| COMPLETED | Event finished |
| CANCELLED | Event cancelled |

**EventVisibility:**
| Value | Description |
|-------|-------------|
| PUBLIC | Anyone can see |
| PROJECT_MEMBERS | Project members only |
| GUILD_MEMBERS | Guild members only |
| INVITE_ONLY | Invited users only |

**AttendeeStatus:**
| Value | Description |
|-------|-------------|
| INVITED | Invited, no response |
| GOING | Confirmed attending |
| MAYBE | Tentative |
| NOT_GOING | Declined |
| ATTENDED | Marked as attended |

### New Notification Types

| Notification Type           | Trigger                       |
| --------------------------- | ----------------------------- |
| PROJECT_INVITATION          | User invited to project       |
| PROJECT_INVITATION_ACCEPTED | Invitee accepted              |
| PROJECT_INVITATION_DECLINED | Invitee declined              |
| PROJECT_MEMBERSHIP_REQUEST  | User applied to project       |
| PROJECT_MEMBERSHIP_APPROVED | Application approved          |
| PROJECT_MEMBERSHIP_REJECTED | Application rejected          |
| GUILD_INVITATION            | User invited to guild         |
| GUILD_INVITATION_ACCEPTED   | Invitee accepted              |
| GUILD_INVITATION_DECLINED   | Invitee declined              |
| GUILD_APPLICATION           | User applied to guild         |
| GUILD_APPLICATION_APPROVED  | Application approved          |
| GUILD_APPLICATION_REJECTED  | Application rejected          |
| EVENT_INVITATION            | Invited to event              |
| EVENT_REMINDER              | Upcoming event reminder       |
| EVENT_STARTING_SOON         | Event starting shortly        |
| EVENT_CANCELLED             | Event was cancelled           |
| EVENT_UPDATED               | Event details changed         |
| USER_FOLLOWED               | Someone followed you          |
| PROJECT_FOLLOWED            | Someone followed your project |
| GUILD_FOLLOWED              | Someone followed your guild   |

---

## Code Generation & EF Core

### Prisma to C# Entity Generation

The database schema is defined in DBML (`ardanova-client/prisma/database-archietecture.dbml`) which generates Prisma schema, and C# entities are generated from DBML.

**Source of Truth:** `prisma/database-archietecture.dbml` → `prisma/schema.prisma` → C# Entities

**Generator:** `ardanova-client/scripts/generate-csharp-models.ts`

### Attribute-Based EF Core Configuration

The generator uses EF Core data annotations (attributes) for all model configuration, eliminating manual Fluent API in DbContext.

**Generated Attributes:**

| Attribute                                 | Source                         | Purpose                     |
| ----------------------------------------- | ------------------------------ | --------------------------- |
| `[Table("Name")]`                         | DBML table name                | Table mapping               |
| `[Key]`                                   | DBML `pk`                      | Primary key                 |
| `[Required]`                              | DBML `not null`                | Required field              |
| `[Column(TypeName = "text")]`             | DBML `text` type               | Text column type            |
| `[Precision(18, 8)]`                      | DBML `decimal(x,y)` or default | Decimal precision           |
| `[Index(nameof(field), IsUnique = true)]` | DBML `unique`                  | Unique index                |
| `[ForeignKey("fieldId")]`                 | DBML references                | Foreign key                 |
| `[InverseProperty("Collection")]`         | Multi-FK detection             | Relationship disambiguation |

**Example Generated Entity:**

```csharp
[Index(nameof(email), IsUnique = true)]
[Table("User")]
public class User
{
    [Key]
    [Required]
    public string id { get; set; } = string.Empty;

    [Required]
    public string email { get; set; } = string.Empty;

    [Column(TypeName = "text")]
    public string? bio { get; set; }

    [Required]
    [Precision(18, 8)]
    public decimal trustScore { get; set; }

    // Multi-FK collections with descriptive names
    public virtual ICollection<Referral> ReferralsAsReferrer { get; set; } = new List<Referral>();
    public virtual ICollection<Referral> ReferralsAsReferred { get; set; } = new List<Referral>();
}
```

### Convention-Based DbContext

The DbContext uses two conventions instead of manual Fluent API configuration:

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);

    // All enums stored as strings in the database
    modelBuilder.ApplyEnumStringConvention();

    // Composite indexes and other generated configurations
    modelBuilder.ApplyGeneratedConfigurations();
}
```

**Key Files:**

| File                                                           | Purpose                                                 |
| -------------------------------------------------------------- | ------------------------------------------------------- |
| `ArdaNova.Infrastructure/Conventions/EnumStringConvention.cs`  | Converts all enum properties to strings                 |
| `ArdaNova.Infrastructure/Data/GeneratedModelConfigurations.cs` | Generated composite index configurations                |
| `ArdaNova.Domain/ArdaNova.Domain.csproj`                       | References `Microsoft.EntityFrameworkCore.Abstractions` |

### Multi-FK Handling with `[InverseProperty]` Attributes

The generator automatically handles entities with multiple foreign keys to the same type using EF Core's `[InverseProperty]` attribute. This eliminates the need for manual Fluent API configuration.

**How it works:**

1. **Detection**: The generator detects when multiple FKs from one entity point to the same target entity
2. **Descriptive Collections**: Generates named collections on the target entity (e.g., `ReferralsAsReferrer`, `ReferralsAsReferred`)
3. **InverseProperty**: Adds `[InverseProperty]` attribute on the many-side to link navigation properties with their corresponding collections

**Example Generated Code:**

```csharp
// User.cs (the "one" side - target entity)
public virtual ICollection<Referral> ReferralsAsReferrer { get; set; } = new List<Referral>();
public virtual ICollection<Referral> ReferralsAsReferred { get; set; } = new List<Referral>();

// Referral.cs (the "many" side - source entity)
[ForeignKey("referrerId")]
[InverseProperty("ReferralsAsReferrer")]
public virtual User? Referrer { get; set; }

[ForeignKey("referredId")]
[InverseProperty("ReferralsAsReferred")]
public virtual User? Referred { get; set; }
```

**Entities with Multi-FK Patterns:**

- `Referral` → User (referrerId, referredId)
- `DelegatedVote` → User (delegatorId, delegateeId)
- `ChatMessage` → User (userFromId, userToId)
- `Invoice` → User (userId, buyerId)
- `Sale` → User (userId, buyerId)
- `TaskSubmission` → User (submittedById, reviewedById)
- `TokenSwap` → ProjectToken (fromTokenId, toTokenId)
- `LiquidityPool` → ProjectToken (token1Id, token2Id)
- `ProjectTaskDependency` → ProjectTask (taskId, dependsOnId)

**Delete Behavior:** Uses EF Core defaults (Cascade for required FKs, ClientSetNull for optional). Override at database level if Restrict behavior is needed

### Running the Generator

```bash
cd ardanova-client
node --import tsx scripts/generate-csharp-models.ts

# Options:
# --dry-run    Preview changes without writing files
# --no-remove  Don't remove stale DbSets from DbContext
```

The generator:

1. Reads `prisma/database-archietecture.dbml`
2. Generates C# entities in `ArdaNova.Domain/Models/Entities/`
3. Generates C# enums in `ArdaNova.Domain/Models/Enums/`
4. Generates `GeneratedModelConfigurations.cs` for composite indexes
5. Updates `ArdaNovaDbContext.cs` with new/removed DbSets

---

## API Layer

### REST Controllers (42 controller files)

| Controller                        | Endpoints | Description                                            |
| --------------------------------- | --------- | ------------------------------------------------------ |
| `UsersController`                 | 8         | User CRUD, verification                                |
| `ProjectsController`              | 13        | Project CRUD, publishing, featured                     |
| `ActivitiesController`            | 4         | Activity logging                                       |
| `AgenciesController`              | 7         | Agency management                                      |
| `BusinessesController`            | 6         | Business operations                                    |
| `DelegatedVotesController`        | 5         | Governance voting                                      |
| `ExchangeController`              | 6         | Token swap/liquidity                                   |
| `NotificationsController`         | 5         | Notification management                                |
| `ReferralsController`             | 5         | Referral system                                        |
| `TaskEscrowsController`           | 5         | Escrow services                                        |
| `UserStreaksController`           | 4         | Gamification streaks                                   |
| `WalletsController`               | 5         | Wallet operations                                      |
| `AttachmentsController`           | 9         | File storage                                           |
| `MembershipCredentialsController` | 10        | Credential CRUD, guild queries, tier                   |
| `CredentialUtilityController`     | 6         | Grant-and-mint, revoke-and-burn, tier upgrade          |
| `KycController`                   | 6         | Retired local KYC routes return 410                    |
| `AzoaCustodialAccountController`  | 5         | Actor-bound capability, account, and KYC orchestration |

### Service Layer (52 service implementations)

| Category               | Services                                                                                                                                                                                                 |
| ---------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **User**               | UserService, AccountService, SessionService, UserSkillService, UserExperienceService                                                                                                                     |
| **Project**            | ProjectService, ProjectTaskService, ProjectResourceService, ProjectMilestoneService, ProjectSupportService, ProjectApplicationService, ProjectCommentService, ProjectUpdateService, ProjectEquityService |
| **Project Membership** | ProjectInvitationService (TODO), ProjectMembershipRequestService (TODO)                                                                                                                                  |
| **Guild**              | GuildService, GuildMemberService, ProjectBidService, GuildReviewService                                                                                                                                  |
| **Guild Membership**   | GuildInvitationService (TODO), GuildApplicationService (TODO)                                                                                                                                            |
| **Events**             | EventService (TODO), EventAttendeeService (TODO), EventReminderService (TODO)                                                                                                                            |
| **Social**             | UserFollowService (TODO), ProjectFollowService (TODO), GuildFollowService (TODO)                                                                                                                         |
| **Business**           | BusinessService, CustomerService, ProductService, InvoiceService, SaleService, InventoryItemService, MarketingCampaignService                                                                            |
| **Financial**          | WalletService, TaskEscrowService, TokenSwapService, LiquidityPoolService                                                                                                                                 |
| **Gamification**       | UserStreakService, ReferralService                                                                                                                                                                       |
| **Communication**      | NotificationService, ActivityService                                                                                                                                                                     |
| **Governance**         | DelegatedVoteService                                                                                                                                                                                     |
| **Credential Utility** | MembershipCredentialService, CredentialUtilityService, AlgorandService                                                                                                                                   |
| **KYC**                | AzoaCustodialAccountService + live Azoa-backed KycGateService; local KycService retained only for data compatibility                                                                                     |
| **Storage**            | AttachmentService                                                                                                                                                                                        |

---

## Authentication & Authorization

### Authentication Flow

```
┌──────────┐     ┌──────────┐     ┌──────────┐
│  Client  │────>│  OAuth   │────>│  Google  │
│  (Web)   │     │ Redirect │     │ Provider │
└──────────┘     └──────────┘     └────┬─────┘
                                       │
                                       ▼
                                ┌──────────┐
                                │ Callback │
                                │ NextAuth │
                                └────┬─────┘
                                     │
                        ┌────────────┼────────────┐
                        ▼            ▼            ▼
                  ┌──────────┐ ┌──────────┐ ┌──────────┐
                  │  Create  │ │  Create  │ │  Issue   │
                  │   User   │ │  Session │ │   JWT    │
                  └──────────┘ └──────────┘ └──────────┘
```

### Authorization (RBAC)

| Role             | Level | Permissions                                  |
| ---------------- | ----- | -------------------------------------------- |
| Founder          | 100   | All permissions                              |
| Leader           | 80    | Roadmap, treasury propose, member management |
| Core Contributor | 60    | Epic CRUD, task assign/review, proposals     |
| Contributor      | 40    | Task claim/submit, vote, agent use           |
| Observer         | 20    | Read-only access                             |

---

## AI & MCP Integration

### Python AI Orchestrator (FastAPI, operates Nova AI — implemented, not yet wired)

Real tree (verified against source, replacing the earlier "stubbed" placeholder tree):

```
ardanova-ai-client/
├── src/
│   ├── agents/                # Agent definitions
│   ├── api/
│   │   ├── main.py            # FastAPI app: CORS, lifespan-managed MCPClient/builders
│   │   └── routes.py          # /api/agent/{analyze-project, pitch/generate,
│   │                          #   pitch/enhance, pitch/deck}; main.py also exposes /health
│   ├── config/
│   │   └── settings.py        # App settings (port 8081, mcp_server_url default :8080)
│   ├── core/
│   │   ├── agent_builder.py   # Builds CrewAI agents from MCP tool registry
│   │   ├── crew_builder.py    # Assembles crews from agents
│   │   ├── graph_builder.py   # LangGraph graph assembly
│   │   ├── state.py           # Shared workflow state
│   │   └── tools/
│   │       ├── mcp_tool.py    # CrewAI tool wrapping MCP calls (registry caveat below)
│   │       └── gamma_tool.py  # Gamma API client for pitch decks
│   ├── crews/                 # CrewAI crews — fully built
│   │   ├── pitch_crew.py
│   │   └── project_analysis_crew.py
│   ├── mcp/
│   │   └── client.py          # MCP client — call_tool/list_tools/get_resource are still
│   │                          #   unimplemented stubs; nothing reaches the MCP server yet
│   ├── orchestrator/          # Agent routing & context
│   └── workflows/             # LangGraph workflows — nodes return placeholder data,
│       ├── pitch_generation.py#   crews are built but not yet invoked from the graph
│       └── project_analysis.py
├── AGENTS.md                  # Boundary rules + known gaps (authoritative)
├── requirements.txt
└── Dockerfile                 # EXPOSE 8081, uvicorn src.api.main:app
```

This is **FastAPI**, confirmed in `src/api/main.py` (`from fastapi import FastAPI`) — not
Sanic. The `ai-orchestrator` service block is commented out in both `docker-compose.yml` and
`docker-compose.dev.yml`, and nothing under `ardanova-client/src` calls this service, so the
client → Nova AI leg is **planned, not yet wired**, even though the FastAPI routes and CrewAI
crews themselves are real and built. See `ardanova-ai-client/AGENTS.md` for the full boundary
rules and known gaps.

### MCP Tools (42 implemented, discovery-only)

The stdio MCP host has no trusted end-user principal (`ArdaNova.MCP/Tools/AGENTS.md`), so
every tool below is a read or a non-sensitive log/notification write — no value-, role-,
credential-, or governance-mutating tool exists here. Tool files live in
`ArdaNova.MCP/Tools/*.cs`:

| Category                 | Tools (representative)                                                                                                                             |
| ------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------- |
| **User**                 | user_get_by_id, user_get_by_email, user_get_paged                                                                                                 |
| **Project**              | project_get_by_id, project_get_by_slug, project_get_all, project_get_paged, project_get_by_user, project_get_by_category, project_get_featured    |
| **Guild**                | guild_get_by_id, guild_get_by_slug, guild_get_paged, guild_get_by_owner, guild_get_verified                                                       |
| **Wallet**               | wallet_get_by_id, wallet_get_by_user_id, wallet_get_by_address, wallet_get_primary                                                                |
| **Notification**         | notification_get_by_id, notification_get_unread, notification_get_summary, notification_create, notification_mark_read, notification_mark_all_read |
| **Activity**             | activity_get_by_id, activity_get_by_user_id, activity_get_by_project_id, activity_create                                                          |
| **Gamification**        | streak_get_by_user_id, streak_record_activity, streak_reset, referral_get_by_id, referral_get_by_referrer, referral_get_by_code, referral_complete |
| **Membership credential** | membership_credential_get_by_id, _get_by_project, _get_active_by_project, _get_by_user, _check                                                   |

`MCP_TOOL_REGISTRY` in `ardanova-ai-client/src/core/tools/mcp_tool.py` additionally lists
`escrow`, `exchange`, and delegation (`governance`) write tools — those have no matching
`*Tools.cs` file; they were intentionally removed server-side for lacking a verified caller
principal (`ArdaNova.MCP/Tools/AGENTS.md`). Treat those registry entries as aspirational, not
current capability.

---

## Ledger Integration

Financial workflows (identity/KYC, custody, minting, allocations, cash-out, quests, …) go
through **AZOA**, the external blockchain-operations node — never directly from
`ardanova-client` and never directly against Algorand. **Algorand is the target real chain**
AZOA settles value to (the golden path has so far run only on AZOA's first-class `Simulated`
chain; real chains are activation-gated node-side); ArdaNova's own `ArdaNova.Infrastructure/Algorand/` code is a narrower,
credential-specific path (below) that is itself swappable to route through AZOA.

### Financial workflows via AZOA

`ArdaNova.Infrastructure/Azoa/` is the **only** code in the platform that holds an AZOA
credential, via scoped typed HTTP clients — one per workflow family, so a leaked key cannot
cross trust boundaries:

- **Custody** (`AzoaCustodialNodeClient`, `Azoa:CustodyApiKey`) — tenant provisioning, managed
  wallets, KYC read/submit.
- **Value** (`AzoaNodeClient`, `Azoa:ValueApiKey`) — allocations, fungible-mint and its four
  consent-lifecycle steps, reserve partitions; plus `AzoaHoldClient` (`api/hold-commands`,
  `transfer:sign`) and `AzoaExchangeClient` (exchange + receipt, `swap:sign`).
- **Quest** (`AzoaQuestNodeClient`, `Azoa:QuestApiKey`) — the DAG action engine's quest
  authoring/read/signal routes.
- **Issuance** (`AzoaCredentialIssuanceClient`, `Azoa:IssuanceApiKey`) — credential minting.
- Cross-cutting: `AzoaProjectTokenNodeGateway` (needs both custody + value keys),
  `AzoaCashOutClient` (custody key), `AzoaSettlementGateway` (exchange operations, token base
  units only — never a fiat amount), `AzoaFiatRailGateway` (the one on-ramp adapter, custody
  key, `Fiat.USD` rail only).

Production enforces the four keys as **separate secrets** and rejects reuse across them; a
`Azoa:TenantApiKey` fallback exists only outside Production. Full transport rules (route
allow-lists, credential separation, error-message sanitization) live in
`ArdaNova.Infrastructure/Azoa/AGENTS.md`. The canonical integration contract — AZOA's mental
model, custody-follows-the-operator rule, and the action-engine semantics — is
`conductor/ARDANOVA-AZOA-INTEGRATION-CONTRACT.md` (byte-identical copy lives in the AZOA repo).
Its §12 holds the state machines both sides must agree on: exchange settlement (incl. the
typed-404 no-effect reopen), the hold-command door (request keys, `holdRecordId` derivation,
`ReplayConflict`), and the consent lifecycle (fungible creation, implied participation grant).
Settlement persistence and ceilings: `ArdaNova.Infrastructure/Outbox/AGENTS.md`.

### Credential Utility Service (In Progress)

The first Algorand integration: soulbound membership credentials minted as ASAs on-chain.

```
┌──────────────────────────────────────────────────────────────────┐
│                CREDENTIAL UTILITY ARCHITECTURE                    │
├──────────────────────────────────────────────────────────────────┤
│                                                                   │
│  ┌─────────────────────┐    ┌─────────────────────┐              │
│  │ IMembershipCredential│    │  IAlgorandService   │              │
│  │      Service         │    │ (Infrastructure)    │              │
│  │  (Off-chain CRUD)    │    │ (dotnet-algorand-sdk)│             │
│  └──────────┬──────────┘    └──────────┬──────────┘              │
│             │                          │                          │
│             └──────────┬───────────────┘                          │
│                        │                                          │
│                        ▼                                          │
│  ┌──────────────────────────────────────────────────────────┐    │
│  │           ICredentialUtilityService                       │    │
│  │              (Orchestrator)                                │    │
│  │                                                           │    │
│  │  • GrantAndMint → grant credential + mint soulbound ASA  │    │
│  │  • RevokeAndBurn → burn ASA + revoke credential          │    │
│  │  • UpgradeTier → update tier + update on-chain metadata  │    │
│  │  • CheckAndAutoGrant → auto-grant based on thresholds    │    │
│  │  • RetryMint → re-attempt failed ASA minting             │    │
│  └──────────────────────────────────────────────────────────┘    │
│                        │                                          │
│                        ▼                                          │
│  ┌──────────────────────────────────────────────────────────┐    │
│  │                 ALGORAND NETWORK                           │    │
│  │  • Soulbound ASAs (defaultFrozen=true, clawback=platform) │    │
│  │  • ARC-19 metadata (credential type, tier, scope)         │    │
│  │  • TestNet → MainNet                                      │    │
│  └──────────────────────────────────────────────────────────┘    │
│                                                                   │
│  DESIGN PRINCIPLES:                                               │
│  • Off-chain first — credentials work without blockchain          │
│  • Graceful degradation — chain down = credential still valid     │
│  • Custodial model — platform signs all transactions              │
│  • XOR scope — each credential: project OR guild, never both     │
│                                                                   │
└──────────────────────────────────────────────────────────────────┘
```

`IAlgorandService` is now a seam with three selectable providers (`Algorand:Provider`:
Legacy | Azoa | Simulated — see `Infrastructure/DependencyInjection.cs`): the legacy
custodial-mnemonic signer shown above, `AzoaBackedAlgorandService` (routes the same calls
through the shared AZOA node instead), and a simulated provider for environments without a
live signer. Callers (`CredentialUtilityService`, tokenomics) keep calling the same interface
unchanged regardless of which provider is active.

### Algorand Configuration

| Variable                     | Description               | Default                              |
| ---------------------------- | ------------------------- | ------------------------------------ |
| `ALGORAND_NETWORK`           | Network (testnet/mainnet) | `testnet`                            |
| `ALGORAND_NODE_URL`          | Algod API endpoint        | `https://testnet-api.algonode.cloud` |
| `ALGORAND_INDEXER_URL`       | Indexer API endpoint      | `https://testnet-idx.algonode.cloud` |
| `ALGORAND_PLATFORM_MNEMONIC` | Platform account mnemonic | (required)                           |

### Smart Contracts (PyTeal - Stubbed)

```
contracts/
├── governance/     # DAO governance (proposals, voting)
│   └── __init__.py
├── tokens/         # ASA token factories
│   └── __init__.py
├── ico/            # ICO lifecycle contracts
│   └── __init__.py
├── exchange/       # DEX/swap contracts
│   └── __init__.py
└── escrow/         # Task payment escrow
    └── __init__.py
```

### Client Integration

`ardanova-client` does **not** have a direct Algorand SDK integration — `lib/blockchain/`
(ASA token management, ICO/exchange logic, wallet connectors, indexer client) does not exist
in the repo and remains aspirational/TODO. What exists and is live today is narrower and
read-only:

```
ardanova-client/src/lib/azoa-sdk/
├── client.ts       # Browser-safe AzoaApiClient singleton, NO API key, configured from
│                   #   NEXT_PUBLIC_AZOA_BASE_URL; write methods imported but never called
├── queries.ts       # React Query wrappers (wallets, portfolios, holons, action run-state)
└── types.ts
```

Any value-bearing operation — mint, transfer, allocation, swap-execute, bridge — is
intentionally unreachable from this layer; it goes through the .NET API, per
[Financial workflows via AZOA](#financial-workflows-via-azoa).

---

## Infrastructure

> **As-built deployment** is the "Released topology" table in
> [Architecture at a glance](#architecture-at-a-glance-as-built-2026-10-26): one Railway
> project carrying the client, the BFF, Postgres and the AZOA node's services. The diagram and
> subdomains below are the original **target** layout (Vercel edge, Redis, `*.ardanova.com`);
> none of them is provisioned today, and AZOA does run in the same Railway project rather than
> "not deployed alongside ArdaNova".

### Deployment Architecture (target, not provisioned)

```
┌─────────────────────────────────────────────────────────────────┐
│                    DEPLOYMENT ARCHITECTURE                       │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  ┌─────────────────────────────────────────────────────────┐    │
│  │                  CDN (Vercel Edge)                      │    │
│  │  • Static assets, Edge functions, Image optimization    │    │
│  └─────────────────────────────────────────────────────────┘    │
│                              │                                   │
│                              ▼                                   │
│  ┌─────────────────────────────────────────────────────────┐    │
│  │                  APPLICATION TIER                        │    │
│  │  ┌────────────────────────────────────────────────────┐ │    │
│  │  │         ardanova-client (Railway/Vercel)           │ │    │
│  │  │         www.ardanova.com                           │ │    │
│  │  └────────────────────────────────────────────────────┘ │    │
│  └─────────────────────────────────────────────────────────┘    │
│                              │                                   │
│                              ▼                                   │
│  ┌─────────────────────────────────────────────────────────┐    │
│  │                    SERVICE TIER                          │    │
│  │  ┌──────────────┐  ┌──────────────┐                     │    │
│  │  │ .NET Backend │  │ Python AI    │                     │    │
│  │  │ api.ardanova │  │ ai.ardanova  │                     │    │
│  │  │ + SignalR    │  │ (Railway)    │                     │    │
│  │  │ + Storage    │  │              │                     │    │
│  │  │ (Railway)    │  │              │                     │    │
│  │  └──────────────┘  └──────────────┘                     │    │
│  └─────────────────────────────────────────────────────────┘    │
│                              │                                   │
│                              ▼                                   │
│  ┌─────────────────────────────────────────────────────────┐    │
│  │                      DATA TIER                           │    │
│  │  ┌──────────┐  ┌──────────┐  ┌──────────┐               │    │
│  │  │PostgreSQL│  │  Redis   │  │S3/R2/MinIO              │    │
│  │  │(Railway) │  │ (Upstash)│  │(Cloudflare)│             │    │
│  │  └──────────┘  └──────────┘  └──────────┘               │    │
│  └─────────────────────────────────────────────────────────┘    │
│                              │                                   │
│                              ▼                                   │
│  ┌─────────────────────────────────────────────────────────┐    │
│  │              FINANCIAL-WORKFLOW TIER (external)           │    │
│  │  AZOA — called ONLY from ArdaNova.Infrastructure/Azoa/,   │    │
│  │  not hosted by or deployed alongside ArdaNova             │    │
│  └──────────────────────────┬──────────────────────────────┘    │
│                              ▼                                   │
│  ┌─────────────────────────────────────────────────────────┐    │
│  │                   BLOCKCHAIN TIER                        │    │
│  │  Algorand (TestNet → MainNet) — the chain AZOA settles to │    │
│  └─────────────────────────────────────────────────────────┘    │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

### Subdomains

| Subdomain        | Service                  | Description                  |
| ---------------- | ------------------------ | ----------------------------- |
| www.ardanova.com | ardanova-client          | Main platform (owns the UX)  |
| api.ardanova.com | ardanova-backend-api-mcp | .NET 10 API + MCP + SignalR; only tier holding AZOA credentials |
| ai.ardanova.com  | ardanova-ai-client       | FastAPI orchestrator operating Nova AI (implemented, not yet wired to `ardanova-client`) |

AZOA itself has no `ardanova.com` subdomain — it is an external, separately-hosted node called
server-side only (see [Financial workflows via AZOA](#financial-workflows-via-azoa)); ArdaNova
exposes no explorer or AZOA-facing public endpoint of its own.

---

## Security

### Security Measures

1. **Authentication**
   - OAuth 2.0 with Google (NextAuth)
   - API Key authentication for backend
   - JWT tokens for API calls

2. **Authorization**
   - Role-based access control (RBAC)
   - Permission checks in tRPC procedures
   - Token-weighted permissions for DAO

3. **Data Protection**
   - Encryption at rest (database)
   - Encryption in transit (TLS 1.3)
   - Sensitive data hashing
   - Presigned URLs for file access

4. **API Security**
   - Input validation (Zod schemas)
   - SQL injection prevention (EF Core)
   - Rate limiting
   - CORS configuration

5. **Identity Verification (KYC)**
   - Sensitive gates require a fresh authoritative Azoa approval; a local PRO/EXPERT field is insufficient
   - ArdaNova stores only tenant-bound avatar/wallet references and normalized status
   - The tenant GUID comes only from trusted deploy-time configuration; user requests cannot select it, and mismatched node responses are rejected
   - Local submission/review routes are retired; provider selection and review live in Azoa
   - Public document URLs are not accepted by the ArdaNova user experience
   - Hosted and private-upload providers remain fail-closed until their durable lifecycle is reviewed

6. **Blockchain Security**
   - Soulbound ASAs (frozen, clawback-only, no transfer)
   - Custody follows the selected Azoa node operator; ArdaNova stores no wallet key or mnemonic
   - No credential-free client exists (the anonymous `AzoaPublicNodeClient` was deleted); separate custody, value (`nft:mint`, `token:create:sign`, `swap:sign`, `transfer:sign`), issuance, and quest (`dapp:develop`) clients prevent authority crossover
   - ARC-19 metadata for credential verification
   - Multi-sig for admin functions
   - Timelocks for governance
   - Audits before mainnet

---

## Implementation Status

### Completed ✅

| Feature                         | Status      | Location                                                      |
| ------------------------------- | ----------- | ------------------------------------------------------------- |
| .NET Backend API                | ✅ Complete | `ardanova-backend-api-mcp/`                                   |
| 42 REST Controllers             | ✅ Complete | `ArdaNova.API/Controllers/`                                   |
| 52+ Application Services        | ✅ Complete | `ArdaNova.Application/Services/`                              |
| 42 MCP Tools (discovery-only)   | ✅ Complete | `ArdaNova.MCP/`                                               |
| Event Bus                       | ✅ Complete | `ArdaNova.API/EventBus/`                                      |
| SignalR WebSocket               | ✅ Complete | `ArdaNova.API/WebSocket/`                                     |
| S3 File Storage                 | ✅ Complete | `ArdaNova.Infrastructure/Storage/`                            |
| Env Var Config Binding          | ✅ Complete | `StorageServiceExtensions.cs`                                 |
| Attachment Controller           | ✅ Complete | `ArdaNova.API/Controllers/AttachmentsController.cs`           |
| C# Model Generator              | ✅ Complete | `ardanova-client/scripts/generate-csharp-models.ts`           |
| EF Core Conventions             | ✅ Complete | `ArdaNova.Infrastructure/Conventions/`                        |
| Dev Scripts (Bash/PS)           | ✅ Complete | `scripts/`                                                    |
| Docker Compose (Dev)            | ✅ Complete | `docker-compose.dev.yml`                                      |
| AI Client (FastAPI, Nova AI)    | 🔄 Implemented, not wired | `ardanova-ai-client/` — real routes + CrewAI/LangGraph built; MCP client is a stub and `ardanova-client` has no caller into it yet |
| Smart Contracts Stub            | ✅ Stubbed  | `contracts/`                                                  |
| **Project Invitations**         | ✅ Complete | `ArdaNova.Domain/Models/Entities/ProjectInvitation.cs`        |
| **Project Membership Requests** | ✅ Complete | `ArdaNova.Domain/Models/Entities/ProjectMembershipRequest.cs` |
| **Guild Invitations**           | ✅ Complete | `ArdaNova.Domain/Models/Entities/GuildInvitation.cs`          |
| **Guild Applications**          | ✅ Complete | `ArdaNova.Domain/Models/Entities/GuildApplication.cs`         |
| **Events System**               | ✅ Complete | `ArdaNova.Domain/Models/Entities/Event.cs`                    |
| **Event Attendees**             | ✅ Complete | `ArdaNova.Domain/Models/Entities/EventAttendee.cs`            |
| **Event Co-Hosts**              | ✅ Complete | `ArdaNova.Domain/Models/Entities/EventCoHost.cs`              |
| **Event Reminders**             | ✅ Complete | `ArdaNova.Domain/Models/Entities/EventReminder.cs`            |
| **User Following**              | ✅ Complete | `ArdaNova.Domain/Models/Entities/UserFollow.cs`               |
| **Project Following**           | ✅ Complete | `ArdaNova.Domain/Models/Entities/ProjectFollow.cs`            |
| **Guild Following**             | ✅ Complete | `ArdaNova.Domain/Models/Entities/GuildFollow.cs`              |
| **Trending System**             | ✅ Complete | Added to Project, Guild, Post entities                        |
| **MembershipCredential Entity** | ✅ Complete | `ArdaNova.Domain/Models/Entities/MembershipCredential.cs`     |
| **MembershipCredential Enums**  | ✅ Complete | `MembershipCredentialStatus`, `MembershipGrantType`           |
| **Dual-Asset Model Schema**     | ✅ Complete | DBML updated with governance/economic separation              |

### Database Schema Stats

| Metric                | Count |
| --------------------- | ----- |
| Total Entities        | 75+   |
| Total Enums           | 48+   |
| Schema Modules        | 10    |
| New Tables (Jan 2025) | 11    |
| New Enums (Jan 2025)  | 6     |

### In Progress 🔄

| Feature                         | Status         | Notes                                                              |
| ------------------------------- | -------------- | ------------------------------------------------------------------ |
| **Credential Utility Service**  | 🔄 In Progress | Soulbound ASA minting, guild credentials, tier system              |
| **Algorand Integration (.NET)** | 🔄 In Progress | `dotnet-algorand-sdk` for on-chain credential operations           |
| **Guild Credentials**           | 🔄 In Progress | Extend MembershipCredential with guildId, XOR validation           |
| **Credential Tier System**      | 🔄 In Progress | BRONZE → DIAMOND progression tracked on-chain                      |
| **Azoa Identity & Custody**     | 🔄 In Progress | Tenant orchestration complete; production custody/provider pending |
| **KYC Gate Service**            | ✅ Implemented | Fresh Azoa approval required by Project/Credential services        |
| Membership Services             | 🔄 TODO        | CRUD services for invitations/applications                         |
| Events Services                 | 🔄 TODO        | CRUD services for events system                                    |
| Following Services              | 🔄 TODO        | CRUD services for social following                                 |
| Trending Analytics Job          | 🔄 TODO        | Background job to calculate trending scores                        |
| Governance UI (`app/governance/`) | 🔄 In Progress | Governance proposals, voting — real pages exist, not a stub        |
| Studio UI (`app/studio/`)       | 🔄 In Progress | Gamma API integration — real pages exist, not a stub               |
| Swap UI (`app/swap/`)           | 🔄 In Progress | Token swap — real pages exist, not a stub                          |
| Explorer UI                     | ⛔ Retired     | Not planned — AZOA is the custody/chain boundary, not an ArdaNova-hosted surface   |
| Member chat UI (`app/chats/`)   | ✅ Active      | Member-to-member messaging via the .NET chat endpoints; no Nova AI chat surface exists yet |
| Game SDK                        | 🔄 TODO        | Unity & Godot SDKs                                                 |

### Known Issues

| Issue | Workaround | Status |
| ----- | ---------- | ------ |

---

## Next Steps

1. ✅ **Phase 1**: .NET Backend API with Clean Architecture - **COMPLETE**
2. ✅ **Phase 2**: MCP Tools Integration (40+ tools) - **COMPLETE**
3. ✅ **Phase 3**: Event Bus & WebSocket - **COMPLETE**
4. ✅ **Phase 4**: File Storage Service - **COMPLETE**
5. ✅ **Phase 5**: AI Client & Contracts Stubs - **COMPLETE**
6. ✅ **Phase 5.1**: Dev Scripts & Docker Hot-Reload - **COMPLETE**
7. ✅ **Phase 5.2**: Membership Management (Invitations/Applications) - **COMPLETE**
8. ✅ **Phase 5.3**: Events System - **COMPLETE**
9. ✅ **Phase 5.4**: Social Following System - **COMPLETE**
10. ✅ **Phase 5.5**: Trending System - **COMPLETE**
11. 🔄 **Phase 6**: Backend Services for new entities (invitations, events, follows)
12. 🔄 **Phase 7**: Consolidate DAO, Studio, Exchange into client (Explorer retired, not planned)
13. 🔄 **Phase 8**: Blockchain integration (Algorand) — **IN PROGRESS** (Credential Utility Service with `dotnet-algorand-sdk`)
14. **Phase 9**: Smart contract development (AVM/TEAL for governance automation)
15. **Phase 10**: Game SDK (Unity, Godot)

See [ROADMAP.md](./ROADMAP.md) for detailed timelines.

---

**Last Updated**: 2026-10-26 (SR-02: as-built four-tier architecture with AZOA as settlement/custody node; earlier sections retained, superseded where they disagree)
**Version**: 6.0.0 (AZOA slot-in, golden path live on the tandem stack)
