# ArdaNova — A cooperative project and work platform

<div align="center">

**Discover a problem. Define a solution. Iterate.**

[![TypeScript](https://img.shields.io/badge/TypeScript-5.8-blue.svg)](https://www.typescriptlang.org/)
[![Next.js](https://img.shields.io/badge/Next.js-15-black.svg)](https://nextjs.org/)
[![Algorand](https://img.shields.io/badge/Algorand-ASA-black.svg)](https://algorand.com/)
[![Prisma](https://img.shields.io/badge/Prisma-6.19-2D3748.svg)](https://www.prisma.io/)

[Roadmap](./documentation/ROADMAP.md) | [Architecture](./documentation/ARCHITECTURE.md) | [Frontend experience](./documentation/FRONTEND_EXPERIENCE.md) | [Language](./documentation/LINGUISTIC_GUIDE.md) | [Release playbook](./documentation/FRONTEND_RELEASE_PLAYBOOK.md) | [Documentation](#documentation)

</div>

---

## Vision

ArdaNova is **social media for doing, not doom-scrolling**: a shared workspace that helps people move from a lived problem to accountable, community-supported work. It brings project formation, contribution, coordination, governance, and transparent records together without turning attention into the product.

- **Project formation** — shape a brief, terms, milestones, and participation model
- **Work coordination** — connect contributors with tasks and opportunities
- **Community support** — contribute time, skills, resources, or funds under visible terms
- **Member-led governance** — use explicit credentials and decision rules without implying economic rights
- **Legible value** — distinguish project-token utility, membership credentials, and separately approved ownership shares
- **Nova-assisted workflows** — draft, review, present, and rehearse while a person remains in control
- **Game integrations** — connect supported Unity and Godot experiences as those SDK paths mature

The frontend follows one trust rule throughout: a draft, submitted intent, confirmed operation, and reconciled result are different states. Interface availability is not evidence that a financial, governance, ledger, or AI integration is complete.

### Experience direction

The 2026 frontend redesign uses flat neobrutalism with abstract-expressionist energy: warm parchment, ink, signal red, restrained system cyan, hard rules, oversized editorial type, and no decorative glow. Public pages invite imagination; the authenticated workspace stays quiet and operational.

Nova is designed as a collaborator beside the artifact rather than an autonomous agent. It can propose and explain changes, but it cannot publish, approve governance, move funds, release escrow, swap assets, issue credentials, pay contributors, or change rights. See [Frontend experience](./documentation/FRONTEND_EXPERIENCE.md) and [Brand guidelines](./documentation/BRAND_GUIDELINES.md).

### The solutionary loop

ArdaNova's core loop is **discover a problem -> define a solution -> iterate**. A solutionary approach is revolutionary because it turns passive reaction into shared inquiry, a reviewable proposal, and sustained learning with the people affected.

| Stage              | The work                                                                                     | Useful output                    |
| ------------------ | -------------------------------------------------------------------------------------------- | -------------------------------- |
| Discover a problem | Gather lived context, affected people, evidence, constraints, and open questions             | An inspectable problem statement |
| Define a solution  | Shape assumptions, scope, roles, milestones, terms, decision authority, and success measures | A reviewable solution brief      |
| Iterate            | Contribute, test, compare, reconcile, revise, pause, or stop                                 | Evidence for the next decision   |

This is a loop rather than an engagement funnel. Social activity should help people find field notes, questions, proposals, contributions, decisions, and next moves - not encourage endless consumption, virality, or time-on-platform.

ArdaNova also treats technology as infrastructure for **empowering ownership, not surveillance**. The interface should explain what context is used, why it is needed, and who can act. Ownership language must always name the specific, documented right; it is never inferred from activity, membership, or a token alone. See the [Linguistic guide](./documentation/LINGUISTIC_GUIDE.md) for the full vocabulary, CTA, community activity, and Nova voice system.

| Record                | What it may represent                                              | What it does not imply                                |
| --------------------- | ------------------------------------------------------------------ | ----------------------------------------------------- |
| Membership credential | Eligibility or governance participation under stated project rules | Equity, transferability, dividends, or redemption     |
| Project token         | Only the utility disclosed for that project                        | Membership, voting rights, or ownership               |
| Ownership share       | Separately approved economic rights under its governing terms      | Automatic governance authority or a guaranteed return |

Projects may use one or more of these records, but they remain separate. A contribution creates only the compensation or rights explicitly offered, accepted, and reconciled.

---

## Platform capabilities

ArdaNova unifies collaboration with explicit trust and value records:

| Collaboration layer                         | Trust and value layer                                     |
| ------------------------------------------- | --------------------------------------------------------- |
| Project planning, milestones, and work      | Evidence-rich activity and state history                  |
| Tasks, opportunities, guilds, and community | Role, permission, and membership credentials              |
| Funding and resource-support workflows      | Project-token utility under disclosed terms               |
| Nova drafting and presentation interface    | Human review before publication or rights-changing action |
| Unity and Godot SDK paths                   | Separately approved ownership and settlement records      |

Some surfaces are active, some depend on configured external services, and some are interface previews. Route presence alone is not a completion claim; consult the relevant runbook, API contract, and release notes.

---

## Core Features

### Repository capability snapshot

Status describes implementation presence in this repository, not automatic availability in every environment. External-service configuration, migrations, authorization, and end-to-end reconciliation may still be required.

| Feature                    | Description                                                                                | Status            |
| -------------------------- | ------------------------------------------------------------------------------------------ | ----------------- |
| **Project Management**     | Full CRUD with rich metadata, categories, and status tracking                              | Complete          |
| **Google OAuth**           | NextAuth authentication; provider configuration required per environment                   | Available         |
| **User Profiles**          | Roles, types, skills, and verification levels                                              | Complete          |
| **Community Support**      | Voting, subscriptions, volunteering, applications                                          | Complete          |
| **Guild System**           | Professional guilds, membership, bidding, and reviews                                      | Complete          |
| **Shop System**            | Shop management, products, inventory, analytics                                            | Complete          |
| **Value Tracking**         | Project-token, credential, and ownership data kept distinct; reconciliation work continues | In Progress       |
| **Nova Studio**            | Human-controlled drafting, review, presentation, and rehearsal interaction model           | Interface Preview |
| **.NET Backend API**       | Clean Architecture API with 6 projects, 40+ MCP tools                                      | Complete          |
| **Membership Credentials** | Read-only checkpoint while issuance and lifecycle authorization are hardened               | In Progress       |
| **Credential Utility**     | Read-only checkpoint for Algorand minting and credential-tier operations                   | In Progress       |
| **Azoa Identity & Custody** | Tenant-bound account orchestration and fresh Azoa KYC gates; production custody/provider required | Fail-closed seam  |
| **Event Bus**              | In-memory domain event publishing and handling                                             | Complete          |
| **SignalR WebSocket**      | Real-time updates for activities, notifications, projects                                  | Complete          |
| **S3/Local Storage**       | File attachments with S3 or local filesystem support                                       | Complete          |
| **C# Model Generator**     | DBML → C# entities with EF Core attributes                                                 | Complete          |

### Roadmap (v2.0+)

| Feature                       | Description                                                           | Timeline            |
| ----------------------------- | --------------------------------------------------------------------- | ------------------- |
| **Gamification Layer**        | XP, levels, achievements, leaderboards, seasons                       | Q1 2025             |
| **Project Hierarchy**         | Roadmaps → Epics → Sprints → PBIs → Tasks                             | Q2 2025             |
| **Task Marketplace**          | Bounties, bidding, escrow, multiple compensation models               | Q2 2025             |
| **Secure Ledger Integration** | Soulbound credentials (ASA), ownership shares, wallet connect         | In Progress         |
| **Fundraising Lifecycle**     | Equity creation, funding phases, redemption                           | Q3 2025             |
| **Gamma Integration**         | External presentation generation behind the reviewed Nova Studio flow | Integration pending |
| **Cooperative Governance**    | Transparent proposals, voting, treasury                               | Q3 2025             |
| **Game SDK**                  | Unity/Godot C# SDK for play-to-earn                                   | Q4 2025             |
| **MCP Agent**                 | AI agent for project management with role-based access                | Q4 2025             |
| **Cross-Project Investment**  | Invest completed shares into new projects                             | Q1 2026             |
| **Platform Cooperative**      | $ARDA shares, revenue sharing, meta-governance                        | Q2 2026             |

See the full [Roadmap](./documentation/ROADMAP.md) for detailed plans.

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
└── ardanova-backend-api-mcp/     # .NET 10 Backend
    └── api-server/
        └── src/
            ├── ArdaNova.Domain/        # Entities, Enums
            ├── ArdaNova.Application/   # Services, DTOs
            ├── ArdaNova.Infrastructure/# DbContext, Repos
            ├── ArdaNova.API/           # Controllers
            └── ArdaNova.MCP/           # 40+ MCP Tools
```

### Data Flow

| Layer          | Technology                                          | Purpose                                                    |
| -------------- | --------------------------------------------------- | ---------------------------------------------------------- |
| **Frontend**   | Next.js + tRPC                                      | UI, validation, session                                    |
| **API Client** | TypeScript                                          | Typed backend communication                                |
| **Backend**    | .NET 10 Clean Architecture                          | Business logic, data access                                |
| **Real-time**  | SignalR                                             | WebSocket for live updates                                 |
| **Events**     | In-Memory Event Bus                                 | Domain event publishing                                    |
| **Auth**       | NextAuth JWT sessions + Prisma user synchronization | Google authentication and server-side authorization claims |
| **Storage**    | S3 / Local filesystem                               | File attachments                                           |
| **Blockchain** | Algorand (dotnet-algorand-sdk)                      | Soulbound credential ASAs                                  |
| **Database**   | PostgreSQL                                          | Persistent storage                                         |

See [ARCHITECTURE.md](./documentation/ARCHITECTURE.md) for detailed technical documentation.

---

## Technology Stack

### Current Stack

| Layer          | Technology                                                       |
| -------------- | ---------------------------------------------------------------- |
| **Frontend**   | Next.js 15, React 19, TypeScript 5.8                             |
| **UI**         | Radix UI, Tailwind CSS 4.0                                       |
| **API Layer**  | tRPC (frontend) → .NET 10 (backend)                              |
| **Database**   | PostgreSQL                                                       |
| **Schema**     | DBML → Prisma (migrations) + EF Core (C# entities via generator) |
| **Auth**       | NextAuth 5 JWT sessions + custom Prisma user synchronization     |
| **State**      | TanStack Query 5                                                 |
| **Deployment** | Railway (frontend, API, and database)                            |

### Secure Ledger Stack (ArdaNova)

| Layer                     | Technology                             |
| ------------------------- | -------------------------------------- |
| **Ledger**                | Algorand (10,000+ TPS, 2.85s finality) |
| **Shares/Equity**         | Standard Assets (ASAs)                 |
| **Credential SDK**        | dotnet-algorand-sdk (C# native)        |
| **Soulbound Credentials** | Frozen ASAs with ARC-19 metadata       |
| **Automated Agreements**  | AVM / TEAL                             |
| **Digital Wallets**       | Pera Wallet, Defly, MyAlgo             |
| **Indexer**               | Algorand Indexer API                   |
| **Node**                  | AlgoNode / Self-hosted                 |

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

| Layer                | Technology                                    | Status      |
| -------------------- | --------------------------------------------- | ----------- |
| **Monorepo**         | Turborepo                                     | Planned     |
| **Backend Services** | Rust (Warp API) for high-performance services | Planned     |
| **AI/LLM**           | Gamma API, Claude MCP                         | In Progress |
| **Mobile**           | React Native / Expo (Wallet App)              | Planned     |
| **Real-time**        | SignalR WebSocket                             | ✅ Complete |
| **Payments**         | Stable coin (USDC-pegged), Direct ALGO        | Planned     |
| **Storage**          | S3/Local file storage                         | ✅ Complete |
| **Search**           | Meilisearch / Algolia                         | Planned     |
| **Game SDK**         | C# for Unity & Godot                          | Planned     |

---

## Solutionary project lifecycle

ArdaNova organizes projects around a repeatable, inspectable loop:

```text
Discover a problem -> Define a solution -> Iterate
        ^                                      |
        +--------------------------------------+
```

- **Discover** documents affected people, lived context, evidence, constraints,
  and open questions.
- **Define** turns that context into a reviewable proposal with explicit scope,
  roles, participation terms, decision authority, and measures of progress.
- **Iterate** records work, decisions, evidence, exceptions, and lessons before
  the next version—or a documented stop.

Nova may help draft, review, present, and rehearse these artifacts. A person
must accept changes, and publication, funding, governance, credentials,
settlement, or rights-changing actions remain separate explicit workflows.
Current interfaces distinguish **draft**, **submitted**, **confirmed**, and
**reconciled** state; the UI does not infer ownership or completion from
activity alone.

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
                                            └── TASKS (compensation only when explicitly configured)
                                                └── Atomic units of work
```

### Role-based access control

Backend policy derives identity from the signed-in session, verifies project
membership and hierarchy ancestry, and limits assignment to authorized project
stewards/managers or the affected assignee. A role label never grants economic,
governance, or credential rights on its own; those rights require their own
terms and contract-backed records. Email-only invitations fail closed until the
invited identity can be resolved safely.

---

## Ownership Model

ArdaNova keeps participation, utility, and ownership distinct. No activity,
task, credential, game event, or project token creates an ownership claim by
itself.

### Membership Credentials (Governance)

Membership credentials are non-transferable participation records. Their
project or guild scope, grant basis, status, and any governance rights must be
stated in the credential and governing terms. A membership credential is not
equity, a transferable project token, or proof of economic ownership.

### Ownership Shares (Economics)

Ownership shares are separate, explicitly documented economic instruments.
Any issuance, allocation, vesting, transfer, revenue, or redemption right must
come from approved project terms and a reconciled backend record. Completing a
task can satisfy a contribution condition; it does not automatically issue a
share.

### Project-token utility

Project-token records describe only the utility and transaction state exposed
by the current contract. They are not interchangeable with membership
credentials or ownership shares. Previewed, submitted, confirmed, and
reconciled amounts remain visibly distinct.

### Rights language

The interface names the object, basis, scope, and current record state whenever
it uses the word **ownership**. Aspirational brand language never substitutes
for governing terms or confirmed settlement evidence.

---

## Game SDK Integration

### For Game Developers

The repository contains experimental Unity and Godot client adapters for
session and API integration. They are not a production authorization channel
and cannot mint credentials, issue ownership, move value, or bypass the BFF/API
policy boundary. Any future game-triggered contribution must enter the same
explicit review, authorization, confirmation, and reconciliation workflow as a
web contribution.

---

## Cooperative Governance

### Project Cooperatives

Projects can document proposals, discussion, votes, delegation, and decision
authority. The governing terms determine who may vote, how votes are weighted,
and what quorum or approval threshold applies. Recording a proposal result does
not automatically execute a treasury, settlement, membership, or ownership
change; those effects require a separate authenticated and auditable contract.

### Crowdfunding Modalities

Projects may invite financial support, labor, resources, knowledge, or
introductions under posted terms. The contribution object and its status are
tracked separately from any proposed compensation or ownership instrument.
New payout requests are currently paused until verified payout processing is
available; the UI must not promise settlement that the backend cannot verify.

---

## AI Integration

### Nova Studio and presentation workflows

The frontend includes a local interface preview for Ask, Draft, Review,
Present, and Rehearse. It exposes source scope, assumptions, uncertainty,
version history, and human accept/edit/reject/undo controls. External LLM, MCP,
and Gamma presentation execution is not yet connected end to end, so the UI
must not claim that a deck was generated, published, or shared.

### MCP server surface

The .NET `ArdaNova.MCP` project contains an extensible tool surface for
AI-assisted project work. Code presence is not production authorization: every
enabled tool must preserve the same identity, membership, hierarchy, financial,
and rights-changing policy boundaries as the web/API path.

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

**Release boundary**

- Nova output remains a draft until a person accepts it.
- Accepting draft text does not publish, vote, fund, swap, settle, issue a
  credential, or create ownership.
- A tool may be exposed only after its server-side authorization, audit, error,
  and recovery behavior are verified; no interface label substitutes for that
  evidence.

---

## Getting Started

### Prerequisites

- Node.js 20+
- npm 10+
- PostgreSQL 14+
- .NET 10 SDK
- Algorand wallet (Pera, Defly, or MyAlgo)

### Installation

```bash
# Clone the repository
git clone https://github.com/HarrSoft/ardanova.git
cd ardanova

# Set up environment variables
cp .env.example .env
# Edit .env with your configuration

# Option 1: Docker (requires DATABASE_URL for reachable PostgreSQL)
docker-compose up

# Option 2: Manual setup
# Terminal 1 - Frontend
cd ardanova-client
npm ci
npm run db:push
npm run generate:prisma
npm run dev

# Terminal 2 - Backend
cd ardanova-backend-api-mcp
dotnet run --project api-server/src/ArdaNova.API
```

For frontend-only interface work without Google OAuth, the development server supports a visibly marked preview session with `DEV_AUTH_BYPASS=true`. The flag is accepted only in development, does not bypass backend authorization, and must never be configured in production. See the [frontend README](./ardanova-client/README.md).

### Environment Variables

Create a `.env` file in the repository root (shared by both services):

```env
# Database (PostgreSQL URL format - works for both Prisma and .NET)
DATABASE_URL="postgresql://user:password@localhost:5432/ardanova"

# .NET Backend API
API_URL="http://localhost:5147"
API_KEY=""
ADMIN_API_KEY=""
ACTOR_ASSERTION_HMAC_KEY=""

# NextAuth
AUTH_SECRET="your-auth-secret-min-32-chars-here"
AUTH_URL="http://localhost:3000"
GOOGLE_CLIENT_ID="your-google-client-id"
GOOGLE_CLIENT_SECRET="your-google-client-secret"

# Algorand
ALGORAND_NETWORK="testnet"
ALGORAND_NODE_URL="https://testnet-api.algonode.cloud"
ALGORAND_INDEXER_URL="https://testnet-idx.algonode.cloud"

# Optional Azoa integration (server-only; disabled by default)
Azoa__BaseUrl="https://your-azoa-node.example"
Azoa__TenantId="your-tenant-guid"
Azoa__CustodyApiKey=""
Azoa__ValueApiKey=""
Azoa__QuestApiKey=""
Azoa__EnableCustodialAccounts="false"
```

`ADMIN_API_KEY` and `ACTOR_ASSERTION_HMAC_KEY` are distinct server-only
secrets. Each must be 32+ bytes and use the exact same respective value on the
frontend BFF and .NET API services. Never prefix either with `NEXT_PUBLIC_` or
print it in logs. The backend pins the MIT-licensed AutoMapper 14 line and does
not require a runtime mapping license secret; upgrading beyond that line is an
explicit dependency and licensing review. Its global `MaxDepth=64` policy is
the reviewed mitigation for
[`GHSA-rvv3-g6hj-g44x`](https://github.com/advisories/GHSA-rvv3-g6hj-g44x);
the build suppresses that advisory URL only after the policy regression tests
pass. See `.env.example` for the complete contract.
The Azoa keys are deliberately different: onboarding, value movement, and quest
automation never inherit one another's authority. Quest authoring also requires
the Azoa API-key owner to hold its persisted dApp Developer role.
Production rejects documentation examples, placeholder markers, and secrets
shorter than 32 UTF-8 bytes. Generate each enabled capability key independently;
never copy a scope label into a secret field.
`Azoa__TenantApiKey` remains available only as a local-development migration
fallback and is ignored in Production.

---

## Monetization

No production revenue-share, staking, issuance-allocation, or redemption model
is asserted by this repository. Potential subscriptions, service fees, or
transaction fees require separate product, legal, accounting, and governance
approval before they appear as available product behavior. The current
interface reports only contract-backed records and clearly labels previews or
paused workflows.

---

## Target Audience

- **Affected people and researchers** — document problems, evidence, and open questions
- **Contributors** — find bounded work with visible terms and decision authority
- **Project stewards** — shape solutions, coordinate iterations, and preserve an auditable record
- **Cooperatives and community organizations** — make participation and governance terms legible
- **Service partners and funders** — support work through explicit, contract-backed arrangements
- **Game and tool developers** — explore interoperable contribution experiences without bypassing platform authorization

---

## Why Algorand?

The repository includes an Algorand integration seam for project utilities and
verifiable records. Provider-backed value operations remain behind the .NET API
and must pass the same authorization and reconciliation boundaries as every
other transaction path.

| Feature                      | Benefit                                                                                         |
| ---------------------------- | ----------------------------------------------------------------------------------------------- |
| **Native assets**            | Represent explicitly scoped utility or credential records                                       |
| **Atomic transactions**      | Keep related ledger instructions together where the provider supports them                      |
| **Finality-oriented design** | Support evidence-based confirmation and reconciliation workflows                                |
| **Provider abstraction**     | Keep legacy, simulated, and future AZOA-backed implementations behind one server-side interface |

---

## Contributing

We welcome contributions from the community. Start with the development
workflow below and open a focused proposal before making a cross-contract or
rights-affecting change.

### Development Workflow

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Make your changes
4. Run tests and linting
5. Commit your changes (`git commit -m 'feat: add amazing feature'`)
6. Push to your fork (`git push origin feature/amazing-feature`)
7. Open a Pull Request

### Code Style

- TypeScript strict null checking with type-aware ESLint; full strict-mode migration remains incremental
- ESLint + Prettier for formatting
- Conventional commits

---

## Documentation

- [Roadmap](./documentation/ROADMAP.md) - Platform vision and implementation timeline
- [Architecture](./documentation/ARCHITECTURE.md) - Technical architecture and data flow
- [Brand guidelines](./documentation/BRAND_GUIDELINES.md) - Visual identity, voice, rights vocabulary, and reference set
- [Linguistic guide](./documentation/LINGUISTIC_GUIDE.md) - Solutionary vocabulary, core-loop language, CTA patterns, community activity, ownership, and anti-surveillance guardrails
- [Frontend experience](./documentation/FRONTEND_EXPERIENCE.md) - Workspace, Nova interaction, accessibility, and trust-state contract
- [Frontend release playbook](./documentation/FRONTEND_RELEASE_PLAYBOOK.md) - Repeatable contract, auth, accessibility, QA, Railway validation, and rollback workflow
- [Authentication setup](./documentation/AUTHENTICATION_SETUP.md) - Google OAuth, JWT user synchronization, local preview boundaries, and production validation
- [Frontend README](./ardanova-client/README.md) - Local setup, preview auth, experience map, quality commands, and Railway notes
- [Phase A baseline critique](./documentation/PHASE_A_BASELINE_CRITIQUE.md) - Doc vs code snapshot, risks, MVP notes
- [Local development smoke runbook](./documentation/LOCAL_DEVELOPMENT_SMOKE.md) - Env, commands, smoke checks, known issues
- [Phase A test matrix](./documentation/PHASE_A_TEST_MATRIX.md) - Narrow manual QA matrix (baseline)
- [Database Schema (DBML)](./ardanova-client/prisma/database-architecture.dbml) - DBML schema (source of truth)
- [Prisma Schema](./ardanova-client/prisma/schema.prisma) - Generated Prisma schema
- [C# Generator](./ardanova-client/scripts/generate-csharp-models.ts) - DBML to C# entity generator
- [API Client](./ardanova-client/src/lib/api/) - Modular TypeScript API client
- [.NET Backend](./ardanova-backend-api-mcp/) - Clean Architecture .NET 10 API with MCP tools

---

## Community

- **GitHub Issues** - Bug reports and feature requests
- **Discussions** - Community discussions and Q&A
- **Discord** - Real-time chat (coming soon)
- **Twitter** - Updates and announcements (coming soon)

---

## License

No repository license file is currently committed. Consult the maintainers
before copying, redistributing, or relying on the code outside this project.

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

**Turn attention into accountable action.**

_Discover a problem. Define a solution. Iterate—with people in authority and technology in service of ownership, not surveillance._

---

**[ardanova.com](https://ardanova.com)**

</div>
