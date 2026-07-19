# Controller contracts

## Hierarchy assignment

Generic epic, sprint, feature, backlog-item, and task updates never change assignment. Where a resource exposes an `/assign` action, that action is the only assignment boundary: a user ID assigns and `null` unassigns. Keep project-membership and manager authorization checks on those actions.

## Email-only invitations

Invitations that contain only an email address are intentionally fail-closed for accept, reject, and user-scoped reads because they are not yet bound to an authenticated user ID. Do not infer ownership from an unverified request value or loosen this guard. Enabling that flow requires a verified email-claim binding at the authentication boundary first.

## Escrow disputes

The dispute boundary derives `DisputedByUserId` from the signed actor assertion and never accepts it from JSON. Reason and narrative are part of the mutation contract, are validated before database access, and are persisted with `disputedAt`; collecting them in the interface without storing them is a data-loss bug. The nullable production columns must exist before deploying an API that writes this contract.

## AZOA custodial accounts

The custodial-account endpoints derive the ArdaNova user from the signed actor
assertion. The application sends AZOA only a stable tenant/user binding and
operation-specific idempotency keys; account ensure and KYC-session ensure each
reuse their own stable key while Azoa owns convergent attempt sequencing. No
private key, mnemonic, generated password, or omnibus
platform-wallet identity belongs in this contract. Wallet and KYC readiness are
authoritative AZOA status, and inconsistent tenant/user bindings fail closed.

The legacy `KycController` is an archival compatibility surface and returns
410. It must never regain local submission, document-read, approval, rejection,
or verification-level authority. Provider configuration and review belong to
AZOA. The actor-bound custodial controller may begin a provider-neutral session;
ArdaNova does not expose provider identifiers or arbitrary document-reference
submission to the public frontend.

Non-hosted KYC can begin only when Azoa explicitly returns
`developmentSimulation=true`. That signal is valid solely for Azoa's available
Manual authority in Development and must be passed through unchanged. It is a
workflow simulation, not identity evidence, and never creates a Production
manual-review path.

The legacy `/api/azoa/avatar/ensure` and `/wallet` mutations return 410. They
used anonymous avatar registration and could not prove a tenant-bound custodial
account. New onboarding uses `/api/azoa/custodial-account`; the legacy status
read remains only for migration visibility.

The legacy status read is still an object-authorization boundary. It resolves
the user only from the single verified `ActorAssertion` identity and does not
accept a route, query, or body user ID. Missing or ambiguous actor identities
fail before `IAzoaAvatarService` is called, which keeps direct controller use
and future filter changes from reintroducing an IDOR path.

## Sensitive mutation checkpoints

Token distribution, credential utility and membership lifecycle changes,
governance and vote-delegation changes, and referral reward claims are
intentionally fail-closed.
They must not invoke application services from public routes until authority is
bound at the backend boundary. Token distribution additionally requires an
atomic RESERVED-to-DISTRIBUTED transition and durable idempotency. Referral
rewards must be server-derived, beneficiary-bound, atomic, and idempotent.
Client-supplied grant reasons, actor IDs, reward amounts, or ownership checks in
the BFF are never sufficient authorization.

# Project creation authority

Project creation is gated inside `ProjectService`, not only in the browser or
controller. The service re-reads the tenant-bound Azoa account and requires the
full ready contract: stable avatar evidence, approved KYC, and a managed wallet
id and address. The aggregate `Ready` flag never substitutes for those fields.
