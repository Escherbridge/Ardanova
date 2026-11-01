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

`task.claim` (self-claim, "Pick up task") reuses `tasks.assign`'s `PUT /api/tasks/{id}/assign` route
with the caller's own id - there is no separate claim route. The API's `TaskClaimStore` makes this a
conditional write; 0 rows updated is a 409, mapped to `CONFLICT`. A 400 here is a membership/
eligibility refusal, not malformed input, so it maps to `FORBIDDEN` rather than `BAD_REQUEST`.

## Project follow (SR-03 Phase 3 §2, §8-6)

`projects.ts`'s `followProject`/`unfollowProject`/`getMyFollowState` assume
`POST|DELETE|GET /api/projects/{id}/follow` from Lane L's `ProjectFollowService` (not yet merged at
plan `computed_at_commit` 9a40e43) - confirm the route at the Phase 3 join. Like `posts.like`, there
is no follower id in the request: the follower is the signed actor. By design there is **no**
follower-list/count route, unlike `users.ts`'s legacy `getFollowers`/`getFollowCounts` - the plan's
own pushback (§8-6) names guild follow's public follower list as a flaw to not repeat.
