-- Every guild's owner is represented by an OWNER membership row. Data only; no schema change.
-- Lands AFTER 20261017120000_vote_one_ballot_per_member.
--
-- GuildService.CreateAsync has always written the OWNER row in the same save as the guild, but
-- guilds created before that (and rows inserted directly, e.g. by seed-local-demo.ts) have none.
-- Without it the Members tab reads "0 members" and the owner is missing from every member list.
-- GuildAuthority.IsMemberAsync already treats Guild.ownerId as a member, so this backfill changes
-- what lists show, not who may act. Guarded and idempotent: re-running inserts nothing.
-- An owner who already holds a non-OWNER row is left alone (the (guildId, userId) index allows
-- one row per person); that case is not expected and would need a human decision.

INSERT INTO "GuildMember" ("id", "guildId", "userId", "role", "joinedAt")
SELECT 'gm_owner_' || g."id", g."id", g."ownerId", 'OWNER'::"GuildMemberRole", g."createdAt"
FROM "Guild" AS g
WHERE EXISTS (SELECT 1 FROM "User" AS u WHERE u."id" = g."ownerId")
  AND NOT EXISTS (
    SELECT 1 FROM "GuildMember" AS m
    WHERE m."guildId" = g."id" AND m."userId" = g."ownerId"
  )
ON CONFLICT ("guildId", "userId") DO NOTHING;
