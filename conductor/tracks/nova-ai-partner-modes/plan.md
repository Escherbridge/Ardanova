---
type: plan
title: Nova AI modes beyond refinement - plan
status: proposed
tags: [nova, ogai]
---
# Plan

## Phase 1: OGAI review capability (OGAI lease holder)
- [ ] Spec the review prompt and strict schema; eval it on 20 real drafts before any partner sees it.
- [ ] `partner/v1/review` route with the brand voice, quota, spend meter and no storage.
- [ ] Flow tests: brand never named (plain and disguised), refusals are machine codes.

## Phase 2: ArdaNova seam
- [ ] `IOgaiClient.ReviewAsync` + service use case + controller route; flow tests with a fake partner.
- [ ] `NovaStatusDto` gains per-mode availability.

## Phase 3: Client
- [ ] Review mode calls the live route when status says so; disclaimers stay on the other modes.

## Phase 4: Brief prefill (projects lane)
- [ ] Session-scoped hand-off from the Nova sheet to `/projects/create`; editable prefill; nothing auto-created.
