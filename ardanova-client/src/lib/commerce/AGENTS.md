# Commerce helpers (`src/lib/commerce`)

## Persisted request keys

`persisted-request-key.ts` is the idempotency-key lifecycle for the two first-party money flows:
add funds (`add-funds-request.ts`, page `app/portfolio/add-funds`) and sponsoring a project
(`sponsorship-request.ts`, page `app/projects/[slug]/invest`). Ported from oasis-sleek
`lib/persisted-request-key.ts`; storage access follows `components/ai/nova-refine-storage.ts`
(an explicit `Storage` parameter, unreadable entries treated as absent).

**The rule** (memory: money-key-doctrine). A saved key is released only when the server PROVES
nothing was or can be paid under it, or hands the request a server id so the page can follow it by
STATE. In the browser that is exactly two answers, and neither is a refusal code:

1. **Before the server names the request**, the only release is a 200 `voided` from the void route
   (`voidSavedRequest`, the single release path for a key with no server id).
2. **Once a 200 hands over a server id, the page decides by that record's state.** Invest's
   `createCheckout` 200 carries the intent id - SETTLEMENT_PENDING included - so its `onSuccess` clears
   the key and the success page follows the intent; the key's job (one admission) is done, and a later
   sponsorship of the project is meant to be a new one. Add funds records the payment id
   (`recordAdmittedId`) and clears only when a read of THAT payment ends it (`endsSavedPayment`).

No money code releases a key on its own. The tRPC/HTTP status is never consulted: on these routes
400/403/409 each mean several things, and the BFF's `ExceptionHandlingMiddleware` answers an
`ArgumentException` or a SQLSTATE 22xxx/23503/23514 with a code-less 400 that can arrive after the
node already moved funds.

- `definite` and `finished` LOOK final (`looksFinal`). Without a server id to read, the page sends
  them to void-or-reveal and acts on its answer: `voided` releases, `revealed` keeps the key and
  follows what the server found. `finished` exists because the node sends INTENT_FINISHED for PAID
  intents too, and REQUEST_KEY_IN_USE names a request that may have been paid (round-3 C-1).
- `needs_review` (round 4 #3) keeps the key AND stops resending it: the payment is held for a manual
  check, and the same key only meets the same hold. It carries the held payment's id
  (`error.data.moneyIntentId`, read with `moneyIntentIdOf`) - see **Add funds** below.
- Everything else - `unconfirmed`, a missing or unknown code, a network drop - keeps the key and
  asks for nothing.
- A failed void (any non-200) keeps the key whatever it says; the BFF also downgrades a failed
  void's code to `unconfirmed` (`MoneyRefusal.ForFailedVoid`), so nothing final-looking leaks out.

**Void-or-reveal routes** (BFF, actor-scoped): `POST /api/add-funds/requests/{key}/void` ->
`{ outcome: "voided" } | { outcome: "revealed", payment }` (proxies the node's own void, which
expires an unpaid Stripe page first); `POST /api/funding-intents/requests/{key}/void` ->
`{ outcome: "voided", intentId } | { outcome: "revealed", intentId, intent }` (BFF-owned; decision
table in `ardanova-backend-api-mcp/.../Services/Implementations/AGENTS.md` §Sponsorship void). The
tRPC proxies are `addFunds.voidRequest` and `fundingIntent.voidRequest`; both validate the 200
body, and an outcome word they do not know throws with no money code (keeps the key).

**How the code reaches the page.** `BaseApiClient.failed` copies the BFF body's `code` (and
`intentId`) onto `ApiResponse`; the money routers throw `moneyRefusalError`
(`server/api/money-refusal-error.ts`), which parks a validated code - and, for `needs_review` only, a
validated payment id - on the `TRPCError`'s cause; `errorFormatter` (`server/api/trpc.ts`) exposes
them as `error.data.moneyCode` / `moneyIntentId`; pages read them with `moneyCodeOf` /
`moneyIntentIdOf`. A body without a code - the middleware's post-commit 400 included - yields
`moneyCode: null`, which keeps the key.

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
- **Held for a manual check** (round 4 #3). A `needs_review` answer - from Continue or from the void -
  is recorded by `recordHeldForReview`: the entry carries `heldForReview: true`, which survives a
  reload, and the held payment (when the answer names one) becomes the key's `admittedId`.
  `savedPaymentLocksContinue` is then locked whatever that payment's page last said - and also when
  the answer named NO id (round 5 L2: the copy says it "won't be sent again", so Continue must not
  resend it; with no id there is nothing to read, so the lock copy does not promise a re-check). The
  banner says honestly "This payment is held for a manual check - quote <id> if you contact us" instead
  of promising that Continue "picks up the same payment" (it used to resend into the same 409 forever).
  "Start a new payment instead" still calls the void, which decides by the node's row
  (oasis-sleek `Providers/Fiat/AGENTS.md` §request-void): a row still `CLAIMED` with no page attached is
  voided at any age (that is exactly the shape begin's `needs_review` comes from), an attached page is
  settled at Stripe (`voided` / `revealed` / 503), and the void answers `needs_review` only for an
  UNATTACHED row that is no longer `CLAIMED` (a shape no begin produces) - keep. A read of the held
  payment that ends it (`added`/`went-wrong`) clears the key as usual.

**Invest** (`sponsorship-request.ts`, H-1). The banner (and step 3 after a kept failure) offers two
honest exits. "Cancel that request" (`cancelSavedSponsorship`) voids: a tombstone if nothing was
admitted, a cancel if nothing was sent, else a reveal (kept, with a link to that sponsorship's
status page). "Try again" re-shows the step-2 acknowledgements for the SAVED request, then resends
the exact saved bytes. Its price copy names both cases, because the page cannot tell them apart: a
request the server never admitted is priced when it finally is (today's estimate is shown), while an
admitted dormant DRAFT completes at the quote it locked when it was admitted.
`settleSponsorshipFailure` sends final-looking codes to void-or-reveal. Step 3's "closed, nothing was
or will be taken" copy keys on an explicit `released` flag set ONLY in the void's `voided` branch -
an empty `savedRequest` is absence of state (no session, or a first send's claim never reached
state), not proof.

**Session.** Invest's Support, Try again and Cancel are disabled until the session resolves; with
no subject there is nowhere to save a key, and a silent no-op reads as a broken button.
