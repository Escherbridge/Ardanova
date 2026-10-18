---
type: plan
---
# Track: Design Loop-4 Follow-ups

> Spec: [spec.md](spec.md). Gate: `ardanova-client/src/styles/design-gate.test.ts`.

## 1. Retry wiring [P0, trpc lane]

- [ ] `src/trpc/query-client.ts:10-14`: `queries: { staleTime: 30 * 1000, retry: shouldRetryQuery }`. Import it from `~/components/ui/query-error-copy`.
- [ ] Remove the per-page `retry: false` overrides (for example `app/portfolio/page.tsx:84`) unless one is intentional.
- [ ] Flow test: render `/projects/<missing>` with `createQueryClient()` and assert the not-found copy appears after one request.

## 2. Custody error copy [P1, azoa/profile lane]

- [ ] `app/dashboard/profile/page.tsx:339-340` and `app/settings/verification/page.tsx:169-170`: change `statusError={custodyStatus.error?.message}` to `statusError={custodyStatus.error ? describeQueryError(custodyStatus.error).explanation : undefined}`, and change `ensureError` to `describeMutationError(ensureCustody.error, "start account setup")`. `isOperatorMessage` is exported for any custom rendering.

## 3. Backend reason codes [P1, backend lane]

- [ ] `AzoaCustodialAccountService.cs:167`: change it to `Conflict("Account setup is not available yet.")` and log the operator detail. Add an Application test that asserts the returned message names neither AZOA nor tenant.
- [ ] Then replace the client `OPERATOR_MESSAGE` blocklist with a reason-code → copy map.

## 4. Neon buttons [P2, events/guilds/opportunities lanes]

- [ ] `app/events/page.tsx:440,856`, `components/guild-form.tsx:170,492`, `components/opportunity-form.tsx:749`: replace `className="bg-neon hover:bg-neon/90 … text-black"` with `variant="neon"` (white on `#117b88` is 4.99:1). `events/page.tsx:487`: change the badge `text-black` to `text-system-foreground`.

## 5. Carry-overs [P2-P3, tasks/people/settings lanes]

- [ ] `app/tasks/create/page.tsx:35-38`: `bg-blue-500/yellow-500/orange-500/red-500` → `bg-muted-foreground/bg-warning/bg-primary/bg-destructive`, plus a text label. Lower `RAW_PALETTE_CEILING` by 4.
- [ ] `app/people/page.tsx:179`: `useState("guilds")` → `useState("people")`. Mirror the tab in `?tab=`.
- [ ] `app/settings/verification/page.tsx`: replace the one "Azoa" callout with "a trusted verification partner".
- [ ] `app/dashboard/tasks/page.tsx:819`: change `text-border` to `text-muted-foreground`, then empty `KNOWN_FILL_AS_TEXT`.
- [ ] `app/dashboard/page.tsx:332,338,532`: optionally change `text-accent` to `text-system` explicitly. It already renders text-safe through `--text-color-accent`.
