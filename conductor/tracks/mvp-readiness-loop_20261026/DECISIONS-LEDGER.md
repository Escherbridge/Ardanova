---
type: decision-ledger
track: mvp-readiness-loop_20261026
created: 2026-10-26
repos: [ardanova, oasis-sleek]
status: draft-for-owner
---

# MVP Decisions Ledger — ArdaNova + AZOA

1. **What this is:** every remaining item across both repos, grouped by the serving plane a user touches, each with one decision.
2. **Goal:** get MVP-ready for real users testing the core loop (create → sponsor → work for stake → cash out). It is not a launch-complete checklist.
3. **Decisions:** DO-NOW (build in this loop) · OWNER-CALL (the owner answers; a recommended answer is given) · CUT-FOR-MVP (not needed to test the business idea) · DEFER(trigger) (only when vendor-blocked or real-chain-only).
4. **States:** done-live (driven live, local tandem stack 2026-10-25) · built-untested · gated (config, owner or vendor) · missing.
5. **† = from a stale board.** These items come from the 2026-09-09 launch-readiness board (LR), which predates platform-stub-closeout and ArdaNova loops 1-4. Re-grep before building; if the code is already fixed, close the row with evidence.
6. **How the loop uses this file:** each pass takes one plane. An adversarial review runs in a separate context, then all of that plane's DO-NOW rows are implemented, then one test sweep runs on both repos, then the golden path is re-driven.
7. **Recording results:** every pass writes a one-line verdict per row (CLOSED+evidence / REOPENED / MOVED). Rows are never deleted.
8. **OWNER-CALL rows:** they block only their own row. The rest of the plane proceeds with the recommended answer as a stated assumption.
9. **Reviewer cites:** reviewers cite the row id (P3-07). New findings are appended as P<plane>-NN with a source.
10. **Order:** see "Loop order" at the end; answer the five first-priority OWNER-CALLs before pass 1 closes.

**Source keys.** AN = `ardanova/` · OS = `oasis-sleek/` · PROD = AN `conductor/product.md` · TR = AN `conductor/tracks.md` · MVP-P / MVP-S = AN `conductor/tracks/mvp-tandem-demo_20261024/plan.md` / `spec.md` · LR = OS `conductor/tracks/launch-readiness_20260909/plan.md` · R4D = OS `conductor/tracks/platform-stub-closeout_20261015/round4-post-qa-deferrals.md` · DEF = OS `.../platform-stub-closeout_20261015/deferred.md` · OS-RB = OS `conductor/RUNBOOK.md` · WTT = OS `tests/AZOA.WebAPI.IntegrationTests/Fixtures/Wtt/AGENTS.md` · UX / UXRB = AN `uxqa/study.md` / `uxqa/RUNBOOK.md` · CTX = orchestrator-verified state 2026-10-25/26 · MEM = project memory `mvp-golden-path-live.md`.

---

## 1 · Onboarding & identity (KYC/KYB, verification UI)

| id | item | source | state | DECISION | size |
|---|---|---|---|---|---|
| P1-01 | Tenant-custodial account + KYC + managed wallet reach `Ready` for all three personas through the UI | MVP-P §C2 Step 1, F8 | done-live | DO-NOW: pin as the first step of a Playwright golden-path regression (LR L5.3) | M |
| P1-02 | Live Identomat KYC/KYB: the trial company cannot create configs, so no released-env user (or tenant, F10) can pass KYC | R4D §Release blockers "Live Identomat drill"; DEF §IDENTOMAT-WORKFLOWS; CTX | gated | DEFER(Identomat lifts trial / creates KYC+KYB configs). See OWNER-CALL #2 | S |
| P1-03 | Ambiguous `/begin` recovery: vendor has no idempotent create; operator attach path built | DEF §IDENTOMAT-RECOVERY | built-untested | DEFER(vendor ships idempotent create or lookup-by-attempt) | — |
| P1-04 | F4 residuals: `_tenant` param also collides; `SurrealQuery.Combine` should throw on duplicate params with different values; no live/integration coverage of bind (F11 NULL-vs-NONE class too) | MVP-P F4 (a)(b)(c), F11 | missing | DO-NOW | S |
| P1-05 | Verification page keeps the stale "could not confirm your account setup" banner after a Ready refetch | MVP-P F5 | missing | DO-NOW | S |
| P1-06 | Node `UserLoginValidator` requires Email; `UserLoginModel` documents UserId-XOR-email login | MVP-P F6, §C1 | missing | DO-NOW (validator follows the model) | S |
| P1-07 | `UnderReview`/`reviewDueAt` plumbed but read by no screen; users see "try again" copy | LR L2.4 | missing† | DO-NOW | M |
| P1-08 | Consent revoke is prose plus a disabled button; the page promises the right to withdraw | LR L2.7 | missing† | DO-NOW (the page makes the promise) | M |
| P1-09 | AML manual-review backend (`aml-reviews` routes) absent; flagged people stay `UnderReview` forever | LR L1.12 | missing† | DO-NOW (needed the day live Identomat AML runs) | M |
| P1-10 | `TenantManager` email trim without case-fold → ambiguous login between two hashes; the other two single-seam gates | LR L1.10 | missing† | DO-NOW | S |
| P1-11 | Terms/Privacy consent before workspace, verified linking on email collision, honest sign-in errors | TR "Auth Consent & Account Linking" | missing | DO-NOW | M |
| P1-12 | Terms/Privacy text not approved (routes `app/terms`, `app/privacy` exist) | TR "Terms and Privacy Pages" | gated | OWNER-CALL: approve short-form text before the first card is charged; link it from sign-up and add-funds | S |
| P1-13 | P0 security tracks: user-directory PII, actor gating on service-key controllers, path-segment safety (partly closed in loops 2-4) | TR "User Directory Privacy", "Backend Authorization Hardening", "API Path-Segment Safety" | built-untested | DO-NOW: review pass closes each with evidence or fixes the remainder | M |
| P1-14 | `DELETE /api/users/{id}` is an unguarded hard delete (a holder could delete over live stake) | TR "Account Closure and Data Export" | missing | DO-NOW: blocker-checked refusal only; export + Settings>Account UI CUT | S |
| P1-15 | First-time onboarding wizard (user-type selection, profile wizard) | TR "Onboarding & First-Time Experience" | missing | CUT-FOR-MVP (verification CTA + empty states, P8-06, cover it) | — |

## 2 · Project creation & funding provisioning (creator)

| id | item | source | state | DECISION | size |
|---|---|---|---|---|---|
| P2-01 | Funding setup via UI: genesis → consent lifecycle → treasury resolve → partitions (GATE STATUS: FUNDING) | MVP-P §C2 Step 2, F13-F17 | done-live | DO-NOW: close LR L1.1/L1.2 with this evidence; keep in golden-path E2E | S |
| P2-02 | F17 reserve-ownership seam (`ReserveOwnerTenantId`) still needs its adversarial security review | MVP-P F17 ("REQUIRED before C3 close") | built-untested | DO-NOW | S |
| P2-03 | Funding form: empty "Short label"/"Name of the stake" submits with no inline error | MVP-P F9 | missing | DO-NOW | S |
| P2-04 | `commerceEnabled` must be flipped before checkout; the seed had to set it. Confirm creators can flip it in the UI | MVP-P F25 | built-untested | DO-NOW (review; add the toggle if absent) | S |
| P2-05 | Ask entered in dollars, ARDA as the derived caption (RULED) | LR L3.1 | missing† | DO-NOW (after OWNER-CALL #1) | M |
| P2-06 | Publish rule enforced at `PublishAsync` only, not `UpdateAsync` | LR L2.8 | missing† | DO-NOW | S |
| P2-07 | Storage: Local default enforces no type/size; attachments take owner from a header | LR L2.13 | missing† | DO-NOW | M |
| P2-08 | Treasury reconcile over an empty set stamps success | LR L2.11 | missing† | DO-NOW | S |
| P2-09 | Genesis supply ceiling `int.MaxValue` (one-way door before real genesis) | LR L1.3 | missing† | DEFER(before any TestNet/MainNet ARDA or project genesis; real-chain only) | M |
| P2-10 | Investor/founder direct allocations (501 stubs) | MVP-S §Verified state Leg 3, D6 | missing | CUT-FOR-MVP | — |
| P2-11 | Nova AI project generation rollout (OGAI partner) | TR "Nova AI …" rows; PROD §Key Features | gated | CUT-FOR-MVP (core loop does not need it; owner/lease-gated) | — |

## 3 · Sponsor funding (add-funds, invest, settlement)

| id | item | source | state | DECISION | size |
|---|---|---|---|---|---|
| P3-01 | Add funds: real Stripe test checkout → webhook → ARDA issued (F19/F21; closes LR L2.3, L1.4) | MVP-P §Step-3 milestone, F19, F21 | done-live | DO-NOW: close L2.3/L1.4 on LR with evidence | S |
| P3-02 | ARDA peg: in-code `UnitsPerUsdMinorUnit="100"` ($0.01) contradicts PROD and the add-funds copy ($1); dev overrides to 1 | MVP-P F22; LR preamble ("1 ARDA = $0.01"), L6.2 | gated | DO-NOW — DECIDED 2026-10-26: keep $0.01/ARDA in code; fix PROD + add-funds copy and drop the dev "1" override | S |
| P3-03 | First sponsorship priced (offering price at `fundingRaised==0`; `<=0` guard); exchange SETTLED with op id | MVP-P F24, STEP 3 SETTLED; LR L2.5 | done-live | DO-NOW: close L2.5 with evidence; re-drive on a fresh DB in the C3 sweep | S |
| P3-04 | 404 exchange receipt parks forever as ATTENTION instead of proving no-effect | MVP-P F29 | built-untested | DO-NOW — DECIDED 2026-10-26: 404 = proof-of-no-effect, redispatch same key; ATTENTION stays for 5xx/ambiguous | M |
| P3-05 | F21 residuals: recipient-prep failures lack the no-effect marker; a Stuck lot needs two resumes; pre-existing AllocationManager Transfer-test failure | MVP-P F21 | missing | DO-NOW | S |
| P3-06 | Rotate/revoke dev tenant keys and Stripe test keys exposed in agent output (rotation leaves old keys valid until expiry) | MVP-P F21, F26, F19 D8 | missing | DO-NOW (operator step + runbook line) | S |
| P3-07 | Intent status lacks `releaseNotBefore`/`holdingReasonDetail` (hard-coded 30 days); no hosted SETTLEMENT_PENDING promoter; backer-name choice | LR L2.6 | missing† | DO-NOW | M |
| P3-08 | Spend seams read gate-only `TokenBalance.isLiquid`, not the allocation window | LR L2.9 | missing† | DO-NOW | M |
| P3-09 | Live-boot guard checks a handler type, not on-ramp partition + price schedule | LR L1.6 | missing† | DO-NOW | S |
| P3-10 | No operator read for "charged and not credited" lots / `Received` fiat events | LR L1.8 | missing† | DO-NOW (before any card on the released env) | M |
| P3-11 | Exchange idempotency key ignores amount; price stamp goes dark with a second tenant | LR L1.11 | missing† | DO-NOW | S |
| P3-12 | Risk-hold (account pause) gate wired at one dispatch seam only | LR L1.7 | missing† | DO-NOW | M |
| P3-13 | Remaining Stripe drill legs: refund, dispute → reversal → clawback, Connect transfer, payout success/failure, tenant-funded payout | R4D §Release blockers "Stripe test-mode drill" (checkout leg closed by MVP-P Step-3) | built-untested | DO-NOW | M |
| P3-14 | Reversal rung 2 provenance (OI-59) + D-10 provenance on transfer + `on_ramp_lot.created_at` index | LR L1.13; R4D §Engineering D-10, RV-L3 R2-3 | missing | DO-NOW before Stripe live keys (ArdaNova's on-ramp is tenant-priced, so the D-10 trigger fires); not blocking a test-mode cohort | L |
| P3-15 | Add-funds "IN PROGRESS — we'll email you": confirm a sender exists for that promise | MVP-P §Step-3 milestone | built-untested | DO-NOW (review; reword if no mail path, UX §Target "no mail paths") | S |

## 4 · Work-for-stake (opportunities, bids, holds, approval, payout windows)

| id | item | source | state | DECISION | size |
|---|---|---|---|---|---|
| P4-01 | Seat apply + paid proposal + accept (hold RESERVED) + approve → PAID with window; portfolio shows 4,000 SOLR | MVP-P §C2 Step 4/5 CLOSED, F20, F31 | done-live | DO-NOW: close LR L2.12/OI-66 with evidence; keep in E2E | S |
| P4-02 | Bids on non-OPEN opportunities blocked client-side only | LR L2.12 (OI-67) | missing† | DO-NOW | S |
| P4-03 | Guild bid award split | LR L2.12 (OI-68) | missing | CUT-FOR-MVP (solo worker path tests the idea) | — |
| P4-04 | Paying a matured window out needs a release MANDATE (opened with `AutoRelease:false`); first window matures 2027-01-23; hold sweep must pay first-time custodial beneficiaries | MVP-P F20 ("step-5 boundary"), §C2 Step 5; LR L1.4 (OI-25) | missing | DEFER(hard date: build by 2026-12-23, 30 days before first maturity). The only time-triggered deferral | M |
| P4-05 | Escrow lifecycle has no entry to FUNDED; components mounted nowhere; honest banner deleted | LR L2.10 | missing† | CUT-FOR-MVP — DECIDED 2026-10-26: hold rail is the MVP escrow; P8-10 banner becomes DO-NOW | L |
| P4-06 | Escrow dispute adjudication (funder self-refund of DISPUTED, contributor path) | TR "Guild Review Integrity…", "Guild Joining and Vote-of-Record Integrity" | missing | CUT-FOR-MVP (unreachable while P4-05 is cut) | — |
| P4-07 | Hiring user-blockers: applicant can track/withdraw an application, applicant notifications, single pending application DB-enforced | TR "Projects Hiring Completion", "…Follow-ups (loop 3)" | missing | DO-NOW (this subset only) | M |
| P4-08 | Hiring/board extras: headcount, guild seating, co-manager parity, task edit, due-date sanity, accept-applications toggle | TR "Projects Hiring Follow-ups (loop 2)", "Task Board Workflow… (loop 4)" | missing | CUT-FOR-MVP | — |
| P4-09 | Governance: author can publish, vote and close → PASSED with one ballot; drafts leak; solo projects never reach quorum | LR L4.2; TR "Governance Reachability…" | missing | OWNER-CALL: recommend hiding governance entry points for the MVP; reopen with L4.2 | S |

## 5 · Cash-out & balances (withdraw, payouts, swap, portfolio)

| id | item | source | state | DECISION | size |
|---|---|---|---|---|---|
| P5-01 | No custodial balance read in the BFF (holdings reads are fail-closed stubs; W5.3 read model unbuilt); Money tab renders engineering prose; insufficient-ARDA UX depends on it | MVP-P F27 tail ("there is NO custodial balance read"); LR L2.14 | missing | DO-NOW (blocks UX gold-see-balance and the invest UX) | L |
| P5-02 | Withdraw ARDA → bank (PayoutService; Stripe Connect KYB at first cash-out) | MVP-S D6; PROD §How value moves 4, §KYC; UX gold-withdraw | built-untested | DO-NOW — DECIDED 2026-10-26: IN | M |
| P5-03 | Sell project units back to ARDA (`/swap`; buy-back disabled by default; queue promise unkept) | LR L1.9, L0.5; UX gold-sell-units | gated | DO-NOW — DECIDED 2026-10-26: sell-units IN for MVP (owner overrode the cut recommendation) | L |
| P5-04 | Raw chain address reachable from one sponsor API call | LR L3.5 | missing† | DO-NOW | S |
| P5-05 | Portfolio jargon: "Source configuration" GUID, "Tokens" column, "tokenomics API", Wallet icons; "the node" on the sponsorship flow | LR L3.3 | missing† | DO-NOW | M |
| P5-06 | UX study pass `01-cash-out-edges` on the released env (adapter skeleton, guard unproven) | UXRB §Study setup; UX §Session adapter | gated | DO-NOW after P7-01 | M |

## 6 · Node operator & tenant admin plane

| id | item | source | state | DECISION | size |
|---|---|---|---|---|---|
| P6-01 | Operator bootstrap on the released node: ARDA genesis, `on-ramp` partition (D7 route), price schedule. Today only `dev-node-up` does it | MVP-P F19 resolution (D7), DEMO-RUNBOOK; LR L6.2 | built-untested | DO-NOW: script it as an operator runbook step for the released node | M |
| P6-02 | Tenant provisioning on the released node: tenant user, tenant KYB + first-party KYC (F10), split keys, canonical `Azoa:TenantId` | MVP-P §Interface contract amendment, F10; MVP-S D3; CTX | gated | DO-NOW (operator); the KYC half waits on P1-02 | M |
| P6-03 | Hold/receipt reads scoped to the issuing key and orphaned by key rotation | MEM (owner decisions) | built-untested | OWNER-CALL: recommend authorizing reads by tenant principal, not by issuing key | M |
| P6-04 | `.worktrees/main-release` holds an uncommitted owner note (NodeAdmin public key + fingerprint); it causes 5 architecture-guard failures | MVP-P A1 | gated | OWNER-CALL: recommend committing the note (public material only), then removing the worktree | S |
| P6-05 | node-admin RBAC release (`feat/node-admin-fix` partial, `feat/node-admin-release` aff895da) + owner live drill | OS-RB §7 RESUME 2026-10-18; MVP-S D1 | built-untested | OWNER-CALL: recommend keeping it out of the MVP release (D1); the operator console already works (R4D C1 FIXED-VERIFIED); ship after the first cohort | L |
| P6-06 | Public self-registration open on the node (D-12); wallet sign-in honours policy (H-A) | R4D §Release blockers row 2, H-A, H-B | gated | OWNER-CALL: recommend closing it before real users; ArdaNova provisions tenant-custodial users (PROD §Blockchain operations) | S |
| P6-07 | AZOA frontend shows the ADMIN sidebar group to ordinary users (NEW-1) | R4D NEW-1 | missing | DO-NOW | S |
| P6-08 | Existence oracle: `DELETE /api/action/{other}` 403 vs GET 404 | R4D L6 (partial) | missing | DO-NOW | S |
| P6-09 | Network label says Devnet/offers Mainnet while running testnet | R4D L7 | missing | DO-NOW | S |
| P6-10 | `/login` while signed in wipes the session; expected-state calls answer 404/403/503 | R4D L5, L3 | missing | DO-NOW | S |
| P6-11 | Fee schedule publish of an unchanged schedule stays "version 0" silently | R4D NEW-2 | missing | DO-NOW (show "nothing changed") | S |
| P6-12 | Plain sign-out leaves the JWT valid server-side | R4D L4 | built-untested | OWNER-CALL: recommend keeping local sign-out as-is and labelling "Log out of all devices" as the revoke | S |
| P6-13 | Action builder: no create confirmation, stale list, no validate control | R4D L8 | missing | CUT-FOR-MVP (not on the ArdaNova path) | — |
| P6-14 | Search LOWs: facet total counts hidden users | R4D SEARCH | missing | DO-NOW | S |
| P6-15 | Search LOWs: "already taken" probe, no operator user-search page, in-memory `SearchManager` | R4D SEARCH | missing | CUT-FOR-MVP (scale/ops nicety; revisit when users > 10k) | — |
| P6-16 | WTT stage-key keyring DEGRADED | R4D NEW-3 | gated | DEFER(`Wtt:Enabled`; real-chain only) | — |
| P6-17 | Carried-forward engineering: Ethereum stuck-nonce owner cancel (RV-L5 R1); WTT takeover plain reads (R2) | R4D §Engineering carried forward | missing | DEFER(Ethereum / WTT enabled; real-chain only) | M |
| P6-18 | Carried-forward: caller-verified owner into `IHolonStore`/`IWalletStore.DeleteAsync`; wallet-ownership-proof + submit-signed UI; vocabulary ratchet +26 | R4D §Engineering carried forward | missing | CUT-FOR-MVP (defense-in-depth refactor; external wallets are not in ArdaNova's custodial path) | — |
| P6-19 | Carried-forward: SDK unit tests (`resolvePayoutAccountSetup` etc.), real child-JWT + wallet-signature HTTP flows, NFT real-DB fixtures | R4D §Engineering carried forward | missing | DO-NOW (in the integrated sweep, batch 1) | M |
| P6-20 | Operator facts with no code: mnemonic rotation moves every treasury address; `CustodyMode=Disabled` default | LR L6.3 | missing† | DO-NOW (docs) | S |
| P6-21 | Governance on-chain review of the AZOA primitive (L4.1/L4.3) | LR L4.1, L4.3; PROD §Governance | missing | CUT-FOR-MVP (ArdaNova's vote of record is permanently off-chain) | — |

## 7 · Platform / infra serving plane (deploys, CI, env provisioning, SDK)

| id | item | source | state | DECISION | size |
|---|---|---|---|---|---|
| P7-01 | Publish `azoa-sdk` 0.5.0 (owner `npm login`) → bump client dep + lockfile → push → client deploy green | CTX; UXRB §Study setup; LR L0.7 (OI-01) | gated | DO-NOW (owner does it, first) | S |
| P7-02 | Replace hand-rolled `FiatReversalView` with the SDK type | LR §OI ids not placed (OI-04) | gated | DO-NOW after P7-01 | S |
| P7-03 | Integrated sweep run once + CI green on `main` (OS) | R4D §Release blockers rows 3-4; CTX (test-factory key fixed) | built-untested | DO-NOW | L |
| P7-04 | Make the env-gated categories (Solana validator, Ethereum contracts, WTT six-route, chaos/perf) explicit CI filters so green means green | CTX; WTT §Status | built-untested | DO-NOW | S |
| P7-05 | WTT six-route: SOLANA-REGISTRATION, EXEC, FIRST-RUN | WTT §Status; OS `round4-lane-verification-fixtures.md:70-74` | missing | DEFER(before `Wtt:Enabled` in production; real-chain only) | L |
| P7-06 | Merge `feat/platform-stub-closeout` → `main`, deploy azoa-api from main | CTX; OS-RB §7 (2026-10-17 steps 1-2) | gated | DO-NOW after P7-03 | M |
| P7-07 | Re-enable Railway "Wait for CI" on every AZOA service | R4D §Release blockers row 1 | missing | DO-NOW (owner does it, right after P7-06) | S |
| P7-08 | Released-env value flags: Stripe keys + webhook, `Azoa__EnableFundingCheckout`, custody/outbox flags, `SelectedSettlementNodeId`, attestation expiry, `OnRamp__Enabled` | CTX; MVP-P §B1, F19; MVP-S §Verified state Leg 2 | gated | DO-NOW (operator; follows OWNER-CALL #2) | S |
| P7-09 | Confirm the node runs Simulated + custody outside `Development` (sim reserve seed lives in `appsettings.Development.json`) or pick the chain | MVP-P F14 (pinned seed in appsettings.Development.json), A5 (`CustodyMode=DevelopmentOnly`) | done-spike | VERDICT 2026-10-26: NO, not config-only — sim custody requires literal ASPNETCORE_ENVIRONMENT=Development (CustodyModeResolver.cs:90; PlatformManaged refuses Simulated :128-133) and the node boot-guards Simulated in Production (BlockchainProviderFactory.cs:162-173); Railway hosts also need AZOA__Activation__DeployedHostAcknowledged=true or pins silently no-op (ChainModuleActivation.cs:30-32). QA cohort therefore runs on a Development-labeled QA-scoped node (full env-var list in the spike report); Development relaxations (rate limits, verbose errors, Swagger) go live on that host — scope it to the cohort. KYC remains the independent value blocker (KycManager.cs:634,708; tenant KYB same, F10) until Identomat lifts the trial. | S |
| P7-10 | ArdaNova prod DB migration (human-gated; rehearse on a clone first) | CTX; LR L0.1 (enum + column migrations) | gated | DO-NOW (owner runs it; agent prepares the rehearsal) | M |
| P7-11 | Financial-engine-domain migration (inert vocabulary) | LR §OI ids not placed; TR "Financial Engine Domain Hardening" ("does not activate value movement") | gated | CUT-FOR-MVP | — |
| P7-12 | Railway template `Kyc__Provider=unavailable` throws at Production boot | LR L6.1 | missing† | DO-NOW | S |
| P7-13 | Production activation pins (D-7 DEX/exchange per chain, webhooks, child creds, `DeployedHostAcknowledged`) | R4D §Release blockers row 7 | gated | DO-NOW for the chosen MVP chain's exchange only; others DEFER(per-chain real-value enablement) | S |
| P7-14 | ArdaNova RUNBOOK still describes `azoa-sdk@0.4.0` unpublished / lockfile regen as open items | AN `conductor/RUNBOOK.md` §1-2 | missing | DO-NOW (update after P7-01) | S |

## 8 · Cross-cutting UX polish (copy, empty states, errors)

| id | item | source | state | DECISION | size |
|---|---|---|---|---|---|
| P8-01 | `shouldRetryQuery` wired (10.8 s 404 stall gone) | MVP-P §B3; TR "Design Loop-4 Follow-ups" | done-live | DO-NOW: close in the design-loop4 track | S |
| P8-02 | Custody errors through the copy mapper + backend reason codes for operator messages | TR "Design Loop-4 Follow-ups" | missing | DO-NOW | M |
| P8-03 | Neon button AA contrast | TR "Design Loop-4 Follow-ups" | missing | DO-NOW | S |
| P8-04 | ~70 raw `error.message` sites | TR "Design-System Debt Burn-down" | missing | DO-NOW on planes 1-5 routes only; the rest is held by the `design-gate.test.ts` ratchet | M |
| P8-05 | 156 raw palette classes, 48 untitled routes, 30 white-alpha borders, proposals tab off-system, task swatches, /people default tab | TR "Design-System Debt Burn-down", "Design Loop-3 Follow-ups", "Design Loop-4 Follow-ups" | missing | CUT-FOR-MVP (ratchet prevents growth) | — |
| P8-06 | Honest "funding not set up" empty state | TR "Design Loop-3 Follow-ups" | missing | DO-NOW | S |
| P8-07 | Thesis sweep: chain off credential surfaces, governance wire enum leak, delete unqualified tax advice | LR L3.2, L3.4 | missing† | DO-NOW | M |
| P8-08 | Repo-wide vocabulary control test (thesis ratchet) | LR L3.8 | missing† | DO-NOW (cheap; protects the loop's own output) | S |
| P8-09 | AZOA rail-vocabulary scrubber at all fifteen exits | LR L3.7 | missing† | DO-NOW (these strings reach ArdaNova users via BFF error mapping) | M |
| P8-10 | Escrow "read-only contract" honest banner restored (if P4-05 is cut) | LR L2.10 | missing† | DO-NOW | S |
| P8-11 | Dashboard/feed: anonymous visitor → discover → project page path | TR "Dashboard & Feed Integration" | built-untested | DO-NOW (review the discovery path only) | S |
| P8-12 | Nova AI label/doc follow-ups; profile directory edit/search | TR "Nova AI loop-4 follow-ups", "Profile Entry Editing…" | missing | CUT-FOR-MVP | — |

---

## Tally

103 rows: DO-NOW 71 · OWNER-CALL 12 · CUT-FOR-MVP 13 · DEFER 7. The DEFERs are P1-02, P1-03 (vendor); P2-09, P6-16, P6-17, P7-05 (real-chain only); and P4-04 (hard date). P7-13's non-MVP-chain half is a DEFER inside a DO-NOW row.

## Loop order

Each pass runs: adversarial review (separate context, refute-first) → implement the plane's DO-NOW rows → one sweep on both repos → re-drive the golden path (local tandem stack, plus the released env once P7 lands) → record verdicts in this file.

0. **Owner hands (parallel, before pass 1):** P7-01 SDK publish · P7-07 Wait-for-CI · the five calls below · P1-02 Identomat escalation (contact on file: david@identomat.com).
1. **P7 Platform/infra:** the released env is dark until the client deploys, the node deploys from main, and the value flags are set. Nothing user-facing can be tested before this.
2. **P1 Onboarding & identity:** every user hits it first; also the security P0s (P1-13, P1-14).
3. **P5-01 + P3 Sponsor funding:** the balance read is shared by sponsors and workers, then the money-in correctness rows.
4. **P2 Creator provisioning:** P2-02 security review, dollars-first ask, storage/publish seams.
5. **P4 Work-for-stake:** hiring user-blockers; P4-04 mandate scheduled by 2026-12-23.
6. **P5 Cash-out:** withdraw drill per call #3, de-chained portfolio, first UX study pass (P5-06).
7. **P6 Operator plane:** bootstrap script, AZOA-frontend LOWs, docs.
8. **P8 Polish:** copy, empty states and vocabulary ratchets; then repeat UX study passes across planes 1-5 until gold ≥ 0.8 (UX §Machine config targets).

## The five OWNER-CALLs to answer first

1. **F22 ARDA peg (P3-02):** recommend $1 = 1.00 ARDA (1 base unit per cent). Change the in-code default to `"1"` and LR's "$0.01" text to match. PROD, the add-funds copy and L3.1's dollars-first ruling all assume it.
2. **First-cohort value mode + KYC (P1-02, P7-08/09):** recommend an invite-only cohort on Stripe TEST mode with the chain the P7-09 spike proves (Simulated if the node allows it outside Development). Keep A-4 (no manual-KYC fallback) and escalate Identomat now. Non-value flows (create, browse, apply, seat-join) open before KYC unblocks. Real money only after P3-13/P3-14.
3. **Cash-out scope (P5-02/P5-03):** recommend withdraw ARDA→bank IN (Connect KYB at first cash-out, per PROD). Sell-units CUT until the first liquidity window (2027-01-23); before then no stake is liquid anyway.
4. **F29 404-receipt self-heal (P3-04):** recommend adopting it. A 404 from the node's durable exchange journal counts as proof of no effect; redispatch under the SAME idempotency key, and keep ATTENTION for any 5xx/ambiguous answer (mirrors the node's F21 no-effect marker).
5. **L2.10 escrow entry (P4-05):** recommend CUT-FOR-MVP. The hold rail (F20, PAID with a window) is the MVP's escrow. Hide the unmounted escrow components and restore the honest banner (P8-10).

## Slotted requests (intake)

Owner requests arriving mid-loop land here with an SR id, join the active plane's
pass (or open one), and close with a verdict like any other row.

| id | request (owner's intent) | arrived | decision | size |
|---|---|---|---|---|
| SR-01 | Brand copy pass: all user-facing copy written for humans in the ArdaNova voice — serious, inspiring, mission "aiding those who find problems and solve them" (Arda-Nova.pdf manifesto: fair share, earned membership, one member one vote, transparency, dignity; NOT crypto-hype). Strip AI-forward framing and "humans are always included" disclaimers — AI features are not the story. Copy must stay truthful to the shipping platform (no Holochain or other unimplemented-tech claims from the deck). Optional: Higgsfield media, only within the minimalist brutalist editorial aesthetic. | 2026-10-26 | DO-NOW (plane 8, runs alongside the active pass) | M |
| SR-02 | Documentation overhaul: update the repo docs to describe what the system BECAME — the four-tier architecture with AZOA slotted in as the settlement/custody node (client -> tRPC thin proxies -> .NET BFF as the only Azoa credential holder -> AZOA node; per-project tokens, consent lifecycle, holds/ReleaseIntoHold, settlement outbox, economy modes fail-closed, Identomat KYC), the released Railway topology, the local tandem dev stack, and the ArdaNova–AZOA integration contract extended with the exchange/hold/consent state machines (both repos' copies). | 2026-10-26 | DO-NOW (plane 7/8; starts when the F29 + copy lanes release their partitions) | L |
| SR-03 | Feed consolidation: ONE polymorphic feed where a guild, a project, an opportunity, a comment/update on any of them — anything feedable — is a postType rendered dynamically; project activity and collaboration ARE the feed. Broaden tags/taxonomies beyond tech-SaaS (any problem domain: construction to hospitality). User-generated content becomes the main copy on feed pages; the serving algorithm optimizes constructive engagement — visibility into developing, implementing and funding projects ("addictive but constructive"). | 2026-10-26 | INTAKE — architecture + taxonomy + algorithm decisions go through the owner question loop (batch 1); build rows follow the answers | L |
| SR-04 | Released-env visibility: owner expected to SEE run data on the live deployment and reports "basically nothing on the backend works". Diagnosed 2026-10-26: run data WAS there (13 QA personas of 19 users, newest 04:09 that day) but ZERO money rows (project routes 500'd on S-04 pre-migration; value flags still off per P7-08) and near-zero content (1 project from Feb, 2 guilds, 2 opportunities, 0 posts) — broken routes + dark value rail + empty content stack into "nothing works". | 2026-10-26 | DIAGNOSED; P7-15 RUN same day (owner un-gated). Successors: P7-08 (value flags), P7-16 (content seeding decision, question loop) | M |

## MILESTONE 2026-10-26 — AZOA CI FULLY GREEN (run 40309480218, main 93e60167)

All nine GitHub Actions jobs green for the first time in the repo's history
(from 31 unique test failures + 3 red jobs at the session's start). Closing
fixes along the way: test-host composition (wallet key), the fan-out publish
regression, five stacked Action-family causes, two concurrency 500s + the
on-ramp lot claim + THREE saga-store retry misuses (a conflict swallowed as
"lost the race" starved G2's 20-claimer gate to zero winners), the
leading-zero secp256k1 key pad, self-hosted fonts (hermetic builds), the E2E
settlement rail on the Simulated modules, and the consent fix: the journey's
payer stays an UNCLAIMED tenant-custodial member because claiming revokes
every live grant (contract §12.3.2). Railway "Wait for CI" is now actionable.

TRUE BLOCKERS REMAINING (owner):
1. P7-15 — DONE 2026-10-26 (owner un-gated it mid-loop; see Loop iteration 2).
   S-04 cleared: 15 pending migrations applied, awardAmountAtoms present.
2. P1-02 — Identomat trial account (vendor): real KYC on the released env.
3. Flip Railway "Wait for CI" ON (now that the gate exists).
4. Do NOT rotate the AZOA value key until P6-03/P3-16 land (double-settle
   hazard, contract §12.0.4).

OPEN DO-NOWs (loop queue): P5-03 sell-units build-out (L), pass-3 UX re-run
after P7-15, P3-15 reopen-count column, P8-13 home money entry, SR-01 media
(icons), P6-03 tenant-scoped receipt reads, and three more bare CREATEs the
lanes flagged for the same conflict-retry wrapper (StripeWebhookInbox.cs:244,
FiatPayoutLedger.cs:112, TenantPayoutLedger.cs:114/138).

## Loop iteration 1 — outcomes (2026-10-26, in-flight log)

- CLOSED P3-02 (peg): dev override + AllowPriceUpdate removed from local-stack.ps1; add-funds copy now says $1 adds 100 ARDA; project-create contract already correct (ARDA_GENESIS_USD_RATE=0.01).
- CLOSED P3-04 (F29): typed-404-only proof-of-no-effect; reopen refunds its attempt inside the same guarded update (EconomicOutboxLeaseStore.cs:191); 5-reopen ceiling then ATTENTION; quiet window from first ambiguity. Root causes found in code: reconcile reads burned attempts, and untyped 404s were trusted.
- CLOSED P4-05/P8-10: escrow UI confirmed unreachable (import != reachable), parked note in src/AGENTS.md; honest hold-rail banner on tasks/[id]/commerce.
- CLOSED SR-01: 47 strings, 15 files, manifesto voice; disclosure copy (where ideas are sent) deliberately kept; icons left for the media task.
- CLOSED P7-07-ish (client release): azoa-sdk 0.5.0 published (owner) + builder-platform lockfile regen; client live at HEAD with /api/health identity; uxqa guards proven OK.
- NEW ROWS from the lanes:
  | id | item | decision | size |
  |---|---|---|---|
  | P3-15 | Real `noEffectReopenCount` column (DBML + entity) replacing the failureDetail note; note is fragile if failureDetail is ever rewritten | DO-NOW next DBML change batch | S |
  | P5-06 | PayoutService now treats untyped 404 as unknown (units stay PROCESSING instead of unlocking) — verify desired in the cash-out plane pass | DO-NOW (plane 5 pass) | S |
  | P8-11 | Topbar badge says "Member"; decide whether to show actual role/verification level (small logic change) | OWNER-CALL (cosmetic) | S |
  | P8-12 | Nova Sparkles/Bot icons remain the last AI-forward cue; swap in the media task within the brutalist aesthetic | DO-NOW with SR-01 media follow-up | S |
  | P3-16 | SAFETY (found by SR-02, contract §12.0.4): F29's typed-404 reopen + key-scoped receipt reads = double settlement if the value key rotates while anything is AWAITING_RECONCILIATION — a new key reads NOT_FOUND for an exchange that exists. Interim rule (unenforced): never rotate with in-flight reconciliation. Real fix pairs with P6-03 (node authorizes receipt reads by tenant principal) or a BFF key-fingerprint-at-dispatch guard that parks instead of reopening on fingerprint change | DO-NOW before any key rotation; blocks P6-03 execution order | M |
  | P7-14 | Deploy-path doctrine conflict (SR-02 Q3): Railway builds azoa-api from GitHub main (what actually happened today) vs oasis-sleek DEPLOY.md's pinned ghcr.io image promotion; client README also claims pinned images. Pick the canonical path and align the docs | OWNER-CALL | S |
- AZOA CI verdict (run 40263034250 vs prior 40259375452): our push cut unique test failures 31 -> 23; every fixed red was ours to fix (composition keys, LocalChain quarantine). Zero real new failures (the one new name, the consent expiry/revoke race test, passes 32/32 locally — CI-runner timing flake). gitleaks history scan: 6 hits, all dead QA-session keys in history-only files at five old commits — commit-scoped allowlist pushed, local scan with the CI image now clean over 575 commits.
- TRUE BLOCKERS remaining for "CI green on main":
  | id | item | decision | size |
  |---|---|---|---|
  | P6-18 | 22 Action/workflow-family integration reds, main's own deferred tenant-AI-workflow QA ("The signing user is unavailable" 400s across ActionArdanovaFlow/GraphBundle/Lifecycle/Semantics) | DO-NOW — next azoa loop iteration | L |
  | P6-19 | E2E journeys: ROOT-CAUSED 2026-10-26 — 4/5 journeys pass every run; only stripe-on-ramp fails (settlement webhook 500: the E2E stack's node lacked a simulated settlement rail; frontend/e2e/AGENTS.md §Current settlement limit, hermetic-stripe-settlement-followup.md). The "design decision" it waits on IS the now-merged Simulated fungible/recipient-prep/exchange modules + the dev-node genesis bootstrap: fix = activate the Simulated economy on the E2E stack's backend config and seed a verified platform unit (mirror Ensure-ArdaGenesis). Interim option: test.fixme the one settlement assertion so the 4 green journeys stop being masked | DO-NOW — wiring, not design; runs after the sim-NFT lane frees the tree | M |
  | P6-20 | Consent expiry/revoke race test flaky on CI runners only; widen the expiry-notification wait | DO-NOW — trivial, same iteration | S |
- UX pass 01 (genuine run, 44 runs): gold 1/4; 25 synthesis findings (7 HIGH); four fix commits landed for 5 HIGHs (fake-$0/readiness/jargon/error-mapping/drawer-focus), pending UX-harness re-verification. S-04 ROOT-CAUSED: released BFF throws Postgres 42703 "column f.awardAmountAtoms does not exist" — the human-gated commerce migration (documentation/GATED_COMMERCE_MIGRATION_RUNBOOK.md) has never run on the released DB, so every project read/create 500s regardless of role.
  | id | item | decision | size |
  |---|---|---|---|
  | P7-15 | OWNER (TRUE BLOCKER for creator/invest on the released env): run the gated commerce migration runbook on the released Postgres (fingerprinted preflight, reviewed additive migration, assetScale contract). Until then S-04 stands and /projects/[slug], create, invest all 500 | CLOSED 2026-10-26 — owner un-gated it; executed via `prisma migrate deploy` (see Loop iteration 2 evidence) | M |
  | P8-13 | S-05: signed-out home has no money framing or entry point (highest-friction route, 339) — product copy/IA decision feeding the next scenario (project discovery) | DO-NOW plane 8 after owner sees it | M |
- P6-18 CLOSED (2026-10-26, commit 170ca668): five stacked causes — test login violated "token holders are registered users" (no user row, no exp), a WIP checkpoint had downgraded publish's fan-out refusal to a warning (product regression, restored), missing Idempotency-Key, execute went durable (tests now settle runs via the job queue), graph-bundle contract drift. Action family 63/64; unit ~Action 855/855. P6-20 CLOSED the strong way: exact event count from RecordExpiryAsync, not a longer wait.
  | id | item | decision | size |
  |---|---|---|---|
  | P6-21 | Simulated NFT module | CLOSED 2026-10-26 (fbe4e892): in-memory ownership ledger, deterministic sim:tx, zero-fee, soulbound stays refused; 6 unit tests + conformance 83/83 | M |
  | P6-22 | Non-Action CI reds | CLOSED-minus-Ethereum (fbe4e892): DappSeries/Holon tests ported to real contracts (Mint/Exchange deliberately route to NFT lifecycle / reserve exchange per M9); TWO REAL product 500s under concurrency fixed with the house transient-conflict retry (action-workflow admission enqueue, wallet-auth challenge budget+CAS); user-delete race sweeps under the migration worker's lease; Nethereum key-zeroing fixture hazard fixed (zeroes a copy, not the provider's internal array — audit the nine SigningKeyMaterial usages, see lane note) | M |
  | P6-23 | Ethereum idempotent re-submit (REAL money-path gap, left red on purpose): after a mined tx, a second Submit re-broadcasts and Hardhat refuses "Nonce too low"; SubmitPreparationAsync folds it into requires-reconciliation (fail-safe, never pays twice, but not the idempotent re-submit the interface promises). Fix: on send error, read eth_getTransactionByHash for the exact stored hash and return quietly if known (or persist Confirmed at observe). ~4 lines ON the §broadcast-outcome value path — requires adversarial review before merge | DO-NOW — reviewed lane with P6-19 wiring | S |

## Loop iteration 2 — outcomes (2026-10-26, in-flight log)

- CLOSED P7-15 (commerce migration on released Postgres). Owner un-gated it
  mid-loop ("the migration should not be human gated for now"). Executed state,
  NOT the worst-case adoption path: the released DB already carried Prisma
  history (baseline 20260718000000 + the four July release migrations, all
  finished, none rolled back — the adoption documented in the runbook had
  already happened on this database). What was missing: the 15 versioned
  migrations 20260908000000..20261018120000. Procedure actually run:
  1. Classification: `_prisma_migrations` inspected; destructive statements in
     the pending set reviewed (all by-design retirements: float-money backfill
     then drop, card-rail retirement incl. StripeWebhookEvent, FundingIntent
     exchange reshape with in-flight rows marked FAILED, dedup DELETEs ahead of
     unique constraints).
  2. Backup (restore evidence): pg_dump -Fc, 305,888 bytes, SHA256
     27532CB17703E3F28F9D45F6F7EFF565EE47F6282CCAD3D93F9598A49A8E0A99
     (session scratchpad, ardanova-released-pre-migration-20261026.dump).
  3. `npm run db:migrate` (= prisma migrate deploy): all 15 applied, exit 0;
     `prisma migrate status` after: "Database schema is up to date!" (20/20).
  4. Verified: FundingIntent.awardAmountAtoms exists (S-04's 42703 column).
  5. Both services redeployed (Ardanova-BFF, Ardanova-Client) so no pooled
     connection serves a stale prepared plan against altered tables.
  Value-moving paths were already disabled on the env throughout (no Stripe
  keys, Azoa__EnableFundingCheckout=false, economy reports disabled).
- SR-04 DIAGNOSED (released-env data visibility): see intake row. Key numbers:
  13/19 users are QA personas (data arrived), 0 money rows, 1 project/0 posts.
- NEW ROW:
  | id | item | decision | size |
  |---|---|---|---|
  | P7-16 | Released env is content-empty (1 project, 0 posts): decide seeding strategy — curated demo/seed content vs organic-only vs QA-persona-generated content as part of UX passes | OWNER-CALL (question loop) | M |
- CLOSED S-04 the whole way (Playwright MCP, signed in as qa-sponsor-ana via the
  7-day uxqa session cookie): `/projects` now renders HTTP 200 with the
  manifesto-voice copy and an honest empty state; `/portfolio` and
  `/portfolio/add-funds` render with the $1→100 ARDA peg copy and the honest
  "you cannot cash out yet" gate. The project routes that 500'd before the
  migration are live. (The lone Feb project stays hidden because it is
  unpublished, not because of an error.)
- WIRED the released value rail toward live-test (owner: "push up the keys …
  seed the data while testing … ungating and unflagging as needed"):
  * Stripe TEST: created webhook endpoint we_1UUnXB… → azoa-api
    /api/fiat/webhook/stripe (enabled, all events); pushed Fiat__Environment=test
    + the sk_test/whsec pair from oasis-sleek/.env (STRIPE_SECRET_API_KEY_DEV).
  * azoa-api economy: ASPNETCORE_ENVIRONMENT=Staging + the full Simulated-economy
    env matrix mirrored from docker-compose.e2e.yml (Blockchain__Mode=Simulated,
    Devnet, CustodyMode=DevelopmentOnly, the reserve + Exchange activation flags,
    Webhooks__Enabled). Two boot crashes fixed on the way: DataProtection__KeyRingPath
    required outside Development (set from appsettings.Production.json).
  * Provisioned the ArdaNova tenant + ARDA platform-unit genesis on the released
    node (.omc/scripts/provision-released-azoa.ps1 — idempotent, re-runnable;
    .omc/scripts/get-released-dburl.ps1 sets DATABASE_URL without echoing it;
    both gitignored. Mirrors dev-node-up §5 +
    ensurePlatformUnit): tenant 3820397b-be9c-47a8-8cc1-d6342633155b, two-key
    contract minted, ARDA asset 5589412360996711624 activated on Simulated/Devnet
    (supply 2,000,000,000; on-ramp partition budget 1e9). State in
    .omc/state/released-azoa-tenant.json.
  * BFF: pushed the two tenant keys + Azoa__TenantId + EnableCustodialAccounts +
    EnableFundingCheckout + OnRamp__Enabled + OnRamp__AssetId/ChainType + the
    settlement attestation; /health now reports economy:"simulated".
- BLOCKER found at the custody seam (P7-17, below). The money path stops at
  custodial-account enrolment: azoaCustodialAccount.ensure → node PUT
  /api/tenant/custodial-accounts/{subject} → **403, "KYB gate denied
  CustodialAccess for tenant …"**. KybGate.RequireVerifiedTenantAsync
  (Services/Kyb/KybGate.cs:48) fails closed unless the tenant holds an APPROVED,
  unexpired kyb_verification row bound to the node's current Identomat authority
  revision. On the local dev stack dev-node-up's Ensure-TenantKyb satisfies this
  through the KYC sidecar's dev verdict seam; the released node has no sidecar and
  KYC_PROVIDER/Identomat is the trial account that cannot create configs (P1-02).
  So there is no legitimate path to a KYB-approved tenant on the released env
  today. I deliberately did NOT hand-write an APPROVED kyb_verification row into
  the node DB to force it — that fabricates a compliance record, and the gate
  exists precisely to refuse exactly that. This is the real edge of "test-mode
  but production-shaped": TEST money (Stripe 4242) is fine, but KYB is a live
  compliance authority with no test backdoor on the released node.
- NEW ROWS:
  | id | item | decision | size |
  |---|---|---|---|
  | P7-17 | Released money path is KYB-gated at custodial enrolment (403 KYB gate denied CustodialAccess). No test seam to approve a tenant's KYB on the released node without Identomat configs (P1-02). Options for owner: (a) stand up the Identomat KYB config (vendor, unblocks P1-02 too); (b) add a Production-refused, non-Development-custody operator test seam in the node to stamp an APPROVED kyb_verification under an explicit test flag (code change + adversarial review — this is the legitimate version of what a hand-written DB row would fake); (c) keep the released env's money path dark and exercise it only on the local tandem stack / E2E where the dev sidecar exists | OWNER-CALL (blocks released-env money e2e) | M |
  | SR-05 | Architecture intent (owner): AZOA owns payments end-to-end; even ArdaNova's front-end money components should be consumed via the azoa-sdk, possibly exposing provisioning UI hooks for micro-frontend-style gates and form steps (KYC/KYB steps, add-funds, cash-out) rather than ArdaNova re-implementing them. Reframes the BFF-thin-proxy money surfaces as SDK-driven embeds | INTAKE — architecture decision for the question loop; affects SR-03 feed work and the client money routes | L |
- NEXT (owner-gated now): the released-env money e2e waits on P7-17 (KYB). The
  UX pass-3 re-run of 01-cash-out-edges CAN run against the current released env
  (the surfaces render; the cash-out path will truthfully show the honest gate).
  P7-08 value flags are effectively DONE for the on-ramp except the KYB gate.

## Owner question loop — answers (2026-10-26)

Four highest-blast-radius ledger decisions, answered via AskUserQuestion:

- **P7-17 (released money KYB gate): PURSUE IDENTOMAT (vendor).** No node test
  seam, no fabricated KYB row. The released-env money e2e is therefore DEFERRED
  behind P1-02 (Identomat lifts the trial / creates KYC+KYB configs). Money e2e
  runs on the local tandem stack / E2E (dev KYC sidecar) until then. The released
  on-ramp stays honestly gated end-to-end (correct "can't cash out yet" copy).
- **SR-03 (polymorphic feed): FULL BUILD NOW.** One feedable postType union
  (guild, project, opportunity, comment/update, …) rendered dynamically; broaden
  taxonomies beyond tech-SaaS (construction→hospitality); UGC becomes the main
  feed copy; constructive engagement/visibility ranking. DO-NOW (L). Plan-first
  because blast radius spans DBML→Prisma/EF→API→tRPC→every feed surface.
- **SR-05 (money via azoa-sdk): ADOPT NOW.** ArdaNova's money surfaces
  (add-funds, withdraw, sell-units, KYC/KYB steps, custodial-account status)
  become azoa-sdk-consumed micro-frontend hooks/embeds rather than
  re-implemented client routes. DO-NOW (L). Plan-first; may require the SDK to
  grow UI hooks it doesn't yet expose (two-repo change).
- **P7-16/SR-04 (released content): QA-PERSONA ORGANIC.** No curated seed step;
  the UX-pass personas generate content as they run journeys. Folds into the
  UX loop; no separate content-seeding lane.

Open OWNER-CALLs NOT asked this round (taken with their recommended defaults as
stated assumptions; each blocks only its own row, trivially reversible):
P8-11 topbar role badge (keep "Member"), P7-14 deploy-path doctrine (canonicalize
on GitHub-main build — what actually ships), P4-09 governance reachability (hide
governance entry points for MVP), P1-12 Terms/Privacy text (owner approves before
first charge — already gated by P7-17), P6-12 plain sign-out (local sign-out as-is),
P6-03 tenant-scoped receipt reads (authorize by tenant principal — still HELD
behind the P3-16 key-rotation hazard). Any of these can be reopened on request.

## Loop iteration 3 — SR-03 + SR-05 planning (2026-10-26, in-flight)

Both owner-chosen builds are L and cross-cutting, so this iteration grounds them
with read-only planning passes (current data model, taxonomy surfaces, azoa-sdk
export surface, client money routes) before any schema or route change. Plans +
file partitions land here; execution follows as partitioned lanes with the usual
phase-boundary review.

### SR-05 plan (planner, 2026-10-26) — money UI from azoa-sdk, privileged calls stay in the BFF

- **Auth decision: option (b), injected transport.** The SDK owns UI, state
  machines and view types; each component takes a host-supplied
  `AzoaMoneyTransport`. ArdaNova implements it over tRPC, so every privileged
  action stays browser → tRPC → BFF (actor assertion, src/server/actor-assertion.ts
  :88-110) → node. **No AZOA bearer of any kind reaches the browser.**
  Rejected: (d) node child JWTs — feature off in production, no fiat endpoint
  accepts them, and FiatCheckoutController.cs:179-194 treats a child JWT as a
  FIRST-PARTY login: it skips the units:issue consent probe and namespaces the
  admission key `self`, so the same requestKey via BFF and via browser would NOT
  dedupe (double-charge vector). (a) BFF-minted embed token adds a second bearer
  for nothing (all provider steps are already hosted redirects). (c) server
  components can't serve both hosts (React 18.3/Next 16 vs React 19/Next 15).
- **The SDK has NO UI layer today** (no React dep, tsconfig has no DOM, no .tsx).
  It grows two subpaths: `azoa-sdk/money` (headless: transport interface,
  MoneyResult/MoneyRefusal codes, narrowed views that drop admissionKey and
  subject/tenant ids, lifted persisted-request-key logic,
  `runMoneyTransportContract`) and `azoa-sdk/react` (unstyled slot-based
  components + hooks; React optional peer; `"use client"` banner). Ships as
  **0.6.0** (CHANGELOG already holds a pending breaking change).
- **Phases** (disjoint file partitions; old routes stay default behind
  src/lib/azoa-money/flags.ts; no big-bang): P0 headless core → P1 react layer
  (SDK, oasis-sleek) → **PUBLISH GATE A (owner: `npm publish azoa-sdk@0.6.0`)** →
  P2 AZOA dogfood ∥ P3a BFF stable refusal codes ∥ P3b ArdaNova tRPC transport +
  Origin check + bump → P4 add-funds, P5 account/KYC/consent, P6 withdraw, P7
  sell-units, P8 balance (parallel once P3b lands) → P9 per-surface cleanup
  after soak. PN (node) runs alongside to unblock F1.
- **Stays OUT of the SDK:** key selection, actor assertions, return-URL building,
  readiness/eligibility policy; the project-token domain (invest/funding-intent,
  disclosures, sell-units PRICING); operator money (processPayout, reversals,
  payout float); KYB (tenant-level, needs the tenant key).

| id | item | decision | size |
|---|---|---|---|
| P5-07 | **Money-safety bug (verified):** request keys live in a React `useRef` and the server mints a fresh one when absent. add-funds (page.tsx:170, router add-funds.ts:69) and invest (page.tsx:301, router funding-intent.ts:26). A lost response + reload admits a SECOND payment instead of resuming. Invest is the worse case: it debits ARDA balance through the outbox, so it is a duplicate debit. Invest is NOT moving to the SDK, so SR-05 alone would never fix it | DO-NOW — lane running: persisted, subject-scoped request keys + server keys REQUIRED | S |
| P5-08 | **F1 blocker for withdraw (owner decision: withdraw is IN):** the BFF starts payout-destination setup with the tenant custody key, but the node's F-H8 rule refuses EVERY tenant principal there (oasis-sleek FiatPayoutController.cs:112-117). Nothing in ArdaNova accounts for this, so withdraw cannot finish for any custodial user through any tenant-mediated design. Needs a node-side account-holder handoff: an AZOA-hosted step where the person proves presence before the node mints the Stripe Connect link. Security-sensitive identity design on a money exit | OWNER-CALL (how a custodial user proves they are the account holder) → then PN lane | M |
| P5-09 | `/api/trpc` route has no Origin / Sec-Fetch-Site check (CSRF defence in depth for money mutations) | DO-NOW in SR-05 P3b | S |

LANES RUNNING (2026-10-26): SR-05 P0+P1 (SDK, oasis-sleek branch
feat/sdk-money-react, no push, no publish) · P5-07 idempotency fix (ArdaNova
client, uncommitted) · SR-05 P3a refusal codes (ArdaNova BFF, uncommitted).
Payment-path diffs get an independent review before commit.

### SR-03 plan (planner, 2026-10-26) — one polymorphic feed

- **Storage: every feed item is an ENVELOPE ROW in the existing `Post` table**
  (type = PostType; provenance `(sourceType, sourceId)` UNIQUE; nullable target
  FKs projectId/guildId + new opportunityId/eventId). Chosen over a separate
  FeedItem table (would need parallel like/comment/share tables) and a read-time
  UNION (~10 sources each with its own visibility join; nothing indexable).
  Envelopes reuse PostLike/Comment/Share/Bookmark, the counters, PostVisibilityPolicy
  and OnAVisibleProject (PostService.cs:474).
- **Written by a PULL projector, not an EF interceptor and not by editing each
  service.** `FeedProjectorCycle` (Application) + `FeedProjectorHostedService`
  (Infrastructure), mirroring the EconomicOutbox worker split; ~5s cycle scans
  committed source rows; hourly 48h sweep removes envelopes whose source is
  gone; backfill = same code, unbounded window; Postgres advisory lock for a
  single runner. Rejected the interceptor because the Application layer has 7
  BeginTransactionAsync call sites: an interceptor would project a funding change
  that later ROLLS BACK (a phantom feed item), and a unique-index race inside it
  would abort the domain write itself.
- **Taxonomy:** a `TaxonomyTerm` lookup table (SECTOR / SKILL / PROJECT_TYPE).
  The 10 legacy category codes are seeded AS sector slugs, so existing rows
  resolve with no backfill; text columns stay text (a vocabulary, not a
  constraint). ProjectType gains ADDITIVE enum values only (SERVICE,
  PHYSICAL_BUILD, VENUE, PROGRAM, RESEARCH, COOPERATIVE).
- **Ranking (.NET):** a stored, viewer-independent quality score
  (Post.trendingScore, an existing column, so far never computed) × per-viewer
  recency decay × affinity × novelty boost for new projects. Diversified (≤2 per
  subject, ≤3 per author per page of 20; every 5th slot is a subject you don't
  follow). Each card shows its Reasons[]. Replies are capped by approval so
  flame threads can't climb. No views, dwell time or negative reactions as
  signals. Funding items are aggregate % only (no backer names, no amounts).
- **DEPLOY ORDER (non-negotiable):** client tolerance (Lane D) → migrations →
  backend → the rest of the client. Migrations are additive, so they are safe
  under the old running code; the backend must not deploy before them or S-04
  repeats (42703 on a missing column).
- Phases: P0 schema (Lane S) + client tolerance (Lane D) → P1 projector (A),
  taxonomy backend (C), client taxonomy plumbing (D2) → P2 ranker + feed API (B),
  client plumbing (E), card renderer (F) → P3 Home feed (G), scoped feeds on
  detail pages (H), taxonomy facets + UGC-led cards (I). Flow test at P3: persona A
  follows a construction project; B posts; within one cycle A's Home shows B's own
  words as the card body with a "Because you follow…" reason.
- Correction recorded: the DBML→Prisma step is `npm run generate:dbml`, NOT
  `generate:prisma`; `generate:prisma:push` does a `db push` and must never run
  against shared data.

| id | item | decision | size |
|---|---|---|---|
| P8-14 | **Post-type forgery (existing bug):** PostService.cs:185 lets any caller create a post typed MILESTONE or TASK_COMPLETED — system-only kinds | DO-NOW in SR-03 Lane A (user-authored type allowlist) | S |
| P8-15 | **Category filter substring collision (existing bug):** ProjectServices.cs:151/:438 use `categories.Contains(x)`, so BUSINESS matches SMALL_BUSINESS; gets worse with a broad vocabulary | DO-NOW in SR-03 Lane C (whole-value match) | S |
| P8-16 | **Opportunity type filter matches nothing (existing bug):** app/opportunities/page.tsx:70-77 offers "Bounty"/"Freelance"/…, which never equal the OpportunityType enum values (DBML:1936), so the filter at :217 always returns empty | DO-NOW in SR-03 Lane I | S |
| P8-17 | Live copy contradicts the owner decision: app/page.tsx:180 "Not an endless feed", app/dashboard/page.tsx:280 "No engagement score. No infinite feed.", server/api/routers/post.ts:11 "there is no global feed" | DO-NOW with SR-03 Lane G (brand-voice aligned, SR-01) | S |

LANE RUNNING: SR-03 Phase 0 (Lane S + Lane D) in an isolated worktree (own
branch; scratch-DB proof on local PG 5432; no push; released DB untouched).

## Owner question loop — round 2 answers (2026-10-26)

- **P5-08 (withdraw blocked by F-H8): "Trust Stripe's identity match."** The
  tenant (ArdaNova) may START payout-destination setup; the node releases payouts
  to that destination ONLY once the identity Stripe verified on the connected
  account matches the person's AZOA KYC record. The safeguard moves from "who may
  start setup" to "who may RECEIVE money". Residual risk accepted by the owner: a
  tenant holding a person's real identity documents could pass Stripe's checks
  as that person. → PN lane (oasis-sleek worktree feat/payout-identity-match);
  security review mandatory before merge.
- **SR-03 feed ending: "Infinite ranked scroll."** No "You're caught up" stop:
  ranking continues, mixing in discovery items from subjects the viewer doesn't
  follow. The constructive safeguards stay: no views, dwell time or negative
  reactions as signals; Reasons[] on every card; diversity caps; aggregate-only
  funding. Changes Lane B (ranker paging past the candidate pool), F (stream
  renderer) and G (Home).

## Loop iteration 3 — lane results (2026-10-26)

- **P5-07 idempotency fix: DONE (uncommitted, under review).**
  src/lib/commerce/persisted-request-key.ts claims and persists the key before
  the request leaves; the saved value carries an owner fingerprint, so another
  person's saved key is dropped on sight; a changed amount returns `pending`
  rather than silently replacing. Router keys are now REQUIRED. Invest dedupe
  verified in the BFF: FundingIntentService.cs:185-189 finds (funderUserId,
  idempotencyKey) BEFORE creating anything and replays. 19 tests; tsc clean.
- **SR-05 P3a refusal codes: DONE (uncommitted, under review).** MoneyRefusal.cs
  is the single mapping authority. KEY FINDING: AzoaNodeClient.MapError
  (AzoaNodeClient.cs:193) collapses every node 403 except KYC_FORBIDDEN into one
  generic "AZOA node denied the request.", and Result<T> has no Code field. So the
  code is inferred from the CALL SITE (each route family has one documented 403
  cause), with prefix checks that start working once codes are re-threaded. 11
  controller-level tests.
- **SR-05 P0+P1 SDK: DONE** — oasis-sleek branch feat/sdk-money-react @74d0c7e5
  (sibling worktree oasis-sleek-sdk-money-react), 362/362 tests, `"use client"`
  on the react entries only (a post-build plugin; tsup's banner is stripped by
  treeshake and would also mark server-safe subpaths client-only). Under
  security review; **publish gate A waits on that verdict.**
- **SR-03 Phase 0: DONE** — branch worktree-agent-a4577bc643a9e0eeb @c85ed74.
  Four guarded migrations; scratch-DB proof: deploy ×2 (second applies nothing),
  migrate diff --exit-code 0, all four files re-run directly as no-ops, and the
  destructive dedupe exercised on seeded duplicates. BFF 552 + 1317 green, client
  674 green. Tolerant reader on ProjectType reads (`ProjectTypeCode =
  ProjectType | (string & {})`). Under review before the released-DB migration.
- NEW ROWS:
  | id | item | decision | size |
  |---|---|---|---|
  | P3-17 | Re-thread node error codes through the BFF: add a Code to Result<T> / the AzoaResultEnvelope pass-through so MoneyRefusal stops inferring from the call site (its prefix checks are dead code until then) | DO-NOW after SR-05 P3b | S |
  | P3-18 | PayoutService sale-restriction refusals (hold windows, trading restrictions, PayoutService.cs:990) fall back to `setup_required`, which undersells "these units are held until March". Needs a refusal code (or a retryAfter/heldUntil) | DO-NOW with SR-05 P7 sell-units | S |
  | P8-18 | PostService.LikeAsync/BookmarkAsync check-then-insert: with the new UNIQUE (postId,userId) a double-tap now surfaces 23505 as a 500 (previously a silent double count). Treat a unique violation as already-liked, as the vote/guild-review services do | DO-NOW in SR-03 Lane A (owns PostService.cs) | S |
  | P7-18 | `generate:csharp` strips hand-written comments from 14 committed entity/enum files on every run (restored by hand in Phase 0). Move those notes into the directory AGENTS.md or teach the generator to preserve them | DO-NOW (next DBML batch) | S |
  | P7-19 | Local env fact: PG16 (password 526152) is on **5433**; 5432 is PG17 with a different password. The user's global CLAUDE.md still says 5432 — owner to update | OWNER (doc) | S |

REVIEWS RUNNING: ArdaNova money diffs · SDK security (publish gate) · SR-03
Phase 0 migrations (migrate gate). BUILD RUNNING: PN payout identity match.

## Review gate verdicts (2026-10-27)

| review | verdict | the finding that decides it |
|---|---|---|
| ArdaNova money diffs (P5-07 + P3a) | **CHANGES_REQUESTED** (1 critical, 3 high, 6 med, 1 low) | REGRESSION: invest clears the saved key on a 400 the BFF can send AFTER the node moved funds (ExceptionHandlingMiddleware maps ArgumentException / SQLSTATE 22xxx/23503/23514 → 400 post-commit) → Retry mints a new key → second debit. The old useRef key never cleared on error |
| SDK 0.6.0 money/react | **BLOCK PUBLISH** (2 high, 3 med, 8 low) | H1: `releasesRequestKey` is a denylist, so ANY unrecognized code wipes the key; H2: status `complete` counted as settled while settlement is still `payment_received` (and `not_started` when a Stripe re-read fails) |
| SR-03 Phase 0 migrations | **SAFE TO MIGRATE, with conditions** | Npgsql loads Postgres types once per data source: a new BFF that connected BEFORE the migration can't write posts until restarted, with /health green. The 002000 dedupe undercounts likesCount in the very race it cleans (EF writes `likesCount += 1` as an absolute value, so 2 racing rows bumped it once) |

**Common root cause** (both money reviews): a saved key was released by HTTP
status, but on these paths 400/403/409 each mean several things. **Rule adopted:
release a saved key ONLY on an allowlisted, server-set money `code`; anything
unknown, missing, transient or ambiguous KEEPS the key.**

**Shared contract** — the node adds a machine `code` to its same-key 409s
(HTTP status and message unchanged); the BFF and SDK map them identically:
`INTENT_FINISHED`, `INTENT_EXPIRED`, `REQUEST_KEY_IN_USE` → `definite` (release
the key); `INTENT_NEEDS_REVIEW` → `unconfirmed` (keep). A 409 without one of these
stays `unconfirmed`. Payout release gains `PAYOUT_IDENTITY_UNMATCHED`.

## Owner question loop — round 3 (2026-10-27)

- **PN lane stopped by its own rule.** "Trust Stripe's identity match" can't be
  built on Express accounts: after an Account Link exists Stripe returns only
  first/last name, and AZOA's KYC person key is HMAC(document number, issuing
  country) (KycPersonKeyDeriver.cs:84-119), which Stripe Connect never returns.
  Design + evidence committed (oasis-sleek feat/payout-identity-match @82f67067,
  Services/Fiat/OffRamp/AGENTS.md §payout-identity-match). F-H8 unchanged meanwhile.
- **Owner chose Option C: platform-collected Stripe requirements.** AZOA collects
  requirements, so the verified flag + date of birth are readable; payouts are
  released only when Stripe's verified (name + DOB) matches a new keyed blind index
  derived at KYC approval. The owner accepts the platform's Stripe compliance
  liability AND the residual risk that a tenant holding a person's real identity
  could enter its own bank account under it (shown that only option B — the link
  goes only to the person — prevents that). Compensating control: the bank
  holder name must match the verified name.

## Loop iteration 4 — fix rounds running (2026-10-27)

| lane | where | scope |
|---|---|---|
| F1 money fix round | ardanova worktree `../ardanova-money-fix`, branch wip/money-idempotency (@d4cd537 = the reviewed diffs) | all 12 review findings; P3-17 code re-threading through AzoaNodeClient/Result<T>; FundingIntentsController emits codes; replay lookup before readiness |
| F2 SDK fix round | oasis-sleek-sdk-money-react, feat/sdk-money-react | H1/H2, M1-M3, L1-L8, emulator rebuilt from the node's real behaviour, missing component tests |
| F3 node replay codes | oasis-sleek-fiat-codes, feat/fiat-replay-codes | the four 409 codes; a failed Stripe re-read FAILS instead of reporting a false `not_started` |
| F4 Phase 0 fixes | ardanova worktree agent-a4577bc643a9e0eeb | race-correct recount + LOCK TABLE; CHECK on the half-filled source pair; lock_timeout; system post-type allowlist (PostService.CreateAsync); runbook = safe deploy order incl. restart-after-migrate |
| PN-C payout setup | oasis-sleek-payout-identity, feat/payout-identity-match | Option C as above; fail-closed release check at RequireReadyAccountAsync (runs at request AND right before send) |

Gates: SDK publish waits on F2 + a re-review. The released-DB feed migration
waits on F4 (runs migrate-first, then pushes the code so the new BFF boots fresh).

## Loop iteration 4 — results so far (2026-10-27)

- **F4 DONE** (176c09e): race-correct likesCount recount in a SEPARATE statement
  scoped by a temp table captured BEFORE the delete (a sibling CTE would read the
  pre-delete snapshot and repeat the bug), LOCK TABLE ahead of the delete, a CHECK
  on the half-filled source pair, lock_timeout, the PostService.CreateAsync
  user-authorable allowlist {POST, PROJECT_UPDATE, GUILD_ACTIVITY, PROPOSAL,
  SHOP_ITEM}. Scratch proof on the race-shaped seed: p1 2 rows / count 2,
  p2 1 row / count 1.
- **SR-03 Phase 0 LIVE on the released env** (merge 69acd14), in the safe order:
  preflight (20 migrations, 0 failed; 0 posts/likes/bookmarks; 0 idle-in-tx; no
  stale feed types) → pg_dump (344,105 bytes, SHA256 3373CD0A…936FEC) → `migrate
  deploy` with the OLD BFF serving → readback (24/24; TaxonomyTerm SECTOR 26 /
  SKILL 54 with parents / PROJECT_TYPE 13; CHECK + unique index; 10 legacy
  sectors resolvable; `migrate diff --from-url --exit-code` = "No difference
  detected") → push → new BFF deployed SUCCESS as a fresh process.
  **Live smoke (Playwright, real session): 10/10** — ProjectType enum 13 values;
  post create/read; like ×2 → likesCount 1; unlike; delete; a forged MILESTONE
  post → 400 "written by the system"; project list 200.
- **F3 DONE** (oasis-sleek feat/fiat-replay-codes @769494f1): the four contract
  codes on the same-key 409s (FiatRailProvider.cs:601/606/624/630/641;
  REQUEST_KEY_IN_USE also on cash-out, OffRampService.cs:389); a failed Stripe
  re-read now returns 503 DEPENDENCY_UNAVAILABLE instead of a 200 that says
  not_started. Every consumer already treats a 5xx as "try again". Unit suite
  5429/5429.
- **INCIDENT FOUND + FIXED: the client had not deployed for ~1.5 days.** Every
  Ardanova-Client build since 2026-10-26 06:52 FAILED (7 in a row) while Railway
  kept serving 3423b5d7; the BFF kept deploying fine, so nothing alerted.
  Cause: 5fc7d30 (a UX review-fix commit) rewrote package-lock.json on Windows,
  dropping the wasm32 optional bindings' nested @emnapi/core@1.11.1 +
  @emnapi/runtime@1.11.1 (31 → 26 entries) → builder `npm ci` EUSAGE. Fixed in
  f281231 with the recorded e54e9c9 recipe inside node:20-alpine (npm 11.21.1);
  `npm ci --dry-run` in the same image passes. So the pass-1 UX review fixes in
  5fc7d30 and everything after were NOT live until this deploy.
  | id | item | decision | size |
  |---|---|---|---|
  | P7-20 | Guard against lockfile platform drift | **CLOSED 2026-10-27**: `scripts/verify-lockfile-platform.mjs` resolves every `*-wasm32-wasi` binding dependency the way npm's node_modules lookup does and fails closed on missing, mismatched or unverifiable pins. Proven on the real broken lock (reproduces Railway's exact `@emnapi/core@1.11.1` vs hoisted `1.11.2`, exit 1) and on the fixed and last-good locks (exit 0). Runs in CI before `npm ci`; `npm run verify:lockfile-platform`; 6 tests; RUNBOOK §2a. **Client deploy confirmed: f2812310 SUCCESS** | S |
  | P5-10 | **F1 money fix round DONE** (wip/money-idempotency @cd2ae18, merges cleanly onto main): a saved key is released ONLY on server money code `definite` (persisted-request-key.ts:195); the code travels Result<T>.Code → BFF body `code` → TRPCError.cause → `error.data.moneyCode` (trpc.ts:73) and is never derived from the tRPC/HTTP status. The post-commit 400 carries no code, so the key survives and Retry replays it. Invest stores and resends the exact wire request; add-funds records the admitted paymentId before redirect; key storage now includes the user fingerprint, cleared on sign-out; Failure → 503 on the money controllers; node codes preserved through AzoaNodeClient.MapError (P3-17 closed). 105 vitest + 189 dotnet green | RE-REVIEW RUNNING before merge | M |
  | SR-05-F2 | **SDK fix round DONE** (feat/sdk-money-react @70c32b23): all 14 findings fixed; 514/514 tests; mutation-checked (31 single-fix inversions, every one red). H1 is now `typeof code === "string" && KEY_RELEASING_CODES.has(code)` (refusal.ts:50); H2 splits "key may be forgotten" (isTerminalCheckout) from "settled" (isSettledCheckout); Web Locks for cross-tab claims; all 13 views allowlist-narrowed; browser apiKey refused at construction. Breaking for hosts: AddFundsForm requires currencyCode; formatMinorUnits may return null | **PUBLISH-GATE RE-REVIEW RUNNING** (SDK + node codes together, contract seam first) | L |
  | SR-05-R2 | **Publish-gate re-review: BLOCK again — a cross-repo seam.** HIGH-1: the node sends 409 INTENT_EXPIRED as soon as a Stripe session is attached and its page has lapsed (FiatRailProvider.cs:619-624), WITHOUT re-reading Stripe or checking the intent. So an ACH payment still clearing, or a card payment Stripe has marked paid before the webhook landed, gets "expired" on Resume → the SDK (correctly per contract) releases the key and unlocks the form → second charge. The SAME mapping is in the ArdaNova money branch (MoneyRefusal.cs:122-125 → `definite`). Also: the emulator still models the pre-fix node; INTENT_NEEDS_REVIEW shows false copy; Stripe's `idempotency_key_in_use` (a same-key request still IN FLIGHT) maps to a final 400. **Rule adopted: a code may release a key only if its emitter can PROVE nothing was or can be paid under that key; once a request has a server id, decide by STATE, not by code.** Fix lanes running: node (narrow INTENT_EXPIRED to a fresh-read-proven unpaid session; a clearing/paid session → 200 projection with paymentUrl null; a failed read → 503; in-flight → non-final 409) and SDK round 2 (decide by state, lock until settled, emulator = new node, `needs_review` code, safe default redirect origins). The node change 769494f1 is SAFE TO DEPLOY ALONE (today's clients keep the key on every 409) but NOT together with a client that treats INTENT_EXPIRED as final | BLOCKED → fix lanes | M |
  | PN-C-REVIEW | **Option C security review: BLOCK — and option C is ABANDONED (owner, 2026-10-28).** Top finding: withdraw was unreachable for ArdaNova members under ANY payout option. Cash-out needs a live `transfer:sign` grant (FiatPayoutController.cs:254-257). The provisioned grant can never carry it (ConsentLifecycleIdentity.cs:21-22). A UserExplicit grant needs the person's own login (SurrealConsentLifecycleStore.cs:50-51), unclaimed users can't log in (UserManager.cs:111), and claiming revokes every grant. Plus a CRITICAL hole: the final bank payout (`/v1/payouts`) was never identity-gated nor pinned to the verified `ba_` id. And a HIGH: a tenant could re-point a matched destination knowing only the name. Branch feat/payout-identity-match @c64e79a6 stays UNMERGED (reference only). F-H8 stays | CLOSED (abandoned) | — |
  | P5-16 | **OWNER DECISION: claim-and-consent withdraw flow** — "sound; artfully done; for any email work stub a resend for now". When a member first cashes out, ArdaNova requests a node claim invite carrying a tenant-declared consent request (transfer:sign + the scopes ArdaNova needs to keep serving them, e.g. units:issue). Then on AZOA-hosted pages the person sets their own credential, approves exactly those scopes as THEIR OWN UserExplicit grant (strictly AFTER the claim's revocations commit), and sets up their payout destination on Stripe's hosted page AS THEMSELVES. Email delivery is a STUB (recorded message + link, with resend). PLANNER RUNNING (incl. the post-claim behaviour of every BFF→node call a member uses) | DO-NOW (plan first) | L |
  | P5-17 | Salvage from option C onto main: personal/bank-field redaction in the node's JsonlExceptionLogger (it writes SurrealForge query params; KYC binding names could reach disk) | IN the node void-or-reveal lane | S |
  | SR-05-R3 | **Round 2 results.** Node `cee103bd`: a same-key resend returns INTENT_EXPIRED only when `ProvesNothingPaid` (our expiry AND Stripe's agree, intent AwaitingSubject, not paid). A clearing or paid session → 200 projection with paymentUrl null; a failed read → 503; Stripe in-flight/409 → non-final 409; 5442/5442. SDK `21813447`: decides by STATE once a request has a serverId; the form stays locked until a settled view; `needs_review` code; safe default redirect origins (checkout.stripe.com, connect.stripe.com, widget.identomat.com); 540 tests, 61/62 mutations red. ArdaNova money re-review: **CHANGES_REQUESTED** — C-1: the node still sends INTENT_FINISHED for a PAID session and the BFF maps it to `definite`; H-1: invest refusals BEFORE admission keep the key, and the only exit ("Resume it") later debits the OLD amount; M-1: "start a new payment" sets aside a still-payable Stripe page; M-3: 503 now reads as "cash-out unavailable"; M-4: false "nothing can charge you" fallback copy; M-5: HoldAsync can cancel an outbox the worker is still sending; M-6: sign-out clears unresolved keys | → round 3 | M |
  | P5-18 | **The missing primitive: VOID-OR-REVEAL by request key.** A client can't safely discard a saved key unless the server can say "nothing was admitted under this key and nothing ever will be". `POST …/checkout/{userId}/requests/{requestKey}/void` under the SAME admission fence as begin. Nothing admitted → tombstone (a later begin → 409 `REQUEST_VOIDED`). Admitted but unpaid → expire the Stripe session and mark Abandoned. Paid/clearing → REVEAL the state instead. Unreadable → 503. ArdaNova invest gets the same semantics on FundingIntentService, with a conditional cancel only on a non-leased outbox (fixes M-5). Closes C-1, H-1, M-1 and the L-1 race | NODE LANE RUNNING; ArdaNova + SDK round 3 start on its implemented wire contract | M |
  | SR-03-P1 | **Phase 1 lanes DONE.** A projector @8e13cae (real-PG flow tests; session-level advisory lock on a held connection; idempotent insert: unique violation = already projected; envelopes excluded from person reads; envelope edit/delete refused; P8-18 like/bookmark race fixed). C taxonomy @5039dc0 (GET /api/taxonomy; whole-value category filter, SQL `',' \|\| categories \|\| ',' LIKE '%,X,%'`). D2 client plumbing @091a6455. Automated security finding on A: the hourly sweep only reconciled envelopes created in the last 48h, so cards for sources deleted later would linger → full-reconciliation-in-batches fix RUNNING. Operator after deploy: set `Feed__Projector__Enabled=true`, review a backfill dry run, then backfill | REVIEW + SWEEP FIX RUNNING → merge | L |
  | SR-03-P1-R | **Phase 1 review: CHANGES_REQUESTED (1 HIGH, 6 MED, 7 LOW).** H1: the projector defaulted ON in Development, and docker-compose.dev.yml runs Development against the root .env's REMOTE Railway DATABASE_URL → any laptop becomes the shared DB's feed writer (its first cycle sweeps = hard deletes). Fix: default OFF everywhere; explicit ON only in .omc/scripts/local-stack.ps1 (local DB). Held up under attack: lock (single physical connection, unlock on any exception, a dropped connection frees it), privacy (every card at most as visible as its source), duplicate-key handling, admin auth. Full-reconciliation sweep fix @abc4157 (every envelope, batches of 500; RED proven on the old 48h filter). Fix round RUNNING on A (H1, M1 per-item isolation + high-water mark, M3, M4 real-PG tests run in CI, M5, M6, L3-L7) and C (L1 canonical category codes + refuse commas, L2) | → merge after fixes | M |
  | SR-03-D1 | Stated defaults (owner may reverse): **M3** TASK_COMPLETED/MILESTONE cards credit the person who did the work (`assignedToId ?? project creator`), since the assignee is already public and crediting the founder for a contributor's work cuts against "fair share". **M5** a malformed feed setting logs Critical and leaves the projector OFF rather than failing API startup (a feed typo must not take down payments and sign-in) | ASSUMED | S |
  | SR-03-P2-PRE | **Phase 2 precondition (M2):** retraction (opportunity→DRAFT; event→DRAFT/CANCELLED/INVITE_ONLY) currently HARD-deletes the card along with others' replies/likes, and re-opening mints a fresh card that recency ranking bumps. Needs a retracted state (additive column) restoring the same row. No UI shows cards until Phase 2/3, so nothing live is at risk | DO-NOW before Phase 2 | M |
  | P5-19 | **SECURITY (found by the claim-consent planner, live on the node today):** `POST /api/user/claim-invite` returns the RAW claim token to the tenant (WalletAuthManager.cs:313-316), stored in PLAINTEXT (SurrealWalletAuthClaimTokenStore.cs:24-31). A tenant can claim a member's account with a password it chooses, log in as them, and grant itself transfer:sign. Fix N1: store hashed (`h1:`+SHA-256, constant-time compare; legacy plaintext refused); never return the token unless `Tenant:ClaimInvites:ReturnTokenToTenant=true` (IntegrationTest config only); lifetime configurable (72h default, 7d max) | LANE RUNNING (oasis-sleek fix/claim-invite-hardening) — ships FIRST and alone | S |
  | P5-20 | **Claim-and-consent plan (planner, 2026-10-28).** One AZOA-hosted flow, in oasis-sleek `frontend/` (not node-served: it already has SDK sessions, wallet signing, the payout UI, X-Frame-Options DENY). The person sets their credential; THEN, under that fresh login, approves the tenant's request as their own UserExplicit grant (the JWT only exists after the claim commits, and the grant transaction re-checks AuthNotBefore); THEN sets up their own Stripe payout; THEN clicks back to ArdaNova. Requested scopes: kyc:read, units:issue, **swap:sign** (selling/backing/rewards ride on implied swap:sign today, and that dies at the claim), transfer:sign, kyc:submit. Token travels in the URL FRAGMENT (never reaches servers/logs/Referer), single use, resend retires the old link. Address comes from ArdaNova (the node deliberately has no member email) = accepted residual risk. Email = `IOutboundEmailSender` shaped like a Resend.com send, with a STUB that records the message for the operator console + resend; Production refuses the stub without an explicit owner acknowledgement. CRITICAL ANALYSIS: every ArdaNova value action passes ONE readiness gate (custodial status), which requires the tenant link that claiming removes → claiming would silently break add-funds, sales, rewards and withdraw unless N2 lands first. **Release order:** N1 token hardening → N2 claimed-member continuity (claimed branch in TenantCustodialAuthorityFence + ensure + allocation correlation + a tenant "give up grants" DELETE) → A1 ArdaNova reads ownership/consent status → N3 consent requests + invites + email stub ∥ F hosted pages (behind `Tenant:ClaimInvites:Enabled`) → A2 ArdaNova ownership step on first withdraw (behind `Azoa:ClaimInvites:Enabled`) | N1 + N2 RUNNING; A1 waits for the money branch to merge (same files) | L |
  | P5-21 | Claim-consent risks to carry: (1) the delivery address is tenant-asserted, so a compromised tenant can route the link to its own inbox — later fix: an identity re-check at claim matched to KycPersonKey; (2) between claim and approve ArdaNova holds no authority — pause member-scoped outbox dispatch while consent is pending; (3) a member who DECLINES can't cash out on AZOA either (KYC is tenant-scoped) — copy must not promise it; (4) wallet drift after claim breaks ArdaNova's binding check (AzoaCustodialAccountService.cs:215); (5) 12-month grant expiry needs the consent-only renewal path | CARRY into N3/A2 | — |
  | SR-03-P1-LIVE | **SR-03 Phase 1 LIVE (main 28556c3, 2026-10-28).** Integrated in a throwaway worktree (A 99b6f29 + C 243e7ca + D2 091a6455; doc-only AGENTS.md conflict, both sections kept) and verified there BEFORE main moved: backend API 552/552 + Application 1432 passed / 0 failed with ARDANOVA_TEST_POSTGRES set (the real-PG feed tests EXECUTED); client npm ci + lockfile guard + tsc + vitest 692/692. Main fast-forwarded to exactly that tree; both services deployed SUCCESS. Live smoke: GET taxonomy → 93 terms (SECTOR 26 / SKILL 54, all with a parent sector, e.g. CARPENTRY→CONSTRUCTION / PROJECT_TYPE 13); unknown kind → 400. `Feed__Projector__Enabled=true` set on the released BFF: cycles running (advisory-lock query every ~5s), 0 errors. **Backfill deliberately deferred** to Phase 2 (the admin route needs an actor assertion; the released env holds only ~5 sources); new activity is captured from now on | DONE | L |
  | P7-22 | Config bug (node, LIVE): `Exchange:MaxDeclaredRateAge` and `Exchange:BuyBack:QueueEntryTtl` were `"24:00:00"`, which .NET parses as **24 DAYS** (verified: TotalHours 576), so a declared exchange rate up to 24 days old was accepted as fresh. Fixed to `"1.00:00:00"` (oasis-sleek fix/claim-invite-hardening @9ec5f237); a sweep found no other such values | SHIPS WITH N1 | S |
  | P7-23 | oasis-sleek git topology: the remote is `upstream`; local `main` is stale (085eaa12 vs upstream/main 6801cdc1) and held by the owner's worktree. Every node lane so far branched from the stale base — integrate each onto `upstream/main` in a throwaway worktree, re-test there, then `git push upstream HEAD:main`. Future lanes branch from `upstream/main` | PROCESS | — |
  | P5-19-R | **N1 security review: SAFE TO DEPLOY** (could not refute: 256-bit tokens from RandomNumberGenerator; strict 67-char `h1:` digest; the presented value is always hashed before lookup, so a stolen stored digest can't be redeemed; single use + target binding re-checked inside the claim transaction; no raw token in Surreal statements, logs or telemetry). Condition applied: a boot guard refusing `ReturnTokenToTenant` outside Development/IntegrationTest, because one env var (or ASPNETCORE_ENVIRONMENT=IntegrationTest — that config ships in the image) would silently re-open the hole. Plus a unit test proving the default withholds the token. N1 integrating onto upstream/main | → deploy | S |
  | P5-22 | OWNER audit (the hole was live on the released node until N1 deploys): list consumed `wallet_auth_claim_token` rows grouped by `tenant_id`, and cross-check any `transfer:sign` grant to that tenant soon after a claim. ArdaNova never calls claim-invite (zero hits), and tenant keys need operator issuance, so exposure is likely nil — but only you can confirm which tenants you issued keys to | OWNER | S |
  | P5-23 | Follow-ups for N3: the SDK's `createWalletOwnershipInvite` still types `claimToken: string` (now absent outside test hosts) → `claimToken?: string`; revoke a target's earlier live invites on each new mint; rate-limit minting; delivery must NEVER go to an address the tenant chose without the pinning/audit in the plan | CARRY into N3 | S |
  | P5-24 | **Node fiat branch review: BLOCK on one proof gap.** The void design held under every interleaving (the tombstone claim + version-checked switch vs the fence and the attach; expire-at-Stripe before abandon; ACH `complete`+unpaid always revealed, never voided). BUT `ProvesNothingPaid` (FiatRailProvider.cs:970-971) answered INTENT_EXPIRED while Stripe still read `open`: the row's ExpiresAt IS Stripe's expires_at, so its "Stripe's own clock" clause repeated OUR clock (deleting it left 407 tests green). A payer mid-3DS at expiry → key released → Stripe completes seconds later. Fix: resume runs the SAME classifier as void (expire at Stripe first, then judge; uncertain → 503). Plus: intentId + projection in final 409 bodies; the webhook inbox re-reads before calling a lost CAS "applied"; one fewer Stripe read per checkout; a Critical log when a voided intent gets paid; `email` redaction; the real-SurrealDB race tests made runnable against an ISOLATED container (the endpoint was hard-coded to the demo DB on 8020) | FIX ROUND RUNNING | M |
  | P5-19-LIVE | **N1 DEPLOYED to the released node** (2026-10-28): boot guard added (73ce884f) + default-withholds unit test (7275ddd6); integrated onto the REAL upstream/main in a throwaway worktree (merge a42977b4; one doc-only conflict, both sections kept); full unit project on the integrated tree 5547/5547; `git push upstream HEAD:main`; azoa-api deployment SUCCESS, /health 200. The claim-token takeover hole is closed in production, and the 24-day exchange-rate config is fixed | DONE (CI run watching) | S |
  | SR-05-R3 | **Round 3 results.** SDK @3ee3fc0a: `checkout.void` on the transport (REQUIRED — host transports must forward it); only a `voided` answer clears a key (flows.ts:163); a FAILED void proves nothing whatever its code (flows.ts:169); a lost first answer meeting a key-releasing refusal goes to the void first; 572 tests, 20/20 new mutants red. Node @33168340: ONE classifier (`SettleCheckoutSessionAsync`) for void and resend — an open page past expiry is expired AT STRIPE before any final answer, `ProvesNothingPaid` deleted; final 409s carry the GET projection; webhook inbox re-reads a lost CAS; real-SurrealDB races verified on an ISOLATED container (8-way parallel: conflicts surface, never more than one winner, demo DB untouched). SDK emulator sync to 33168340 RUNNING → then the combined publish/deploy-gate review | → final seam review | M |
  | P5-25 | **ArdaNova money round 3 DONE** (wip/money-idempotency @696a337): the browser releases a key ONLY on a void route's 200 `voided` (persisted-request-key.ts:243). BFF: REQUEST_VOIDED/INTENT_EXPIRED → `definite`; INTENT_FINISHED/REQUEST_KEY_IN_USE → a new non-releasing `finished`; a failed void can never look final. New BFF void routes for add-funds (proxying the node) and invest (`FundingIntentService.VoidRequestAsync`: tombstone table FundingIntentVoid via migration 20261028000000; settlements created AUTHORIZED and released to PENDING_DISPATCH by CAS only by the live request, so void and dispatch race on one row; HoldAsync cancels only an UNLEASED outbox). Real-Postgres race tests for every interleaving. INTEGRATION onto main RUNNING (DBML union + regenerate) → then the combined money release-gate review (node + SDK + ArdaNova) | → integrate + review | L |
  | P5-26 | Owner-visible behaviour changes from round 3: (1) a create that crashes between commit and release leaves a DORMANT sponsorship that nothing sends until the sponsor retries or cancels (no money moves) — needs a cleanup sweeper; (2) add-funds Continue stays LOCKED while an earlier payment is still landing (e.g. ACH clearing) — safe, but the person can't add more meanwhile; (3) server-side binding of the display-name choice in ReplayAsync is still client-only | (1) DO-NOW sweeper; (2)(3) ACCEPTED | S |
  | P5-27 | **Money release gate staged (2026-10-28).** All three sides settled: node feat/fiat-replay-codes @33168340; SDK feat/sdk-money-react @4ea72656 (emulator synced to the node; final-409 projection decided by STATE via the existing settlement gate, flows.ts:151; 577 tests); ArdaNova wip/money-idempotency @e3daaca (main merged in @2afced6c — the 25-migration chain proven on a scratch DB (deploy ×2 + migrate diff clean); vitest 741, API 604/605, Application 1435; real-PG race tests RUN; the 6 invest race tests now report SKIPPED, not a fake PASS, without a DB, via a linked PostgresFact + a per-assembly policy twin). Two parallel adversarial reviews RUNNING: node+SDK seam (deploy + publish verdicts) and the ArdaNova branch (merge + migrate + deploy verdict) | GATE RUNNING | — |
  | P5-28 | **Release gate, node + SDK: Node SAFE TO DEPLOY; azoa-sdk 0.6.0 SAFE TO PUBLISH.** 30-row seam table (every begin/resend/void/GET answer → SDK handling); 6 guard mutations red; real-SurrealDB void races 4/4 on an isolated container. Pre-publish fixes running (a published version can't be changed). **M1:** the node's internal admissionKey (user + tenant ids) reached the UI via un-narrowed `refusal.serverResult` (refusal.ts:76). **Two LIVENESS traps**, each a pair of individually conservative choices that together leave no exit: **M2** an unattached claim >23 h old → begin AND void say needs-review while the SDK disables discard, with no operator endpoint (node fix: void abandons ANY unattached claimed row); **M3** a paid intent whose funds never land keeps its key forever (SDK: let a terminal paid intent be acknowledged and released, with honest copy). Plus emulator drift (no `starting` projections — how M2 went unnoticed) and LOW-3 (the projection reports `expired` when a fresh Stripe read says complete) | FIX ROUND RUNNING → verify → integrate + deploy + owner publish | M |
  | P5-29 | **Release gate, ArdaNova: BLOCK** (add-funds seam + migration CLEAN; two invest debit-after-"nothing taken" paths, both reproduced on real Postgres). CRITICAL: request-path sends take no lease, so a resend's readiness blip let TryHoldAsync cancel a settlement an EARLIER, aborted request had already sent (TryCancelUnleasedAsync only rules out a WORKER lease), and the void read "CANCELLED + opId null" as proof → "Nothing was taken" after a real debit. HIGH: a cancel is TWO commits (an abort between them → DRAFT+CANCELLED → a void trap, and a same-key replay still dispatched because CompleteAsync's full-row update overwrote CANCELLED). Fix round 4 RUNNING: reconcile-before-cancel via the node receipt by idempotency key; no unleased resend of a released settlement; cancel in ONE transaction; no update overwrites a terminal state; add-funds `needs_review` code carrying the intentId; invest "closed" copy keyed on an explicit `released` flag. Latent today (sponsors stop at the KYB gate) | FIX ROUND 4 RUNNING | M |
  | P5-30 | Pre-existing (outside this diff), for the withdraw/A2 work: the withdraw page keeps its request key in React state only (app/portfolio/withdraw/page.tsx:118, rotated at :154), so a reload after a lost answer mints a NEW key → possible double cash-out once withdraw is reachable; payouts.ts:193/:380 fall back to "nothing was moved/sent" on a 2xx with no body. Withdraw is unreachable until claim-and-consent ships | DO-NOW with A2 (before withdraw is reachable) | S |
  | P5-31 | **Final node + SDK fixes landed.** Node @01c99aae: the void abandons ANY unattached CLAIMED row (safe at any age because the attach requires CLAIMED and the abandon removes it); a fresh Stripe "complete" projects `processing` except for a recorded Complete or a failed bank debit; 5496/5496. SDK @0bfca68a: the admissionKey leak closed at the single entry point (allowlist narrowing; only 3 codes may carry a result); a never-started claim releases its key; Discard enabled on needs_review; a terminal paid intent can be acknowledged; 598 tests, 7 mutations red. N1 CI fully GREEN on upstream/main @5287292f (9/9 jobs). Narrow verification review RUNNING → then integrate node + SDK onto upstream/main, deploy, and the owner publishes | FINAL VERIFY RUNNING | S |
  | P5-32 | **OWNER policy question:** the node's settlement handler CREDITS money that lands on a voided/abandoned checkout intent (it credits from the Stripe payment, not the intent state). That is correct for the money (the person gets what they paid for), but someone who pays a voided page AND a new one is credited twice and has paid twice. Options: keep crediting (they can ask for a refund) vs auto-refund payments that land on a voided intent. Runbook now tells operators to check rather than assume (RUNBOOK.md:251) | OWNER | S |
  | P7-18-UP | **Upgraded: `generate:csharp` is a CORRECTNESS risk, not cosmetic.** A raw regen would change a runtime default (`Project.fundingGoalUnit = ProjectFundingUnits.Ledger` → `string.Empty`) and DELETE hand-written members (`PayoutRequest.ExchangeIdempotencyKey`, `TokenBalance.AvailableBalance`, `ProjectTokenConfig.IsFundable`), breaking the build. Every integration so far has had to restore 14-15 files by hand. Fix: the generator emits `partial` classes, with hand-written members in sibling `*.Custom.cs` files | DO-NOW (next DBML batch) | M |
  | P5-12 | The AZOA first-party frontend (oasis-sleek frontend/src/lib/persisted-request-key.ts, funds/page.tsx) still uses the original isFinishedCheckout and likely has the same H2 bug | DO-NOW via SR-05 P2 (dogfood the fixed SDK) | S |
  | PN-C | **Option C payout setup DONE** (oasis-sleek feat/payout-identity-match @c64e79a6): controller properties (requirement_collection=application, no dashboard; `type` is deprecated); PII forwarded once, never stored; name+DOB keyed blind index written at KYC approval; a basis digest over every fact read; the release check can only refuse/downgrade, never write `matched`. 5491/5491 | **SECURITY REVIEW RUNNING** (top question: can an ArdaNova unclaimed custodial member ever hold the live transfer:sign grant setup requires?) | L |
  | P5-13 | OWNER (Stripe dashboard): allow PLATFORM-COLLECTED connected accounts on the platform profile (Option C requires it) | OWNER | S |
  | P5-14 | Connected-account webhooks (`account.updated`, `account.external_account.*`) need a Stripe **Connect** endpoint (its own signing secret), separate from the platform endpoint created 2026-10-26 | DO-NOW after PN-C merges (operator: create via Stripe API + push the secret) | S |
  | P5-15 | Option C regression: the node's own dashboard cash-out (frontend/src/app/(dashboard)/cash-out/page.tsx) now gets 400 — setup requires a details body — until the SDK's PayoutDestinationStep collects the fields and the agreement acceptance | DO-NOW (SDK follow-up + P2 dogfood) | M |
  | P5-11 | Judgment calls carried by F1 (owner-visible): invest "Resume it" replays the saved request (no browser cancel-by-key — a true cancel needs a server-side marker row); add-funds Discard clears an open UNPAID payment; setup_required/under_review keep the key (allowlist is `definite` only), so a readiness flip mid-flow shows a resume banner | DEFER(cancel-by-key) until QA shows sponsors are surprised by Resume | S |
  | P7-21 | Deploy-failure visibility: client builds failed 7× silently. Railway deploy-failure alerts (webhook/notification) or a post-push check that /api/health serves the pushed commit | DO-NOW (with the owner's "Wait for CI" flip) | S |
