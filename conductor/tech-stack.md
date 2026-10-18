---
type: tech-stack
---

# ArdaNova Tech Stack

## Overview
Four tiers. UX, API/MCP, and AI orchestration are ArdaNova's own code; financial workflows run
on Azoa, an external node ArdaNova is a tenant of.

| Tier | Framework | Owns |
|------|-----------|------|
| 1. `ardanova-client/` | Next.js 15.2.3, React 19, TypeScript 5.8 | User experience |
| 2. `ardanova-backend-api-mcp/` | .NET 10 (net10.0), EF Core 10, SignalR | HTTP API (BFF) + MCP server, business logic, Azoa credentials |
| 3. Azoa (external repo) | — | Financial workflows: custody, minting, exchange, cash-out |
| 4. `ardanova-ai-client/` | FastAPI (Python 3.11), Anthropic, CrewAI, LangGraph | Nova AI orchestration — **built, not yet wired** |

## Architecture

### Tier 1 — `ardanova-client/` (User Experience)
- **Framework**: Next.js 15.2.3 (App Router), React 19, TypeScript 5.8, TailwindCSS 4, tRPC 11
- **Pattern**: UI, routing, and session management only — thin proxies, no business logic
- **tRPC Routers**: Thin proxies that call the .NET API via `apiClient`
- **API Client**: `src/lib/api/` — typed HTTP client for the .NET backend
- **Auth**: NextAuth.js 5.0.0-beta.31 with Google OAuth (JWT strategy); Prisma 6.19.3 is used
  here ONLY for auth/session persistence — no other Prisma writes in this tier
- **Azoa read layer**: `src/lib/azoa-sdk/client.ts` wraps the published npm `azoa-sdk` (^0.4.0) —
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

### Tier 3 — Azoa (external financial-workflow node)
- **What it is**: a generic, self-hostable blockchain-operations node, maintained in a
  **separate repo**. ArdaNova is a tenant on it, never its operator.
- **What it executes**: identity/KYC, custodial accounts & managed wallets, NFT/fungible
  minting, allocations, reserve partitions, project tokens, credential issuance, cash-out, and
  its DAG action engine for multi-step value workflows.
- **Called from**: server-side only, from `ArdaNova.Infrastructure/Azoa/` (Tier 2), via scoped
  typed clients — custody, value, issuance, and quest keys are separate secrets, enforced in
  Production. Never called from the browser for writes.
- **Contract**: `conductor/ARDANOVA-AZOA-INTEGRATION-CONTRACT.md` (byte-identical with Azoa's
  copy); transport rules: `ardanova-backend-api-mcp/api-server/src/ArdaNova.Infrastructure/Azoa/AGENTS.md`.
- **Custody and chain inspection**: custody follows the node operator; chain state is inspected
  through Azoa operator tooling — ArdaNova hosts no explorer. User-facing copy never says "Azoa".

### Tier 4 — `ardanova-ai-client/` (Nova AI orchestration)
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
Browser → `ardanova-client` (Tier 1, UX) → tRPC → .NET API (Tier 2, BFF) → PostgreSQL / Azoa
(Tier 3, financial workflows). Nova AI (Tier 4) is designed to call .NET MCP tools from the
client path, but is implemented-but-dormant today — see Tier 4, above, for the three verified
gaps (dev stack doesn't start it, client doesn't call it, its own MCP client is a stub).

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
- **AI-Agent**: MCP Protocol for communication between AI agents (Tier 4) and the .NET MCP tools
  (Tier 2) — see "Request flow," above, for current wiring status

### Authentication
- **Auth**: NextAuth.js (5.0.0-beta.31) with Google Provider
- **Strategy**: JWT with claims: `userId`, `email`, `role`, `userType`, `verificationLevel`
- **Auth is the ONE exception** where Next.js uses Prisma directly (for user creation on sign-in)

### KYC & Identity Verification
- **Provider Abstraction**: `IKycProviderService` interface (Strategy pattern)
- **Default Provider**: `ManualKycProviderService` — document upload + admin review
- **Planned Provider**: Veriff SDK (`VeriffKycProviderService`) — behind `KYC_PROVIDER=veriff` feature flag
- **Gate Service**: `IKycGateService` — reusable PRO verification check injected into ProjectService, MembershipCredentialService
- **Level Gating**: ANONYMOUS → VERIFIED (email) → PRO (KYC) → EXPERT (future)

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
| `ITokenBalanceService` | `ArdaNova.Application/Services/` | Token balance tracking | reads Azoa wallet/portfolio state |
| `IExchangeService` | `ArdaNova.Application/Services/` | Deterministic exchange rate calculation | via `ArdaNova.Infrastructure/Azoa/AzoaExchangeClient` |
| `IPayoutService` | `ArdaNova.Application/Services/` | Token → USD payout processing | via `ArdaNova.Infrastructure/Azoa/AzoaCashOutClient` |

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
