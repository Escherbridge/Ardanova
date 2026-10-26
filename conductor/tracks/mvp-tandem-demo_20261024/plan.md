---
type: plan
---

# Plan — MVP Tandem Demo

Three workstreams. A and B are disjoint by repo and run in parallel; C drives the stack
and is the integration truth-finder. Fix batches found in C are applied in sweeps, with
the full test/lint sweep ONCE at the end of each round (user policy).

## Interface contract (fixed up front, both sides build to it)

- WS-A produces `oasis-sleek/.omc/state/dev-node-env.json`:
  `{ "baseUrl", "tenantApiKey", "tenantId", "kyc": { "beginBaseUrl", "resultBaseUrl", "companyKey", "configId", "callbackApiKey" }, "surrealEndpoint" }`
  (`tenantId` added 2026-10-24: ArdaNova's `ValidateCustodialAccounts` throws at boot when
  `Azoa:EnableCustodialAccounts=true` without a canonical GUID in `Azoa:TenantId` —
  `DependencyInjection.cs:396`.)
- **Contract amendment (2026-10-24, from WS-A):** the node has no route minting one
  8-scope key — the operator route issues a fixed 4-scope set (`ApiKeyController.cs:58-59`)
  and `tenant:provision` can't be self-issued. The JSON therefore carries TWO keys:
  `tenantApiKey` (provisioning scopes) and `tenantValueApiKey` (the 7 value scopes,
  self-issued by the tenant user). ArdaNova side maps them onto its split keys
  (`Azoa__CustodyApiKey` ← tenantApiKey; value/quest/issuance ← tenantValueApiKey)
  instead of the single-key fallback.
  plus an idempotent `oasis-sleek/tools/dev-node/dev-node-up.ps1` (starts SurrealDB +
  KYC sidecar + WebAPI, mints/reuses the tenant key, writes the JSON) and `dev-node-down.ps1`.
- WS-B's `local-stack.ps1 -WithAzoa [-AzoaRepo <path>]` invokes that script, reads the
  JSON, and maps it onto `Azoa__*` env for the BFF. WS-B owns discovering the exact
  ArdaNova-side key names (`AzoaSettings.cs`) including whatever satisfies
  `SelectedSettlementNodeId`, the gateway's `SelectedNodeId`, and the operator
  attestation expiry.

## WS-A — AZOA node demo readiness (owns `oasis-sleek/**`)

- [~] **A1** *(2026-10-24: `node-admin-release` + `tenant-economic-workflow-ai` removed
  (clean); `.worktrees/main-release` LEFT — it holds 3 uncommitted lines of operator
  material (a NodeAdmin root public key + fingerprint in
  `node-admin-rbac-cli_20261018/owner-live-drill.md`). Guard failures 7→5, all from
  `main-release`. OWNER DECISION: commit or discard that note, then
  `git worktree remove .worktrees/main-release`.)* Remove the three stale worktrees.
- [x] **A2** *(done — build + typecheck exit 0 at 0.5.0; not published)* Rebuild
  `sdk/azoa-wallet` so junction consumers get round-4 code.
- [x] **A3** *(done — kept the committed :8020 pin; `dev-node-up.ps1` exports
  `SURREALDB_HOST_PORT=8020` and passes `SurrealRuntime__Endpoint` explicitly; compose
  default untouched for `dev-up.ps1`'s own path)* Align the SurrealDB port.
- [x] **A4** *(done 2026-10-24 — `tools/dev-kyc-vendor/server.mjs`, dependency-free Node,
  two listeners (API host :8031 — /begin, KYB /result, 4 AML endpoints; widget host
  :8032 — /external-api/result/ + delete + applicant page), modes
  approve-on-begin/approve-on-poll/manual, verdict control endpoint, keyed callbacks at
  1.5/5/15 s. Allowance `Services/Kyc/KycRuntimeSafety.cs:21-39`: Development AND
  IsManualSimulationAllowed AND `Kyc:Identomat:AllowInsecureDevVendor=true`, loopback-only
  (private/link-local/metadata still refused); applied at
  `IdentomatKycProviderService.cs:526-537` (+ redirect same-host check),
  `IdentomatKybProvider.cs:126` (KYB needed per D-6), and the registry now asks the
  adapter for missing keys instead of duplicating the rule
  (`KycProviderRegistry.cs:387`). 25 origin tests + sidecar 5/5. Adversarial review
  PENDING — required before the demo is called done.)* Dev KYC vendor sidecar.
  (`tests/.../Http/Infrastructure/IdentomatVendor.cs` is the reference implementation —
  `/begin`, result endpoints on both hosts' shapes, callbacks, AML screening surface) as a
  small standalone dev-only service under `tools/dev-kyc-vendor/`, with an auto-approve
  mode and a control endpoint to set verdicts. **Beware:** the root csproj globs
  `**/*.cs` — either write it in TypeScript/Node or exclude the path in the csproj.
  Includes a Development-only allowance for a loopback vendor origin at
  `IdentomatKycProviderService.ValidHttpsOrigin` (or a config seam), with a regression
  test proving Production still refuses loopback/internal hosts.
- [x] **A5** *(done 2026-10-24 — up: SurrealDB (either engine; `-StartPodmanMachine`
  helper) → surrealforge schema → sidecar → `dotnet <dll>` (not `dotnet run`, so the
  recorded PID is the real server) → health wait → mint-or-reuse TWO keys (see contract
  amendment) → enable+assign Identomat KYC profile → best-effort tenant KYB → write
  `dev-node-env.json`. down: stops only recorded PIDs with start-time match; never
  name-matches. Smoke: parses under PS 5.1; podman-stopped exits 1 in 3 s clean; sidecar
  start/own/down cycle live-tested. FIRST FULL LIVE RUN DEFERRED to WS-C — podman machine
  was stopped. Secrets/logs in oasis-sleek `.omc/state/dev-node/`.)*
  `tools/dev-node/dev-node-up.ps1` / `dev-node-down.ps1` per the interface
  contract: SurrealDB (podman/docker compose), sidecar, WebAPI (`Blockchain:Mode=Simulated`,
  `CustodialAccounts:CustodyMode=DevelopmentOnly`, `Kyc__Identomat__*` → sidecar), mint the
  tenant key via `NodeOperator` credentials with scopes
  `tenant:provision,wallet:manage,kyc:read,kyc:submit,units:issue,nft:mint,token:create:sign,grant:sign`,
  write `dev-node-env.json`. Health-check loop; everything it starts, `down` reaps.

## WS-B — ArdaNova stack wiring (owns `ardanova/.omc/scripts/**`, `ardanova-client/scripts/seed-local-demo.ts`, `ardanova-client/src/trpc/query-client.ts`)

- [x] **B1** *(done 2026-10-24 — env matrix discovered incl. `Azoa__SettlementAttestationExpiresAt` (now+365d), `Azoa__SelectedSettlementNodeId=local-dev-node` (self-consistent: the gateway reads the same setting, `DependencyInjection.cs:146`), `Algorand__Provider=Azoa`; custody flag guarded off until `tenantId` arrives from WS-A)* Extend `local-stack.ps1` with `-WithAzoa`: invoke WS-A's script, map
  `dev-node-env.json` → `Azoa__BaseUrl`, `Azoa__TenantApiKey`,
  `Azoa__EnableCustodialAccounts=true`, `Azoa__EnableFundingCheckout=true`,
  `Azoa__EnableSettlementOutboxWorker=true`, `Azoa__SelectedSettlementNodeId=<discover>`,
  attestation expiry, and the correct `Algorand:Provider` value for the AZOA-backed
  service. Verify by hitting the BFF's economy-mode health field: must say **simulated**,
  not disabled. `down` also tears down the node stack.
- [x] **B2** *(done 2026-10-24 — `demo-creator`/`demo-sponsor`/`demo-freelancer`, project `solar-micro-grid-commons` (PUBLISHED, no token config), opportunity `solar-micro-grid-wiring-packet` (FREELANCE, OPEN, comp 1500); idempotent; seed guard intact)* Seed personas + demo fixtures: creator/sponsor/freelancer users, one
  demo project WITHOUT funding config (the wizard provisions it live in the demo), one
  opportunity with a paid role. Custody/KYC provisioning is NOT seeded — it runs through
  the real APIs in WS-C so the demo exercises the true path.
- [x] **B3** *(done 2026-10-24 — `retry: shouldRetryQuery` at `query-client.ts:19`, queries only; wiring test added (NOT_FOUND no-retry / network-error retry through the real QueryClient); 26/26 tests, `tsc --noEmit` clean)* Wire `shouldRetryQuery` into `src/trpc/query-client.ts` per
  `design-loop4-followups_20261018/spec.md` — kills the ~10.8 s stall on every
  missing-config 404 during the demo drive.

## WS-C — Golden-path drive (after A+B; integration truth)

- [x] **C0** *(done 2026-10-24 — `ardanova-dev-persona` cookie override inside the existing
  dev-bypass guards; resolves only rows in the seed-owned `@local.ardanova.test` email
  namespace; malformed/foreign cookie refuses (null session) instead of falling back to
  the steward; absent cookie keeps the fixed identity. Tests 13/13 through the real tRPC
  `protectedProcedure` seam; rationale in `src/server/auth/AGENTS.md` §Development
  preview — persona switching)* Persona switching for the browser drive: `DEV_AUTH_BYPASS`
  resolved to the single hard-coded `local-preview-user`; Playwright now sets the cookie
  per browser context to act as creator/sponsor/freelancer.
- [x] **C1** *(done 2026-10-24/25 — full tandem stack healthy: SurrealDB :8020, AZOA node
  :5000, KYC sidecar :8031/:8032, BFF :5147 reporting `"economy":"simulated"`, client
  :3000. Bring-up fixes: (1) node `UserLoginValidator` requires Email although
  `UserLoginModel` documents a UserId-XOR-email login — the UserId path 400s; the
  dev-node script logs in by email, and the validator mismatch is recorded as an AZOA
  bug; (2) ArdaNova's `IsSafeHostedVerificationUrl` was https-only — added
  `Azoa:AllowInsecureDevVerificationUrl` (dev-only, boot-refused in Production at
  `DependencyInjection.ValidateCustodialAccounts`), mirroring the node's A4 seam.)*
  Bring up the tandem stack.

### C2 findings ledger (live drive, in progress)
- **Step-3 milestone (2026-10-25):** the sponsor paid a REAL $25 Stripe test-mode checkout
  through the hosted page (test card, Link save unchecked — its phone requirement was the
  only snag), returned to the honest "IN PROGRESS — we'll email you" state, and the
  webhook chain delivered: `checkout.session.completed` reached the node via the CLI
  listener. This closes the stub-closeout's deferred "Stripe test-mode drill never run
  live". D7's operator route + `on-ramp` partition (1,000,000,000 base units) landed
  first; the asset/chain for the on-ramp now travels via `ardaAssetId`/`ardaChainType`
  in the contract JSON.
- **F21 (node, FIXED 2026-10-25 — the $25 SETTLED: intent `Issued`, 250000 base units,
  op `Transferred`):** TWO bugs, and the scope hypothesis was right but REVERSED — KYC
  lookups match the EXACT scope of the approval; the sponsor's only approval is
  tenant-scoped, and while the handler's pre-check used `(subject, tenantId)` (passed),
  `AllocationManager.cs:180` re-checked `(sponsor, actingTenantId=null)` = GLOBAL → no
  row. (Fungible-mint passed because ITS actor is the tenant, whose approval IS global.)
  Fix: optional `identityTenantId` on the allocation seam — `actingTenantId ??
  identityTenantId` decides ONLY where the approval is looked up, never signing/consent
  authority. Second bug: the Simulated provider had no RECIPIENT-PREPARATION module
  (`...RecipientPreparation.cs` added: every sim address can receive). Plus a recovery
  seam: a failed issuance replayed forever under its idempotency key; operator resume now
  releases a claim ONLY when it carries the `allocation:no-value-effect:v1` marker
  (`TryReopenAfterNoEffectAsync`, same conditional-delete pattern as refund reservation),
  uncertain failures stay blocked. One AUDITED manual SurrealDB edit set that marker on
  the pre-fix failure row (verified no-effect first: no IssueUnits op, partition at full
  budget). 158/158 tests. Residuals: recipient-prep failures still record without the
  marker; a Stuck lot needs two resumes; one pre-existing unrelated AllocationManager
  Transfer-test failure; tenant API keys were exposed once in agent output — rotate the
  dev keys (re-mint for the SAME tenant user) at wrap-up.
- **F22 (ardanova, OWNER FLAG + dev-fixed):** $25 bought 2500.00 ARDA — the gateway's
  in-code default `UnitsPerUsdMinorUnit="100"` (AzoaFiatRailGateway.cs:377) is a
  $0.01/ARDA peg, contradicting product.md AND the add-funds page ("$1 of ARDA is worth
  $1 of backing"; with 2dp ARDA the peg is 1 base unit per cent). Dev launcher now sets
  `UnitsPerUsdMinorUnit=1` + `AllowPriceUpdate=true` (registrar writes v2). OWNER must
  ratify which side is right (Q-X2's resolved value vs product copy) and fix the default
  or the copy. The sponsor's windfall lot (2500.00 ARDA from v1) stays on the dev ledger.
- **F24 = launch-readiness L2.5, CONFIRMED LIVE → delegated (2026-10-25):** the invest
  estimate fails "Cannot calculate ARDA value: circulating supply is zero"
  (`ExchangeService.cs:499-501`, legacy NAV divide over the genesis-state local treasury
  singleton) and "Support remains disabled" — the first sponsorship of any project is
  unpriceable, exactly as L2.5's thesis says ("the product refusing its own opening
  move"). Executor implementing L2.5 as prescribed (offering price when fundingRaised==0
  + refuse-by-name `<=0` guard) and threading it through the live preview seam. On-ramp
  price v2 (honest peg) registered first.
- **F25 (seed, fixed):** `FundingIntentService` refuses checkout unless the owner flipped
  `project.commerceEnabled` — the seed's demo project never set it. Seed fixed + the live
  row flipped. The invest wizard itself behaved impeccably through the failure: the
  intent survived, and "Retry uses the same idempotency key, so the server can safely
  replay this sponsorship rather than create a second one."
- **F26 (dev-node, fixed — and accidental rotation):** the exchange door requires
  `swap:sign` (`ReserveExchangeManager.ExchangeSigningScope = AzoaScopes.SwapSign`),
  which the dev value key never carried (the W5.1 exchange postdates the key spec in the
  original A5 brief). Added to `$ValueKeyScopes`; forced a re-mint, which also ROTATED
  both previously-exposed tenant keys (the old keys stay valid node-side until expiry —
  truly revoking them is an operator step, noted for the runbook). Also fixed: the
  bring-up treated 409 "provider already selected" as fatal (idempotency gap). Bonus
  proof: the on-ramp partition re-declare showed available 999,750,000/1,000,000,000 —
  the durable ledger carries the settled $25's draw across restarts, and re-declare SETS.
- **STEP 3 SETTLED (2026-10-25 21:19 UTC):** intent 227ed2b5 reached
  `SETTLED | op=e06eb038-9122-93cc-f949-48db4cdc54ba` after the receipt gained its
  operation id — and the funding tab shows the settled truth everywhere: **$25 / 0.2%
  funded, 59,900 of 60,000 sponsor units remaining**, preview pricing live. En route,
  F28's follow-up found that **no exchange receipt on ANY rail ever carried an
  operationId** (`ReserveExchangeManager.Receipt()` never set it) — production's W6.3
  reconcile could never have completed either; fixed by deriving a stable Guid from the
  stored source id at READ time (`ExchangeHoldSourceIdentity.OperationId`), healing all
  old rows with no migration.
- **STEP 4 (in flight):** seat application + 4,000-SOLR proposal + accept + **hold
  RESERVED** (4,000 SOLR drawn from the contributor partition, BFF receipt logged) all
  work; the APPROVE leg's ReleaseIntoHold 400s with "original scoped hold operation was
  not found" — F31 delegated: node-side row-guard diagnosis (HoldCommandService.cs:49-118
  family) + ArdaNova-side window-instant anchoring (already applied:
  `leg.workerHoldReleasesAt` persisted before the node call so retries are
  intent-identical — the exact recalculated-window defect F20 pre-flagged).
- **F28 (node, OPEN → delegated):** with consent passing (proved by probing — the
  validation ladder sits BEHIND the consent check, so reaching "must carry the rate"
  proves the F27 fix live), the full valid exchange answers 503 `RAIL_UNAVAILABLE`.
  Diagnosed via the door's own `previewOnly` mode — "runs every gate and validation and
  then STOPS", purpose-built for exactly this. Cause family: `ChainModuleActivation`
  gates exactly DEX + Exchange per network and Simulated has no exchange
  activation/module (the last missing Simulated-first-class capability: the W5.1 atomic
  reserve-exchange bundle). Executor implementing/activating it; live proof = the
  ArdaNova outbox's own retry settling intent eca40d99. The BFF-side mapping was correct
  throughout: node 5xx → `AZOA_TRANSPORT_UNCERTAIN` (can't know if processed) →
  receipt-reconcile → 404 receipt → safe redispatch.
- **F29 (ardanova, OWNER FLAG):** the settlement reconciler treats a 404 exchange receipt
  as UNKNOWN effect forever — after its ceiling (10) it raises
  `AZOA_SETTLEMENT_RECONCILIATION_ATTENTION` ("NOT failed") and parks for an operator.
  Honest, but a 404 from the node's durable exchange journal IS proof of no admission;
  adopting "404 receipt ⇒ confirmed no-effect ⇒ safe redispatch" (the node-side F21
  no-effect-marker philosophy) would let uncertain settlements self-heal. Owner decision.
- **F30 (node, FIXED inline):** second-and-later writes of the tenant-declared rate 500'd:
  `holon.created_date` is READONLY with no schema default, and
  `TenantDeclaredRateStore.RecordAsync` builds a fresh row with `CreatedDate` unset →
  epoch-min vs stored = "changed readonly value" (first write fine, every re-declaration
  thereafter 500 → every sponsorship after the first-ever rate record went
  TRANSPORT_UNCERTAIN). Fix in `SurrealHolonStore.UpsertAsync`: when the caller left
  `CreatedDate` default, adopt the stored instant (or stamp now on first write) so the
  readonly re-send is byte-identical. Scope-check: hold scope `transfer:sign` also added
  to the dev value key (`ActionSigningScopes.cs:15` — reserve-funded holds sign under it;
  the TENANT's credential, never a child grant), with a live hold open→Completed→Refund
  probe proving the whole hold rail.
- **F27 RESOLVED (2026-10-25):** implied-grant coverage is now COMPUTED AT CHECK TIME
  (`Core/ConsentScopeCoverage.cs`: stored scopes ∪ {swap:sign} when origin=Participation
  with the unforgeable `tenant-provisioned:v1:` ref) — no data migration, existing
  children covered; revocation still refuses (liveness checked first), and the authority
  FENCE locks origin+ref+scopes+`revoked_at=NONE` so a revoke between admission and
  settlement throws `EXCHANGE_PARTICIPANT_AUTHORITY_CHANGED`. The executor also found and
  fixed the SECOND refusal hiding behind the probe (ExchangeAuthorityFactory/Fence only
  accepted UserExplicit grants). transfer:sign/DEX/child-JWT paths provably unreachable
  from the implied scope. 181/181 unit + consent/exchange integration green. The live
  intent b7cd2020 hit its 8-attempt ceiling during the rebuild window and self-parked
  HOLDING/CANCELLED by design — a fresh sponsorship is the live proof.
- **F27 (node, OPEN → delegated):** with `swap:sign` on the key, the exchange still 403s
  at the CONSENT probe (`ExchangeController.cs:281-305`) — the W3.6 implied participation
  grant doesn't cover the exchange signing scope, yet the consent copy on the
  verification page explicitly promises "to move your funds when you back a project",
  and W5.1/W6.2 designed the sponsorship exchange for exactly these custodial users.
  Executor aligning the implied grant's scope coverage (swap:sign in, `transfer:sign`
  still out — the exchange is the node's own atomic reserve bundle under declared terms,
  not an arbitrary spend), with revocation still killing it, applicability to existing
  grant rows, and live verification via the outbox's own retry of intent b7cd2020. there is NO custodial balance read in the BFF —
  `AzoaExchangeClient.cs:27-34` documents holdings reads as fail-closed stubs — so every
  "balance not available" all day was structural (W5.3 read-model unbuilt), not a bug in
  the drive. The invest leg's insufficient-ARDA UX depends on it; the node-side issued-lot
  read (`/api/operator/on-ramp-lots/accounts/{id}/issued`) is the nearest surface.
- **F1 (sidecar, fixed):** the widget `/external-api/result/` returned lowercase
  `'approved'/'rejected'`; the provider's verdict authority accepts only uppercase
  `APPROVED/REJECTED` (`IdentomatKycProviderService.cs:435`), so every submission parked
  as Pending. Fixed in `server.mjs` (uppercase outcomes).
- **F2 (sidecar, fixed):** one shared fake person for every applicant would trip the
  node's person-uniqueness gate across personas; the person is now derived from
  `clientUserId` (unique name/birthday/document per applicant).
- **F3 (sidecar, added):** `POST /__dev/sessions/inject` recovers a session the node
  still references after a sidecar restart (in-memory store); candidate for file-backed
  persistence in the M2 hardening batch.
- **F4 (node, ROOT-CAUSED + FIXED 2026-10-25 — a launch blocker, not a demo nit):**
  every live KYC approval failed with `outcome_detail="The verified attempt changed."`
  because `SurrealQuery.Combine` shares ONE parameter namespace across the combined
  transaction: the attempt fence's `.WithParam("_provider", expected.ProviderKey)`
  (`"identomat"`) was clobbered by `BuildBindingQuery`'s `.WithParam("_provider",
  body.Provider.ToString())` (`"IDENTOMAT"`, the enum name), so
  `provider_key = $_provider` matched 0 rows → `KYC_BINDING_AUTHORITY_LOST` on every
  bind. Pinned by decoding the transaction's CBOR debug payload (both `_provider =
  IDENTOMAT` and `_provider_key = identomat` present in one call). Fix: fence param
  renamed `_attempt_provider_key` (`SurrealKycIdentityStore.cs` BindVerifiedUserAsync).
  **Residual hazards for the node lane:** (a) `_tenant` also collides between the fence
  and the binding query — benign today only because a precondition forces the values
  equal; (b) systemic fix: `SurrealQuery.Combine` should throw on duplicate parameter
  names with differing values; (c) this path had no live/integration coverage — the
  mocked tests construct both sides from one value and cannot see the clobber.
- **F5 (client, minor):** the verification page keeps showing the stale "could not
  confirm your account setup" banner after successful refetches showing Ready states.
- **F6 (node, recorded):** `UserLoginValidator` vs `UserLoginModel` UserId-login mismatch.
- **F7 (ops, ROOT-CAUSED + FIXED 2026-10-25, second occurrence confirmed it):** the
  reap-strays Stop hook killed the detached BFF twice. Cause: its leak signature
  `$LEAK = '(\bmcp\b|--serve\b)'` matches the PATH SEGMENT
  `ardanova-backend-api-mcp\api-server` (hyphens are word boundaries), so the BFF
  pattern-matched as an abandoned MCP chain the moment its launcher shell exited, and
  Rule 1's cascade took its children. Fixed in `~/.claude/hooks/reap-strays.ps1`: `$LEAK`
  now requires a standalone ` mcp ` argument token / `mcp-server` / `modelcontextprotocol`
  / `--serve`, plus a `$NEVER` hard-exemption for `ardanova-backend-api-mcp`; documented
  in `~/.claude/hooks/AGENTS.md`. (This is the user's own 2026-10-09 lesson — "sweep by
  command line, not name" — biting from the other side: the command-line pattern itself
  must match argument tokens, not paths.)
- **F8 (verified 2026-10-25):** after the F4 fix, the full KYC leg completes live for all
  three personas — `kycStatus=Approved, ready=true` from the node for demo-creator,
  demo-sponsor, demo-freelancer, each bound to a distinct person (F2 proven).
- **F9 (client, minor):** the funding setup form (`funding-tab.tsx` handleSubmit) has no
  validation for an empty "Short label" (`unitName`) / "Name of the stake" (`assetName`);
  an empty short label submits (or silently does nothing) with no inline error — the
  dollars/units validations all speak, the name fields don't.
- **F10 (node+dev-node, found live then automated):** value endpoints gate on
  `RequireValueAccessAsync(actor)` — the TENANT user needs its own first-party KYC
  approval, not just KYB; `POST /api/nft/fungible-mint` answered
  `KYC_FORBIDDEN` with KYB APPROVED. `dev-node-up.ps1` now runs `Ensure-TenantKyc`
  (first-party `/api/kyc/session` via tenant JWT, sidecar-approved) after the KYB step and
  writes `tenantKycStatus` into the contract JSON. Also observed: a hosted-session resume
  can sit in `RESUMING` until its 2-minute lease expires and a fresh callback re-claims it
  — the sidecar's finite callback schedule (1.5/5/15 s) means a resume that dies needs a
  manual re-fire; candidate sidecar improvement: a slow retry tail (e.g. +60 s, +180 s).
- **F12 (ops, explains every "hang" this session):** `dev-node-up.ps1` COMPLETES, but any
  shell that captures its stdout never sees EOF — the spawned servers inherit handles, so
  the pipe stays open for as long as the stack lives. Every "stalled" bring-up was
  finished work behind an open pipe. Rule for driving it: redirect its output to a file
  and poll health yourself, never wait on the process.
- **F13 (node, FIXED 2026-10-25):** `SimulatedBlockchainProvider` implemented no
  `IFungibleCreationModule` — project-token genesis had NEVER been possible on a Simulated
  chain (wave-era claims were mock-tested only). Implemented in
  `SimulatedBlockchainProvider.Fungible.cs`: deterministic quote (zero cost, ids from
  `FungibleCreationIdentity` so `IsValidQuote` accepts), stateless idempotent prepare,
  submit/observe that re-derive and refuse tampered candidates field-by-field (surviving
  the `PreparedJson` round-trip), observe `Pending`→`Confirmed` with a stable numeric sim
  asset id — deliberately exercising the service's resubmit path. 57/57 nearby tests; no
  activation config needed (`TryGetModule` is interface-implementation discovery; only
  DEX/Exchange are activation-gated). Rationale: `Providers/Blockchain/AGENTS.md`
  §simulated-fungible-creation. Sim submissions are in-memory only (harmless resubmit
  after restart; noted as non-durable).
- **F14 (node, OPEN → delegated 2026-10-25):** the simulated-mode redirect
  (`BlockchainProviderFactory.cs:265`) rewrites `ChainType` to "Simulated", but the
  wallet/key/reserve seams below the mint admission only know Algorand/Solana/Ethereum:
  `WalletBootstrapIdentity.CanonicalChain("Simulated")` → null refusal at
  `WalletManager.cs:458-460` ("Chain type 'Simulated' is not supported for wallet
  generation"), `WalletKeyService.cs:49` generates no sim keys, and
  `reserve.GetAccountAsync("Simulated")` (`FungibleTokenManager.cs:87`) is the expected
  next refusal. Design decision taken: Simulated is its own first-class canonical chain in
  those seams (save-time keying already stamps it; custody's Algorand wallets stay
  untouched). **FIXED 2026-10-25:** `CanonicalChain` knows Simulated (static — the mode
  check sits where keys are minted: `WalletManager.RefuseSimulatedChainOffSimulatedNode`
  at all three creation paths, so a Simulated key exists only under
  `Blockchain:Mode=Simulated`); `WalletKeyService` generates sim keypairs (address =
  `SimulatedBlockchainProvider.SimAddress` — the only format its `ValidateAddressAsync`
  accepts); Simulated reserve follows the per-chain key-derivation registry
  (`SimulatedPlatformKeyDerivation`, dev seed + pinned expected address in
  appsettings.Development.json — the pinned address worked as a canary catching a wrong
  address derivation, note the provider's `\x1f` part separator). Live replay: genesis
  **Completed, asset id 6260732372893272157**, reserve as creator; 143/143 tests. The 5
  architecture-guard failures are the known `.worktrees/main-release` leftover (A1 owner
  decision still pending).
- **F15 (ardanova BFF, OPEN → delegated 2026-10-25):** two adapter defects block the UI
  retry even though the node path works: (a) `AzoaBackedAlgorandService.CreateFungibleASAAsync`
  → `AzoaNodeClient.PostAsync` sends NO Idempotency-Key, which the node's admission guard
  refuses with the generic 400; (b) the node's creation became a CONSENT LIFECYCLE
  (202 AwaitingConsent → quote → accept → continue → refresh Completed) during round 3's
  cost-consent redesign, while the adapter still expects a synchronous asset id ("CLEAN
  MAP" comment now wrong) and `AzoaNodeRoutePolicy.IsValueRoutePath` doesn't even allow
  the lifecycle routes. Classic cross-repo wire-contract drift (same family as the
  avatarId→userId rename). Executor is threading a stable
  `project-token-genesis:{config.id}` key through `IAlgorandService` and teaching the
  adapter to drive the lifecycle with a bounded poll budget.
  **FIXED 2026-10-25:** `PostAsync` overload carries the Idempotency-Key (overload, not an
  optional arg — optional args break existing Moq setups with CS0854);
  `AzoaNodeRoutePolicy` widened to exactly the four lifecycle steps with path-safe id
  validation; `DriveCreationAsync` is a resumable state machine (state → next step;
  continue's requestId is a SHA-256 Guid of (operationId, version) so a retried continue
  is the same authorization; 30 s budget, honest "last state" failure that names the
  resume path). 129/129 tests; live adapter run minted and idempotently replayed the same
  asset (7902425028085132469). Verified through the UI path: mint → quote → accept-quote
  → continue → refresh all flowed in the BFF log.
- **F16 (node+ardanova, OPEN → delegated 2026-10-25):** provisioning now stops one leg
  later at `POST /api/projects/treasury/resolve` → 400. Probed both spellings:
  "Algorand" → "No platform signing key configured for chain 'Algorand' (set
  'AZOA:Algorand:PlatformMnemonic')" (correct in dev); "Simulated" → "No project treasury
  key derivation is registered for chain 'Simulated'" — the PROJECT-TREASURY derivation
  registry is separate from the reserve's and never got the Simulated entry. Executor is
  mirroring the reserve precedent on the node and making ArdaNova record + send the chain
  the asset actually minted on (`config.assetChain` from the Completed view), with licence
  to also fix the next partition-declaration leg if it is the same pattern.
  **FIXED 2026-10-25:** node — one registry entry
  (`ProjectTreasuryService.cs:582`, Simulated key length; the address derivation already
  mapped Simulated); per-project HKDF child keys of the existing dev platform seed; live:
  200 with stable distinct `sim:` addresses per project reference, idempotent re-resolve,
  Algorand still refusing without a mnemonic. ArdaNova — the node's `FungibleCreationView`
  already carries `chainType` ("Simulated" after the redirect), so the adapter surfaces it,
  `ProjectTokenService` persists it to `config.assetChain` and sends it to BOTH treasury
  resolve and partition declaration. 92/92 node + 79/79 ardanova tests. Caveat: config
  rows minted before this change persist `assetChain=Algorand` — use a fresh project (the
  demo will reset-db before the final drive).
- **F17 (node, OPEN → delegated 2026-10-25, security-sensitive):** partition declaration
  403s because the MINT door and the PARTITIONS door disagree about who the tenant is: the
  mint passes `User.GetActingTenantId()` (only the `act_as_tenant` claim → null for a
  plain tenant key; `NftController.cs:108`), so the reserve row gets `tenant_id = null`
  and `ReserveDrawAuthority.cs:166` treats the stake as the node's own unit — operator
  only. The partitions door (L1.1) resolves the key principal's user AS the tenant.
  L1.1's ratified rule ("tenant id comes from the API key principal") says the mint door
  is the one out of line. Executor aligning it (act_as_tenant wins; tenant-scope key →
  its owning tenant; first-party stays null), with regression tests pinning the
  operator-only refusal for platform units. **FIXED 2026-10-25, better than briefed:**
  ownership travels on a new server-set `[JsonIgnore] ReserveOwnerTenantId` (body can't
  supply it, same as AuthenticationWitness) instead of the delegation `tenant` — because a
  non-null tenant means "acting FOR a user" and demands a consent grant the tenant can't
  give itself (`SigningAdmissionAuthorityFactory.cs:35-50`). `OperationId` unchanged → NO
  replay caveat; pre-change ops replay under old keys, and a replay that adds the owner
  field is refused as a conflict, never silently replayed as the platform unit. Null owner
  is omitted from the command hash so saved commands re-hash identically. The first live
  run caught a store fence (`SurrealFungibleCreationStore.cs:214`) that unit tests
  couldn't. Live: mint → `partitions` declare 200 (body is a name→budget MAP, not an
  array) → re-declare "nothing was changed", version stayed 1; the previously stuck stake
  also completed. 110/110 unit + 27/27 integration. ADVERSARIAL SECURITY REVIEW still
  REQUIRED before C3 close (reserve-ownership seam).
- **F19 (both, OPEN → delegated 2026-10-25, step-3 bring-up):** the sponsor on-ramp is
  dark for two reasons: (a) **ARDA has never been genesis'd on the dev node** — the BFF's
  `ArdaOnRampPriceScheduleRegistrar` fails at startup wanting `Azoa:OnRamp:AssetId`
  (+ chain), so no price schedule, which also explains the funding-preview nit and
  probably the ARDA balance read; (b) the node 403s `POST /api/fiat/checkout/{userId}`
  (scope or rail gate — under investigation). `Azoa__OnRamp__Enabled=true` added to the
  launcher (it swaps out the fail-closed `DisabledFiatRailGateway`, whose "refuses rather
  than pretending" design worked exactly as documented). Executor bootstrapping ARDA
  genesis (W3.2) into `dev-node-up`, wiring `ardaAssetId` through the contract JSON, and
  root-causing the 403; if checkout completion hard-requires Stripe test keys, that comes
  back as an operator decision (Stripe CLI sandbox is an option).
- **Step-4 progress (2026-10-25):** the freelancer's seat application AND 4,000-SOLR paid
  proposal both submit through the UI ("Application sent" / "SUBMITTED — Withdraw
  proposal"); the creator sees both ("1 applicant", Accept/Reject, "Accept bid"). The
  bid-accept then 404s — see F20. (The seat Accept also errored once; re-check after F20.)
- **F20 (ardanova, FIXED 2026-10-25):** `AzoaHoldClient` rewritten against the node's real
  hold door (`api/hold-commands`, intent-shaped `HoldStartRequest`; the old flat
  `/api/holds` dialect 404'd on every route). Key moves: both repos now compute and
  TEST-PIN the same `holdRecordId` (SHA-256 over canonical
  `{Domain:"hold-custody-v1",ChainType,Network,ProtocolIdentity,CustodyTenantId,
  OwnerUserId,HoldId}`), so wire drift on either side fails a test; fixed per-hold request
  keys (`open:{id}`, `refund:{id}`, `release-into-hold:{id}:{next}`); the node's direct
  door refuses `nextAutoRelease=true`, so the worker window is opened with
  `AutoRelease:false` — paying a finished window out to a wallet would need a release
  MANDATE, which is not built (recorded as a step-5 boundary). 1290/1290 ArdaNova
  application tests; node 113 hold/reserve/principal tests green (5 pre-existing
  WorkflowOpening timeouts vs shared SurrealDB, untouched paths).
- **F18 CLOSED — already solved in the store:** `SurrealHoldCommandStore.cs:94-95` treats
  a plain tenant key that OWNS the reserve row as that tenant
  (`credential.TenantId ?? (reserve.TenantId == ActorUserId ? ActorUserId : null)`), and
  the reserve-assets read uses the same fallback. The F17 helper was deliberately NOT
  applied at credential capture — the signing checks REQUIRE credential tenant to stay
  null for a tenant's own key. Tenant-key draw tests added both ways (own partition
  admitted; foreign partition and platform unit refused). Verdict recorded in
  `Services/Hold/AGENTS.md` §tenant-key-draw.
- **F19 resolution (2026-10-25):** ARDA genesis is an OPERATOR route
  (`POST /api/admin/platform-token/genesis`, fixed in-code definition: ARDA, 2 decimals,
  20,000,000.00 — kept over the suggested 1B/6dp because ArdaNova's seed price
  `UnitsPerUsdMinorUnit="100"` assumes 2dp), now idempotently ensured by `dev-node-up`
  (`Ensure-ArdaGenesis`; asset survives restarts; contract fields `ardaAssetId`,
  `ardaChainType`, `ardaDecimals`). Price schedule registered v1 at BFF startup. The 403
  was a consent-scope mismatch: `FiatCheckoutController.cs:233` checked `transfer:sign`
  while on-ramp settlement signs `IssueUnits` under `units:issue` (which every custodial
  participation grant carries; custodial users can NEVER hold transfer:sign) — fixed with
  flow tests through the real consent gate; payout direction correctly still checks
  transfer:sign. Checkout returns a live Stripe test-mode URL. REMAINING for credited
  ARDA: (a) Stripe completion loop (hosted page payment + `stripe listen` forwarding —
  CLI not yet installed; node has test keys in its ignored `.env`); (b) the platform
  unit's `on-ramp` reserve partition is operator-only and NO reachable operator route
  declares it; (c) UNVERIFIED whether simulated chain balances survive a node restart.
  **Decisions (orchestrator): D7 — add an operator-only
  `POST /api/admin/platform-token/partitions` beside the genesis route (same authz
  family; required for any on-ramp ever); D8 — the Stripe test-mode drill is in scope
  (closes the stub-closeout deferred item); rotate the test keys afterward per the
  executor's secrets heads-up.**
- **F18 (node, WATCH — pre-spotted):** the DRAW side passes
  `ReserveDrawAuthority.ForTenant(GetActingTenantId())` — null for a plain tenant key — so
  tenant-owned reserve rows will refuse the tenant's own IssueUnits/Hold draws until that
  door adopts the same principal-tenant resolution. Trigger: the first sponsor exchange or
  worker-stake hold (golden-path steps 3-4).
- **F11 (node, ROOT-CAUSED + FIXED 2026-10-25 — first-party KYC binding never ran live):**
  the tenant's first-party KYC resume looped in `RESUMING` forever because the identity
  UPSERT wrote `tenant_id = $_tenant` with a NULL parameter (first-party has no tenant;
  `SurrealRecordParam.Of` returns C# null → CBOR NULL) while the column type is
  `none | record<user>` — SurrealDB refuses NULL where NONE is required, the
  `HostedOutcomePollWorker` sweep threw the same coercion error every cycle
  (`kyc_identity:750f44...`), and the fence half already knew better (`$_has_tenant`
  guard). Fixed in `BuildBindingQuery`: emit `tenant_id = NONE` when the candidate has no
  tenant. The tenant-custodial persona path never hit this because it always has a tenant.
- [x] **C2 — THE GOLDEN PATH IS CLOSED (2026-10-25).** All five steps verified through
  the real UI against the real node:
  **Step 4 CLOSED:** after F31 (two ArdaNova bugs: the EF change-tracker update-on-added
  row in `TokenBalanceService.CreditAsync` that rolled back approve #1 AFTER the node had
  committed the window, and the per-attempt window recomputation), the approve click
  returned **PAID**; the node fix was message-honesty only (`ReplayConflict` instead of
  "not found" for same-actor byte-different replays). The repair pattern:
  `ResolveWorkerWindowAsync` reads the node's committed window FIRST and replays its
  exact instant — "the node commits before ArdaNova does; any local failure after the
  node call needs a path that reads the node's state first."
  **Step 5 VERIFIED:** worker sees "PAID — yours, and you can move it from January 23,
  2027 … Nobody can take it back in the meantime" and portfolio shows "Solar Commons
  Stake — CONTRIBUTOR — 4,000 SOLR"; creator's funding tab shows $25/0.2% and
  59,900/60,000; sponsor's settled intent carries the operation id. Node 90/90 hold
  integration tests; ArdaNova 552/552 API + 1303/1304 Application.
- [~] **C2 (historical header)** Drive steps 1–5 of the spec; record every refusal verbatim.
  **Step 1 VERIFIED 2026-10-25:** all three personas reach `kycStatus=Approved,
  ready=true` through the browser (account setup → identity check via sidecar →
  managed wallet), distinct persons each.
  **Step 2 VERIFIED 2026-10-25 (fresh DB, first try):** creator's funding setup ran the
  whole chain through the UI — genesis mint → consent lifecycle → treasury resolve →
  partition declaration — and the tab now shows GATE STATUS: FUNDING, $0 of $15,000,
  "60000 of 60000 sponsor units and 40000 of 40000 worker units are still available",
  with the disclosure rendered. Steps 3–5 (sponsor add-funds/invest, freelancer
  stake/pay, verification) in progress; first step-3 nit: "The pricing service did not
  return a valid preview" on the funding preview (suspect W6.1 price-schedule
  registration at BFF startup).
- [ ] **C3** Fix batch → single full sweep (both repos) → re-drive. Record final
  evidence (balances/holdings/receipts) in this track; map findings onto
  launch-readiness L-items (close with evidence or re-open).

## Review lane

- A4 (security-sensitive seam) gets an adversarial review before C1 relies on it:
  separate context, prompted to refute, verdict recorded here.
- Phase boundary A+B → C: verifier pass on the two scripts' idempotency and reaping.

## Verdicts

- **A4 adversarial review (2026-10-24): MERGE-WITH-FIXES, no critical/high.** The
  production invariant held against environment-promotion, loopback-bypass
  (IPv6-mapped/hex/decimal forms probed), redirect-escape, and registry-widening attacks;
  the flag appears in no appsettings/deploy file and the Docker image ships no sidecar.
  Fix batch **APPLIED 2026-10-25** (all but L3, deferred with rationale in
  `tools/dev-kyc-vendor/AGENTS.md`): **M1** shared
  `KycRuntimeSafety.IsPinnedLoopbackRedirect` called from BOTH adapters (off-port loopback
  redirect now refused on KYB too); **M2** sidecar refuses off-loopback bind at start,
  415s non-JSON POSTs (verified all four vendor-protocol POSTs are JSON), 421s non-loopback
  Host headers (DNS-rebinding guard); **L1** gate-test table widened with Live-chain and
  RealValueEnabled rows across all three seams + fragment-URL case; **L2** `GuardStartup`
  throws when the flag is set outside Development; **L4** random fallback secrets.
  Sidecar 9/9, KYC/KYB suites 99/99, build 0 errors. Takes effect at next stack restart.
  (Cleanup nit for the C3 sweep: two unused declarations in `server.test.mjs:5,44`.)
- *(pending)* A+B verifier pass.
