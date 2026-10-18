---
type: plan
---
# Track: Projects hiring follow-ups (loop 3)

> Spec: [spec.md](spec.md).

## Phase 1: Applicant side [P1]
- [ ] Add an actor-scoped `GET /api/opportunities/applications/mine` and an applicant withdraw route (req 1)
- [ ] Build a "My applications" view with status and a withdraw action (req 1)

## Phase 2: Form and co-manager parity [P1]
- [ ] Clean up the `opportunity-form.tsx` role and type options (req 2; design/components lane)
- [ ] Pass the viewer's project role into `TeamTab` and drive the selects and remove controls from `member-authority.ts` (req 5)

## Phase 3: Lifecycle [P2, schema]
- [ ] Add headcount to the DBML and a hand-written migration; set FILLED when the headcount is reached; refuse applications past the deadline (req 3)
- [ ] Send accept, reject and new-application notifications through the event bus (req 4)

## Phase 4: Spot-check
- [ ] Manually check /projects loading with a long-lived profile (req 6)
