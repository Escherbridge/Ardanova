---
type: spec
---
# Backend ownership enforcement and generator preservation

## Context

Loop 2 (branch `loop2/backend`) put the following into the API pipeline
(`ArdaNova.API/Filters`, see its `AGENTS.md`):

- fail-closed actor gating for every unannotated mutation;
- admin gating for gamification administration;
- actor-subject binding for body- and route-named users;
- public projection of `UserDto`;
- paging bounds;
- one error envelope.

That closes `backend-authz-hardening_20261017` §1 (PII on reads) and §2
(service-key-only mutations) at the HTTP edge. What remains needs either a
record lookup inside a service, or files owned by other lanes.

## Findings still open

1. **Event update/delete ownership exists only in the BFF**
   (`routers/event.ts:236-310`). `PUT/DELETE /api/Events/{id}` now require an
   actor, but any actor may edit any event. `EventService.UpdateAsync` and
   `DeleteAsync` must take the actor id and compare it with `organizerId`
   (admins excepted). Then remove the router check.
2. **Product ownership.** `POST /api/Products` accepts any `projectId`. The
   create, update, delete and toggle actions need a project-membership check
   (`IHierarchyAuthorizationService`).
3. **`GET /api/Referrals/{id}`** returns any referral to any caller. It should
   be limited to the referrer, the referred user, or an admin.
4. **Nested `UserDto`** in `AttachmentDto.UploadedBy` still carries email and
   phone. Project it to a public DTO in the Application mapping.
5. **BFF error mapping.** About 195 router sites `throw new Error(response.error)`,
   so a backend 404 or 403 reaches the browser as a tRPC 500. The fix is one
   helper (in `lib/api/base-client.ts` or a router util) that maps
   `response.status` to a `TRPCError` code.
6. **`app/api/sdk/me/route.ts`** calls `users.getById` without an actor, so it
   now receives the public projection (no email). Run it under
   `runWithActorAssertion` for the session user.
7. **`Enum.Parse` on request values** (GuildServices, ProjectServices:1702).
   Switch to `TryParse` and return `ValidationError`, then delete the
   `ArgumentException → 400` arm in `ExceptionHandlingMiddleware`.
8. **The C# generator erases hand-written entity code.** `generate:csharp`
   rewrites every entity file whole. It drops class and property doc-comments,
   constants, `[NotMapped]` members and nested helper types. Examples:
   `PayoutRequest.ExchangeIdempotencyKey`, `Project`'s `ProjectFundingUnits`,
   the doc on `OpportunityBid.guildAwardSplit`, and enum members in
   `EconomicSettlementKind` and `FundingIntentStatus`. As a result, the CI step
   that checks generated artifacts are committed fails on a clean checkout.
   Loop 2 stopped the generator from *deleting* hand-written files
   (`PostVisibilityPolicy.cs`): only files that map a `[Table(...)]` are
   treated as stale. What remains:
   - Emit `public partial class <Table>`, and move every hand-written member
     into `Entities/Partials/<Table>.cs`. The finance entities are owned by the
     finance lane.
   - Carry property doc-comments from DBML `note:` so they survive
     regeneration.
   - For enums with members that exist only in C#, add those members to the
     DBML (the source of truth).
   - Re-run the CI check locally and confirm a zero diff.

## Acceptance

- An actor who is not the organizer gets 403 from `PUT/DELETE /api/Events/{id}`.
  An actor who does not belong to a project gets 403 from
  `POST/PUT/DELETE /api/Products...` for that project. Both are proven with
  pipeline tests in `ArdaNova.API.Tests`.
- Running `npm run generate:dbml && npm run generate:csharp` on a clean
  checkout leaves `git status` empty for the paths the CI step checks.
- A backend 404 reaches the browser as tRPC `NOT_FOUND`, not
  `INTERNAL_SERVER_ERROR`.
