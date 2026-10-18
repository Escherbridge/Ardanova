# API filters and conventions

`ArdaNovaMvcPolicies.AddArdaNovaEndpointPolicies` (called from `Program.cs`)
installs everything in this directory: one application-model convention and
three global filters. They are cross-cutting rules that apply to every
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
  (`AdminApiKey`), because they are enumeration and identity-lookup surfaces.
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

Not covered here; these need a lookup and belong in services (see
`conductor/tracks/backend-ownership-enforcement_20261017`):

- event update/delete ownership (still enforced only in `routers/event.ts`);
- product ownership against its project;
- `Referrals.GetById`.

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

Nested `UserDto`s (for example `AttachmentDto.UploadedBy`) are not projected yet.

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
