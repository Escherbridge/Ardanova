# API filters and conventions

`ArdaNovaMvcPolicies.AddArdaNovaEndpointPolicies` (called from `Program.cs`)
installs everything in this directory: one application-model convention and
six global filters (paging, request validation, user privacy, event
visibility, referral disclosure, error envelope). They are cross-cutting rules that apply to every
controller, so a feature controller cannot leave them out.

## Endpoint authorization

`EndpointAuthorizationConvention` runs once at startup over the controller model.

- **Fail-closed mutations.** Every `POST`/`PUT`/`PATCH`/`DELETE` action that
  declares no `[Authorize]`/`[AllowAnonymous]` (on the action or its
  controller) gets the `ActorAssertion` policy. The service key alone proves
  only that the BFF is calling, not on whose behalf. The only exceptions are
  listed in `ServiceKeyMutations`: reads that use `POST` because of their body
  shape (`ProjectTokens.GetMetadata`). Before this convention, 8 controllers
  (Achievements, Activities, Events, Leaderboards, Products, Referrals,
  UserStreaks, XPEvents) wrote with the service key only. QA granted real XP
  that way.
- **Policy overrides.** `PolicyOverrides` adds a stricter policy to named
  actions. These are added on top of anything the controller declares, and
  ASP.NET combines multiple policies with AND. Gamification administration
  (achievements, leaderboards, XP awards, referral state transitions, streak
  create/delete, activity delete) requires `ActorAdministrator`: a verified
  actor whose signed role is `ADMIN`, which the BFF's `adminProcedure` sends.
  The unpaged user list and the email-to-user lookup require the admin key
  (`AdminApiKey`) *and* a signed administrator (`ActorAdministrator`), because
  they return every user's email, phone and trust score. Loop 4 QA showed the
  admin key alone was enough; now a leaked key without an admin's signed
  assertion gets 401. Neither route has a BFF caller (the client's user list
  uses `/api/users/paged`), so nothing legitimate changed.
- **Subject bindings.** `SubjectBindings` names the parameter, or the body
  property, that holds the user the request acts for (event organizer,
  attendee, activity owner, referrer, streak owner, referral reader).
  `ActorSubjectBindingFilter` returns 403 unless that value equals the verified
  actor id, or the caller is an administrator. The BFF already sends
  `ctx.session.user.id` in every one of these fields, so a legitimate call
  never changes. A forged call (QA created an Event, an Activity and a Referral
  for another user) is refused before the action runs.
  `ActorSubjectBinding.EnsureBindsTo` throws at startup if a rename leaves a
  binding pointing at a missing parameter or property, so a rename cannot
  silently turn the check off.

Policies are added as `AuthorizeAttribute` endpoint metadata on each selector.
Do not add them to `action.Filters`: with endpoint routing,
`UseAuthorization` reads only endpoint metadata.

`Products.Create` is bound to `dto.UserId` (loop 3): QA created a product
attributed to a second real user with nothing linking the two.

## Resource guards

`ResourceGuards` maps an action to one or more `ResourceGuard(rule, record)`
entries, each added as its own filter in list order. The record
is read exactly like a subject binding (parameter, or a body property) and is
checked the same way at startup by `EnsureBindsTo`. `ResourceAuthorizationFilter`
then asks `IResourceAuthority` (Application/Common/Security) about that one
row, before the feature service runs:

| Rule | Actions | Allowed |
| --- | --- | --- |
| `EventOrganizer` | `Events.Update`, `Events.Delete` | the organizer |
| `EventViewer` | `Events.GetById`, `Events.GetAttendees`, `Events.Register` | anyone who may see the event (anonymous allowed) |
| `ProjectCatalogManager` | `Products.Create` (`dto.ProjectId`) | project creator, or a FOUNDER/LEADER/CORE_CONTRIBUTOR member |
| `ProductManager` | `Products.Update`, `Products.Delete`, `Products.ToggleActive` | a manager of the product's project |
| `ReferralParticipant` | `Referrals.GetById` | the referrer or the referred user |
| `ProjectAffiliate` (optional) | `Events.Create` (`dto.ProjectId`) | the project creator or any member |
| `GuildAffiliate` (optional) | `Events.Create` (`dto.GuildId`) | the guild owner or any member |

An `Optional` guard is skipped when the request leaves its record empty, so an
event with no project or guild needs no lookup. Loop 4 QA created events,
including `PROJECT_MEMBERS` ones with a meeting link, under a project and a
guild the actor did not belong to; those events then appeared to the real
members under the community's name. "Any member" (not just managers) is the
same set that may *see* a `PROJECT_MEMBERS`/`GUILD_MEMBERS` event, so anyone
who can read a community's events may also host one.

Outcomes: no actor where one is required gives 401; an unknown id gives 404;
a known row the actor may not touch gives 403. An event the viewer may not see
gives 404, not 403, so invite-only events cannot be discovered by probing ids.
Administrators (the admin key, or an actor with role `ADMIN`) skip the lookup.

Why a pipeline filter and not the services: the services belong to the feature
lanes, and QA showed (loop 3, live) that any actor could rewrite or delete any
event or product. The BFF's GET-then-mutate checks in `routers/event.ts` and
`routers/product.ts` are now redundant: they are a time-of-check/time-of-use
race and stricter than the API (only the project *creator* may manage
products). Deleting them belongs to the events and commerce lanes (track
`backend-api-boundary-completion_20261018`). The rows are re-read at
check time, so a guard is only as fresh as the database; it is not a lock.

`Events.Register` is guarded too. Without it, anyone who learned an
invite-only event id could register and then pass the visibility rule as an
attendee.

## Event visibility

`EventVisibilityFilter` applies `EventVisibilityPolicy` to every `EventDto`,
`IEnumerable<EventDto>` and `PagedResult<EventDto>` response:

- `PUBLIC` events: everyone;
- `PROJECT_MEMBERS`: members or the creator of `projectId`;
- `GUILD_MEMBERS`: members or the owner of `guildId`;
- `INVITE_ONLY`: the organizer, co-hosts, and anyone with an RSVP row (including `INVITED`).

`meetingUrl` is blanked unless the viewer is a host or an attendee with status
`GOING`/`MAYBE`/`ATTENDED`. A single hidden event becomes a 404 envelope. In
lists, hidden rows are dropped. In a `PagedResult`, `totalCount` drops by the
number of hidden rows on that page. Before loop 4 it kept the repository's
count, and because search matches title and description,
`search?searchTerm=…` returned `{items: [], totalCount: 1}` for a hidden
`INVITE_ONLY` event. That let anyone recover its text one character at a time.
What remains: a hidden row on *another* page still counts toward that page's
total, and pages can still come back short. Only filtering at the query closes
that, and it belongs to the events lane (track
`backend-loop4-residuals_20261018`). The viewer's relations are loaded once per
response with id-keyed queries, so a list costs a fixed number of lookups, not
one per row.

## Referral disclosure

`ReferralDisclosureFilter` projects any `ReferralDto` (single or list) to
`ReferralCodeView { referralCode, status }` unless the viewer is the referrer,
the referred user or an administrator. `GET /api/Referrals/code/{code}` is a
public tRPC procedure, and it used to return both user ids, the reward and
timestamps to anonymous callers.

## Request validation

`RequestValidationFilter` runs the `IRequestValidator` registered for each bound
argument's type (`Application/Validators`, see the AGENTS.md there). Failures
answer 400 with the same `{ error, statusCode, traceId, errors }` shape as model
validation. The filter is registered with `Order = 100`
(`ArdaNovaMvcPolicies.AfterAuthorizationFilters`), so every subject binding and
resource guard (order 0, any scope) runs first. An invalid body from a caller
who may not touch the record still gets the 403, not a 400 that confirms the
record exists.

## UTC timestamps

`Program.cs` turns on `Npgsql.EnableLegacyTimestampBehavior`, so
`timestamp without time zone` columns come back as `DateTimeKind.Unspecified`.
System.Text.Json wrote those without an offset, and the client's
`new Date(...)` read them as local time, which shifted every event by the
viewer's UTC offset. `ArdaNovaJson.Configure` (used by `Program.cs` and the
pipeline tests) installs `UtcDateTimeJsonConverter`:

- every `DateTime` is written as UTC with a `Z` suffix;
- every incoming timestamp is normalised to its UTC instant. An offset is
  honoured; a value with no offset is taken as UTC.

`DateTime?` gets the same treatment automatically. SignalR payloads use their
own serializer options and are not covered yet.

Tests: `tests/ArdaNova.API.Tests/Filters/ResourceGuardPipelineTests.cs` seeds
rows behind list-backed repositories and drives Events, Products and Referrals
through the production order: ownership, visibility, meeting links, referral
disclosure, validation, UTC output and the per-actor rate limit.

Tests: `tests/ArdaNova.API.Tests/Filters/EndpointPolicyPipelineTests.cs`
drives the real controllers through the production middleware order. It also
asserts that every mutating endpoint carries authorization metadata, and that
every override and binding key matches a loaded action.

## User directory privacy

`UserDirectoryPrivacyFilter` projects any top-level `UserDto`,
`IEnumerable<UserDto>` or `PagedResult<UserDto>` response to `PublicUserView`.
The projection drops `email`, `emailVerified`, `phone`, `role` and
`trustScore`, the same field list as the BFF's `toPublicUser`. The full record
is returned only when the viewer is that user (verified actor id equals
`user.Id`) or an administrator (admin key, or an actor with role `ADMIN`).

Two related changes make this work:

- `trpc.ts` `publicProcedure` signs an actor assertion whenever a session
  exists, so a signed-in user's own `user.getById` still returns their email;
- previously `profile.getProfileById` returned every user's full record, and
  this filter closes that too.

The only nested full `UserDto` in the DTO layer, `AttachmentWithUserDto.UploadedBy`,
is redacted in place: `email`, `emailVerified` and `phone` are nulled, and
`trustScore` is zeroed, unless the viewer is the uploader or an administrator.
The property is typed `UserDto`, so the field names stay and only the values are
blanked; `role` is kept. No controller returns this DTO today, so this is
defence in depth.

`ProjectMemberDto.User.Email` is deliberately *not* handled here. Whether a
viewer may see it depends on the project (a manager needs the address of the
people they manage), and this filter has no project context. The single rule
lives in `ProjectsController` (Controllers/AGENTS.md §Member disclosure). The
loop 4 integration removed a filter branch that nulled the email for everyone
but the member: it ran after the controller and hid emails from managers too.

## Paging bounds

`PagingArgumentFilter` clamps bound `int` arguments named
`page`/`pageSize`/`limit` (case-insensitive) using
`Application/Common/Results/PageRequest`:

- `page ≥ 1`;
- `1 ≤ pageSize ≤ 100`, and `limit` is clamped the same way;
- `page` is capped so that `(page - 1) * pageSize` cannot overflow an int.

Negative values, `int.MaxValue`, and `pageSize=0` (which used to report
`totalPages = 2147483647` and `hasNextPage = true`) all return 200. The
repository and `PagedResult` apply the same bounds a second time, as defence in
depth. See `ArdaNova.Infrastructure/Repositories/AGENTS.md`.

## Error envelope

`ErrorEnvelopeResultFilter` (an always-run result filter) rewrites every
controller-authored 4xx/5xx result into the middleware's
`{ error, statusCode, traceId }` envelope. Before it, the API had three shapes:
`{ error }`, the envelope, and RFC 7807 `problem+json`. The filter handles:

- `{ error }` anonymous objects;
- dictionaries;
- strings;
- `ProblemDetails` (including the ones `[ApiController]` produces for bare
  `NotFound()`);
- bare `StatusCodeResult`s.

Extra fields a controller adds (for example `code`) are kept. `ForbidResult`
and `ChallengeResult` are left alone, because the denial scheme writes those.

Responses that never reach a controller (an unmatched route gives 404, a
known route with the wrong verb gives 405) come back with no body from
routing. `UseArdaNovaStatusEnvelope` (Middleware/ApiErrorEnvelope.cs, wired
right after `UseExceptionHandling`) is `UseStatusCodePages`. It writes the same
envelope, using `DefaultMessage`, but only when the response has no body yet.

For 5xx results, only a 501 or 503 keeps its message. Those are the
controllers' authored "not available" texts. Every other 5xx (500, 502, 504)
is rewritten to the opaque `ExceptionHandlingMiddleware.ServerErrorMessage`,
because a service that catches an exception can put `ex.Message` (connection
strings, SQL, hostnames) into its result. Infrastructure storage and Algorand
services no longer append `ex.Message` to their `Result` failures; they log
it.
