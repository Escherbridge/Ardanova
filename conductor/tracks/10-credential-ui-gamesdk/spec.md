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

### Unity SDK (`ardanova-game-sdk/game-sdk-unity/`)

```
game-sdk-unity/
├── package.json              # Unity Package Manager manifest
├── Runtime/
│   ├── ArdaNovaClient.cs     # Main API client
│   ├── Models/               # Data models (User, Credential, Token)
│   ├── Auth/
│   │   └── JwtAuthProvider.cs # JWT token management
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
│   ├── ardanova_client.gd   # Main API client (GDScript)
│   ├── models/              # Data models
│   ├── auth/
│   │   └── jwt_provider.gd  # JWT token management
│   └── plugin.gd            # Plugin entry point
└── tests/
```

### Core SDK Methods

| Method | Description | API Endpoint |
|--------|-------------|-------------|
| `Authenticate(email, token)` | Validate JWT and store session | `GET /api/Users/me` |
| `GetProfile()` | Get current user profile | `GET /api/Users/me` |
| `GetCredentials()` | Get user's credentials | `GET /api/MembershipCredentials/user/{userId}` |
| `GetTokenBalance(projectId)` | Get token balance | `GET /api/TokenBalances/me/project/{projectId}` |
| `ReportAction(actionType, data)` | Report in-game action for XP/rewards | `POST /api/GameActions` |

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
| `game-sdk-unity/package.json` | Unity package manifest |
| `game-sdk-unity/Runtime/ArdaNovaClient.cs` | Unity API client |
| `game-sdk-godot/plugin.cfg` | Godot plugin manifest |
| `game-sdk-godot/addons/ardanova/ardanova_client.gd` | Godot API client |

### Modified Files
| File | Changes |
|------|---------|
| `src/app/dashboard/profile/page.tsx` | Add Credentials tab |
| `src/components/projects/team-tab.tsx` | Add credential badges to member cards |
| `src/components/guilds/members-tab.tsx` | Add credential badges to member cards |
| `src/app/projects/[slug]/page.tsx` | Add credentialed members stat |
| `src/app/guilds/[slug]/page.tsx` | Add credentialed members stat |
