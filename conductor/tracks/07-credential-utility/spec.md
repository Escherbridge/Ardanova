# Credential Utility Service — Technical Specification

## Overview

The Credential Utility Service extends ArdaNova's existing MembershipCredential system with:
1. **Guild credentials** — credentials scoped to guilds (in addition to projects)
2. **Tier system** — BRONZE → SILVER → GOLD → PLATINUM → DIAMOND progression
3. **Algorand blockchain integration** — soulbound ASAs with ARC-19 metadata
4. **Orchestrator service** — grant-and-mint, revoke-and-burn, auto-grant, retry

## Design Principles

- **Off-chain first** — credentials are always granted in the database; blockchain minting is additive
- **Graceful degradation** — if the Algorand network is unavailable, credentials still work off-chain with `assetId = null`
- **Soulbound enforcement** — ASAs are frozen at creation, only the platform can clawback (burn)
- **Custodial model** — the platform account signs all blockchain transactions on behalf of users
- **XOR scope** — each credential belongs to exactly one project OR one guild, never both

---

## Schema Changes

### MembershipCredential Table (DBML)

```dbml
Table MembershipCredential {
  id varchar [not null, pk, default: `cuid()`]
  projectId varchar [note: 'Nullable — credential for a project']
  guildId varchar [note: 'Nullable — credential for a guild']
  userId varchar [not null]
  assetId varchar [note: 'Algorand ASA ID for this soulbound credential']
  status MembershipCredentialStatus [not null, default: 'ACTIVE']
  isTransferable boolean [not null, default: false, note: 'Soulbound — always false']
  tier UserTier [note: 'Credential tier: BRONZE, SILVER, GOLD, PLATINUM, DIAMOND']
  grantedVia MembershipGrantType [not null]
  grantedByProposalId varchar [note: 'Populated if membership was granted via DAO vote']
  metadataUri varchar [note: 'ARC-19 metadata URI (IPFS or HTTP)']
  mintTxHash varchar
  revokeTxHash varchar
  mintedAt datetime
  revokedAt datetime
  createdAt datetime [not null, default: `now()`]
  updatedAt datetime [not null, note: 'Updated at']

  indexes {
    (projectId, userId) [unique, note: 'One credential per user per project']
    (guildId, userId) [unique, note: 'One credential per user per guild']
  }
}
Ref: MembershipCredential.projectId > Project.id
Ref: MembershipCredential.guildId > Guild.id
Ref: MembershipCredential.userId > User.id
Ref: MembershipCredential.grantedByProposalId > Proposal.id
```

**Key changes from current schema:**
- `projectId` becomes nullable (was `not null`)
- New: `guildId varchar` (nullable, FK → Guild)
- New: `tier UserTier` (nullable)
- New: `metadataUri varchar` (ARC-19 metadata pointer)
- New index: `(guildId, userId) [unique]`
- Reuses existing `UserTier` enum (BRONZE/SILVER/GOLD/PLATINUM/DIAMOND)

---

## Soulbound ASA Design (Algorand)

### ASA Creation Parameters

```
AssetParams:
  total: 1                          # Non-fungible (single unit)
  decimals: 0                       # No fractional units
  defaultFrozen: true               # Soulbound — cannot be transferred
  unitName: "CRED"                  # Short identifier
  assetName: "{projectName|guildName} Membership"
  url: "{metadataUri}"              # ARC-19 metadata pointer
  manager: platformAddress          # Platform can update metadata
  reserve: platformAddress          # Platform reserve
  freeze: platformAddress           # Platform controls freeze
  clawback: platformAddress         # Platform can burn (clawback)
  # NOTE: No transfer address — recipients cannot opt-out
```

### Soulbound Enforcement

| Property | Value | Purpose |
|----------|-------|---------|
| `defaultFrozen` | `true` | Prevents any transfers after opt-in |
| `clawback` | Platform address | Allows platform to burn (revoke) the ASA |
| `freeze` | Platform address | Platform controls freeze state |
| `manager` | Platform address | Platform can update metadata |
| Total supply | 1 | Non-fungible single unit |

### Why This Works

- The recipient opts in to receive the ASA
- `defaultFrozen=true` means they can never send it to anyone else
- Only the platform's `clawback` address can move (burn) the ASA
- This creates a true soulbound token — owned but non-transferable

---

## ARC-19 Metadata Schema

Each credential ASA points to a JSON metadata document:

```json
{
  "standard": "arc19",
  "name": "ArdaNova Membership Credential",
  "description": "Soulbound governance credential for {project/guild name}",
  "properties": {
    "credentialId": "cuid_xxx",
    "scope": "PROJECT" | "GUILD",
    "scopeId": "project_or_guild_id",
    "scopeName": "Project/Guild Name",
    "userId": "user_cuid",
    "tier": "BRONZE" | "SILVER" | "GOLD" | "PLATINUM" | "DIAMOND" | null,
    "grantedVia": "FOUNDER" | "DAO_VOTE" | "CONTRIBUTION_THRESHOLD" | "APPLICATION_APPROVED" | "GAME_SDK_THRESHOLD",
    "grantedAt": "2025-01-15T00:00:00Z",
    "isTransferable": false,
    "platform": "ArdaNova",
    "version": "1.0"
  }
}
```

**Storage:** Metadata is stored via IPFS (preferred) or an HTTP endpoint. The `metadataUri` field on the credential record points to this JSON.

---

## Service Architecture

### CredentialUtilityService Orchestration

```
┌──────────────────────────────────────────────────────────────────┐
│                  CREDENTIAL UTILITY SERVICE                       │
│              (Orchestrates credential lifecycle)                  │
├──────────────────────────────────────────────────────────────────┤
│                                                                   │
│  ┌─────────────────────┐    ┌─────────────────────┐              │
│  │ IMembershipCredential│    │  IAlgorandService   │              │
│  │      Service         │    │  (Infrastructure)   │              │
│  │  (Off-chain CRUD)    │    │  (On-chain ops)     │              │
│  └──────────┬──────────┘    └──────────┬──────────┘              │
│             │                          │                          │
│             └──────────┬───────────────┘                          │
│                        │                                          │
│                        ▼                                          │
│  ┌──────────────────────────────────────────────────────────┐    │
│  │              ORCHESTRATED WORKFLOWS                       │    │
│  │                                                           │    │
│  │  GrantAndMint:                                            │    │
│  │    1. Validate eligibility                                │    │
│  │    2. Grant credential (IMembershipCredentialService)     │    │
│  │    3. Build ARC-19 metadata                               │    │
│  │    4. Mint soulbound ASA (IAlgorandService)               │    │
│  │    5. Update credential with assetId + mintTxHash         │    │
│  │    6. If chain fails → credential still valid off-chain   │    │
│  │                                                           │    │
│  │  RevokeAndBurn:                                           │    │
│  │    1. Validate credential is ACTIVE                       │    │
│  │    2. Clawback (burn) ASA if assetId exists               │    │
│  │    3. Revoke credential (IMembershipCredentialService)    │    │
│  │    4. Store revokeTxHash                                  │    │
│  │                                                           │    │
│  │  UpgradeTier:                                             │    │
│  │    1. Validate tier progression (can only go up)          │    │
│  │    2. Update credential tier                              │    │
│  │    3. Update on-chain metadata if ASA exists              │    │
│  │                                                           │    │
│  │  CheckAndAutoGrant:                                       │    │
│  │    1. Check user's XP/contribution level                  │    │
│  │    2. If threshold met → GrantAndMint automatically       │    │
│  │    3. Return credential or null                           │    │
│  │                                                           │    │
│  │  RetryMint:                                               │    │
│  │    1. Find credentials with null assetId                  │    │
│  │    2. Re-attempt ASA minting                              │    │
│  │    3. Update credential on success                        │    │
│  └──────────────────────────────────────────────────────────┘    │
│                                                                   │
└──────────────────────────────────────────────────────────────────┘
```

### Grant-and-Mint Sequence

```
Client          CredentialUtility    MembershipCredSvc    AlgorandService    Algorand
  │                    │                    │                    │              │
  │ POST grant-and-mint│                    │                    │              │
  │───────────────────>│                    │                    │              │
  │                    │ GrantAsync(dto)    │                    │              │
  │                    │───────────────────>│                    │              │
  │                    │  credential (DB)   │                    │              │
  │                    │<───────────────────│                    │              │
  │                    │                    │                    │              │
  │                    │ BuildARC19Metadata │                    │              │
  │                    │───────────────────────────────────────>│              │
  │                    │  metadataJson      │                    │              │
  │                    │<───────────────────────────────────────│              │
  │                    │                    │                    │              │
  │                    │ MintSoulboundASA   │                    │              │
  │                    │───────────────────────────────────────>│              │
  │                    │                    │                    │ Create ASA   │
  │                    │                    │                    │─────────────>│
  │                    │                    │                    │ (assetId, tx)│
  │                    │                    │                    │<─────────────│
  │                    │  (assetId, txHash) │                    │              │
  │                    │<───────────────────────────────────────│              │
  │                    │                    │                    │              │
  │                    │ UpdateMintInfo     │                    │              │
  │                    │───────────────────>│                    │              │
  │                    │  updated credential│                    │              │
  │                    │<───────────────────│                    │              │
  │                    │                    │                    │              │
  │  credential + ASA  │                    │                    │              │
  │<───────────────────│                    │                    │              │
```

### Revoke-and-Burn Sequence

```
Client          CredentialUtility    MembershipCredSvc    AlgorandService    Algorand
  │                    │                    │                    │              │
  │ POST revoke-and-burn                   │                    │              │
  │───────────────────>│                    │                    │              │
  │                    │ GetById            │                    │              │
  │                    │───────────────────>│                    │              │
  │                    │  credential        │                    │              │
  │                    │<───────────────────│                    │              │
  │                    │                    │                    │              │
  │                    │ BurnASA(assetId)   │ (if assetId exists)│              │
  │                    │───────────────────────────────────────>│              │
  │                    │                    │                    │ Clawback ASA │
  │                    │                    │                    │─────────────>│
  │                    │                    │                    │   txHash     │
  │                    │                    │                    │<─────────────│
  │                    │   revokeTxHash     │                    │              │
  │                    │<───────────────────────────────────────│              │
  │                    │                    │                    │              │
  │                    │ RevokeAsync(id, tx)│                    │              │
  │                    │───────────────────>│                    │              │
  │                    │  revoked credential│                    │              │
  │                    │<───────────────────│                    │              │
  │                    │                    │                    │              │
  │  revoked credential│                    │                    │              │
  │<───────────────────│                    │                    │              │
```

---

## Environment Variables

| Variable | Description | Default |
|----------|-------------|---------|
| `ALGORAND_NETWORK` | Network to connect to | `testnet` |
| `ALGORAND_NODE_URL` | Algod API endpoint | `https://testnet-api.algonode.cloud` |
| `ALGORAND_INDEXER_URL` | Indexer API endpoint | `https://testnet-idx.algonode.cloud` |
| `ALGORAND_PLATFORM_MNEMONIC` | 25-word mnemonic for platform account | (required) |
| `ALGORAND_PLATFORM_ADDRESS` | Platform account address (derived from mnemonic) | (derived) |

---

## Error Handling & Graceful Degradation

| Scenario | Behavior |
|----------|----------|
| Algorand node unreachable | Credential granted off-chain, `assetId = null`, logged for retry |
| ASA minting fails | Credential still ACTIVE, `mintTxHash = null`, retry via `RetryMint` |
| ASA burn fails on revoke | Credential revoked off-chain, `revokeTxHash = null`, logged |
| Invalid recipient address | Credential granted, minting skipped, warning logged |
| Duplicate ASA creation | Idempotent — check if credential already has assetId before minting |

---

## File Manifest

### New Files
| File | Layer | Purpose |
|------|-------|---------|
| `ArdaNova.Infrastructure/Algorand/AlgorandSettings.cs` | Infrastructure | Configuration POCO |
| `ArdaNova.Infrastructure/Algorand/AlgorandService.cs` | Infrastructure | Algorand SDK wrapper |
| `ArdaNova.Application/Services/Interfaces/IAlgorandService.cs` | Application | Algorand abstraction |
| `ArdaNova.Application/Services/Interfaces/ICredentialUtilityService.cs` | Application | Orchestrator interface |
| `ArdaNova.Application/Services/Implementations/CredentialUtilityService.cs` | Application | Orchestrator impl |
| `ArdaNova.API/Controllers/CredentialUtilityController.cs` | API | REST endpoints |
| `ardanova-client/src/lib/api/ardanova/endpoints/credential-utility.ts` | Frontend | API client |
| `tests/.../CredentialUtilityServiceTests.cs` | Tests | Orchestrator tests |
| `tests/.../AlgorandServiceTests.cs` | Tests | Algorand service tests |

### Modified Files
| File | Changes |
|------|---------|
| `ardanova-client/prisma/database-architecture.dbml` | Add guildId, tier, metadataUri; make projectId nullable |
| `ArdaNova.Application/DTOs/MembershipCredentialDtos.cs` | Add GuildId, Tier, MetadataUri fields |
| `ArdaNova.Application/Services/Interfaces/IMembershipCredentialService.cs` | Add guild query + tier methods |
| `ArdaNova.Application/Services/Implementations/MembershipCredentialServices.cs` | Implement guild + tier methods |
| `ArdaNova.API/Controllers/MembershipCredentialsController.cs` | Add guild + tier endpoints |
| `ArdaNova.Application/Mappings/MappingProfile.cs` | Add new field mappings |
| `ArdaNova.Application/DependencyInjection.cs` | Register new services |
| `ArdaNova.Infrastructure.csproj` | Add dotnet-algorand-sdk NuGet |
| `ardanova-client/src/lib/api/ardanova/endpoints/membership-credentials.ts` | Add guild + tier fields/methods |
| `ardanova-client/src/lib/api/ardanova/index.ts` | Register credential-utility endpoint |
| `tests/.../MembershipCredentialServiceTests.cs` | Add guild credential tests |
