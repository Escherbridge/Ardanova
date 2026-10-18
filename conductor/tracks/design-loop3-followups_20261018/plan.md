---
type: plan
---
# Track: Design Loop-3 Follow-ups

> Spec: [spec.md](spec.md). Gate: `ardanova-client/src/styles/design-gate.test.ts`.

## 1. Invest loading terminal state [P1, trpc + projects lanes]

- [ ] `src/trpc/query-client.ts`: `queries: { staleTime: 30_000, retry: shouldRetryQuery }`. Import it from `~/components/ui/query-error-copy`. This one line ends the about-10 s stall on every deterministic 4xx.
- [ ] `app/projects/[slug]/invest/page.tsx:352-356`: on `configQuery.error` with `data.code === "NOT_FOUND"`, show "Funding is not set up for this project yet. Nothing was charged." Keep the contract copy for `configContractInvalid` only.
- [ ] `components/projects/funding-tab.tsx:437`: the same `NOT_FOUND` empty state.
- [ ] `components/equity/equity-preview.tsx:124`: hide the invest CTA when no config exists.
- [ ] Seed: one project with a FUNDING-gate config and non-zero ARDA circulating supply.

## 2. Per-route titles [P2, each feature lane] (lowers UNTITLED_PAGE_CEILING)

- [ ] Add a server `layout.tsx` (or extend the existing one, e.g. `app/people/layout.tsx`, `app/guilds/layout.tsx`) with `export const metadata = { title: "<Section>" }` for projects, tasks, people, guilds, governance, events, portfolio, chats, dashboard, studio, opportunities, settings, swap, credentials, admin and auth.
- [ ] Add `generateMetadata` on `[slug]` / `[id]` routes so the tab shows the entity name.

## 3. White-alpha borders [P2] (lowers WHITE_ALPHA_BORDER_CEILING)

- [ ] `border-white/N` and `divide-white/N` → `border-border` / `divide-border` in `app/settings/{settings-nav,profile/page,payouts/page}.tsx`, `app/tasks/[id]/page.tsx`, `components/escrow/{escrow-detail-card,dispute-detail}.tsx`, `components/equity/treasury-{buckets,transaction-log}.tsx`, `app/admin/{treasury,disputes}/page.tsx`.

## 4. Proposals tab on the design system [P2, projects/governance lane] (lowers RAW_PALETTE_CEILING)

- [ ] `components/projects/proposals-tab.tsx`: map gray text to `text-muted-foreground` / `text-foreground` and gray borders to `border-border`. Map status colours to `success` / `warning` / `destructive` / `system`, and remove `rounded-*`.
