# Track 10 — Credential Frontend UI & Game SDK Setup

## 1. Credential UI Components
- [x] **[P0] CredentialBadge component**
    - `src/components/credentials/credential-badge.tsx`
    - Compact badge showing credential status (ACTIVE/SUSPENDED/REVOKED)
    - Tier display with color mapping:
      - BRONZE → `variant="outline"` with amber accent
      - SILVER → `variant="secondary"` with slate accent
      - GOLD → `variant="neon"` with yellow accent
      - PLATINUM → `variant="neon-purple"`
      - DIAMOND → `variant="neon-pink"`
    - Props: `status`, `tier`, `size` (sm/md/lg)
    - Follow brutalist design system (sharp edges, neon accents)
- [x] **[P0] CredentialCard component**
    - `src/components/credentials/credential-card.tsx`
    - Full credential display card with:
      - Scope indicator (PROJECT/GUILD) with scope name
      - Tier badge (using CredentialBadge)
      - Status badge (ACTIVE/REVOKED)
      - Grant method (FOUNDER, DAO_VOTE, CONTRIBUTION_THRESHOLD, etc.)
      - Grant date
      - On-chain status indicator (minted/pending/failed)
      - Asset ID link (if minted on Algorand)
    - Uses Card component variants (neon for active, ghost for revoked)
- [x] **[P1] TierProgressBar component**
    - `src/components/credentials/tier-progress.tsx`
    - Visual tier progression: BRONZE → SILVER → GOLD → PLATINUM → DIAMOND
    - Highlight current tier, show next tier target
    - Uses Progress component with neon variant
- [x] **[P1] CredentialList component**
    - `src/components/credentials/credential-list.tsx`
    - List/grid of CredentialCards
    - Empty state: "No credentials yet" with CTA
    - Loading state with spinner
    - Filter by status (ACTIVE/REVOKED/ALL)

## 2. Profile Page — Credentials Tab
- [x] **[P0] Add Credentials tab to profile page**
    - Update `src/app/dashboard/profile/page.tsx`
    - New tab: "Credentials" between guilds and contributions
    - Shows all user's membership credentials across projects/guilds
    - Uses CredentialList component
    - tRPC query: `membershipCredential.getByUserId(userId)`
- [x] **[P1] Credential summary in profile header**
    - Show credential count and highest tier in profile header stats
    - Badge-style display with CredentialBadge component

## 3. Project Detail — Credential Integration
- [x] **[P0] Team tab: Add credential badges to member cards**
    - Update `src/components/projects/team-tab.tsx`
    - Show CredentialBadge next to member role badge
    - Fetch project credentials via `membershipCredential.getByProjectId(projectId)`
    - Map credentials to team members by userId
- [x] **[P1] Project header: Credentialed members count**
    - Update `src/app/projects/[slug]/page.tsx`
    - Added "Credentialed" stat with Shield icon and neon-cyan accent
    - Fetches ACTIVE credentials count via `membershipCredential.getActiveByProjectId(projectId)`
- [ ] **[P2] Grant credential action (project owner)**
    - "Grant Credential" button in team tab (visible to project owner/admin)
    - Modal: select member, grant type, optional tier
    - Calls `credentialUtility.grantAndMint()`
    - Shows success/error with on-chain status

## 4. Guild Detail — Credential Integration
- [x] **[P0] Members tab: Add credential badges to member cards**
    - Update `src/components/guilds/members-tab.tsx`
    - Show CredentialBadge next to member role badge
    - Fetch guild credentials via `membershipCredential.getByGuildId(guildId)`
    - Map credentials to guild members by userId
    - Added `getByGuildId` and `getActiveByGuildId` tRPC queries
- [x] **[P1] Guild header: Credentialed members count**
    - Update `src/app/guilds/[slug]/page.tsx`
    - Added "Credentialed" stat with Shield icon and neon-cyan accent
    - Fetches ACTIVE credentials count via `membershipCredential.getActiveByGuildId(guildId)`
- [ ] **[P2] Grant credential action (guild owner)**
    - "Grant Credential" button in members tab (visible to guild owner/admin)
    - Same modal pattern as project credential grant

## 5. Credential Management Page
- [x] **[P1] Credential detail page**
    - `src/app/credentials/[id]/page.tsx`
    - Full credential details with on-chain data
    - Calls `credentialUtility.getChainData(id)`
    - Shows: credential info, ASA details, blockchain verification status
    - TierProgress component for tier visualization
- [ ] **[P2] Admin credential dashboard**
    - `src/app/admin/credentials/page.tsx`
    - List all credentials platform-wide
    - Filter by project, guild, status, tier
    - Actions: revoke, upgrade tier, retry mint

## 6. Game SDK Package Setup
- [x] **[P1] Unity SDK project scaffold**
    - Set up `ardanova-game-sdk/game-sdk-unity/` as a proper Unity package
    - `package.json` for Unity Package Manager
    - Core structure: `Runtime/`, `Editor/`, `Tests/`, `Samples~/`
- [x] **[P1] Unity SDK — ArdaNova Client (routed through Next.js)**
    - HTTP client class using UnityWebRequest
    - Routes through Next.js `/api/sdk/*` — NOT .NET backend directly
    - Session-based auth via `SessionProvider` (PlayerPrefs storage)
    - Methods: Authenticate, GetProfile, GetCredentials, CheckCredential, ReportAction, GetTokenBalance
    - ScriptableObject-based settings for editor configuration
- [x] **[P1] Unity SDK — Content Gating Helpers**
    - `CredentialGate` class: `HasCredential()`, `HasMinTier()`, `HasTokenBalance()`
    - Models: `CredentialCheckResult`, `ActionResult`
- [x] **[P1] Godot SDK project scaffold**
    - Set up `ardanova-game-sdk/game-sdk-godot/` as a GDScript addon
    - `plugin.cfg` for Godot Plugin Manager
    - Core structure: `addons/ardanova/`, scripts, resources
- [x] **[P1] Godot SDK — ArdaNova Client (routed through Next.js)**
    - GDScript HTTP client routed through Next.js `/api/sdk/*`
    - Session-based auth via `SessionProvider` (ConfigFile storage)
    - Signal-based async pattern (Godot convention)
    - Methods: authenticate, get_profile, get_credentials, check_credential, report_action
- [x] **[P1] Godot SDK — Content Gating Helpers**
    - `ArdaNovaCredentialGate`: `has_credential()`, `has_min_tier()`, `has_token_balance()`
    - Data models with `from_dict` constructors
- [x] **[P1] Next.js `/api/sdk/*` route handlers**
    - Session-authenticated API routes that proxy to .NET backend via `apiClient`
    - `src/app/api/sdk/_lib/session.ts` — shared NextAuth session validation helper
    - `src/app/api/sdk/auth/session/route.ts` — POST: exchange auth code for session
    - `src/app/api/sdk/me/route.ts` — GET: current user profile
    - `src/app/api/sdk/me/credentials/route.ts` — GET: list user credentials
    - `src/app/api/sdk/me/credentials/check/route.ts` — GET: check credential/tier (content gating)
    - `src/app/api/sdk/me/token-balances/route.ts` — GET: all token balances (placeholder, pending Track 09)
    - `src/app/api/sdk/me/token-balances/[projectId]/route.ts` — GET: project-specific balance (placeholder)
    - `src/app/api/sdk/actions/route.ts` — POST: report game action (placeholder, pending Track 09)
- [ ] **[P2] Shared API specification**
    - OpenAPI/Swagger spec export from .NET controllers
    - SDK codegen configuration for Unity (C#) and Godot (GDScript)

## 7. Tests
- [ ] **[P1] Credential UI component tests (vitest)**
    - CredentialBadge renders correct tier colors
    - CredentialCard displays all fields correctly
    - CredentialList handles empty/loading/error states
    - TierProgressBar shows correct progression
- [ ] **[P2] Game SDK unit tests**
    - Unity: NUnit tests for API client
    - Godot: GdUnit tests for API client
