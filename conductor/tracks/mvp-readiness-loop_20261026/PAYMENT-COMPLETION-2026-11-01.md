---
type: plan
track: mvp-readiness-loop_20261026
date: 2026-11-01
updated: 2026-11-03
status: in-progress
source_session: c17ff8f2-e847-43ba-bca8-5bf8a66e911e
---

# Payment integration completion

The owner explicitly extended the takeover to the remaining payment integration
across ArdaNova and AZOA, and chose no fresh local dependency audit. Ordinary
release CI retains its production-audit check. The feed closeout is a milestone
within this same active goal.

Snapshot updated November 3 UTC. Node review head
`f1b8824b30901cfb2073f2e14e9e08f2219b33e0` includes the reviewed 16-file hosted
handoff/login-validator repair. Current PR CI `41449375013` is **SUCCESS with all
nine jobs successful**: single full unit 5,687 passed/zero failed/skipped in
1m 7s; normal integration 1,205 passed/zero failed/one existing G3 skip out of
1,206 in 49m 14s; selected G1/G2/G3/G5/G7 gates passed 2/2/2/1/1 cases.
SDK 646/45 files, frontend 326/59 and Chromium 24/eight passed. A single complete
Playwright run passed all eight cases in 1.8 minutes at 13:07 UTC, including all
three new handoff journeys, the sign-in shell, and existing signup/KYC, on-ramp,
KYB and webhook journeys. Prior head `1f83837b` retains its separate fully green
CI `41377213143`; the current counts above are direct current-head evidence.
Combined Arda [draft PR #9](https://github.com/Escherbridge/Ardanova/pull/9) at
`072585db831a93914d80452d112a387805f71383` has terminal **SUCCESS** in CI
`41462170432`: Application 1,606 passed/one existing skip out of 1,607; API 652
passed; full published-SDK client 1,035 passed across 111 files; typecheck, lint,
vocabulary, formatting, pin/platform guards and 48-page build passed. The ordinary
CI production audit reported zero vulnerabilities; no local audit was launched.
Both validation jobs succeeded and all three image-publication/record jobs were
skipped as a PR, so this is validation rather than image publication or deployment.
Backend-only PR #8 is now closed as superseded by this exact-head combined proof;
its earlier CI and 11-case harness-repair evidence remain retained. The pushed
combined branch includes frontend `d4faa5a`, backend harness correction `83f38c9`,
decision ledger `63b119e`, Linux-generated SDK adoption `62d3e52` and docs
`072585db`. Node source is committed
and pushed at `f1b8824b` on its review branch; the preceding 103-file source
attestation at `1f83837b` and the new 16-file reviewed repair remain recorded.
Node main is unchanged, no payment deployment occurred, and
[draft PR #3](https://github.com/JadeZaher/azoa-autonomous-zones-of-action/pull/3)
is open with the current CI status above. Earlier completed remote jobs passed
SDK 646/45 files, frontend 326/59 and Chromium 24/eight. Local browser evidence is
honestly cumulative: three existing regressions, two new handoff cases, then the
final renewal case passed across separate runs. All 20 validator cases passed,
including 17 new cases. The privacy canary found no raw/encoded password, ARIA
snapshot or trace/screenshot/video output. All owned local stack resources and
the private forwarding helper were cleaned up without changing other containers.
The current-head eight-case CI run now supplies single-run browser proof,
including the webhook excluded locally by the unchanged receiver-subnet policy.
Production activation flags remain closed. The deployed Arda-to-node-to-bank
withdrawal journey remains a further gate.

**Protected storage A1/A2 is CLOSED and independently approved.** The owner-authorized
one-file DPAPI CurrentUser backup/recovery and seeded-volume checks were followed
by an exact-c7 maintenance image cutover. API deployment
`3a0e8eb9-06fd-4a9a-87c7-199e15b93f7d` is SUCCESS at immutable public index
`sha256:f001e8c54bd337b11dcae69a3024df9f793c8361304efce47839b6c1a76b1934`
(linux/amd64 manifest `sha256:434a2d39da60e55626ec0f1312d4b95efd7d2f3e5c348236660280578bbd242e`),
with volume `3c9b266d-586b-495b-98d1-6113e981f1f7` mounted persistently at `/app/data`.
The original one-file, 1,001-byte key hash `dc95ebb8...83795` is unchanged, file
mode `0600`/ring mode `0700` and UID/GID `1654` verified. The original `9098` challenge passed
on the API before and after an actual controlled restart: PID1 start ticks changed
from `418222674` to `418252693`, health returned `200 Healthy`, and the probe ran as real/
effective UID/GID `1654` with zero supplementary groups. Wrapper `c056985b...efb3a`
passed 53 Windows and six actual Linux checks; DLL and transfer artifacts are unchanged.
C's closure receipt and the post-restart proof are linked in the runbook.
The runtime remains source `c7cfa4af`, environment Staging and boot migrations
skipped; Node PR3 `f1b8824b` is not deployed. Helper deployment
`a6863596-908b-4671-ac80-6ebe03306e39` is retained idle without a mount for later
probe work; staging is empty. Earlier rejected skip-deploy/staged-mount attempts
are historical receipts, not current blockers or replayable procedures.

Database recoverability remains open. PostgreSQL and SurrealDB snapshot catalog
records do not establish a completed disposable restore. Read-only PG 17.11
inspection and independent release-blob comparison found 27 applied migrations
with matching checksums and only `20261101000000_sale_admission_proof` pending,
Linux release SHA256 `92adcf8e3617eac53e539545794d2839b7795a2ca03d248dc366343fbcf75bf8`.
Its SQL and actual catalog are compatible; recoverability and shared migration
execution remain gates. No database migration or real-money activation occurred.

npm owner `jadeoni-az` published 0.6.0; the public
archive exactly matches reviewed SHA256
`7431db9e1a5bfc5e628b8764f0371d2128c8fa68b6e41442032542d9d48ec3fd`
and registry SRI. The reviewed Linux-generated `^0.6.0` manifest/lock are committed;
clean registry `npm ci` installed 603 packages, completed generation/patching,
passed the SDK and three-binding platform guards, and imported actual money and
React exports through ESM and CommonJS. The owned verification container is
removed; Windows dependencies remain untouched. Planned engineering proceeds
under the accepted authorization; owner choices are carried in the current
[decision ledger](DECISION-LEDGER.md). See the
[release evidence and proposed rollout](PAYMENT-RELEASE-RUNBOOK-2026-11-01.md)
for exact checks, publication/adoption, release CI and protected-file/database
prerequisites. No payment deployment or activation is claimed.

Normal AZOA production-promotion configuration still needs the required
protected environments, designated reviewer identity and scoped Railway deployment
credential. The eight nonsecret target variables are now configured and their
exact IDs/health URLs were read back successfully.
This is an existing administration prerequisite, not a new blanket approval gate
for engineering. Reviewer selection is recorded as PAY-D04; nonsecret IDs/URLs
remain routine configuration. The separate maintenance publisher has no
environment or deployment authority and does not waive production protections.

## Working copies and ownership

- ArdaNova: current workspace, released feed baseline main `f915686`.
  Its existing `node_modules` is stale; `.omc/qa/takeover-verify` has a clean
  lockfile install, verified payment source and actual SDK 0.6 overlay for validation.
- AZOA: clean independent clone `.omc/payment-node`, based on confirmed remote
  main `c7cfa4af`. The original dirty checkout and owner-held main worktree are
  untouched. Its `origin` points to the project's canonical GitHub remote.
- SDK: public `0.6.0` exactly matches the reviewed archive. Arda's `^0.6.0`
  manifest/lock adoption is committed at `62d3e52` after independent review,
  clean Linux install/import proof and successful combined CI `41462170432`;
  release-image publication, migrations and deployment remain.

## Lane N: node requests, delivery and hosted ownership flow

Add persisted, immutable tenant consent requests with member/tenant binding,
exact scopes, expiry, stable consent command ID, and allowlisted return URL.
The accepted scopes are `kyc:read`, `kyc:submit`, `units:issue`, `swap:sign`, and
`transfer:sign`.

Create/resend returns metadata only to the tenant. Resend atomically retires old
live invitations. Raw links belong only in protected operator-readable delivery
stub records and recipient URL fragments, never ordinary logs or tenant responses.
Production stub use needs explicit acknowledgement; sending real email is out of
scope because the owner chose a resend-capable stub.

Hosted pages scrub the fragment, preview safe terms, let the member choose their
credential, wait for claim/revocation commit, then use that first-party session
to approve or decline the exact persisted grant. Renewal is consent-only.
Payout-account setup remains a first-party action on the node's hosted flow.
Default claim switches remain off until the complete return journey is verified.

Frozen tenant routes are `POST .../custodial-accounts/{external}/handoff`,
`POST .../handoff/{requestId}/resend`, and `GET .../handoff/{requestId}`. The BFF
supplies the authenticated member's stored email and its own fixed return URL.
The response contains only request ID, target, ownership, status, expiry, and
`delivery: operator_stub`. It never contains the raw link. Member preview,
read, approve and decline live under `/api/user/handoffs`; delivery retrieval is
restricted to a recent node-operator session. Stub UI must not claim email was sent.

## Lane A: ArdaNova backend continuity and sale proof

Consume ownership/consent before requiring full binding fields. A minimal claimed
status preserves stored node IDs, wallet bindings, and omitted verification
mirrors; it never manufactures authority through ensure. Full authenticated
claimed status may update the wallet mirror while retaining stable node identity.
Use the claimed-member relinquish route and pause member value dispatch while
required consent is absent or expired.

Persist one immutable exchange admission deadline per sale and use it for every
same-key send. Only typed receipt absence from a read started after that deadline
plus skew/margin, with enforcement attested, can release units. Legacy rows without
proof remain unresolved. Serialize terminal payout transitions with balance
changes to prevent concurrent cancel/process from double-unlocking or debiting.

Provide host-priced durable quotes and request-keyed sale admission for the SDK's
`{requestKey, quoteId}` contract. A browser-generated label is not a quote or an
idempotency guarantee. Lane A owns DBML, additive migrations and generated models.

The schema batch is `20261101000000_sale_admission_proof`: immutable payout
deadline/command/quote/request-key fields and a persisted `PayoutSaleQuote`.
New quote/admission endpoints are `POST /api/Payouts/sale/quotes`,
`POST /api/Payouts/sale`, and actor-scoped `GET /api/Payouts/sale/{id}`. A retry
must resolve its admitted request before checking an expired quote. Value amounts
cross the new boundary as decimal strings, with explicit ARDA precision.

## Lane C: host transport and money screens

Implement `AzoaMoneyTransport` over thin authenticated tRPC/BFF endpoints; no node
bearer reaches the browser. Cover account/KYC/consent, add-funds with required
checkout void, cash-out, balance, and host-priced sales. Keep project investment,
pricing decisions, actor assertions and return URL policy in ArdaNova.

Use the SDK money/react components with host slots. Persist exact subject-scoped
withdrawal requests before sending. Lost responses, refresh, consent failures and
unknown results preserve the key; an empty 2xx is unconfirmed. The ownership step
uses node delivery metadata and first-party hosted setup, never an ArdaNova-held
node login. Node frontend also adopts the SDK money layer as its first host.

Integration inspection found and repaired two then-unpublished SDK issues.
Cash-out/sale local discard and broad refusal codes could forget a key after a
lost accepted reply; those flows now retain the key until authoritative proof
resolves it.
Recovery must remain possible through same-key retry after setup or consent is
restored. Also, sale proceeds and ARDA balances need explicit denomination and
bounded precision rather than assuming ISO currency or displaying a USD reference
as actual proceeds. Both changes received SDK seam tests and independent review
and are included in the exact published 0.6.0 archive.

The independent reviews also found queued sends crossing account switches and
unreadable saved keys being overwritten. Every host procedure now needs an
expected-subject guard against its authenticated session, and unreadable pending
storage must block a replacement rather than masquerade as an absent request.

Retention alone leaves a liveness gap when no sale/cash-out was admitted. Add
authoritative void-or-reveal operations: ArdaNova sale requests use a scoped
`PayoutSaleVoid` tombstone under the same admission lock; node cash-outs use a
`fiat_payout_request_fence` in the same transaction as payout admission. A failed
void never clears a key. Existing transactions are revealed, never blindly
refunded or aborted. The new node void route remains disabled until old node
processes without this fence have drained, then receives explicit activation.

## Review and verification gates

1. Share exact node and backend route/DTO contracts before client wiring.
2. Keep file ownership disjoint; record any integration collision explicitly.
3. Review cross-repository authority, request keys, receipt proof, redirect and
   token-delivery seams independently; apply all findings before the final sweep.
4. Run one final integrated sweep after all fixes: SDK, both frontends, node unit
   and isolated-SurrealDB integration, ArdaNova application/API with scratch
   Postgres, formatting and generated-schema checks. Use the authorized prior
   green audit evidence; do not rerun the rejected npm audit.
5. Exercise claim/resend races, cross-tenant denial, immutable scope approval,
   renewal/revoke/decline, response loss and ambiguous payout receipts in an
   isolated integrated stack. Never use demo SurrealDB or fabricate compliance.
6. Exact reviewed SDK publication, source adoption and combined clean registry CI
   are verified. Obtain immutable release images and migrate-first
   with backup for shared data. Deploy node before BFF/client and enable each
   capability only after its consumer and hosted return path are verified.

Identomat, Stripe TEST prerequisites, dependency decisions, coordinated Postgres
password rotation, and release-image/operational gates remain explicit. Never
rotate the AZOA value key while P6-03 remains open. None of this plan authorizes
live-money activation or invented KYC/KYB evidence.
