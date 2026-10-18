---
type: spec
title: Nova AI loop-4 follow-ups
status: proposed
tags: [nova, ogai, white-label, copy, tests]
---
# Nova AI loop-4 follow-ups

## Why

The loop-4 fix lane (`loop4/nova`) fixed four problems from the adversarial review:

- A first turn that ArdaNova dropped still spent the member's allowance.
- A dropped continuation left the member retrying forever.
- The brand guard missed the short forms of the provider's name.
- The header and footer copy contradicted the mode.

It also fixed these smaller items:

- Unknown verdict outcomes were rendered raw. They are now dropped.
- Source links accepted any scheme. Only http(s) links are kept now.
- Refinements were saved in the tab without an owner. They are now bound to the signed-in member.
- A stale local preview showed the BAD_REQUEST copy. It now returns 404 ("start again").
- The privacy copy now names third-party models and web search.

The items below were too large for the final loop, or they need someone else's decision.

## Scope

1. **Preview tools call template text "Nova" drafts.** In `nova-assistant.tsx` (the `Present` source label, the
   "Edit Nova draft" aria-label, and others), the copy implies that Nova AI wrote local template text. Rename it to
   "preview draft" wherever no model produced the text, and keep `NOVA_NON_ACTIONS` as the single boundary list.
2. **Two documents both claim to be "Nova AI" and contradict each other.** The adversarial critique was cut off
   before it named them. Find them first (candidates: `components/ai/AGENTS.md` versus
   `Infrastructure/Ogai/AGENTS.md`, or the OGAI `partner/AGENTS.md`), then reconcile them.
3. **Component test for "Create a draft project".** Use `nova-refine-panel.tsx`. Drive draftProject, then
   `project.create`, then the success and error UI through a rendered panel with tRPC faked at the edge. Today only
   the pure mapping (`nova-brief-draft.test.ts`) is covered.
4. **`describeError` coverage.** One table-driven test that asserts each tRPC code's copy blames the right party:
   own limit, capacity, start over, or try again.
5. **OGAI `partnerVerticalNote` opt-out.** It is baked into every partner's prompt. Give `Brand` a field before a
   second commercial partner arrives. This needs the OGAI lease holder.
6. **Capacity numbers and the `nova_capacity` alert** and **a persisted allowance** stay in `nova-ai-live-rollout`.

## Out of scope

New Nova modes (`nova-ai-partner-modes`).

## Acceptance

No member-visible string calls template output "Nova". Both documents agree. The draft click flow and the error
copy have tests that would fail if a code mapped to the wrong party.
