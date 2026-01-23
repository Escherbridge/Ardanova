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
- **Tokenized Crowdfunding (ICO)** - Launch project tokens on Algorand for fractional ownership
- **Multi-Modal Funding** - Fund with capital, labor, resources, crypto, or social capital
- **DAO-Governed Projects** - Transparent, participatory governance at every level
- **Worker-Owned Cooperatives** - Every project becomes a tokenized cooperative
- **Play-to-Earn Integration** - Earn project tokens through games and engagement
- **AI-Powered Generation** - Gamma API integration for instant pitch creation
- **Game SDK** - Unity/Godot integration for gamified experiences

```
┌─────────────────────────────────────────────────────────────────┐
│                    THE CIRCULAR ECONOMY MODEL                    │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  ┌─────────────────────────────────────────────────────────┐    │
│  │                                                          │    │
│  │   CONTRIBUTORS ──► PROJECTS ──► TOKEN VALUE CREATION    │    │
│  │        ▲                               │                 │    │
│  │        │                               │                 │    │
│  │        └──── OWNERSHIP (ASA TOKENS) ◄──┘                │    │
│  │                                                          │    │
│  └─────────────────────────────────────────────────────────┘    │
│                                                                  │
│  ┌─────────────────────────────────────────────────────────┐    │
│  │                   PLAY-TO-EARN LAYER                     │    │
│  │   GAMES ──► ENGAGEMENT ──► TOKENS ──► INVESTMENT/REDEEM │    │
│  └─────────────────────────────────────────────────────────┘    │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

---

## Platform Capabilities

ArdaNova unifies project management with blockchain infrastructure:

| Application Layer | Blockchain Layer |
|-----------|------------------------|
| Project management & agile workflows | Algorand blockchain & ASA tokens |
| DAO governance & worker cooperatives | ICO lifecycle & fractional ownership |
| Crowdfunding (labor, capital, resources) | Stable coin & token exchange |
| AI/MCP for project generation | Game SDK for Unity/Godot |
| Gamma API for pitch creation | Play-to-earn token distribution |
| Role-based permissions | Cross-project token investment |

---

## Core Features

### Current (v1.0)

| Feature | Description | Status |
|---------|-------------|--------|
| **Project Management** | Full CRUD with rich metadata, categories, and status tracking | Complete |
| **Google OAuth** | Secure authentication with NextAuth | Complete |
| **User Profiles** | Roles, types, skills, and verification levels | Complete |
| **Community Support** | Voting, subscriptions, volunteering, applications | Complete |
| **Agency System** | Agency profiles, bidding, and reviews | Complete |
| **Equity Tracking** | Share allocation to supporters and contributors | Complete |
| **Business Tools** | Invoicing, inventory, sales, marketing (schema ready) | In Progress |
| **.NET Backend API** | Clean Architecture API with 6 projects, 40+ MCP tools, 33 tests | Complete |

### Roadmap (v2.0+)

| Feature | Description | Timeline |
|---------|-------------|----------|
| **Gamification Layer** | XP, levels, achievements, leaderboards, seasons | Q1 2025 |
| **Project Hierarchy** | Roadmaps → Epics → Sprints → PBIs → Tasks | Q2 2025 |
| **Task Marketplace** | Bounties, bidding, escrow, multiple compensation models | Q2 2025 |
| **Algorand Integration** | ASA tokens, wallet connect, stable coin | Q2 2025 |
| **ICO Lifecycle** | Token creation, funding phases, redemption | Q3 2025 |
| **Gamma Integration** | AI-powered pitch generation and presentation builder | Q3 2025 |
| **DAO Governance** | On-chain proposals, voting, treasury | Q3 2025 |
| **Game SDK** | Unity/Godot C# SDK for play-to-earn | Q4 2025 |
| **MCP Agent** | AI agent for project management with role-based access | Q4 2025 |
| **Cross-ICO Investment** | Invest completed tokens into new projects | Q1 2026 |
| **Platform Token** | $ARDA token, revenue sharing, meta-governance | Q2 2026 |

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
│   │   │           ├── agencies.ts
│   │   │           └── businesses.ts
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
| **Backend** | .NET 8 | Business logic, data access |
| **Auth** | NextAuth + Prisma | Authentication only |
| **Database** | PostgreSQL | Persistent storage |

See [ARCHITECTURE.md](./ARCHITECTURE.md) for detailed technical documentation.

---

## Technology Stack

### Current Stack

| Layer | Technology |
|-------|------------|
| **Frontend** | Next.js 15, React 19, TypeScript 5.8 |
| **UI** | Radix UI, Tailwind CSS 4.0 |
| **API Layer** | tRPC (frontend) → .NET 8 (backend) |
| **Database** | PostgreSQL |
| **Schema** | Prisma (migrations) + EF Core (read/write) |
| **Auth** | NextAuth 5 + Prisma Adapter |
| **State** | TanStack Query 5 |
| **Deployment** | Railway (API, DB) + Vercel (Web) |

### Blockchain Stack (ArdaNova)

| Layer | Technology |
|-------|------------|
| **Blockchain** | Algorand (10,000+ TPS, 2.85s finality) |
| **Tokens** | Algorand Standard Assets (ASAs) |
| **Smart Contracts** | PyTeal / TEAL |
| **Wallets** | Pera Wallet, Defly, MyAlgo |
| **Indexer** | Algorand Indexer API |
| **Node** | AlgoNode / Self-hosted |

### Planned Additions

| Layer | Technology |
|-------|------------|
| **Monorepo** | Turborepo |
| **Backend Services** | Rust (Warp API) for high-performance services |
| **AI/LLM** | Gamma API, Claude MCP |
| **Mobile** | React Native / Expo (Wallet App) |
| **Real-time** | WebSockets / Pusher |
| **Payments** | Stable coin (USDC-pegged), Direct ALGO |
| **Storage** | S3-compatible (media, documents) |
| **Search** | Meilisearch / Algolia |
| **Game SDK** | C# for Unity & Godot |

---

## ICO Lifecycle

Projects on ArdaNova follow a tokenized lifecycle powered by Algorand:

```
┌─────────────────────────────────────────────────────────────────┐
│                      ICO LIFECYCLE ON DOSO                       │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  PHASE 1: CREATION                                              │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │ • Project owner creates project on ArdaNova                  │   │
│  │ • Defines token parameters (supply, allocation, vesting) │   │
│  │ • ASA (Algorand Standard Asset) created on-chain         │   │
│  │ • Pitch generated via Gamma API                          │   │
│  │ • ICO published for community review                     │   │
│  └──────────────────────────────────────────────────────────┘   │
│                              │                                   │
│                              ▼                                   │
│  PHASE 2: ACTIVE FUNDING                                        │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │ • Users purchase tokens via stable coin or ALGO          │   │
│  │ • Contributors earn tokens for completing tasks          │   │
│  │ • Players earn tokens through integrated games           │   │
│  │ • Real-time funding progress tracking                    │   │
│  │ • Transaction fees collected (platform revenue)          │   │
│  └──────────────────────────────────────────────────────────┘   │
│                              │                                   │
│                              ▼                                   │
│  PHASE 3: COMPLETION                                            │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │ • ICO reaches funding goal or deadline                   │   │
│  │ • Status marked as "funded" on platform                  │   │
│  │ • Token holders can:                                     │   │
│  │   - Redeem for fractional ownership/shares               │   │
│  │   - Redeem for goods/services from project               │   │
│  │   - Invest tokens into other active ICOs                 │   │
│  │   - Exchange for stable coin or other tokens             │   │
│  └──────────────────────────────────────────────────────────┘   │
│                              │                                   │
│                              ▼                                   │
│  PHASE 4: POST-ICO ECOSYSTEM                                    │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │ • Tokens circulate in ecosystem                          │   │
│  │ • DAO governance active (proposals, voting)              │   │
│  │ • Revenue sharing to token holders                       │   │
│  │ • Cross-project investment opportunities                 │   │
│  │ • Analytics track token performance                      │   │
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
    └── ROLE STRUCTURE (DAO Roles)
        └── Organizational hierarchy and token-weighted permissions
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
                                            └── TASKS (Token-Compensated)
                                                └── Atomic units of work
```

### Role-Based Access Control

| Role | Permissions | Voting Weight | Token Requirement |
|------|-------------|---------------|-------------------|
| **Founder** | Full access, veto rights | 3x | Initial allocation |
| **Leader** | Roadmap, epics, treasury proposals | 2x | 5% of supply |
| **Core Contributor** | Task management, proposals | 1.5x | 1% of supply |
| **Contributor** | Task execution, voting | 1x | Any holding |
| **Observer** | View only | 0x | None |

---

## Token Economics

### Project Tokens (ASAs)

Each project on ArdaNova can issue its own token via Algorand Standard Assets:

```typescript
interface ProjectTokenAllocation {
  founders: 20;        // % to founding team (vested over 2 years)
  contributors: 30;    // % for task/bounty rewards
  treasury: 20;        // % held by project DAO
  investors: 20;       // % for ICO participants
  community: 5;        // % for airdrops, engagement
  platform: 5;         // % to ArdaNova platform treasury
}
```

### Platform Stable Coin

- Pegged to USDC/USDT for stability
- All project tokens exchangeable with stable coin
- Provides liquidity for cross-project transactions
- Managed by platform smart contracts

### Token Utility

| Utility | Description |
|---------|-------------|
| **Governance** | Vote on project proposals (weight by holdings) |
| **Revenue Share** | Receive dividends from project revenue |
| **Access** | Unlock premium features, early access |
| **Staking** | Earn rewards by staking tokens |
| **Redemption** | Exchange for ownership, goods, or services |
| **Cross-Investment** | Invest tokens into other projects |

---

## Game SDK Integration

### For Game Developers

The ArdaNova Game SDK enables Unity and Godot developers to integrate blockchain tokens:

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
│  │ 3. Choose which project tokens to integrate               │   │
│  │ 4. Implement reward triggers (achievements, wins, etc.)   │   │
│  │ 5. Enable in-game token exchange                          │   │
│  └──────────────────────────────────────────────────────────┘   │
│                                                                  │
│  PLAYER                                                          │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │ 1. Link ArdaNova wallet to game                               │   │
│  │ 2. Play games → Earn project tokens                       │   │
│  │ 3. Exchange tokens in-game or on platform                 │   │
│  │ 4. View portfolio across all games in ArdaNova wallet         │   │
│  │ 5. Invest earned tokens into new projects                 │   │
│  │ 6. Redeem tokens for ownership or services                │   │
│  └──────────────────────────────────────────────────────────┘   │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

---

## DAO Governance

### Project DAOs

Every project on ArdaNova operates as a Decentralized Autonomous Organization:

- **Token-weighted governance** - Voting power proportional to holdings
- **Multi-stakeholder structure** - Founders, workers, and investors all have voice
- **Proposal types** - Treasury, governance, strategic, operational, emergency
- **Voting mechanisms** - Weighted, quadratic, conviction, delegation
- **On-chain execution** - Proposals execute automatically via smart contracts
- **Rage quit** - Members can exit with their fair share of treasury

### Crowdfunding Modalities

| Mode | Description | Token Reward |
|------|-------------|--------------|
| **Financial** | Stable coin, ALGO, other tokens | Direct purchase |
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
- `AgencyTools` - agency_get_by_id, agency_get_by_slug, agency_create, agency_verify, agency_delete
- `BusinessTools` - business_get_by_id, business_get_by_owner, business_create, business_upgrade_plan, business_toggle_active

**Future Tools (Planned)**:
- `generate_epic` - Create epics from descriptions
- `generate_tasks` - Break down features into tasks
- `suggest_contributors` - Match tasks to suitable members
- `generate_pitch` - Create presentations via Gamma
- `create_proposal` - Draft governance proposals
- `analyze_progress` - Generate progress reports
- `estimate_token_rewards` - Calculate task compensation

**Permissions**
- All tools respect role-based and token-weighted access control
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

# Algorand (future)
ALGORAND_NETWORK="testnet"
ALGORAND_NODE_URL="https://testnet-api.algonode.cloud"
ALGORAND_INDEXER_URL="https://testnet-idx.algonode.cloud"
```

---

## Monetization

### Revenue Streams

| Stream | Description |
|--------|-------------|
| **Transaction Fees** | % on all token exchanges and ICO transactions |
| **ICO Launch Fees** | Fee for launching new project tokens |
| **Platform Token** | 5% allocation from all project tokens |
| **SaaS Subscriptions** | Tiered plans for business tools |
| **API Usage Fees** | Metered billing for Game SDK developers |
| **Premium Features** | AI generation credits, advanced analytics |
| **Ownership Facilitation** | Fees on fractional ownership redemptions |

### Revenue Distribution (Platform DAO)

| Allocation | Percentage |
|------------|------------|
| Project Treasury (fund new projects) | 35% |
| $ANOVA Stakers (passive income) | 30% |
| Operations (team, infrastructure) | 25% |
| Community Grants | 10% |

---

## Target Audience

- **Innovators** - Individuals with ideas seeking to launch tokenized projects
- **Contributors** - Freelancers and professionals seeking ownership, not just wages
- **Investors** - Backers looking for fractional ownership with governance rights
- **Agencies** - Professional service providers seeking project opportunities
- **SMEs** - Businesses needing affordable, localized digital tools
- **Game Developers** - Studios wanting blockchain/play-to-earn integration
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
| **Native ASAs** | Simple token creation |
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

- [Architecture](./ARCHITECTURE.md) - Technical architecture and data flow
- [Database Schema](./ardanova-client/prisma/schema.prisma) - Prisma schema (source of truth)
- [API Client](./ardanova-client/src/lib/api/) - Modular TypeScript API client
- [.NET Backend](./ardanova-backend-api-mcp/) - Clean Architecture .NET 8 API

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

*Workers own what they build. Tokens flow to contributors. Governance is participatory. Games create value. Success is shared.*

---

**[ardanova.com](https://ardanova.com)**

</div>
