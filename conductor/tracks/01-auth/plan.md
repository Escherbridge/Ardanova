## 1. Identity & Credentials (User Core)
- [x] **[P0] OAuth & Sessions**: Google OAuth, JWT Claims (`userId`, `email`, `role`, `userType`, `verificationLevel`), Session persistence.
- [x] **[P0] Membership Credentials (Dual-Asset)**: 
    - [x] Mint non-transferable credential on project joining/founding.
    - [x] Enforce unique credential per project per user.
    - [x] Support revocation status (`ACTIVE`, `REVOKED`, `SUSPENDED`).
- [x] **[P1] RBAC & Permissions**: Implement `ADMIN`, `GUILD`, `INDIVIDUAL` roles, `UserType` (`INNOVATOR`, `FREELANCER`, etc.), and Verification Levels.
- [x] **[P0] Profile Management**: `Name`, `Bio`, `Location`, `Socials` (`LinkedIn`, `Twitter`), `TrustScore`.

## 2. Gamification Engine (XP & Levels)
- [x] **[P0] XP System**: `XPEvent` entity (`amount`, `source`, `sourceId`, `metadata`).
    - [x] Triggers: `TASK_COMPLETED`, `PROPOSAL_CREATED`, `VOTE_CAST`, `REFERRAL`, `STREAK`.
- [x] **[P0] Leveling**: Calculate User Level based on `totalXP` thresholds (Exponential curve).
- [ ] **[P1] Tiers**: `BRONZE` -> `SILVER` -> `GOLD` -> `PLATINUM` -> `DIAMOND` based on XP ranges.
- [ ] **[P1] Streaks**: Track `DAILY_LOGIN`, `DAILY_CONTRIBUTION`, `WEEKLY_TASK` streaks with reset logic.

## 3. Achievements & Reputation
- [ ] **[P1] Achievement System**: `Achievement` definitions (Criteria JSON, Rarity: `COMMON` -> `LEGENDARY`).
- [ ] **[P1] User Progress**: `UserAchievement` tracking (`progress`, `earnedAt`).
- [ ] **[P1] Leaderboards**: `DAILY`, `WEEKLY`, `MONTHLY`, `ALL_TIME` rankings by XP, Tasks, Votes.
- [ ] **[P2] Referrals**: `Referral` system with unique codes, status (`PENDING` -> `COMPLETED`), and dual-sided rewards.

