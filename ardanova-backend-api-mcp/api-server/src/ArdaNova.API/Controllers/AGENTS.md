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

# Money refusal codes

`MoneyRefusal.cs` gives every refusal on the money routes (`AddFundsController`,
`AzoaCustodialAccountController`, `PayoutsController`, `FundingIntentsController`) a body of
`{ error, code, message, retryAfterSeconds, reason }`. `code` is one of `MoneyRefusalCodes`.

**The key-release contract** (memory: money-key-doctrine). A key may be dropped only where its
emitter PROVED nothing was or can be paid under it. The HTTP status is NOT the signal: on these
routes 400/403/409 each mean several things, and `ExceptionHandlingMiddleware` turns an
`ArgumentException` or a SQLSTATE 22xxx/23503/23514 into a code-less 400 that can arrive AFTER the
node already moved funds. `definite` is emitted only for:

- `ValidationError`/`BadRequest` - local input checks, the sponsorship quote/fundability/supply
  refusals (all before the intent is persisted), the node's 422 decline, and its uncoded or
  INVALID_REQUEST 400s (raised before its claim).
- The node's `REQUEST_VOIDED` (reason `voided`: the key was voided) and `INTENT_EXPIRED`
  (`expired`: sent only after a fresh Stripe read proves nothing was paid, oasis-sleek cee103bd).
  `FundingIntentService` labels a voided sponsorship key with the same `REQUEST_VOIDED`.

`finished` (new in round 3, C-1) is NOT a release: `INTENT_FINISHED` (reason `finished`) is sent
for terminal intents INCLUDING paid ones, and `REQUEST_KEY_IN_USE` (`key_in_use`) names a request
that may itself have been paid. The caller reads the admitted record, or asks the void route
below. The node's `RAIL_REFUSED` is a 400 raised AFTER it claimed the key, so it is `unconfirmed`
(reason `rail_refused`, L-5). In-progress 409s carry no code and stay `unconfirmed`. The browser
treats even `definite` as "ask the void route" when it holds no server id (client
`lib/commerce/AGENTS.md` §Persisted request keys).

`needs_review` (round 4 #3) is the node's INTENT_NEEDS_REVIEW on begin AND void: a payment held for a
manual check. It releases nothing, and unlike `unconfirmed` it must not invite a resend (the same key
only meets the same hold). Its body carries `intentId` - the compact id of the held payment, kept from
the node's 409 `result` (`AzoaNodeClient.SendAsync(failureReference:)` -> `Result<T>.Reference`) and
set on no other code - so the page records it, locks Continue and tells the person which payment to
quote. `ForFailedVoid` leaves it as is: it is neither final-looking nor a release.

## Request-key void

`POST /api/add-funds/requests/{requestKey}/void` (`AddFundsController.VoidRequest`) and
`POST /api/funding-intents/requests/{requestKey}/void` (`FundingIntentsController.VoidRequest`)
are the only answers a caller may discard a key on: 200 `voided` (nothing was or can be paid under
it) or 200 `revealed` (keep the key, follow the named payment / sponsorship). Both are actor-scoped
- the account comes from the signed actor, never the request. Add funds proxies the node's own
void (oasis-sleek `FiatCheckoutController.VoidRequest`; it expires an open Stripe page first) and
deliberately skips the readiness gate, like `GetPayment`. A FAILED void proved nothing:
`MoneyRefusal.ForFailedVoid` turns a final-looking code into `unconfirmed`, so 503
DEPENDENCY_UNAVAILABLE / RAIL_BUSY, 409 INTENT_NEEDS_REVIEW / REQUEST_KEY_IN_USE, a 400, and a 200
with an unknown outcome word all keep the key.

**Resolution order** (`MoneyRefusal.From`): the upstream `Result<T>.Code` first, then the
`ResultType`, then message markers, then the call-site context as a last fallback.

| Signal | Code |
| --- | --- |
| `KYB_REQUIRED` (node) | `kyb_required` |
| `REQUEST_VOIDED` / `INTENT_EXPIRED` | `definite` (reason `voided` / `expired`) |
| `INTENT_FINISHED` / `REQUEST_KEY_IN_USE` | `finished` (reason `finished` / `key_in_use`) |
| `RAIL_REFUSED` (node 400 after its claim) | `unconfirmed` (reason `rail_refused`) |
| `INTENT_NEEDS_REVIEW` | `needs_review` (reason `needs_review`), body `intentId` = the held payment |
| `PAYMENTS_NOT_CONFIGURED`, BFF route-guard / unconfigured transport | `payments_not_configured` |
| BFF `ARDANOVA_UPSTREAM_RATE_LIMITED` (a real node 429) | `rail_busy`, `retryAfterSeconds` 30 |
| `Unauthorized` | `unauthenticated` |
| `NotFound`, `Conflict` | `unconfirmed` |
| `ValidationError`, `BadRequest` | `definite` |
| `Failure` (timeout, empty/unparseable/no-payload 2xx, dropped connection) | `unconfirmed`, HTTP 503 |
| `Forbidden` + "review team is finishing one last check" | `under_review` |
| `Forbidden` + `KYC_FORBIDDEN:` (also never-finished KYC, and the node checkout's "has to finish identity verification", L-4) | `setup_required` |
| `Forbidden`, add-funds checkout (units:issue consent probe) | `consent_revoked` |
| `Forbidden`, custodial lifecycle, UNCODED | `kyb_required`; coded non-KYB → `payments_not_configured` |
| `Forbidden`, payout destination SETUP (F-H8, POST only) | `account_holder_required` |
| `Forbidden`, any other cash-out call (incl. withdraw, destination read) | `setup_required` |

`rail_busy` is ONLY a real 429; nothing that might have been admitted is ever called busy. A
`Failure` is the platform's fault, never the caller's request, so every money controller answers
it with 503 (the error-envelope filter passes 503 bodies through unchanged).
## Task visibility

Task and opportunity reads are anonymous-readable, so they open with the same project-visibility
check as `ProjectsController`: a task, task list or backlog-item task list on a project this viewer
may not see reads as 404 (Forbidden would confirm the draft exists). `PUT /api/tasks/{id}/assign`
applies the same check before any permission or assignee check, so a hidden or missing task both
answer 404 with the same body (otherwise its 403-vs-400 split is an existence oracle). It is
the task's assignment action: managers assign any project member or null; a non-manager member may
only claim an unassigned task for themselves or release their own.

`PUT /api/tasks/{id}`, `PATCH /api/tasks/{id}/status` and `DELETE /api/tasks/{id}` follow the same
404-first rule through `GetVisibleTaskAsync`; only a visible task can then be refused with 403.

The epic, sprint, feature and backlog-item controllers apply the same 404-first check to every read.
That includes the by-parent lists (`/milestones/{id}/epics`, `/epics/{id}/sprints`,
`/sprints/{id}/features`, `/features/{id}/product-backlog-items`), whose 404 names the parent. See
`ArdaNova.Application/Services/Implementations/AGENTS.md` §Hierarchy read visibility.

`GET /api/opportunities/{id}/management` returns `{ canManage }` for the signed-in viewer, using the
same check as every management action. Visibility comes first: a hidden role returns 404, not
`false`. The opportunity detail and edit pages call this endpoint, so co-managers get the hiring
panel too, not only the poster.

## Member disclosure

`GET /api/projects/{id}/members` and `GET /api/projects/{id}/members/{memberId}` are anonymous-readable,
so a member's `user.email` is filled only for an admin, a project manager, or that member's own row.
Everyone else receives `email: null`, and the UI falls back to the member's name. This is the only
member-email rule: `UserDirectoryPrivacyFilter` leaves `ProjectMemberDto` alone, because it cannot tell
a manager from a stranger. The add and update member routes are manager-only, so they return the full row. The single-member read also checks that the
row belongs to the routed project, the same check the update and remove routes make. Without it, a
member of a hidden draft could be read through any public project's URL.

## Profile ownership

Skill and experience writes (`UserSkillsController`, `UserExperienceController`) require
`ActorAssertion` and a route `userId` equal to the signed actor. The service also limits update
and delete to that owner and returns `NotFound` for a row that belongs to someone else, so an
opaque child ID cannot be used to probe or change another person's profile. Reads stay public.
`POST /api/users/{id}/verify` is retired (410) because a person marking their own account
verified is not evidence; AZOA KYC is authoritative. Owner denials return an explicit 403 body
instead of `Forbid()`.

## Bid privacy

Opportunity bids carry a private proposal and price. Every bid read requires `ActorAssertion`: the
per-opportunity list is for that opportunity's managers (project managers for a project role, else
the poster) and answers 404 to everyone else; a single bid is readable by its bidder or a manager;
the guild listing returns only the actor's own bids. The bidder's email is never part of a bid
(`OpportunityBidService` nulls it), matching the public-user redaction elsewhere. Role hiring
(apply, accept, seat as member) uses opportunity applications; bids are for paid task work and go
through task commerce, which needs a positive award.
