---
type: plan
track: mvp-readiness-loop_20261026
date: 2026-11-01
status: in-progress
source_session: c17ff8f2-e847-43ba-bca8-5bf8a66e911e
---

# Payment integration completion

The owner explicitly extended the takeover to the remaining payment integration
across ArdaNova and AZOA, and chose existing green CI audit evidence instead of a
new npm audit. The feed closeout is a milestone within this same active goal.

Local implementation verification is complete. The node full run and targeted
repairs resolve all 13 initial failures; independent TRX parsing reports latest
outcomes of 1,205 passed, zero failed and one existing G3 skip across 1,206 unique
cases, including four new regressions. This is full-plus-targeted evidence, not a
single green full rerun. Final release gates remain open. Arda source is in local,
unpushed backend `c760b51` and frontend `d4faa5a` commits. Node source is committed
and pushed at `1f83837b` on its review branch, with all 103 reviewed files matching
the final attestation; node main is unchanged, no deployment occurred, and branch
[draft PR #3](https://github.com/JadeZaher/azoa-autonomous-zones-of-action/pull/3)
is open with PR CI run `41377213143` in progress. See the
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
- SDK: source `0.6.0` in that clone; registry latest `0.5.0` on investigation.
  Production adoption follows the existing publication gate. A local reviewed
  build may support integration verification without pretending publication.

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

Integration inspection reopened two unpublished SDK issues. Cash-out/sale local
discard and broad refusal codes could forget a key after a lost accepted reply;
those flows must retain the key until a validated named result resolves it.
Recovery must remain possible through same-key retry after setup or consent is
restored. Also, sale proceeds and ARDA balances need explicit denomination and
bounded precision rather than assuming ISO currency or displaying a USD reference
as actual proceeds. Both changes require SDK seam tests and renewed publish review.

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
6. Publish the reviewed SDK before production package adoption; migrate-first
   with backup for shared data. Deploy node before BFF/client and enable each
   capability only after its consumer and hosted return path are verified.

Identomat, Stripe TEST prerequisites, dependency decisions, coordinated Postgres
password rotation, and the existing SDK publication gate remain explicit. Never
rotate the AZOA value key while P6-03 remains open. None of this plan authorizes
live-money activation or invented KYC/KYB evidence.
