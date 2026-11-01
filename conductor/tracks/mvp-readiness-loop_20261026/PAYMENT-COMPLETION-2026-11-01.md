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

## Working copies and ownership

- ArdaNova: current workspace, baseline main `8f1641d` plus reviewed feed cleanup.
  Its existing `node_modules` is stale; `.omc/qa/takeover-verify` has a clean
  lockfile install and the reviewed feed diff for validation.
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
