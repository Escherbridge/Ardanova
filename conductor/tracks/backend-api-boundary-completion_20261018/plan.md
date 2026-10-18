---
type: plan
---
# Plan: backend API boundary completion

## Phase 1: feature lanes adopt the pipeline rules

- [ ] Events lane: delete the update/delete pre-checks in `routers/event.ts` (spec item 1).
- [ ] Commerce lane: delete the product pre-checks in `routers/product.ts` (spec item 1).
- [ ] Events lane: add a viewer parameter to the `EventService` list reads and filter in the query (spec item 2).
- [ ] Events lane: check update-time date coherence in `EventService.UpdateAsync` (spec item 3).
- [ ] Gamification lane: generate referral codes server-side (spec item 8).
- [ ] Phase review: `/code-review high` plus a live re-run of QA groups C, D, E and F.

## Phase 2: error mapping and edge limits

- [ ] All lanes: replace `throw new Error(response.error)` with typed `TRPCError`s, router by router (spec item 4).
- [ ] Platform: add an IP-keyed limiter for anonymous traffic in `src/middleware.ts` or at the proxy (spec item 6).
- [ ] Decide on the read-only key for anonymous reads, and record the decision here (spec item 7).
- [ ] Phase review: `/security-review`.

## Phase 3: storage and realtime

- [ ] Attachments lane and infrastructure: move uploads to a presigned `POST` with `content-length-range` (spec item 5).
- [ ] Realtime: use the UTC converter in SignalR serializer options (spec item 9).
- [ ] Operator: publish `azoa-sdk@0.4.0`, then regenerate the lockfile (spec item 11).
- [ ] Phase review: `/code-review high`.
