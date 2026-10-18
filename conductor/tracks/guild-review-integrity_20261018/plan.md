---
type: plan
---

# Plan: Guild Review Integrity and Escrow Dispute Adjudication

## Phase 1: Decide who may review (guilds lane)

- [ ] Record the review-eligibility decision in spec.md scope 1.
- [ ] Enforce it in `GuildReviewService.CreateAsync`; flow test in `GuildAuthorityFlowTests`.
- [ ] Add a project picker to `components/guilds/reviews-tab.tsx` if a project becomes required.

## Phase 2: Escrow dispute adjudication (escrow lane)

- [ ] `RefundAsync` accepts only `FUNDED`; only Resolve moves a `DISPUTED` escrow.
- [ ] Assignee dispute path in the service and controller; verbatim refusal messages in the UI.
- [ ] `resolvedById`/`resolvedAt` through the DBML, plus an additive migration.

## Phase 3: Data and route parity

- [ ] Rehearse and apply `20261017130000_guild_owner_member_backfill` and
      `20261018120000_guild_review_one_per_reviewer` on a populated copy, then locally.
- [ ] `ProjectsController.CreateProposalComment` uses `ProposalNotVisibleAsync`.
