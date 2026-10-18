---
type: spec
status: proposed
priority: P1
source: loop3 design QA + adversarial critique (2026-10-18)
---
# Design Loop-3 Follow-ups

## Problem

Loop 3 fixed the design-system side: text-safe accent tokens, a WCAG contrast
gate, and white-alpha and page-title ratchets. The rest of the work is in
files owned by feature lanes:

1. **Invest stalls for about 10 s, then blames the wrong thing.** A project
   with no token config gets a fast 404 from .NET (5 ms) and `NOT_FOUND` from
   `projectTokens.getConfigByProject`. `src/trpc/query-client.ts` sets no
   `retry`, so React Query retries 3 times with backoff before the page shows
   "could not verify … against the funding contract". `shouldRetryQuery`
   already exists in `components/ui/query-error-copy.ts`, but nothing calls it.
2. **48 of 52 app routes share the root `<title>`** (WCAG 2.4.2). The gate
   ceiling is `UNTITLED_PAGE_CEILING = 48`.
3. **30 `border-white/N` / `divide-white/N` classes** are invisible on the
   paper background. They are in settings, tasks/[id], escrow, equity treasury
   and admin. Ceiling `WHITE_ALPHA_BORDER_CEILING = 30`.
4. **`components/projects/proposals-tab.tsx`** has about 80 raw palette
   classes. It is the largest single block of the `RAW_PALETTE_CEILING = 144`.

## Acceptance

- `/projects/<slug>/invest` for an unconfigured project shows "Funding is not
  set up for this project" within one request round-trip (no retries on 4xx).
  The contract-failure copy is shown only for `configContractInvalid`.
- Every ceiling named above is lowered in the same commit that pays it down.
  None of them is raised.
- Seed one project with a FUNDING-gate token config and non-zero ARDA supply,
  so the invest journey can reach step 2 in QA.
