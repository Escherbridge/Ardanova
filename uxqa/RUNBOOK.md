---
type: UX Study Runbook
title: "ArdaNova UX study runbook"
description: Rolling state of the study - newest block first; one block per pass with its recorded verdict.
---

# UX study runbook

Newest block first. Each pass appends one block; nothing here is deleted.

## Pass 01-cash-out-edges - 2026-10-26 - CHANGES_REQUESTED

- **Deployed commit:** d5031cd (client), BFF Staging, azoa-api main@500ce8fb
- **Exit checks:** gold 0/4 (no lane had a session), friction/journey 1 -> 15
  (all friction sits on the sign-in wall, not comparable), new HIGH: 2 open
  (F1 no pre-auth money entry, F2 no public project preview), coverage 2/53,
  review APPROVE, coordinator re-run GREEN (590/590, tsc 0, build 0).
- **Ledger:** FIX 3 shipped (commits 0a79d90 wordmark role=img + mobile
  primary nav, ca1be70 sign-in names the return destination, 3254a81
  regression checks); RESEARCH_QUESTION 12 (pre-auth findings carried).
- **Structural blocker (fixed):** adapter seed read personas.json as a bare
  array; the plugin writes {personas:[...]}. Fixed + plugin uxSession now
  preflights the session before launching a browser (10 leaked chromium
  processes reaped). All 12 personas seed: SEEDED 12.
- **Top friction routes:** / and /auth/signin (the wall; everything else
  unreached).
- **Persona refinement:** cast kept as designed; Gus's fixture-creation moves
  to prebake; Hana gets an independent sub-goal; add a keyboard-only 390px
  persona; Ben/Dina's holdings-dependent cases flagged to the owner (seed
  cannot grant holdings).
- **Next:** RE-RUN 01-cash-out-edges with sessions (this run counts for
  nothing); after a genuine run: project discovery / evaluate-before-commit
  (F2, F5).

## Study setup - 2026-10-25 - PARTIAL (blocked on client deploy)

- Scope requested: financial side - cash out / withdraw, wallet balances,
  funding settlement surfaces; hunt edge cases on the released environment.
- Release state at setup: BFF deployed `dcdb0f6` (Staging, economy=disabled);
  client deploy RED - `npm error notarget azoa-sdk@^0.4.0` (publish of
  azoa-sdk 0.5.0 from oasis-sleek/sdk/azoa-wallet requires `npm login`);
  the serving client is still the July build without /api/health identity.
- Guard proven 2026-10-26: `OK https://ardanova-client-production.up.railway.app
  deploymentKey=qa-ardanova-a876ca12` (client deployed e54e9c9 after the
  azoa-sdk publish + builder-platform lockfile regen; adapter guard also OK,
  reporting the served commit).
- Adapter smoke: DONE 2026-10-25 - `node uxqa/adapter/adapter.mjs seed
  uxqa/passes/00-setup/personas.json uxqa/.secrets/sessions.json` (persona
  `qa-uxqa-smoke`) -> `SEEDED 1`. `node -e "console.log(JSON.parse(require(
  'fs').readFileSync('uxqa/.secrets/sessions.json','utf8')).length)"` -> `1`.
  Session-check (fetch `/api/auth/session` with the minted cookie) ->
  `SESSION_CHECK status=200 id=qa-uxqa-smoke
  email=qa-uxqa-smoke@qa.ardanova.test` / `SESSION_CHECK_OK`. Re-ran seed a
  second time: still `SEEDED 1`, one DB row (idempotent upsert by id, no
  duplicate). The mint is a pure `node:crypto` re-implementation of
  `@auth/core/jwt.js` `encode()` (A256CBC-HS512, alg "dir") run inside the
  container over `railway ssh`; the standalone build's node_modules does not
  carry `@auth/core`/`jose` as externals, so those can't be `require`d
  in-container - verified byte-compatible by round-tripping a locally-minted
  token through the real `decode()` before relying on it, then proven against
  the live deployed `/api/auth/session`. `link <handle>` refuses
  (`REFUSED link requires app support`, exit 2): Auth.js here is JWT
  cookie-only and there's no app route to hand a human a URL that sets an
  HttpOnly session cookie, and adding one is out of this adapter's partition.
- ux-lib smoke: DONE 2026-10-26 - flows/smoke/setup-smoke.mjs as qa-uxqa-smoke
  against the live client -> `{"outcome":"COMPLETED","pageViews":2,
  "axeViolations":1,"friction":1}` with trace.zip in
  passes/00-setup/runs/qa-uxqa-smoke--setup-smoke/.
- ux-report: DONE - `ux-report.mjs 00-setup` wrote ux-report.json (gold checks
  idle in setup, as expected). gold-tasks check: all four tasks valid, no
  problems.
- SETUP COMPLETE. NEXT: pass `01-cash-out-edges` (fix-committing allowed;
  hooks: afterFix=git push origin main, waitDeploy=wait-deploy.mjs; launches
  after the azoa authoritative sweep frees local CPU).
