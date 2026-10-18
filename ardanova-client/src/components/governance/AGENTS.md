# components/governance

## Ballot gate

`overview-tab.tsx` shows the "Cast Your Vote" form only when the API's
`GET /api/governance/proposals/{id}/my-ballot` (tRPC `governance.getMyBallot`)
answers `canVote: true` and the proposal's `votingEnd` has not passed.

- The API computes `canVote` with the same private checks `CastVoteAsync` uses
  (`VotingWindowRefusal` + `VoterRefusalAsync` in `GovernanceServices.cs`), so the
  form and the server cannot disagree about who may vote.
- Everyone else gets one status line from `ballotStandingMessage`
  (`proposal-tally.ts`): their own cast choice, "voting has closed", "sign in", or
  the server's own refusal reason (for example "Only confirmed members of this
  project can vote on it").
- The client's `votingEnd` check is only for display. Status leaves ACTIVE lazily,
  when a read resolves due proposals, so the client hides the form as soon as the
  window ends instead of waiting for that.
