---
type: spec
---

# MVP Tandem Demo — creator → sponsor → freelancer, ArdaNova + AZOA in one local stack

**Created 2026-10-24.** Goal: one command stands up BOTH stacks locally (ArdaNova client +
BFF + local Postgres, AZOA node + SurrealDB, both in Simulated chain mode) and the product's
core flow — the solutionary loop of `product.md` — is demonstrable end to end:

1. **Onboard** — creator, sponsor, freelancer each get a tenant-custodial AZOA account,
   pass KYC (dev vendor, see D4), and reach `Ready`.
2. **Create** — creator publishes a project with funding config: per-project ASA genesis
   into the platform reserve + a node-side project treasury (`projectTokens.provisionFunding`).
3. **Sponsor** — sponsor adds funds (fiat test rail → ARDA), then sponsors the project
   (ARDA → project units via the node exchange; intent settles through the outbox →
   `AzoaSettlementGateway`).
4. **Join for stake/pay** — freelancer applies to a role (seat join), then the paid path:
   bid → accept (HOLD from the contributor partition) → owner approves completion →
   `ReleaseIntoHold` → freelancer holds units.
5. **Verify** — observable outcomes only: ARDA balance, project treasury receipt,
   freelancer holdings (pending/available), task commerce state SETTLED.

## Verified current state (2026-10-24, both repos)

**ArdaNova (main):** all three legs are coded; the economy is configured off.
- Leg 1 works; the wizard's funding step skips provisioning when custody isn't ready
  (`ardanova-client/src/app/projects/create/page.tsx:1066`, `:1106-1112`).
- Leg 2 is unreachable: `AzoaFundingSettlementReadiness.FailingLeg()` requires
  `Azoa:EnableFundingCheckout` + a registered outbox loop + a selected node + matching
  gateway capability + an unexpired operator attestation; appsettings defaults are all
  off/blank, so `DisabledAzoaSettlementGateway` is wired
  (`ArdaNova.Infrastructure/.../DependencyInjection.cs:145-154`).
- Leg 3: seat-join works (hiring is not value-gated); the paid path sits behind the same
  eligibility gate. `AllocateToInvestor`/`AllocateToFounder` are 501 stubs
  (`ProjectTokensController.cs:194-212`) — out of MVP scope (D6).
- `.omc/scripts/local-stack.ps1` starts only the BFF and client; it never starts the AZOA
  node and sets only `Azoa__Mode=Simulated`, so the economy resolves **Disabled**
  (`AzoaSettings.cs:221-261`).
- `seed-local-demo.ts` creates no `ProjectTokenConfig`, no custodial account, no
  opportunities — a fresh reset has nothing fundable.

**AZOA node (oasis-sleek, `feat/platform-stub-closeout`):** builds clean; 5346/5353 tests
green (7 failures are stale `.worktrees/*` tripping architecture guards). Runs via
`dev-up.ps1` — SurrealDB v3.1.4 (compose, host :8000; `appsettings.Development.json` pins
:8020 — mismatch), WebAPI :5000, `Blockchain:Mode=Simulated`,
`CustodialAccounts:CustodyMode=DevelopmentOnly`. Endpoints for every MVP step exist
(tenant custodial provision, project-treasury resolve, fiat checkout, exchange + receipt,
allocation IssueUnits, action DAGs); genesis/treasury paths are built but their round-4
tests are Authored-only — this demo is their first integrated run.

**The one hard gate — KYC (round-4 decision A-4):** value gates never accept the MANUAL
provider; dev uses the same Identomat adapter as production, and without
`Kyc__Identomat__*` secrets it reports UNAVAILABLE — custodial accounts can never reach
`Ready = kycStatus == Approved` (`TenantCustodialAccountManager.cs:499-504`), which fails
every value flow closed. The vendor origin check also refuses loopback
(`IdentomatKycProviderService.cs:523-530`). Tests pass this with a faithful in-memory
vendor fake (`tests/AZOA.WebAPI.IntegrationTests/Http/Infrastructure/IdentomatVendor.cs`).

**SDK:** npm has `azoa-sdk` ≤0.2.2; the client pins `^0.4.0`; local source is 0.5.0 and
works via a machine-local junction. `sdk/azoa-wallet/dist` is stale (last built
2026-09-08; 37/46 sources newer). CI stays red until an operator publishes — out of scope
here (D5); local junction + rebuilt dist is sufficient for the demo.

**Launch-readiness boards (`oasis-sleek/conductor/tracks/launch-readiness_20260909/`)
are stale**: L1/L2 items were written 2026-09-09, before `platform-stub-closeout` (4
rounds, live QA 2026-10-18) and ArdaNova loops 1–4. This demo IS the runtime
reconciliation of those boards — blockers found while driving it are the true remainder.

## Decisions & assumptions

- **D1** — Demo runs oasis-sleek on `feat/platform-stub-closeout` as-is; no merges, and
  the partial/untested `feat/node-admin-fix` worktree stays out. *Reversal: branch switch.*
- **D2** — Both sides Simulated (`Blockchain:Mode=Simulated` ↔ `Azoa:Mode=Simulated`);
  mixed modes are refused by design.
- **D3** — Single tenant API key via the non-Production `Azoa:TenantApiKey` fallback,
  minted with the full scope set. Production uses four split keys; not this demo's problem.
- **D4** — KYC passes via a **dev Identomat vendor sidecar**: the integration tests'
  vendor fake hosted as a standalone local service, plus a Development-only relaxation of
  the loopback-origin refusal (production behavior unchanged, with a test proving it).
  A-4 stays intact: the provider is still `identomat`; only the vendor host is fake.
  *Reversal: delete sidecar + relaxation; the gate fails closed again.*
- **D5** — No `npm publish`, no prod deploy, no prod DB migration (all operator-gated).
- **D6** — Investor/founder direct allocations (501 stubs) and cash-out (Wave 8 /
  `PayoutService`) are out of scope; the sponsor and worker-stake paths are the MVP.

## Acceptance

1. From a clean machine state: `.omc/scripts/local-stack.ps1 -Action up -WithAzoa` brings
   up SurrealDB, the AZOA node, the KYC sidecar, the BFF (economy resolving **Simulated**,
   not Disabled), and the client — with health checks, and `-Action down` reaps everything.
2. A scripted golden-path run (API-driven, plus Playwright for the user-visible steps)
   completes steps 1–5 above, asserting observable outcomes at each step.
3. A demo runbook documents the one command, the three persona logins, and the script.
4. Blockers found while driving are recorded in this track and mapped back to
   launch-readiness L-items (closing or re-opening them with evidence).
