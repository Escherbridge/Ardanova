---
type: spec
title: Nova AI powered by OVERSEASGENAI (white-label)
status: in-progress
tags: [nova, ogai, ai, white-label]
---
# Nova AI powered by OVERSEASGENAI (white-label)

## Overview

Nova AI is ArdaNova's in-product assistant. Its first live capability is **project idea refinement**:
a short grilling conversation that ends in a feasibility verdict (go / adjust / not_yet) with cited
public evidence and a refined brief. It is powered by OVERSEASGENAI's validate ("Grill-Me") stage,
consumed through a server-to-server partner API, and white-labelled: members only see "Nova AI"
(a single footer credit names the provider).

## Built (loop 1)

- **OGAI** (repo `overseasgenai`, branch `partner/ardanova-nova`, not pushed or deployed):
  `POST /api/partner/v1/validate`, bearer partner key checked against SHA-256 hashes in
  `PARTNER_API_KEYS` (constant time, fail-closed mounting); `validate.Brand` threaded through
  `validate.Config` (own prompt identity, host names excluded from every reply and source, brief padded
  in the partner's name, own state key, spend meter and session quota, no transcript, no receipt).
- **ArdaNova .NET**: `NovaController` (API key + actor assertion) → `NovaOrchestrationService`
  (validation, per-member daily allowance, brand guard) → `IOgaiClient` (`Infrastructure/Ogai`: typed
  HttpClient, local preview when unconfigured, disabled in Production when unconfigured).
- **ArdaNova client**: tRPC `nova.refineProject` thin proxy; the Nova sheet gains a live "Refine idea"
  surface; preview tools stay local and say so; stale-source version reuse after undo fixed.

## Requirements still open

1. Provision: generate the partner key, store the raw key as `Ogai__PartnerApiKey` on the .NET service
   and its hash as `PARTNER_API_KEYS=ardanova:<sha256>` on the OGAI site; set `Ogai__BaseUrl`.
2. Merge and deploy `partner/ardanova-nova` in OGAI, through that repo's orchestrator lease holder
   (`overseasgenai/conductor/RUNBOOK.md` "Orchestrator lease"), then a live smoke through ArdaNova.
3. Decide whether Nova should be allowed to read a project the member can access (server-side read
   check in the service) before any page record is sent upstream; today only typed text is sent.
4. Back the remaining Nova modes (Ask, Review, Present, Rehearse) or keep them labelled previews;
   map OGAI's launch stage to ArdaNova project creation only after it is agentic upstream.
5. Partner billing/usage export and key rotation (two live hashes per id).

## Acceptance

- A member on a deployed stack completes a refinement and gets a verdict whose response, logs and UI
  never name the provider outside the footer credit.
- With OGAI down, the member sees "Nova AI is unavailable" and their idea is kept.
