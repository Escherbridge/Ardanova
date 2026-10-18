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
