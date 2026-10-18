# API middleware invariants

## Dependency lifetimes

These classes use ASP.NET Core's conventional `UseMiddleware<T>` activation. Middleware constructors are created from the application root provider, so constructor dependencies must be singleton-safe. Resolve scoped services as `InvokeAsync` parameters; ASP.NET Core supplies them from `HttpContext.RequestServices` for the current request.

`ActorAssertionMiddleware` therefore receives `IActorAssertionReplayLedger` in `InvokeAsync`. Moving it back into the constructor prevents startup when scope validation is enabled and risks coupling a request-scoped database context to application lifetime.

## Actor assertions

Actor assertions remain fail-closed and single-use. Validation binds the subject to the HTTP method, exact request target, normalized content type, optional idempotency key, and body digest before the scoped replay ledger atomically consumes the assertion ID. Do not replace the database-backed ledger with process-local replay state in production.

The `ActorAssertion` authorization policy validates the identity as a unit: it
must use the middleware's authentication type, carry the v2 marker, and contain
one nonempty name-identifier subject. Looking up a claim across the whole
principal is unsafe because a principal can contain service and actor
identities simultaneously. Multiple matching actor identities are ambiguous
and therefore denied.

The accepted clock skew is shared with the replay retention policy. Replay rows may be purged only after that policy's longer post-expiry window has elapsed; see `ArdaNova.Infrastructure/Security/AGENTS.md`.

The API middleware test suite includes a scope-validation regression test. Keep that test whenever constructor or invocation dependencies change.

SignalR transports reuse this exact v2 envelope validator and claims identity,
but consume the nonce inside the hub because the framework owns negotiation and
transport requests. Do not restore a plain actor-ID header or fork the payload
format for realtime connections.

## Service API key

`API_KEY` is a service credential, not an actor identity. Runtime authentication
and readiness reject values shorter than 32 UTF-8 bytes and known documentation
sentinels such as `replace-with`, `placeholder`, and `change-me`. The admin and
actor-signing secrets use the same validation. The SignalR hub uses the shared
resolver, strength check, and fixed-time comparison as the HTTP middleware.

Authorization has an authenticated fallback policy, so newly added endpoints
cannot silently omit authentication metadata. Health/readiness are explicitly
anonymous. Stripe webhook ingress and SignalR are also explicit fallback
exceptions because they authenticate with a provider signature and the signed
hub handshake respectively; neither exception makes its downstream actions
anonymous. A service-key principal satisfies only the fallback, not actor or
admin policies.

## Authorization denials

Identity is built by `ApiKeyMiddleware` (service principal, optional `Admin`
role) and `ActorAssertionMiddleware` (added actor identity). Neither is an
ASP.NET authentication handler, so the pipeline registers a denial-only default
scheme (`ArdaNovaDenialAuthenticationHandler`). Its `HandleAuthenticateAsync`
always returns `NoResult`, so the automatic `UseAuthentication()` that
`WebApplication` inserts can never replace the principal. It exists only so that
`ChallengeAsync`/`ForbidAsync` (and therefore every controller `Forbid()`) write
401/403 envelopes. Without it they throw "No authenticationScheme was
specified", which used to surface as a 400 with the framework message.

Status mapping:

- missing/invalid service key: 401 from `ApiKeyMiddleware` (unchanged);
- invalid, replayed or tampered actor assertion: 401 from `ActorAssertionMiddleware` (unchanged);
- a policy that contains `ActorAssertionRequirement` (the `ActorAssertion`
  policy), and the principal has no single valid actor identity
  (`ActorAssertionMiddleware.TryGetActorId`): 401
  (`ArdaNovaAuthorizationResultHandler`), because the caller has not proven who
  it acts for;
- anything else that fails a policy, or a controller `Forbid()`: 403.

The handler recognises the actor policy by requirement *type*, so the policy
must keep using `ActorAssertionRequirement`. Rewriting it as `RequireAssertion`
or `RequireClaim` silently turns every missing-actor 401 into a 403 (this
happened once: the handler looked for a `ClaimsAuthorizationRequirement` that
the policy never contained). `AuthorizationPipelineTests` pins the 401.

There is exactly one authentication scheme (`ArdaNova`). Loop 1 briefly
registered a second denial-only scheme (`StatusCodeAuthenticationHandler`); it
was folded into this one. Do not add another `AddAuthentication(...)` default.

The handler only shapes the response. The allow/deny decision is still made by
the policies in `ArdaNovaAuthorizationExtensions` and by the controllers.
`tests/ArdaNova.API.Tests/Middleware/AuthorizationPipelineTests.cs` sends real
HTTP requests through the production middleware order and pins each outcome.

## Error envelope

Every error the pipeline writes itself (`ApiKeyMiddleware`,
`ActorAssertionMiddleware`, the denial scheme, `ExceptionHandlingMiddleware`,
model validation) goes through `ApiErrorEnvelope` and has the same shape:
`{ "error": string, "statusCode": number, "traceId": string }`. Model-validation
failures add `errors: { field: string[] }` (`ApiErrorEnvelope.AddArdaNovaErrorEnvelope`).
Input-formatter exception text is turned off, so JSON errors no longer leak
DTO namespaces. Controller-authored results (`NotFound(new { error })`, bare `NotFound()`,
`ProblemDetails`, strings) are rewritten into the same envelope by
`Filters/ErrorEnvelopeResultFilter` (see `Filters/AGENTS.md` §Error envelope).

`ExceptionHandlingMiddleware` never echoes exception text. Services report
expected failures through `Result<T>`, so an exception reaching it is a server
fault and becomes a 500. Two cases are exceptions:

- a 4xx `BadHttpRequestException` (Kestrel body or limit faults) keeps its status;
- an `ArgumentException` that is not an `ArgumentNullException` becomes a generic 400.
  This is because some services still call `Enum.Parse` on request values (see the
  `backend-authz-hardening_20261017` track). Remove this case once those calls use `TryParse`.

`InvalidOperationException` is no longer a 400. In this codebase it marks
configuration and invariant failures, which belong in 5xx monitoring. A response
that has already started is rethrown so the server aborts it, instead of being
written over.

## Database faults

`Classify` walks the inner-exception chain for an exception that exposes a
`SqlState` string (Npgsql's `PostgresException`). It reads the property by
reflection, so this file needs no Npgsql reference. The mapping:

- `22xxx` (data exception, e.g. invalid JSON for a `jsonb` column, value too
  long): 400;
- `23505` (unique violation): 409;
- `23503` (foreign key violation) and `23514` (check violation): 400.

Any other state stays a 500. That includes `23502` (not-null violation),
because a missing required value is a server bug, not something the caller
did. Like every other case, the mapped message is fixed and never echoes the
database text. Without this mapping, `POST /api/Achievements` with a
plain-text `criteria` returned an opaque 500.
