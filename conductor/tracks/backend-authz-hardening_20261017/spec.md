---
type: spec
---
# Backend Authorization Hardening

## Outcome

Every .NET endpoint that reads private user data or changes state decides
authorization itself, from the verified actor claim, and never from a route or
body `userId`. The Next.js BFF only forwards requests. Holding the shared
`API_KEY` alone no longer lets a caller award XP, verify guilds, or read another
user's email and phone.

## Why now

Loop 1 QA and the adversarial review (2026-10-17) found these issues. The
backend lane fixed the denial response (401/403 envelope, no framework text; see
`ArdaNova.API/Middleware/AGENTS.md` §Authorization denials). The gaps below are
in controllers, services, DTOs and routers that belong to feature lanes, so this
track carries them.

## Findings in scope

1. **Critical. Public PII listing.** `user.getAll`, `user.search` and
   `user.getById` (`ardanova-client/src/server/api/routers/user.ts`) are
   `publicProcedure`s that return the full `UserDto`: email, phone, role and
   trust score. `GET /api/Users`, `/api/Users/search` and `/api/Users/{id}` need
   only the service key.
2. **High. State changes gated only by the service key.** Achievements,
   Activities, Events, Guilds, Leaderboards, Products, Referrals,
   UserExperience, UserSkills, UserStreaks, XPEvents and Enums carry no
   `ActorAssertion` or `AdminApiKey` policy. The ownership checks live only in
   tRPC routers (`guild.ts`, `product.ts`, `xp.ts`, `achievement.ts`), where they
   also have a check-then-act race across two HTTP calls.
3. **Medium. `Enum.Parse` on request values.** These calls throw
   `ArgumentException` on bad input: `GuildServices.cs:519`, `GuildServices.cs:660`
   and `ProjectServices.cs:1326`. They should return `Result.ValidationError`.
   When they do, drop the `ArgumentException` to 400 case from
   `ExceptionHandlingMiddleware`.
4. **Medium. Realtime SSE uses raw `auth()`.** `ardanova-client/src/app/api/realtime/route.ts`
   ignores the DEV_AUTH_BYPASS preview that `trpc.ts` now honours, through
   `authForPage`.
5. **Medium. The BFF rethrows backend error bodies as plain `Error`s.** About 195
   router sites do `throw new Error(response.error)`. The text is now a safe
   envelope, but it is raw JSON, and tRPC maps it to a 500. Map
   `response.status` to `TRPCError` codes in one helper.

## Acceptance

- An anonymous or foreign viewer receives a public profile projection: id, name,
  image, bio, tier and level. Email and phone go only to the subject or an
  admin. An API test asserts this through the HTTP pipeline.
- Every mutating action on the 12 controllers requires `ActorAssertion`. Award,
  verify and leaderboard writes also require `AdminApiKey` or a server-checked
  admin role. Ownership is enforced in Application services.
- A reflection test fails when a mutating action has neither policy, unless the
  action is on an explicit allowlist.
- tRPC routers for these domains contain no ownership logic.
