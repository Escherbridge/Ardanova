# Azoa transport boundaries

ArdaNova uses several typed HTTP clients so credentials cannot drift across trust
boundaries. There is deliberately no credential-free client: the anonymous
`AzoaPublicNodeClient` was deleted with the self-sovereign avatar path, because
its only caller registered the user's real email on a shared node ArdaNova does
not own.

- `AzoaCustodialNodeClient` sends `Azoa:CustodyApiKey` and accepts only
  `/api/tenant/custodial-accounts` routes. Its key is limited to
  `tenant:provision`, `wallet:manage`, `kyc:read`, and `kyc:submit`.
- `AzoaNodeClient` sends `Azoa:ValueApiKey` only to allocation, fungible-mint and
  reserve-partition routes. Those calls currently need `nft:mint` and
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

Production accepts AZOA as intentionally disabled only when no capability or
scoped credential is configured. Any partial capability configuration requires
an HTTPS base origin. Base URLs are origins, not arbitrary URIs: credentials,
paths, queries, and fragments are rejected so every typed client has one clear
authority and route policy.

Tenant custody remains anchored to the configured tenant GUID. Every returned
account status must match that tenant and the actor-derived ArdaNova user before
thin avatar, wallet, or KYC references are persisted.

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

`DisabledAzoaSettlementGateway` is what is registered when no node is selected to settle:
every capability answer (`CanDispatch`, `CanReconcile`, `HasCurrentOperatorAttestation`,
`SupportsCanonicalFundingSettlement`) is `false`, and it still implements
`ISelectedNodeSettlementCapability` on purpose — readiness is a type test
(`gateway is ISelectedNodeSettlementCapability`), so a disabled gateway that merely
omitted the interface would pass that gate by accident of typing rather than by declaring
what it cannot do.
