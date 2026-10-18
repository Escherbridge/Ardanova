# Application services

## Guild authority

`GuildServices.cs` owns every "who may act on this guild" decision. Controllers pass a
`GuildActor(Id, IsPlatformAdmin)` built only from the signed actor assertion; request bodies never
carry an owner, reviewer, inviter, applicant or follower id (those DTO fields were removed so a
caller cannot even express one). The Next.js `guild` router is a pass-through and holds no
ownership checks.

- **Owner**: `guild.ownerId`, an `OWNER` membership row, or a platform admin.
  **Manager**: owner, or an `ADMIN` member. Managers edit the profile, post updates, add/remove
  members, invite, and review applications.
- **OWNER is never grantable** through member update, add, invitation or application
  (`GuildAuthority.TryParseGrantableRole`). Ownership transfer is a separate act that does not
  exist yet. The OWNER row cannot be re-roled or removed.
- **Only the owner moves people in or out of ADMIN** (appoint, demote, remove, invite or admit as
  admin), so an admin cannot mint further admins.
- **Every child lookup is scoped by `(guildId, id)`.** A member, review, update, application or
  invitation id that belongs to another guild is NotFound through this guild's route. This closes
  the cross-guild IDOR where an owner of guild A changed roles in guild B.
- **Invitations bind to an account at creation.** The invitee is looked up by user id or by the
  email on their account (case-insensitive); no account means the invitation is refused, not
  stored. Accept/reject require `invitedUserId == actor`, `PENDING`, and not expired (14 days).
  Email-only rows stay fail-closed (see `ArdaNova.API/Controllers/AGENTS.md` §Email-only
  invitations). Trade-off: a manager can learn whether an email has an account; acceptable for a
  member-to-member invite, revisit if invites open to the public.
- Members cannot review their own guild; one review per person per guild.
- `VerifyAsync` (the verified badge) is gated on the admin API key in the controller.
- Guild child DTOs are mapped by hand in the service (there are no AutoMapper maps for
  invitations, applications, updates or follows); member reads carry name/image, never email.

## Governance

`GovernanceService` treats a proposal as a vote of record: one member one vote, quorum counted in
confirmed members with a two-ballot floor, outcome resolved from the tally.

- **One ballot per member is enforced by the database.** The check-then-insert in
  `CastVoteAsync` is not atomic; the `Vote (proposalId, voterId)` unique index (migration
  `20261017120000_vote_one_ballot_per_member`) is the guard, and its SQLSTATE 23505 maps to
  "already voted".
- **Minimum voting window (12 h).** A stated `votingEnd` closer than `MinimumVotingWindow` from
  now is refused at create, draft edit and publish. Publish refuses rather than silently moving
  the date: a closing time the gate does not honour is worse than none. 12 h (not 24) so a
  one-day draft can still be opened the same day it was written.
- **The tally is the only arithmetic.** `ProposalDto` and the vote summary both expose
  `EligibleMembers`, `VotesNeededForQuorum`, `QuorumReached` and per-choice counts from
  `TallyAsync`; clients display these and never recompute quorum.
- Closing early vs. cancelling: see the `ProposalCloseAuthority` remarks in `GovernanceServices.cs`.
