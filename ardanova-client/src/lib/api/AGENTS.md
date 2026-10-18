# API client (`src/lib/api`)

Server-only client for the .NET API. Every request carries the server API key, so whatever URL the
client builds is fetched with server authority. Endpoint-specific notes live in
`ardanova/endpoints/AGENTS.md`.

## Path safety

`path.ts` is the only way an endpoint puts a caller-supplied value into a URL.

- **`` apiPath`/api/users/${id}/skills` ``** (tagged template). Each value before the first `?` in
  the literal text goes through `segment()`; each value after it is `encodeURIComponent`-ed as a
  query value; a `URLSearchParams` value is spliced in as a whole query string (`?` or `&` added as
  needed, nothing when empty).
- **`segment(value)`** = `encodeURIComponent(String(value))`, and throws `InvalidApiPathError` for
  `""`, `.` and `..`. Encoding turns `/`, `?`, `&`, `#`, `%` into data, so an id can no longer
  change which route or which query parameters the server sees; the reject list covers the values
  that encoding leaves meaningful (an empty segment collapses the route, dot segments traverse).
- **`query({...})`** builds a `URLSearchParams`, dropping `undefined`/`null`. Optional filters use
  `query({ status: status || undefined })` so empty strings stay off the wire as before.

Why central: in loop 2 ids such as `paged?page=1&pageSize=3` or `../users/paged?x=` sent to
`/api/users/${id}` let any caller drive arbitrary .NET GETs with the server key. Router-level zod
regexes (`recordIdSchema` in `server/api/routers/user.ts`) stay as defence in depth, but the client
is safe even when a router forgets one.

Rules:

- Never write a raw `` `/api/...${x}` `` template or call `encodeURIComponent` in an endpoint file —
  `path.test.ts` fails the build on either (it would double-encode inside `apiPath`).
- Plain string literals without values (`"/api/TokenBalance/me/arda"`) need no tag.
- `InvalidApiPathError` thrown inside a tRPC procedure is mapped to `BAD_REQUEST` by
  `invalidApiPathMiddleware` in `server/api/trpc.ts`; elsewhere (route handlers) it surfaces as a
  thrown error, i.e. a 500 with no request sent.
- Routers that call `apiClient.get/post` directly (governance, payouts, project, project-tokens,
  swap) use the same tag.

## Failure trail

About 195 router sites end in `throw new Error(response.error)` (or a `TRPCError` with code
`INTERNAL_SERVER_ERROR` and the same message). That turned every API 403/404 into a tRPC 500, and
the API now answers 403/404 much more often: resource guards, event visibility and referral
participants. Rewriting every router belongs to the feature lanes, so the status is restored
centrally instead:

- `failure-trail.ts` holds an `AsyncLocalStorage` list per procedure. `BaseApiClient` records
  every non-2xx answer as `{ status, message }`, where the message is the one `readApiError`
  produced.
- `apiStatusMiddleware` (`server/api/trpc.ts`, on `publicProcedure` and `protectedProcedure`)
  runs the procedure inside a trail. If the procedure fails with `INTERNAL_SERVER_ERROR` **and
  its message equals a recorded API failure's message word for word**, it rethrows with that
  failure's code: 400, 401, 403, 404, 409 or 429. Anything else stays a 500.

The exact-message match is the safety rule. A router that wrote its own message, or a genuine bug
thrown after an unrelated 404, is never relabelled. Routers that already throw a typed
`TRPCError` are untouched. Proper per-router mapping is still owed (track
`backend-api-boundary-completion_20261018`); this middleware is the floor, not the fix.
