# Track 15 — Token & Equity Frontend UI — Technical Specification

## Overview

Track 15 builds the complete frontend equity layer on top of a fully-functional backend (Track 09). The backend exposes 533-test-verified services for tokens, balances, exchange, payouts, treasury, and project gates. The frontend has zero equity UI today.

This spec defines every page, component, data flow, and interaction needed to expose ArdaNova's worker-equity mechanics to end users — investors funding projects, contributors earning equity for tasks, founders managing their projects, and admins overseeing the treasury.

---

## System Architecture Recap (Frontend Perspective)

All data flows through tRPC routers that act as thin proxies to the .NET API. No business logic lives in Next.js. Components call `api.<router>.<procedure>` via tRPC hooks; data arrives typed via existing tRPC routers.

### Available tRPC Routers

| Router | Key Procedures |
|---|---|
| `api.projectTokens` | `getConfigByProject`, `getGateStatus`, `getAllocations`, `getInvestors`, `evaluateGate`, `failProject` |
| `api.tokenBalances` | `getPortfolio`, `getBalance`, `getArdaBalance`, `checkLiquidity`, `getConversionPreview`, `getProjectTokenValue` |
| `api.exchange` | `getProjectTokenValue`, `getArdaValue`, `getConversionPreview`, `getTreasuryStatus` |
| `api.payouts` | `requestPayout`, `cancelPayout`, `getPayoutsByUser`, `getPendingPayouts` |
| `api.treasury` | `getStatus`, `getTransactions`, `processFundingInflow`, `rebalance`, `reconcile` |
| `api.projectGates` | gate status queries |

---

## Token Architecture (User-Facing Mental Model)

Users never see blockchain terminology. The UI translates the three-tier token system into plain USD values and equity percentages.

### What Users See vs What the System Does

```
WHAT THE USER SEES              WHAT THE SYSTEM DOES
─────────────────────────────────────────────────────────
"You own 2.5% of EcoWaste"  →  250 project tokens out of 10,000 total
"Current value: $125"        →  250 tokens × ($5,000 raised / 10,000 supply)
"Available to withdraw"      →  isLiquid=true (Gate 1 cleared, CONTRIBUTOR class)
"Locked until project exits" →  isLiquid=false (INVESTOR class, Gate 2 not reached)
"Payout: $125 → $0.10/ARDA → 1,250 ARDA → $125 USD"  →  full conversion chain
```

### Gate Status → UI State Mapping

| Gate Status | Badge Color | Funding Tab CTA | Contributor Tokens | Investor/Founder Tokens |
|---|---|---|---|---|
| `FUNDING` | `#00d4ff` (cyan) | "Back This Project" — Stripe checkout | Locked (project not started) | Locked |
| `ACTIVE` | `#00ff88` (green) | "Project Funded" — disabled | LIQUID — can withdraw | Locked |
| `SUCCEEDED` | `#00ff88` (green) | "Project Succeeded" — disabled | LIQUID | LIQUID |
| `FAILED` | `#ff0080` (pink) | "Project Failed" — disabled | LIQUID (kept) | Trust protection info |

### Holder Class Rules (Shown in UI)

| Class | Acquired By | Liquid After | Failure Outcome | UI Badge |
|---|---|---|---|---|
| CONTRIBUTOR | Completing tasks | Gate 1 | Kept — earned through labor | `#00ff88` — "Earned" |
| INVESTOR | Funding with USD via Stripe | Gate 2 | Partial refund (trust protection) | `#00d4ff` — "Invested" |
| FOUNDER | Project creator reserved allocation | Gate 2 | BURNED — founder eats the loss | `#ff0080` — "Founder" |

---

## UI Pages

### Page 1 — Project Funding Tab (P0)

**Route:** `/projects/[slug]` — new "Funding" tab alongside existing tabs (Overview, Updates, Team, etc.)

**Purpose:** Show funding progress, gate status, and enable investment via Stripe checkout.

**Wireframe:**

```
┌─────────────────────────────────────────────────────────────────┐
│  GATE STATUS BANNER                                              │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │  [FUNDING]  Gate 1: Raise $50,000 to activate project    │   │
│  │  ████████████████░░░░░░░░░░░░░  $32,000 / $50,000 (64%) │   │
│  └──────────────────────────────────────────────────────────┘   │
│                                                                  │
│  GATE TIMELINE                                                   │
│  ○ FUNDING  ──────  ○ ACTIVE  ──────  ○ SUCCEEDED               │
│  (current)                                                       │
│                                                                  │
│  EQUITY PREVIEW (for potential investor)                         │
│  ┌────────────────────────────────────────────────────────┐      │
│  │  Invest: [$____] USD                                   │      │
│  │  You receive: X.XX% equity  (N project tokens)        │      │
│  │  Current token value: $X.XX each                      │      │
│  │  Locked until: Gate 2 (project success)               │      │
│  │  [BACK THIS PROJECT →]  (electric cyan button)        │      │
│  └────────────────────────────────────────────────────────┘      │
│                                                                  │
│  CURRENT INVESTORS                                               │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │  Avatar  John D.    $2,500    0.05%   [INVESTOR]         │   │
│  │  Avatar  Maria S.   $5,000    0.10%   [INVESTOR]         │   │
│  └──────────────────────────────────────────────────────────┘   │
│                                                                  │
│  FOUNDER ALLOCATION                                              │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │  Avatar  Alex K. (Founder)  20%   [FOUNDER] Locked       │   │
│  └──────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────┘
```

**Data Flow:**

```
Component mounts
  → api.projectTokens.getConfigByProject({ projectId }) — token config + supply
  → api.projectTokens.getGateStatus({ configId })        — current gate state
  → api.projectTokens.getInvestors({ configId })         — investor list
  → api.exchange.getProjectTokenValue({ configId })      — $/token rate

User changes investment amount input
  → api.exchange.getConversionPreview({ configId, tokenAmount }) (debounced 400ms)
  → Display: equity%, token count, USD value, lock status

"Back This Project" clicked
  → Navigate to Investment Flow modal/page (Page 2)
```

**Components:**
- `GateStatusBanner` — progress bar + gate label + transition description
- `GateTimeline` — three-step stepper (FUNDING → ACTIVE → SUCCEEDED/FAILED)
- `EquityPreview` — investment amount input with live conversion
- `InvestorTable` — paginated list of investors with equity %
- `FounderAllocationRow` — founder equity display

---

### Page 2 — Investment Flow (P0)

**Route:** `/projects/[slug]/invest` or modal overlay

**Purpose:** Multi-step guided flow for investing via Stripe checkout.

**Wireframe — Step 1: Amount & Preview**

```
┌─────────────────────────────────────────────────────────────────┐
│  INVEST IN [PROJECT NAME]                                        │
│  Step 1 of 3: Choose Amount                                      │
│                                                                  │
│  Investment Amount                                               │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │  $ [_________]  USD                                      │   │
│  │                                                          │   │
│  │  Quick amounts: [$100] [$500] [$1,000] [$5,000]          │   │
│  └──────────────────────────────────────────────────────────┘   │
│                                                                  │
│  EQUITY BREAKDOWN                                                │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │  Project tokens you receive:      500 ECOW               │   │
│  │  Equity percentage:               0.05%                  │   │
│  │  Current token value:             $1.00 / token          │   │
│  │  Your position value today:       $500                   │   │
│  │  Liquidity:                       Locked until Gate 2    │   │
│  │  Trust protection:                50% if project fails   │   │
│  └──────────────────────────────────────────────────────────┘   │
│                                                                  │
│  RATE TRANSPARENCY                                               │
│  $500 → 500 ECOW → N ARDA → $500 USD (at current rates)         │
│                                                                  │
│  [CONTINUE →]                                                    │
└─────────────────────────────────────────────────────────────────┘
```

**Wireframe — Step 2: Review & Confirm**

```
┌─────────────────────────────────────────────────────────────────┐
│  Step 2 of 3: Review                                             │
│                                                                  │
│  You are investing $500 in EcoWaste Solutions                    │
│  You will receive 500 ECOW tokens (0.05% equity)                │
│  Tokens are locked until Gate 2 (project success)               │
│  If the project fails, you receive up to $250 back              │
│                                                                  │
│  [ ] I understand tokens are locked until project success        │
│  [ ] I understand the trust protection terms                     │
│                                                                  │
│  [BACK]  [PROCEED TO PAYMENT →]                                  │
└─────────────────────────────────────────────────────────────────┘
```

**Wireframe — Step 3: Stripe Redirect**

```
Redirect to Stripe Checkout (external).
On success: redirect to /projects/[slug]/invest/success
On cancel:  redirect to /projects/[slug]?tab=funding
```

**Wireframe — Success Screen**

```
┌─────────────────────────────────────────────────────────────────┐
│  INVESTMENT CONFIRMED                                            │
│  ✓  $500 invested in EcoWaste Solutions                          │
│  ✓  500 ECOW tokens added to your portfolio                      │
│  ✓  Tokens locked until Gate 2                                   │
│                                                                  │
│  [VIEW YOUR PORTFOLIO]  [BACK TO PROJECT]                        │
└─────────────────────────────────────────────────────────────────┘
```

**Data Flow:**

```
Step 1:
  → api.exchange.getConversionPreview (debounced on amount input)
  → Derive: tokenCount = usdAmount / tokenValueUsd
             equityPct  = tokenCount / totalSupply * 100

Step 2 (Confirm):
  → Checkbox validation

Step 3 (Payment):
  → Stripe Checkout Session URL (provided by backend on investment initiation)
  → Webhook handles payment_intent.succeeded → allocateToInvestor → creditAsync → evaluateGate1

Success:
  → api.projectTokens.getGateStatus (re-fetch to reflect possible Gate 1 trigger)
  → api.tokenBalances.getPortfolio (re-fetch user portfolio)
```

---

### Page 3 — User Portfolio Page (P0)

**Route:** `/portfolio` (new top-level route) or `/dashboard/portfolio`

**Purpose:** Complete equity overview for the logged-in user. Holdings by project, ARDA balance, USD value, liquidity status, payout history.

**Wireframe:**

```
┌─────────────────────────────────────────────────────────────────┐
│  MY EQUITY PORTFOLIO                                             │
│                                                                  │
│  TOTAL VALUE                                                     │
│  ┌────────────┬────────────┬────────────┬─────────────┐          │
│  │ Total USD  │ Liquid Now │ Locked     │ ARDA Balance│          │
│  │ $4,250     │ $1,750     │ $2,500     │ 12,500 ARDA │          │
│  └────────────┴────────────┴────────────┴─────────────┘          │
│                                                                  │
│  HOLDINGS                                  Filter: [All ▼]       │
│                                                                  │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │  ECOWASTE SOLUTIONS                    [ACTIVE] [FUNDED] │   │
│  │  250 ECOW  ·  2.5% equity  ·  $1,250                    │   │
│  │  [CONTRIBUTOR]  LIQUID  ·  [WITHDRAW]                    │   │
│  ├──────────────────────────────────────────────────────────┤   │
│  │  HEALTHTRACK                          [ACTIVE] [FUNDED] │   │
│  │  500 HLTH  ·  0.05% equity  ·  $500                     │   │
│  │  [INVESTOR]  LOCKED until Gate 2                         │   │
│  ├──────────────────────────────────────────────────────────┤   │
│  │  EDUCONNECT                            [SUCCEEDED]       │   │
│  │  100 EDUC  ·  0.01% equity  ·  $2,500                   │   │
│  │  [FOUNDER]  LIQUID  ·  [WITHDRAW]                        │   │
│  └──────────────────────────────────────────────────────────┘   │
│                                                                  │
│  PAYOUT HISTORY                                                  │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │  2026-02-15  EcoWaste  250 ECOW  →  $1,250  COMPLETED    │   │
│  │  2026-01-03  HealthTrack  50 HLTH  →  $50   CANCELLED    │   │
│  └──────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────┘
```

**Data Flow:**

```
Component mounts
  → api.tokenBalances.getPortfolio({ userId: session.user.id })
    Response contains: holdings[] with { projectName, tokenAmount, equityPct, usdValue, isLiquid, holderClass }
  → api.tokenBalances.getArdaBalance({ userId })
  → api.payouts.getPayoutsByUser({ userId })

"WITHDRAW" clicked on liquid holding
  → Navigate to Payout Request Flow (Page 4)
```

**Portfolio Item Component States:**
- Liquid + CONTRIBUTOR: green border, "WITHDRAW" button active
- Locked + INVESTOR: cyan border, "Locked until Gate 2" label, no action
- Locked + FOUNDER: pink border, "Locked until Gate 2" label, no action
- FAILED project: gray border, "Project Failed" badge, show trust protection amount if INVESTOR

---

### Page 4 — Payout Request Flow (P1)

**Route:** `/portfolio/withdraw` or modal from portfolio page

**Purpose:** Convert project tokens to USD and initiate Stripe Connect payout.

**Wireframe — Step 1: Conversion Preview**

```
┌─────────────────────────────────────────────────────────────────┐
│  WITHDRAW EQUITY                                                 │
│  From: EcoWaste Solutions  [CONTRIBUTOR]                         │
│                                                                  │
│  How many tokens to convert?                                     │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │  [_____] ECOW  of 250 available                          │   │
│  │  Quick: [25%] [50%] [75%] [100% — $1,250]               │   │
│  └──────────────────────────────────────────────────────────┘   │
│                                                                  │
│  CONVERSION CHAIN                                                │
│  250 ECOW  →  25,000 ARDA  →  $1,250 USD                        │
│  Token rate:   1 ECOW = $5.00                                    │
│  ARDA rate:    1 ARDA = $0.10                                    │
│  Platform fee: $0 (currently)                                    │
│  You receive:  $1,250                                            │
│                                                                  │
│  ⚠ Payout requires Stripe Connect account                        │
│  [Setup Payout Account] if not connected                         │
│                                                                  │
│  [SUBMIT PAYOUT REQUEST →]                                       │
└─────────────────────────────────────────────────────────────────┘
```

**Wireframe — Payout Status Tracking**

```
┌──────────────────────────────────────────────────────────────────┐
│  PAYOUT STATUS                                                    │
│                                                                   │
│  $1,250 from EcoWaste Solutions                                   │
│  Submitted: 2026-03-11 14:23                                      │
│                                                                   │
│  ○ PENDING  ──→  ○ PROCESSING  ──→  ○ COMPLETED                  │
│  (current)                                                        │
│                                                                   │
│  [CANCEL PAYOUT]  (only while PENDING)                            │
└──────────────────────────────────────────────────────────────────┘
```

**Data Flow:**

```
Amount input changes
  → api.exchange.getConversionPreview({ configId, tokenAmount })
  → Display full conversion chain: tokens → ARDA → USD

Submit clicked
  → api.payouts.requestPayout({
      userId, sourceProjectTokenConfigId, sourceTokenAmount, holderClass
    })
  → On success: show status tracker, invalidate portfolio query

Cancel clicked
  → api.payouts.cancelPayout({ payoutRequestId })
  → Invalidate portfolio + payout queries
```

---

### Page 5 — Task Equity Display (P1)

**Route:** Inline enhancement to existing task cards and task detail pages

**Purpose:** Show contributors exactly how much equity a task is worth before and after completion.

**Wireframe — Task Card Enhancement:**

```
┌──────────────────────────────────────────────────────────────────┐
│  TASK: Build user authentication                                  │
│  Status: [IN_PROGRESS]  Difficulty: MEDIUM                       │
│                                                                   │
│  ┌─────────────────────────────────────────────────────────┐     │
│  │  EQUITY REWARD                                          │     │
│  │  0.25% equity  ·  25 ECOW  ·  ≈ $125                   │     │
│  │  [CONTRIBUTOR] Liquid after Gate 1                      │     │
│  └─────────────────────────────────────────────────────────┘     │
└──────────────────────────────────────────────────────────────────┘
```

**Wireframe — Task Detail Equity Section:**

```
┌──────────────────────────────────────────────────────────────────┐
│  EQUITY DETAILS                                                   │
│  Allocation: 25 ECOW tokens  (0.25% of total project equity)     │
│  Current value: $125  (at $5.00/token)                           │
│  Liquidity: Available after Gate 1                               │
│  Status: [RESERVED] → [DISTRIBUTED on completion]               │
│                                                                   │
│  If you complete this task:                                       │
│  → 25 ECOW credited to your portfolio                            │
│  → Liquid immediately if Gate 1 already passed                   │
│  → Or liquid when Gate 1 clears                                  │
└──────────────────────────────────────────────────────────────────┘
```

**Data Flow:**

```
Task card renders
  → api.projectTokens.getAllocationsByTask({ taskId })
  → api.exchange.getProjectTokenValue({ configId })
  → Compute: usdValue = allocation.equityPercentage/100 * totalSupply * tokenValueUsd

Note: This adds a query per visible task card. Use React Query's staleTime and
deduplicate configId lookups via separate config query at task list level.
```

---

### Page 6 — Project Equity Dashboard (Owner View) (P1)

**Route:** New "Equity" tab on `/projects/[slug]` — visible only to project owner/admin

**Purpose:** Full equity management for project owners: supply breakdown, all allocations, gate management timeline.

**Wireframe:**

```
┌─────────────────────────────────────────────────────────────────┐
│  EQUITY DASHBOARD  [ECOWASTE SOLUTIONS]                          │
│                                                                  │
│  SUPPLY BREAKDOWN                                                │
│  ┌──────────────┬──────────────┬──────────────┬──────────────┐  │
│  │ Total Supply │ Contributor  │ Investor     │ Founder      │  │
│  │ 10,000 ECOW  │ 3,500 (35%) │ 4,500 (45%) │ 2,000 (20%) │  │
│  └──────────────┴──────────────┴──────────────┴──────────────┘  │
│                                                                  │
│  VISUAL BREAKDOWN (stacked bar)                                  │
│  [████████████████████░░░░░░░░░░░░░░░░░░░░░░░░░] 45% allocated   │
│   Contributor ██  Investor ░░  Founder ▒▒  Available ···        │
│                                                                  │
│  GATE MANAGEMENT                                                 │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │  Gate 1: CLEARED  ✓  ($50,000 raised on 2026-01-15)     │   │
│  │  Gate 2: PENDING  ○  [MARK PROJECT SUCCEEDED]            │   │
│  └──────────────────────────────────────────────────────────┘   │
│                                                                  │
│  ALLOCATION TABLE                                                │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │  Filter: [All ▼]  Sort: [Equity % ▼]                    │   │
│  ├──────────────────────────────────────────────────────────┤   │
│  │  Task: Build Auth       25 ECOW  0.25%  [DISTRIBUTED]    │   │
│  │  Task: Design UI        50 ECOW  0.50%  [RESERVED]       │   │
│  │  Investor: John D.    4,500 ECOW 45.0%  [DISTRIBUTED]    │   │
│  │  Founder: Alex K.    2,000 ECOW  20.0%  [DISTRIBUTED]    │   │
│  └──────────────────────────────────────────────────────────┘   │
│                                                                  │
│  DANGER ZONE                                                     │
│  [FAIL PROJECT]  (destructive — requires confirmation)           │
└─────────────────────────────────────────────────────────────────┘
```

**Data Flow:**

```
Component mounts
  → api.projectTokens.getConfigByProject({ projectId })  — config + supply
  → api.projectTokens.getSupply({ id: configId })        — breakdown by class
  → api.projectTokens.getGateStatus({ configId })        — gate state
  → api.projectTokens.getAllocations({ configId })        — full allocation table

"Mark Project Succeeded" clicked
  → Confirmation dialog
  → api.projectTokens.clearGate({ configId, verifiedByUserId: session.user.id })
  → Invalidate gate status, refetch portfolio for all investors

"Fail Project" clicked
  → Double-confirmation dialog with typed confirmation ("FAIL")
  → api.projectTokens.failProject({ configId, reason })
  → Backend handles: burn founder tokens, trigger trust protection, update state
```

---

### Page 7 — Treasury Dashboard (Admin) (P2)

**Route:** `/admin/treasury`

**Purpose:** Admin-only view of the three-bucket treasury, total platform reserves, and full transaction audit log.

**Wireframe:**

```
┌─────────────────────────────────────────────────────────────────┐
│  PLATFORM TREASURY                     Admin                     │
│                                                                  │
│  THREE-BUCKET VISUALIZATION                                      │
│  ┌─────────────────────────────────────────────────────────┐    │
│  │  Index Fund       Liquid Reserve    Operations           │    │
│  │  $550,000 (55%)   $300,000 (30%)    $150,000 (15%)       │    │
│  │  ████████████     ███████           ████                 │    │
│  │  [neon-cyan]      [neon-green]      [neon-pink]          │    │
│  └─────────────────────────────────────────────────────────┘    │
│                                                                  │
│  TOTAL: $1,000,000  ARDA VALUE: $0.10/ARDA  SUPPLY: 10M ARDA   │
│                                                                  │
│  ADMIN ACTIONS                                                   │
│  [APPLY INDEX RETURN]  [REBALANCE]  [RECONCILE]                  │
│                                                                  │
│  TRANSACTION AUDIT LOG                                           │
│  ┌────────────────────────────────────────────────────────┐     │
│  │  Filter by type: [All ▼]    Date range: [─────────]    │     │
│  ├────────────────────────────────────────────────────────┤     │
│  │  2026-03-11  FUNDING_INFLOW    +$5,000   EcoWaste       │    │
│  │  2026-03-10  PAYOUT_DEBIT      -$1,250   John D.        │    │
│  │  2026-03-09  REBALANCE         $500 idx→liquid          │    │
│  │  2026-03-08  INDEX_RETURN      +$6,800   monthly        │    │
│  │  2026-03-07  TRUST_PROTECTION  -$250     HealthTrack     │    │
│  │  2026-03-06  FOUNDER_BURN      0         EduConnect     │    │
│  └────────────────────────────────────────────────────────┘     │
└─────────────────────────────────────────────────────────────────┘
```

**Data Flow:**

```
Component mounts (admin guard via session.user.role check)
  → api.treasury.getStatus()
  → api.treasury.getTransactions({ limit: 100 })

"Apply Index Return" clicked
  → api.treasury.applyIndexReturn()
  → Refetch status + transactions

"Rebalance" clicked
  → Input dialog: required liquid amount
  → api.treasury.rebalance({ requiredLiquid })

"Reconcile" clicked
  → api.treasury.reconcile()
  → Refetch all
```

---

### Page 8 — Stripe Connect Onboarding (P2)

**Route:** `/settings/payouts` (integrated into existing settings)

**Purpose:** Allow users to connect a Stripe account so they can receive USD payouts.

**Wireframe:**

```
┌─────────────────────────────────────────────────────────────────┐
│  PAYOUT ACCOUNT                                                  │
│                                                                  │
│  [NOT CONNECTED]                                                 │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │  Connect a bank account to receive your equity payouts   │   │
│  │  Powered by Stripe Connect.                              │   │
│  │                                                          │   │
│  │  [CONNECT PAYOUT ACCOUNT →]  (electric cyan button)     │   │
│  └──────────────────────────────────────────────────────────┘   │
│                                                                  │
│  [CONNECTED]  (after onboarding)                                 │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │  ✓ Bank account connected                                │   │
│  │  Account: ****1234  (last 4 from Stripe)                 │   │
│  │  Status: ACTIVE                                          │   │
│  │  [UPDATE ACCOUNT]                                        │   │
│  └──────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────┘
```

**Data Flow:**

```
"Connect Payout Account" clicked
  → POST /api/stripe/connect-account (Next.js API route)
    → Calls backend: StripeService.CreateConnectedAccountAsync(userId, email)
    → Returns: { onboardingUrl }
  → Redirect to Stripe Connect onboarding (external)
  → Return URL: /settings/payouts?connected=true
  → Show success state + refresh account status
```

---

## Design System Application

All components use the existing Swiss Brutalism design system already present in the codebase.

### Color Semantics for Equity UI

| Token | Hex | Usage in Equity UI |
|---|---|---|
| `text-primary` / `text-neon-cyan` | `#00d4ff` | Investor class badge, primary CTAs, locked token borders, ARDA values |
| `text-neon-green` | `#00ff88` | Contributor class badge, liquid status, Gate 1/2 success, USD values |
| `text-neon-pink` | `#ff0080` | Founder class badge, failure states, burn events, warning borders |
| `text-foreground` | `#e8eaed` | All numeric values, token amounts |
| `bg-card` / `border-border` | — | Container cards |

### Liquidity Status Indicator Pattern

```tsx
// Liquid — green glow border
<div className="border-2 border-neon-green p-4">
  <span className="text-neon-green text-xs font-mono">LIQUID</span>
</div>

// Locked — cyan border, subdued
<div className="border-2 border-primary/40 p-4">
  <span className="text-muted-foreground text-xs font-mono">LOCKED</span>
</div>

// Failed/Burned — pink border
<div className="border-2 border-neon-pink p-4">
  <span className="text-neon-pink text-xs font-mono">BURNED</span>
</div>
```

### Progress Bar — Funding Goal

Use the existing `<Progress variant="neon" />` component. The project detail page already renders a funding progress bar from `project.currentFunding / project.fundingGoal`. The Funding tab will augment this with gate status context.

### Numeric Format Convention

- USD values: `$1,250.00` — always 2 decimal places, dollar sign prefix
- Token amounts: `1,250 ECOW` — integer, ticker suffix
- Equity percentages: `0.25%` — 2 decimal places minimum, `%` suffix
- ARDA amounts: `12,500 ARDA` — integer display

---

## Component File Structure

```
ardanova-client/src/
├── app/
│   ├── projects/[slug]/
│   │   └── (existing page — add "Funding" and "Equity" tabs)
│   ├── portfolio/
│   │   ├── page.tsx                    — User Portfolio Page (P0)
│   │   └── withdraw/
│   │       └── page.tsx                — Payout Request Flow (P1)
│   ├── projects/[slug]/invest/
│   │   ├── page.tsx                    — Investment Flow (P0)
│   │   └── success/
│   │       └── page.tsx                — Post-payment success screen
│   └── admin/
│       └── treasury/
│           └── page.tsx                — Treasury Dashboard (P2)
├── components/
│   ├── equity/
│   │   ├── gate-status-banner.tsx      — FUNDING/ACTIVE/SUCCEEDED/FAILED banner
│   │   ├── gate-timeline.tsx           — Three-step stepper
│   │   ├── equity-preview.tsx          — Investment amount → equity calculation
│   │   ├── investor-table.tsx          — Investor list with equity %
│   │   ├── founder-allocation.tsx      — Founder equity row
│   │   ├── portfolio-holding-card.tsx  — Per-project holding card
│   │   ├── payout-status-tracker.tsx   — PENDING/PROCESSING/COMPLETED stepper
│   │   ├── conversion-chain.tsx        — tokens → ARDA → USD visual
│   │   ├── supply-breakdown-bar.tsx    — Stacked bar (contributor/investor/founder)
│   │   ├── allocation-table.tsx        — All allocations with filter/sort
│   │   ├── treasury-buckets.tsx        — Three-bucket visualization (admin)
│   │   ├── treasury-transaction-log.tsx — Audit log table (admin)
│   │   └── task-equity-badge.tsx       — Inline equity display on task cards
│   └── projects/
│       ├── funding-tab.tsx             — New tab content for project detail
│       └── equity-tab.tsx             — New owner-only tab content
```

---

## Error States & Edge Cases

### No Token Config Yet

If `getConfigByProject` returns 404, the Funding tab shows:
```
"Funding not yet configured for this project."
Only visible to project owner: [CONFIGURE FUNDING]
```

### Gate 1 Not Yet Met

Investment CTA is active. Contributor token liquidity badge shows "Locked — awaiting Gate 1".

### Project Failed

- Portfolio cards show `FAILED` badge in pink
- CONTRIBUTOR holdings: green border, "LIQUID — your labor is protected"
- INVESTOR holdings: shows trust protection amount ("You receive $250 protection payout")
- FOUNDER holdings: shows `BURNED` in pink with $0 value

### Payout Blocked by Gate

If `requestPayout` fails due to gate (INVESTOR before Gate 2):
```
"Your investment tokens are locked until this project reaches Gate 2 (success).
You cannot withdraw until the project owner marks it as succeeded."
```

### Stripe Not Connected

`WITHDRAW` button on portfolio shows tooltip: "Connect a payout account first" and links to `/settings/payouts`.

---

## Accessibility & Performance Notes

- All equity values must have `aria-label` with plain-language descriptions (e.g., "25 EcoWaste tokens, worth approximately 125 US dollars, available to withdraw")
- Conversion preview queries are debounced 400ms to avoid hammering the API on keystroke
- Portfolio page uses `staleTime: 30_000` — balances don't change second-by-second
- Treasury dashboard requires `session.user.role === "ADMIN"` check in component AND middleware
- `getConversionPreview` is called optimistically on amount change; show skeleton state during fetch
