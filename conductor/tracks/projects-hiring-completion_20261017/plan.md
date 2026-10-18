---
type: plan
---

# Projects hiring completion: plan

## Phase 1: Unblock the live flows (other lanes)
- [ ] Auth lane: `createTRPCContext` resolves the same bypass session as `authForPage()` in development, with a regression test (spec 5)
- [ ] Platform lane: register a default forbid scheme so refusals return 403 (spec 6)

## Phase 2: Finish hiring
- [ ] DBML: add opportunity `headcount`/`filledCount` and a partial unique index on pending project applications, with a hand-written migration (spec 1, 4)
- [ ] `OpportunityService`: fill and close the role on acceptance, and seat guild members (spec 1, 2)
- [ ] Notify the applicant when an application is decided (spec 3)

## Phase 3: Assignment surfaces
- [ ] Add assignee pickers to the task detail page and the board's list view (spec 7)
- [ ] Re-run the QA harness hiring and membership flow live
