# Commerce helpers (`src/lib/commerce`)

## Persisted request keys

`persisted-request-key.ts` is the idempotency-key lifecycle for the two first-party money flows:
add funds (`add-funds-request.ts`, page `app/portfolio/add-funds`) and sponsoring a project
(`sponsorship-request.ts`, page `app/projects/[slug]/invest`). Ported from oasis-sleek
`lib/persisted-request-key.ts`; storage access follows `components/ai/nova-refine-storage.ts`
(an explicit `Storage` parameter, unreadable entries treated as absent).

**The rule.** A saved key is released ONLY on an allowlisted, server-set money code - and the
allowlist is `definite` alone (`releasesSavedRequest`). Anything unknown, missing, transient or
ambiguous KEEPS the key. The tRPC/HTTP status is never consulted: on these routes 400/403/409 each
mean several things. The BFF's `ExceptionHandlingMiddleware` answers an `ArgumentException` or a
SQLSTATE 22xxx/23503/23514 with a 400 that can arrive after the node already moved funds
(`FundingIntentService.CompleteAsync` rolls back and rethrows); clearing on that 400 let Retry mint
a fresh key and debit twice. The BFF emits `definite` only where nothing was, or ever will be,
admitted under the key - see `ardanova-backend-api-mcp/.../Controllers/AGENTS.md` §Money refusal
codes for exactly where.

**How the code reaches the page.** `BaseApiClient.failed` copies the BFF body's `code` onto
`ApiResponse.code`; the money routers throw `moneyRefusalError` (`server/api/money-refusal-error.ts`),
which parks a validated code on the `TRPCError`'s cause; `errorFormatter` (`server/api/trpc.ts`)
exposes it as `error.data.moneyCode`; pages read it with `moneyCodeOf`. A body without a code -
the middleware's post-commit 400 included - yields `moneyCode: null`, which keeps the key.

**Exact bytes, never recomputed.** A `SavedRequest` holds the request key AND the wire body it
covered. Retry and every resume send `saved.wire` verbatim. The invest page used to compare
dollars but send platform units recomputed from the live rate, and `FundingIntentService` binds a
key to `requestedPlatformBaseUnits` - so one rate move after a lost answer made the key a
permanent 409. `sponsorshipRequestFlow.isSameRequest` compares the platform-unit AMOUNT STRING
(plus config and disclosure), the same fields the server binds.

**One slot per subject per flow.** The storage key is
`ardanova:commerce:request:v2:<fnv1a(subject)>:<scope>`, so two people on one device coexist and
a read never erases somebody else's entry. Sign-out (`components/app-sidebar.tsx`) erases the
signing-out subject's entries with `clearSavedRequestsForSubject`. The fingerprint is not a
security boundary; it keeps a raw user id out of the key. `v2` replaced the unreleased `v1`
(ownership inside the value, amount-only payload); `v1` entries are never read.

**Clears are keyed.** `clearSavedRequest` takes the request key the answer was for and does
nothing if a different key is saved now, so a late callback cannot erase a newer attempt.

**Add funds.** The payment id the server returns is saved (`recordAdmittedId`) BEFORE
`window.location.assign`. On every visit the page reads the saved key's own payment
(`addFunds.getPayment`); on a `?intent=` return it polls the returning payment. The key is cleared
only when `endsSavedPayment` holds: the payment read IS the saved one (ids normalised, compact or
dashed) and its outcome is `added` or `went-wrong`. `unconfirmed` (an unrecognised word, `poll:
false`) and `unfinished` keep it. A resumed payment with no page left (already paid or closed) is
returned by the router instead of refused, and the page reads its state rather than looping.

**The way out asks the server first.** The resume banner renders whenever a key is saved
(including on a `?intent=` return, where Stripe's success AND cancel both land) and after any
failure that kept the key. "Start a new payment instead" reads the saved payment (or, when the
answer naming it was lost, replays the saved key on a non-redirecting mutation) and lets the key
go on any outcome the node actually stated - an open, unpaid page charged nothing. On invest there
is no safe "drop": the banner's action is "Resume it", which replays the saved bytes; a success
lands on the status page, a `definite` clears the key, anything else keeps it. A sponsorship that
may already be on its way cannot be cancelled from the browser.

**Session.** Invest's Support, Retry and Resume are disabled until the session resolves; with no
subject there is nowhere to save a key, and a silent no-op reads as a broken button.
