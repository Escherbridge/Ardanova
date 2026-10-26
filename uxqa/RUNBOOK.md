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
- Guard proven: PENDING - `node <plugin>/lib/guard.mjs` requires the deployed
  client to serve /api/health with deploymentKey=qa-ardanova-a876ca12
  (variable already set on the service; endpoint committed in `dcdb0f6`).
- Adapter smoke: PENDING - adapter.mjs is a skeleton; finish the Auth.js JWE
  mint over `railway ssh` once the client deploys.
- ux-lib smoke: PENDING (after the two gates above).
- NEXT: publish azoa-sdk -> bump ardanova-client dep + lockfile -> push ->
  client deploys -> finish adapter -> run the three setup gates -> first pass
  `01-cash-out-edges` (research-only until fix-committing is re-authorized).
