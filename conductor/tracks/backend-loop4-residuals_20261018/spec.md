---
type: spec
---
# Backend loop-4 residuals

## Context

The loop 4 backend fix lane (branch `loop4/backend`) closed these adversarial
QA findings in the API pipeline. The rationale for each is in
`ArdaNova.API/Filters/AGENTS.md`, `ArdaNova.Application/Validators/AGENTS.md`,
`ArdaNova.API/Readiness/AGENTS.md` and `ardanova-client/src/AGENTS.md`.

- **`Events.Create` affiliation.** A `projectId` or `guildId` on a new event
  now requires the organizer to be the creator or owner, or a member, of that
  project or guild. It answers 403 otherwise, and 404 for an unknown id. This
  is done with optional `ResourceGuard`s.
- **Search totals.** Hidden events no longer count toward a page's
  `totalCount`, which closes the one-character-at-a-time oracle on invite-only
  titles.
- **Member emails.** `ProjectMemberDto.User.Email` is redacted unless the
  viewer is that member or an administrator.
- **Validators.** These now exist for guild create/update, user update, group
  conversations and notifications. They refuse `javascript:` links and
  malformed emails.
- **User dumps.** `Users.GetAll` and `Users.GetByEmail` need the admin key
  *and* a signed administrator.
- **Error envelope.** Unmatched routes and 405s return the shared envelope.
- **`/ready`.** Anonymous `/ready` reuses its verdict for 5 s.
- **Next.js headers.** Next.js sends nosniff, frame-deny, referrer and
  permissions headers, plus HSTS in production.
- **Dead code.** The dead multipart upload path in the API client was removed.

The items below were out of this lane's ownership or too large for the final
loop.

## Items

1. **Event capacity race (events lane, high).** In `EventService.RegisterAsync`
   the capacity check and the counter update can be interleaved by concurrent
   requests (read `attendeesCount`, compare, `+1` in memory, save). QA overbooked
   a `maxAttendees=1` event in 4 of 5 runs, and `attendeesCount` drifted from the
   number of attendee rows. Fix: a conditional
   `UPDATE … SET attendeesCount = attendeesCount + 1 WHERE id = @id AND (maxAttendees IS NULL OR attendeesCount < maxAttendees)`
   that checks the affected-row count inside the registration transaction, or
   derive the count from `COUNT(*)`. Unregister needs the symmetric decrement.
   Add a concurrent-registration flow test that asserts attendees never exceed
   `maxAttendees`.
2. **Query-level event visibility (events lane).** This supersedes
   `backend-api-boundary-completion_20261018` item 2. Pass the viewer id into
   `SearchAsync`, `GetPagedAsync` and the list reads, and filter with an EF
   predicate built from `EventVisibilityPolicy` before `Count` and `Skip/Take`.
   The pipeline filter's total adjustment only covers hidden rows on the
   current page.
3. **Anonymous rate limiting at the edge.** Same as boundary-completion item 6.
   Requests without an actor are still unlimited in the API, because every
   anonymous visitor shares the BFF's service key. Add an IP-keyed limit in
   Next.js middleware or the proxy, plus a shared store for multiple replicas.
   `/ready` no longer amplifies these requests into database load.
4. **Least-privilege keys.** Same as boundary-completion item 7. Also give the
   admin key its own audit trail. Loop 4 closed only the PII-dump routes. The
   other `AdminApiKey`-only surfaces (Treasury, ProjectTokens, Users.Create,
   Guilds verify) still accept the key with no actor.
5. **Script/style CSP with nonces (client).** Only `frame-ancestors` ships
   today. A full CSP needs nonce plumbing through the root layout for Next's
   inline bootstrap.
6. **Validator coverage for the remaining body DTOs.** These include project
   create/update, opportunities and bids, proposals, tasks and KYC `FileUrl`.
   Each one is a new `RequestValidator<T>` class with no controller edit.
   Inventory them with
   `grep -n "public record \(Create\|Update\)" Application/DTOs/*.cs`.
7. **One nested-user projection.** `UserDirectoryPrivacyFilter` handles
   `UserDto`, `AttachmentWithUserDto` and `ProjectMemberDto` one shape at a
   time. Introduce a shared "user as others see them" projection that every
   nested `*UserDto` uses (project invitations and opportunity applicants carry
   emails too), so a new DTO cannot leak by omission.
8. **QA harness secrets (orchestrator).** The loop QA scripts under
   `.omc/qa-harness/` hardcode the local service, admin and HMAC keys. Read them
   from the environment instead.
9. **CI `generate:csharp` versus hand-written entity code.** Carried over from
   `backend-ownership-enforcement_20261017`. The generator must emit partial
   classes, or skip hand-maintained files, so the contract gate can run on
   every PR.

## Acceptance

- Concurrent registration never exceeds `maxAttendees`, and `attendeesCount`
  equals the number of attending rows (flow test plus a live re-run of the QA
  `p6.mjs` race).
- `GET /api/Events/search` and `/paged` return totals and page sizes that
  count only rows the viewer can see.
- Anonymous floods are answered with 429 at the edge, and signed-in actors are
  unaffected.
- Every create/update DTO that has a URL, an email or free text has a validator
  (a reflection test lists the uncovered ones and fails).
