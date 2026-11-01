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
