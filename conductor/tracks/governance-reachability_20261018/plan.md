---
type: plan
---

# Plan — Governance Reachability and Proposal Privacy

## Phase 1: Make voting testable locally (seed/integration lane)
- [ ] Seed ACTIVE MembershipCredentials for seeded project members (scope 1).
- [ ] Seed the guild owner's OWNER row and honest counters (scope 5).
- [ ] Apply migration `20261017130000_guild_owner_member_backfill` to the local demo DB.

## Phase 2: Close the project-scoped proposal routes (projects lane)
- [ ] `ProjectsController` proposal reads use the viewer-scoped governance reads and check `ProjectId` (scope 3).
- [ ] Flow test: anonymous read of a private proposal through a public project id is 404.

## Phase 3: Decisions
- [ ] Product: quorum floor for one-member projects (scope 2).
- [ ] Auth: verified-email claim, then uniform invite responses (scope 4).

## Phase 4: QA harness
- [ ] Invite assertions read the modal's visible error text (scope 6).
