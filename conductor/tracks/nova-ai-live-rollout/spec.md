---
type: spec
title: Nova AI live rollout - first real end-to-end run against OVERSEASGENAI
status: proposed
tags: [nova, ogai, rollout, white-label]
---
# Nova AI live rollout

## Why

Every Nova AI QA pass so far ran against `LocalNovaPreviewClient` (`nova.status` = `local-preview`), so nothing has
proven that Nova AI powered by OVERSEASGENAI works. The partner branch `partner/ardanova-nova` is committed but not
pushed or deployed, and a Production ArdaNova without `Ogai__BaseUrl`/`Ogai__PartnerApiKey` serves Nova as `off`.
Loop 3 QA's "real" quota exhaustion and white-label body scan ran over the preview client. They prove ArdaNova's own
copy and counters, not the upstream.

## Scope

1. The OGAI lease holder (overseasgenai `conductor/RUNBOOK.md`) pushes and deploys `partner/ardanova-nova` (head
   `2d70a4f` or later), generates the partner key and sets `PARTNER_API_KEYS=ardanova:<sha256>`.
2. A staging ArdaNova sets `Ogai__BaseUrl` and `Ogai__PartnerApiKey`, and `nova.status` reads `live`.
3. One recorded session runs the whole flow to a go/adjust verdict with a brief, for a commercial idea and for a
   community one (for example a tool library). Both must reach a verdict. Loop 3 removed the launch-vertical gate that
   turned community ideas into 503s.
4. Rerun the white-label scan over the raw live `nova.*` response bodies, and create a draft project from a live brief.
5. Size capacity before launch. OGAI's ArdaNova brand allows 400 sessions and $12 a day, so about 40 members using
   their full 10 fill the platform's day. Pick the numbers from expected daily active members and add an alert on
   `nova_capacity`.
6. Persist the per-member allowance (DBML table plus migration) once ArdaNova runs more than one API replica. Until
   then the in-memory counter is per replica.

## Out of scope

New Nova modes (`nova-ai-partner-modes`).

## Acceptance

A recorded live session reaches a verdict with a brief for both kinds of idea. A scan of the live bodies finds no
upstream name. The owner signs off the capacity numbers.
