---
type: plan
title: Nova AI live rollout - plan
status: proposed
tags: [nova, ogai, rollout]
---
# Plan

- [ ] OGAI lease holder: push `partner/ardanova-nova`, deploy it, and set `PARTNER_API_KEYS`.
- [ ] Staging ArdaNova: set `Ogai__BaseUrl` and `Ogai__PartnerApiKey`; confirm `GET /api/nova/status` = `live`.
- [ ] Live run with a commercial idea: question turns, then a go/adjust verdict with a brief. Save the transcript (no PII).
- [ ] Live run with a community idea: it reaches a verdict with a brief, not a 503.
- [ ] Scan the raw live `nova.*` response bodies for the upstream name; create a draft project from the live brief.
- [ ] Owner: size `DailySessionCap`/`DailySpendUSD` against expected daily active members; alert on `nova_capacity`.
- [ ] Before a second API replica: persist `NovaSessionAllowance` (DBML first, then the migration).
