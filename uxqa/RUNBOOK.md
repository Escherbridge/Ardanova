---
type: UX Study Runbook
title: "ArdaNova UX study runbook"
description: Rolling state of the study - newest block first; one block per pass with its recorded verdict.
---

# UX study runbook

Newest block first. Each pass appends one block; nothing here is deleted.

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
- Adapter smoke: PENDING - adapter.mjs seed is a skeleton; finish the Auth.js
  JWE mint over `railway ssh` (sessions minted in-container, secrets never
  leave it).
- ux-lib smoke: PENDING (after seed).
- NEXT: finish adapter seed -> SEEDED smoke -> ux-lib smoke + ux-report ->
  first pass `01-cash-out-edges` (fix-committing allowed again now that both
  repos are released; hooks: afterFix=git push, waitDeploy=wait-deploy.mjs).
