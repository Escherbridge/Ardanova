# Azoa transport boundaries

ArdaNova uses several typed HTTP clients so credentials cannot drift across trust
boundaries. There is deliberately no credential-free client: the anonymous
`AzoaPublicNodeClient` was deleted with the self-sovereign avatar path, because
its only caller registered the user's real email on a shared node ArdaNova does
not own.

- `AzoaCustodialNodeClient` sends `Azoa:CustodyApiKey` and accepts only
  `/api/tenant/custodial-accounts` routes. Its key is limited to
  `tenant:provision`, `wallet:manage`, `kyc:read`, and `kyc:submit`.
- `AzoaNodeClient` sends `Azoa:ValueApiKey` only to allocation, fungible-mint, the
  fungible-creation lifecycle steps (`/api/fungible-creation/{operationId}/quote|accept-quote|continue|refresh`)
  and reserve-partition routes. Those calls currently need `nft:mint` and
  `token:create:sign`; the key carries no onboarding or quest authority. Its route
  allowance lives in `AzoaNodeRoutePolicy` in the same file, a SECOND policy type beside
  `AzoaNodePathPolicy` in `AzoaCustodialNodeClient.cs`. The two exist only because a wave
  could not reopen the file holding the first; consolidating them is owed.
- `AzoaProjectTokenNodeGateway` needs BOTH keys: custody to provision, value to sign a
  project token into existence.
- `AzoaCredentialIssuanceClient` sends `Azoa:IssuanceApiKey`, a FOURTH credential whose
  owner must carry the `dapp:manager` role and the `nft:mint` scope. When it does not
  resolve, no issuance node is registered at all and a member card reads "Verification
  pending" rather than claiming a proof ArdaNova does not hold. AZOA has no replay ledger
  on the credential-issue route yet — only the allocation door has one — so the adapter
  sends a stable `Idempotency-Key` and says in its own remarks that the guarantee becomes
  real when the node covers the route, rather than claiming an idempotency it lacks.
- `AzoaCashOutClient` uses the custody key; the node reads the tenant off the key owner.
- `AzoaQuestNodeClient` sends `Azoa:QuestApiKey` only to `/api/quest` routes.
  Authoring requires `dapp:develop` and an API-key owner with the persisted dApp
  Developer role. Current read/validate/signal routes require authentication but
  do not enforce an additional quest scope.

`Azoa:TenantApiKey` is a migration fallback only outside Production. Production
ignores it, requires explicit keys for enabled capabilities, and rejects the
same secret across custody, value, quest and issuance clients. The fourth-key
separation check currently lives in `Infrastructure/DependencyInjection.cs`, not in
`AzoaCredentialSelection.AreCredentialsSeparated`, which still compares three. Redirect following stays disabled
for every client so a scoped key cannot cross origins.

Production credentials also pass the shared generated-secret policy: at least
32 UTF-8 bytes, no placeholder markers, and no published README examples. Scope
names describe authority; they are never usable secret values.

All credential-bearing clients reject absolute destinations, query/fragment
suffixes, dot segments, and encoded path delimiters before network I/O. This
prevents URI normalization from moving a scoped key into another route family.

Node response messages are provider-controlled and never cross into API
results verbatim. They can contain stack traces, database details, or secret
material even when wrapped in a nominal error envelope. The transport maps
status codes to stable public messages and logs only request metadata. The
`KYC_FORBIDDEN` signal remains part of the public contract, but its provider
detail is replaced with a fixed message.

## Failure codes

The node's prose is collapsed; its machine `code` is not. `AzoaNodeClient.MapError` copies the
envelope's `code` onto the result (`Result<T>.Code`, see `ArdaNova.Application/Common/AGENTS.md`
§Failure codes), so `KYB_REQUIRED` and the same-key outcomes (`INTENT_FINISHED`,
`INTENT_EXPIRED`, `REQUEST_KEY_IN_USE`, `INTENT_NEEDS_REVIEW`, `REQUEST_VOIDED`, `RAIL_REFUSED`)
reach `MoneyRefusal` as facts rather than call-site guesses. One prose signal is kept on purpose:
the node checkout's KYC refusal shares the consent refusal's `FORBIDDEN` code, so a 403 whose
sentence says the account "has to finish identity verification" collapses to the public
`KYC_FORBIDDEN:` message (`CheckoutKycMarker`) and reads as `setup_required`, not `consent_revoked`. Two labels are the transport's own: a 429 is always
`ARDANOVA_UPSTREAM_RATE_LIMITED` (the only failure anyone may call "busy"), and a route guard
refusing to send is `ARDANOVA_ROUTE_NOT_ALLOWED` - a configuration fault, never a person's KYB.
An empty, unparseable or payload-less 2xx, a timeout and a dropped connection carry NO code: the
request may have been admitted, so they surface as `unconfirmed`, never as busy or final.

Production accepts AZOA as intentionally disabled only when no capability or
scoped credential is configured. Any partial capability configuration requires
an HTTPS base origin. Base URLs are origins, not arbitrary URIs: credentials,
paths, queries, and fragments are rejected so every typed client has one clear
authority and route policy.

Tenant custody remains anchored to the configured tenant GUID. Every returned
account status must match that tenant and the actor-derived ArdaNova user before
thin avatar, wallet, or KYC references are persisted.

# Fungible-mint consent lifecycle

`POST /api/nft/fungible-mint` no longer answers with a finished asset. The node
(`NftController.FungibleMint` → `FungibleTokenManager.CreateCoreAsync`) admits a durable
creation and returns `FungibleTokenResult.operation` (a `FungibleCreationView`), usually
`202 AwaitingConsent`. `AzoaBackedAlgorandService.DriveCreationAsync` walks it to a
terminal view on `FungibleCreationController` (`api/fungible-creation`):

| View state | Step the adapter posts | Body |
| --- | --- | --- |
| `AwaitingConsent`, no `quote` | `{id}/quote` | none |
| `AwaitingConsent`, `quote` set | `{id}/accept-quote` | `{ version, quoteId }` from that view |
| `Admitted` / `Preparing` / `Prepared` | `{id}/continue` | `{ requestId, version }` |
| anything else non-terminal (`Submitted`, `NeedsOperator`, `ProjectionPending`, ...) | `{id}/refresh` after a poll delay | none |

Terminal: `Completed` (asset id read from THAT view, never the mint's top-level field),
`Cancelled` / `FailedNoEffect` (failure, nothing created). The node answers 200 only for
those three and 202 otherwise; both are success envelopes to the transport.

- **Minted chain.** The terminal view's `chainType` is surfaced as `FungibleAsaCreateResult.ChainType`.
  A `Blockchain:Mode=Simulated` node redirects an `Algorand` request and reports `Simulated`;
  `ProjectTokenService` persists that to `config.assetChain` and resolves the treasury and declares
  partitions on it (falling back to `Azoa:ChainType` only when no chain is reported). A config row
  minted BEFORE this change has `assetChain = Algorand` frozen and will keep asking there.
- **Idempotency-Key.** The node refuses a blank key with 400. `ProjectTokenService` sends
  `project-token-genesis:{config.id}` — one key per config row, so every retry of a
  declaration replays the same operation (and resumes it mid-lifecycle, since the loop
  starts from whatever state the replayed view reports). Only the mint carries the key;
  the step routes are addressed by operation id and version instead.
- **Re-declaration caveat.** `ProvisionAsync` lets an owner re-declare while `assetId` is
  still empty, but the node binds a key to its first intent ("Creation request key belongs
  to a different intent"). That refusal is deliberate here: a re-declaration after a
  half-finished creation must not start a second asset. Freeing a stuck key (node cancel,
  then a new key) is owed, not built.
- **Continuation request id** is a SHA-256-derived Guid of `(operationId, version)`, so a
  retried continue at the same version is the same authorization on the node's audit row.
- **Budget.** `AzoaFungibleCreationBudget.Default` = 30 s total, 1 s poll. A step that does
  not move the state also waits the poll delay, so a stalled step polls instead of spinning.
  On exhaustion the failure names the last state and says a retry resumes; the caller
  (`RecordSetupFailureAsync`) records it. No retries beyond the budget.
- **Route policy.** `AzoaNodeRoutePolicy.IsFungibleCreationStepPath` admits exactly those
  four steps for one addressable operation-id segment (letters, digits, `-`, `_`). The
  path is built by `FungibleCreationStepPath`, which refuses — never escapes — an id that
  fails that rule. `replace`, `cancel` and `attachment` are not on the value key.

# Hold commands

`AzoaHoldClient` (value key) speaks the node's keyed hold door, `HoldCommandsController`
(`api/hold-commands`). There is no `/api/holds` route on the node; the old paths 404'd. Every
award hold is reserve-funded, so each command is a LOCAL node transition that answers 200
`Completed` synchronously, with no wallet and no quote.

| Port call | Node calls | Intent |
| --- | --- | --- |
| `OpenAsync` | `GET reserve-assets`, then `POST /api/hold-commands` | `Open`, `definition.fundingSource = "Reserve"` |
| `ReleaseIntoHoldAsync` | `GET reserve-assets`, `GET liabilities/{recordId}`, `POST` | `ReleaseIntoHold` + `next*` |
| `RefundAsync` | same reads, `POST` | `Refund` + `reason` |
| `GetAsync` | `GET reserve-assets`, `GET liabilities/{recordId}` | — |

- **Body.** `{ intent: HoldCommandIntent, senderWalletId: null, recipientWalletId: null }`. The
  intent's `actorUserId`/`ownerUserId` are `Azoa:TenantId` (the tenant user the value key belongs
  to); `tenantId`/`custodyTenantId` are null because a plain tenant key carries no acting-tenant
  claim, and the node refuses an intent whose tenant differs from the captured credential's.
  `asset` (`HoldAssetIdentity`: chain, network, protocol identity, asset id, holding address) is
  echoed verbatim from the node's own `reserve-assets` / liability read. ArdaNova never builds one.
- **Request keys** are deterministic (`open:{holdId}`, `refund:{holdId}`,
  `release-into-hold:{holdId}:{nextHoldId}`), so a redelivery replays the original receipt. A
  replay with a different intent (for example a recomputed window date) is refused by the node.
- **Response.** `HoldCommandView` reports COMMAND state. A `Completed` command maps to the hold
  state it implies (`Open`→`Held`, `Refund`→`Refunded`, `ReleaseIntoHold`→`ReleasedIntoHold`).
  `Cancelled`/`FailedNoEffect` map to a decline. Anything else maps to `Pending`.
- **Addressing.** The node addresses a hold by `holdRecordId` = SHA-256 over canonical JSON of
  `{Domain:"hold-custody-v1", ChainType, Network(ordinal), ProtocolIdentity, CustodyTenantId,
  OwnerUserId, HoldId}` (node `HoldCommandIdentity.Hold`). `AzoaHoldRecordIdentity` mirrors it, and
  both repos pin the same literal vectors (`AzoaHoldClientContractTests` here,
  `HoldCommandIdentityContractTests` on the node). Each network the tenant's reserve exposes on
  the rail is one candidate. A 404 moves to the next candidate, and any other failure stops.
- **Auto-release.** The direct door refuses `autoRelease`/`nextAutoRelease = true` (an automatic
  release needs a release mandate, `api/hold-release-mandates`, which ArdaNova does not call). The
  client refuses it before the wire, and the worker window is enforced by ArdaNova's
  `UnitWindow`. Releasing a window to the worker's wallet is owed, not built.
- **Route policy.** Exactly `POST /api/hold-commands`, `GET /api/hold-commands/reserve-assets` and
  `GET /api/hold-commands/liabilities/{64 lowercase hex}`. The key needs `transfer:sign`.

# On-ramp

`AzoaFiatRailGateway` is the ONE adapter ArdaNova uses to add funds: it calls the node's
`Fiat.USD` rail and nothing else, using the custody key under the same route-family
restriction as every other typed client in this directory (`CheckoutPrefix`,
`PriceSchedulePath`). ArdaNova never holds a card credential, never terminates a payment
webhook, and never names a payment provider anywhere a person can read.

`ArdaOnRampOptions` (section `Azoa:OnRamp`, defined in `AzoaFiatRailGateway.cs`) is this
deployment's own on-ramp configuration, separate from `AzoaSettings`: `Enabled` is the
master switch — off leaves the seam bound to its fail-closed refusal; `AssetId` names the
ARDA asset the money buys, blank meaning nothing is registered; `UnitsPerUsdMinorUnit`
(default `"100"`) is the ONE-TIME genesis price registration — 1 ARDA = $0.01 — not a
per-tick rate, because the live rate is backing-derived from that seed (users think in
dollars; ARDA is at most a derived caption, never the unit a sponsor is asked to reason
in); `MinimumAmountMinorUnits`/`MaximumAmountMinorUnits` bound one payment ($1–$10,000 by
default).

`VoidRequestAsync` is the request-key void (oasis-sleek f2cb4fe8): `POST
/api/fiat/checkout/{account}/requests/{key}/void`, no body, the key ONE escaped segment of the
node's own alphabet (`IsCheckoutVoidPath` refuses anything else, and a dot-only key never leaves).
It returns `voided` or `revealed` (with the same projection a read returns); an outcome word it does
not know, or a reveal it cannot describe, is a `Conflict` - never read as "voided".

`ArdaOnRampPriceScheduleRegistrar` declares that price to the node exactly once, after
boot. It never blocks startup and never throws: an unregistered price makes the on-ramp
issue nothing, which is the fail-closed outcome, so a loud log is the right escalation —
the same shape every other AZOA seam here takes: a refusal to serve, never a refusal to
boot.

# Settlement gateway

`AzoaSettlementGateway` settles ArdaNova's own immutable economic decisions as AZOA
exchange operations — one node call per settlement, carrying token base units only. It
never learns a fiat amount: a settlement whose asset code IS a currency (`FiatAssetCodes`:
USD, EUR, GBP, …) is refused here before the call is made, never sent to the node as if it
were a token unit. Everything the exchange needs beyond the settlement row — the project,
its asset, the reserve partition, both legs, and the declared rate — travels in the
settlement's own immutable terms snapshot, locked at authorization time; unreadable terms
are a refusal here, never a guess.

Reconciliation reads `GET /api/exchange/receipt/{idempotencyKey}`, not an operation id —
the allocation receipt route cannot resolve one, so the durable idempotency key is the
only handle that survives an ambiguous dispatch. Amount accounting is a control, not a
log line: gross is what the payer side gave up, net is what the beneficiary side
received, and the difference is the fee the node kept. A receipt that lands MORE than
ArdaNova authorised is not a bonus — it is unreconciled, and dispatch stops rather than
accepting it.

Only the node's own not-found envelope (`{"isError":true,"code":"NOT_FOUND"}`, HTTP 404) maps a
receipt read to `NotFound`. Any other 404 - a proxy page, an unrouted path on an older node - is
an ambiguous failure (`AzoaNodeClient.SendAsync(..., notFoundNeedsNodeCode: true)`), because the
settlement outbox redispatches on `NotFound` and the payout path unlocks a seller's units on it.
See `Outbox/AGENTS.md`, "No-effect reopen".

**Admission deadline (round 5 H2).** A dispatch whose `AzoaSettlementRequest.AdmitBefore` is set
sends it as the `Admit-Before` header (`AzoaExchangeClient`; strict UTC, whole seconds; never in the
hashed body), and is not sent at all once that instant has passed on this host's clock. The node's 409
`ADMISSION_DEADLINE_PASSED` maps by its CODE to a Retry (`AZOA_ADMISSION_DEADLINE_PASSED`, "this attempt
admitted nothing"); every other 409 on the route stays ambiguous. That is why `AzoaExchangeClient.MapFailure`
now keeps the node's code (it used to drop it). The windows come from `Azoa:Exchange:*`
(`AzoaExchangeAdmissionSettings`), validated at boot (W > W_admit + skew); the same registration
(`AddAzoaExchangeProof`) runs the P6-03 value-key boot check. Rationale and the clock assumption:
`Application/Services/Implementations/AGENTS.md` §Node proof.

`DisabledAzoaSettlementGateway` is what is registered when no node is selected to settle:
every capability answer (`CanDispatch`, `CanReconcile`, `HasCurrentOperatorAttestation`,
`SupportsCanonicalFundingSettlement`) is `false`, and it still implements
`ISelectedNodeSettlementCapability` on purpose — readiness is a type test
(`gateway is ISelectedNodeSettlementCapability`), so a disabled gateway that merely
omitted the interface would pass that gate by accident of typing rather than by declaring
what it cannot do.
