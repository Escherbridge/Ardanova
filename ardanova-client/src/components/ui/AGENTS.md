---
type: reference
---
# components/ui

Shared design-system primitives. Radius and shadow scales are zeroed in
`src/styles/globals.css`, so `rounded-*` / `shadow-*` classes here render sharp
and flat by design; do not reintroduce per-component radius.

## query-error-state

`QueryErrorState` (`query-error-state.tsx`) is the honest-failure card first
written inline in the chats inbox ("This is a connection or service error, not
an empty inbox"). Use it for any query whose `isError` is true instead of an
empty state or an indefinite "Loading…".

- Copy comes from `describeQueryError` (`query-error-copy.ts`), which maps the
  tRPC `data.code` to plain language. Raw codes such as `UNAUTHORIZED` are
  never shown to users.
- A signed-out failure shows a **Sign in** link back to the current path; a
  retry button would just fail again. Permission, not-found and validation
  failures show no retry for the same reason.
- `shouldRetryQuery` is the matching React Query `retry` predicate. Wire it in
  `src/trpc/query-client.ts` (`defaultOptions.queries.retry`) so auth and
  permission failures surface immediately instead of after three retries plus
  backoff (~8s of "Loading…").

## status-page

`components/brand/status-page.tsx` renders the public-shell header and a
headline block. `app/not-found.tsx`, `app/error.tsx` and `app/global-error.tsx`
use it. `global-error` replaces the root layout, so it imports `globals.css`
itself and uses a plain `<a>` (the client router may be what failed).
