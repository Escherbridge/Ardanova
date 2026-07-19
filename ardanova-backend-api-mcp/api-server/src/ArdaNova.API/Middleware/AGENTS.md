# API middleware invariants

## Dependency lifetimes

These classes use ASP.NET Core's conventional `UseMiddleware<T>` activation. Middleware constructors are created from the application root provider, so constructor dependencies must be singleton-safe. Resolve scoped services as `InvokeAsync` parameters; ASP.NET Core supplies them from `HttpContext.RequestServices` for the current request.

`ActorAssertionMiddleware` therefore receives `IActorAssertionReplayLedger` in `InvokeAsync`. Moving it back into the constructor prevents startup when scope validation is enabled and risks coupling a request-scoped database context to application lifetime.

## Actor assertions

Actor assertions remain fail-closed and single-use. Validation binds the subject to the HTTP method, exact request target, normalized content type, optional idempotency key, and body digest before the scoped replay ledger atomically consumes the assertion ID. Do not replace the database-backed ledger with process-local replay state in production.

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
