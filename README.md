# ArdaNova - The Decentralized Worker-Owned Cooperative Platform

<div align="center">

**Building the infrastructure for a circular, worker-owned economy powered by Algorand**

[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)
[![TypeScript](https://img.shields.io/badge/TypeScript-5.8-blue.svg)](https://www.typescriptlang.org/)
[![Next.js](https://img.shields.io/badge/Next.js-15-black.svg)](https://nextjs.org/)
[![Algorand](https://img.shields.io/badge/Algorand-ASA-black.svg)](https://algorand.com/)
[![Prisma](https://img.shields.io/badge/Prisma-6.5-2D3748.svg)](https://www.prisma.io/)

[Roadmap](./ROADMAP.md) | [Architecture](./ARCHITECTURE.md) | [Contributing](#contributing) | [Documentation](#documentation)

</div>

---

## Vision

**ArdaNova** is a **gamified, decentralized cooperative platform** that combines:

- **Gamified Talent Marketplace** - An Upwork reimagined where contributors earn ownership
- **Community Fundraising** - Launch project equity for fractional ownership
- **Multi-Modal Funding** - Fund with capital, labor, resources, crypto, or social capital
- **DAO-Governed Projects** - Transparent, participatory governance at every level
- **Worker-Owned Cooperatives** - Every project becomes a member-governed cooperative
- **Play-to-Earn Integration** - Earn ownership shares through games and engagement
- **AI-Powered Generation** - Gamma API integration for instant pitch creation
- **Game SDK** - Unity/Godot integration for gamified experiences

```
┌─────────────────────────────────────────────────────────────────┐
│                  THE DUAL-ASSET COOPERATIVE MODEL                 │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  GOVERNANCE (Voice)              ECONOMICS (Value)               │
│  ┌───────────────────────┐      ┌───────────────────────┐       │
│  │  Membership Credential │      │   Ownership Shares     │       │
│  │  • Earned, not bought  │      │   • Earned or funded   │       │
│  │  • Non-transferable    │      │   • Transferable       │       │
│  │  • 1 member = 1 vote  │      │   • Proportional       │       │
│  │  • Revocable by DAO   │      │     dividends          │       │
│  └───────────┬───────────┘      └───────────┬───────────┘       │
│              │                               │                   │
│              └───────────┐   ┌───────────────┘                   │
│                          ▼   ▼                                   │
│               ┌──────────────────────┐                           │
│               │ COOPERATIVE PROJECT  │                           │
│               │  Contributors earn   │                           │
│               │  both voice & value  │                           │
│               └──────────────────────┘                           │
│                                                                  │
│  ┌─────────────────────────────────────────────────────────┐    │
│  │                   PLAY-TO-EARN LAYER                     │    │
│  │   GAMES ──► ENGAGEMENT ──► SHARES ──► INVESTMENT/REDEEM │    │
│  └─────────────────────────────────────────────────────────┘    │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

---

## Platform Capabilities

ArdaNova unifies project management with transparent ownership infrastructure:

| Application Layer | Ownership Layer |
|-----------|------------------------|
| Project management & agile workflows | Secure ledger & Ownership Shares |
| Cooperative governance & worker cooperatives | Community fundraising & fractional ownership |
| Crowdfunding (labor, capital, resources) | Stable coin & share exchange |
| AI/MCP for project generation | Game SDK for Unity/Godot |
| Gamma API for pitch creation | Play-to-earn share distribution |
| Role-based permissions | Cross-project share investment |

---

## Core Features

### Current (v1.0)

| Feature | Description | Status |
|---------|-------------|--------|
| **Project Management** | Full CRUD with rich metadata, categories, and status tracking | Complete |
| **Google OAuth** | Secure authentication with NextAuth | Complete |
| **User Profiles** | Roles, types, skills, and verification levels | Complete |
| **Community Support** | Voting, subscriptions, volunteering, applications | Complete |
| **Guild System** | Professional guilds, membership, bidding, and reviews | Complete |
| **Shop System** | Shop management, products, inventory, analytics | Complete |
| **Equity Tracking** | Share allocation to supporters and contributors | Complete |
| **.NET Backend API** | Clean Architecture API with 6 projects, 40+ MCP tools | Complete |
| **Membership Credentials** | Soulbound governance credentials for projects and guilds | Complete |
| **Credential Utility** | Algorand ASA minting, tier system (BRONZE→DIAMOND), guild credentials | In Progress |
| **KYC Verification** | Identity verification gating project creation and credentials (Manual + Veriff) | In Progress |
| **Event Bus** | In-memory domain event publishing and handling | Complete |
| **SignalR WebSocket** | Real-time updates for activities, notifications, projects | Complete |
| **S3/Local Storage** | File attachments with S3 or local filesystem support | Complete |
| **C# Model Generator** | DBML → C# entities with EF Core attributes | Complete |

### Roadmap (v2.0+)

| Feature | Description | Timeline |
|---------|-------------|----------|
| **Gamification Layer** | XP, levels, achievements, leaderboards, seasons | Q1 2025 |
| **Project Hierarchy** | Roadmaps → Epics → Sprints → PBIs → Tasks | Q2 2025 |
| **Task Marketplace** | Bounties, bidding, escrow, multiple compensation models | Q2 2025 |
| **Secure Ledger Integration** | Soulbound credentials (ASA), ownership shares, wallet connect | In Progress |
| **Fundraising Lifecycle** | Equity creation, funding phases, redemption | Q3 2025 |
| **Gamma Integration** | AI-powered pitch generation and presentation builder | Q3 2025 |
| **Cooperative Governance** | Transparent proposals, voting, treasury | Q3 2025 |
| **Game SDK** | Unity/Godot C# SDK for play-to-earn | Q4 2025 |
| **MCP Agent** | AI agent for project management with role-based access | Q4 2025 |
| **Cross-Project Investment** | Invest completed shares into new projects | Q1 2026 |
| **Platform Cooperative** | $ARDA shares, revenue sharing, meta-governance | Q2 2026 |

See the full [Roadmap](./ROADMAP.md) for detailed plans.

---

## Architecture

ArdaNova uses a distributed architecture with clear separation of concerns:

```
┌─────────────────┐     ┌─────────────────┐     ┌─────────────────┐
│   Next.js App   │────▶│  .NET Backend   │────▶│   PostgreSQL    │
│   (Frontend)    │     │     (API)       │     │   (Database)    │
└─────────────────┘     └─────────────────┘     └─────────────────┘
        │                                               ▲
        └──────────── NextAuth (Auth Only) ─────────────┘
```

### Project Structure

```
ardanova/
├── .env                          # Shared environment variables
├── docker-compose.yml            # Local development
│
├── ardanova-client/              # Next.js Frontend
│   ├── src/
│   │   ├── lib/api/              # Modular API Client
│   │   │   ├── base-client.ts    # Generic reusable base
│   │   │   └── ardanova/         # ArdaNova endpoints
│   │   │       └── endpoints/
│   │   │           ├── users.ts
│   │   │           ├── projects.ts
│   │   │           ├── guilds.ts
│   │   │           └── shops.ts
│   │   └── server/api/routers/   # tRPC routers (thin layer)
│   └── prisma/schema.prisma      # Schema source of truth
│
└── ardanova-backend-api-mcp/     # .NET 8 Backend
    └── api-server/
        └── src/
            ├── ArdaNova.Domain/        # Entities, Enums
            ├── ArdaNova.Application/   # Services, DTOs
            ├── ArdaNova.Infrastructure/# DbContext, Repos
            ├── ArdaNova.API/           # Controllers
            └── ArdaNova.MCP/           # 40+ MCP Tools
```

### Data Flow

| Layer | Technology | Purpose |
|-------|------------|---------|
| **Frontend** | Next.js + tRPC | UI, validation, session |
| **API Client** | TypeScript | Typed backend communication |
| **Backend** | .NET 9 Clean Architecture | Business logic, data access |
| **Real-time** | SignalR | WebSocket for live updates |
| **Events** | In-Memory Event Bus | Domain event publishing |
| **Auth** | NextAuth + Prisma | Authentication only |
| **Storage** | S3 / Local filesystem | File attachments |
| **Blockchain** | Algorand (dotnet-algorand-sdk) | Soulbound credential ASAs |
| **Database** | PostgreSQL | Persistent storage |

See [ARCHITECTURE.md](./ARCHITECTURE.md) for detailed technical documentation.

---

## Technology Stack

### Current Stack

| Layer | Technology |
|-------|------------|
| **Frontend** | Next.js 15, React 19, TypeScript 5.8 |
| **UI** | Radix UI, Tailwind CSS 4.0 |
| **API Layer** | tRPC (frontend) → .NET 9 (backend) |
| **Database** | PostgreSQL |
| **Schema** | DBML → Prisma (migrations) + EF Core (C# entities via generator) |
| **Auth** | NextAuth 5 + Prisma Adapter |
| **State** | TanStack Query 5 |
| **Deployment** | Railway (API, DB) + Vercel (Web) |

### Secure Ledger Stack (ArdaNova)

| Layer | Technology |
|-------|------------|
| **Ledger** | Algorand (10,000+ TPS, 2.85s finality) |
| **Shares/Equity** | Standard Assets (ASAs) |
| **Credential SDK** | dotnet-algorand-sdk (C# native) |
| **Soulbound Credentials** | Frozen ASAs with ARC-19 metadata |
| **Automated Agreements** | AVM / TEAL |
| **Digital Wallets** | Pera Wallet, Defly, MyAlgo |
| **Indexer** | Algorand Indexer API |
| **Node** | AlgoNode / Self-hosted |

### Code Generation Pipeline

```
DBML Schema (source of truth)
    │
    ├──► Prisma Schema (dbml-to-prisma.ts)
    │         │
    │         └──► PostgreSQL migrations (prisma migrate)
    │
    └──► C# Entities (generate-csharp-models.ts)
              │
              ├── [Table], [Key], [Column], [Precision], [Index]
              ├── [ForeignKey], [InverseProperty] for relationships
              └── EnumStringConvention for all enums
```

### Planned Additions

| Layer | Technology | Status |
|-------|------------|--------|
| **Monorepo** | Turborepo | Planned |
| **Backend Services** | Rust (Warp API) for high-performance services | Planned |
| **AI/LLM** | Gamma API, Claude MCP | In Progress |
| **Mobile** | React Native / Expo (Wallet App) | Planned |
| **Real-time** | SignalR WebSocket | ✅ Complete |
| **Payments** | Stable coin (USDC-pegged), Direct ALGO | Planned |
| **Storage** | S3/Local file storage | ✅ Complete |
| **Search** | Meilisearch / Algolia | Planned |
| **Game SDK** | C# for Unity & Godot | Planned |

---

## Community Fundraising Lifecycle

Projects on ArdaNova follow a transparent lifecycle powered by our secure ledger:

```
┌─────────────────────────────────────────────────────────────────┐
│               COMMUNITY FUNDRAISING LIFECYCLE                │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  PHASE 1: CREATION                                              │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │ • Project owner creates project on ArdaNova                  │   │
│  │ • Defines equity parameters (supply, allocation, vesting) │   │
│  │ • Asset created on secure ledger                         │   │
│  │ • Pitch generated via Gamma API                          │   │
│  │ • Fundraising published for community review             │   │
│  └──────────────────────────────────────────────────────────┘   │
│                              │                                   │
│                              ▼                                   │
│  PHASE 2: ACTIVE FUNDING                                        │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │ • Users purchase shares via digital payments             │   │
│  │ • Contributors earn equity for completing tasks          │   │
│  │ • Players earn rewards through integrated games          │   │
│  │ • Real-time funding progress tracking                    │   │
│  │ • Transaction fees collected (platform revenue)          │   │
│  └──────────────────────────────────────────────────────────┘   │
│                              │                                   │
│                              ▼                                   │
│  PHASE 3: COMPLETION                                            │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │ • Fundraising reaches goal or deadline                   │   │
│  │ • Status marked as "funded" on platform                  │   │
│  │ • Share holders can:                                     │   │
│  │   - Redeem for fractional ownership/shares               │   │
│  │   - Redeem for goods/services from project               │   │
│  │   - Invest shares into other active projects             │   │
│  │   - Exchange for digital currency or other assets        │   │
│  └──────────────────────────────────────────────────────────┘   │
│                              │                                   │
│                              ▼                                   │
│  PHASE 4: POST-FUNDING ECOSYSTEM                                 │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │ • Shares circulate in ecosystem                          │   │
│  │ • Cooperative governance active (proposals, voting)      │   │
│  │ • Revenue sharing to share holders                       │   │
│  │ • Cross-project investment opportunities                 │   │
│  │ • Analytics track share performance                      │   │
│  └──────────────────────────────────────────────────────────┘   │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

---

## Project Management Hierarchy

ArdaNova implements a comprehensive agile project management structure:

```
ROADMAP
└── Strategic vision and long-term goals
    │
    └── ROLE STRUCTURE (Cooperative Roles)
        └── Organizational hierarchy and credential-gated permissions
            │
            └── EPICS
                └── Large bodies of work
                    │
                    └── SPRINTS / CYCLES
                        └── Time-boxed periods
                            │
                            └── PRODUCT BACKLOG ITEMS (PBIs)
                                └── Features and capabilities
                                    │
                                    └── BACKLOG ITEMS
                                        └── Smaller work items
                                            │
                                            └── TASKS (Share-Compensated)
                                                └── Atomic units of work
```

### Role-Based Access Control

| Role | Permissions | Governance Vote | Credential Requirement |
|------|-------------|-----------------|------------------------|
| **Founder** | Full access, emergency proposals | 1 vote | Membership Credential (auto-granted) |
| **Leader** | Roadmap, epics, treasury proposals | 1 vote | Membership Credential |
| **Core Contributor** | Task management, proposals | 1 vote | Membership Credential |
| **Contributor** | Task execution, voting | 1 vote | Membership Credential |
| **Observer** | View only | No vote | None (no credential) |

> **Dual-Asset Model:** Governance rights (1 member = 1 vote) are separated from economic rights (ownership shares proportional to contribution). Membership Credentials are earned, non-transferable, and revocable by cooperative vote. See the [Whitepaper](./documentation/ARDA%20NOVA%20-%20Whitepaper.txt) for the full model.

---

## Ownership Model

ArdaNova separates governance rights from economic rights using a dual-asset model:

### Membership Credentials (Governance)

Non-transferable credentials that grant cooperative voting rights:
- **Earned, never bought** — granted via founding, contribution thresholds, DAO vote, or Game SDK
- **1 member = 1 vote** — equal governance regardless of economic stake
- **Revocable** — by cooperative vote (66% quorum, 75% approval)
- **Soulbound** — cannot be transferred or sold (Algorand ASAs with `defaultFrozen=true`)
- **KYC-gated** — requires PRO verification level (identity verification) before issuance
- **Scoped** — issued per-project or per-guild with XOR validation
- **Tiered** — BRONZE → SILVER → GOLD → PLATINUM → DIAMOND progression
- **On-chain verified** — ARC-19 metadata on Algorand for trustless verification

### Ownership Shares (Economics)

Fungible shares representing economic stake in a project:

```typescript
interface ProjectShareAllocation {
  founders: 20;        // % to founding team (vested over 2 years)
  contributors: 30;    // % for task/bounty rewards
  treasury: 20;        // % held by project cooperative
  investors: 20;       // % for fundraising participants
  community: 5;        // % for engagement rewards
  platform: 5;         // % to ArdaNova platform treasury
}
```

### Platform Digital Currency

- Pegged to USDC/USDT for value stability
- All project shares exchangeable with digital currency
- Provides liquidity for cross-project transactions
- Managed by platform automated agreements

### Ownership Utility

| Utility | Description |
|---------|-------------|
| **Governance** | Vote on project proposals (1 credential = 1 vote) |
| **Revenue Share** | Receive dividends proportional to share holdings |
| **Access** | Unlock premium features, early access |
| **Staking** | Earn rewards by staking shares |
| **Redemption** | Exchange for ownership, goods, or services |
| **Cross-Investment** | Invest shares into other projects |

---

## Game SDK Integration

### For Game Developers

The ArdaNova Game SDK enables Unity and Godot developers to integrate ownership shares:

```csharp
// C# SDK Example (Unity)
using ArdaNova.GameSDK;

public class TokenRewardSystem : MonoBehaviour
{
    private ArdaNovaClient client;

    async void Start()
    {
        client = new ArdaNovaClient("YOUR_API_KEY");
        await client.ConnectWallet();
    }

    public async void RewardPlayer(string playerId, int amount, string tokenId)
    {
        // Award project tokens for gameplay achievements
        var result = await client.AwardTokens(playerId, tokenId, amount);

        if (result.Success)
        {
            Debug.Log($"Awarded {amount} tokens to {playerId}");
        }
    }

    public async void ExchangeTokens(string fromToken, string toToken, int amount)
    {
        // In-game token exchange
        var rate = await client.GetExchangeRate(fromToken, toToken);
        var result = await client.Exchange(fromToken, toToken, amount);
    }

    public async Task<TokenBalance[]> GetPlayerBalances(string playerId)
    {
        // Query all token balances
        return await client.GetBalances(playerId);
    }
}
```

### Play-to-Earn Flow

```
┌─────────────────────────────────────────────────────────────────┐
│                    PLAY-TO-EARN INTEGRATION                      │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  GAME DEVELOPER                                                  │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │ 1. Register on ArdaNova platform → Get API credentials       │   │
│  │ 2. Integrate Game SDK (Unity/Godot C#)                   │   │
│  │ 3. Choose which project shares to integrate               │   │
│  │ 4. Implement reward triggers (achievements, wins, etc.)   │   │
│  │ 5. Enable in-game share exchange                           │   │
│  └──────────────────────────────────────────────────────────┘   │
│                                                                  │
│  PLAYER                                                          │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │ 1. Link ArdaNova wallet to game                               │   │
│  │ 2. Play games → Earn ownership shares                      │   │
│  │ 3. Exchange shares in-game or on platform                 │   │
│  │ 4. View portfolio across all games in ArdaNova wallet         │   │
│  │ 5. Invest earned shares into new projects                 │   │
│  │ 6. Redeem shares for ownership or services                │   │
│  └──────────────────────────────────────────────────────────┘   │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

---

## Cooperative Governance

### Project Cooperatives

Every project on ArdaNova operates as a member-governed cooperative:

- **Credential-gated governance** - 1 Membership Credential = 1 vote, regardless of economic stake
- **Multi-stakeholder structure** - Founders, workers, and investors all have voice
- **Proposal types** - Treasury, governance, strategic, operational, emergency
- **Voting mechanisms** - Equal-weight, quadratic, conviction, delegation
- **Automated execution** - Proposals execute automatically via secure agreements
- **Fair exit** - Members can exit with their fair share of treasury

### Crowdfunding Modalities

| Mode | Description | Share Reward |
|------|-------------|--------------|
| **Financial** | Stable coin, ALGO, other shares | Direct purchase |
| **Labor** | Time commitments, task completion | Task bounties |
| **Resources** | Equipment, licenses, infrastructure | Negotiated |
| **Gaming** | Play-to-earn engagement | Per achievement |
| **Social Capital** | Introductions, partnerships | Referral bonus |

---

## AI Integration

### Gamma API (Pitch Studio)

Generate professional pitch presentations from project data:

- **Templates** - Problem-Solution, Investment Deck, Team Recruitment, Progress Update
- **Customization** - Themes, branding, sections
- **Outputs** - Slides, PDF, video, embeddable
- **Pitch Posts** - Share pitches for community review and funding

### MCP Server (AI Agent) ✅ Implemented

The Model Context Protocol server enables AI-assisted project management. The .NET MCP layer (`ArdaNova.MCP`) has been implemented with 40+ tools.

**Implemented MCP Tools** (in `ArdaNova.MCP/Tools/`):
- `UserTools` - user_get_by_id, user_get_by_email, user_get_paged, user_create, user_verify
- `ProjectTools` - project_get_by_id, project_get_by_slug, project_get_all, project_create, project_publish, project_delete
- `GuildTools` - guild_get_by_id, guild_get_by_slug, guild_create, guild_add_member, guild_delete
- `ShopTools` - shop_get_by_id, shop_get_by_owner, shop_create, shop_update, shop_toggle_active
- `ActivityTools` - activity_get_by_id, activity_get_paged, activity_create
- `NotificationTools` - notification_get_by_id, notification_get_paged, notification_mark_read
- `WalletTools` - wallet_get_by_id, wallet_get_by_user, wallet_create, wallet_update_balance
- `ExchangeTools` - swap_create, swap_get_by_id, liquidity_pool_get
- `TaskEscrowTools` - escrow_create, escrow_release, escrow_get_by_task
- `DelegatedVoteTools` - vote_delegate, vote_revoke, vote_get_delegations
- `GamificationExtendedTools` - streak_get, referral_create, achievement_award

**Future Tools (Planned)**:
- `generate_epic` - Create epics from descriptions
- `generate_tasks` - Break down features into tasks
- `suggest_contributors` - Match tasks to suitable members
- `generate_pitch` - Create presentations via Gamma
- `create_proposal` - Draft governance proposals
- `analyze_progress` - Generate progress reports
- `estimate_share_rewards` - Calculate task compensation

**Permissions**
- All tools respect role-based and credential-gated access control
- Actions are audit-logged on-chain
- Sensitive operations require multi-sig approval

---

## Getting Started

### Prerequisites

- Node.js 20+
- npm 10+
- PostgreSQL 14+
- .NET 8 SDK
- Algorand wallet (Pera, Defly, or MyAlgo)

### Installation

```bash
# Clone the repository
git clone https://github.com/HarrSoft/ardanova.git
cd ardanova

# Set up environment variables
cp .env.example .env
# Edit .env with your configuration

# Option 1: Docker (recommended)
docker-compose up

# Option 2: Manual setup
# Terminal 1 - Frontend
cd ardanova-client
npm install
npx prisma db push
npx prisma generate
npm run dev

# Terminal 2 - Backend
cd ardanova-backend-api-mcp
dotnet run --project api-server/src/ArdaNova.API
```

### Environment Variables

Create a `.env` file in the repository root (shared by both services):

```env
# Database (PostgreSQL URL format - works for both Prisma and .NET)
DATABASE_URL="postgresql://user:password@localhost:5432/ardanova"

# .NET Backend API
API_URL="http://localhost:8080"
API_KEY="your-api-key"

# NextAuth
AUTH_SECRET="your-auth-secret"
AUTH_URL="http://localhost:3000"
GOOGLE_CLIENT_ID="your-google-client-id"
GOOGLE_CLIENT_SECRET="your-google-client-secret"

# Algorand
ALGORAND_NETWORK="testnet"
ALGORAND_NODE_URL="https://testnet-api.algonode.cloud"
ALGORAND_INDEXER_URL="https://testnet-idx.algonode.cloud"
```

---

## Monetization

### Revenue Streams

| Stream | Description |
|--------|-------------|
| **Transaction Fees** | % on all share exchanges and fundraising transactions |
| **Fundraising Launch Fees** | Fee for launching new project share offerings |
| **Platform Share** | 5% allocation from all project share issuances |
| **SaaS Subscriptions** | Tiered plans for business tools |
| **API Usage Fees** | Metered billing for Game SDK developers |
| **Premium Features** | AI generation credits, advanced analytics |
| **Credential Issuance** | Fees on membership credential issuance and verification |

### Revenue Distribution (Platform DAO)

| Allocation | Percentage |
|------------|------------|
| Project Treasury (fund new projects) | 35% |
| $ANOVA Stakers (passive income) | 30% |
| Operations (team, infrastructure) | 25% |
| Community Grants | 10% |

---

## Target Audience

- **Innovators** - Individuals with ideas seeking to launch cooperative projects
- **Contributors** - Freelancers and professionals seeking ownership, not just wages
- **Investors** - Backers looking for fractional ownership with governance rights
- **Agencies** - Professional service providers seeking project opportunities
- **SMEs** - Businesses needing affordable, localized digital tools
- **Game Developers** - Studios wanting play-to-earn integration
- **Players** - Gamers seeking to earn while playing
- **Cooperatives** - Organizations aligned with worker-ownership principles

---

## Why Algorand?

ArdaNova chose Algorand for its blockchain infrastructure:

| Feature | Benefit |
|---------|---------|
| **10,000+ TPS** | Handle high transaction volumes |
| **2.85s Finality** | Instant confirmation, no waiting |
| **~$0.001 Fees** | Affordable for micro-transactions |
| **Pure PoS** | Energy efficient, sustainable |
| **No Forking** | Transaction certainty |
| **Native ASAs** | Simple asset creation |
| **Atomic Txns** | Complex multi-step operations |
| **State Proofs** | Cross-chain interoperability |

---

## Contributing

We welcome contributions from the community! See our [Contributing Guide](./CONTRIBUTING.md) for details.

### Development Workflow

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Make your changes
4. Run tests and linting
5. Commit your changes (`git commit -m 'feat: add amazing feature'`)
6. Push to your fork (`git push origin feature/amazing-feature`)
7. Open a Pull Request

### Code Style

- TypeScript strict mode
- ESLint + Prettier for formatting
- Conventional commits

---

## Documentation

- [Roadmap](./documentation/ROADMAP.md) - Platform vision and implementation timeline
- [Architecture](./documentation/ARCHITECTURE.md) - Technical architecture and data flow
- [Phase A baseline critique](./documentation/PHASE_A_BASELINE_CRITIQUE.md) - Doc vs code snapshot, risks, MVP notes
- [Local development smoke runbook](./documentation/LOCAL_DEVELOPMENT_SMOKE.md) - Env, commands, smoke checks, known issues
- [Phase A test matrix](./documentation/PHASE_A_TEST_MATRIX.md) - Narrow manual QA matrix (baseline)
- [Database Schema (DBML)](./ardanova-client/prisma/database-architecture.dbml) - DBML schema (source of truth)
- [Prisma Schema](./ardanova-client/prisma/schema.prisma) - Generated Prisma schema
- [C# Generator](./ardanova-client/scripts/generate-csharp-models.ts) - DBML to C# entity generator
- [API Client](./ardanova-client/src/lib/api/) - Modular TypeScript API client
- [.NET Backend](./ardanova-backend-api-mcp/) - Clean Architecture .NET 8 API with MCP tools

---

## Community

- **GitHub Issues** - Bug reports and feature requests
- **Discussions** - Community discussions and Q&A
- **Discord** - Real-time chat (coming soon)
- **Twitter** - Updates and announcements (coming soon)

---

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

---

## Acknowledgments

ArdaNova is built on the shoulders of giants:

- [Next.js](https://nextjs.org/) - The React framework
- [.NET](https://dotnet.microsoft.com/) - For our robust backend API
- [Algorand](https://algorand.com/) - Pure proof-of-stake blockchain
- [Prisma](https://prisma.io/) - Next-generation ORM
- [Tailwind CSS](https://tailwindcss.com/) - Utility-first CSS
- [Radix UI](https://radix-ui.com/) - Unstyled, accessible components
- [Anthropic Claude](https://anthropic.com/) - AI assistance

---

<div align="center">

**Let's build the future of work together.**

*Workers own what they build. Shares flow to contributors. Governance is one member, one vote. Games create value. Success is shared.*

---

**[ardanova.com](https://ardanova.com)**

</div>
