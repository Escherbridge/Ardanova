---
type: plan
---

# Plan — Guild & Governance Authority Follow-ups

## Phase 1: Unblock the UI (other lanes)
- [ ] Auth lane: `auth()` honours `DEV_AUTH_BYPASS` (scope item 3); re-run loop-1 guild QA flows.
- [ ] UI lane: create-mode fields in `guild-form.tsx` (scope item 4).

## Phase 2: Disputes (escrow lane)
- [ ] Assignee may raise a dispute; funder refund refused while DISPUTED; admin resolution path.
- [ ] Flow tests in `TaskEscrowServiceTests` asserting escrow status rows.

## Phase 3: Guild ownership & invitations
- [ ] Two-sided ownership transfer in `GuildService` (+ controller, router, UI).
- [ ] Verified-email invitation binding (depends on auth boundary claim).

## Phase 4: Governance & contracts
- [ ] Draft voting duration instead of absolute `votingEnd` (DBML + migration).
- [ ] `ArdaNova.API.Tests` WebApplicationFactory contract tests for guild payloads.
