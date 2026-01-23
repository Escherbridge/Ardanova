# ArdaNova Platform Architecture

This document provides a detailed technical architecture for the ArdaNova platform, covering the repository structure, service boundaries, data flow, and integration patterns.

---

## Recent Updates (January 2025)

### Architecture Simplification ✅

The architecture has been simplified to consolidate all features into a single platform:

| Component | Status | Description |
|-----------|--------|-------------|
| `ardanova-client` | ✅ Active | Main Next.js app (all features consolidated) |
| `ardanova-backend-api-mcp` | ✅ Complete | .NET 8 API + MCP Server (40+ tools) |
| `ardanova-ai-client` | 🔄 TODO | Python AI orchestrator |
| `ardanova-game-sdk` | 🔄 TODO | Unity & Godot SDKs (NuGet) |
| `contracts` | 🔄 TODO | Algorand smart contracts (PyTeal) |

**Key Changes:**
- DAO, Studio, Exchange, Explorer, and Agent UI consolidated into main platform
- No separate mobile app
- No separate docs website (lives in `/documentation/`)
- Single blockchain integration in client (`lib/blockchain/`)
- All API calls routed through tRPC

---

## Table of Contents

1. [System Overview](#system-overview)
2. [Repository Structure](#repository-structure)
3. [Application Architecture](#application-architecture)
4. [Data Flow](#data-flow)
5. [Database Design](#database-design)
6. [API Layer](#api-layer)
7. [Authentication & Authorization](#authentication--authorization)
8. [AI & MCP Integration](#ai--mcp-integration)
9. [Blockchain Integration](#blockchain-integration)
10. [Infrastructure](#infrastructure)
11. [Security](#security)

---

## System Overview

### High-Level Architecture

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                              CLIENT LAYER                                    │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  ┌────────────────────────────────────────────────────────────────────────┐ │
│  │                     ARDANOVA-CLIENT (Next.js 14)                        │ │
│  │  ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌──────────┐    │ │
│  │  │ Projects │ │   DAO    │ │  Studio  │ │ Exchange │ │  Agent   │    │ │
│  │  │Dashboard │ │Governance│ │  (Gamma) │ │  Tokens  │ │   Chat   │    │ │
│  │  └────┬─────┘ └────┬─────┘ └────┬─────┘ └────┬─────┘ └────┬─────┘    │ │
│  │       └────────────┴────────────┴─────┬──────┴────────────┘          │ │
│  │                                       │                               │ │
│  │                              ┌────────▼────────┐                     │ │
│  │                              │   tRPC Client   │                     │ │
│  │                              └────────┬────────┘                     │ │
│  └───────────────────────────────────────┼──────────────────────────────┘ │
│                                          │                                 │
└──────────────────────────────────────────┼─────────────────────────────────┘
                                           │
         ┌─────────────────────────────────┼─────────────────────────────────┐
         │                                 │                                  │
         ▼                                 ▼                                  ▼
┌─────────────────┐            ┌─────────────────────┐            ┌─────────────────┐
│  tRPC Server    │            │ ARDANOVA-AI-CLIENT  │            │ ALGORAND        │
│  (Next.js API)  │            │ (Python FastAPI)    │            │ BLOCKCHAIN      │
├─────────────────┤            ├─────────────────────┤            ├─────────────────┤
│ • project.ts    │            │ • AI Orchestrator   │            │ • ASA Tokens    │
│ • dao.ts        │───────────>│ • Agent Routing     │            │ • Governance    │
│ • exchange.ts   │            │ • MCP Client        │            │ • Treasury      │
│ • agent.ts      │            └─────────┬───────────┘            │ • Escrow        │
└────────┬────────┘                      │                        └────────┬────────┘
         │                               │ MCP Protocol                     │
         │                               ▼                                  │
         │                  ┌─────────────────────────┐                    │
         └─────────────────>│ ARDANOVA-BACKEND-API    │<───────────────────┘
                            │ (.NET 8 + MCP Server)   │
                            ├─────────────────────────┤
                            │ • REST Controllers      │
                            │ • 40+ MCP Tools         │
                            │ • Clean Architecture    │
                            └─────────────┬───────────┘
                                          │
                              ┌───────────▼───────────┐
                              │      DATA LAYER       │
                              ├───────────────────────┤
                              │  PostgreSQL (Primary) │
                              │  Redis (Cache)        │
                              │  S3 (Media)           │
                              └───────────────────────┘
```

### Core Principles

1. **Simplicity** - Single platform with all features consolidated
2. **Type Safety** - End-to-end TypeScript with tRPC
3. **Clean Architecture** - Separation of concerns in backend
4. **AI-First** - Native AI integration via MCP protocol
5. **Decentralization** - Progressive decentralization via Algorand DAOs

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
├── ardanova-backend-api-mcp/           # .NET 8 Backend + MCP Server
│   ├── ardanova.sln                    # Solution file (6 projects)
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
├── documentation/                      # All documentation
│   ├── ARCHITECTURE.md                 # This file
│   ├── ROADMAP.md                      # Development roadmap
│   └── ...                             # API docs, SDK docs, guides
│
├── docker-compose.yml                  # Local development orchestration
├── railway.toml                        # Root Railway deployment config
└── README.md                           # Project overview
```

---

## Application Architecture

### Client Application (ardanova-client)

The main platform is a Next.js 14 application using the App Router pattern.

```
┌─────────────────────────────────────────────────────────────────┐
│                    ARDANOVA-CLIENT ARCHITECTURE                  │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  ┌─────────────────────────────────────────────────────────┐    │
│  │                     APP ROUTES                           │    │
│  │  /auth, /dashboard, /projects, /dao, /studio,           │    │
│  │  /exchange, /explorer, /agent, /marketplace             │    │
│  └────────────────────────┬────────────────────────────────┘    │
│                           │                                      │
│                           ▼                                      │
│  ┌─────────────────────────────────────────────────────────┐    │
│  │                    tRPC CLIENT                           │    │
│  │  • React Query integration                               │    │
│  │  • Type-safe API calls                                   │    │
│  │  • Optimistic updates                                    │    │
│  └────────────────────────┬────────────────────────────────┘    │
│                           │                                      │
│                           ▼                                      │
│  ┌─────────────────────────────────────────────────────────┐    │
│  │                    tRPC SERVER                           │    │
│  │  server/api/routers/                                     │    │
│  │  ├── project.ts   → apiClient.projects.*                │    │
│  │  ├── dao.ts       → apiClient.governance.*              │    │
│  │  ├── exchange.ts  → apiClient.exchange.*                │    │
│  │  └── agent.ts     → AI orchestrator API                 │    │
│  └────────────────────────┬────────────────────────────────┘    │
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

### tRPC Router Pattern

All API calls flow through tRPC routers that call the API client:

```typescript
// server/api/routers/project.ts
export const projectRouter = createTRPCRouter({
  create: protectedProcedure
    .input(createProjectSchema)
    .mutation(async ({ input, ctx }) => {
      const response = await apiClient.projects.create({
        ...input,
        createdById: ctx.session.user.id,
      });
      if (response.error) throw new Error(response.error);
      return response.data;
    }),

  getAll: publicProcedure
    .input(z.object({ limit: z.number(), page: z.number() }))
    .query(async ({ input }) => {
      const response = await apiClient.projects.getPaged(input.page, input.limit);
      return response.data;
    }),
});
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
                                                                      ▼
                                                               ┌──────────┐
                                                               │PostgreSQL│
                                                               └──────────┘
```

### AI Agent Flow: Client → tRPC → Python Orchestrator → MCP Server

```
┌──────────┐    ┌──────────┐    ┌──────────┐    ┌──────────┐    ┌──────────┐
│  Agent   │───>│  tRPC    │───>│  tRPC    │───>│  Python  │───>│  .NET    │
│  Chat UI │    │  Client  │    │  Server  │    │Orchestrator   │  MCP     │
└──────────┘    └──────────┘    └──────────┘    └──────────┘    └────┬─────┘
                                                    │                 │
                                                    │ Agent routing   │ MCP tools
                                                    │ Context mgmt    │ Resources
                                                    ▼                 ▼
                                              ┌──────────┐    ┌──────────┐
                                              │   LLM    │    │PostgreSQL│
                                              │  Claude  │    └──────────┘
                                              └──────────┘
```

---

## Database Design

### Project Management Hierarchy

The platform uses a hierarchical project management structure aligned with agile methodologies:

```
PROJECT
└── ROADMAP (Strategic vision)
    │
    └── PROJECT MEMBERS (Token-weighted roles)
        │   ├── Founder (10%+ tokens, all permissions)
        │   ├── Leader (5%+ tokens, roadmap management)
        │   ├── Core Contributor (1%+ tokens, epic/task management)
        │   ├── Contributor (any tokens, task execution)
        │   └── Observer (no tokens, read-only)
        │
        └── EPICS (Large bodies of work)
            │
            └── SPRINTS (Time-boxed periods)
                │
                └── PRODUCT BACKLOG ITEMS (PBIs)
                    │   (Features, enhancements, capabilities)
                    │
                    └── BACKLOG ITEMS
                        │   (Smaller work items, bugs, research)
                        │
                        └── TASKS (Token-Compensated)
                            (Atomic units of work with token rewards)
```

### Entity Relationship Overview

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                              USER & GAMIFICATION                             │
└─────────────────────────────────────────────────────────────────────────────┘

┌──────────────────┐      ┌──────────────────┐      ┌──────────────────┐
│       User       │─────<│   XPEvent        │      │   Achievement    │
├──────────────────┤      ├──────────────────┤      ├──────────────────┤
│ id               │      │ userId           │      │ id               │
│ email, name      │      │ eventType        │      │ name             │
│ totalXP          │      │ amount           │      │ category         │
│ level            │      │ source           │      │ criteria (JSON)  │
│ tier             │      │ sourceId         │      │ xpReward         │
│ trustScore       │      │ metadata         │      │ tokenReward      │
│ verificationLevel│      └──────────────────┘      │ rarity           │
└──────────────────┘                                └────────┬─────────┘
         │                                                   │
         │         ┌─────────────────────────────────────────┘
         │         │
         ▼         ▼
┌──────────────────────────────────────┐
│          UserAchievement             │
├──────────────────────────────────────┤
│ userId, achievementId                │
│ earnedAt, progress                   │
└──────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────────┐
│                         PROJECT & WORK HIERARCHY                             │
└─────────────────────────────────────────────────────────────────────────────┘

┌──────────────────┐      ┌──────────────────┐      ┌──────────────────┐
│     Project      │─────>│     Roadmap      │─────<│  RoadmapPhase    │
├──────────────────┤      ├──────────────────┤      ├──────────────────┤
│ id, title, slug  │      │ projectId        │      │ roadmapId        │
│ description      │      │ title, vision    │      │ name, order      │
│ status, category │      │ status           │      │ startDate        │
│ fundingGoal      │      │ createdAt        │      │ endDate          │
│ tokenAssetId     │      └──────────────────┘      │ status           │
└──────────────────┘                                └────────┬─────────┘
         │                                                   │
         │                                                   ▼
         │         ┌──────────────────┐      ┌──────────────────┐
         │         │   ProjectMember  │      │      Epic        │
         │         ├──────────────────┤      ├──────────────────┤
         └────────>│ projectId        │      │ phaseId          │
                   │ userId           │      │ title, status    │
                   │ role             │      │ priority         │
                   │ tokenBalance     │      │ tokenBudget      │
                   │ votingPower      │      │ progress         │
                   │ joinedAt         │      └────────┬─────────┘
                   └──────────────────┘               │
                                                      │
                            ┌─────────────────────────┴───────────────────────┐
                            │                                                  │
                            ▼                                                  ▼
                   ┌──────────────────┐                           ┌──────────────────┐
                   │     Sprint       │                           │       PBI        │
                   ├──────────────────┤                           ├──────────────────┤
                   │ projectId        │                           │ epicId           │
                   │ name, goal       │                           │ title, type      │
                   │ startDate        │<─────────────────────────>│ storyPoints      │
                   │ endDate          │                           │ status           │
                   │ tokenBudget      │                           │ acceptanceCrit.  │
                   │ velocity         │                           └────────┬─────────┘
                   │ status           │                                    │
                   └──────────────────┘                                    ▼
                                                              ┌──────────────────┐
                                                              │   BacklogItem    │
                                                              ├──────────────────┤
                                                              │ pbiId            │
                                                              │ title, type      │
                                                              │ status           │
                                                              │ estimate         │
                                                              └────────┬─────────┘
                                                                       │
                                                                       ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                              TASK (Token-Compensated)                        │
├─────────────────────────────────────────────────────────────────────────────┤
│  ┌──────────────────┐     ┌──────────────────┐     ┌──────────────────┐    │
│  │  ProjectTask     │────>│ TaskCompensation │     │ TaskSubmission   │    │
│  ├──────────────────┤     ├──────────────────┤     ├──────────────────┤    │
│  │ backlogItemId    │     │ taskId           │     │ taskId           │    │
│  │ sprintId         │     │ model (FIXED,    │     │ submittedById    │    │
│  │ assigneeId       │     │   HOURLY, EQUITY,│     │ content          │    │
│  │ title, status    │     │   HYBRID, BOUNTY)│     │ status           │    │
│  │ priority         │     │ tokenAmount      │     │ reviewedById     │    │
│  │ estimatedHours   │     │ equityPercent    │     │ feedback         │    │
│  │ tokenReward      │     │ vestingMonths    │     │ submittedAt      │    │
│  │ escrowStatus     │     └──────────────────┘     └──────────────────┘    │
│  └──────────────────┘                                                       │
└─────────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────────┐
│                              GOVERNANCE (DAO)                                │
└─────────────────────────────────────────────────────────────────────────────┘

┌──────────────────┐      ┌──────────────────┐      ┌──────────────────┐
│    Proposal      │─────<│      Vote        │>─────│      User        │
├──────────────────┤      ├──────────────────┤      └──────────────────┘
│ projectId        │      │ proposalId       │
│ creatorId        │      │ voterId          │
│ type (TREASURY,  │      │ choice           │
│  GOVERNANCE,     │      │ weight           │
│  STRATEGIC, etc) │      │ reason           │
│ title, desc      │      │ txHash           │
│ status           │      └──────────────────┘
│ quorum, threshold│
│ votingStart/End  │
│ executionDelay   │
└──────────────────┘
         │
         ▼
┌──────────────────┐      ┌──────────────────┐
│ProposalExecution │      │    Treasury      │
├──────────────────┤      ├──────────────────┤
│ proposalId       │      │ projectId        │
│ executedAt       │      │ balance          │
│ txHash           │      │ tokenAssetId     │
│ result           │      │ transactions[]   │
└──────────────────┘      └──────────────────┘

┌─────────────────────────────────────────────────────────────────────────────┐
│                              TOKEN & ICO                                     │
└─────────────────────────────────────────────────────────────────────────────┘

┌──────────────────┐      ┌──────────────────┐      ┌──────────────────┐
│  ProjectToken    │─────<│   TokenHolder    │>─────│      User        │
├──────────────────┤      ├──────────────────┤      └──────────────────┘
│ projectId        │      │ tokenId          │
│ assetId (Algo)   │      │ userId           │
│ name, symbol     │      │ balance          │
│ totalSupply      │      │ stakedAmount     │
│ decimals         │      │ vestingSchedule  │
│ allocation (JSON)│      └──────────────────┘
│ vestingConfig    │               │
└──────────────────┘               │
         │                         ▼
         │            ┌──────────────────┐
         │            │  TokenVesting    │
         │            ├──────────────────┤
         │            │ holderId         │
         │            │ totalAmount      │
         │            │ releasedAmount   │
         │            │ cliffEnd         │
         │            │ vestingEnd       │
         │            │ releaseFrequency │
         ▼            └──────────────────┘
┌──────────────────┐
│       ICO        │
├──────────────────┤
│ projectId        │
│ tokenId          │
│ fundingGoal      │
│ currentFunding   │
│ startDate        │
│ endDate          │
│ status           │
│ contributions[]  │
└──────────────────┘
```

### Key Schema Relationships

| Parent | Child | Relationship |
|--------|-------|--------------|
| Project | Roadmap | 1:1 |
| Roadmap | RoadmapPhase | 1:N |
| RoadmapPhase | Epic | 1:N |
| Epic | PBI | 1:N |
| PBI | BacklogItem | 1:N |
| BacklogItem | ProjectTask | 1:N |
| Sprint | ProjectTask | N:M (via SprintItem) |
| Project | ProjectMember | 1:N |
| Project | Proposal | 1:N |
| Project | ProjectToken | 1:1 |
| ProjectToken | ICO | 1:1 |
| User | XPEvent | 1:N |
| User | UserAchievement | 1:N |

---

## API Layer

### .NET Backend (ardanova-backend-api-mcp)

The backend uses Clean Architecture with the following layers:

```
┌─────────────────────────────────────────────────────────────┐
│                    ArdaNova.API                              │
│              (Controllers, Middleware)                       │
├──────────────────────────┬──────────────────────────────────┤
│                          │                                   │
│     ArdaNova.MCP         │      ArdaNova.Application         │
│   (MCP Tool Classes)     │    (Services, DTOs, Interfaces)   │
├──────────────────────────┴──────────────────────────────────┤
│                   ArdaNova.Infrastructure                    │
│              (DbContext, Repository Impl)                    │
├─────────────────────────────────────────────────────────────┤
│                     ArdaNova.Domain                          │
│              (Entities, Enums, Value Objects)                │
└─────────────────────────────────────────────────────────────┘
```

**Key Components:**
- **Domain**: 25+ entities with private constructors + factory methods
- **Application**: Services with Result<T> pattern, AutoMapper for DTOs
- **Infrastructure**: EF Core with PostgreSQL, Generic Repository pattern
- **API**: REST controllers with Swagger, API Key middleware
- **MCP**: 40+ tools for AI agent integration

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

Role-based permissions for project access:

| Role | Level | Permissions |
|------|-------|-------------|
| Founder | 100 | All permissions |
| Leader | 80 | Roadmap, treasury propose, member management |
| Core Contributor | 60 | Epic CRUD, task assign/review, proposals |
| Contributor | 40 | Task claim/submit, vote, agent use |
| Observer | 20 | Read-only access |

---

## AI & MCP Integration

### Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                    AI AGENT ARCHITECTURE                         │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  ┌─────────────────────────────────────────────────────────┐    │
│  │              ARDANOVA-CLIENT (Next.js)                   │    │
│  │              AI Agent Chat Interface                     │    │
│  │  • Embedded agent UI in main platform                   │    │
│  │  • tRPC client for AI orchestrator communication        │    │
│  │  • Real-time streaming responses                        │    │
│  └──────────────────────────┬──────────────────────────────┘    │
│                              │ tRPC / REST                       │
│                              ▼                                   │
│  ┌─────────────────────────────────────────────────────────┐    │
│  │           ARDANOVA-AI-CLIENT (Python)                    │    │
│  │              AI Orchestrator Service                     │    │
│  │                                                          │    │
│  │  ORCHESTRATOR                                            │    │
│  │  ├── Agent routing & selection                          │    │
│  │  ├── Conversation management                            │    │
│  │  └── Response streaming                                 │    │
│  │                                                          │    │
│  │  AGENTS                                                  │    │
│  │  ├── Project Manager    Sprint planning, task breakdown │    │
│  │  ├── Pitch Generator    Gamma API integration           │    │
│  │  ├── Governance Advisor DAO proposals, voting guidance  │    │
│  │  └── Token Analyst      Tokenomics, ICO analysis        │    │
│  │                                                          │    │
│  │  MCP CLIENT → Connects to .NET MCP server               │    │
│  └──────────────────────────┬──────────────────────────────┘    │
│                              │ MCP Protocol                      │
│                              ▼                                   │
│  ┌─────────────────────────────────────────────────────────┐    │
│  │        ARDANOVA-BACKEND-API-MCP (.NET 8)                 │    │
│  │              MCP Server + REST API                       │    │
│  │                                                          │    │
│  │  MCP TOOLS (40+ implemented)                            │    │
│  │  ├── user_get_by_id, user_create, user_update           │    │
│  │  ├── project_create, project_get, project_update        │    │
│  │  ├── task_assign, task_complete, task_review            │    │
│  │  ├── proposal_create, proposal_vote                     │    │
│  │  └── analytics_*, report_*                              │    │
│  └─────────────────────────────────────────────────────────┘    │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

### MCP Tool Example

```csharp
[McpServerToolType]
public class ProjectTools
{
    [McpServerTool(Name = "project_create")]
    [Description("Creates a new project")]
    public async Task<ProjectDto?> CreateProject(
        [Description("Project title")] string title,
        [Description("Project description")] string description,
        [Description("Category")] string category,
        CancellationToken ct = default)
    {
        var result = await _projectService.CreateAsync(new CreateProjectDto
        {
            Title = title,
            Description = description,
            Category = category
        }, ct);
        return result.IsSuccess ? result.Value : null;
    }
}
```

---

## Blockchain Integration

### Algorand Smart Contracts

Smart contracts are written in PyTeal and stored in `/contracts/`:

```
contracts/
├── governance/     # DAO governance (proposals, voting)
├── tokens/         # ASA token factories
├── ico/            # ICO lifecycle contracts
├── exchange/       # DEX/swap contracts
└── escrow/         # Task payment escrow
```

### Client Integration

Algorand integration lives in `ardanova-client/src/lib/blockchain/`:

```
lib/blockchain/
├── sdk/            # Core Algorand SDK wrapper
├── tokens/         # ASA token management
├── ico/            # ICO lifecycle management
├── exchange/       # Token exchange logic
├── wallet/         # Wallet connection (Pera, Defly)
├── indexer/        # Algorand indexer client
└── contracts/      # Smart contract ABIs & interactions
```

---

## Infrastructure

### Deployment Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                    DEPLOYMENT ARCHITECTURE                      │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  ┌─────────────────────────────────────────────────────────┐    │
│  │                  CDN (Vercel Edge)                      │    │
│  │  • Static assets, Edge functions, Image optimization    │    │
│  └─────────────────────────────────────────────────────────┘    │
│                              │                                  │
│                              ▼                                  │
│  ┌─────────────────────────────────────────────────────────┐    │
│  │                  APPLICATION TIER                       │    │
│  │  ┌────────────────────────────────────────────────────┐ │    │
│  │  │         ardanova-client (Railway)                  │ │    │
│  │  │         www.ardanova.com                           │ │    │
│  │  └────────────────────────────────────────────────────┘ │    │
│  └─────────────────────────────────────────────────────────┘    │
│                              │                                  │
│                              ▼                                  │
│  ┌─────────────────────────────────────────────────────────┐    │
│  │                    SERVICE TIER                         │    │
│  │  ┌──────────────┐  ┌──────────────┐                     │    │
│  │  │ .NET Backend │  │ Python AI    │                     │    │
│  │  │ api.ardanova │  │ ai.ardanova  │                     │    │
│  │  │ (Railway)    │  │ (Railway)    │                     │    │
│  │  └──────────────┘  └──────────────┘                     │    │
│  └─────────────────────────────────────────────────────────┘    │
│                              │                                  │
│                              ▼                                  │
│  ┌─────────────────────────────────────────────────────────┐    │
│  │                      DATA TIER                          │    │
│  │  ┌──────────┐  ┌──────────┐  ┌──────────┐               │    │
│  │  │PostgreSQL│  │  Redis   │  │    S3    │               │    │
│  │  │(Railway) │  │ (Upstash)│  │(Cloudflare)│             │    │
│  │  └──────────┘  └──────────┘  └──────────┘               │    │
│  └─────────────────────────────────────────────────────────┘    │
│                              │                                  │
│                              ▼                                  │
│  ┌─────────────────────────────────────────────────────────┐    │
│  │                   BLOCKCHAIN TIER                       │    │
│  │  Algorand (TestNet → MainNet)                           │    │
│  └─────────────────────────────────────────────────────────┘    │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

### Subdomains

| Subdomain | Service | Description |
|-----------|---------|-------------|
| www.ardanova.com | ardanova-client | Main platform (all features) |
| api.ardanova.com | ardanova-backend-api-mcp | .NET 8 API + MCP Server |
| ai.ardanova.com | ardanova-ai-client | Python AI orchestrator |

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

4. **API Security**
   - Input validation (Zod schemas)
   - SQL injection prevention (EF Core)
   - Rate limiting
   - CORS configuration

5. **Smart Contract Security**
   - PyTeal best practices
   - Multi-sig for admin functions
   - Timelocks for governance
   - Audits before mainnet

---

## Next Steps

1. ✅ **Phase 1**: .NET Backend API with Clean Architecture - **COMPLETE**
2. ✅ **Phase 2**: MCP Tools Integration (40+ tools) - **COMPLETE**
3. 🔄 **Phase 3**: Python AI Orchestrator setup
4. 🔄 **Phase 4**: Consolidate DAO, Studio, Exchange, Explorer into client
5. **Phase 5**: Blockchain integration (Algorand)
6. **Phase 6**: Smart contract development (PyTeal)
7. **Phase 7**: Game SDK (Unity, Godot)

See [ROADMAP.md](./ROADMAP.md) for detailed timelines.

---

**Last Updated**: January 2025
**Version**: 3.0.0 (Simplified Architecture)
