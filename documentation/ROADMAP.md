# ArdaNova Platform Roadmap

## Vision: The Decentralized Worker-Owned Cooperative Economy

**ArdaNova** is a **gamified, decentralized ecosystem** that enables:

- **Gamified Talent Marketplace** (Upwork reimagined with ownership)
- **Community Fundraising** on Algorand ledger
- **Cooperative Projects** with transparent governance
- **Worker-Owned Cooperatives** at every level
- **Play-to-Earn Integration** via Game SDK
- **AI-Powered Project Generation** via Gamma API + MCP
- **Circular Economy** where value flows back to contributors

---


---

## Core Philosophy

```
┌─────────────────────────────────────────────────────────────────┐
│              THE DUAL-ASSET COOPERATIVE ECONOMY                  │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  DUAL-ASSET MODEL:                                               │
│  ┌───────────────────────┐    ┌───────────────────────┐          │
│  │   MEMBERSHIP          │    │   PROJECT SHARES      │          │
│  │   CREDENTIAL          │    │   (Economic)          │          │
│  │   (Governance)        │    │                       │          │
│  ├───────────────────────┤    ├───────────────────────┤          │
│  │ • Soulbound / Non-    │    │ • Fungible ASA tokens │          │
│  │   transferable        │    │ • Transferable        │          │
│  │ • 1 member = 1 vote   │    │ • Proportional to     │          │
│  │ • Earned, never bought│    │   contribution        │          │
│  │ • Revocable by DAO    │    │ • Revenue dividends   │          │
│  └───────────────────────┘    └───────────────────────┘          │
│                                                                  │
│  HOW MEMBERSHIP IS EARNED:                                       │
│  • Founder grant (project creation)                              │
│  • DAO vote (existing members approve)                           │
│  • Contribution threshold (sustained task completion)            │
│  • Application approved (membership request accepted)            │
│  • Game SDK threshold (play-to-earn participation level)         │
│                                                                  │
│  CIRCULAR VALUE FLOW:                                            │
│  CONTRIBUTORS ──────► PROJECTS ──────► VALUE CREATION            │
│       ▲                                      │                   │
│       │                                      │                   │
│       └──────── SHARES (Economic) ◄──────────┘                   │
│       └──────── CREDENTIAL (Governance) ◄────┘                   │
│                                                                  │
│  PLAYERS ─────► GAMES ─────► ENGAGEMENT ─────► SHARES + CREDENTIAL │
│       ▲                                           │              │
│       │                                           │              │
│       └──────── INVEST / REDEEM / EXCHANGE ◄─────┘               │
│                                                                  │
│  • Workers earn ownership, not just wages                        │
│  • Supporters become equity stakeholders                         │
│  • Membership grants equal governance regardless of investment   │
│  • Projects become digital cooperatives                          │
│  • Platform is governed by $ARDA holders                         │
│  • Games distribute value through play-to-earn                   │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

---

## Domain & Subdomain Architecture

### Subdomain Structure (Simplified)

| Subdomain | Purpose | Location |
|-----------|---------|----------|
| `www.ardanova.com` | Main platform (all features consolidated) | `ardanova-client/` |
| `api.ardanova.com` | .NET 8 API gateway + MCP server ✅ | `ardanova-backend-api-mcp/` |
| `ai.ardanova.com` | Python AI orchestrator API | `ardanova-ai-client/` |

**Consolidated into Main Platform:**
- DAO governance (proposals, voting)
- Pitch Studio (Gamma integration)
- Token Exchange (swap, liquidity)
- Blockchain Explorer
- AI Agent interface (via tRPC)

**Provisioned Separately:**
- Game SDK → NuGet package distribution
- Documentation → `/documentation/` in repo

---

## Repository Structure

```
ardanova/
│
├── ardanova-client/                    # Main Next.js application (all features)
│   ├── src/
│   │   ├── app/                        # Next.js 14 App Router
│   │   │   ├── auth/                   # Authentication pages (signin, error)
│   │   │   ├── dashboard/              # User dashboard (create, edit projects)
│   │   │   ├── projects/               # Project listing & details
│   │   │   ├── marketplace/            # Task marketplace (TODO)
│   │   │   ├── dao/                    # Governance UI - proposals, voting (TODO)
│   │   │   ├── studio/                 # Pitch builder - Gamma integration (TODO)
│   │   │   ├── exchange/               # Token exchange, swap, liquidity (TODO)
│   │   │   ├── explorer/               # Blockchain/token explorer (TODO)
│   │   │   ├── agent/                  # AI agent chat interface (TODO)
│   │   │   ├── api/                    # Next.js API routes
│   │   │   │   ├── auth/               # NextAuth routes
│   │   │   │   ├── trpc/               # tRPC endpoint
│   │   │   │   └── health/             # Health check endpoint
│   │   │   └── _components/            # App-specific components
│   │   │
│   │   ├── components/                 # Shared UI components
│   │   │   └── ui/                     # shadcn/ui components
│   │   │
│   │   ├── lib/                        # Utilities and helpers
│   │   │   ├── api/                    # API client for .NET backend
│   │   │   │   ├── base-client.ts      # Base HTTP client
│   │   │   │   └── ardanova/           # ArdaNova API endpoints
│   │   │   │       └── endpoints/      # users, projects, agencies, businesses
│   │   │   ├── blockchain/             # Algorand integration (TODO)
│   │   │   └── utils.ts                # General utilities
│   │   │
│   │   ├── server/                     # Server-side code
│   │   │   ├── api/                    # tRPC server
│   │   │   │   ├── root.ts             # Main router
│   │   │   │   ├── trpc.ts             # tRPC context & procedures
│   │   │   │   └── routers/            # Feature routers
│   │   │   │       ├── project.ts      # Project CRUD via API client
│   │   │   │       ├── post.ts         # Legacy posts
│   │   │   │       ├── dao.ts          # DAO governance (TODO)
│   │   │   │       ├── exchange.ts     # Token exchange (TODO)
│   │   │   │       ├── agent.ts        # AI agent orchestrator (TODO)
│   │   │   │       └── gamification.ts # XP, badges, leaderboards (TODO)
│   │   │   ├── auth/                   # NextAuth configuration
│   │   │   └── db.ts                   # Prisma client
│   │   │
│   │   ├── trpc/                       # tRPC client
│   │   │   ├── react.tsx               # React Query integration
│   │   │   ├── server.ts               # Server-side caller
│   │   │   └── query-client.ts         # Query client config
│   │   │
│   │   └── styles/                     # Global styles
│   │
│   ├── prisma/                         # Database schema & migrations
│   └── public/                         # Static assets
│
├── ardanova-backend-api-mcp/           # ✅ IMPLEMENTED - .NET 8 Backend
│   ├── ardanova.sln                    # Solution (6 projects)
│   ├── Dockerfile                      # Container deployment
│   ├── railway.toml                    # Railway deployment config
│   └── api-server/
│       ├── src/
│       │   ├── ArdaNova.Domain/        # Entities, Enums
│       │   ├── ArdaNova.Application/   # Services, DTOs, Interfaces
│       │   ├── ArdaNova.Infrastructure/# DbContext, Repositories
│       │   ├── ArdaNova.API/           # Controllers, Middleware
│       │   └── ArdaNova.MCP/           # 40+ MCP Tools
│       └── tests/
│           └── ArdaNova.Application.Tests/
│
├── ardanova-ai-client/                 # Python AI Orchestrator (TODO)
│   ├── src/
│   │   ├── api/                        # FastAPI endpoints
│   │   ├── orchestrator/               # AI agent orchestration logic
│   │   ├── agents/                     # Agent definitions & behaviors
│   │   └── mcp/                        # MCP client (connects to .NET MCP)
│   ├── requirements.txt
│   └── Dockerfile
│
├── ardanova-game-sdk/                  # Game SDK (distributed via NuGet)
│   ├── game-sdk-unity/                 # C# SDK for Unity
│   └── game-sdk-godot/                 # C# SDK for Godot
│
├── contracts/                          # Algorand smart contracts (PyTeal)
│   ├── governance/                     # DAO governance (proposals, voting)
│   ├── tokens/                         # ASA token factories
│   ├── ico/                            # ICO lifecycle contracts
│   ├── exchange/                       # DEX/swap contracts
│   └── escrow/                         # Task payment escrow
│
├── documentation/                      # All documentation (no separate website)
│   ├── ROADMAP.md                      # This file
│   └── ...                             # API docs, SDK docs, guides
│
├── docker-compose.yml                  # Local development orchestration
├── railway.toml                        # Root Railway deployment config
└── README.md                           # Project overview
```

### Blockchain Integration (in ardanova-client)

Algorand/ledger functionality lives directly in `ardanova-client/src/lib/blockchain/`:

```
lib/blockchain/                         # Ledger integration (TODO)
├── sdk/                                # Core Algorand SDK wrapper
├── tokens/                             # ASA share management
├── ico/                                # Fundraising lifecycle management
├── exchange/                           # Share exchange logic
├── wallet/                             # Wallet connection (Pera, Defly)
├── indexer/                            # Ledger indexer client
└── contracts/                          # Agreement ABIs & client interactions
```

Automated agreements source code (PyTeal) is in the root `/contracts/` folder.

---

## Phase 1: Foundation & Gamification Layer
**Timeline: Q1 2025**

### 1.1 Gamification System

#### Experience & Reputation

```typescript
interface UserReputation {
  // Core XP System
  totalXP: number;
  level: number;
  tier: 'Bronze' | 'Silver' | 'Gold' | 'Platinum' | 'Diamond';

  // Domain-Specific Reputation
  domains: {
    development: ReputationScore;
    design: ReputationScore;
    marketing: ReputationScore;
    management: ReputationScore;
    community: ReputationScore;
  };

  // Trust Score (for DAO voting weight)
  trustScore: number;

  // Verification Levels
  verificationLevel: 'Anonymous' | 'Verified' | 'Pro' | 'Expert';

  // Token Holdings (affects governance)
  tokenHoldings: {
    projectTokens: TokenBalance[];
    platformToken: bigint; // $ARDA balance
  };
}
```

#### Achievement System

| Category | Examples |
|----------|----------|
| **Contributor** | First Task, 100 Tasks, Task Streak, Quality Champion |
| **Collaborator** | Team Player, Cross-Project, Mentor Badge |
| **Investor** | First Backing, Portfolio Builder, Impact Investor |
| **Governance** | First Vote, Proposal Author, Consensus Builder |
| **Community** | Connector, Recruiter, Ambassador |
| **Gaming** | First Earn, Power Player, Cross-Game Investor |

#### Leaderboards & Seasons

- **Global Leaderboard**: Top contributors across all metrics
- **Domain Leaderboards**: Per skill category
- **Project Leaderboards**: Within specific projects
- **Gaming Leaderboards**: Top earners from play-to-earn
- **Seasonal Competitions**: Monthly/quarterly challenges with token rewards

### 1.2 Enhanced User Profiles

```typescript
interface EnhancedProfile {
  // Identity
  identity: {
    displayName: string;
    handle: string;
    avatar: string;
    banner: string;
    bio: string;
    location: string;
    timezone: string;
  };

  // Professional
  professional: {
    title: string;
    skills: Skill[];
    experience: Experience[];
    portfolio: PortfolioItem[];
    certifications: Certification[];
    availability: AvailabilityStatus;
    hourlyRate?: number;
  };

  // Wallet & Tokens
  wallet: {
    algorandAddress: string;
    connectedWallets: WalletConnection[];  // Pera, Defly, MyAlgo
    tokenBalances: TokenBalance[];
    stakedTokens: StakedToken[];
  };

  // DAO Memberships
  memberships: {
    projects: ProjectMembership[];
    votingPower: VotingPowerBreakdown;
  };

  // Reputation
  reputation: UserReputation;

  // Activity
  activity: {
    projectsContributed: number;
    projectsFounded: number;
    tasksCompleted: number;
    tokensEarned: bigint;
    proposalsCreated: number;
    votesCast: number;
    gamesPlayed: number;
  };
}
```

### 1.3 Task Marketplace (Upwork-Style)

#### Task Lifecycle

```
┌──────────┐     ┌──────────┐     ┌──────────┐     ┌──────────┐
│  POSTED  │ ──► │ ASSIGNED │ ──► │   WORK   │ ──► │  REVIEW  │
└──────────┘     └──────────┘     └──────────┘     └──────────┘
                                                        │
                       ┌────────────────────────────────┤
                       ▼                                ▼
                 ┌──────────┐                    ┌──────────┐
                 │ REVISION │                    │ APPROVED │
                 └──────────┘                    └──────────┘
                                                        │
                                                        ▼
                                                 ┌──────────┐
                                                 │  TOKENS  │
                                                 │  PAID    │
                                                 └──────────┘
```

#### Compensation Models (Token-Based)

| Model | Description | Token Flow |
|-------|-------------|------------|
| **Fixed Token** | Set token amount for task | Direct ASA transfer |
| **Hourly Token** | Time-tracked with approval | Periodic ASA release |
| **Equity %** | Percentage of project tokens | Vested allocation |
| **Hybrid** | Stable coin + project tokens | Split compensation |
| **Bounty** | Open competition | Winner takes all |
| **Milestone** | Tokens at checkpoints | Staged releases |

---

## Phase 2: Ledger Integration & Fundraising Infrastructure
**Timeline: Q2 2025**

### 2.1 Algorand Ledger Integration

```typescript
// packages/@ardanova/blockchain/src/sdk - Core Algorand integration
interface AlgorandConfig {
  network: 'testnet' | 'mainnet';
  nodeUrl: string;
  indexerUrl: string;

  // Wallet providers
  wallets: {
    pera: PeraWalletConfig;
    defly: DeflyWalletConfig;
    myAlgo: MyAlgoConfig;
  };
}

interface ASAToken {
  assetId: number;
  name: string;
  symbol: string;
  decimals: number;
  totalSupply: bigint;
  creator: string;

  // ASA properties
  freezeAddress?: string;
  clawbackAddress?: string;
  managerAddress?: string;
  reserveAddress?: string;

  // Metadata
  url?: string;       // IPFS URL for metadata
  metadataHash?: string;
}
```

### 2.2 Fundraising Lifecycle Implementation

```
┌─────────────────────────────────────────────────────────────────┐
│                    FUNDRAISING LIFECYCLE                         │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  PHASE 1: CREATION                                              │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │ 1. Project owner creates project on ArdaNova                 │   │
│  │ 2. Defines equity parameters:                             │   │
│  │    • Total supply, decimals, symbol                      │   │
│  │    • Allocation (founders, contributors, investors, etc) │   │
│  │    • Vesting schedules                                    │   │
│  │ 3. ASA created on Algorand                                │   │
│  │ 4. Pitch generated via Gamma API                         │   │
│  │ 5. Campaign published for funding                         │   │
│  │                                                           │   │
│  │ Smart Contract: ICOFactory.py                            │   │
│  │ • create_ico(params) -> asset_id                         │   │
│  │ • set_funding_goal(amount, deadline)                     │   │
│  │ • configure_vesting(schedule)                            │   │
│  └──────────────────────────────────────────────────────────┘   │
│                              │                                   │
│                              ▼                                   │
│  PHASE 2: ACTIVE FUNDING                                        │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │ 1. Users purchase shares:                                 │   │
│  │    • Via digital currency (platform's USDC-pegged asset)  │   │
│  │    • Via direct ALGO                                      │   │
│  │    • Via other project tokens (cross-investment)         │   │
│  │ 2. Contributors earn tokens:                              │   │
│  │    • Task completion -> immediate token release          │   │
│  │    • Milestone achievement -> vested allocation          │   │
│  │ 3. Players earn tokens via Game SDK                       │   │
│  │ 4. Real-time funding tracking                             │   │
│  │ 5. Transaction fees collected                             │   │
│  │                                                           │   │
│  │ Smart Contract: ICOFunding.py                            │   │
│  │ • purchase(amount, payment_asset) -> tokens              │   │
│  │ • earn_task_reward(task_id, worker) -> tokens            │   │
│  │ • earn_game_reward(game_id, player, amount) -> tokens    │   │
│  │ • get_funding_status() -> FundingStatus                  │   │
│  └──────────────────────────────────────────────────────────┘   │
│                              │                                   │
│                              ▼                                   │
│  PHASE 3: COMPLETION                                            │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │ Conditions: Funding goal reached OR deadline passed       │   │
│  │                                                           │   │
│  │ If SUCCESSFUL:                                            │   │
│  │ • Status marked as "funded"                               │   │
│  │ • Vesting schedules activated                             │   │
│  │ • DAO governance enabled                                  │   │
│  │ • Token holders can:                                      │   │
│  │   - Vote on proposals                                     │   │
│  │   - Claim revenue dividends                               │   │
│  │   - Redeem for goods/services                            │   │
│  │   - Invest into other ICOs                               │   │
│  │   - Exchange for stable coin                             │   │
│  │                                                           │   │
│  │ If FAILED:                                                │   │
│  │ • Refunds processed automatically                         │   │
│  │ • Project can restart with new terms                      │   │
│  │                                                           │   │
│  │ Smart Contract: ICOCompletion.py                         │   │
│  │ • finalize() -> CompletionStatus                         │   │
│  │ • process_refunds() -> RefundResult[]                    │   │
│  │ • enable_governance() -> GovernanceContract              │   │
│  └──────────────────────────────────────────────────────────┘   │
│                              │                                   │
│                              ▼                                   │
│  PHASE 4: POST-ICO ECOSYSTEM                                    │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │ • Tokens circulate in ecosystem                          │   │
│  │ • DAO governance active                                   │   │
│  │ • Revenue sharing to token holders                        │   │
│  │ • Cross-project investment available                      │   │
│  │ • Game integration continues                              │   │
│  │ • Analytics track token performance                       │   │
│  │                                                           │   │
│  │ Smart Contracts:                                          │   │
│  │ • Governance.py - proposals, voting                      │   │
│  │ • Treasury.py - fund management                          │   │
│  │ • Distribution.py - revenue sharing                      │   │
│  └──────────────────────────────────────────────────────────┘   │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

### 2.3 Token Economics

```typescript
interface ProjectTokenConfig {
  // Basic token info (economic rights - fungible ASA)
  name: string;
  symbol: string;
  totalSupply: bigint;
  decimals: number;  // Usually 6 on Algorand

  // Allocation percentages (economic shares only)
  allocation: {
    founders: number;      // 20% - vested over 2 years
    contributors: number;  // 30% - task rewards pool
    treasury: number;      // 20% - DAO-controlled
    investors: number;     // 20% - ICO participants
    community: number;     // 5% - airdrops, engagement
    platform: number;      // 5% - ArdaNova platform fee
  };

  // Vesting schedules
  vesting: {
    founders: {
      cliffMonths: 6;
      vestingMonths: 24;
      releaseFrequency: 'monthly';
    };
    earlyContributors: {
      cliffMonths: 3;
      vestingMonths: 12;
      releaseFrequency: 'monthly';
    };
  };

  // Economic utility (shares grant these rights)
  utility: {
    revenue: true;         // Revenue sharing (proportional to holdings)
    access: true;          // Feature access
    staking: true;         // Staking rewards
    redemption: true;      // Redeem for goods/services
  };
}

// Governance rights come from MembershipCredential, NOT from token holdings
interface MembershipCredentialConfig {
  // Soulbound credential per project (non-transferable)
  isTransferable: false;

  // How membership is earned (not purchased)
  grantMethods: {
    founder: true;                    // Auto-granted to project creator
    daoVote: true;                    // Existing members vote to grant
    contributionThreshold: true;      // Earned after sustained contribution
    applicationApproved: true;        // Membership request accepted by project
    gameSdkThreshold: true;           // Play-to-earn participation level
  };

  // Governance rights (equal for all members)
  governance: {
    votingWeight: 1;                  // Always 1 — one member, one vote
    proposalCreation: true;           // Members can create proposals
    delegationAllowed: true;          // Can delegate vote to another member
  };

  // Revocation (requires DAO vote)
  revocation: {
    requiresProposal: true;
    quorumThreshold: 0.66;           // 66% of members must vote
    passThreshold: 0.75;             // 75% must approve revocation
  };
}
```

### 2.4 Stable Coin Integration

```typescript
interface StableCoinConfig {
  // Platform stable coin (USDC-pegged ASA)
  assetId: number;
  symbol: 'DSOS';  // ArdaNova Stable
  peg: 'USDC';

  // Liquidity management
  liquidity: {
    reserves: bigint;
    collateralRatio: number;  // e.g., 1.05 for 105%
  };

  // Exchange rates
  getExchangeRate(fromAsset: number, toAsset: number): Promise<number>;

  // Swap functions
  swap(from: TokenAmount, to: number): Promise<SwapResult>;
}
```

---

## Phase 3: Project Management Hierarchy
**Timeline: Q2-Q3 2025**

### 3.1 Hierarchical Structure

```
┌─────────────────────────────────────────────────────────────────┐
│                         ROADMAP                                  │
│  Strategic vision and long-term goals for the project           │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │                    ROLE STRUCTURE                         │   │
│  │  Defines organizational hierarchy and token permissions   │   │
│  │  • Founders (initial token allocation, veto rights)       │   │
│  │  • Leaders (5%+ token holding, full management)           │   │
│  │  • Core Contributors (1%+ holding, task management)       │   │
│  │  • Contributors (any holding, task execution)             │   │
│  │  • Observers (no holding, view only)                      │   │
│  └──────────────────────────────────────────────────────────┘   │
│                              │                                   │
│                              ▼                                   │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │                        EPICS                              │   │
│  │  Large bodies of work that can be broken down             │   │
│  │  • "User Authentication System" (50 tokens reward)        │   │
│  │  • "Marketplace MVP" (200 tokens reward)                  │   │
│  └──────────────────────────────────────────────────────────┘   │
│                              │                                   │
│                              ▼                                   │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │              SPRINTS / CYCLES / TIMELINES                 │   │
│  │  Time-boxed periods for completing work                   │   │
│  │  • 2-week sprints                                         │   │
│  │  • Monthly cycles                                         │   │
│  │  • Token budget per sprint                                │   │
│  └──────────────────────────────────────────────────────────┘   │
│                              │                                   │
│                              ▼                                   │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │            PRODUCT BACKLOG ITEMS (PBIs)                   │   │
│  │  Features, enhancements, or capabilities                  │   │
│  │  • User stories with acceptance criteria                  │   │
│  │  • Token bounty attached                                  │   │
│  └──────────────────────────────────────────────────────────┘   │
│                              │                                   │
│                              ▼                                   │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │                   BACKLOG ITEMS                           │   │
│  │  Smaller work items ready for development                 │   │
│  │  • Bug fixes, improvements, research                      │   │
│  └──────────────────────────────────────────────────────────┘   │
│                              │                                   │
│                              ▼                                   │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │                 TASKS (Token-Compensated)                 │   │
│  │  Atomic units of work assignable to individuals           │   │
│  │  • Token reward (5-50 tokens typical)                     │   │
│  │  • Estimated hours, dependencies, deliverables            │   │
│  │  • Escrow until completion verified                       │   │
│  └──────────────────────────────────────────────────────────┘   │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```


---

## Phase 4: DAO Governance & On-Chain Operations
**Timeline: Q3 2025**

### 4.1 Governance Structure

```
┌─────────────────────────────────────────────────────────────────┐
│                      PROJECT DAO STRUCTURE                       │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  ┌─────────────────────────────────────────────────────────┐    │
│  │                    GOVERNANCE COUNCIL                    │    │
│  │  • Elected representatives (top token holders)           │    │
│  │  • Oversees major decisions and disputes                 │    │
│  │  • Emergency powers for critical situations              │    │
│  │  • Multi-sig treasury access                             │    │
│  └─────────────────────────────────────────────────────────┘    │
│                              │                                   │
│            ┌─────────────────┼─────────────────┐                │
│            ▼                 ▼                 ▼                │
│  ┌─────────────┐   ┌─────────────┐   ┌─────────────┐           │
│  │  FOUNDERS   │   │   WORKERS   │   │ INVESTORS   │           │
│  │  Council    │   │   Guild     │   │   Circle    │           │
│  ├─────────────┤   ├─────────────┤   ├─────────────┤           │
│  │ • Vision    │   │ • Execute   │   │ • Fund      │           │
│  │ • Strategy  │   │ • Build     │   │ • Advise    │           │
│  │ • Veto*     │   │ • Maintain  │   │ • Network   │           │
│  │ • 1 vote    │   │ • 1 vote    │   │ • 1 vote**  │           │
│  └─────────────┘   └─────────────┘   └─────────────┘           │
│                                                                  │
│  * Founder veto requires DAO proposal to override               │
│  ** Investors get governance vote ONLY if they hold a            │
│     MembershipCredential (earned via contribution, not purchase) │
│                                                                  │
│  ┌─────────────────────────────────────────────────────────┐    │
│  │                      PROPOSAL TYPES                      │    │
│  ├─────────────────────────────────────────────────────────┤    │
│  │ • Treasury:     Funding allocations, payments           │    │
│  │ • Governance:   Rule changes, role modifications        │    │
│  │ • Strategic:    Direction, partnerships, pivots         │    │
│  │ • Operational:  Day-to-day decisions, hiring            │    │
│  │ • Emergency:    Critical issues, security               │    │
│  │ • Token:        Supply changes, burns, distributions    │    │
│  └─────────────────────────────────────────────────────────┘    │
│                                                                  │
│  ┌─────────────────────────────────────────────────────────┐    │
│  │                    VOTING MECHANISMS                     │    │
│  ├─────────────────────────────────────────────────────────┤    │
│  │ • Credential-gated voting (1 MembershipCredential = 1 vote) │    │
│  │ • Quadratic voting (for community decisions)            │    │
│  │ • Conviction voting (for continuous proposals)          │    │
│  │ • Delegation (delegate credential vote to another member) │    │
│  │ • Rage quit (exit with fair share if disagree)          │    │
│  │ • On-chain execution via Algorand smart contracts       │    │
│  │ • Economic votes: share-weighted (for dividend policy)  │    │
│  └─────────────────────────────────────────────────────────┘    │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

### 4.2 Smart Contract Architecture (PyTeal)

```python
# contracts/governance/Governor.py

from pyteal import *

class GovernanceContract:
    """
    On-chain governance for project DAOs
    """

    def create_proposal(self):
        """
        Create a new governance proposal
        Required: Min 1% token holding
        """
        return Seq([
            # Verify proposer has sufficient tokens
            Assert(self.get_token_balance(Txn.sender()) >= self.min_proposal_threshold),

            # Create proposal record
            App.globalPut(
                Concat(Bytes("proposal_"), Itob(self.proposal_count)),
                Txn.application_args[1]  # Proposal data
            ),

            # Increment proposal count
            App.globalPut(
                Bytes("proposal_count"),
                self.proposal_count + Int(1)
            ),

            Approve()
        ])

    def cast_vote(self):
        """
        Cast vote on proposal
        Weight = 1 if voter holds MembershipCredential, 0 otherwise
        Economic proposals may use share-weighted voting
        """
        proposal_id = Btoi(Txn.application_args[1])
        vote_choice = Btoi(Txn.application_args[2])

        return Seq([
            # Verify voter holds MembershipCredential (soulbound governance credential)
            Assert(self.has_membership_credential(Txn.sender())),

            # Governance voting power is always 1 (one member, one vote)
            voting_power := Int(1),

            # Record vote
            App.localPut(
                Txn.sender(),
                Concat(Bytes("vote_"), Itob(proposal_id)),
                vote_choice
            ),

            # Add to vote tally
            If(vote_choice == Int(1),
                App.globalPut(
                    Concat(Bytes("yes_"), Itob(proposal_id)),
                    App.globalGet(Concat(Bytes("yes_"), Itob(proposal_id))) + voting_power
                ),
                App.globalPut(
                    Concat(Bytes("no_"), Itob(proposal_id)),
                    App.globalGet(Concat(Bytes("no_"), Itob(proposal_id))) + voting_power
                )
            ),

            Approve()
        ])

    def execute_proposal(self):
        """
        Execute passed proposal after timelock
        """
        proposal_id = Btoi(Txn.application_args[1])

        return Seq([
            # Verify proposal passed
            Assert(self.proposal_passed(proposal_id)),

            # Verify timelock elapsed
            Assert(Global.latest_timestamp() >= self.get_execution_time(proposal_id)),

            # Mark as executed
            App.globalPut(
                Concat(Bytes("executed_"), Itob(proposal_id)),
                Int(1)
            ),

            # Execute action (inner transaction)
            InnerTxnBuilder.Execute({
                TxnField.type_enum: TxnType.Payment,
                TxnField.receiver: self.get_proposal_recipient(proposal_id),
                TxnField.amount: self.get_proposal_amount(proposal_id),
            }),

            Approve()
        ])
```

---

## Phase 5: Gamma API & Pitch Studio
**Timeline: Q3-Q4 2025**

### 5.1 Pitch Generation Workflow

```
┌─────────────────────────────────────────────────────────────────┐
│                       PITCH STUDIO FLOW                          │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  ┌─────────────┐     ┌─────────────┐     ┌─────────────┐        │
│  │   PROJECT   │ ──► │    GAMMA    │ ──► │   REVIEW    │        │
│  │    DATA     │     │     API     │     │   & EDIT    │        │
│  └─────────────┘     └─────────────┘     └─────────────┘        │
│        │                    │                    │               │
│        │                    ▼                    │               │
│        │            ┌─────────────┐              │               │
│        │            │  GENERATED  │              │               │
│        │            │   SLIDES    │              │               │
│        └───────────►│   • Deck    │◄─────────────┘               │
│                     │   • Video   │                              │
│                     │   • PDF     │                              │
│                     └─────────────┘                              │
│                            │                                     │
│                            ▼                                     │
│                     ┌─────────────┐                              │
│                     │   PUBLISH   │                              │
│                     │  as ICO     │                              │
│                     │   Pitch     │                              │
│                     └─────────────┘                              │
│                            │                                     │
│                            ▼                                     │
│                     ┌─────────────┐                              │
│                     │  COMMUNITY  │                              │
│                     │   REVIEW    │                              │
│                     │   & FUND    │                              │
│                     └─────────────┘                              │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

### 5.2 Pitch Templates

| Template | Purpose | Slides | Best For |
|----------|---------|--------|----------|
| **ICO Launch** | Token offering pitch | 12-15 | New projects seeking funding |
| **Problem-Solution** | Standard project pitch | 8-10 | General awareness |
| **Team Recruitment** | Attract contributors | 6-8 | Hiring token-compensated talent |
| **Progress Update** | Stakeholder updates | 5-7 | Token holder communications |
| **Demo Day** | Competition/showcase | 10-12 | Events, accelerators |
| **Partnership** | B2B collaboration | 8-10 | Strategic partnerships |

---

## Phase 6: Game SDK & Play-to-Earn
**Timeline: Q4 2025**

### 6.1 Game SDK Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                      GAME SDK ARCHITECTURE                       │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  ┌─────────────────────────────────────────────────────────┐    │
│  │                    GAME SDK CORE                         │    │
│  │                   (TypeScript)                           │    │
│  ├─────────────────────────────────────────────────────────┤    │
│  │ • Algorand connection management                        │    │
│  │ • Token balance queries                                  │    │
│  │ • Transaction building & signing                         │    │
│  │ • Event system for callbacks                             │    │
│  │ • API authentication (JWT + API keys)                    │    │
│  └─────────────────────────────────────────────────────────┘    │
│                              │                                   │
│            ┌─────────────────┼─────────────────┐                │
│            ▼                                   ▼                │
│  ┌─────────────────────────┐    ┌─────────────────────────┐    │
│  │    UNITY SDK (C#)       │    │    GODOT SDK (C#)       │    │
│  ├─────────────────────────┤    ├─────────────────────────┤    │
│  │ • Native C# bindings    │    │ • Native C# bindings    │    │
│  │ • Unity-specific events │    │ • Godot signal system   │    │
│  │ • Prefabs for UI        │    │ • Scene integration     │    │
│  │ • Asset store ready     │    │ • Addon format          │    │
│  └─────────────────────────┘    └─────────────────────────┘    │
│                                                                  │
│  ┌─────────────────────────────────────────────────────────┐    │
│  │                    API GATEWAY                           │    │
│  │                 (api.ardanova.com)                       │    │
│  ├─────────────────────────────────────────────────────────┤    │
│  │ • Rate limiting per API key                             │    │
│  │ • Usage metering for billing                            │    │
│  │ • Request validation                                     │    │
│  │ • Algorand transaction relay                             │    │
│  └─────────────────────────────────────────────────────────┘    │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

### 6.2 C# SDK Interface

```csharp
// sdk/game-sdk-unity/ArdaNovaClient.cs

using System;
using System.Threading.Tasks;
using UnityEngine;

namespace ArdaNova.GameSDK
{
    public class ArdaNovaClient
    {
        private readonly string apiKey;
        private readonly string baseUrl = "https://api.ardanova.com";

        // Events for game integration
        public event Action<TokenBalance[]> OnBalancesUpdated;
        public event Action<Transaction> OnTransactionConfirmed;
        public event Action<string> OnError;

        public ArdaNovaClient(string apiKey)
        {
            this.apiKey = apiKey;
        }

        /// <summary>
        /// Connect player's Algorand wallet
        /// </summary>
        public async Task<WalletConnection> ConnectWallet()
        {
            // Opens wallet selection modal
            // Supports: Pera, Defly, MyAlgo
            return await WalletConnector.Connect();
        }

        /// <summary>
        /// Award project tokens to a player
        /// Used for: achievements, level ups, wins, etc.
        /// </summary>
        public async Task<AwardResult> AwardTokens(
            string playerId,
            string tokenAssetId,
            int amount,
            string reason = null)
        {
            var request = new AwardRequest
            {
                PlayerId = playerId,
                AssetId = tokenAssetId,
                Amount = amount,
                Reason = reason,
                GameId = this.gameId
            };

            var response = await PostAsync<AwardResult>("/v1/tokens/award", request);

            if (response.Success)
            {
                OnTransactionConfirmed?.Invoke(response.Transaction);
            }

            return response;
        }

        /// <summary>
        /// Get current exchange rate between tokens
        /// </summary>
        public async Task<decimal> GetExchangeRate(string fromAssetId, string toAssetId)
        {
            return await GetAsync<decimal>($"/v1/exchange/rate?from={fromAssetId}&to={toAssetId}");
        }

        /// <summary>
        /// Exchange tokens (player-initiated swap)
        /// </summary>
        public async Task<SwapResult> Exchange(
            string fromAssetId,
            string toAssetId,
            int amount)
        {
            var request = new SwapRequest
            {
                FromAsset = fromAssetId,
                ToAsset = toAssetId,
                Amount = amount
            };

            return await PostAsync<SwapResult>("/v1/exchange/swap", request);
        }

        /// <summary>
        /// Get all token balances for a player
        /// </summary>
        public async Task<TokenBalance[]> GetBalances(string playerId)
        {
            var balances = await GetAsync<TokenBalance[]>($"/v1/players/{playerId}/balances");
            OnBalancesUpdated?.Invoke(balances);
            return balances;
        }

        /// <summary>
        /// Verify a player owns specific tokens (for access control)
        /// </summary>
        public async Task<bool> VerifyTokenOwnership(
            string playerId,
            string tokenAssetId,
            int minAmount = 1)
        {
            var balance = await GetAsync<TokenBalance>(
                $"/v1/players/{playerId}/balance/{tokenAssetId}");
            return balance.Amount >= minAmount;
        }

        /// <summary>
        /// Subscribe to real-time balance updates
        /// </summary>
        public void SubscribeToBalances(string playerId)
        {
            websocket.Subscribe($"player:{playerId}:balances", (data) =>
            {
                var balances = JsonConvert.DeserializeObject<TokenBalance[]>(data);
                OnBalancesUpdated?.Invoke(balances);
            });
        }
    }

    // Data structures
    public class TokenBalance
    {
        public string AssetId { get; set; }
        public string Symbol { get; set; }
        public string Name { get; set; }
        public long Amount { get; set; }
        public int Decimals { get; set; }
        public decimal UsdValue { get; set; }
    }

    public class AwardResult
    {
        public bool Success { get; set; }
        public string TransactionId { get; set; }
        public Transaction Transaction { get; set; }
        public string Error { get; set; }
    }

    public class SwapResult
    {
        public bool Success { get; set; }
        public decimal AmountReceived { get; set; }
        public decimal ExchangeRate { get; set; }
        public decimal Fee { get; set; }
        public string TransactionId { get; set; }
    }
}
```

### 6.3 Play-to-Earn Integration Patterns

```typescript
// Example integration patterns for game developers

// Pattern 1: Achievement-based rewards
const achievementRewards = {
  'first_win': { tokens: 10, type: 'PROJECT_TOKEN' },
  'level_10': { tokens: 50, type: 'PROJECT_TOKEN' },
  'boss_defeated': { tokens: 100, type: 'PROJECT_TOKEN' },
  'tournament_winner': { tokens: 500, type: 'PROJECT_TOKEN' },
};

// Pattern 2: Time-based rewards (daily login, streaks)
const dailyRewards = {
  day1: 5,
  day2: 10,
  day3: 15,
  day7: 50,  // Weekly bonus
  day30: 200, // Monthly bonus
};

// Pattern 3: Competition pools
interface TournamentPool {
  entryFee: number;       // Tokens to enter
  prizePool: number;      // Total prize tokens
  distribution: {
    first: 0.5,           // 50% to winner
    second: 0.25,         // 25% to second
    third: 0.15,          // 15% to third
    platform: 0.10,       // 10% platform fee
  };
}

// Pattern 4: Token-gated content
interface TokenGatedContent {
  assetId: string;
  minBalance: number;
  content: {
    type: 'character' | 'level' | 'item' | 'feature';
    id: string;
  };
}
```

---

## Phase 7: AI Agent & MCP Integration
**Timeline: Q4 2025 - Q1 2026**

### 7.1 AI Agent Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                    AI AGENT ARCHITECTURE                         │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  ┌─────────────────────────────────────────────────────────┐    │
│  │              ARDANOVA-CLIENT (Next.js)                   │    │
│  │              AI Agent Chat Interface                     │    │
│  ├─────────────────────────────────────────────────────────┤    │
│  │  • Embedded agent UI in main platform                   │    │
│  │  • tRPC client for AI orchestrator communication        │    │
│  │  • Real-time streaming responses                        │    │
│  │  • Context-aware (project, user, permissions)           │    │
│  └──────────────────────────┬──────────────────────────────┘    │
│                              │ tRPC / REST                       │
│                              ▼                                   │
│  ┌─────────────────────────────────────────────────────────┐    │
│  │           ARDANOVA-AI-CLIENT (Python)                    │    │
│  │              AI Orchestrator Service                     │    │
│  ├─────────────────────────────────────────────────────────┤    │
│  │                                                          │    │
│  │  ORCHESTRATOR                                            │    │
│  │  ├── Agent routing & selection                          │    │
│  │  ├── Conversation management                            │    │
│  │  ├── Context aggregation                                │    │
│  │  └── Response streaming                                 │    │
│  │                                                          │    │
│  │  AGENTS                                                  │    │
│  │  ├── Project Manager    Sprint planning, task breakdown │    │
│  │  ├── Pitch Generator    Gamma API integration           │    │
│  │  ├── Governance Advisor DAO proposals, voting guidance  │    │
│  │  ├── Token Analyst      Tokenomics, ICO analysis        │    │
│  │  └── Code Assistant     Technical guidance              │    │
│  │                                                          │    │
│  │  MCP CLIENT                                              │    │
│  │  └── Connects to .NET MCP server for tool execution     │    │
│  │                                                          │    │
│  └──────────────────────────┬──────────────────────────────┘    │
│                              │ MCP Protocol                      │
│                              ▼                                   │
│  ┌─────────────────────────────────────────────────────────┐    │
│  │        ARDANOVA-BACKEND-API-MCP (.NET 8)                 │    │
│  │              MCP Server + REST API                       │    │
│  ├─────────────────────────────────────────────────────────┤    │
│  │                                                          │    │
│  │  MCP TOOLS (40+ implemented)                            │    │
│  │  ├── Project CRUD        Create, read, update projects  │    │
│  │  ├── Task management     Assign, complete, review       │    │
│  │  ├── Member operations   Profiles, permissions          │    │
│  │  ├── ICO tools           Create, fund, finalize         │    │
│  │  ├── DAO tools           Proposals, voting              │    │
│  │  └── Analytics           Reports, metrics               │    │
│  │                                                          │    │
│  │  RESOURCES (Context)                                     │    │
│  │  ├── project://{id}      Project details                │    │
│  │  ├── task://{id}         Task details                   │    │
│  │  ├── member://{id}       Member profile                 │    │
│  │  ├── token://{assetId}   Token info & balances          │    │
│  │  └── ico://{id}          ICO status & metrics           │    │
│  │                                                          │    │
│  │  PERMISSION LAYER                                        │    │
│  │  ├── Token-weighted tool access                         │    │
│  │  ├── Role-based permissions                             │    │
│  │  ├── Action audit logging                               │    │
│  │  └── Rate limiting per user/role                        │    │
│  │                                                          │    │
│  └─────────────────────────────────────────────────────────┘    │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

### 7.2 AI Agent Commands

```typescript
// Natural language commands supported by AI agent
const agentCommands = {
  // Project Management
  '/plan sprint': 'Suggest items for next sprint based on priority and velocity',
  '/breakdown [feature]': 'Break down a feature into token-compensated tasks',
  '/assign': 'Suggest assignments for unassigned tasks based on skills',
  '/status': 'Generate current sprint status report',
  '/risks': 'Identify current blockers and risks',

  // Content Generation
  '/pitch [template]': 'Generate pitch presentation via Gamma',
  '/update': 'Draft project update for token holders',
  '/docs [topic]': 'Generate documentation for topic',

  // Governance
  '/propose [type]': 'Draft a governance proposal',
  '/analyze proposal [id]': 'Analyze proposal implications',
  '/voting summary': 'Summarize current voting status',

  // Token Economics
  '/tokenomics': 'Generate tokenomics report',
  '/rewards estimate [task]': 'Estimate token reward for task',
  '/distribution': 'Show current token distribution',

  // ICO Management
  '/ico status': 'Get ICO funding status',
  '/ico projections': 'Project funding timeline',
  '/investor report': 'Generate investor update',

  // Analytics
  '/retro': 'Generate retrospective insights',
  '/metrics': 'Show key project metrics',
  '/health': 'Project health check',
};
```

---

## Phase 8: Platform DAO & $ARDA Token
**Timeline: Q1-Q2 2026**

### 8.1 Platform Governance

```
┌─────────────────────────────────────────────────────────────────┐
│                    ArdaNova PLATFORM DAO                             │
│           (Governs the platform itself)                          │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  ┌─────────────────────────────────────────────────────────┐    │
│  │                   $ARDA TOKEN (ASA)                      │    │
│  ├─────────────────────────────────────────────────────────┤    │
│  │ • Earned by: Contributing, staking, governance          │    │
│  │ • 5% allocation from every project ICO                  │    │
│  │ • Utility: Voting, fee discounts, premium features      │    │
│  │ • Distribution: Community (60%), Team (20%), Treasury   │    │
│  │ • Blockchain: Algorand Standard Asset (ASA)             │    │
│  └─────────────────────────────────────────────────────────┘    │
│                                                                  │
│  ┌─────────────────────────────────────────────────────────┐    │
│  │              COOPERATIVE TRUST STRUCTURE                 │    │
│  ├─────────────────────────────────────────────────────────┤    │
│  │ • Platform operated as a cooperative trust entity        │    │
│  │ • Pass-through taxation (no double taxation)            │    │
│  │ • Project shares are registered securities              │    │
│  │ • Regulatory compliance: Reg D / Reg CF / Reg A+       │    │
│  │ • MembershipCredentials are non-security governance instruments │    │
│  │ • Trust charter enforces cooperative principles         │    │
│  │ • Revenue distribution governed by $ARDA DAO            │    │
│  └─────────────────────────────────────────────────────────┘    │
│                                                                  │
│  ┌─────────────────────────────────────────────────────────┐    │
│  │                GOVERNANCE STRUCTURE                      │    │
│  ├─────────────────────────────────────────────────────────┤    │
│  │                                                          │    │
│  │  CIRCLES (Specialized Governance)                        │    │
│  │  ├── Product Circle    Feature priorities, UX            │    │
│  │  ├── Treasury Circle   Platform funds, grants           │    │
│  │  ├── Community Circle  Guidelines, disputes              │    │
│  │  ├── Technical Circle  Infrastructure, security          │    │
│  │  ├── Gaming Circle     SDK, play-to-earn policies       │    │
│  │  └── Growth Circle     Partnerships, expansion           │    │
│  │                                                          │    │
│  │  PROPOSAL TYPES                                          │    │
│  │  ├── DIP (ArdaNova Improvement Proposal)                    │    │
│  │  ├── DGP (ArdaNova Grant Proposal)                          │    │
│  │  ├── DEP (ArdaNova Expansion Proposal)                      │    │
│  │  └── DCP (ArdaNova Constitution Proposal)                   │    │
│  │                                                          │    │
│  └─────────────────────────────────────────────────────────┘    │
│                                                                  │
│  ┌─────────────────────────────────────────────────────────┐    │
│  │                   REVENUE SHARING                        │    │
│  ├─────────────────────────────────────────────────────────┤    │
│  │                                                          │    │
│  │  Platform Revenue → Distribution                         │    │
│  │  ├── 35% → Project Treasury (fund new projects)         │    │
│  │  ├── 30% → $ARDA Stakers (passive income)               │    │
│  │  ├── 25% → Operations (team, infrastructure)            │    │
│  │  └── 10% → Community Grants (ecosystem growth)          │    │
│  │                                                          │    │
│  └─────────────────────────────────────────────────────────┘    │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

### 8.2 Regional Expansion

```
┌─────────────────────────────────────────────────────────────────┐
│                   REGIONAL COOPERATIVE NETWORK                   │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│                    ┌─────────────────┐                          │
│                    │   ArdaNova Global   │                          │
│                    │   (Meta-DAO)    │                          │
│                    │   $ARDA Token   │                          │
│                    └────────┬────────┘                          │
│                             │                                    │
│         ┌───────────────────┼───────────────────┐               │
│         │                   │                   │               │
│         ▼                   ▼                   ▼               │
│  ┌─────────────┐    ┌─────────────┐    ┌─────────────┐         │
│  │ ArdaNova Africa │    │ ArdaNova LatAm  │    │  ArdaNova Asia  │         │
│  │   Regional  │    │   Regional  │    │   Regional  │         │
│  └──────┬──────┘    └──────┬──────┘    └──────┬──────┘         │
│         │                  │                  │                 │
│    ┌────┼────┐        ┌────┼────┐        ┌────┼────┐           │
│    ▼    ▼    ▼        ▼    ▼    ▼        ▼    ▼    ▼           │
│   NG   KE   ZA      BR   MX   CO      IN   ID   PH            │
│                                                                  │
│  REGIONAL AUTONOMY:                                             │
│  • Local currency integration (fiat on/off ramps)              │
│  • Localized categories & industries                           │
│  • Regional governance councils                                 │
│  • Cultural adaptation                                          │
│  • Local payment integrations                                   │
│  • Regional stable coins                                        │
│                                                                  │
│  GLOBAL COORDINATION:                                           │
│  • Shared Algorand infrastructure                              │
│  • Cross-regional collaboration                                 │
│  • Knowledge sharing                                            │
│  • Token interoperability ($ARDA universal)                    │
│  • Unified Game SDK                                             │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

---

## Technical Implementation Priorities

### Immediate (Q1 2025)

1. **Backend API & MCP Server** ✅ COMPLETE
   - ✅ .NET 8 API service (`ardanova-backend-api-mcp/`)
   - ✅ Clean Architecture (Domain, Application, Infrastructure, API, MCP)
   - ✅ 40+ MCP tools implemented
   - ✅ PostgreSQL with EF Core
   - ✅ API Key authentication
   - ✅ Event Bus for domain events
   - ✅ SignalR WebSocket for real-time updates
   - ✅ S3/Local file storage service
   - 🔄 CI/CD pipeline setup

2. **C# Model Generator** ✅ COMPLETE
   - ✅ DBML → C# entity generation (`scripts/generate-csharp-models.ts`)
   - ✅ Attribute-based EF Core configuration (`[Table]`, `[Key]`, `[Precision]`, `[Index]`)
   - ✅ Multi-FK handling with `[InverseProperty]` attributes
   - ✅ EnumStringConvention for all enums stored as strings
   - ✅ DbContext simplified from 682 → 120 lines (OnModelCreating: 570+ → 9 lines)

3. **Python AI Orchestrator** (`ardanova-ai-client/`) 🔄 STUBBED
   - ✅ Directory structure created
   - FastAPI service setup
   - MCP client connection to .NET backend
   - Agent framework (routing, context, streaming)
   - Initial agents: Project Manager, Code Assistant

4. **Main Platform Consolidation** (`ardanova-client/`)
   - Integrate DAO governance UI (proposals, voting)
   - Integrate Pitch Studio (Gamma API)
   - Integrate Exchange UI (swap, liquidity)
   - Integrate Explorer (token/transaction viewer)
   - Embed AI Agent chat interface (tRPC to Python orchestrator)
   - Gamification logic (XP, badges, leaderboards) in `/lib/gamification/`
   - Services in `/services/` (notifications, media, oracle)

### Short-term (Q2 2025)

4. **Blockchain Integration** (`ardanova-client/src/lib/blockchain/`)
   - Algorand SDK wrapper
   - Wallet connection (Pera, Defly)
   - ASA token creation & transfers
   - tRPC routers for blockchain operations

5. **ICO Infrastructure**
   - Token factory contract (`contracts/tokens/`, `contracts/ico/`)
   - ICO lifecycle management
   - Funding tracking & vesting
   - tRPC router: `server/api/routers/ico.ts`

6. **Exchange & Stable Coin**
   - Platform stable coin ASA
   - Exchange logic in `lib/blockchain/exchange/`
   - Liquidity management
   - tRPC router: `server/api/routers/exchange.ts`

### Medium-term (Q3-Q4 2025)

7. **Gamma Integration** (Pitch Studio)
   - API connection in `lib/api/gamma/`
   - Pitch templates
   - ICO pitch generation
   - App route: `app/studio/`

8. **DAO Governance**
   - On-chain proposal system (`contracts/governance/`)
   - Voting mechanisms
   - Treasury management
   - App route: `app/dao/`
   - tRPC router: `server/api/routers/dao.ts`

9. **Game SDK** (`ardanova-game-sdk/`, NuGet distribution)
   - Unity C# wrapper
   - Godot C# wrapper
   - Token operations API
   - Documentation in `/documentation/`

10. **AI Agent Expansion** (`ardanova-ai-client/`)
    - Additional agents (Pitch Generator, Governance Advisor, Token Analyst)
    - Enhanced orchestration
    - Context-aware responses
    - App route: `app/agent/`
    - tRPC router: `server/api/routers/agent.ts`

### Long-term (2026)

11. **Platform DAO**
    - $ARDA token launch
    - Revenue sharing
    - Meta-governance

12. **Game SDK v2.0**
    - Godot support
    - Tournament system
    - Advanced play-to-earn features

13. **Advanced Features**
    - Skill verification (on-chain credentials)
    - Dispute resolution
    - Cross-project collaboration

---

## Success Metrics

### Platform Health

| Metric | Target (Year 1) | Target (Year 3) |
|--------|-----------------|-----------------|
| Active Projects | 500 | 10,000 |
| Active Contributors | 5,000 | 100,000 |
| Tasks Completed | 25,000 | 1,000,000 |
| Total ICO Funding | $1M | $50M |
| Games Integrated | 10 | 500 |
| Monthly Token Transactions | 50,000 | 5,000,000 |

### Economic Indicators

| Metric | Target (Year 1) | Target (Year 3) |
|--------|-----------------|-----------------|
| Avg. Contributor Token Earnings | $500/mo equiv. | $2,000/mo equiv. |
| Project Success Rate | 40% | 60% |
| Repeat Contributors | 30% | 50% |
| Cross-Project Investment | 20% | 40% |
| Play-to-Earn Active Users | 1,000 | 100,000 |

### Governance Health

| Metric | Target (Year 1) | Target (Year 3) |
|--------|-----------------|-----------------|
| Voter Participation | 20% | 40% |
| Proposal Pass Rate | 60% | 70% |
| On-chain Governance Actions | 500 | 10,000 |
| Member Satisfaction | 4.0/5 | 4.5/5 |

---

## Call to Action

ArdaNova is building the infrastructure for a new economic paradigm—one where:

- **Workers own what they build** (tokenized equity shares)
- **Capital follows contribution** (ICO + task rewards)
- **Governance is truly democratic** (MembershipCredential = one member, one vote)
- **Economic rights are proportional** (shares reflect contribution)
- **Games create value** (play-to-earn shares + membership)
- **Success is shared** (revenue distribution via cooperative trust)

Join us in building the future of work.

---

*This roadmap is a living document. Updates will be made as we learn and iterate.*

**Website**: [ardanova.com](https://ardanova.com)

**Last Updated**: January 2025
**Version**: 4.2.0 (C# Generator & EF Core Conventions)
