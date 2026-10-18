---
type: spec
---

# Governance Reachability and Proposal Privacy (loop 2 follow-ups)

## Overview

Loop 2 fixed proposal comments (the request body no longer fails JSON binding), scoped every
`/api/Governance/*` read to the viewer, made a guild's owner of record count as a member, and
derived guild project counts from real rows. This track holds what another lane owns, or what
needs a product decision.

## Scope

1. **Voting needs a MembershipCredential that cannot be issued locally.** `GovernanceService`
   counts the electorate from `MembershipCredential` (ACTIVE). The seed
   (`ardanova-client/scripts/seed-local-demo.ts`) writes a `ProjectMember` row but no credential,
   and issuing one is gated on the AZOA value-eligibility service, which the local sandbox does not
   run (HTTP 409 `ServiceUnavailable`). Owner: seed/integration lane. Fix: seed ACTIVE credentials
   for seeded members, or give the local sandbox a stub eligibility answer.
2. **A solo-founder project can never decide anything.** `MinimumBallotsForQuorum = 2` means a
   project with one eligible member always resolves EXPIRED. Product decision: keep the floor
   (anti-manipulation) and say so in the UI, or scale it to the electorate size.
3. **Project-scoped proposal routes** (`ProjectsController`, projects lane): `GetProposalById`,
   `GetProposalComments`, `GetProposalVoteSummary` and `GetVotes` check the routed project is
   visible but not that the proposal belongs to it, and the project proposal list includes other
   members' DRAFT proposals. Route them through `IGovernanceService.GetVisibleProposalByIdAsync`
   / `SearchVisibleProposalsAsync` (added in loop 2) and compare `proposal.ProjectId`.
4. **Guild invite reveals account existence** ("No ArdaNova account uses that email yet"). A truly
   uniform answer needs email-bound pending invitations, which need a verified-email claim
   (`ArdaNova.API/Controllers/AGENTS.md` §Email-only invitations). Until then the clear inline
   refusal is kept deliberately (critique: preserve).
5. **Seed realism** (`seed-local-demo.ts`): insert the guild owner's OWNER `GuildMember` row, real
   `membersCount`, `projectsCount: 0`, `isVerified: false`. Migration
   `20261017130000_guild_owner_member_backfill` fixes existing databases once applied.
6. **QA harness**: the guild invite assertions read an empty response-message field; read the
   modal's visible error text instead.

## Acceptance

- A seeded project member can cast a ballot locally; a two-member proposal can reach PASSED.
- Anonymous `GET /api/projects/{public}/proposals/{privateProposal}` returns 404.
- Seeded guild Members tab lists the owner.
