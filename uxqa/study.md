---
type: UX Study
title: "ArdaNova UX QA + research study"
description: Target, guard, adapter, routes, gold tasks and safety rules for agent-swarm UX passes against the released Railway environment.
timestamp: "2026-10-25"
---

# ArdaNova - UX study

## Product

ArdaNova is a project-funding and work-for-stake platform: creators provision a
project with its own token, sponsors fund it (fiat -> ARDA -> project token),
and freelancers join projects for stake or pay, settled through the AZOA node.
This study focuses on the financial side: cash-out (withdraw / payouts), wallet
balances, add-funds, invest and settlement surfaces, hunting edge cases.

## Target environment

- **Base URL:** https://ardanova-client-production.up.railway.app (the released
  Railway QA deployment; despite the environment's name it is the team's QA
  target, BFF runs ASPNETCORE_ENVIRONMENT=Staging and reports economy=disabled
  until value rails are provisioned)
- **Guard:** `GET /api/health` -> `deploymentKey` must equal
  `qa-ardanova-a876ca12` (set only on this service; a real production
  deployment never carries it). Deny-listed hosts: none yet - add any future
  custom production domain here.
- **Mail:** no mail paths on the financial surfaces; Google OAuth is bypassed
  by the adapter (no real sign-in emails).
- **Who owns it / how it is reset:** Jade Zaher; Railway project
  `a876ca12-bbf8-4c84-bce7-2c4268f3296c`. No automated reset - fixtures are
  marker-tagged (`uxqa`) and personas are namespaced (`qa-*`).

## Session adapter

- Seed: `node uxqa/adapter/adapter.mjs seed {personas} {out}`
- Link: `node uxqa/adapter/adapter.mjs link {handle}`
- Guard: `node uxqa/adapter/adapter.mjs guard`
- Handle rule: `^qa-[a-z0-9-]+$`
- Sessions file: `uxqa/.secrets/sessions.json` (gitignored; never printed)
- Mechanism: `railway ssh` into the Ardanova-Client service; a pushed-over-stdin
  script upserts `qa-*` users with Prisma and mints Auth.js v5 session JWEs with
  the in-container AUTH_SECRET (secret never leaves the container). STATUS:
  skeleton - finish + smoke once the client deploys (blocked on azoa-sdk publish).

## Routes and API

- Routes dir: `ardanova-client/src/app` (next-app)
- API style: tRPC at `/api/trpc`; REST under `/api/`

## Gold tasks (success texts to re-verify at smoke against the deployed build)

| id | persona | goal (user words) | success |
|---|---|---|---|
| gold-see-balance | qa-gold-sponsor | Find how much money you have available | textVisible "ARDA" on /portfolio |
| gold-add-funds | qa-gold-sponsor | Add $5 to your balance with your card | urlMatches checkout.stripe.com |
| gold-withdraw | qa-gold-sponsor | Send $1 from your balance to your own bank | textVisible "On its way" on /portfolio/withdraw |
| gold-sell-units | qa-gold-worker | Sell some of your project units | textVisible "going through" via /swap |

## Safety rules (in addition to the plugin's reference/safety.md)

- Stripe: TEST card 4242 4242 4242 4242 only; amounts <= $25; always UNTICK
  "Save my information with Link" (it demands a phone number otherwise).
- Never touch the `demo-*` personas or the `solar-micro-grid-commons` project
  (the owner's demo state); mutate only `uxqa`-marked fixtures created by
  `qa-*` personas.
- Never change Railway services, variables or deployments; the adapter's
  `railway ssh` seed is the only permitted infrastructure touch.
- KYC only through the UI verification flow; never call azoa-api admin doors.
- The AZOA node (azoa-api) is shared: probes read public health only, no keys.

## Verification commands (re-run by the coordinator, never trusted from agents)

- Tests: `cd ardanova-client && npx vitest run`
- Typecheck: `cd ardanova-client && npx tsc --noEmit`
- Build: `cd ardanova-client && npm run build`
- Known-red baseline: none

## Machine config

The block below is what the scripts read. Keep it in sync with the prose above;
it wins when they disagree.

```json uxqa-config
{
 "product": "ArdaNova",
 "baseUrl": "https://ardanova-client-production.up.railway.app",
 "guard": { "path": "/api/health", "field": "deploymentKey", "equals": "qa-ardanova-a876ca12", "forbidHosts": [] },
 "session": { "cookieName": "__Secure-authjs.session-token", "cookieDomain": null, "sessionsFile": "uxqa/.secrets/sessions.json" },
 "adapter": { "seed": "node uxqa/adapter/adapter.mjs seed {personas} {out}", "link": "node uxqa/adapter/adapter.mjs link {handle}", "guard": "node uxqa/adapter/adapter.mjs guard" },
 "routes": { "dir": "ardanova-client/src/app", "framework": "next-app", "ignorePrefix": null },
 "api": { "style": "trpc", "prefixes": ["/api/"], "trpcPrefix": "/api/trpc", "proceduresFile": null },
 "playwright": ".omc/qa-harness/node_modules/playwright/index.mjs",
 "browserChannel": null,
 "axePath": null,
 "marker": "uxqa",
 "attemptBudget": 3,
 "lenses": [],
 "goldTasks": [
  { "id": "gold-see-balance", "persona": "qa-gold-sponsor", "start": "/portfolio", "goal": "Find how much money you have available", "success": { "textVisible": "ARDA" }, "maxSeconds": 240 },
  { "id": "gold-add-funds", "persona": "qa-gold-sponsor", "start": "/portfolio/add-funds", "goal": "Add $5 to your balance with your card", "success": { "urlMatches": "checkout\\.stripe\\.com" }, "maxSeconds": 300 },
  { "id": "gold-withdraw", "persona": "qa-gold-sponsor", "start": "/portfolio/withdraw", "goal": "Send $1 from your balance to your own bank", "success": { "textVisible": "On its way" }, "maxSeconds": 300 },
  { "id": "gold-sell-units", "persona": "qa-gold-worker", "start": "/swap", "goal": "Sell some of your project units", "success": { "textVisible": "going through" }, "maxSeconds": 300 }
 ],
 "targets": { "goldSuccess": 0.8, "newHigh": 0, "frictionTolerance": 0.1, "routeCoverage": null },
 "fixRepo": null,
 "testCommand": "cd ardanova-client && npx vitest run",
 "typecheckCommand": "cd ardanova-client && npx tsc --noEmit",
 "buildCommand": "cd ardanova-client && npm run build",
 "baseRedFile": null,
 "hooks": { "afterFix": "git push origin main", "waitDeploy": "node uxqa/adapter/wait-deploy.mjs", "beforePass": null },
 "extraRules": [
  "Stripe TEST card 4242 only, amounts <= $25, always untick Link save",
  "Never touch demo-* personas or solar-micro-grid-commons; mutate only uxqa-marked fixtures owned by qa-* personas",
  "Never change Railway services, variables or deployments",
  "KYC only through the UI verification flow; no azoa-api admin doors",
  "Node probes read public health only, never with keys"
 ]
}
```
