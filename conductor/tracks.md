---
type: index
---
# ArdaNova Tracks Registry

## Core Modules (Tracks 01-11)
- [x] [Auth & User Core](tracks/01-auth/plan.md) - Identity, RBAC, Session Management, XP, Achievements, Leaderboards, Referrals.
- [x] [Project Management](tracks/02-projects/spec.md) - Project lifecycle, Agile hierarchy, CRUD, members, applications, invitations. *(Retroactive documentation)*
- [x] [Guild Module](tracks/03-guilds/spec.md) - Guild structure, membership, roles, applications, reviews. *(Retroactive documentation)*
- [x] [Financial & Tokenomics (Legacy)](tracks/04-finance/spec.md) - Superseded by Track 09. *(Retroactive documentation)*
- [x] [Social & Real-time](tracks/05-social/spec.md) - Chat complete, posts scaffolded, notifications backend complete. *(Retroactive documentation)*
- [x] [Events Module](tracks/06-events/spec.md) - Event lifecycle, RSVP, co-hosts, reminders. *(Retroactive documentation)*
- [x] [Credential Utility & Blockchain](tracks/07-credential-utility/spec.md) - Soulbound credentials, Algorand ASA minting, tier system.
- [x] [KYC & Identity Verification](tracks/08-kyc/spec.md) - Legacy ArdaNova-side KYC gate (manual review / Veriff path). **Superseded by D46/D48**: KYC is uniform and gates *value*, not project creation, and the provider is AZOA-hosted Identomat under node-level policy — see the spec's header and `azoa-user-onboarding`.
- [ ] [Tokenomics & Project Equity](tracks/09-tokenomics/spec.md) - Project tokens, ARDA platform token, equity distribution, payouts, and Stripe integration. **Core implementation exists; gated-commerce correctness, authorization, settlement durability, and live evidence remain P0.**
- [x] [Credential Frontend UI & Game SDK](tracks/10-credential-ui-gamesdk/spec.md) - Credential badges, tier UI, profile/project/guild integration, Unity/Godot SDK setup. **P0-P1 complete. P2 items remaining (grant modals, admin dashboard, component tests).**
- [ ] [Position Management, Equity & Treasury Pool](tracks/11-position-equity-treasury/spec.md) - Position detail/edit pages, equity cap enforcement, treasury pool allocation UI. **P0 complete. P1 in progress (editable pool targets, per-member equity view).**

## Frontend Integration (Tracks 12-14) — Phase A: Fix Broken Frontend
- [ ] [Dashboard & Feed Integration](tracks/12-dashboard-feed-integration/spec.md) - Wire dashboard feed, post system, social actions to backend. **P0**
- [ ] [Task Board Integration](tracks/13-task-board-integration/spec.md) - Wire Kanban task board to backend, task→equity flow. **P0**
- [ ] [Notification System UI](tracks/14-notification-ui/spec.md) - Notification bell, dropdown, real-time via SignalR. **P0**

## Revenue & Equity UI (Tracks 15-16) — Phase B: Core Revenue
- [ ] [Token & Equity Frontend UI](tracks/15-token-equity-ui/spec.md) - Project funding, investment flow, portfolio, payouts, equity dashboard. **P0**
- [ ] [Application & Hiring Flow UI](tracks/16-application-hiring-ui/spec.md) - Apply to opportunities, application review, accept/reject. **P1**
- [ ] [Projects Hiring Completion](tracks/projects-hiring-completion_20261017/spec.md) - Covers what loop 1 left open: role headcount, guild seating, applicant notifications, a DB-enforced single pending application, the dev-bypass tRPC context, 403 refusals, and the remaining assignee pickers. **P1**

## Growth & Polish (Tracks 17-19) — Phase C
- [ ] [Onboarding & First-Time Experience](tracks/17-onboarding/spec.md) - User type selection, profile wizard, first action prompt. **P1**
- [ ] [Auth Consent & Account Linking](tracks/auth-consent-and-account-linking/spec.md) - Terms/Privacy consent before the workspace, verified linking for email collisions, honest sign-in errors, profile-router PII projection. **P1**
- [ ] [Search & Discovery Enhancement](tracks/18-search-discovery/spec.md) - Global search, advanced filters, trending, recommendations. **P1**
- [ ] [Governance & Voting Polish](tracks/19-governance-polish/spec.md) - Credential-gated voting, delegation, execution feedback. **P2**

## Platform Maturity (Tracks 20-23) — Phase D
- [ ] [Dispute Resolution & Escrow UI](tracks/20-dispute-escrow-ui/spec.md) - Escrow display, milestone release, disputes, arbitration. **P2**
- [ ] [Reviews & Trust Enhancement](tracks/21-reviews-trust/spec.md) - Guild/project reviews, trust scores. **P3**
- [ ] [Settings & Preferences](tracks/22-settings-preferences/spec.md) - Profile settings, notification prefs, Stripe Connect, wallets. **P2**
- [ ] [Analytics & Admin Dashboard](tracks/23-analytics-admin/spec.md) - Platform metrics, treasury overview, financial reports. **P3**

## AZOA Blockchain-Node Integration — Phase E
> Authored against [`ARDANOVA-AZOA-INTEGRATION-CONTRACT.md`](ARDANOVA-AZOA-INTEGRATION-CONTRACT.md) §10/§11.
> **Superseded in part by D44-D51** (`ardanova-production-readiness-waves_20260907`, settled
> 2026-09-07). ArdaNova is a **tenant** on an operator-run AZOA node (D48), provisioning its
> users as **tenant-custodial** child users via a `tenant:provision` key its code already holds
> — not the "self-register, self-run, no fleet map, no `tenant:provision`" shape this header used
> to claim, which described the self-sovereign path W0.5 is deleting. KYC is uniform and gates
> value, not creation (D46); a stake is a real per-project ASA (D47); sponsorship is
> fiat→ARDA→project-token on chain, never a Stripe checkout for a project (D44/D45).
> Dependency order for the *tracks listed below* is still user-onboarding → provider-adapter →
> action-authoring → treasury-allocation. For the overall value-movement critical path, that
> linear order is superseded by the chain-first re-sequencing: reserve + genesis + per-project
> treasury (Wave 3) → the ARDA↔project-token exchange (Wave 5) → the first sponsorship (Wave 6) —
> see the plan's dependency graph. Real-value allocation gates on ArdaNova's own production
> trigger — NOT on AZOA-side P7, which is done (see the row below).

- [ ] [Gated Commerce and AZOA Settlement](tracks/gated-commerce-and-azoa-settlement/spec.md) - Durable bid, funding, task-award, project-token/ARDA exchange, wallet, and settlement flows. **P0 launch prerequisite.**
- [ ] [Financial Engine Domain Hardening](tracks/financial-engine-domain-hardening/spec.md) - Immutable asset definitions, utility-token versus equity/redemption-right policies, eligibility decisions, and fixed-scale migration. **P0 prerequisite; does not activate value movement.**
- [ ] [Guild & Governance Authority Follow-ups](tracks/guild-governance-authority-followups/spec.md) - Ownership transfer, two-party disputes, local-preview auth for tRPC, create-form fields, verified-email invites, draft voting duration, guild API contract tests. **P1; loop-1 follow-ups.**
- [~] [AZOA User Onboarding](tracks/azoa-user-onboarding/spec.md) - Link each ArdaNova user to a **tenant-custodial** AZOA user (D48 — not the self-sovereign shape this row used to name); readiness check before **any value action**, uniformly (D46 — not a "Tier-2" tier). **Dependency root. Largely shipped — the legacy `/api/azoa/avatar/*` routes are **gone, not 410**: Wave 0 deleted `AzoaAvatarController` and the whole self-sovereign path, so those paths now 404. The live surface is `api/azoa/custodial-account` + `IAzoaValueEligibilityService`, both committed.**
- [~] [AZOA Provider Adapter](tracks/azoa-provider-adapter/spec.md) - `AzoaBackedAlgorandService` behind `IAlgorandService`, feature-flagged (`Algorand:Provider` = Legacy | Azoa | Simulated), platform mnemonic removal still owed. **Backend-only.** Its `MintSoulboundASAAsync`/`CreateFungibleASAAsync` calls are one-off ASA creation (credential mint, per-project genesis) — unaffected by D47. Reward/funding **distribution** after that first mint belongs to `treasury-reward-to-azoa-allocation`, and D47 requires that path to be `IssueUnits` from the reserve, never a per-reward `Mint`.
- [~] [AZOA Action Authoring](tracks/azoa-action-authoring/spec.md) - Publish scrum-lifecycle action definitions/templates; users self-run; board events → gate signals. **Authoring + signal services and their tests shipped; no controller or frontend surface yet.**
- [~] [Treasury / Reward → AZOA Allocation](tracks/treasury-reward-to-azoa-allocation/spec.md) - Funding + task reward → `POST /api/allocation/{userId}` with stable idempotency keys. **Allocation client + tests shipped; no production trigger dispatches it yet.** ~~Real-value flip gated on AZOA-side `action-reconcile-retry-wiring` (P7).~~ ⚠ **Corrected 2026-09-07: P7 is NOT a blocker and that track never existed.** AZOA's `conductor/retros/DEPLOY-STEPS-TODO.md:32` records P7 (reconcile-before-retry) as done, and the code agrees — a run parked in `AwaitingReconciliation` is never auto-re-broadcast, with operator re-probe and sweep routes on `ActionController` and coverage in `DurableWorkflowEngineTests`. The slug cited here came from §12 of the shared integration contract, which was a Phase-2 *proposal list*, not a status board; five of its AZOA slugs were never created. The real-value flip is gated on ArdaNova's own production trigger, not on AZOA.

---

## Cross-cutting status — reconciled 2026-09-07

**Legend:** `[x]` shipped · `[~]` partially on disk · `[ ]` not started.

### The AZOA rename cutover (`f2e4bc5`) landed
AZOA renamed its domain nouns — **avatar → user**, **quest → action** — across
routes, wire fields and SDK exports. ArdaNova adapted in `f2e4bc5`, which fixed a
silent production break: `AzoaCustodialAccountGateway.StatusWire` had matched
AZOA's `avatarId` case-insensitively with no `[JsonPropertyName]`, so AZOA's
switch to `userId` would have yielded `null` — tripping the readiness invariant
and making every money-moving action fail closed with no error raised.

The rename was applied to the **contract surface only**. Read every doc in this
tree with that split in mind:

| Renamed (use new names) | Deliberately unchanged (old names are correct) |
|---|---|
| `/api/user/register` (ArdaNova no longer calls it), `/api/action*`, `POST /api/allocation/{userId}` | C# types: `AzoaQuestNodeAdapter`, `AzoaQuestNodeClient`, `IAzoaQuestAuthoringService` — `IAzoaAvatarService` was **deleted** in Wave 0, not left unrenamed |
| Wire field `userId` (`[JsonPropertyName("userId")]`) | DB column `User.azoaAvatarId`, config key `Azoa:QuestApiKey` |
| npm `azoa-sdk` exports | Directory `ArdaNova.Application/Azoa/Quests/` |
| Track slugs `azoa-user-onboarding`, `azoa-action-authoring` (contract §10) | |

### Open items
See [`RUNBOOK.md`](RUNBOOK.md) — in particular the **unpublished `azoa-sdk@0.3.0`**
(CI-red, green-only-under-`npm link`) and the ~81 uncommitted files from the
in-flight security-hardening pass.
- [ ] [AZOA Avatar Onboarding](tracks/azoa-avatar-onboarding/spec.md) - Link each user to a self-sovereign AZOA avatar; wallet-bound check before Tier-2. **Dependency root.**
- [ ] [AZOA Provider Adapter](tracks/azoa-provider-adapter/spec.md) - `AzoaBackedAlgorandService` behind `IAlgorandService`, feature-flagged, platform mnemonic removed. **Backend-only.**
- [ ] [AZOA Quest Authoring](tracks/azoa-quest-authoring/spec.md) - Publish scrum-lifecycle quest definitions/templates; avatars self-run; board events → gate signals.
- [ ] [Treasury / Reward → AZOA Allocation](tracks/treasury-reward-to-azoa-allocation/spec.md) - Funding + task reward → `POST /api/allocation` with stable idempotency keys. **Real-value flip gated on AZOA P7.**

## Loop 1 design QA follow-ups

- [ ] [User Directory Privacy](tracks/user-directory-privacy/spec.md) - Unauthenticated `user.getAll` exposes every email/phone; protect the procedures and project a public profile DTO in .NET. **P0 security.**
- [ ] [Terms and Privacy Pages](tracks/legal-pages/spec.md) - Publish human-approved terms and privacy notice and link them from sign-up and add-funds. **P1, blocked on approved text.**
- [ ] [Nova AI powered by OVERSEASGENAI](tracks/nova-ai-ogai-partner/spec.md) - White-label project refinement via the OGAI partner validate API; .NET-only caller. **Loop 1 built; rollout owner-gated.**
- [ ] [Nova AI modes beyond refinement](tracks/nova-ai-partner-modes/spec.md) - Ask/Draft/Review/Present/Rehearse need a new OGAI partner capability (validate does not map); brief prefill for project create. **Proposed; needs OGAI lease holder.**
- [ ] [Nova AI live rollout](tracks/nova-ai-live-rollout/spec.md) - The first real end-to-end run against OVERSEASGENAI (every QA pass so far ran on the local preview). Push and deploy the partner branch, set staging keys, get a live verdict for a commercial idea and a community idea, and size capacity. **Proposed; needs the OGAI lease holder and the owner.**
- [ ] [Retire ardanova-ai-client](tracks/ai-client-retirement/spec.md) - Dormant FastAPI/CrewAI service superseded by Nova AI; routes now key-guarded. **Proposal; owner decides deletion.**
- [ ] [Backend Authorization Hardening](tracks/backend-authz-hardening_20261017/spec.md) - User PII projection, actor gating for 12 service-key-only controllers, ownership checks moved into .NET services. **P0 security; from loop-1 backend QA.**
- [ ] [API Path-Segment Safety](tracks/api-path-segment-safety/spec.md) - Unencoded ids in endpoints/*.ts let any public procedure issue arbitrary server-keyed GETs; central path builder + id schema sweep. **P0 security; user/profile routers closed in loop 2.**
- [ ] [Projects Hiring Follow-ups (loop 2)](tracks/projects-hiring-followups_20261018/spec.md) - Paid-bid seating and token-config setup (finance, on hold), co-manager UI parity, an accept-applications toggle, task editing, thin project/task routers, and a shared ErrorCard. **P1**
- [ ] [Projects Hiring Follow-ups (loop 3)](tracks/projects-hiring-loop3-followups_20261018/spec.md) - Applicants can track and withdraw their applications, the opportunity form drops FOUNDER and GUILD_POSITION for projects, deadline/fill (schema), hiring notifications, and a rank-aware Team tab for co-managers. **P1**
- [ ] [Governance Reachability and Proposal Privacy](tracks/governance-reachability_20261018/spec.md) - Voting needs a credential the local sandbox cannot issue, solo projects never reach quorum, project-scoped proposal routes still leak drafts. **From loop-2 guilds QA.**
- [ ] [Design-System Debt Burn-down](tracks/design-debt-burndown_20261017/spec.md) - Move ~70 raw `error.message` sites, 156 raw palette classes and 34 shared titles onto the loop-2 seams; ratchet in `design-gate.test.ts`. **P1; from loop-2 design QA.**
- [ ] [Backend Ownership Enforcement](tracks/backend-ownership-enforcement_20261017/spec.md) - Event/product/referral ownership in .NET services, BFF status-to-TRPCError mapping, and a C# generator that preserves hand-written entity code so the CI contract gate passes. **P0/P1; follows loop-2 pipeline gating.**
- [ ] [Guild Review Integrity and Escrow Dispute Adjudication](tracks/guild-review-integrity_20261018/spec.md) - Decide who may review a guild, stop funder self-refund of DISPUTED escrows and add the contributor dispute path, rehearse the two pending guild migrations. **From loop-3 guilds review.**
- [ ] [Backend API Boundary Completion](tracks/backend-api-boundary-completion_20261018/spec.md) - Loop 3 put event/product/referral ownership, event visibility, validation and UTC timestamps into the .NET pipeline; feature lanes still need to thin BFF pre-checks, filter event lists in the query, map router errors, and enforce upload size. **P1; from loop-3 backend QA.**
- [ ] [Design Loop-3 Follow-ups](tracks/design-loop3-followups_20261018/spec.md) - Wire `shouldRetryQuery` (invest ~10 s 404 stall) and an honest "funding not set up" state, per-route titles (48 untitled), 30 white-alpha borders, proposals-tab off-system. Ceilings in `design-gate.test.ts`. **P1; from loop-3 design QA.**
