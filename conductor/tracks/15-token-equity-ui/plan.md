# Track 15 — Token & Equity Frontend UI

## Prerequisites (all complete from Track 09)
- Backend services: ProjectTokenService, TokenBalanceService, ExchangeService, PayoutService, TreasuryService, ProjectGateService, StripeService
- tRPC routers: `projectTokens`, `tokenBalances`, `payouts`, `treasury`, `projectGates`, `exchange`
- API client endpoints registered in `ArdaNovaApiClient`

---

## 1. Shared Equity Components [P0]

- [ ] **[P0] `components/equity/gate-status-banner.tsx`**
  - Props: `gateStatus: "FUNDING" | "ACTIVE" | "SUCCEEDED" | "FAILED"`, `fundingRaised: number`, `fundingGoal: number`
  - Renders: color-coded banner, funding progress bar (reuse `<Progress variant="neon" />`), gate label
  - Colors: FUNDING=cyan, ACTIVE/SUCCEEDED=green, FAILED=pink
  - Gate transition description text per status

- [ ] **[P0] `components/equity/gate-timeline.tsx`**
  - Props: `currentGate: ProjectGateStatus`
  - Renders: three-step stepper (FUNDING → ACTIVE → SUCCEEDED, with FAILED branch)
  - Active step highlighted with primary color, completed steps with green checkmark

- [ ] **[P0] `components/equity/equity-preview.tsx`**
  - Props: `configId: string`, `totalSupply: number`, `fundingRaised: number`
  - Internal state: `usdAmount: number`
  - Calls `api.exchange.getConversionPreview` debounced 400ms on amount change
  - Displays: token count, equity%, current token value, lock status description
  - Quick amount buttons: $100, $500, $1,000, $5,000

- [ ] **[P0] `components/equity/conversion-chain.tsx`**
  - Props: `tokenAmount: number`, `tokenTicker: string`, `ardaAmount: number`, `usdAmount: number`
  - Renders: `N TOKEN → M ARDA → $USD` with rate labels beneath each arrow
  - Visual chain in mono font, neon-cyan accents on arrows

- [ ] **[P0] `components/equity/liquidity-badge.tsx`**
  - Props: `isLiquid: boolean`, `holderClass: TokenHolderClass`, `gateStatus: ProjectGateStatus`
  - Renders: `LIQUID` (green) or `LOCKED until Gate N` (cyan muted) or `BURNED` (pink)
  - Includes tooltip explaining why tokens are locked/liquid

- [ ] **[P0] `components/equity/holder-class-badge.tsx`**
  - Props: `holderClass: "CONTRIBUTOR" | "INVESTOR" | "FOUNDER"`
  - CONTRIBUTOR = neon-green badge
  - INVESTOR = neon-cyan badge
  - FOUNDER = neon-pink badge

---

## 2. Project Funding Tab [P0]

- [ ] **[P0] `components/projects/funding-tab.tsx`**
  - Queries:
    - `api.projectTokens.getConfigByProject({ projectId })`
    - `api.projectTokens.getGateStatus({ configId })`
    - `api.projectTokens.getInvestors({ configId })`
    - `api.exchange.getProjectTokenValue({ configId })`
  - Renders:
    - `<GateStatusBanner />` at top
    - `<GateTimeline />` below banner
    - `<EquityPreview />` for investment amount input
    - "Back This Project" button (→ `/projects/[slug]/invest`) enabled only during FUNDING status
    - Investor table: avatar, name, USD invested, equity %, holder class badge
    - Founder allocation row

- [ ] **[P0] Add "Funding" tab to `/projects/[slug]/page.tsx`**
  - Add `{ id: "funding", label: "Funding", icon: TrendingUp }` to `tabs` array
  - Add `{activeTab === "funding" && <FundingTab projectId={project.id} />}` to tab content
  - No role restriction — all users can see funding tab

- [ ] **[P0] Add "Equity" tab to `/projects/[slug]/page.tsx` (owner-only)**
  - Add `{ id: "equity", label: "Equity", icon: BarChart2 }` — only push to `tabs` if `isOwner`
  - Add `{activeTab === "equity" && isOwner && <EquityDashboardTab projectId={project.id} />}`

---

## 3. Investment Flow [P0]

- [ ] **[P0] `app/projects/[slug]/invest/page.tsx`** — multi-step investment flow
  - Step 1: Amount selection + `<EquityPreview />` + `<ConversionChain />`
  - Step 2: Review + two checkboxes (lock acknowledgment + trust protection acknowledgment)
  - Step 3: Redirect to Stripe Checkout URL
  - Route guard: must be authenticated, project must be in FUNDING gate

- [ ] **[P0] `app/projects/[slug]/invest/success/page.tsx`** — post-payment confirmation
  - Show confirmed investment details (from URL params or session storage)
  - "View Your Portfolio" → `/portfolio`
  - "Back to Project" → `/projects/[slug]`
  - Invalidates `api.tokenBalances.getPortfolio` on mount

- [ ] **[P0] Stripe checkout initiation**
  - Investment form submit calls backend endpoint to create Stripe Checkout Session
  - Add `api/stripe/checkout` Next.js API route in `app/api/stripe/checkout/route.ts`
  - POST body: `{ projectTokenConfigId, userId, usdAmount }`
  - Calls .NET API `POST /api/stripe/checkout-session`
  - Returns `{ sessionUrl }` → `router.push(sessionUrl)`

---

## 4. User Portfolio Page [P0]

- [ ] **[P0] `app/portfolio/page.tsx`**
  - Queries:
    - `api.tokenBalances.getPortfolio({ userId: session.user.id })`
    - `api.tokenBalances.getArdaBalance({ userId })`
    - `api.payouts.getPayoutsByUser({ userId })`
  - Renders summary stats row: total USD value, liquid USD, locked USD, ARDA balance
  - Filter dropdown: All / Liquid / Locked / By project
  - Holdings list: `<PortfolioHoldingCard />` per position
  - Payout history table

- [ ] **[P0] `components/equity/portfolio-holding-card.tsx`**
  - Props: portfolio holding item from `UserPortfolioDto`
  - Renders: project name, token amount + ticker, equity%, USD value, liquidity badge, holder class badge
  - Liquid + has payout account: shows "WITHDRAW" button → `/portfolio/withdraw?configId=...&class=...`
  - Liquid + no payout account: shows "Connect Account" link
  - Locked: shows lock reason
  - Failed project: special failure state layout

- [ ] **[P0] Add `/portfolio` to navigation**
  - Add "Portfolio" link to the sidebar/nav with `Wallet` icon from lucide-react
  - Badge with liquid USD value if > $0

---

## 5. Payout Request Flow [P1]

- [ ] **[P1] `app/portfolio/withdraw/page.tsx`**
  - URL params: `configId`, `holderClass`
  - Step 1: Token amount input + `<ConversionChain />` live preview
    - `api.exchange.getConversionPreview` debounced on amount change
    - Quick percentage buttons: 25%, 50%, 75%, 100%
    - Check for Stripe Connect account; show warning if not connected
  - Step 2: Confirm and submit
    - `api.payouts.requestPayout({ userId, sourceProjectTokenConfigId, sourceTokenAmount, holderClass })`
    - On success: redirect to payout status view

- [ ] **[P1] `components/equity/payout-status-tracker.tsx`**
  - Props: `payout: PayoutRequestDto`
  - Renders three-step stepper: PENDING → PROCESSING → COMPLETED (or FAILED/CANCELLED)
  - "Cancel" button visible only if status === PENDING
    - `api.payouts.cancelPayout({ payoutRequestId })`
  - Shows amounts, submitted timestamp, completion timestamp if done

- [ ] **[P1] Add payout history section to Portfolio page**
  - Table: date, project, amount (tokens → USD), status badge, action (view/cancel)
  - Status badge variants: PENDING=cyan, PROCESSING=yellow/warning, COMPLETED=green, FAILED/CANCELLED=pink

---

## 6. Task Equity Display [P1]

- [ ] **[P1] `components/equity/task-equity-badge.tsx`**
  - Props: `taskId: string`, `projectTokenConfigId: string`
  - Queries:
    - `api.projectTokens.getAllocationsByTask({ taskId })` — get allocation
    - `api.exchange.getProjectTokenValue({ configId })` — get token value
  - Renders compact badge: `0.25% · 25 ECOW · ≈$125`
  - Uses `[CONTRIBUTOR]` holder class badge in mini size
  - If no allocation: renders nothing (null)
  - `staleTime: 60_000` — token values don't change per second

- [ ] **[P1] Inject `<TaskEquityBadge />` into existing task cards**
  - Locate task card component(s) in `components/`
  - Add badge below task title/description
  - Pass `taskId` and `projectTokenConfigId` (get configId from project context)

- [ ] **[P1] Task detail equity section**
  - On task detail page/modal, add "Equity Reward" section below task metadata
  - Show full breakdown: allocation status (RESERVED/DISTRIBUTED), tokens, equity%, current value, liquidity rule

---

## 7. Project Equity Dashboard (Owner View) [P1]

- [ ] **[P1] `components/projects/equity-tab.tsx`**
  - Queries:
    - `api.projectTokens.getConfigByProject({ projectId })`
    - `api.projectTokens.getSupply({ id: configId })`
    - `api.projectTokens.getGateStatus({ configId })`
    - `api.projectTokens.getAllocations({ configId })`
  - Renders supply breakdown stat cards (total, contributor, investor, founder supply)
  - `<SupplyBreakdownBar />` stacked visualization
  - Gate management section with gate action buttons
  - `<AllocationTable />` with filter and sort
  - Danger zone: "Fail Project" with double-confirmation

- [ ] **[P1] `components/equity/supply-breakdown-bar.tsx`**
  - Props: `contributorSupply`, `investorSupply`, `founderSupply`, `totalSupply`, `burnedSupply`
  - Stacked horizontal bar: contributor (green) / investor (cyan) / founder (pink) / unallocated (muted)
  - Legend beneath with exact counts and percentages

- [ ] **[P1] `components/equity/allocation-table.tsx`**
  - Props: `allocations: TokenAllocationDto[]`
  - Columns: type (task/investor/founder), recipient, tokens, equity%, status badge
  - Filter by holder class dropdown
  - Sort by equity% or status
  - Status badges: RESERVED=muted, DISTRIBUTED=green, REVOKED=pink, BURNED=pink strikethrough

- [ ] **[P1] Gate action buttons in equity tab**
  - "Mark Project Succeeded" (Gate 2):
    - Only shown when gateStatus === ACTIVE
    - Confirmation dialog explaining effects (all investor + founder tokens unlock)
    - Calls `api.projectTokens.clearGate({ configId, verifiedByUserId })`
  - "Fail Project" (destructive):
    - Only shown when gateStatus === ACTIVE
    - Double confirmation: type "FAIL" to confirm
    - Calls `api.projectTokens.failProject({ configId, reason })`
    - Shows consequences: founder tokens burn, investor trust protection triggers

---

## 8. Treasury Dashboard (Admin) [P2]

- [ ] **[P2] `app/admin/treasury/page.tsx`**
  - Admin role guard: `if (session.user.role !== "ADMIN") redirect("/dashboard")`
  - Queries:
    - `api.treasury.getStatus()`
    - `api.treasury.getTransactions({ limit: 100 })`
  - Renders `<TreasuryBuckets />` + total/ARDA stats + action buttons + `<TreasuryTransactionLog />`

- [ ] **[P2] `components/equity/treasury-buckets.tsx`**
  - Props: `indexFund: number`, `liquidReserve: number`, `operations: number`
  - Three-column layout with distinct colors: cyan (index), green (liquid), pink (operations)
  - Stacked bar + percentages + absolute USD values
  - ARDA value derived: `totalTreasury / ardaCirculatingSupply`

- [ ] **[P2] `components/equity/treasury-transaction-log.tsx`**
  - Props: `transactions: TreasuryTransactionDto[]`
  - Columns: timestamp, type, amount, project/user context, bucket affected
  - Type color coding: FUNDING_INFLOW=green, PAYOUT_DEBIT=pink, REBALANCE=cyan, TRUST_PROTECTION=yellow, FOUNDER_BURN=pink
  - Filter by transaction type dropdown
  - Date range filter

- [ ] **[P2] Admin action buttons**
  - "Apply Index Return" → `api.treasury.applyIndexReturn()` → success toast + refetch
  - "Rebalance" → amount input dialog → `api.treasury.rebalance({ requiredLiquid })` → refetch
  - "Reconcile" → confirmation → `api.treasury.reconcile()` → refetch

- [ ] **[P2] Add "Treasury" to admin navigation**
  - Add link in `/admin/layout.tsx` or admin sidebar

---

## 9. Stripe Connect Onboarding [P2]

- [ ] **[P2] `app/settings/payouts/page.tsx`** (or section within `/settings`)
  - Query user's connected Stripe account status from backend
  - Not connected state: explain payouts, show "Connect Payout Account" button
  - Connected state: show masked account info, status badge, "Update Account" link
  - Returns URL handler: `?connected=true` param shows success confirmation

- [ ] **[P2] `app/api/stripe/connect-account/route.ts`** — Next.js API route
  - POST: calls .NET API `POST /api/stripe/connected-account` with userId + email
  - Returns `{ onboardingUrl }`
  - Client redirects to Stripe Connect onboarding

- [ ] **[P2] "Connect payout account" gate in payout flow**
  - In `app/portfolio/withdraw/page.tsx`: check if user has connected account
  - If not: show inline warning with link to `/settings/payouts`
  - Disable submit button until account is connected

---

## 10. Navigation & Routing [P0/P1]

- [ ] **[P0] Add "Portfolio" to main navigation**
  - Link: `/portfolio`
  - Icon: `Wallet` from lucide-react
  - Position: after Dashboard, before Tasks (or in user section)

- [ ] **[P1] Add "Treasury" to admin navigation**
  - Link: `/admin/treasury`
  - Icon: `Landmark` from lucide-react
  - Only visible to ADMIN role

- [ ] **[P2] Add "Payouts" to settings navigation**
  - Link: `/settings/payouts`
  - Icon: `CreditCard` from lucide-react

---

## 11. Error Handling & Loading States [P0]

- [ ] **[P0] Loading skeletons for all equity components**
  - `GateStatusBanner`: skeleton progress bar + badge placeholder
  - `PortfolioHoldingCard`: skeleton card with shimmer
  - `AllocationTable`: skeleton rows
  - Use existing pattern from task/project components

- [ ] **[P0] Error boundary for equity data**
  - If `getConfigByProject` fails: "Funding not configured" empty state
  - If `getPortfolio` fails: "Unable to load portfolio" with retry button
  - All tRPC errors surface as toast notifications (use existing toast system)

- [ ] **[P1] Gate-locked payout error messaging**
  - Catch TRPC `BAD_REQUEST` from `requestPayout`
  - Parse error message from backend (gate-specific rejection text)
  - Display in red bordered box above submit button, not just a toast

---

## Acceptance Criteria

### P0 — Must ship together
- [ ] Funding tab visible on all project detail pages
- [ ] Investment flow completes: amount → preview → Stripe → success screen
- [ ] Portfolio page loads user's holdings with correct USD values and liquidity status
- [ ] Portfolio page shows payout history

### P1 — Second milestone
- [ ] Payout request flow submits and shows status tracker
- [ ] Task equity badges appear on task cards with correct values
- [ ] Equity dashboard tab visible to project owners with supply breakdown and gate actions
- [ ] "Mark Succeeded" and "Fail Project" flows work end-to-end

### P2 — Third milestone
- [ ] Treasury dashboard visible to admins with three-bucket visualization
- [ ] Admin actions (apply return, rebalance, reconcile) work
- [ ] Stripe Connect onboarding page functional
- [ ] Payout flow gates on Stripe account being connected
