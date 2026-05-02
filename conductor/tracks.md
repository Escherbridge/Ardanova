# ArdaNova Tracks Registry

## Core Modules (Tracks 01-11)
- [x] [Auth & User Core](tracks/01-auth/plan.md) - Identity, RBAC, Session Management, XP, Achievements, Leaderboards, Referrals.
- [x] [Project Management](tracks/02-projects/spec.md) - Project lifecycle, Agile hierarchy, CRUD, members, applications, invitations. *(Retroactive documentation)*
- [x] [Guild Module](tracks/03-guilds/spec.md) - Guild structure, membership, roles, applications, reviews. *(Retroactive documentation)*
- [x] [Financial & Tokenomics (Legacy)](tracks/04-finance/spec.md) - Superseded by Track 09. *(Retroactive documentation)*
- [x] [Social & Real-time](tracks/05-social/spec.md) - Chat complete, posts scaffolded, notifications backend complete. *(Retroactive documentation)*
- [x] [Events Module](tracks/06-events/spec.md) - Event lifecycle, RSVP, co-hosts, reminders. *(Retroactive documentation)*
- [x] [Credential Utility & Blockchain](tracks/07-credential-utility/spec.md) - Soulbound credentials, Algorand ASA minting, tier system.
- [x] [KYC & Identity Verification](tracks/08-kyc/spec.md) - KYC submission, provider abstraction, verification gating.
- [x] [Tokenomics & Project Equity](tracks/09-tokenomics/spec.md) - Project tokens, ARDA platform token, equity distribution, payouts, Stripe integration. **All 12 phases complete (schema, services, controllers, tests, Stripe SDK, API client/tRPC, flow tests).**
- [x] [Credential Frontend UI & Game SDK](tracks/10-credential-ui-gamesdk/spec.md) - Credential badges, tier UI, profile/project/guild integration, Unity/Godot SDK setup. **P0-P1 complete. P2 items remaining (grant modals, admin dashboard, component tests).**
- [ ] [Position Management, Equity & Treasury Pool](tracks/11-position-equity-treasury/spec.md) - Position detail/edit pages, equity cap enforcement, treasury pool allocation UI. **P0 complete. P1 in progress (editable pool targets, per-member equity view).**

## Frontend Integration (Tracks 12-14) — Phase A: Fix Broken Frontend
- [ ] [Dashboard & Feed Integration](tracks/12-dashboard-feed-integration/spec.md) - Wire dashboard feed, post system, social actions to backend. **P0**
- [ ] [Task Board Integration](tracks/13-task-board-integration/spec.md) - Wire Kanban task board to backend, task→equity flow. **P0**
- [ ] [Notification System UI](tracks/14-notification-ui/spec.md) - Notification bell, dropdown, real-time via SignalR. **P0**

## Revenue & Equity UI (Tracks 15-16) — Phase B: Core Revenue
- [ ] [Token & Equity Frontend UI](tracks/15-token-equity-ui/spec.md) - Project funding, investment flow, portfolio, payouts, equity dashboard. **P0**
- [ ] [Application & Hiring Flow UI](tracks/16-application-hiring-ui/spec.md) - Apply to opportunities, application review, accept/reject. **P1**

## Growth & Polish (Tracks 17-19) — Phase C
- [ ] [Onboarding & First-Time Experience](tracks/17-onboarding/spec.md) - User type selection, profile wizard, first action prompt. **P1**
- [ ] [Search & Discovery Enhancement](tracks/18-search-discovery/spec.md) - Global search, advanced filters, trending, recommendations. **P1**
- [ ] [Governance & Voting Polish](tracks/19-governance-polish/spec.md) - Credential-gated voting, delegation, execution feedback. **P2**

## Platform Maturity (Tracks 20-23) — Phase D
- [ ] [Dispute Resolution & Escrow UI](tracks/20-dispute-escrow-ui/spec.md) - Escrow display, milestone release, disputes, arbitration. **P2**
- [ ] [Reviews & Trust Enhancement](tracks/21-reviews-trust/spec.md) - Guild/project reviews, trust scores. **P3**
- [ ] [Settings & Preferences](tracks/22-settings-preferences/spec.md) - Profile settings, notification prefs, Stripe Connect, wallets. **P2**
- [ ] [Analytics & Admin Dashboard](tracks/23-analytics-admin/spec.md) - Platform metrics, treasury overview, financial reports. **P3**
