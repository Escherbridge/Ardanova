---
type: spec
---

# Guild & Governance Authority — Follow-ups (loop 1)

## Overview

Loop 1 moved every guild authority check into the .NET guild services, made guild writes
actor-bound, bound invitations to real accounts with an inbox, put a unique index behind
one-member-one-ballot, set a 12 h minimum voting window, and made governance pages show the API
tally. This track holds what was too large, or owned by another lane, to finish there.

## Scope

1. **Ownership transfer** — an explicit, two-sided transfer (owner offers, recipient accepts) that
   updates `Guild.ownerId` and swaps the OWNER row atomically. Today OWNER is never grantable.
2. **Two-party disputes** (escrow lane: `TaskEscrowServices.cs`, `TaskEscrowsController.cs`,
   `task-escrow.ts` router) — the contributor (assignee) must be able to raise a dispute, and a
   funder must not be able to refund a DISPUTED escrow themselves; resolution is admin-only.
3. **Local preview auth** (auth lane: `src/server/auth/index.ts`, `src/server/api/trpc.ts`) —
   `auth()` must honour `DEV_AUTH_BYPASS` the way `authForPage()` does, so `protectedProcedure`
   works in local preview. Until then every guild/governance write is unreachable from the UI.
4. **Create-guild form** (`src/components/guild-form.tsx`) — render Specialties, Address,
   Portfolio and Logo in create mode and send them; the API and router accept them now.
5. **Invitations for people without an account** — needs a verified-email claim at the auth
   boundary before an email-only invitation can be accepted (Controllers/AGENTS.md).
6. **Draft voting duration** — store a duration on drafts instead of an absolute `votingEnd`, so
   a draft opened late is not refused by the minimum-window rule.
7. **API contract tests** — WebApplicationFactory flows that post the client's real payloads to
   `GuildsController` so DTO drift fails CI.

## Acceptance

Each item ships with a flow test through its public seam asserting stored rows.
