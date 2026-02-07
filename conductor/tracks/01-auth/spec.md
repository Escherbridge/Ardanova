# Authentication & User Core Specification

## Overview
This track covers the foundational identity, authentication, and user management systems. It establishes the "User" entity, their security credentials, and their progression through the platform via gamification layers.

## Data Models

### User
Core identity entity.
- `Id`: UUID (PK)
- `Email`: String (Unique)
- `Name`: String?
- `Image`: String?
- `Bio`: String?
- `Location`: String?
- `Phone`: String?
- `Website`: String?
- `LinkedIn`: String?
- `Twitter`: String?
- `Role`: Enum (INDIVIDUAL, GUILD, ADMIN)
- `UserType`: Enum (INNOVATOR, SUPPORTER, VOLUNTEER, FREELANCER, SME_OWNER, GUILD_MEMBER)
- `IsVerified`: Boolean
- `TotalXP`: Integer (DB-only, exposed via Gamification endpoints)
- `Level`: Integer
- `Tier`: Enum (BRONZE, SILVER, GOLD, PLATINUM, DIAMOND)
- `TrustScore`: Decimal
- `VerificationLevel`: Enum (ANONYMOUS, VERIFIED, PRO, EXPERT)
- `CreatedAt`: DateTime

### UserSkill
- `Id`: UUID
- `UserId`: UUID
- `Skill`: String
- `Level`: Integer (1-10)

### UserExperience
- `Id`: UUID
- `UserId`: UUID
- `Title`: String
- `Company`: String
- `StartDate`: DateTime
- `EndDate`: DateTime?
- `IsCurrent`: Boolean

### MembershipCredential
Soulbound token representing project membership & governance rights.
- `Id`: UUID (PK)
- `UserId`: UUID
- `ProjectId`: UUID
- `AssetId`: String? (Algorand ASA ID)
- `GrantType`: Enum (FOUNDER, DAO_VOTE, CONTRIBUTION_THRESHOLD, APPLICATION_APPROVED, GAME_SDK_THRESHOLD)
- `Status`: Enum (ACTIVE, REVOKED, SUSPENDED)
- `IsTransferable`: Boolean (Usually false)
- `MintTxHash`: String?
- `MintedAt`: DateTime?

### XPEvent
Records experience point transactions for users.
- `Id`: UUID (PK)
- `UserId`: UUID
- `Amount`: Integer
- `Source`: Enum (TASK_COMPLETED, PROPOSAL_CREATED, VOTE_CAST, REFERRAL, STREAK)
- `SourceId`: String? (ID of the entity that triggered the XP event, e.g., Task ID, Proposal ID)
- `Metadata`: JSONB? (Additional context about the event)
- `CreatedAt`: DateTime

## API Endpoints (`UsersController`)

| Method | Endpoint | Description | DTO |
| :--- | :--- | :--- | :--- |
| `GET` | `/api/users/me` | Current user profile | `UserDto` |
| `PUT` | `/api/users/me` | Update profile | `UpdateUserDto` |
| `GET` | `/api/users/{id}` | Public profile | `UserDto` |
| `GET` | `/api/users/search` | Search users | `UserDto[]` |
| `POST` | `/api/users/verify` | Request verification | - |
| `GET` | `/api/users/{id}/reputation` | Gamification stats | `UserReputationDto` |
| `GET` | `/api/users/{id}/credentials` | Membership credentials | `MembershipCredentialDto[]` |
| `POST` | `/api/users/skills` | Add skill | `CreateUserSkillDto` |
| `POST` | `/api/users/experience` | Add experience | `CreateUserExperienceDto` |

## Business Logic & Validation

### 1. Identity & Auth
- **OAuth Only**: Primary auth via Google (`Account` model).
- **Session**: `Session` model with `sessionToken`.
- **RBAC**: `UserRole` determines high-level access (Admin vs User).

### 2. Gamification Engine
- **Integrated Model**: XP, Level, and Tier are stored directly on the `User` table for efficient querying.
- **Leveling**: Calculated based on `TotalXP`.
- **Tiers**: Determined by Level milestones.

### 3. Dual-Asset Identity
- **MembershipCredential**:
    - Represents **Governance Rights** (1 Credential = 1 Vote).
    - Distinct from **ProjectShare** (Economic Rights).
    - Can be on-chain (Algorand) if `AssetId` is present.
    - **Revocation**: Updates `Status` to `REVOKED` and optionally records `RevokeTxHash`.

## Integration Points
- **Events**: `UserCreatedEvent`, `UserVerifiedEvent`, `UserLevelUpEvent`.
- **Finance**: `Wallet` entity links User to Algorand Address.
- **SignalR**: Real-time updates for notifications.
