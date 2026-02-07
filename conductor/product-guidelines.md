# ArdaNova Product Guidelines & QA

This document outlines the critical validation rules, state transitions, and expected behaviors for the ArdaNova platform. It is derived from the `QA-TESTING-CHECKLIST.md` and serves as the source of truth for business logic implementation.

## 1. Authentication, Identity & Gamification

### Identity & Credentials (User Core)
- **OAuth & Sessions**: Google OAuth. Sessions persist across refreshes. Sign-out invalidates both client/server.
- **Dual-Asset Identity**: 
    - **Membership Credentials**: Non-transferable (Soulbound). One per project. Grants **Governance Rights** (1 member = 1 vote).
    - **Revocation**: Credentials can be `REVOKED` by DAO vote, removing voting rights.
- **Profile**:
    - `username` must be unique.
    - `trustScore` starts at 0, non-negative.
    - **Verification**: `ANONYMOUS` -> `VERIFIED` (Email/KYC) -> `PRO` -> `EXPERT`. Financial actions require `VERIFIED`.

### Gamification & Reputation
- **XP System**: Users earn XP for `TASK_COMPLETED`, `VOTE_CAST`, `PROPOSAL_CREATED`.
- **Leveling**: 
    - Exponential curve. Level Up triggers `LEVEL_UP` notification.
- **Tiers**: `BRONZE` -> `SILVER` -> `GOLD` -> `PLATINUM` -> `DIAMOND`.
    - Tiers determine perks/visibility.
- **Streaks**:
    - `DAILY_LOGIN`: Resets if day missed.
    - `WEEKLY_TASK`: Resets if week missed.
- **Achievements**:
    - Rarity: `COMMON` -> `LEGENDARY`.
    - Criteria defined in JSON.

## 2. Project Management & Governance

### Project Lifecycle
**State Machine**: `DRAFT` -> `PUBLISHED` -> `SEEKING_SUPPORT` -> `FUNDED` -> `IN_PROGRESS` -> `COMPLETED`/`CANCELLED`
- **Slug**: Unique. Suffix on collision.
- **Agreements**: Creator must set Operating, Membership, and Task agreements in `DRAFT`.
- **Deletion**: Cascade delete Tasks, Milestones.

### Opportunities Marketplace
- **Types**: `GUILD_POSITION`, `PROJECT_ROLE`, `TASK_BOUNTY`, `FREELANCE`.
- **Applications**: `PENDING` -> `ACCEPTED` / `REJECTED`.
- **Bids**: For bounties. `proposedAmount`, `timeline`.
- **Constraints**: `deadline`, `maxApplications`, `location` (Remote/On-site).

### DAO Governance (Dual-Asset)
- **Voting Power**: 
    - **1 Credential = 1 Vote**. 
    - Shares grant **Economic Rights** (Dividends), NOT voting power.
- **Proposals**: 
    - Types: `TREASURY`, `GOVERNANCE`, `STRATEGIC`.
    - Lifecycle: `DRAFT` -> `ACTIVE` -> `PASSED`/`REJECTED` -> `EXECUTED`.
    - **Quorum**: % of active credentials. **Threshold**: % approval of votes cast.

## 3. Guild Module

### Guild Structure
- **Roles**: `ADMIN` -> `MANAGER` -> `MEMBER` -> `APPRENTICE`.
- **Ownership**: At least one Admin. Admins own equally.
- **Verification**: Admin-managed `isVerified` badge.

### Engagement
- **Follow**: Users can follow Guilds (`notifyUpdates`, `notifyEvents`).
- **Reviews**: 1-5 stars. `rating` is aggregate average.
- **Updates**: Announcements visible to followers.

## 4. Financial & Tokenomics

### Core Finance
- **Precision**: `Decimal(18, 8)` for ALL values. No floats.
- **Treasury**: One per Project/Guild.
- **Assets**: 
    - `ProjectShare` (ERC-20 style).
    - `ProjectEquity` (Legal rights).

### DeFi Primitives
- **Escrow**: `PENDING` -> `FUNDED` -> `RELEASED` / `REFUNDED` / `DISPUTED`.
- **Vesting**: Schedules with `cliffEnd`, `vestingEnd`, `frequency` (`DAILY`...`QUARTERLY`).
- **Fundraising**: `fundingGoal`, `min`/`maxContribution`. Refund if goal not met by `endDate`.
- **Swaps**: Atomic exchange (Deduct A, Add B).

## 5. Social, Real-time & Graph

### Social Graph (Follow)
- **Entities**: User can follow **User**, **Project**, **Guild**.
- **Counters**: Atomic increments for followers/following.
- **Feed**: Aggregated chronological feed from all followed entities.

### Posts & Messaging
- **Posts**: `PUBLIC`, `PROJECT_MEMBERS`, `GUILD_MEMBERS`.
- **Chat**:
    - Direct (1:1) & Group.
    - Lifecycle: `SENT` -> `DELIVERED` -> `READ`.
    - Features: Replies, Reactions, Typing Indicators.
- **Media**: Image, Video, Doc attachments.

## 6. Events Module

### Lifecycle & Management
**States**: `DRAFT` -> `SCHEDULED` -> `LIVE` -> `COMPLETED`/`CANCELLED`
- **Co-Hosts**: Can edit events.
- **Reminders**: User-set (e.g., "1 hour before").
- **Timezones**: Dates stored with timezone info.

### Attendance
- **RSVP**: `INVITED` -> `GOING` / `MAYBE` / `NOT_GOING` -> `ATTENDED`.
- **Capacity**: Block `GOING` RSVPs when `maxAttendees` reached. `MAYBE` ignores limit.

## 7. Edge Cases & Negative Testing
- **Race Conditions**: 
    - Enforce DB constraints on unique actions (e.g., Voting, Following).
    - Use transactions for Inventory/Capacity/Financials.
- **Validation**: 
    - Trim whitespace.
    - Strict regex for Emails/URLs.
    - Non-negative financial values.
- **Security**: 
    - Verify `req.user.id` ownership for all mutations.
