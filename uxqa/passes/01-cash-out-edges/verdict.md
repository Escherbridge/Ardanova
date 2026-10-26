---
type: Pass Verdict
pass: 01-cash-out-edges
scenario: cash-out-edges
status: CHANGES_REQUESTED
---

# Pass 01 verdict: cash-out and the money's edges

## 1. Exit checks

Source: `uxqa/passes/01-cash-out-edges/ux-report.json` → `exitChecks`, cross-read against `synthesis.md` §3, §6, §7 (no `ledger.json` exists in this pass directory — see note below the table).

| Check | Target | Observed | Result |
|---|---|---|---|
| Gold success rate | ≥ 0.8 | 0 of 4 gold tasks ran: `gold-see-balance`, `gold-add-funds`, `gold-withdraw`, `gold-sell-units` all missing (empty run dirs) | `ok: false` |
| Friction per journey vs previous pass | within 10% tolerance of previous pass | previous (`00-setup`) = 1, current (`01-cash-out-edges`) = 15 | `ok: false` — `synthesis.md` §3 flags this as a blocker artifact: all 165 friction points sit on two routes (`/`, `/auth/signin`) hit by all 11 signed-out runs, not a real regression against `00-setup`; the two passes are not comparable this way |
| New HIGH-severity findings | 0 | `ux-report.json`'s own `exitChecks.newHigh` is uncomputed (`count: null`, `ok: null`). `ledger.json` was not found at `uxqa/passes/01-cash-out-edges/ledger.json` or anywhere under `uxqa/`. The closest available record is `synthesis.md` §6's severity table, which bands 4 findings critical this pass, 3 of them explicitly tagged HIGH (F1 score 27, F2 score 18, F3 score 18); F3 has since been promoted to FIX (commit `ca1be70`), leaving **F1 and F2 open as HIGH-band** | not computed by the harness; 2 HIGH-band findings remain open per the synthesis ledger |
| Route coverage | not set | 2 of 53 page routes visited (ratio 0.0377); 1 of 12 declared APIs hit (`GET /api/auth/session` only) | ratio reported, `ok: null` (no target configured) |
| Review verdict | — | **APPROVE** | APPROVE |
| Coordinator re-run | — | 2026-10-26: `npx vitest run` -> `Tests  590 passed (590)`; `npx tsc --noEmit` -> clean (exit 0); `npm run build` -> exit 0. Matches the integrator's claims verbatim. | **GREEN** |

**Coordinator ruling:** status CHANGES_REQUESTED — the three fixes are verified
and released, but the pass itself is structurally void (adapter seed read the
personas file as a bare array while the plugin writes `{personas:[...]}`; fixed
at adapter.mjs seed + a session preflight added to the plugin's uxSession so a
missing seed can never again leak one browser per lane). All 12 personas now
seed (`SEEDED 12`). The pass re-runs under the same scenario; this run does not
count toward exit criteria.

**Note on `ledger.json`:** the task that produced this verdict asked for "new HIGH from `ledger.json`," but no such file exists under `uxqa/passes/01-cash-out-edges/` or elsewhere in `uxqa/`. This verdict substitutes `synthesis.md` §6 ("Findings ranked by severity") and §10 ("Triage outcomes") as the closest equivalent — that table already carries a "Band / ledger" column and FIX/RESEARCH_QUESTION decisions. If a dedicated `ledger.json` is expected to exist going forward, that is itself a gap for the coordinator to close, separate from the re-run.

## 2. Claims to re-run

Every test/typecheck/build command the fix lanes, the integrator, and the fix-review lane reported, verbatim, so the coordinator can re-run each one and confirm the claimed output still holds.

### Fix lane A — wordmark `role="img"` + mobile primary nav (commit `0a79d90`)

```
1) cd ardanova-client && npx vitest run src/components/marketing/home-primary-nav.test.ts
   -> FAIL ... Error: Failed to parse source for import analysis because the content contains invalid JS syntax. If you use tsconfig.json, make sure to not set jsx to preserve.
   -> Test Files  1 failed (1)
   (harness limit; the file was removed from the tree so `npm test` stays green)

2) cd ardanova-client && npx tsc --noEmit 2>&1 | grep -E "wordmark|src/app/page\.tsx|home-primary-nav"
   -> no output; whole project shows `total errors: 0`

3) cd ardanova-client && npx eslint src/components/brand/wordmark.tsx src/app/page.tsx src/components/marketing/home-primary-nav.tsx
   -> eslint exit 0

4) node <scratchpad>/home-a11y.cjs
   (Playwright from .omc/qa-harness + axe-core 4.10.3 from client node_modules, against local dev server at http://localhost:3000 serving this working tree;
    curl confirmed `>Menu<` and `role="img" aria-label="ArdaNova"` x2 in the HTML)
   -> at 390px: axe violations: none; PASS no aria-prohibited-attr; PASS toggle collapsed;
      PASS toggle >=44px (80.859375x44); PASS expanded after click;
      PASS links reachable: Method|Rights|Nova|Projects; axe (menu open): none;
      PASS Escape closes; PASS focus returned to toggle;
      PASS link activation closes + scrolls to #rights (top=88)
   -> same results at 683px (1366 at 200% zoom)
   -> at 1280px: axe violations: none; PASS desktop nav visible, menu button hidden
   -> final line: ALL PASS
   (an earlier run failed the link-activation check only because Next updates the URL hash
    asynchronously; a 2.5s wait for the smooth scroll fixed it; the menu was closed in both runs)
```

### Fix lane B — sign-in destination label (commit `ca1be70`)

```
cd ardanova-client && npx vitest run src/app/auth/signin
   -> Test Files  1 passed (1) / Tests  14 passed (14)

cd ardanova-client && npx tsc --noEmit 2>&1 | grep -E "src/app/auth/signin"
   -> no output (no errors on my paths)

cd ardanova-client && npx tsc --noEmit 2>&1 | grep -c "error TS"
   -> 0 (no errors anywhere in the repo)
```

### Integrator

```
1) cd ardanova-client && npx vitest run
   -> Test Files  62 passed (62) / Tests  590 passed (590)
      (full suite: all pre-existing tests plus the new destination-label.test.ts)

2) cd ardanova-client && npx tsc --noEmit
   -> no output (clean, exit 0)

3) cd ardanova-client && npm run build
   -> ✓ Compiled successfully in 33.3s ... ✓ Generating static pages (48/48)
      (full Next.js production build, no errors)

4) Pre/post-build `git status --porcelain=v1` diff -> identical
   (build/typecheck/test runs did not mutate the tree)

5) `git diff --stat` before staging -> exactly: wordmark.tsx +1, page.tsx +2, signin/page.tsx +12
   (matches both lane reports' claimed line ranges, no stray edits)
```

### Fix-review lane

```
Server under test: `npx next dev --turbo -p 3107` in ardanova-client (own server; port 3000 was
already held by another PID returning 500, left alone). Stopped afterwards via taskkill on its
PID and child PID; port 3107 confirmed free.

1) UXQA_CHECK_BASE=http://localhost:3107 node --test uxqa/checks/01-cash-out-edges.check.mjs
   (final run, source clean):
   ✔ sign-in names the money destination the visitor came from
   ✔ sign-in names no destination for an off-site callbackUrl
   ✔ home menu below 768px opens, lists the four destinations, and Escape returns focus to the toggle
   ✔ axe scan of / reports no aria-prohibited-attr (F9)
   tests 4 / pass 4 / fail 0

2) Mutation run (signin label nulled, role="img" removed, focus() removed):
   ✖ destination, ✔ off-site (expected — checks absence), ✔ menu (false pass — focus already on
     toggle; test was then fixed to focus a panel link first), ✖ axe. pass 2 / fail 2.
   Re-run with only focus() removed: ✖ menu, ✖ axe. pass 2 / fail 2.
   (the axe failure here is unexplained since the wordmark had already been restored — suspected
    stale hot-reload, not confirmed)
   All three files restored from scratchpad backups; `git status` showed no tracked changes before
   the final green run.

3) npx vitest run src/app/auth/signin/destination-label.test.ts (in ardanova-client)
   -> Test Files 1 passed (1), Tests 14 passed (14)
```

## 3. Ledger summary

Reconstructed from `synthesis.md` §6 and §10, since `ledger.json` does not exist (see §1 note). 14 findings total this pass (`G1`, `F1`–`F12`, `F14`).

**By severity band:**

| Band | Count | Findings |
|---|---|---|
| critical | 4 | G1, F1, F2, F3 |
| serious | 5 | F4, F5, F6, F7, F12 |
| minor | 5 | F8, F9, F10, F11, F14 |

**By decision:**

| Decision | Count | Findings |
|---|---|---|
| FIX | 2 numbered findings + 1 unnumbered code defect | F9, F3 (promoted outside the original triage rule), mobile primary-nav disclosure (not tied to a finding number) |
| RESEARCH_QUESTION (deferred to the §8 human-validation plan) | 12 | G1, F1, F2, F4, F5, F6, F7, F8, F10, F11, F12, F14 |

**FIX items with commits:**

| Finding | What changed | Commit |
|---|---|---|
| F9 — `aria-prohibited-attr` on `/` | `ArdaNovaWordmark`'s outer `<span>` had no role while carrying `aria-label`; it now has `role="img"` | `0a79d90` |
| Mobile primary nav (no finding number — a code-checkable defect, not F1) | Home `<nav>` was `hidden ... md:flex` with no sub-768px fallback; `HomePrimaryNavMenu` now exposes Method / Rights / Nova / Projects below 768px | `0a79d90` |
| F3 — sign-in wall ignores the destination | `auth/signin/page.tsx` normalized `callbackUrl` but never displayed it; it now shows `destinationLabel(callbackUrl)` for named internal routes (`data-testid="signin-return-destination"`) | `ca1be70` |
| Regression checks + synthesis §10/triage-rule update | `uxqa/checks/01-cash-out-edges.check.mjs` added (sign-in destination present/absent, mobile menu disclosure + Escape focus return, axe scan of `/`); `synthesis.md` updated to record F3's promotion and that F1 stays open | `3254a81` |

F1 ("no discoverable money entry point pre-auth") is explicitly recorded in `synthesis.md` §10 as **still a RESEARCH_QUESTION, not resolved** by the mobile-nav fix: the new mobile menu carries the same four non-money links as the desktop nav. F11 (duplicate `GET /api/auth/session` calls) was not fixed this pass — not yet confirmed in network code.

## 4. Persona refinement

Coverage this pass was almost entirely blocked: 51 of 53 routes are unvisited, including every money-specific route named in the scenario (`/portfolio`, `/portfolio/add-funds`, `/portfolio/withdraw`, `/settings/payouts`, `/settings/verification`, `/swap`, `/projects/[slug]/invest`). 11 of 12 declared APIs were never hit. The cause is a single infrastructure gap, `PREBAKE-OWNER-1` (no sessions seeded for any pass-01 persona), which every lane independently rediscovered.

**Keep as designed, just needs the seed + a real run:**
- `qa-member-eli` / `qa-member-fay` (`ux-plain-words-cash-out`) — the lane's own feedback calls both personas well-designed and complementary (Eli tests plain-language clarity, Fay tests responsiveness/error recovery under attempt budgets 2 and 1); the lane produced no run directory at all this pass, so it is pure backlog, not a persona defect.
- `qa-sponsor-ana` / `qa-freelancer-chidi` (`ux-keyboard-payout-setup`) — well-specified; Chidi's keyboard-only, patience-3 audit is exactly the shape needed, but it was never exercised (zero sub-goals reached), so the patience=3 budget itself is still uncalibrated and should be re-evaluated once the real settings/payouts form is reachable.
- `qa-freelancer-dina` / `qa-sme-hana` (`ux-phone-and-locale-amounts`) — traits (tap-fast/double-submit for Dina, fr-FR cents parsing for Hana) only surface with a real authenticated money form in front of them; keep both, but do not read this run's friction score (28 for Hana) as real product friction — it was spent entirely on the structural wall.
- `qa-gold-sponsor` / `qa-gold-worker` — keep the stable handles for cross-pass trend comparability; `qa-gold-worker`'s context field ("seed cannot grant holdings, so an honest zero is a valid outcome") is well-calibrated and should not change.

**Re-power / adjust for next run:**
- `qa-sme-hana` in `ux-screen-reader-money-forms` currently depends entirely on `qa-auditor-ivo` producing a list of unnamed controls first; give her an independent sub-goal (e.g., typing `1,50` and `1 000` into the add-funds amount field) so she always has work even if Ivo is blocked.
- `qa-creator-gus`'s role in `ux-screen-reader-money-forms` is purely fixture creation (the "uxqa-p1 Reader Garden" project); that job belongs in prebake, not in a UX lane — move it so Gus's lane time is spent on the creator journey, not fixture setup.
- `qa-sponsor-ben` and `qa-freelancer-dina` in `ux-empty-vs-error-truth` are written assuming "may hold units" / "verified balance" states that the seed mechanism cannot grant even once sessions exist (per `personas.json`'s own `seed` block — it only sets `userType`/`isVerified`/`verificationLevel`). Those two edge cases (genuinely-empty-but-verified, more-than-available balance) will stay untestable without either a second seed pass granting real holdings, or a deliberate decision to accept them as out of scope this cycle — flag to the owner rather than silently re-running and hoping.
- `qa-sponsor-ben` in `probe-money-capabilities` is "pure dead weight when blocked" per that lane's own feedback: there is no safe partial substitute for a verified, authenticated read the way anonymous HTTP can stand in for "no session at all." Keep the persona for when sessions exist, but the probe lane's instructions should explicitly say an anonymous-floor substitute (what `qa-probe-member` did this run, via cookie-less curl against the live deployment) is encouraged, not just tolerated, when the real session is unavailable — it is what produced this pass's only genuine probe signal (typed 401s, auth-before-validation ordering, no fake zeros).

**Add next pass:**
- A keyboard-only persona at 390px on the money forms. The scenario names this combination explicitly but no lane in this pass covers it; `qa-freelancer-chidi` is keyboard-only but not mobile-width, and `qa-freelancer-dina`/`qa-sme-hana` are mobile but not keyboard-only.

**Retire:** none this pass — every persona's own feedback describes a usable, well-targeted design; the gap is infrastructure, not persona fit.

**Cross-cutting process fix to flag to the owner (raised independently by at least 5 of 9 lanes: `ux-phone-and-locale-amounts`, `ux-empty-vs-error-truth`, `gold-sponsor-money`, `gold-worker-sell`, `probe-money-capabilities`):** `ux-lib.mjs`'s `uxSession()` calls `chromium.launch()` and creates the run directory *before* `loadSession()` checks whether the handle exists in `sessions.json`, so every lane that hit the missing-session throw leaked an unclosed browser process. Recommended fix (not a persona change): check `fs.existsSync` + handle-presence first, and fail in milliseconds with zero process cost; separately, wrap the browser lifecycle in try/finally. Several lanes also recommend a single coordinator-side preflight (check `sessions.json` has every handle `personas.json` lists, once, before dispatching any lane) instead of N lanes each rediscovering the identical blocker.

## 5. Next scenario suggestion

Nothing in this pass actually exercised a cash-out edge — all 11 runs died at the sign-in wall, 0 of 4 gold tasks ran, and the money forms the scenario was named for (amount validation, idempotency, KYC gating, locale cents parsing, screen-reader money forms) remain completely untested. The immediate next step is not a new scenario: it is re-running `01-cash-out-edges` itself once `PREBAKE-OWNER-1` is resolved (sessions seeded for every persona in `personas.json`), since this pass does not count toward the loop's two-consecutive-pass exit criteria and nothing here can be trusted as a cash-out finding yet.

For the scenario after that genuine cash-out-edges run, the strongest candidate surfaced by this pass is **project discovery and the evaluate-before-commit decision** — directly motivated by F2 ("Discover projects" and every project link go straight to the sign-in wall; no public project preview exists) and F5 (verification tiers, fees, payout timelines and rails are undisclosed anywhere pre-auth). That scenario would finally exercise `/opportunities`, `/opportunities/[slug]`, `/projects`, `/projects/[slug]`, `/projects/[slug]/invest`, `/governance` and `/settings/verification` — seven of the routes in this pass's `unvisited` list that sit on the critical path between "see a project" and "decide to fund it," and it shares the same now-seeded personas (`qa-sponsor-ben`, `qa-creator-gus`) this pass already cast for exactly that handoff.
