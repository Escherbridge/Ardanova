---
type: spec
status: proposed
priority: P1
source: loop2 design QA + adversarial critique (2026-10-17)
---
# Design-System Debt Burn-down

## Problem

The design system now has the right primitives, but most feature screens do
not use them:

- About 70 JSX sites render a raw `{…error.message}`. For a Zod input failure
  that message is the stringified issue array, which is how `/tasks/create`
  showed users `[{"received":"URGENT","code":"invalid_enum_value",…}]`.
  The sites include money flows (`portfolio/withdraw`, `swap`) and governance
  (`components/projects/proposals-tab.tsx`).
- 156 raw Tailwind palette classes (`bg-blue-500`, `text-gray-900`, …)
  remain, 92 of them in `proposals-tab.tsx`. This is the screen where users
  vote on rights, and it looks like a different product from the shell around
  it. Several of them, such as `text-yellow-500` and `text-green-500` on
  paper, fail WCAG AA.
- 34 routes share one document title, because feature route segments have no
  `layout.tsx` metadata.

## What loop 2 design already shipped (the seams to migrate onto)

- `describeMutationError` / `<MutationErrorNotice>` in
  `ardanova-client/src/components/ui/` (§mutation-error), plus the existing
  `QueryErrorState`.
- `src/styles/design-gate.test.ts`: the undefined-token check plus ratchet
  ceilings for raw palette classes and raw `error.message` JSX.
- `--color-neon` and a distinct `neon-purple`.

## Outcome

1. Every `{…error.message}` JSX site goes through `MutationErrorNotice` or
   `QueryErrorState`. `RAW_ERROR_MESSAGE_CEILING` reaches 0. Then turn the
   ratchet into an ESLint `no-restricted-syntax` rule.
2. `proposals-tab`, the opportunities tabs, `credential-badge`,
   `guilds/[slug]`, `guilds/reviews-tab`, `events` and `tasks/create` map
   their statuses to Badge variants (`success` / `warning` / `destructive` /
   `system` / `neon-purple`). `RAW_PALETTE_CEILING` reaches 0.
3. Each feature route segment gets a `layout.tsx` with
   `export const metadata = { title: "…" }`. The root template already adds
   " — ArdaNova".
4. `neon-*` aliases are replaced with semantic tokens and then deleted from
   `globals.css`.

## Acceptance

- `npx vitest run src/styles/design-gate.test.ts` passes with both ceilings
  lowered to the new counts at each step.
- Live QA: a forced validation failure on withdraw, swap and proposal create
  shows a field-named sentence, never JSON.
- Live QA: document titles are unique across the audited routes.
