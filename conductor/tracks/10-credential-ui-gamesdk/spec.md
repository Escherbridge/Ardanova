# Credential Frontend UI & Game SDK — Technical Specification

## Overview

This track delivers the frontend UI for ArdaNova's membership credential system and bootstraps the Game SDK packages. The credential backend (Track 07) and API client/tRPC routers are complete — this track builds the visual layer.

## Design Principles

- **Use existing UI primitives** — Build from `src/components/ui/` (Badge, Card, Progress, Avatar)
- **Brutalist neon aesthetic** — Sharp edges, electric accents, dark surfaces per design system
- **Thin data layer** — Components fetch via tRPC hooks, no local state management
- **Progressive disclosure** — Show credential badges compactly, expand details on interaction
- **Blockchain abstracted** — Users see tier/status badges, not ASA IDs (unless they expand details)

---

## Credential UI Component Architecture

```
src/components/credentials/
├── credential-badge.tsx      # Compact tier + status badge
├── credential-card.tsx       # Full credential display card
├── credential-list.tsx       # List/grid of credential cards
└── tier-progress.tsx         # Tier progression visualization
```

### Tier Color Mapping

| Tier | Badge Color | Border Accent | Icon |
|------|------------|---------------|------|
| BRONZE | Amber/Orange (#f59e0b) | border-amber-500 | Shield |
| SILVER | Slate/Gray (#94a3b8) | border-slate-400 | ShieldCheck |
| GOLD | Yellow/Gold (#fbbf24) | border-yellow-400 | Crown |
| PLATINUM | Purple (#8b5cf6) | border-purple-500 | Star |
| DIAMOND | Pink/Neon (#ff0080) | border-pink-500 | Gem |
| No tier | Muted (#64748b) | border-white/10 | Shield |

### Status Color Mapping

| Status | Badge Variant | Color |
|--------|--------------|-------|
| ACTIVE | success | Neon Green (#00ff88) |
| SUSPENDED | warning | Warning Yellow (#fbbf24) |
| REVOKED | destructive | Red (#ef4444) |

---

## Integration Points

### Profile Page (`/dashboard/profile`)

```
┌─────────────────────────────────────────────────────┐
│  Profile Header                                      │
│  [Avatar] [Name] [Role] [3 Credentials | Gold Tier] │
├─────────────────────────────────────────────────────┤
│  [Overview] [Activity] [Credentials] [Settings]      │
│                                                       │
│  ┌─ Credentials Tab ─────────────────────────────┐   │
│  │  ┌──────────┐  ┌──────────┐  ┌──────────┐    │   │
│  │  │ Project A │  │ Guild B  │  │ Project C │    │   │
│  │  │ GOLD      │  │ SILVER   │  │ BRONZE    │    │   │
│  │  │ ACTIVE    │  │ ACTIVE   │  │ REVOKED   │    │   │
│  │  │ Founder   │  │ DAO Vote │  │ Contrib   │    │   │
│  │  └──────────┘  └──────────┘  └──────────┘    │   │
│  └───────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────┘
```

### Team Tab (Project Detail)

```
┌─────────────────────────────────────────────┐
│  Team Members                                │
│                                               │
│  [Avatar] Alice Smith                         │
│  [FOUNDER badge] [GOLD credential badge]     │
│  Joined: Jan 2025  |  On-chain: ✓            │
│                                               │
│  [Avatar] Bob Jones                           │
│  [CONTRIBUTOR badge] [BRONZE credential badge]│
│  Joined: Mar 2025  |  On-chain: ✓            │
│                                               │
│  [Avatar] Carol Wu                            │
│  [MEMBER badge] [No credential]               │
│  Joined: Jun 2025                             │
└─────────────────────────────────────────────┘
```

---

## Data Flow

### Fetching Credentials

```typescript
// Profile page — all credentials for current user
const { data: credentials } = api.membershipCredential.getByUserId.useQuery(userId);

// Project team tab — credentials for a project
const { data: projectCredentials } = api.membershipCredential.getByProjectId.useQuery(projectId);

// Guild members tab — credentials for a guild
const { data: guildCredentials } = api.membershipCredential.getByGuildId.useQuery(guildId);

// Credential detail page — with on-chain data
const { data: chainData } = api.credentialUtility.getChainData.useQuery(credentialId);
```

### Granting Credentials

```typescript
// Grant and mint (project owner action)
const grantMutation = api.credentialUtility.grantAndMint.useMutation();
await grantMutation.mutateAsync({
  userId: memberId,
  projectId: projectId,
  grantedVia: "APPLICATION_APPROVED",
});
```

---

## Game SDK Architecture

### CRITICAL: Routing & Auth Model

The Game SDKs **DO NOT** hit the .NET backend directly. They route through the
Next.js API layer, which handles session authorization per user.

```
Game Client (Unity/Godot)
    │
    │  HTTPS + Session Token (NextAuth JWT)
    │
    ▼
Next.js API (/api/sdk/*)         ← New dedicated SDK route namespace
    │
    │  Session validated via NextAuth
    │  Scoped to current user only
    │
    ▼
tRPC / apiClient → .NET Backend  ← Existing auth model (API key)
```

**Why not hit .NET directly?**
- The .NET API uses an internal API key shared with Next.js — exposing that
  to open SDK clients would grant full platform access to every game developer.
- NextAuth session tokens scope requests to the authenticated user.
- The SDK API surface is intentionally limited: games can read their own
  credentials and token balances, report actions, and gate content — but
  cannot grant credentials, revoke, mint, or perform admin operations.

### SDK API Surface (Next.js `/api/sdk/` routes)

| Endpoint | Method | Description | Auth |
|----------|--------|-------------|------|
| `/api/sdk/auth/session` | POST | Exchange game auth code for session token | Public |
| `/api/sdk/me` | GET | Get current user profile | Session |
| `/api/sdk/me/credentials` | GET | Get current user's credentials | Session |
| `/api/sdk/me/credentials/check` | GET | Check if user holds credential (by project/guild) | Session |
| `/api/sdk/me/token-balances` | GET | Get current user's token balances | Session |
| `/api/sdk/me/token-balances/{projectId}` | GET | Get balance for specific project | Session |
| `/api/sdk/actions` | POST | Report in-game action (earns XP/equity) | Session |

### Content Gating Pattern

The primary SDK use case is **content gating** — checking whether the current
player holds a specific credential or tier before unlocking game content.

```
Game Client                          Next.js SDK API
    │                                      │
    │  GET /api/sdk/me/credentials/check   │
    │  ?projectId=xxx&minTier=GOLD         │
    │─────────────────────────────────────>│
    │                                      │
    │  { hasCredential: true,              │
    │    tier: "PLATINUM",                 │
    │    meetsMinTier: true }              │
    │<─────────────────────────────────────│
    │                                      │
    │  → Unlock premium content            │
```

### Token Exchange on Game Actions

Games report player actions, and the platform awards equity shares based on
configured task allocations. The game never directly mints or transfers tokens.

```
Game Client                     Next.js SDK API              .NET Backend
    │                                │                            │
    │  POST /api/sdk/actions         │                            │
    │  { actionType: "QUEST_DONE",   │                            │
    │    taskId: "abc",              │                            │
    │    metadata: {...} }           │                            │
    │───────────────────────────────>│                            │
    │                                │  Validate session          │
    │                                │  Forward to .NET ─────────>│
    │                                │                            │  Award equity
    │                                │                            │  Update balance
    │                                │         result            │
    │                                │<───────────────────────────│
    │  { awarded: true,              │                            │
    │    tokensEarned: 50,           │                            │
    │    newBalance: 1250 }          │                            │
    │<───────────────────────────────│                            │
```

### Unity SDK (`ardanova-game-sdk/game-sdk-unity/`)

```
game-sdk-unity/
├── package.json              # Unity Package Manager manifest
├── Runtime/
│   ├── ArdaNovaClient.cs     # Main API client → Next.js SDK routes
│   ├── Models/               # Data models (User, Credential, Token)
│   ├── Auth/
│   │   └── SessionProvider.cs # Session token management
│   ├── Gating/
│   │   └── CredentialGate.cs  # Content gating helper
│   └── ArdaNova.Runtime.asmdef
├── Editor/
│   ├── ArdaNovaSettings.cs   # ScriptableObject for API config
│   └── ArdaNova.Editor.asmdef
├── Tests/
│   └── ArdaNova.Tests.asmdef
└── Samples~/
    └── BasicSetup/
```

### Godot SDK (`ardanova-game-sdk/game-sdk-godot/`)

```
game-sdk-godot/
├── plugin.cfg               # Godot plugin manifest
├── addons/ardanova/
│   ├── ardanova_client.gd   # Main API client → Next.js SDK routes
│   ├── models/              # Data models
│   ├── auth/
│   │   └── session_provider.gd # Session token management
│   ├── gating/
│   │   └── credential_gate.gd  # Content gating helper
│   └── plugin.gd            # Plugin entry point
└── tests/
```

### Core SDK Methods

| Method | Description | Hits |
|--------|-------------|------|
| `Authenticate(authCode)` | Exchange auth code for session, store token | `POST /api/sdk/auth/session` |
| `GetProfile()` | Get current user profile | `GET /api/sdk/me` |
| `GetCredentials()` | Get current user's credentials | `GET /api/sdk/me/credentials` |
| `CheckCredential(projectId?, guildId?, minTier?)` | Check if user holds credential at tier | `GET /api/sdk/me/credentials/check` |
| `GetTokenBalances()` | Get all token balances | `GET /api/sdk/me/token-balances` |
| `GetTokenBalance(projectId)` | Get balance for a project | `GET /api/sdk/me/token-balances/{id}` |
| `ReportAction(actionType, taskId, metadata)` | Report game action for equity award | `POST /api/sdk/actions` |

### Content Gating Helpers

Both SDKs provide high-level helpers for common gating patterns:

```csharp
// Unity — C#
if (await ArdaNova.Gate.HasCredential(projectId: "abc"))
    EnablePremiumContent();

if (await ArdaNova.Gate.HasMinTier(projectId: "abc", minTier: "GOLD"))
    EnableGoldContent();

if (await ArdaNova.Gate.HasTokenBalance(projectId: "abc", minBalance: 100))
    EnableTokenGatedContent();
```

```gdscript
# Godot — GDScript
if await ArdaNova.gate.has_credential(project_id):
    enable_premium_content()

if await ArdaNova.gate.has_min_tier(project_id, "GOLD"):
    enable_gold_content()

if await ArdaNova.gate.has_token_balance(project_id, 100):
    enable_token_gated_content()
```

---

## File Manifest

### New Files
| File | Purpose |
|------|---------|
| `src/components/credentials/credential-badge.tsx` | Compact tier + status badge |
| `src/components/credentials/credential-card.tsx` | Full credential display card |
| `src/components/credentials/credential-list.tsx` | List/grid of credential cards |
| `src/components/credentials/tier-progress.tsx` | Tier progression visualization |
| `src/app/credentials/[id]/page.tsx` | Credential detail page |
| `src/app/admin/credentials/page.tsx` | Admin credential dashboard |
| `src/app/api/sdk/auth/session/route.ts` | SDK auth endpoint (Next.js Route Handler) |
| `src/app/api/sdk/me/route.ts` | SDK profile endpoint |
| `src/app/api/sdk/me/credentials/route.ts` | SDK credentials endpoint |
| `src/app/api/sdk/me/credentials/check/route.ts` | SDK credential check endpoint |
| `src/app/api/sdk/me/token-balances/route.ts` | SDK token balances endpoint |
| `src/app/api/sdk/actions/route.ts` | SDK game action endpoint |
| `game-sdk-unity/package.json` | Unity package manifest |
| `game-sdk-unity/Runtime/ArdaNovaClient.cs` | Unity API client |
| `game-sdk-unity/Runtime/Gating/CredentialGate.cs` | Content gating helpers |
| `game-sdk-godot/plugin.cfg` | Godot plugin manifest |
| `game-sdk-godot/addons/ardanova/ardanova_client.gd` | Godot API client |
| `game-sdk-godot/addons/ardanova/gating/credential_gate.gd` | Content gating helpers |

### Modified Files
| File | Changes |
|------|---------|
| `src/app/dashboard/profile/page.tsx` | Add Credentials tab |
| `src/components/projects/team-tab.tsx` | Add credential badges to member cards |
| `src/components/guilds/members-tab.tsx` | Add credential badges to member cards |
| `src/app/projects/[slug]/page.tsx` | Add credentialed members stat |
| `src/app/guilds/[slug]/page.tsx` | Add credentialed members stat |
