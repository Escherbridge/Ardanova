## 1. Identity & Credentials (User Core)
- [x] **[P0] OAuth & Sessions**: Google OAuth, JWT Claims (`userId`, `email`, `role`, `userType`, `verificationLevel`), Session persistence. [e538d82]
- [x] **[P0] Membership Credentials (Dual-Asset)**: [f1b811c]
    - [x] Mint non-transferable credential on project joining/founding.
    - [x] Enforce unique credential per project per user.
    - [x] Support revocation status (`ACTIVE`, `REVOKED`, `SUSPENDED`).
- [x] **[P1] RBAC & Permissions**: Implement `ADMIN`, `GUILD`, `INDIVIDUAL` roles, `UserType` (`INNOVATOR`, `FREELANCER`, etc.), and Verification Levels. [59c7f87]
- [x] **[P0] Profile Management**: `Name`, `Bio`, `Location`, `Socials` (`LinkedIn`, `Twitter`), `TrustScore`. [1540a41]

## 2. Gamification Engine (XP & Levels)
- [x] **[P0] XP System**: `XPEvent` entity (`amount`, `source`, `sourceId`, `metadata`). [a1df1dd]
    - [x] Triggers: `TASK_COMPLETED`, `PROPOSAL_CREATED`, `VOTE_CAST`, `REFERRAL`, `STREAK`.
- [x] **[P0] Leveling**: Calculate User Level based on `totalXP` thresholds (Exponential curve). [a1df1dd]
- [x] **[P1] Tiers**: `BRONZE` -> `SILVER` -> `GOLD` -> `PLATINUM` -> `DIAMOND` based on XP ranges. [a1df1dd]
- [x] **[P1] Streaks**: Track `DAILY_LOGIN`, `DAILY_CONTRIBUTION`, `WEEKLY_TASK` streaks with reset logic. [f3b9b27]

## 3. Achievements & Reputation
- [x] **[P1] Achievement System**: `Achievement` definitions (Criteria JSON, Rarity: `COMMON` -> `LEGENDARY`). [975fe0e]
- [x] **[P1] User Progress**: `UserAchievement` tracking (`progress`, `earnedAt`). [975fe0e]
- [x] **[P1] Leaderboards**: `DAILY`, `WEEKLY`, `MONTHLY`, `ALL_TIME` rankings by XP, Tasks, Votes. [84f855c]
- [x] **[P2] Referrals**: `Referral` system with unique codes, status (`PENDING` -> `COMPLETED`), and dual-sided rewards. [84f855c]
