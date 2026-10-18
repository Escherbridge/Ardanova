---
type: spec
---

# Guild Review Integrity and Escrow Dispute Adjudication (loop 3 follow-ups)

## Overview

Loop 3 closed the server-side defects the adversarial review found in guilds and governance:
proposal comments now require read visibility, threading is same-proposal only, comments are
length-capped, the governance close route works for managers, a `GuildReview (guildId, userId)`
unique index settles concurrent reviews, and guild rating/reviewsCount are derived from rows.
This track holds what needs a product decision or belongs to another lane.

## Scope

1. **Who may review a guild (product decision, guilds lane).** Today any signed-in non-member can
   rate any guild; only a named `projectId` is checked (it must be the reviewer's project and
   assigned to the guild). Review-bombing by strangers is still possible. Options: require a
   completed engagement (reviewer created a project the guild was assigned to, or a task the guild
   delivered) before any review; or keep open reviews but weight/label "verified engagement"
   reviews. The UI (`components/guilds/reviews-tab.tsx`) does not send a `projectId` at all, so the
   chosen rule needs a project picker there.
2. **Escrow disputes (escrow lane, carryover from loops 1-3).** `TaskEscrowServices.RefundAsync`
   still accepts `DISPUTED`, so a funder can open a dispute and refund themselves past the
   admin-only Resolve; the contributor (task assignee) has no dispute path; resolutions record no
   `resolvedById`/`resolvedAt`. Required by `conductor/tracks/20-dispute-escrow-ui`
   ("Either contributor or project owner can raise a dispute"). Owned by
   `TaskEscrowsController.cs` / `TaskEscrowServices.cs`, outside the guilds lane.
3. **Apply the two pending data migrations to the local demo DB (seed/integration lane).**
   `20261017130000_guild_owner_member_backfill` and `20261018120000_guild_review_one_per_reviewer`
   are both UNREHEARSED. Until the first runs, the Members tab shows a guild's owner as absent.
4. **Project-scoped comment route parity (projects lane).** `ProjectsController.CreateProposalComment`
   now inherits the visibility check from the service, but still pre-reads with the unscoped
   `GetProposalByIdAsync`, so a hidden proposal filed under another project answers "not found in
   the routed project" before the service's NotFound. Harmless (both 404) but should use
   `ProposalNotVisibleAsync` like its sibling reads.

## Acceptance

- A product decision for scope 1 is recorded here, and the service plus a flow test enforce it.
- A funder cannot refund a DISPUTED escrow; an assignee can raise a dispute; resolution records who
  decided it (flow tests through the service).
- Both migrations have been rehearsed on a copy of a populated database and applied locally.
