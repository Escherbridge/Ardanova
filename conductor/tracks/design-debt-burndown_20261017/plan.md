---
type: plan
---
# Track: Design-System Debt Burn-down

> Spec: [spec.md](spec.md). Each phase lowers a ceiling in
> `ardanova-client/src/styles/design-gate.test.ts` in the same commit.

## 1. Raw error messages, money and rights first [P1]

- [ ] `app/portfolio/withdraw/page.tsx` (7 sites) → `MutationErrorNotice action="request the withdrawal"`.
- [ ] `app/swap/page.tsx` (3 sites) → `action="complete the swap"`.
- [ ] `components/projects/proposals-tab.tsx` (5 sites): list/detail loads use `QueryErrorState`; create/vote use `MutationErrorNotice`.
- [ ] `app/tasks/create/page.tsx:228`: `errors.submit` set from `describeMutationError(error, itemType === "pbi" ? "create the PBI" : "create the task")`.
- [ ] Remaining sites (`tasks/page.tsx:718`, `guilds/updates-tab.tsx:215`, …): owner lanes.
- [ ] Ceiling at 0 → replace the ratchet with an ESLint `no-restricted-syntax` rule.

## 2. Raw palette → tokens [P2]

- [ ] `proposals-tab.tsx` (92 classes): gray → `muted`/`foreground`/`border`; red/green/blue/amber boxes and type badges → Badge variants.
- [ ] Opportunities tabs, `opportunity-detail-view`, `credential-badge`, `guilds/[slug]`, `guilds/reviews-tab`.
- [ ] `tasks/create/page.tsx:33-36`: LOW `bg-system`, MEDIUM `bg-warning`, HIGH `bg-primary`, URGENT `bg-destructive`; `:288` `system-purple` → `system`, and drop it from `KNOWN_UNDEFINED_TOKENS`.
- [ ] `events/page.tsx:156-157`: webinar `bg-system/20 text-system border-system/30`, ama `bg-warning/20 text-warning border-warning/30`.

## 3. Per-route titles [P2]

- [ ] Add a `layout.tsx` with metadata to each of: dashboard, projects, tasks, people, guilds, events, opportunities, governance, portfolio, swap, chats, settings, studio, credentials, admin.

## 4. Retire neon aliases [P3]

- [ ] Codemod `neon` → `system`, `neon-pink` → `primary` or `brand-signal-deep` (text), `neon-green` → `success`, `neon-yellow` → `warning`, `neon-purple` → `chart-5`; delete the aliases.
