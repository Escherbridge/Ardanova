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
- **The owner of record is a member.** `GuildAuthority.IsMemberAsync` answers "already belongs"
  from `guild.ownerId` OR a membership row, because legacy and seeded guilds have no OWNER row
  (migration `20261017130000_guild_owner_member_backfill` adds them). Every "already a member"
  check (add member, invite create/accept, apply/admit, review) uses it, so an owner cannot invite,
  admit or add themselves into a second, lesser row.
- **Concurrent joins are settled by the `(guildId, userId)` unique index.** The existence checks
  are not atomic; a 23505 on save becomes the same "already a member" Conflict
  (`GuildAuthority.IsDuplicateGuildUserRow`), mirroring the ballot index below.
- **`ProjectsCount`, `ReviewsCount` and `Rating` are counted, not stored.** Nothing maintains
  `Guild.projectsCount`, and the stored `rating`/`reviewsCount` were a read-modify-write that two
  concurrent reviews left wrong, so `GuildService` reads derive all three from the rows
  (`assignedGuildId` projects; `GuildReview` ratings). The stored columns are still recounted after
  each review write, best effort, for any raw-SQL reader.

### Guild reviews

- Members cannot review their own guild; one review per person per guild, enforced by the
  `GuildReview (guildId, userId)` unique index (migration
  `20261018120000_guild_review_one_per_reviewer`, which also removes pre-existing duplicates). A
  23505 on save is the same "already reviewed" Conflict as the pre-check.
- A review may name a `projectId` only when that project exists, was assigned to this guild, and
  was created by the reviewer; anything else is a ValidationError, so a review cannot claim a
  working relationship that never existed. Reviews without a project are still accepted: requiring
  a real engagement before anyone may review is a product decision tracked in
  `conductor/tracks/guild-review-integrity_20261018`.
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
  Both close routes (`PATCH /api/Governance/proposals/{id}/close` and
  `POST /api/projects/{pid}/proposals/{id}/close`) prove the closer runs the project and then call
  `IProposalVoteClosing.CloseVoteAsync` with `ProjectAuthority`. The governance route used to go
  through the generic update seam, which carries `Unattributed` and therefore refused every early
  close, even a legitimate manager's.
- **Writing into a discussion requires reading it.** `CreateProposalCommentAsync` resolves the
  author's `ProjectViewer` (admin from the stored user role) and answers NotFound unless the
  proposal is in `VisibleProposals` for them, so neither comment route can post into a DRAFT or a
  private project's proposal the author cannot GET. Visibility is the only bar: anyone who can read
  a published proposal may discuss it (voting, unlike commenting, still requires membership).
  A reply's `parentId` must be a comment on the same proposal (otherwise a ValidationError, not an
  opaque FK 400), and content is capped at `MaxCommentLength` (10,000 characters) on create and
  edit. Editing or deleting someone else's comment is Forbidden (403), not a 400.
- **Route-bound request fields.** `CreateProposalCommentDto.ProposalId`/`UserId` are not
  `required`: the client posts `{content, parentId}` and the controller fills both from the route
  and the actor. A C# `required` member makes System.Text.Json reject a body that omits it, so
  every comment failed model binding with a 400 before the controller ran. Do not mark a field
  `required` on a request DTO when the server supplies it.

### Governance visibility

Every anonymous-readable `/api/Governance/*` read answers for a `ProjectViewer` (built in the
controller only from a verified actor assertion) through `GovernanceService.VisibleProposals`:

- the proposal's project must pass `ProjectQueryFilters.VisibleTo` (DRAFT projects are private
  to their creator, members and admins), and
- a DRAFT proposal is visible only to its author and to the people who run the project (creator,
  FOUNDER, LEADER, CORE_CONTRIBUTOR — the same set as `CanManageProjectAsync`). Admins see all.

Votes, vote summary and comments open with the same check and answer NotFound, so a private
proposal's ballots and discussion are as hidden as the proposal. Search and paging apply the
visibility source first and the caller's filters after, so `projectId` cannot widen it. The
unscoped `IGovernanceService` reads remain for internal callers; the project-scoped routes in
`ProjectsController` still use them (tracked in `conductor/tracks/governance-reachability_20261018`).
The Next.js `governance` router wraps its public reads in `readAsViewer` so a signed-in reader is
named to the API.

## Profile rules

`UserProfileRules` (bottom of `UserServices.cs`) holds every rule for profile, skill and experience
writes, because the .NET API is reachable by any BFF, SDK or MCP client with an API key and an actor
assertion. The Next.js zod checks in `routers/profile.ts` only mirror these rules to give earlier
feedback; the service rules are the ones that apply.

- **Links**: website and profile image must be absolute `http(s)` URLs. LinkedIn and Twitter may be a
  bare handle, but any value that names a scheme (or starts with `//`) must be `http(s)` with a host.
  This blocks stored `javascript:`/`data:` URLs, which every non-web consumer (SDK, MCP, email) would
  otherwise trust. An empty string still clears a field.
- **Skills**: the name is trimmed and 1-100 characters, the level is 1-10 on create and update, and the
  same name (case-insensitive) cannot be listed twice by one user.
- **Experience**: title and organization are required (at most 200 characters each). The end date cannot
  be before the start date, and a current role has no end date. Marking an entry current clears its stored end date.
- **Owner id**: `CreateUserSkillDto.UserId` and `CreateUserExperienceDto.UserId` are `[JsonIgnore]`. The
  controller sets them from the route after the actor check. They used to be `required`, which made
  System.Text.Json reject the client's `{skill, level}` body with a 400 before the controller ran, so
  every Add skill and Add experience failed.
