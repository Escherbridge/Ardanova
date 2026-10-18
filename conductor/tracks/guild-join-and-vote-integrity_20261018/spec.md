---
type: spec
---

# Guild Joining and Vote-of-Record Integrity (loop 4 follow-ups)

## Overview

Loop 4 (guilds lane, branch `loop4/guilds`) shipped these:

- Guild joining in the product: Apply, Withdraw, Follow, Leave, and a managers' Applications panel.
- ADMIN powers in the UI (`canManage`) and a role selector.
- The direct "add member without consent" path is now platform-admin only.
- Email invitations give one uniform answer, so they no longer reveal whether an email has an account.
- The ballot gate: the vote form appears only when the server would accept the ballot.

This track holds the remaining work. Each item was too large for the final loop, needs a schema
change, or belongs to another lane.

## Scope

1. **Escrow dispute adjudication (escrow lane, carry-over, HIGH).** This was re-verified as still
   unfixed on 1b58c40. There are two defects:
   - `TaskEscrowsController.Dispute` lets only the funder raise a dispute, so the contributor
     cannot.
   - `TaskEscrowServices.RefundAsync` accepts `DISPUTED`, so the funder can settle their own
     dispute without going through the admin Resolve.

   Fix: give the assignee a dispute path, and make `RefundAsync` accept only `FUNDED`. Add a flow
   test where the funder disputes, then tries to refund, and gets 400/409. Use the house
   exactly-once idiom: a guarded state transition plus a 23505 catch. This is the same work as
   `guild-review-integrity_20261018` Phase 2 and `20-dispute-escrow-ui`. Close all three together.
2. **Tally against the electorate that existed when voting opened (schema).** `TallyAsync` counts
   today's ACTIVE `MembershipCredential` rows and every stored ballot. As a result:
   - quorum shifts when members join or leave mid-vote;
   - a revoked member's ballot still counts.

   Fix:
   - Add a `ProposalElectorate (proposalId, userId)` snapshot, written by `PublishProposalAsync`.
   - Count only ballots from voters in that snapshot.
   - Check eligibility against the snapshot in `VoterRefusalAsync`, which `CastVoteAsync` and
     `GetBallotStatusAsync` already share.
   - Write an additive migration and a DBML change.

   Decide first: should a member revoked mid-vote keep their ballot? Recommendation: yes if it was
   cast before revocation. That is why this needs the snapshot, not a live filter.

   Note: `GovernanceServiceTests` mocks `CountAsync`. Move those cases to the in-memory flow
   harness.
3. **Execute has effects.** `ExecuteProposalAsync` only sets `EXECUTED` and writes a
   `ProposalExecution` row. Define per-`ProposalType` effects, for example a treasury transfer
   through the existing commerce services or a role change through `ProjectMember`. Run them in the
   same save. Until this is done, label the UI action "Mark as carried out".
4. **Email invitations: bind on verified signup, and rate-limit.** An invitation to an email with
   no account is now stored unbound, and nobody can accept it.
   - When an account verifies that email, bind the pending, unexpired rows: set `invitedUserId`.
     The binding must sit behind a verified-email claim (`Controllers/AGENTS.md` §Email-only
     invitations).
   - Add a per-actor policy for `POST /api/Guilds/{id}/invitations`, for example 30/hour, in
     `Middleware/ActorRateLimiting.cs` (owned by the platform lane).
5. **Ownership transfer and guild deletion in the UI.** `TryParseGrantableRole` refuses OWNER
   because no transfer exists. Add `POST /api/Guilds/{id}/transfer`:
   - owner only;
   - the target must already be an ADMIN;
   - it swaps `guild.ownerId` and the OWNER/ADMIN rows in one save.

   Then add owner-only "Transfer ownership" and "Delete guild" controls. `guild.delete` already
   exists in tRPC.
6. **Live quorum-floor test.** After `governance-reachability_20261018` scope 1 seeds
   `MembershipCredential` rows, add a live test: one confirmed member casts a lopsided vote, and it
   must resolve as not passed. This turns the static R9 regression into an enforced check.
7. **Seeds for the new flows.** `seed-local-demo.ts` (seed lane) should include:
   - a pending guild application, so the Applications panel shows in demos;
   - an ADMIN member, so the `canManage` paths are exercised live.

## Acceptance

- Scope 1: in a flow test, after the funder disputes, `refund` returns 400/409 and the escrow is
  still `DISPUTED`. The assignee can raise a dispute.
- Scope 2: in a flow test, a credential granted after publish does not change `EligibleMembers`.
  A voter revoked after casting is counted according to the recorded decision.
- Scope 4: in a flow test, inviting an unknown email, then signing up and verifying with it, puts
  the invitation in that user's inbox. A burst of invitations returns 429.
- Scope 5: in a flow test, after a transfer the old owner is an ADMIN and the new owner passes
  `IsOwnerAsync`. At no point is there a second OWNER row.
