---
type: spec
---
# Backend API boundary completion (after loop 3)

## Context

Loop 3 (branch `loop3/backend`) moved record-level authorization into the .NET
API pipeline, which the feature services cannot bypass. The details are in
`ArdaNova.API/Filters/AGENTS.md` and `ArdaNova.Application/Common/AGENTS.md`.

- **Resource guards.** Only the organizer may update or delete an event. Only
  project managers may create, update, delete or toggle products, and
  `Products.Create` is also bound to `dto.UserId`. Only participants may read
  `Referrals.GetById`. An event the viewer may not see answers 404 by id and
  for its attendee list, and cannot be registered for.
- **Event visibility.** It is applied to every `EventDto` response, and the
  meeting link is shown only to hosts and attendees.
- **Referral code lookups** return only `{ referralCode, status }` to anyone
  outside the referral.
- **Request validators** cover Event, Product and Referral create/update.
- **Timestamps** are written as UTC with a `Z` suffix.
- **5xx bodies** are opaque, except authored 501/503 messages.
- **Rate limit.** Each verified actor has a request budget per minute.
- **`/ready`** details require the service key.
- **CI** fails on unsuppressed high or critical NuGet advisories.
- **BFF status mapping.** The BFF keeps the API's 4xx status when a router
  rethrows the API's message (`lib/api/failure-trail.ts`).

This track covers what remains. Each item needs files owned by a feature lane,
an operator, or a design decision.

## Items

1. **Thin the event and product routers (events lane, commerce lane).**
   - `routers/event.ts` (update, delete) and `routers/product.ts` (create,
     update, delete, toggleActive) still GET the record and compare ids before
     mutating. This is a TOCTOU race, and it is now redundant.
   - The product check is also stricter than the API: it requires the project
     *creator*, while the API allows FOUNDER, LEADER and CORE_CONTRIBUTOR
     members. Delete the pre-checks so the API is the single rule.
2. **Query-level event visibility (events lane).**
   - `EventService.GetPagedAsync`, `SearchAsync`, `GetUpcomingAsync`,
     `GetByOrganizerIdAsync` and `GetRegisteredByUserIdAsync` should take a
     nullable viewer id and filter in the repository query.
   - Until then, the pipeline filter drops hidden rows after paging, so pages
     can be short and `totalCount` counts hidden rows.
3. **Update-time date coherence (events lane).** `EventService.UpdateAsync`
   should reject an `endDate` earlier than the stored `startDate` when only one
   of the two is sent. The validator cannot see the stored row.
4. **Per-router error mapping (all lanes).** Replace
   `throw new Error(response.error)` with typed `TRPCError`s. The failure-trail
   middleware is a floor: it maps only messages rethrown verbatim.
5. **Enforced upload size (attachments lane + infrastructure).** Move S3
   uploads to a presigned `POST` policy with `content-length-range`. A
   presigned `PUT` cannot bound the bytes uploaded.
6. **Anonymous rate limiting at the edge.** The API limits only verified
   actors, because every anonymous call shares the BFF's key. Add an IP-keyed
   limiter in `src/middleware.ts` or at the proxy, and make the API limiter
   distributed if the API scales past one replica.
7. **Least-privilege key for anonymous reads (decision needed).**
   - A read-only API key (GET/HEAD only) for public procedures without a
     session would limit what a BFF code-path bug can do.
   - It adds little while both keys live in the same Next.js process. Decide
     whether the threat model (for example, a separate edge renderer) justifies
     it before building.
8. **Referral codes (gamification lane).** Generate codes server-side with at
   least 64 bits of entropy instead of accepting caller-chosen strings.
9. **SignalR timestamp contract.** Hub payloads use their own serializer
   options and do not yet use `UtcDateTimeJsonConverter`.
10. **Still open from `backend-ownership-enforcement_20261017`.** These are
    unchanged and are referenced, not duplicated, here:
    - `app/api/sdk/me` actor assertion;
    - realtime SSE raw `auth()`;
    - `Enum.Parse` in Guild and Project services;
    - C# generator partial classes for the CI contract gate.
11. **`azoa-sdk` pin (operator).** `verify-azoa-sdk-pin.mjs` fails on `main`:
    `package.json` asks for `^0.4.0`, the lockfile has `0.1.0`, and the local
    link is `0.5.0`. It is blocked on publishing `azoa-sdk@0.4.0` from
    `oasis-sleek/sdk/azoa-wallet`.

## Acceptance

- QA group `D-event-ownership-carryover`, `F-product-ownership-carryover`,
  `E-referral-idor-carryover` and `C-mass-assignment-create` (Products row) pass
  live (expected already after the loop-3 merge).
- An anonymous `GET /api/Events/upcoming` never lists an `INVITE_ONLY` event,
  and `totalCount` matches the visible rows (item 2).
- `git grep "existing.data.organizerId !== userId"` and
  `"project.data.createdById !== userId"` under `ardanova-client/src/server/api/routers`
  return nothing (item 1).
