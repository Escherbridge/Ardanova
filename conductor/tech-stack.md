# ArdaNova Tech Stack

## Overview
- **Frontend**: Next.js 15 (App Router), TypeScript, TailwindCSS, tRPC
- **Backend**: C# .NET 9, EF Core, SignalR
- **Database**: PostgreSQL (Managed via Prisma)
- **AI Integration**: Gamma API + MCP Server (C#)

## Architecture

### .NET Backend (Business Logic)
- **Location**: `ardanova-backend-api-mcp/api-server/`
- **Pattern**: Clean Architecture (Domain → Application → Infrastructure → API)
- **Services**: All business logic in `ArdaNova.Application/Services/`
- **Controllers**: REST API endpoints in `ArdaNova.API/Controllers/`
- **Tests**: `tests/ArdaNova.Application.Tests/` and `tests/ArdaNova.API.Tests/`

### Next.js Frontend (UI + Thin API Layer)
- **Location**: `ardanova-client/`
- **tRPC Routers**: Thin proxies that call the .NET API via `apiClient`
- **API Client**: `src/lib/api/` - typed HTTP client for .NET backend
- **Auth**: NextAuth.js v5 with Google OAuth (JWT strategy)
- **No business logic in Next.js** - only UI, routing, and auth session management

## Key Architecture Decisions

### Database Management
- **Source of Truth**: `ardanova-client/prisma/database-architecture.dbml`
- **Schema**: All database schema changes MUST be defined in the DBML file first
- **Generators**:
  1. `npm run generate:prisma` → Converts DBML to Prisma Schema (`schema.prisma`) and pushes to DB
  2. `npm run generate:csharp` → Converts DBML/Prisma schema to C# EF Core entities in `ArdaNova.Infrastructure`

### Communication
- **Client-Server**: tRPC for type-safe API calls (Next.js frontend ↔ tRPC routers)
- **tRPC → .NET**: HTTP via `apiClient` (tRPC routers proxy to .NET API)
- **Real-time**: SignalR for WebSocket connections (notifications, chat, live updates)
- **AI-Agent**: MCP Protocol for communication between AI agents and the backend tools

### Authentication
- **Auth**: NextAuth.js (v5) with Google Provider
- **Strategy**: JWT with claims: `userId`, `email`, `role`, `userType`, `verificationLevel`
- **Auth is the ONE exception** where Next.js uses Prisma directly (for user creation on sign-in)

### KYC & Identity Verification
- **Provider Abstraction**: `IKycProviderService` interface (Strategy pattern)
- **Default Provider**: `ManualKycProviderService` — document upload + admin review
- **Planned Provider**: Veriff SDK (`VeriffKycProviderService`) — behind `KYC_PROVIDER=veriff` feature flag
- **Gate Service**: `IKycGateService` — reusable PRO verification check injected into ProjectService, MembershipCredentialService
- **Level Gating**: ANONYMOUS → VERIFIED (email) → PRO (KYC) → EXPERT (future)

## Blockchain (Algorand)

### Algorand Integration
- **SDK**: dotnet-algorand-sdk (C# native, NuGet package in .NET backend)
- **Network**: Algorand TestNet → MainNet
- **Node**: AlgoNode (`https://testnet-api.algonode.cloud`)
- **Indexer**: AlgoNode Indexer (`https://testnet-idx.algonode.cloud`)
- **Credentials**: Soulbound ASAs (ARC-19 metadata, `defaultFrozen=true`, clawback-only)
- **Pattern**: Custodial — platform account signs all blockchain transactions
- **Config**: Environment variables (`ALGORAND_NETWORK`, `ALGORAND_NODE_URL`, `ALGORAND_INDEXER_URL`, `ALGORAND_PLATFORM_MNEMONIC`)

### Credential Architecture
- **Soulbound ASAs**: Non-transferable Algorand Standard Assets representing membership credentials
- **ARC-19 Metadata**: On-chain metadata with credential type, tier, grant method, scope (project/guild)
- **Graceful Degradation**: Credentials always granted off-chain first; blockchain minting is additive
- **Tier System**: Reuses `UserTier` enum (BRONZE → SILVER → GOLD → PLATINUM → DIAMOND)

### Tokenomics Architecture
- **ARDA Token**: Platform-wide fungible ASA serving as universal exchange medium
- **Project Tokens**: Per-project fungible ASAs representing equity shares (fixed supply = 100%)
- **Exchange Model**: Deterministic treasury-backed rates (not market/AMM-based)
- **Fiat Integration**: Stripe for USD crowdfunding (in) and Stripe Connect for payouts (out)
- **Equity Model**: Tasks assigned equity percentages; contributors earn project tokens on task completion
- **Conversion Path**: Project Tokens → ARDA → USDCa → USD (all abstracted from users)

### Key Blockchain Services
| Service | Location | Purpose |
|---------|----------|---------|
| `IAlgorandService` | `ArdaNova.Infrastructure/Algorand/` | Algorand SDK wrapper (mint, burn, verify, fungible ASA ops) |
| `ICredentialUtilityService` | `ArdaNova.Application/Services/` | Orchestrates grant-and-mint, revoke-and-burn |
| `IMembershipCredentialService` | `ArdaNova.Application/Services/` | Off-chain credential CRUD |
| `IProjectTokenService` | `ArdaNova.Application/Services/` | Project equity token management |
| `ITokenBalanceService` | `ArdaNova.Application/Services/` | Token balance tracking |
| `IExchangeService` | `ArdaNova.Application/Services/` | Deterministic exchange rate calculation |
| `IPayoutService` | `ArdaNova.Application/Services/` | Token → USD payout processing |

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
