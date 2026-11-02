---
type: plan
track: mvp-readiness-loop_20261026
date: 2026-11-01
updated: 2026-11-02
status: in-progress
source_session: c17ff8f2-e847-43ba-bca8-5bf8a66e911e
---

# Payment integration completion

The owner explicitly extended the takeover to the remaining payment integration
across ArdaNova and AZOA, and chose existing green CI audit evidence instead of a
new npm audit. The feed closeout is a milestone within this same active goal.

Snapshot: November 2. Node review head
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
The new Arda payment UI now has both its
earlier overlay verification and a clean Linux public-registry install/import
proof for the published SDK 0.6.0. Combined-branch build/test CI remains pending.
Arda backend head `bcbc2258da3ae32a8c55d47090cd0aeae599de06` is pushed separately on
`codex/payment-backend-closeout-20261101` in
[draft PR #8](https://github.com/Escherbridge/Ardanova/pull/8), with corrected CI
`41381713622` successful: full Application 1,606 passed/one existing skip out of
1,607 and API 652 passed; client validation also passed, while three image
publication jobs were skipped as a PR. The first run's lone old feed-registration
harness failure was fixed by reusing its existing test-only warning helper;
11 targeted cases passed, and production registration was unchanged. The combined
frontend/documentation branch remains local at
`62d3e52f9b6339ce409acbf5dc494f2b088d934b`, with frontend `d4faa5a`, prior docs
`f1f02bb`, backend harness cherry-pick `83f38c9`, current decision ledger
`63b119e` and the two-file SDK adoption commit `62d3e52`. Node source is committed
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

The owner authorized the real key backup; one file is now backed up with verified
DPAPI CurrentUser recovery. The protected manifest records backup hash
`2a301d...559c` (abbreviated). The owner explicitly authorized outbound restore;
the protected volume was seeded through the private helper with the one matching
file, hashes, ownership and modes verified. It is now retained **unattached**.
The final continuity wrapper has 50 passing
synthetic cases, actual Create/Verify proof against the unchanged old API
deployment `3177e8e7-fa01-4f73-aa4e-d482a9bf8340`, and successful verification of
the same challenge while the helper held the seeded volume. API attachment with
`skipDeploys: true` failed with `BAD_USER_INPUT`: attachment requires deployment.
Its sole staged delta was cleared. A never-used empty test volume on the helper
then proved that an explicit same-build redeploy did not apply its staged mount;
that patch was cleared and the empty test volume deleted. Current helper deployment
`8265f5ac-e914-46fc-aee8-c780be2c5fc3` is idle on the same pinned image with no
actual volume mount; all staging is empty. API deployment `3177e8e7-fa01-4f73-aa4e-d482a9bf8340`,
its original source/image and its running state remain unchanged. There has been
no API cutover or restart. The skip-deploy attachment/same-build route is not a
viable procedure; a supported pinned-image alternative is under review, with no
automatic API fallback. PostgreSQL and SurrealDB snapshot
catalog records were created at 12:59 UTC; workflow status returned Not Authorized
and no restoration was proven. npm owner `jadeoni-az` published 0.6.0; the public
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

## Working copies and ownership

- ArdaNova: current workspace, released feed baseline main `f915686`.
  Its existing `node_modules` is stale; `.omc/qa/takeover-verify` has a clean
  lockfile install, verified payment source and actual SDK 0.6 overlay for validation.
- AZOA: clean independent clone `.omc/payment-node`, based on confirmed remote
  main `c7cfa4af`. The original dirty checkout and owner-held main worktree are
  untouched. Its `origin` points to the project's canonical GitHub remote.
- SDK: public `0.6.0` exactly matches the reviewed archive. Arda's `^0.6.0`
  manifest/lock adoption is committed at `62d3e52` after independent review and
  clean Linux install/import proof; combined release CI and deployment remain.

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
6. Exact reviewed SDK publication and source adoption are verified; obtain the
   combined branch's clean registry CI before release. Migrate-first
   with backup for shared data. Deploy node before BFF/client and enable each
   capability only after its consumer and hosted return path are verified.

Identomat, Stripe TEST prerequisites, dependency decisions, coordinated Postgres
password rotation, and combined release CI remain explicit. Never
rotate the AZOA value key while P6-03 remains open. None of this plan authorizes
live-money activation or invented KYC/KYB evidence.
