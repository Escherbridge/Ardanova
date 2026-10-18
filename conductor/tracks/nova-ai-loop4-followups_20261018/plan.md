---
type: plan
title: Nova AI loop-4 follow-ups - plan
---
# Plan: Nova AI loop-4 follow-ups

## Phase 1: Copy truth (client only)
- [ ] Rename the "Nova draft" labels on template output to "preview draft" (`components/ai/nova-assistant.tsx`).
- [ ] Find the two contradicting "Nova AI" documents and reconcile them.

## Phase 2: Tests
- [ ] Panel test: the draft click flow succeeds and fails with tRPC faked at the edge.
- [ ] Table test for `describeError`: each code's copy names the right party.

## Phase 3: OGAI (lease holder)
- [ ] Add a per-brand opt-out for `partnerVerticalNote` before onboarding a second partner.

Review gate: `/code-review` at the end of each phase. Record a one-line verdict here.
