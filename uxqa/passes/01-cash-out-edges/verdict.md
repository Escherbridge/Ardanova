---
type: Pass Verdict
pass: 01-cash-out-edges
scenario: cash-out-edges
status: CHANGES_REQUESTED
---

# Pass 01 verdict: cash-out and the money's edges (post-fix re-run)

This verdict covers the real, session-seeded run recorded in `ux-report.json`
(generated 2026-10-25T22:38Z — 44 runs: 36 UX + 8 probe) and the synthesis
built from it in `synthesis.md` (generated 2026-10-25, title "re-run with
sessions"). It supersedes the prior `verdict.md` content, which judged an
earlier **void** run (all 11 UI runs died at the sign-in wall because of the
`personas.json` seed-shape bug fixed in commit `927d5c6`'s line of work). Four
fix commits (`c347787`, `29d65bb`, `1a5fe14`, `5fc7d30`) landed *after* this
synthesis was generated, so this verdict's job is to lay out what exists and
what still needs a coordinator re-run — not to declare the pass done.

## 1. Exit checks

Source: `uxqa/passes/01-cash-out-edges/ux-report.json` → `exitChecks`, cross-read
against `synthesis.md` §3, §6, §7. As before, **no `ledger.json` exists**
anywhere under `uxqa/` — this is the same gap the prior verdict flagged, still
unresolved. The "new HIGH" row below substitutes `synthesis.md` §6's own
`Ledger` column (HIGH/MEDIUM/LOW/RESEARCH_QUESTION), the closest equivalent on
disk.

| Check | Target | Observed | Result |
|---|---|---|---|
| Gold success rate | ≥ 0.8 | 1 of 4 gold tasks met target: `gold-see-balance` (1.00, median 4s/0 clicks/friction 6). Below target: `gold-add-funds` (0.00 — Continue never enabled, median friction 20), `gold-withdraw` (0.00 — amount field `<input disabled>`, 3× 30s timeouts, median friction 30), `gold-sell-units` (0.00 — "What you hold" read failed 3× in 3s, median friction 25) | `ok: false` |
| Friction per journey vs previous pass | within 10% tolerance | previous (`00-setup`) = 1, current (`01-cash-out-edges`) = 28.11 | `ok: false`. Unlike the earlier void run (where a "friction=15" reading was an artefact of 11 identical sign-in-wall bounces), this is a real measurement: totals are 1012 friction points, 82 failed attempts, 12 workarounds, 283 console errors, 30 axe violations, 0 rage/dead clicks across 36 UI runs. `00-setup` is a setup pass with near-zero activity by construction, so the comparison is still not apples-to-apples, but this time the numerator is genuine product friction, not harness noise |
| New HIGH-severity findings | 0 | `ux-report.json`'s own `exitChecks.newHigh` is uncomputed (`count: null`, `ok: null`) — there is no prior-pass S-ID baseline to diff against either, since `00-setup` predates this taxonomy. `synthesis.md` §6 currently carries **7 HIGH-band findings**: S-01, S-02, S-03, S-04, S-05, S-06, S-13. Of those, the 4 fix commits below plausibly touch S-01, S-02, S-03 (partially — see §3), S-06 and S-13; **S-04** (project-service 500 for every role, flagged `HIGH (BLOCKER: no workaround)`) and **S-05** (signed-out home has no money entry point, `HIGH`) are untouched by any commit in this window | not harness-computed; 2 of 7 HIGH-band findings (S-04, S-05) have no code change addressing them yet, and the other 5 are unverified claims pending re-run |
| Route coverage | not set | 14 of 53 page routes visited (ratio 0.2641509433962264); 2 of 12 declared REST API routes hit (`GET /api/auth/session`, `GET /api/health`) — the bulk of real traffic was tRPC procedures (`tokenBalances.getArdaBalance` ×46, `payouts.getPayoutsByUser` ×44, `project.getAll` ×38, etc.), which aren't in the declared-API list at all | ratio reported, `ok: null` (no target configured) |
| Review verdict | — | **APPROVE** | APPROVE |
| Coordinator re-run | — | 2026-10-26: full `npx vitest run` exit 0, `npx tsc --noEmit` exit 0, `npm run build` exit 0 on the tree containing all four fix commits — the integrator's 659/659 figures reproduced at exit-code level. | **GREEN** |

**Coordinator ruling:** CHANGES_REQUESTED — engineering claims verified, but
the five targeted HIGHs await UX-harness re-observation, S-04 is root-caused
to the un-run owner-gated commerce migration (Postgres 42703: column
f.awardAmountAtoms does not exist on the released DB; ledger P7-15), and S-05
needs a product decision (P8-13). Next: re-run this scenario against the fixed
build; the creator/invest lane stays blocked until the owner runs the
migration runbook.

## 2. Claims to re-run

Every test/typecheck/build command the fix lanes and the integrator reported,
verbatim, grouped by the commit each lane's diff matches in `git log` (matched
by file names mentioned in the command output, e.g. `query-error-copy.test.ts`
→ `c347787`, `on-ramp-status.test.ts` → `29d65bb`, `money-route-titles.test.ts`
→ `1a5fe14`, `account-setup-copy.test.ts` + `app-topbar.test.tsx` →
`5fc7d30`). The coordinator should run each block and confirm the same output.

### Fix lane — plain-language errors and honest cash-out copy (commit `c347787`)

Touches `query-error-copy.ts`/`.test.ts`, `design-gate.test.ts`,
`withdraw/page.tsx`, `swap/page.tsx`, `portfolio/page.tsx`, `projects/page.tsx`
— the S-01/S-02/S-06/S-13/S-19 cluster.

```
cd ardanova-client && npx tsc --noEmit 2>&1 | grep -E "<owned paths>"  -> no matches; total "error TS" count across the repo = 0
cd ardanova-client && npx vitest run src/components/ui/query-error-copy.test.ts src/styles/design-gate.test.ts -> "Test Files  2 passed (2)" / "Tests  50 passed (50)"
cd ardanova-client && npx eslint <6 owned ts/tsx files> -> no output, exit 0
cd ardanova-client && npx prettier --write <6 owned ts/tsx files> -> formatted. Prettier changed the files after the first run, so I ran vitest, tsc (filtered count 0) and eslint (exit 0) once more. Same results: "Tests  50 passed (50)".
```

### Fix lane — add-funds readiness states and amount-format errors (commit `29d65bb`)

Touches `on-ramp-status.ts`/`.test.ts`, `portfolio/add-funds/page.tsx` — the
S-01 (add-funds half) / S-16 cluster.

```
cd ardanova-client && npx vitest run src/lib/commerce/on-ramp-status.test.ts -> first run: "Tests 1 failed | 30 passed (31)". The failure was my own test fixture: the ready account had no avatarId/walletId/walletAddress, and evaluateValueActionAccount requires all three. After fixing the fixture: "Test Files 1 passed (1) / Tests 31 passed (31)".
cd ardanova-client && npx tsc --noEmit 2>&1 | grep -E "add-funds/page|on-ramp-status" -> no output, grep exit=1 (no type errors in the owned paths).
cd ardanova-client && npx eslint src/app/portfolio/add-funds/page.tsx src/lib/commerce/on-ramp-status.ts src/lib/commerce/on-ramp-status.test.ts -> no output, eslint-exit=0.
```

Note: the fix lane's own test fixture had a bug (missing `avatarId`/`walletId`/
`walletAddress` on a "ready" account) that it caught and corrected before
reporting green — worth the coordinator spot-checking that the corrected
fixture is what actually landed, not the broken one.

### Fix lane — payout setup gating, drawer focus, verification copy, route titles, backend error mapping (commit `1a5fe14`)

Touches `custodial-account-card.tsx`, `account-setup-copy.ts`,
`app-topbar.tsx`, `settings/verification/page.tsx`, `payouts.ts` /
`token-balances.ts` / `swap.ts` routers, 7 `layout.tsx` metadata-title files —
the S-07/S-08 cluster plus the data-quality flag about mis-mapped backend
status codes.

```
cd ardanova-client && npx vitest run src/server/api/routers/money-read-errors.test.ts src/components/azoa/account-setup-copy.test.ts src/app/money-route-titles.test.ts src/components/ui/query-error-copy.test.ts
  -> Test Files  4 passed (4) / Tests  70 passed (70)   (also re-run after prettier: same result)
cd ardanova-client && npx vitest run
  -> Test Files  65 passed (65) / Tests  658 passed (658)
cd ardanova-client && npx tsc --noEmit > scratchpad/tsc.log
  -> exit 0; 'error TS' count 0 (nothing to filter for owned paths)
cd ardanova-client && npx eslint <17 touched files>
  -> ✖ 1 problem (0 errors, 1 warning): pre-existing react-hooks/exhaustive-deps at settings/verification/page.tsx:102; eslint exit 0
cd ardanova-client && npx prettier --write <17 touched files>
  -> reformatted 5 files (whitespace only, including two existing long lines in custodial-account-card.tsx)
```

Note: this lane's own full-suite run already reports 65/658 — the same
numbers the integrator reports below, which is a useful cross-check that no
lane's local sweep disagreed with the integrated one.

### Integrator (full repo, after all three fix lanes merged)

```
cd ardanova-client && npx vitest run -> "Test Files  65 passed (65)" / "Tests  658 passed (658)" (5.87s)
cd ardanova-client && npx tsc --noEmit -> no output, exit clean (0 "error TS" across the whole client)
cd ardanova-client && npm run build -> "✓ Compiled successfully in 19.8s" / "✓ Generating static pages (48/48)" -- full production build succeeded, including all touched routes (/portfolio, /portfolio/add-funds, /portfolio/withdraw, /swap, /projects, /settings/payouts, /settings/verification)
```

### Fix-review lane (commit `5fc7d30`, review findings)

Touches `dashboard/profile/page.tsx`, `app-topbar.test.tsx`,
`account-setup-copy.test.ts`, `custodial-account-card.test.tsx`,
`vitest.config.ts` — extends the S-08 fix to `/dashboard/profile` and replaces
source-grep assertions with real jsdom/RTL behaviour tests.

```
cd ardanova-client && npx vitest run src/components/azoa/account-setup-copy.test.ts src/components/azoa/custodial-account-card.test.tsx src/components/app-topbar.test.tsx
  -> Test Files  3 passed (3) / Tests  15 passed (15)
Mutation checks (source restored afterwards; git diff on both files is empty):
  in app-topbar.tsx, replaced `event.preventDefault();` with `return;` -> npx vitest run src/components/app-topbar.test.tsx -> Tests  1 failed (1)
  in custodial-account-card.tsx, changed `offerSetup = needsProvisioning && notice.setupOpen` to `offerSetup = needsProvisioning` -> npx vitest run src/components/azoa/custodial-account-card.test.tsx -> Tests  1 failed | 3 passed (4)
Full sweep: npx vitest run -> Test Files  67 passed (67) / Tests  659 passed (659)
npx tsc --noEmit -p . -> no output (clean)
```

Note: the mutation checks (deliberately breaking `offerSetup`'s gating
condition and the drawer's `preventDefault`) are a genuine falsifiability test
on the two highest-stakes lines in this review pass — if the coordinator only
has budget to spot-check one thing, re-running those two mutations and
confirming they still go red is the highest-signal single check available.

**What the coordinator still needs to establish beyond these commands:** all
of the above is engineering-suite evidence (vitest/tsc/eslint/build). None of
it is a UX-harness re-run against the fixed code — the S-01/S-02/S-06/S-07/
S-08/S-13 findings these commits target have not been re-observed by an actual
`uxSession()` run yet. That re-run, not just the test sweep, is what the
`PENDING_COORDINATOR_RERUN` status is waiting on.

## 3. Ledger summary

Reconstructed from `synthesis.md` §6, since `ledger.json` does not exist (see
§1). 25 synthesis findings this pass (`S-01`–`S-25`), merged from 58 raw
per-lens findings.

**By severity band:**

| Band | Count | Findings |
|---|---|---|
| critical | 7 | S-01, S-02, S-03, S-04, S-05, S-06, S-13 |
| serious | 9 | S-07, S-08, S-09, S-11, S-18, S-19, S-20, S-21, S-22 |
| minor (2 held at MEDIUM under the accessibility exception) | 9 | S-10, S-12, S-14, S-15, S-16, S-17, S-23, S-24, S-25 |

**By `Ledger` column (synthesis.md's severity-tier substitute for a decision):**

| Tier | Count | Findings |
|---|---|---|
| HIGH | 7 | S-01, S-02, S-03, S-04, S-05, S-06, S-13 |
| MEDIUM | 10 | S-07, S-08, S-09, S-11, S-12, S-14, S-19, S-20, S-21, S-22 |
| LOW | 6 | S-10, S-15, S-16, S-17, S-23, S-24 |
| RESEARCH_QUESTION | 2 | S-18 (withdraw double-submit, untested), S-25 (busy announcements on slow network, untested) |

Per `synthesis.md`'s own triage rule, a finding becomes `FIX` only once
confirmed in code **and** backed by `ux.json` evidence — as written, that rule
means none of the 25 are formally promoted to `FIX` yet, because the synthesis
itself predates the four commits below. Those commits are code evidence; the
`ux.json` evidence (a UX-harness re-run) is what's still missing, which is
exactly why this verdict is `PENDING_COORDINATOR_RERUN` rather than claiming
anything is fixed outright.

**FIX items with commits (claimed by the commit message; not yet re-verified by a UX re-run):**

| Finding(s) targeted | What changed | Commit |
|---|---|---|
| S-01 (fake $0 / disabled CTAs read as "loading"), S-02 (/swap's "whenever you like" contradicted withdraw/add-funds), S-06 (ARDA/units undefined), S-13 (raw generic error on /swap), S-19 (timing-dependent fake $0 on /projects) | New `query-error-copy.ts`: `describeListLoadFailure()`, `readFigure()` (shows "-" while loading, "Not available" on error, never a fake $0), `CASH_OUT_NOT_AVAILABLE`/`readCashOutState()`/`describeCashOutStep()`/`describeWithdrawBlocker()` as single source of truth for whether a bank send is actually open; `/portfolio` now defines ARDA units and renames "Stake" → "Units"; `/swap` step-two text reads the real gate state instead of an unconditional promise | `c347787` |
| S-01 (add-funds Continue never enabling), S-16 (locale amount-format rejections — message only, not actual fr-FR parsing support) | `getAddFundsReadiness()` gets a distinct not-open state (`PRECONDITION_FAILED` → "Adding money isn't open yet", no retry) separate from a transient failure (offers retry); `describeAddFundsAmountProblem()` gives a specific "Use a dot for cents" message for comma/space input instead of a generic hint | `29d65bb` |
| S-08 ("Start account setup" enabled while its sibling is disabled; fires a mutation with no visible feedback), S-07 (operator language on Verification/Payouts), data-quality flag (401/403/404/5xx mis-mapped as NOT_FOUND/BAD_REQUEST, which is the likely cause of `httpErrors=0` despite 283 console errors) | `custodial-account-card.tsx` only renders "Start account setup" when `needsProvisioning && notice.setupOpen`; a failed `ensure` gets its own `role="alert"` line instead of overwriting the status line; `settings/verification/page.tsx` routes all AZOA/operator copy through `describeAccountSetupNotice`/`describeIdentityVerificationProblem`; `payouts.ts`/`token-balances.ts`/`swap.ts` routers now map 5xx/unknown/empty-2xx to `INTERNAL_SERVER_ERROR` instead of `NOT_FOUND`/`BAD_REQUEST`; 7 routes get per-route `metadata.title` | `1a5fe14` |
| S-08 (extended to `/dashboard/profile`, which had the same gating bug), plus hardened regression coverage for the above | `dashboard/profile/page.tsx` now passes `statusErrorCode` into `CustodialAccountCard`; source-grep assertions replaced with jsdom/RTL behaviour tests for both the drawer-focus and setup-gating fixes | `5fc7d30` |

**Not touched by any commit in this window — still open:**

- **S-04** (project service returns 500 for every role; HIGH, flagged `BLOCKER: no workaround`) — this is a backend/service-layer defect, outside the frontend fix lanes' reach; it still blocks the creator-to-sponsor funding hand-off and `/projects/[slug]/invest` entirely.
- **S-05** (signed-out home has no money framing or entry point; HIGH, the single highest-friction route at 339) — no commit touches `/` or its CTAs.
- S-09, S-11, S-18, S-20, S-21, S-22 (serious band) and S-10, S-12, S-14, S-15, S-17, S-23, S-24, S-25 (minor/research band) — none mentioned in any of the four commits' diffs.

## 4. Persona refinement

Coverage this pass: 14 of 53 routes visited (26%), concentrated on `/`,
`/portfolio`, `/portfolio/withdraw`, `/portfolio/add-funds`, `/swap`,
`/projects`, `/projects/[slug]`, `/projects/create`, `/auth/signin`,
`/dashboard`, `/settings/verification`, `/settings/payouts`,
`/settings/profile`. 39 routes remain unvisited, and the ones most relevant to
this scenario's own name are `/projects/[slug]/invest` and
`/projects/[slug]/invest/success` (the actual fund-a-project decision point —
blocked by S-04, never reached by any lane this pass) and
`/tasks/[id]/commerce` (the worker-payment hand-off `gold-worker-sell` is
supposed to exercise but never reaches). `/admin/kyc`, `/dashboard/profile`,
`/dashboard/profile/[id]` and the `*/create`/`*/edit` admin-authoring routes
are also untouched but further from this scenario's core.

**Keep as designed — real sessions now ran and the personas did what they were built for:**
- **Eli Turner / Fay Nakamura** (`ux-plain-words-cash-out`) — both realistic; Eli's low-literacy constraint surfaced that jargon is pervasive and that text-dependent navigation can fail silently, Fay's impatient/slow-network profile is the right shape even though her double-submit test still couldn't complete (harness-level browser-close-on-cleanup issue, not a persona problem). Patience budgets of 2 and 1 were appropriately tight for their archetypes.
- **Ana (`qa-sponsor-ana`) / Chidi (`qa-freelancer-chidi`)** (`ux-keyboard-phone-payout-setup`) — this is the exact 390px + keyboard-only combination the *prior* pass verdict asked be added, and it was: Chidi's strict Tab/Enter/Escape navigation found a real focus bug (focus landing on "Sign out" instead of the drawer's first link), which is now the specific fix in `app-topbar.tsx`'s `onOpenAutoFocus={focusFirstNavigationLink}` (commit `1a5fe14`) — a persona finding that turned directly into a shipped line of code. Ana's follow-through (pressing "Check status", reloading, confirming silence) was the highest-value single action in the lane. Keep both unchanged.
- **Hana (`qa-sme-hana`) / Dina (`qa-freelancer-dina`)** (`ux-phone-and-locale-amounts`) — the void-run's core problem (sign-in-wall blocks) is fully resolved: zero workarounds, every sub-goal in both scripts ran to completion. Patience of 3 for both was sufficient; nothing was abandoned for lack of patience, only for the field's genuine `disabled` state, which is the correct outcome to record.
- **qa-gold-sponsor / qa-gold-worker** — keep the stable handles for cross-pass trend comparability. `qa-gold-worker`'s "seed grants no holdings, so an honest empty is valid" framing is well-calibrated for the *expected*-empty case, but this run's actual finding was a broken read (an error, not an empty state) — see the re-power note below.

**Re-power / adjust for next pass:**
- **`qa-creator-gus`** (`ux-creator-to-sponsor-funding-4`/`-5`) — **the prior verdict already recommended moving Gus's fixture-creation role into prebake instead of a UX lane, and that recommendation was not actioned**: Gus's two lane tasks this pass were `BLOCKED` and `PARTIAL` again, both for the same reason (`find my project uxqa-p1 Tool Library` / `Try again shows my project` — the project the fixture needs doesn't resolve). This is now a two-pass-running fixture gap, not a one-off; move Gus's project-creation step into prebake before the next run, as already advised once.
- **`qa-sponsor-ben` / `qa-creator-gus`** (`ux-creator-to-sponsor-funding`) — per this pass's own lane feedback, the journey still depends on a fixture prebake could not create, so the creator-to-backer hand-off and `/invest` remain unreached. Ben's amount-audit goal targets `/portfolio/withdraw`, which is disabled under the economy gate; retarget it at `/portfolio/add-funds` or the `/swap` amount field — the enabled surfaces — while rails stay gated. The lane is also missing an **unverified** member on the same money pages to contrast against a verified member's refusal copy.
- **`qa-auditor-ivo`** (`ux-screen-reader-money-forms`) — still could not exercise his core goal: rails are gated, add-funds is blocked, so no submit was ever enabled and the double-submit/pending-announcement checks stayed unreachable. Needs at least one money form that can actually submit (even into a refusal) to test busy and idempotency announcements. A **funded user whose balance read actually succeeds** is a missing persona across this whole pass — every persona currently sees "Not available" everywhere, which covers the honest-error copy well but leaves real amount validation (more-than-available, withdraw minimums) completely untested.
- **`qa-gold-worker`**'s persona brief conflates two distinct anticipated outcomes — "seed grants no holdings" (an expected empty state) and "the read itself is broken" (this run's actual observation) — under one line. Split them in the brief; they call for different defect severities and this pass's PARTIAL outcome was correctly the broken-read case, not the empty-state case the brief anticipated.
- **`qa-sponsor-ben` / `qa-freelancer-dina`** (`ux-empty-vs-error-truth`) — this lane's harness limitation (`captureRoute()` only throws on an unexpected sign-in bounce, never on "the page behaves badly") means a persona can record `COMPLETED` on a sub-goal while the real defect (e.g. a permanently disabled Continue button) produces no abandonment signal. Recommend `attempt()` be allowed to fail when the UI doesn't do what the goal implies, so impatient personas like Fay would actually abandon and that abandonment becomes evidence. Keep Ben as the "auditor" tier (opus, patience 5, genuine probing with custom amounts and submits) — that tiering choice worked and is worth reusing for any future auditor role. Dina's slow-network emulation (400ms/200kbps) was too light a touch to produce the transient garbage states her persona bio anticipates; consider a slower profile if loading-vs-error ambiguity is the thing to stress next.
- **`qa-sponsor-ben`** (`probe-money-capabilities`) — his model tier (opus) is set persona-wide, but his slice of this specific probe lane is 8 mechanical read-only calls with no judgment required; sonnet (or haiku, which this subagent effectively ran at) would suffice for that slice specifically. The lane's own scope restriction — reads only, no verified-member mutation data point — is a real, named gap, not a persona failure: a verified mutation very likely hits the same gate as the reads do, but that's an inference, not a measured result. Four of this lane's reads are currently masked by `PREBAKE-1`'s backend-500 crash and would need re-probing once that's fixed, specifically, before this gap can be called closed.

**Harness/methodology notes raised by multiple lanes (not persona changes, but should be fixed before the next pass so lane time isn't spent re-discovering the same thing):**
- `uxSession`'s `attempt()` retries the identical closure up to the attempt budget on failure — right for flaky/transient actions, wrong for "does X exist at all" checks (burns the full patience budget on 3 byte-identical tries) and wrong for a genuinely `disabled` field (an `el.isDisabled()` precheck before the attempt loop would record the same true finding in under a second instead of ~9s of identical timeouts). Two independent lanes (`ux-keyboard-phone-payout-setup`, `ux-phone-and-locale-amounts`) hit this.
- A `position:fixed` skip-link's apparent position in a `fullPage` Playwright screenshot does not match its real viewport position — one lane spent an extra verification pass chasing what looked like a real overlap defect before confirming it was a screenshot artefact. Cross-check any `fullPage`-sourced reflow/clipping finding against a viewport-only shot before filing it.
- `gold-see-balance`'s success predicate (`textVisible 'ARDA'`) is satisfied by the page's own "YOUR ARDA BALANCE" heading even when the balance read fails and shows "Not available" underneath — this gold task will read `COMPLETED` on every run against this gated environment regardless of whether a real balance was ever shown. If the trend metric is meant to track "can a sponsor actually see their balance," tighten the predicate to something that requires the numeric value itself (e.g. `roleVisible` on the figure, or `textVisible` on a dollar-formatted string).
- None of the four run logs in `ux-creator-to-sponsor-funding` recorded an HTTP error even though the API calls fail — tRPC batch responses or server-side calls likely don't surface as `status >= 400`, so `metrics.httpErrors` under-reports in this study. Flagged as uncertain by the lane itself; worth a dedicated instrumentation check rather than guessing.

**Retire:** none — every lane's own feedback describes a well-targeted persona design; every open gap traces to fixtures, gating, or harness behavior, not to a persona that was the wrong shape for its job.

## 5. Next scenario suggestion

This pass is the first real (non-void) run of `01-cash-out-edges`, and it did
exercise genuine cash-out surfaces — unlike the prior void run, where nothing
got past the sign-in wall. Four commits now exist that plausibly fix 5 of the
7 HIGH-band findings (S-01, S-02, S-03 partially, S-06, S-13), but none of
those fixes have been re-observed by an actual UX-harness run yet — only by
the engineering test suite in §2. **The immediate next step is re-running
`01-cash-out-edges` itself** against the fixed code, once the coordinator
re-run in §1/§2 is complete, so the S-01/S-02/S-06/S-07/S-08/S-13 claims get
`ux.json` evidence instead of just commit messages, and so `gold-add-funds` /
`gold-withdraw` / `gold-sell-units` get a second data point each — `synthesis.md`
§7 already notes that pass 02 needs at least 3 runs per gold task before any
trend claim means anything, and right now every gold task has exactly 1.

For the scenario *after* that confirmation run, the strongest candidate is
unchanged from the prior verdict's read: **project discovery and the
evaluate-before-commit decision**, motivated directly by **S-04** (the project
service's 500 breaks every creator-to-sponsor hand-off and leaves
`/projects/[slug]/invest` unreached by any persona across two passes now) and
**S-23** (no-equity disclosure unconfirmed at the actual invest decision
point, currently blocked by S-04). That scenario would finally exercise
`/opportunities`, `/opportunities/[slug]`, `/projects/[slug]/invest`,
`/projects/[slug]/invest/success`, `/governance` and `/settings/verification`
— all still in this pass's `unvisited` list — and it can reuse `qa-sponsor-ben`
and `qa-creator-gus`, already cast for exactly that hand-off, provided Gus's
fixture-creation step is moved into prebake first as recommended in §4.
