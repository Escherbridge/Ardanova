---
type: plan
---
# Track: Projects hiring follow-ups (loop 2)

> Spec: [spec.md](spec.md).

## Phase 1: Non-finance UI parity [P1]
- [ ] Add `viewerCanManage` to the API's opportunity read and use it to show the owner panel (req 3)
- [ ] Add the "Accept applications" switch and a DRAFT badge to `opportunity-form.tsx` (req 4)
- [ ] Add editing of non-financial fields to the task detail page (req 5)
- [ ] Reduce the `project.ts` and `task.ts` routers to thin proxies (req 6)
- [ ] Use a shared ErrorCard on the four routes (req 7)

## Phase 2: Commerce (blocked: finance on hold) [P1]
- [ ] Seat the accepted bidder as a member inside `AcceptBidAsync` (req 1)
- [ ] Add a token-config setup entry point, or explain the gate (req 2)
