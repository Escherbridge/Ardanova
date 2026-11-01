# API endpoint clients

## Guilds

`guilds.ts` request bodies mirror the .NET DTOs field for field. A body that does not match fails
ASP.NET model binding with 400 before any service runs, which is how create-review, post-update,
add-member and delete-guild were silently broken (`reviewerId`/`content` vs `UserId`/`Comment`,
required `GuildId` in bodies, a required `requesterId` query). Identity is never sent: owner,
reviewer, inviter, applicant and follower are the signed actor the backend reads from the
`X-Ardanova-Actor` assertion, so these clients have no `userId`/`ownerId` parameters for writes.

When a DTO changes in `ArdaNova.Application/DTOs/GuildDtos.cs`, change the matching interface here
in the same commit.

## Feed

`feed.ts` wraps `GET /api/feed?cursor=&limit=&scope=` (home, `project:{id}`, `guild:{id}`, `tag:{tag}`). The
contract lives in `src/lib/contracts/feed-contract.ts` and mirrors `ArdaNova.Application/DTOs/FeedDtos.cs`.
`postType`, `sourceType` and reason `kind` are read tolerantly (`FeedPostType | (string & {})`): the API adds
envelope kinds before this client redeploys, and one unknown card must not fail the page. The cursor is opaque -
pass back `nextCursor` untouched; the API refuses a cursor from another scope (400). The viewer is the signed
actor, never a parameter. Render a card's primary action from `viewer` (`isMember`/`isFollowing`/`canClaim`),
never from reason text; `funding` is a percent only, by design. Server rules: `ArdaNova.Application/Feed/AGENTS.md`.

## HELP_WANTED posts (SR-03 Phase 3 §2)

`posts.ts`'s `CreatePostDto.taskId` is required only for `type: "HELP_WANTED"` and refused on every
other type - the API decides, this client never replicates the rule. `Post.taskId [unique]` (S3)
means a second ask on the same task is a 23505 the API maps to a 409 carrying the existing post's
id as the body's `conflictId` field (`base-client.ts` extracts it the same way it extracts money's
`intentId`). `post.ts`'s `create` turns that into a `TRPCError` with code `CONFLICT` whose `cause`
is `HelpWantedConflictCause` (`server/api/help-wanted-conflict-error.ts`); `trpc.ts`'s
`errorFormatter` exposes it as `error.data.helpWantedExistingPostId` so the composer can offer
"Edit your ask" instead of a bare error. A project manager removing a HELP_WANTED on their project
reuses the plain `post.delete` route (`DELETE /api/posts/{id}`) - the API's authorization decides
who may, same as `project.removeMember`.

## Task claim (SR-03 Phase 3 §2)

`task.claim` (self-claim, "Pick up task") calls `tasks.claim`: the same `PUT /api/tasks/{id}/assign`
route as `tasks.assign`, with the caller's own id and `claim: true` - there is no separate claim route.
The flag is what keeps a manager's tap honest: without it the API reads a manager's self-assign of a
task someone holds as a deliberate takeover, so a card rendered before a member claimed the task
would silently take it from them. With it, the API's `TaskClaimStore` conditional write decides for
everyone; 0 rows updated is a 409, mapped to `CONFLICT`. A 400 here is a membership/eligibility
refusal ("Assignee must be a member of the project"), not malformed input, so it maps to `FORBIDDEN`
rather than `BAD_REQUEST`.

The 409 body is `{ error, code }` with .NET `TaskClaimCodes`: `TASK_TAKEN`, `TASK_DONE`,
`TASK_UNDER_AGREEMENT`. `base-client.ts` already extracts `code`; `task.claim` wraps it in
`TaskClaimRefusalCause` (`server/api/task-claim-conflict-error.ts`) and `trpc.ts`'s `errorFormatter`
exposes it as `error.data.taskClaimCode`. The browser reads it with `taskClaimRefusalOf`
(`src/lib/tasks/claim-refusal.ts`) to word "already taken" apart from "done"; a code this client
does not know reads as null, and the API's own message still arrives (`describeMutationError`).
Proven over real HTTP in `server/api/root.test.ts`.

**Testing these routes:** `server/api/dotnet-test-edge.ts` runs the real app router over in-process HTTP
with the real API client, and fakes only .NET's `fetch` (answered with the API's real JSON envelopes). It
records each .NET request as .NET reads it (method, URL, body, signed actor subject). `root.test.ts` and the
`post`, `task` and `project-follow` router tests use it. Do not mock `apiClient` for these routes: a mock
can hand the router a pre-parsed body that `base-client.ts` never produces.

## Project follow (SR-03 Phase 3 §2, §8-6)

`projects.ts`'s `followProject`/`unfollowProject`/`getMyFollowState` call Lane L's
`POST|DELETE|GET /api/projects/{id}/follow` (confirmed at the wave-1 join). The bodies differ by
verb: POST answers the follow row (.NET `ProjectFollowDto`), GET and DELETE answer
`ProjectFollowStatusDto` `{ following }`. The `project` router maps all three to one
`ProjectFollowState` `{ projectId, isFollowing }`, so callers never see the .NET shapes (passing the
raw body through is how `isFollowing` read `undefined` before the join). Like `posts.like`, there
is no follower id in the request: the follower is the signed actor. By design there is **no**
follower-list/count route, unlike `users.ts`'s legacy `getFollowers`/`getFollowCounts` - the plan's
own pushback (§8-6) names guild follow's public follower list as a flaw to not repeat.

## Memberships (SR-03 Phase 3 final join)

`projects.getMine()` → `GET /api/projects/mine` and `guilds.getMine()` → `GET /api/guilds/mine`: what the signed
actor belongs to (created/owns, or a member row). No userId in the request. The `project.getMyMemberships` and
`guild.getMyMemberships` procedures feed the composer's attach picker; `getMyProjects`/`getMyGuilds` keep their
creator/owner meaning for the profile page and the opportunity entity picker. Proof:
`server/api/routers/memberships.test.ts`.
