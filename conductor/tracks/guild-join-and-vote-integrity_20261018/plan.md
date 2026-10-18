---
type: plan
---

# Plan: Guild Joining and Vote-of-Record Integrity

## Phase 1: Escrow adjudication (escrow lane; carry-over HIGH)

- [ ] `RefundAsync` accepts `FUNDED` only. A `DISPUTED` escrow moves only through admin Resolve.
- [ ] Add an assignee dispute branch in `TaskEscrowsController.Dispute`.
- [ ] Flow test: dispute, then refund, returns 400/409 and the escrow stays `DISPUTED`.

## Phase 2: Electorate snapshot (governance; schema)

- [ ] Record the decision on revoked-after-casting ballots in spec.md scope 2.
- [ ] Add `ProposalElectorate` to the DBML plus an additive migration. The orchestrator regenerates
      Prisma and EF.
- [ ] `PublishProposalAsync` writes the snapshot.
- [ ] `TallyAsync` and `VoterRefusalAsync` read the snapshot.
- [ ] Move the `CountAsync`-mocked tally cases to `GovernanceBallotIntegrityFlowTests`.

## Phase 3: Execution effects (governance)

- [ ] Define effects per `ProposalType` and run them in the same save as `EXECUTED`.
- [ ] Until then, rename the UI action to "Mark as carried out".

## Phase 4: Invitations and ownership (guilds)

- [ ] Bind unbound email invitations on verified signup.
- [ ] Add a per-actor invitation rate limit (platform lane owns the middleware).
- [ ] Add the transfer-ownership endpoint, then owner-only Transfer and Delete controls.

## Phase 5: Live verification (seed and QA lanes)

- [ ] Seed a pending application and an ADMIN member.
- [ ] Add the live quorum-floor test once `MembershipCredential` rows exist.
- [ ] Live re-test of the loop-4 guild flows: apply, accept, leave, admin powers, the uniform
      invitation answer, and the ballot gate.
