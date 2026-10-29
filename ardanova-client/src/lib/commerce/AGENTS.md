# Commerce helpers (`src/lib/commerce`)

## Persisted request keys

`persisted-request-key.ts` is the idempotency-key lifecycle for the two first-party money flows:
add funds (`add-funds-request.ts`, page `app/portfolio/add-funds`) and sponsoring a project
(`sponsorship-request.ts`, page `app/projects/[slug]/invest`). Ported from oasis-sleek
`lib/persisted-request-key.ts`; storage access follows `components/ai/nova-refine-storage.ts`
(an explicit `Storage` parameter, unreadable entries treated as absent).

**The rule** (memory: money-key-doctrine). A saved key is released only when the server PROVES
nothing was or can be paid under it - and in the browser that proof is exactly one answer: a 200
`voided` from the void route (`voidSavedRequest`, the single release path). No money code releases
a key on its own. The tRPC/HTTP status is never consulted: on these routes 400/403/409 each mean
several things, and the BFF's `ExceptionHandlingMiddleware` answers an `ArgumentException` or a
SQLSTATE 22xxx/23503/23514 with a code-less 400 that can arrive after the node already moved funds.

- `definite` and `finished` LOOK final (`looksFinal`). Without a server id to read, the page sends
  them to void-or-reveal and acts on its answer: `voided` releases, `revealed` keeps the key and
  follows what the server found. `finished` exists because the node sends INTENT_FINISHED for PAID
  intents too, and REQUEST_KEY_IN_USE names a request that may have been paid (round-3 C-1).
- Everything else - `unconfirmed`, a needs-review hold, a missing or unknown code, a network drop -
  keeps the key and asks for nothing.
- A failed void (any non-200) keeps the key whatever it says; the BFF also downgrades a failed
  void's code to `unconfirmed` (`MoneyRefusal.ForFailedVoid`), so nothing final-looking leaks out.

**Void-or-reveal routes** (BFF, actor-scoped): `POST /api/add-funds/requests/{key}/void` ->
`{ outcome: "voided" } | { outcome: "revealed", payment }` (proxies the node's own void, which
expires an unpaid Stripe page first); `POST /api/funding-intents/requests/{key}/void` ->
`{ outcome: "voided", intentId } | { outcome: "revealed", intentId, intent }` (BFF-owned; decision
table in `ardanova-backend-api-mcp/.../Services/Implementations/AGENTS.md` §Sponsorship void). The
tRPC proxies are `addFunds.voidRequest` and `fundingIntent.voidRequest`; both validate the 200
body, and an outcome word they do not know throws with no money code (keeps the key).

**How the code reaches the page.** `BaseApiClient.failed` copies the BFF body's `code` onto
`ApiResponse.code`; the money routers throw `moneyRefusalError` (`server/api/money-refusal-error.ts`),
which parks a validated code on the `TRPCError`'s cause; `errorFormatter` (`server/api/trpc.ts`)
exposes it as `error.data.moneyCode`; pages read it with `moneyCodeOf`. A body without a code -
the middleware's post-commit 400 included - yields `moneyCode: null`, which keeps the key.

**Exact bytes, never recomputed.** A `SavedRequest` holds the request key AND the wire body it
covered. Try again and every resume send `saved.wire` verbatim. The invest page used to compare
dollars but send platform units recomputed from the live rate, and `FundingIntentService` binds a
key to `requestedPlatformBaseUnits` - so one rate move after a lost answer made the key a
permanent 409. `sponsorshipRequestFlow.isSameRequest` compares the platform-unit AMOUNT STRING,
config and disclosure (what the server binds) AND `displayNameConsent` (L-3): the server would
replay the FIRST send's naming choice, so a changed choice is a different request, never a resume.

**One slot per subject per flow.** The storage key is
`ardanova:commerce:request:v2:<fnv1a(subject)>:<scope>`, so two people on one device coexist and
a read never erases somebody else's entry. Sign-out (`components/app-sidebar.tsx`) runs
`clearResolvedRequestsForSubject`, which KEEPS every readable entry (M-6): a readable entry is
unresolved by construction (every final answer clears it), it may still cover a payment in flight,
and it is useless to anyone else - another subject reads another slot, and the server binds the key
to this person. Only unreadable leftovers are dropped. The fingerprint is not a security boundary;
it keeps a raw user id out of the key. `v2` replaced the unreleased `v1`; `v1` entries are never read.

**Clears are keyed.** `clearSavedRequest` takes the request key the answer was for and does
nothing if a different key is saved now, so a late callback cannot erase a newer attempt.

**Add funds** (`add-funds-request.ts`). The payment id the server returns is saved
(`recordAdmittedId`) BEFORE `window.location.assign`. On every visit the page reads the saved key's
own payment (`addFunds.getPayment`; a return polls briskly, a saved payment slowly); the key is
cleared by STATE only when `endsSavedPayment` holds - the read IS the saved payment (ids normalised)
and its outcome is `added` or `went-wrong`.
- `settleAddFundsFailure` (C-1): once the key names a payment, a failure code is ignored and that
  payment is READ; with none, a final-looking code goes to void-or-reveal; a reveal records the
  revealed payment as the key's own.
- `savedPaymentLocksContinue`: Continue stays disabled while the saved payment has no settled view
  - not read, unreadable, unconfirmed, or paid and still landing. `unfinished` is the exception:
  Continue resumes that same open page under the same key.
- "Start a new payment instead" (`setAsideSavedPayment`, M-1/L-2) re-reads storage, then voids:
  `voided` (the node expires an open page first) releases, `revealed` keeps and shows that payment,
  anything else keeps. It settles on the void's own answer, never on a read's code.
- Copy (M-4): router fallbacks come from `addFundsFallbackMessage(moneyCode)`; only
  `payments_not_configured` and `definite` may say nothing was or can be charged.

**Invest** (`sponsorship-request.ts`, H-1). The banner (and step 3 after a kept failure) offers two
honest exits. "Cancel that request" (`cancelSavedSponsorship`) voids: a tombstone if nothing was
admitted, a cancel if nothing was sent, else a reveal (kept, with a link to that sponsorship's
status page). "Try again" re-shows the step-2 acknowledgements for the SAVED request - with today's
quote for its amount, because a never-admitted request is priced when it finally is - then resends
the exact saved bytes. `settleSponsorshipFailure` sends final-looking codes to void-or-reveal.

**Session.** Invest's Support, Try again and Cancel are disabled until the session resolves; with
no subject there is nowhere to save a key, and a silent no-op reads as a broken button.
