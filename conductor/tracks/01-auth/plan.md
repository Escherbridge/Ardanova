## 1. Identity & Credentials (User Core)
- [~] **[P0] OAuth & Sessions**: Google OAuth, JWT Claims (`userId`, `email`, `role`, `userType`, `verificationLevel`), Session persistence.
- [ ] **[P0] Membership Credentials (Dual-Asset)**: 
    - [ ] Mint non-transferable credential on project joining/founding.
    - [ ] Enforce unique credential per project per user.
    - [ ] Support revocation status (`ACTIVE`, `REVOKED`, `SUSPENDED`).
- [ ] **[P1] RBAC & Permissions**: Implement `ADMIN`, `GUILD`, `INDIVIDUAL` roles, `UserType` (`INNOVATOR`, `FREELANCER`, etc.), and Verification Levels.
- [ ] **[P0] Profile Management**: `Name`, `Bio`, `Location`, `Socials` (`LinkedIn`, `Twitter`), `TrustScore`.

## 2. Gamification Engine (XP & Levels)
- [ ] **[P0] XP System**: `XPEvent` entity (`amount`, `source`, `sourceId`, `metadata`).
    - [ ] Triggers: `TASK_COMPLETED`, `PROPOSAL_CREATED`, `VOTE_CAST`, `REFERRAL`, `STREAK`.
- [ ] **[P0] Leveling**: Calculate User Level based on `totalXP` thresholds (Exponential curve).
- [ ] **[P1] Tiers**: `BRONZE` -> `SILVER` -> `GOLD` -> `PLATINUM` -> `DIAMOND` based on XP ranges.
- [ ] **[P1] Streaks**: Track `DAILY_LOGIN`, `DAILY_CONTRIBUTION`, `WEEKLY_TASK` streaks with reset logic.

## 3. Achievements & Reputation
- [ ] **[P1] Achievement System**: `Achievement` definitions (Criteria JSON, Rarity: `COMMON` -> `LEGENDARY`).
- [ ] **[P1] User Progress**: `UserAchievement` tracking (`progress`, `earnedAt`).
- [ ] **[P1] Leaderboards**: `DAILY`, `WEEKLY`, `MONTHLY`, `ALL_TIME` rankings by XP, Tasks, Votes.
- [ ] **[P2] Referrals**: `Referral` system with unique codes, status (`PENDING` -> `COMPLETED`), and dual-sided rewards.

