---
type: plan
---

# Plan — Gated Commerce and AZOA Settlement

## 1. Correctness and boundary controls [P0]

- [ ] Add claims-based authorization and ownership checks to financial, gate,
      payout, escrow, wallet, and AZOA controller surfaces; the BFF must continue to
      derive the current user rather than accept `userId`.
  - Partial: project-token allocation/gate/failure mutations, treasury mutations,
    and payout processing now require a distinct fail-closed `AdminApiKey` claim.
    A bounded signed BFF actor assertion now binds `sub`, role, HTTP method and
    path to a 90-second HMAC envelope. The API verifies the envelope before
    granting the `ActorAssertion` policy; swap and AZOA avatar routes use the
    assertion as their sole user-id source. The BFF API key remains service
    authentication only. Extend the policy to each ownership-sensitive route
    only alongside its controller/BFF ownership tests. Wallet, token balance/portfolio/liquidity,
    payout request/history/cancel,
    task-escrow, and bid mutation routes now use that actor as their server-side
    identity: the public REST/BFF contracts no longer accept a wallet owner,
    payout user, escrow funder, or bid bidder id. They expose `/me` for self
    reads and reject cross-user wallet/escrow/bid access; bid disposition also
    verifies the opportunity poster or project owner in the API, rather than
    trusting the BFF-only check. Public bid discovery remains read-only. This is
    boundary hardening only, not a task agreement, checkout, or settlement flow.
    Token balance self-reads now use ActorAssertion-protected `/me` controller
    routes; the BFF and SDK routes neither accept nor forward a user id, and SDK
    requests create the same request-bound assertion as tRPC. Public aggregate
    exchange-value/preview reads remain identity-free. Admin payout processing and
    pending-payout reads now use a server-only client that sends both the service
    API key and distinct admin key, rather than broadening the service policy.
    The admin key is documented in the BFF environment template, and both BFF and
    API reject actor-signing material shorter than 32 bytes.
    The actor envelope is now v2-only: an HMAC assertion binds issuer, audience,
    subject/role, method, exact path plus query, normalized `Content-Type`, exact
    SHA-256 of the materialized JSON bytes, optional `X-Idempotency-Key`, timing,
    and a CSPRNG `jti`. `ActorAssertionReplay` is DBML-first and stores each `jti`
    under a primary-key uniqueness invariant before MVC is reached; expired ids are
    retained beyond the accepted expiry/skew window and then removed by a bounded,
    replica-safe cleanup worker. The API buffers a bounded request
    body then resets its stream for MVC, and the BFF sends the exact bytes it signs.
    V1 envelopes and actor-signed multipart requests are rejected. Focused tests
    cover altered query/body/content type/idempotency metadata, stream preservation,
    weak-key rejection, and one-winner concurrent replay. This still requires a
    reviewed additive Prisma migration and deployed database concurrency evidence
    before value-moving flows are enabled outside the trusted BFF boundary.
    Task self-reads now use the ActorAssertion-protected `/api/tasks/me` route;
    the prior caller-controlled `/api/tasks/user/{userId}` route is removed. Task
    creation, full edits, and deletion require the project creator or signed
    `ADMIN` actor; an assignee may update only that task's status. Explicit public
    task discovery routes remain read-only. Task status is not a settlement or
    reward signal. Controller and BFF endpoint tests cover actor-derived reads,
    creator/assignee/admin scopes, and foreign task/project mutation denial;
    broader task membership roles are still not modeled at this boundary.
- [x] Centralize holder-class x project-gate liquidity policy and replace the
      inverted duplicate implementation with table-driven parity tests.
  - `TokenLiquidityPolicy` is now the sole gate/holder-class matrix used by both
    allocation distribution and balance liquidity checks. Contributors become
    liquid at `ACTIVE`; investors and founders only at `SUCCEEDED`.
- [ ] Replace float/double money contracts on commerce paths with fixed-scale
      values; validate positive amounts and serialized equity-cap updates.
  - `UsdMoney` now provides the strict non-negative USD boundary: exact cents,
    invariant parsing, and rejection of sub-cent, grouped, negative, and
    overflowing input. Migrate `IStripeService` checkout/funding/payout inputs,
    Stripe metadata parsing, and `ProjectInvestment`/payout persistence to this
    value before changing the DBML/Prisma columns from `float`; the legacy
    service is deliberately not partially migrated while its transactional
    funding slice is being edited.
- [~] Remove committed database credentials from active backend configuration
  and fail fast when `DATABASE_URL`/`ConnectionStrings__DefaultConnection` is
  absent. The tracked API and MCP configuration no longer contains a database
  connection value; Docker/local templates and Railway now use deployment
  environment variables. Credential rotation and a reviewed git-history
  remediation remain operator actions, and repository-wide secret scanning is
  still required before launch.

## 2. Durable economic settlement [P0]

- [x] Add dormant DBML-first `EconomicSettlement` and `EconomicOutbox` models
      with fixed-scale decimal amounts, unique semantic/external keys, optimistic
      version, settlement state contract, and lease/reconciliation metadata. They
      are generated into Prisma and EF entities but have no controller, dispatcher,
      or AZOA invocation.
- [~] Add DBML-first payment inbox, `FundingIntent`, `SwapQuote`, `SwapOrder`,
  and `TaskCommerceAgreement` models. `FundingIntent` and
  `TaskCommerceAgreement` are now DBML-first and generated into Prisma/EF:
  both have stable semantic keys, fixed-scale amounts, immutable terms hashes
  and snapshots, explicit lifecycle states, and unique provider/settlement or
  bid/task/escrow links. Funding reaches `EconomicOutbox` only through its
  unique `EconomicSettlement`, so there is no second dispatch relationship.
  The Stripe-only `StripeWebhookEvent` inbox is generated and its ingress uses a provider event-id lease to make completed
  deliveries idempotent and failed deliveries retryable. Stripe funding now wraps
  its existing local allocation, balance, treasury, investment, and Gate 1 writes
  in one shared database transaction and stamps its single Stripe funding
  `ProjectInvestment` with the payment-intent id. A reclaimed provider-event lease therefore either
  finds the committed investment and acknowledges it or retries after a rollback;
  it cannot replay a partially committed local funding effect. The DBML/Prisma/EF
  mapping now makes `ProjectInvestment.stripePaymentIntentId` unique (while
  allowing multiple nulls), so distinct Stripe events for the same payment intent
  cannot produce more than one investment record. This is a bounded
  bridge over the legacy services, not the required local-decision + outbox design:
  it still needs a reviewed Prisma migration/deployment, a transactional intent/
  settlement/outbox writer, and the remaining `SwapQuote`/`SwapOrder` models.
  Funding checkout now has a bounded actor-scoped vertical slice: a canonical
  request idempotency key creates one immutable `FundingIntent` before the
  provider gateway is called; a repeated request reuses that intent and Stripe
  checkout session. The provider success/failure webhook records only the
  durable `FundingIntent` state after checking immutable amount/currency and
  provider metadata. It does not create an investment, allocate tokens/equity,
  invoke AZOA, create a settlement/outbox, or treat a browser redirect as
  confirmation. The BFF sends a signed idempotency header and redirects only to
  the server-created checkout URL; the owner-only status endpoint exposes the
  durable intent state, not provider ids. An additive reviewed migration for
  the new idempotency/checkout-session columns, provider webhook reconciliation,
  and production migration/reconciliation evidence are still launch gates. A
  signed `payment_intent.succeeded` now atomically records the immutable funding
  verification, a `PENDING_DISPATCH` `FUNDING_ALLOCATION` settlement keyed by
  the intent and provider event, and exactly one inert `PENDING` outbox row.
  Exact event replay read-validates the immutable decision and divergent terms,
  event ids, missing links, or missing outbox data fail closed. The handler does
  not allocate value, dispatch AZOA, or transition a settlement to confirmed.
- [~] Transactionally persist the local decision and outbox claim; enforce unique
  external event/idempotency keys and CAS/lease transitions.
  - Funding signed-success is now the first transactional writer. Escrow,
    refund, and swap decisions remain unimplemented. Funding now has a typed,
    CAS lease store and bounded dispatcher/reconciler seam: a conditional EF
    claim is single-winner, finalization rejects stale leases inside one local
    transaction, and both claim/finalization condition on the linked settlement
    state and version (`PENDING_DISPATCH` for dispatch,
    `AWAITING_RECONCILIATION` for reconciliation). Dispatch preserves the
    immutable settlement idempotency key and sends a validated canonical
    base-unit amount rather than a culture-sensitive decimal. A typed ambiguous
    result or known transport uncertainty becomes `AWAITING_RECONCILIATION`
    rather than a re-broadcast; caller cancellation still bubbles. Accepted is only `SUBMITTED`, never confirmed or allocated;
    reconciliation retry remains on the reconciliation path. The only runtime
    gateway is fail-closed and the worker is unregistered/disabled by default,
    so this creates no HTTP call, AZOA effect, allocation, payout, or settlement
    completion. A production transport, selected-node attestation, operator
    enablement, hosted execution, and live concurrency evidence are still
    explicit activation gates.
- [ ] Wire verified Stripe funding, approved escrow release/refund, and executed
      swap to the outbox — never directly to the UI action.
- [ ] Record and surface AZOA `replayed`, KYC rejection, operation id, confirmed,
      failed, and `AwaitingReconciliation` states.

The repository currently has no reviewed Prisma migration history. The additive
DBML model is safe to generate locally, but a production deployment must create
and review an additive Prisma migration against a non-production database before
the schema is applied; do not use `prisma db push` against a shared environment.
The review must include existing-data compatibility, nullable-link/backfill
decisions, unique-index build behavior, and rollback evidence. No checkout,
funding-intent status route, settlement dispatcher, or outbox worker may be
economically activated merely because these generated models exist; the present
dispatcher is an inert state-recording seam only.

An operator-only `npm run db:preflight-commerce -- --phase baseline|additive`
now provides a read-only PostgreSQL release check. `stripePaymentIntentId` is
baseline data; only its exact, valid, ready, non-partial unique index is
additive. The preflight verifies each gated-commerce and wallet-verification
column's PostgreSQL type/nullability/default plus each required index's key
order, uniqueness, validity, readiness, and non-partial predicate. It rejects
an `assetScale` default: canonical historical decimals must be backfilled from
the authoritative chain before a separate no-default `NOT NULL` hardening step.
It requires an explicit
operator-approved catalog fingerprint and a database role limited to `SELECT`;
it reports without connection strings, blocks an unexpected baseline or
incomplete additive state, detects duplicate non-null Stripe payment-intent
ids, and lists `ProjectTokenConfig` asset-scale/chain-asset backfill work. The
strict order is: pin/review a non-production baseline fingerprint; create and
review an additive migration on a disposable clone; deploy through the approved
release path; then run additive preflight using a separately reviewed
post-migration fingerprint. See
`documentation/GATED_COMMERCE_MIGRATION_RUNBOOK.md`. This tool neither creates
nor applies a migration and does not make settlement eligible.
The asset-scale path is explicitly staged: Stage 1 adds nullable `integer`
with no default; Stage 2 captures an append-only, dual-reviewed authoritative
chain-asset manifest; Stage 3 validates, version-guards, backfills, reads
back, and reconciles immutable agreements/settlements; only Stage 4 adds
`NOT NULL`, still with no default. The current DBML `NOT NULL DEFAULT 6`
declaration cannot generate Stage 1. The implementation cutover must also
remove DTO defaults, make config creation explicitly scale-bound, and align
the current 0--19 service checks with `FixedScaleAmount`/`numeric(38,18)` at
0--18. See `documentation/ASSET_SCALE_MIGRATION_CONTRACT.md`.

## 3. Commerce UX [P0]

- [~] Bid acceptance creates the task-commerce agreement and redirects to the
  assigned task commerce view; task status alone cannot settle a reward.
  - The actor-derived `POST /api/opportunity-bids/{id}/accept` now authorizes
    the project owner (or the poster before rejecting non-project commerce),
    atomically creates/reuses one assigned task and one immutable
    `TaskCommerceAgreement`, and returns the server-owned
    `/tasks/{taskId}/commerce` route. Its read-only view obtains the agreement
    through an actor-derived `/api/task-commerce/{taskId}` endpoint and is
    visible only to the contributor or project creator. Exact replays, including a database unique
    collision, re-read and return the same immutable agreement; conflicting
    assignment state is rejected. The decimal scale is now captured from the
    canonical project-token configuration rather than inferred at acceptance.
    This is deliberately local and
    effect-free: it creates no escrow, quest, settlement, outbox, payment, or
    token allocation. A reviewed production migration (including the new
    `ProjectTokenConfig.assetScale` column) and multi-process
    concurrency evidence remain before the transition is release-ready. The
    owner-facing bid panel invokes the BFF accept mutation and navigates only to
    its exact, validated internal `commerceUrl`; it never derives a task URL
    from client data or claims a payment, escrow, or reward outcome.
- [~] Build the avatar/wallet panel, asset portfolio, verified external payout
  wallet flow, funding checkout/status flow, and receipt/history export flow.
  - A DBML-first, actor-scoped Algorand wallet proof foundation now replaces the
    unsafe direct verification toggle. `WalletVerificationChallenge` binds the
    actor, wallet, canonical public address, chain, network, and expiry to a
    canonical message. It stores only SHA-256 digests of the CSPRNG nonce and
    submitted signature,
    records a one-time proof outcome, verifies the Algorand checksum plus Ed25519
    signature server-side, and conditionally consumes the challenge and marks the
    wallet verified in one database transaction. Invalid proofs consume the
    challenge; expired, foreign, and concurrent/replay completions fail closed.
    This is not yet a user-facing wallet signer or payout journey: it needs a
    reviewed additive Prisma migration, browser wallet signing integration,
    production database concurrency evidence, policy-gated payout use, and the
    avatar/portfolio/export UX. It exports neither private keys nor mnemonics.
  - The settings shell now exposes an actor-scoped external-wallet list and
    public-address registration, primary selection, removal, AZOA linkage
    status, portfolio navigation, and a route into project funding. The browser
    never asks for a secret, invokes a signer, sends a proof, exports an asset,
    or represents an address record as custody. The previous simulated Stripe
    Connect button was removed: payout settings now state that payout/export is
    disabled until a server-issued provider flow, verification policy, and
    durable settlement reconciliation exist. Browser signer integration, a
    reviewed additive migration, payout eligibility policy, provider credentials,
    and confirmed settlement evidence remain explicit launch gates.
- [~] Replace URL-param investment confirmation with server-owned intent status.
  - The redirect page no longer displays URL-provided amount, token, or equity
    values and never declares an investment confirmed; it shows payment
    verification pending. The actor-scoped `FundingIntent` status endpoint and
    authenticated Stripe checkout now exist. The checkout intent is committed
    before provider contact and replays by exact idempotency key, while signed
    webhooks only record payment verification. It still needs the reviewed
    additive migration, reconciliation/outbox activation, and release-gate
    evidence before the journey is complete.
  - The return page now polls only its server-owned funding intent and maps the
    complete durable lifecycle (`AWAITING_PAYMENT`, payment verification,
    settlement pending, terminal failure, and settled) without treating a URL
    redirect as confirmation. Missing, unknown, or failed status reads remain
    explicitly unconfirmed. This remains UI-only; no payment, allocation,
    custody, or settlement path is activated.
  - Checkout creation now fails closed before an intent or payment-provider
    session is created unless an explicitly enabled, selected-node gateway
    exposes both settlement dispatch and reconciliation capabilities with a
    current operator attestation. The present disabled gateway cannot satisfy
    this port, so configuration cannot accidentally activate collection. A real
    transport, independent attestation validation, worker registration, and
    production reconciliation evidence remain required before this gate can open.
- [ ] Update swap UI to select eligible target assets, show an expiring quote,
      explain ARDA intermediary legs, and display durable order/settlement status.

## 4. Quest and allocation integration [P0]

- [ ] Link accepted agreements to self-run quest instances or the documented
      consented alternative; stop calling tenant-created random-password avatars
      "self-run" until the credential/consent handoff exists.
- [ ] Dispatch settlement through `IAzoaAllocationService` with stable event keys;
      retain AZOA as the custody/exactly-once layer and ArdaNova as the economics
      authority.
- [ ] Block live dispatch unless the selected node has reconciliation, custody,
      fee-funding, and KMS/operator readiness evidence.

## 5. Verification [P0]

- [~] Controller/BFF IDOR and role-policy tests.
  - Focused controller and BFF client-contract tests cover server-derived
    commerce identity and foreign-resource rejection. Broader financial,
    gate, and role-policy coverage remains required.
- [ ] Duplicate payment/release/refund/swap, crash-window, timeout/reconciliation,
      and concurrent equity/gate tests.
- [ ] Simulated end-to-end: bid -> task agreement -> escrow -> quest -> award ->
      project-token/ARDA quote -> order -> settlement receipt.
- [ ] Full backend and frontend verification; real-value cutover remains blocked
      until every acceptance gate in the spec has evidence.

## Retro note — payout completion must follow provider proof

The legacy administrative payout processor previously rebalanced treasury and
debited project tokens before marking a request `COMPLETED`, without creating
or verifying a provider transfer. It is now fail-closed at both the HTTP and
service seams: processing returns `503 Service Unavailable` and performs no
read, write, rebalance, debit, or completion transition. Payout request,
history, and cancellation remain non-settlement records only. Re-enable this
route only with a reviewed durable state machine that records a provider
transfer idempotency key, reconciles signed provider completion/failure, and
atomically finalizes or compensates the token lock; add crash-window and
provider-replay acceptance evidence before any production activation.
