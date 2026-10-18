---
type: plan
---
# Plan: API Path-Segment Safety

## Phase 1: Central guard
- [ ] `base-client.ts`: add a `path` tagged template that encodes every interpolated segment and throws on separator or traversal input. Test it in `base-client.test.ts`
- [ ] Move `recordIdSchema` from `server/api/routers/user.ts` to `src/server/api/ids.ts` and re-export it from `user.ts`

## Phase 2: Sweep
- [ ] Convert all `endpoints/*.ts` interpolations to `path`. The change is mechanical, so fan it out by file
- [ ] Replace `z.string()` id inputs in all routers with `recordIdSchema`
- [ ] Flow test: traversal and query payloads against one public procedure per router return `BAD_REQUEST`

## Phase 3: Review
- [ ] `/security-review` on the sweep diff
