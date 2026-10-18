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

## mutation-error

`describeMutationError(error, action)` and `<MutationErrorNotice>` are the
write-side twin of `describeQueryError`. They exist because a failed tRPC
input check arrives with `error.message` set to the stringified Zod issue array.
Rendering `{mutation.error.message}` is how `/tasks/create` showed users
`[{"received":"URGENT","code":"invalid_enum_value",...}]`.

- Zod failures read `data.zodError.fieldErrors` (shaped by the errorFormatter
  in `server/api/trpc.ts`) and name the fields in plain words: "We couldn't
  create the PBI: check the priority field. Nothing was saved."
- Author-written `TRPCError` messages (FORBIDDEN / CONFLICT / BAD_REQUEST) are
  passed through when they look like prose. JSON, multi-line text and
  stack-like text never are. `INTERNAL_SERVER_ERROR` always gets the generic
  service sentence, because routers re-throw raw .NET `response.error` strings
  there.
- Every sentence ends "Nothing was saved." This is the portfolio page's honest
  failure voice (`app/portfolio/page.tsx`, "This is not a zero balance"), so
  users never have to guess whether a write half-landed.
- `action` completes "We couldn't …", for example "create the PBI", "cast your
  vote" or "request the withdrawal".
- `src/styles/design-gate.test.ts` caps the number of `{…error.message}` JSX
  expressions in `src/`. Migrating a site lowers the ceiling. Adding one fails
  the test.
- Toasts and `alert()` go through the same copy:
  `toast.error(describeMutationError(e, "create the task"))`. Loop-2
  integration migrated all 50 mutation toast/alert sites (including the
  `/tasks/create` PBI path that leaked the `URGENT` Zod dump, both as a toast and
  as the inline `errors.submit`). The one exception is `bid-accept-button.tsx`'s
  client-side navigation `catch`, whose `Error` is authored locally, not a tRPC
  failure. The gate's notifier ratchet (`RAW_ERROR_NOTIFIER_CEILING`) counts
  `toast(...)`/`alert(...)` calls whose arguments contain any `x.message`.

## status-page

`components/brand/status-page.tsx` renders the public-shell header and a
headline block. `app/not-found.tsx`, `app/error.tsx` and `app/global-error.tsx`
use it. `global-error` replaces the root layout, so it imports `globals.css`
itself and uses a plain `<a>` (the client router may be what failed).
