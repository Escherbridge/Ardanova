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
