---
type: plan
title: Nova AI powered by OVERSEASGENAI — plan
---
# Plan — Nova AI powered by OVERSEASGENAI

## Phase 1 — Refinement path (loop 1) [done, unreviewed]
- [x] OGAI partner validate API with hashed key auth and brand isolation (Go tests).
- [x] .NET controller → service → typed client, local preview, Production disabled (flow tests).
- [x] tRPC proxy and live "Refine idea" surface; truthful disclaimers; stale-version fix (vitest).
- [ ] Adversarial review of both branches (`/security-review`: partner auth, key handling).

## Phase 2 — Rollout (owner-gated; step by step in `go-live-checklist.md`)
- [ ] Generate key; set `Ogai__BaseUrl`, `Ogai__PartnerApiKey` (ArdaNova) and `PARTNER_API_KEYS` (OGAI).
- [ ] OGAI lease holder merges and deploys `partner/ardanova-nova`.
- [ ] Live smoke: refinement to a verdict through the deployed ArdaNova stack; check logs carry no content or key.

## Phase 3 — Scope growth
- [ ] Optional project-context read with a server-side access check.
- [ ] Back or retire the remaining preview modes.
- [ ] Usage export and key rotation.
