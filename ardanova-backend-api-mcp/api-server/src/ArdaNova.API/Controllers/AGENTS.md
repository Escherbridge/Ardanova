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

The self-sovereign avatar path is gone: `AzoaAvatarController` and its
`/api/azoa/avatar/ensure`, `/wallet`, and `/status` routes were deleted outright,
so those paths now 404 rather than 410. Avatar linkage was never a complete
account-readiness or value-eligibility signal, and registering a user against the
shared node posted their real email to a node ArdaNova does not own. Onboarding
and readiness reads use `/api/azoa/custodial-account`; value eligibility is
`IAzoaValueEligibilityService`.

## Collaboration identity

Chat and notification controllers require `ActorAssertion` at the controller
boundary. Conversation and message service calls always receive the signed
actor; a route, query, or body user ID is data, never the acting identity.
Notification records additionally require an ownership check after lookup and
before any content read, read-mark, or delete mutation. Public HTTP requests
cannot create arbitrary notifications; trusted application workflows call
`INotificationService` directly.

## Opportunity identity

Opportunity browsing, public updates, and public comments remain readable.
Every opportunity mutation is actor-bound. The controller replaces a supplied
poster, applicant, update author, or comment author with the signed actor and
checks project-manager or guild-manager authority before a person can post an
opportunity. A source-backed opportunity is managed only by a current manager
of that project or guild (or an administrator), so a former manager cannot
retain authority through a historical poster ID. A source-less legacy
opportunity remains poster-managed. A comment can additionally be deleted by
its signed author. Application, update, and comment lookup must occur before
mutation so an opaque child-record ID cannot bypass those parent ownership
rules. A comment reply may point only to a parent in the same opportunity.

Unpaid listings remain ordinary collaboration. Publishing or updating a
listing whose final terms include positive compensation or project units first
requires the signed manager to pass `IAzoaValueEligibilityService`; the API
returns safe account-setup guidance rather than making value promises.

## Sensitive mutation checkpoints

Project-unit allocation and distribution, credential utility and membership lifecycle changes,
governance and vote-delegation changes, and referral reward claims are
intentionally fail-closed.
They must not invoke application services from public routes until authority is
bound at the backend boundary. Token distribution additionally requires an
atomic RESERVED-to-DISTRIBUTED transition and durable idempotency. Referral
rewards must be server-derived, beneficiary-bound, atomic, and idempotent.
Client-supplied grant reasons, actor IDs, reward amounts, or ownership checks in
the BFF are never sufficient authorization.

Direct project-member share-balance and voting-power changes are also paused at
both the route and service layers. Role changes remain ordinary collaboration,
but project-unit and governance allocation require provider-backed settlement
and auditable controls before they may be enabled.

# Project creation authority

Project creation remains actor-bound in `ProjectsController`, but creating a
draft project is collaboration work rather than a value-bearing transition. Do
not require account setup for draft creation, project discussion, or ordinary
unpaid task planning. Funding, paid work, rewards, and every other action that
can create, move, receive, or promise value must use the server-derived
`IAzoaValueEligibilityService` after actor authorization. Its full readiness
contract requires stable avatar evidence, approved identity verification, and
a managed-account id and address; the aggregate `Ready` flag never substitutes
for those fields.

# Adding funds

The payer and the payment-reader are both derived from the signed actor assertion, never from a
body or query value, so nobody can read or top up another person's account by editing a
request.

The return address is composed from CONFIGURATION ONLY (`ArdaNova:ClientBaseUrl`, else
`NEXTJS_URL`); a blank setting refuses with 503 outside Development rather than accepting a
caller-supplied destination. That is the one setting between a working front door and a
refusing one, and blank resolves to the refusal explicitly in code.

The status route re-reads the payment FROM THE NODE and never infers an outcome from the
redirect the browser came back on: `funds_applied` is the only input that produces "Funds
added", and everything unrecognised produces "we can't confirm it". The read path deliberately
skips the readiness gate, so somebody held for review AFTER paying can still be told what
happened to their money.

Retries reuse ONE request key per attempt. The server resumes that person's open payment for a
repeated key, so a fresh key per tap would open a second payment page for one intent.
## Task visibility

Task and opportunity reads are anonymous-readable, so they open with the same project-visibility
check as `ProjectsController`: a task, task list or backlog-item task list on a project this viewer
may not see reads as 404 (Forbidden would confirm the draft exists). `PUT /api/tasks/{id}/assign` is
the task's assignment action: managers assign any project member or null; a non-manager member may
only claim an unassigned task for themselves or release their own.
