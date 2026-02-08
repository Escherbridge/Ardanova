# Tokenomics & Project Equity — Technical Specification

## Overview

ArdaNova's tokenomics system enables worker-owned project equity through a three-tier token architecture with **gated liquidity** — a mechanism that controls when different classes of token holders can convert their equity to cash, based on project lifecycle milestones.

1. **Project Tokens** — Per-project fungible ASAs representing equity shares
2. **ARDA Token** — Platform-wide fungible ASA serving as the universal exchange medium
3. **USDCa** — Algorand-native stablecoin for fiat-convertible payouts

Users fund projects with USD (via Stripe). Contributors earn equity (project tokens) for completed tasks. Investors acquire equity by funding projects. Founders receive reserved allocations. Each holder class has different liquidity rights governed by two project gates. All blockchain operations are abstracted from users — they interact only with USD values and equity percentages.

## Design Principles

- **USD is source of truth** — all values denominated in USD; blockchain is the accountability layer
- **Custodial model** — platform manages all Algorand wallets and transactions on behalf of users
- **Deterministic exchange rates** — treasury-backed, not market-based; no AMM complexity
- **Off-chain first** — balances tracked in PostgreSQL for fast queries; Algorand provides audit trail
- **Graceful degradation** — if Algorand is unavailable, token operations continue off-chain
- **Equity cap enforcement** — total equity allocations can never exceed 100% per project
- **Gated liquidity** — token liquidity is unlocked progressively through project lifecycle gates
- **Holder class fairness** — contributors keep earned equity on failure; founders bear the loss; investors receive partial protection
- **Three-bucket treasury** — USD reserves split into index fund, liquid reserve, and operations for diversified risk management

---

## Token Architecture

### Three-Tier Token Flow (with Gated Liquidity)

```
USD (Stripe)                                                    USD (Stripe Connect)
    │                                                                ▲
    ▼                                                                │
┌──────────────────┐                                    ┌──────────────────┐
│  PROJECT FUNDING  │                                    │   FIAT PAYOUT    │
│  (Crowdfunding)   │                                    │   (Withdrawal)   │
└────────┬─────────┘                                    └────────┬─────────┘
         │                                                       ▲
         ▼                                                       │
┌──────────────────┐                                             │
│  GATE 1           │  Funding Goal Met                          │
│  FUNDING → ACTIVE │  Contributor tokens become liquid           │
└────────┬─────────┘                                             │
         │                                                       │
         ▼                                                       │
┌──────────────────┐    Task Completion    ┌──────────────────┐  │
│  PROJECT TOKEN    │───────────────────>│  TOKEN BALANCES     │  │
│  (Per-project ASA)│   (equity % award)  │                    │  │
│                   │                      │  CONTRIBUTOR ──────┤  │ (liquid after Gate 1)
│  Fixed supply     │   Investment         │  INVESTOR ─────X  │  │ (locked until Gate 2)
│  = 100% equity    │───────────────────>│  FOUNDER ──────X  │  │ (locked until Gate 2)
└──────────────────┘                      └────────┬───────────┘  │
                                                    │              │
┌──────────────────┐                      Convert   │              │
│  GATE 2           │                               ▼              │
│  ACTIVE→SUCCEEDED │                      ┌──────────────────┐    │
│  All tokens liquid│                      │   ARDA TOKEN      │    │
└──────────────────┘                      │  (Platform ASA)   │────┘
                                          │                    │  Convert to USD
         ┌──────────┐                     │  Treasury-backed   │  via USDCa
         │  FAILED   │                    │  exchange rate     │
         │  Founder  │                    └──────────────────┘
         │  tokens   │
         │  BURN     │                    ┌──────────────────┐
         │  Investor │                    │  THREE-BUCKET     │
         │  trust    │───────────────────>│  TREASURY         │
         │  protect  │  partial refund    │                    │
         └──────────┘                     │  55% Index Fund   │
                                          │  30% Liquid Reserve│
                                          │  15% Operations    │
                                          └──────────────────┘
```

### Three Holder Classes

Every project token allocation belongs to exactly one holder class, each with different liquidity rights.

| Class | How Tokens Are Acquired | When Tokens Become Liquid | Fiat Exit | On Project Failure |
|-------|------------------------|--------------------------|-----------|-------------------|
| **CONTRIBUTOR** | Earned by completing project tasks | After Gate 1 (funding goal met) | Yes — immediate after Gate 1 | Keep tokens. Labor was real. |
| **INVESTOR** | Purchased by funding the project with USD | After Gate 2 (project success) | Yes — only after Gate 2 | Partial protection via trust mechanism |
| **FOUNDER** | Reserved allocation as project creator | After Gate 2 (project success), unlocks last | Yes — only after Gate 2 | Tokens BURN. Founder eats the loss. |

### Two Project Gates

Projects progress through a lifecycle with two gates that control token liquidity.

**Gate 1 — Funding Goal Met:**
- Trigger: `project.fundingRaised >= project.fundingGoal`
- Effect: `ProjectGateStatus` transitions `FUNDING → ACTIVE`
- Unlocks: Task creation enabled. Contributor tokens become liquid (can convert or exit to fiat).
- Still locked: Investor tokens, Founder tokens.
- Treasury effect: Crowdfunded USD enters treasury immediately, split across three buckets.

**Gate 2 — Project Success:**
- Trigger: Project owner marks milestones complete AND admin approval (MVP) or community verification (post-MVP)
- Effect: `ProjectGateStatus` transitions `ACTIVE → SUCCEEDED`
- Unlocks: Investor tokens become liquid. Founder tokens become liquid (last).
- Treasury effect: Locked investor capital has been compounding in the index fund.

**Project Failure (after Gate 1):**
- Trigger: Project owner abandons OR community vote OR admin determination
- Effect: `ProjectGateStatus` transitions `ACTIVE → FAILED`
- Founder tokens: **BURN** (reduce total supply, `AllocationStatus = BURNED`)
- Investor tokens: Trust protection activates — investors receive partial refund from index fund at `trustProtectionRate` (default 50%)
- Contributor tokens: **Unaffected.** They earned these through labor. Tokens remain liquid and tradeable.

### Project Token (Per-Project ASA)

Each project creates a fixed-supply fungible ASA representing 100% ownership equity.

```
ASA Parameters:
  total: {totalSupply}             # e.g., 10,000 = 100% equity
  decimals: 0                      # Whole shares only
  defaultFrozen: true              # Platform-controlled transfers
  unitName: "{PSHARE}"             # Max 8 chars, project-specific
  assetName: "{ProjectName} Shares"
  manager: platformAddress         # Platform manages
  reserve: platformAddress
  freeze: platformAddress          # Platform controls transfers
  clawback: platformAddress        # Platform can revoke shares
```

**Why `defaultFrozen: true`?** The platform controls all transfers. Users cannot trade shares peer-to-peer — all conversions go through the platform's exchange service. This prevents unauthorized secondary markets and ensures regulatory compliance.

### ARDA Token (Platform ASA)

Single platform-wide fungible ASA that bridges all project tokens to stablecoins.

```
ASA Parameters:
  total: {ARDA_TOTAL_SUPPLY}       # e.g., 1,000,000,000
  decimals: 6                      # Micro-ARDA precision
  defaultFrozen: true              # Platform-controlled
  unitName: "ARDA"
  assetName: "ArdaNova Platform Token"
  manager: platformAddress
  reserve: platformAddress
  freeze: platformAddress
  clawback: platformAddress
```

**Value determination:** `1 ARDA = total_treasury / arda_circulating_supply`

Where `total_treasury = indexFundBalance + liquidReserveBalance + operationsBalance`

The ARDA token's value is deterministic — backed by the platform's actual USD reserves across all three treasury buckets. As more USD flows into the platform (crowdfunding), the treasury grows, backing the ARDA supply. Index fund returns compound over time, growing the backing.

---

## Exchange Rate Model

### Deterministic, Treasury-Backed Rates

All exchange rates are calculated, not market-determined. This is the **simplest and safest** approach.

| Conversion | Formula | Example |
|-----------|---------|---------|
| Project Token → USD | `project_funding_raised / project_total_supply` | $50,000 / 10,000 = $5/token |
| ARDA → USD | `total_treasury / arda_circulating_supply` | $1,000,000 / 10,000,000 = $0.10/ARDA |
| Project Token → ARDA | `(project_token_value / arda_value) * amount` | ($5 / $0.10) * 100 = 5,000 ARDA |

**Gate-aware value adjustments:**

| Gate Status | Contributor Token Value | Investor Token Value | Founder Token Value |
|------------|------------------------|---------------------|-------------------|
| FUNDING | `fundingRaised / totalSupply` (partial) | `fundingRaised / totalSupply` (locked) | `fundingRaised / totalSupply` (locked) |
| ACTIVE | `fundingRaised / totalSupply` (liquid) | `fundingRaised / totalSupply` (locked) | `fundingRaised / totalSupply` (locked) |
| SUCCEEDED | `fundingRaised / totalSupply` (liquid) | `fundingRaised / totalSupply` (liquid) | `fundingRaised / totalSupply` (liquid) |
| FAILED | `fundingRaised / totalSupply` (liquid) | Trust protection value | **$0** (burned) |

### Treasury Allocation Model

USD inflows are split across three buckets:

```
Treasury Allocation:
  indexFundBalance      — 55% of inflows. Invested in S&P 500 index fund. Compounds over time.
  liquidReserveBalance  — 30% of inflows. Funds immediate contributor payouts and failure refunds.
  operationsBalance     — 15% of inflows. Platform operating costs / retained earnings.
```

**Index fund returns:** Monthly compounding at configurable annual rate (default 8%). Platform takes a profit share (default 25%) of index returns.

**Auto-rebalance rule:** When `liquidReserveBalance` is insufficient to cover a payout, pull the deficit from `indexFundBalance`. This is logged as a `REBALANCE` treasury transaction.

**ARDA value formula:**
```
1 ARDA = (indexFundBalance + liquidReserveBalance + operationsBalance) / ardaCirculatingSupply
```

### Conversion Flow — Gate-Enforced Payout

```
Contributor                 Platform                    Algorand           Stripe
   │                           │                           │                  │
   │  Request Payout           │                           │                  │
   │  (100 project tokens)     │                           │                  │
   │──────────────────────────>│                           │                  │
   │                           │                           │                  │
   │                           │  Check holderClass        │                  │
   │                           │  = CONTRIBUTOR ✓          │                  │
   │                           │                           │                  │
   │                           │  Check gateStatus         │                  │
   │                           │  = ACTIVE ✓ (Gate 1 met) │                  │
   │                           │                           │                  │
   │                           │  Check isLiquid           │                  │
   │                           │  = true ✓                 │                  │
   │                           │                           │                  │
   │                           │  Lock 100 tokens          │                  │
   │                           │  Calculate conversion     │                  │
   │                           │  100 × $5 = $500          │                  │
   │                           │  $500 / $0.10 = 5000 ARDA │                  │
   │                           │                           │                  │
   │                           │  Check liquidReserve      │                  │
   │                           │  IF insufficient:         │                  │
   │                           │    Rebalance from index   │                  │
   │                           │    Log REBALANCE tx       │                  │
   │                           │                           │                  │
   │                           │  Record on-chain ────────>│                  │
   │                           │                 txHash    │                  │
   │                           │<─────────────────────────│                  │
   │                           │                           │                  │
   │                           │  Debit liquidReserve      │                  │
   │                           │  Process USD payout ─────────────────────>│
   │                           │                           │     $500 payout  │
   │                           │                           │                  │
   │  $500 in bank account     │                           │                  │
   │<──────────────────────────│                           │                  │
```

### Conversion Flow — Funding → Gate 1

```
Investor                    Platform                    Stripe              Treasury
   │                           │                           │                    │
   │  Fund project ($5000)     │                           │                    │
   │──────────────────────────>│                           │                    │
   │                           │  Create PaymentIntent     │                    │
   │                           │──────────────────────────>│                    │
   │                           │       payment_intent.id   │                    │
   │                           │<──────────────────────────│                    │
   │                           │                           │                    │
   │  (Stripe checkout)        │  Webhook: payment.success │                    │
   │                           │<──────────────────────────│                    │
   │                           │                           │                    │
   │                           │  Create ProjectInvestment │                    │
   │                           │  (userId, $5000, tokens)  │                    │
   │                           │                           │                    │
   │                           │  Create TokenAllocation   │                    │
   │                           │  (holderClass=INVESTOR,   │                    │
   │                           │   isLiquid=false)         │                    │
   │                           │                           │                    │
   │                           │  Credit TokenBalance      │                    │
   │                           │  (INVESTOR, locked)       │                    │
   │                           │                           │                    │
   │                           │  Update fundingRaised     │                    │
   │                           │                           │                    │
   │                           │  ProcessFundingInflow ────────────────────────>│
   │                           │                           │   $2750 → index    │
   │                           │                           │   $1500 → liquid   │
   │                           │                           │   $ 750 → ops     │
   │                           │                           │                    │
   │                           │  EvaluateGate1            │                    │
   │                           │  (fundingRaised >= goal?) │                    │
   │                           │                           │                    │
   │                           │  IF gate clears:          │                    │
   │                           │    gateStatus = ACTIVE    │                    │
   │                           │    Unlock contributor     │                    │
   │                           │    token balances         │                    │
   │                           │                           │                    │
```

### Conversion Flow — Project Failure

```
Admin/Community             Platform                    Treasury
   │                           │                           │
   │  Mark Project Failed      │                           │
   │──────────────────────────>│                           │
   │                           │                           │
   │                           │  1. Set gateStatus=FAILED │
   │                           │                           │
   │                           │  2. BURN founder tokens:  │
   │                           │     Find all FOUNDER      │
   │                           │     allocations           │
   │                           │     Set status=BURNED     │
   │                           │     Zero founder balances │
   │                           │     Increment burnedSupply│
   │                           │     Log FOUNDER_BURN tx   │
   │                           │                           │
   │                           │  3. TRUST PROTECTION:     │
   │                           │     For each investor:    │
   │                           │     protection = invested │
   │                           │       * trustProtectionRt │
   │                           │                           │
   │                           │     Debit index fund ────>│  -$X from index
   │                           │                           │
   │                           │     Create payout requests│
   │                           │     for each investor     │
   │                           │                           │
   │                           │  4. Contributor tokens:   │
   │                           │     UNCHANGED. Still      │
   │                           │     liquid. Still hold    │
   │                           │     value in ARDA.        │
   │                           │                           │
   │  GateTransitionResult     │                           │
   │  { burned: 2000,          │                           │
   │    protectionPaid: $7500 }│                           │
   │<──────────────────────────│                           │
```

---

## Equity Allocation Model

### Holder-Class-Aware Equity Distribution

Project owners assign equity percentages to tasks (ProductBacklogItems) for contributors. Investors receive equity proportional to their funding contribution. Founders receive a reserved allocation at project creation.

```
Project: "DeFi Dashboard" (total supply: 10,000 tokens = 100%)
Funding Goal: $50,000 | Raised: $50,000 | Gate Status: ACTIVE

Holder Class Breakdown:
┌─────────────────────────────┬────────┬────────┬─────────────┬──────────┐
│ Allocation                  │ Equity │ Tokens │ Holder Class│ Liquid?  │
├─────────────────────────────┼────────┼────────┼─────────────┼──────────┤
│ Build auth system (task)    │  5.0%  │   500  │ CONTRIBUTOR │ ✓ Yes    │
│ Design UI components (task) │  3.0%  │   300  │ CONTRIBUTOR │ ✓ Yes    │
│ Implement trading engine    │ 10.0%  │  1000  │ CONTRIBUTOR │ ✓ Yes    │
│ Write documentation         │  1.5%  │   150  │ CONTRIBUTOR │ ✓ Yes    │
│ ... more tasks              │  ...   │  ...   │ CONTRIBUTOR │ ✓ Yes    │
├─────────────────────────────┼────────┼────────┼─────────────┼──────────┤
│ Investor: Alice ($10,000)   │ 10.0%  │  1000  │ INVESTOR    │ ✗ Locked │
│ Investor: Bob ($5,000)      │  5.0%  │   500  │ INVESTOR    │ ✗ Locked │
│ ... more investors          │  ...   │  ...   │ INVESTOR    │ ✗ Locked │
├─────────────────────────────┼────────┼────────┼─────────────┼──────────┤
│ Founder reserve             │ 20.0%  │  2000  │ FOUNDER     │ ✗ Locked │
├─────────────────────────────┼────────┼────────┼─────────────┼──────────┤
│ Unallocated (available)     │ 45.5%  │  4550  │ —           │ —        │
└─────────────────────────────┴────────┴────────┴─────────────┴──────────┘
                         Total: 100%     10,000
```

### Supply Invariant

At all times: `contributorSupply + investorSupply + founderSupply + burnedSupply ≤ totalSupply`

- **Contributor Supply**: Tokens allocated/distributed to contributors via task completion
- **Investor Supply**: Tokens allocated to investors via crowdfunding
- **Founder Supply**: Reserved for project creator(s), set at token config creation
- **Burned Supply**: Founder tokens destroyed on project failure
- **Available**: `totalSupply - contributorSupply - investorSupply - founderSupply - burnedSupply`

### Liquidity Rules

```
IsLiquid(holderClass, gateStatus):
  CONTRIBUTOR + ACTIVE    → true
  CONTRIBUTOR + SUCCEEDED → true
  INVESTOR    + SUCCEEDED → true
  FOUNDER     + SUCCEEDED → true
  everything else         → false
  FOUNDER     + FAILED    → tokens burned (balance = 0)
```

---

## Schema

### ProjectTokenConfig

```dbml
Table ProjectTokenConfig {
  id varchar [not null, pk, default: `cuid()`]
  projectId varchar [not null, unique]
  assetId varchar [note: 'Algorand ASA ID — null until minted on-chain']
  assetName varchar [not null, note: 'e.g., ProjectName Shares']
  unitName varchar [not null, note: 'Max 8 chars, e.g., PSHARE']
  totalSupply int [not null, note: 'Fixed supply = 100% equity']
  allocatedSupply int [not null, default: 0]
  distributedSupply int [not null, default: 0]
  reservedSupply int [not null, default: 0, note: 'Founder/team reserve']
  mintTxHash varchar
  status ProjectTokenStatus [not null, default: 'PENDING']
  createdAt datetime [not null, default: `now()`]
  updatedAt datetime [not null, note: 'Updated at']

  // Funding Gate (Gate 1)
  fundingGoal float [not null, note: 'USD amount required to clear Gate 1']
  fundingRaised float [not null, default: 0, note: 'USD raised so far']
  gateStatus ProjectGateStatus [not null, default: 'FUNDING']
  gate1ClearedAt datetime [note: 'When funding goal was met']
  gate2ClearedAt datetime [note: 'When project success was verified']
  failedAt datetime [note: 'When project was marked failed']

  // Holder class supply breakdown
  contributorSupply int [not null, default: 0, note: 'Tokens allocated/distributed to contributors']
  investorSupply int [not null, default: 0, note: 'Tokens allocated to investors']
  founderSupply int [not null, default: 0, note: 'Tokens reserved for founder(s)']
  burnedSupply int [not null, default: 0, note: 'Founder tokens burned on failure']

  // Success criteria
  successCriteria text [note: 'Description of what constitutes Gate 2 success']
  successVerifiedBy varchar [note: 'UserId of admin/community who verified success']
}

Ref: ProjectTokenConfig.projectId > Project.id
```

### TokenAllocation

```dbml
Table TokenAllocation {
  id varchar [not null, pk, default: `cuid()`]
  projectTokenConfigId varchar [not null]
  taskId varchar [note: 'Nullable — non-task equity allocations']
  recipientUserId varchar [note: 'Null until distributed']
  equityPercentage float [not null, note: 'e.g., 2.5 = 2.5%']
  tokenAmount int [not null, note: 'Calculated: totalSupply * equityPercentage / 100']
  status AllocationStatus [not null, default: 'RESERVED']
  holderClass TokenHolderClass [not null, note: 'CONTRIBUTOR, INVESTOR, or FOUNDER']
  isLiquid boolean [not null, default: false, note: 'Derived from holderClass + project gateStatus']
  distributedAt datetime
  distributionTxHash varchar
  burnedAt datetime [note: 'Set when founder tokens are burned on project failure']
  createdAt datetime [not null, default: `now()`]
  updatedAt datetime [not null, note: 'Updated at']
}

Ref: TokenAllocation.projectTokenConfigId > ProjectTokenConfig.id
Ref: TokenAllocation.taskId > ProductBacklogItem.id
Ref: TokenAllocation.recipientUserId > User.id
```

### TokenBalance

```dbml
Table TokenBalance {
  id varchar [not null, pk, default: `cuid()`]
  userId varchar [not null]
  projectTokenConfigId varchar [note: 'Null for ARDA token balance']
  isPlatformToken boolean [not null, default: false]
  holderClass TokenHolderClass [note: 'Which class this balance belongs to']
  isLiquid boolean [not null, default: false, note: 'Can this balance be traded/exited?']
  balance int [not null, default: 0]
  lockedBalance int [not null, default: 0, note: 'Locked for pending payouts']
  updatedAt datetime [not null, note: 'Updated at']

  indexes {
    (userId, projectTokenConfigId, holderClass) [unique, note: 'One balance per user per project token per holder class']
  }
}

Ref: TokenBalance.userId > User.id
Ref: TokenBalance.projectTokenConfigId > ProjectTokenConfig.id
```

Note: The unique index is now `(userId, projectTokenConfigId, holderClass)` — a user can hold both CONTRIBUTOR and INVESTOR tokens in the same project.

### PayoutRequest

```dbml
Table PayoutRequest {
  id varchar [not null, pk, default: `cuid()`]
  userId varchar [not null]
  sourceProjectTokenConfigId varchar
  sourceTokenAmount int [not null]
  ardaTokenAmount int
  usdAmount float
  status PayoutStatus [not null, default: 'PENDING']
  holderClass TokenHolderClass [not null, note: 'Which holder class tokens are being exited']
  gateStatusAtRequest ProjectGateStatus [not null, note: 'Snapshot of project gate status when requested']
  conversionTxHash varchar
  payoutTxHash varchar
  stripePayoutId varchar
  failureReason varchar
  requestedAt datetime [not null, default: `now()`]
  processedAt datetime
  completedAt datetime
}

Ref: PayoutRequest.userId > User.id
Ref: PayoutRequest.sourceProjectTokenConfigId > ProjectTokenConfig.id
```

### PlatformTreasury

```dbml
Table PlatformTreasury {
  id varchar [not null, pk, default: `cuid()`]
  ardaTotalSupply bigint [not null]
  ardaCirculatingSupply bigint [not null, default: 0]
  ardaAssetId varchar [note: 'Algorand ASA ID for ARDA token']
  ardaMintTxHash varchar

  // Three-bucket treasury model
  indexFundBalance float [not null, default: 0, note: '55% of inflows. Compounding.']
  liquidReserveBalance float [not null, default: 0, note: '30% of inflows. Immediate payouts.']
  operationsBalance float [not null, default: 0, note: '15% of inflows. Platform ops.']

  // Allocation percentages (configurable)
  indexFundAllocationPct float [not null, default: 0.55]
  liquidReserveAllocationPct float [not null, default: 0.30]
  operationsAllocationPct float [not null, default: 0.15]

  // Index fund parameters
  indexFundAnnualReturn float [not null, default: 0.08]
  platformProfitSharePct float [not null, default: 0.25]
  trustProtectionRate float [not null, default: 0.50, note: '% of investor capital protected on project failure']

  // Tracking
  totalInflows float [not null, default: 0]
  totalPayouts float [not null, default: 0]
  totalRebalanceTransfers float [not null, default: 0, note: 'Cumulative index→liquid transfers']
  lastRebalanceAt datetime
  lastReconciliationAt datetime
  updatedAt datetime [not null, note: 'Updated at']
}
```

Computed property: `totalTreasury = indexFundBalance + liquidReserveBalance + operationsBalance`

ARDA value: `totalTreasury / ardaCirculatingSupply`

### TreasuryTransaction

Audit log for all treasury movements.

```dbml
Table TreasuryTransaction {
  id varchar [not null, pk, default: `cuid()`]
  type TreasuryTransactionType [not null]
  amount float [not null]
  fromBucket varchar [note: 'index, liquid, operations, or external']
  toBucket varchar [note: 'index, liquid, operations, or external']
  relatedProjectId varchar
  relatedPayoutRequestId varchar
  description varchar
  balanceAfter float [not null, note: 'Total treasury after this transaction']
  createdAt datetime [not null, default: `now()`]
}
```

### ProjectInvestment

Tracks individual investor contributions for trust protection calculations.

```dbml
Table ProjectInvestment {
  id varchar [not null, pk, default: `cuid()`]
  projectTokenConfigId varchar [not null]
  userId varchar [not null]
  usdAmount float [not null, note: 'Amount invested in USD']
  tokenAmount int [not null, note: 'Project tokens received']
  stripePaymentIntentId varchar
  investedAt datetime [not null, default: `now()`]

  // Trust protection
  protectionEligible boolean [not null, default: true]
  protectionPaidOut boolean [not null, default: false]
  protectionAmount float [note: 'USD refunded on project failure']
  protectionPaidAt datetime

  indexes {
    (projectTokenConfigId, userId) [note: 'Can have multiple investments']
  }
}

Ref: ProjectInvestment.projectTokenConfigId > ProjectTokenConfig.id
Ref: ProjectInvestment.userId > User.id
```

### Enums

```dbml
Enum ProjectTokenStatus {
  PENDING     [note: 'Token config created, ASA not yet minted']
  ACTIVE      [note: 'ASA minted and operational']
  FROZEN      [note: 'Temporarily suspended']
  DISSOLVED   [note: 'Project ended, tokens retired']
}

Enum ProjectGateStatus {
  FUNDING    [note: 'Raising funds. No tokens liquid. Tasks disabled.']
  ACTIVE     [note: 'Gate 1 cleared. Tasks enabled. Contributor tokens liquid.']
  SUCCEEDED  [note: 'Gate 2 cleared. All tokens liquid.']
  FAILED     [note: 'Project failed. Founder tokens burned. Investor trust protection.']
}

Enum TokenHolderClass {
  CONTRIBUTOR [note: 'Earned through task completion. Liquid after Gate 1.']
  INVESTOR    [note: 'Purchased through crowdfunding. Locked until Gate 2.']
  FOUNDER     [note: 'Reserved for project creator. Locked until Gate 2. Burns on failure.']
}

Enum AllocationStatus {
  RESERVED    [note: 'Assigned to task, not yet distributed']
  DISTRIBUTED [note: 'Given to contributor']
  REVOKED     [note: 'Allocation cancelled']
  BURNED      [note: 'Founder tokens destroyed on project failure']
}

Enum PayoutStatus {
  PENDING     [note: 'Payout requested, awaiting processing']
  PROCESSING  [note: 'Conversion/payout in progress']
  COMPLETED   [note: 'USD delivered to contributor']
  FAILED      [note: 'Payout failed, tokens unlocked']
  CANCELLED   [note: 'Cancelled by user before processing']
}

Enum TreasuryTransactionType {
  FUNDING_INFLOW       [note: 'New project funding received']
  ALLOCATION_INDEX     [note: 'Inflow allocated to index fund']
  ALLOCATION_LIQUID    [note: 'Inflow allocated to liquid reserve']
  ALLOCATION_OPS       [note: 'Inflow allocated to operations']
  PAYOUT_DEBIT         [note: 'Fiat payout to contributor/investor']
  INDEX_RETURN         [note: 'Monthly index fund return applied']
  PROFIT_SHARE         [note: 'Platform profit share taken from index returns']
  REBALANCE            [note: 'Auto-rebalance from index to liquid']
  TRUST_PROTECTION     [note: 'Investor refund on project failure']
  FOUNDER_BURN         [note: 'Founder token burn — no treasury movement, supply reduction']
}
```

---

## Service Architecture

### Service Dependency Graph

```
┌───────────────────────────────────────────────────────────────────────────────────┐
│                              API CONTROLLERS                                       │
│  ProjectTokensController  TokenBalanceController  PayoutsController                │
│  TreasuryController                                                                │
└──────┬──────────────────────┬──────────────────────┬──────────────┬────────────────┘
       │                      │                      │              │
       ▼                      ▼                      ▼              ▼
┌──────────────────┐  ┌────────────────┐  ┌──────────────────┐  ┌──────────────────┐
│ IProjectToken    │  │ ITokenBalance  │  │ IPayoutService   │  │ ITreasuryService │
│    Service       │  │    Service     │  │                  │  │                  │
│                  │  │                │  │ Uses:            │  │ Inflow split     │
│ Create config    │  │ Credit/Debit   │  │ - IExchangeService│ │ Index returns    │
│ Allocate equity  │  │ Lock/Unlock    │  │ - ITokenBalance  │  │ Auto-rebalance   │
│ Distribute       │  │ Portfolio      │  │ - IAlgorandService│ │ Reconciliation   │
│                  │  │ IsLiquid check │  │ - ITreasuryService│ │ Transaction log  │
│ Uses:            │  │                │  │ - Stripe SDK     │  │                  │
│ - ITokenBalance  │  └────────────────┘  └──────────────────┘  └──────────────────┘
│ - IAlgorandService│          │                    │                    │
│ - IProjectGate   │          │                    │                    │
└──────────────────┘          │                    │                    │
       │                      │                    │                    │
       ▼                      └────────────────────┼────────────────────┘
┌──────────────────┐                               │
│ IProjectGate     │                    ┌──────────▼──────────┐
│    Service       │                    │  IExchangeService   │
│                  │                    │                     │
│ EvaluateGate1    │                    │ Project token value │
│ ClearGate2       │                    │ ARDA value (3-bucket│
│ FailProject      │                    │ Conversion calc     │
│                  │                    │                     │
│ Uses:            │                    │ Uses:               │
│ - IProjectToken  │                    │ - ProjectTokenConfig│
│ - ITreasury      │                    │ - PlatformTreasury  │
│ - ITokenBalance  │                    └─────────────────────┘
└──────────────────┘                               │
                                        ┌──────────▼──────────┐
                                        │  IAlgorandService   │
                                        │  (Track 07)         │
                                        │                     │
                                        │ CreateFungibleASA   │
                                        │ TransferASA         │
                                        │ ClawbackASA         │
                                        │ GetASABalance       │
                                        └─────────────────────┘
```

### Integration Points

| Existing Module | Integration |
|----------------|-------------|
| Track 02 (Projects) | Project creation triggers ProjectTokenConfig creation with fundingGoal; gate starts at FUNDING |
| Track 04 (Finance) | Crowdfunding payments create ProjectInvestment, call ProcessFundingInflow (3-bucket split), then EvaluateGate1 |
| Track 07 (Credentials) | Membership credential required before earning equity (KYC gate) |
| Track 08 (KYC) | PRO verification required for payout requests |
| ProductBacklogItem | Tasks linked to TokenAllocation via taskId FK; task creation only when gateStatus >= ACTIVE |

---

## Key Invariants

```
// Supply invariant (holder-class aware, burn-aware)
contributorSupply + investorSupply + founderSupply + burnedSupply <= totalSupply

// Treasury invariant
indexFundBalance + liquidReserveBalance + operationsBalance >= 0

// Allocation percentage invariant
indexFundAllocationPct + liquidReserveAllocationPct + operationsAllocationPct == 1.0

// Liquidity invariant — enforced on every PayoutRequest
holderClass == CONTRIBUTOR → project.gateStatus IN (ACTIVE, SUCCEEDED)
holderClass == INVESTOR    → project.gateStatus == SUCCEEDED
holderClass == FOUNDER     → project.gateStatus == SUCCEEDED
// FAILED projects: only trust protection payouts, not user-initiated

// Founder burn invariant
If project.gateStatus == FAILED:
  All FOUNDER allocations for this project MUST have status == BURNED
  All FOUNDER TokenBalances for this project MUST have balance == 0
```

---

## Environment Variables

| Variable | Description | Default |
|----------|-------------|---------|
| `ARDA_TOTAL_SUPPLY` | Total ARDA tokens to mint | `1000000000` |
| `ARDA_UNIT_NAME` | ARDA token unit name | `ARDA` |
| `STRIPE_SECRET_KEY` | Stripe API key for payments | (required) |
| `STRIPE_WEBHOOK_SECRET` | Stripe webhook signing secret | (required) |
| `STRIPE_CONNECT_ENABLED` | Enable Stripe Connect payouts | `false` |
| `PAYOUT_MIN_USD` | Minimum payout amount in USD | `10.00` |
| `PAYOUT_AUTO_PROCESS` | Auto-process payouts (vs admin approval) | `false` |
| `TREASURY_INDEX_FUND_PCT` | Index fund allocation percentage | `0.55` |
| `TREASURY_LIQUID_RESERVE_PCT` | Liquid reserve allocation percentage | `0.30` |
| `TREASURY_OPERATIONS_PCT` | Operations allocation percentage | `0.15` |
| `INDEX_FUND_ANNUAL_RETURN` | Expected annual index fund return | `0.08` |
| `PLATFORM_PROFIT_SHARE_PCT` | Platform share of index fund returns | `0.25` |
| `TRUST_PROTECTION_RATE` | Investor capital protection rate on failure | `0.50` |
| `GATE1_AUTO_ADVANCE` | Auto-advance Gate 1 when funding goal met | `true` |
| `GATE2_REQUIRE_ADMIN` | Require admin approval for Gate 2 (MVP) | `true` |
| `GATE2_COMMUNITY_VOTE` | Enable community voting for Gate 2 (post-MVP) | `false` |

Algorand variables are shared with Track 07 (`ALGORAND_NETWORK`, `ALGORAND_NODE_URL`, etc.).

---

## Security Considerations

| Concern | Mitigation |
|---------|-----------|
| Double-spending | Atomic balance updates with DB-level locking; lock/unlock pattern for payouts |
| Equity over-allocation | Supply invariant enforced: `contributorSupply + investorSupply + founderSupply + burnedSupply ≤ totalSupply` |
| Unauthorized transfers | `defaultFrozen: true` on all ASAs; platform controls all transfers |
| Treasury insolvency | Three-bucket model with auto-rebalance; reconciliation validates backing |
| Payout fraud | KYC (PRO level) required for payout requests; admin approval option |
| Price manipulation | Deterministic rates (treasury-backed); no AMM or market pricing |
| Premature liquidity | Gate enforcement: holder class + gate status checked before every payout |
| Founder fraud | Founder tokens burn on project failure — founders cannot extract value from failed projects |
| Investor exploitation | Trust protection mechanism provides partial refund (configurable rate) from index fund |
| Gate manipulation | Gate 1 is deterministic (funding threshold); Gate 2 requires admin verification (MVP) |
| Treasury bucket depletion | Auto-rebalance from index to liquid when reserve insufficient; alerts on low balances |

---

## Error Handling & Graceful Degradation

| Scenario | Behavior |
|----------|----------|
| Algorand unavailable during token creation | TokenConfig created in DB with `assetId = null`, status PENDING; retry later |
| Algorand unavailable during distribution | Balance updated off-chain, `distributionTxHash = null`; on-chain sync later |
| Stripe payout fails | Tokens unlocked, PayoutRequest status = FAILED, failureReason logged |
| Over-allocation attempt | Rejected with clear error: "Would exceed 100% equity" |
| Payout with insufficient balance | Rejected with error before any token locking |
| Payout with locked tokens | Rejected with gate-specific message (e.g., "Project hasn't reached funding goal yet") |
| Treasury liquid reserve insufficient | Auto-rebalance from index fund; if total treasury insufficient, payout queued |
| Contributor payout while FUNDING | Rejected: "Project hasn't reached funding goal yet. Tasks aren't available." |
| Investor payout while ACTIVE | Rejected: "Project hasn't reached success milestone. Your tokens unlock when the project delivers." |
| Founder payout while ACTIVE | Rejected: "Founder tokens unlock after project success verification." |
| Founder payout after FAILED | Rejected: "Founder tokens were burned when the project failed." |
| Trust protection with insufficient index fund | Partial protection paid from available funds; remainder queued; admin notified |
| Invalid gate transition (e.g., SUCCEEDED → FAILED) | Rejected with error: invalid state transition |

---

## File Manifest

### New Files
| File | Layer | Purpose |
|------|-------|---------|
| `ArdaNova.Application/DTOs/TokenDtos.cs` | Application | All token-related DTOs |
| `ArdaNova.Application/Services/Interfaces/IProjectTokenService.cs` | Application | Project token management |
| `ArdaNova.Application/Services/Interfaces/ITokenBalanceService.cs` | Application | Balance tracking |
| `ArdaNova.Application/Services/Interfaces/IExchangeService.cs` | Application | Exchange rate calculation |
| `ArdaNova.Application/Services/Interfaces/IPayoutService.cs` | Application | Payout processing |
| `ArdaNova.Application/Services/Interfaces/ITreasuryService.cs` | Application | Three-bucket treasury management |
| `ArdaNova.Application/Services/Interfaces/IProjectGateService.cs` | Application | Gate transition logic |
| `ArdaNova.Application/Services/Implementations/ProjectTokenService.cs` | Application | Token management impl |
| `ArdaNova.Application/Services/Implementations/TokenBalanceService.cs` | Application | Balance tracking impl |
| `ArdaNova.Application/Services/Implementations/ExchangeService.cs` | Application | Exchange rate impl |
| `ArdaNova.Application/Services/Implementations/PayoutService.cs` | Application | Payout processing impl |
| `ArdaNova.Application/Services/Implementations/TreasuryService.cs` | Application | Treasury management impl |
| `ArdaNova.Application/Services/Implementations/ProjectGateService.cs` | Application | Gate transition impl |
| `ArdaNova.API/Controllers/ProjectTokensController.cs` | API | Token management endpoints |
| `ArdaNova.API/Controllers/TokenBalanceController.cs` | API | Balance query endpoints |
| `ArdaNova.API/Controllers/PayoutsController.cs` | API | Payout endpoints |
| `ArdaNova.API/Controllers/TreasuryController.cs` | API | Treasury management endpoints |
| `ardanova-client/src/lib/api/ardanova/endpoints/project-tokens.ts` | Frontend | API client |
| `ardanova-client/src/lib/api/ardanova/endpoints/token-balances.ts` | Frontend | API client |
| `ardanova-client/src/lib/api/ardanova/endpoints/payouts.ts` | Frontend | API client |
| `ardanova-client/src/lib/api/ardanova/endpoints/exchange.ts` | Frontend | API client |
| `ardanova-client/src/lib/api/ardanova/endpoints/treasury.ts` | Frontend | API client |
| `ardanova-client/src/lib/api/ardanova/endpoints/project-gates.ts` | Frontend | API client |
| `tests/.../ProjectTokenServiceTests.cs` | Tests | Token service tests |
| `tests/.../TokenBalanceServiceTests.cs` | Tests | Balance service tests |
| `tests/.../ExchangeServiceTests.cs` | Tests | Exchange service tests |
| `tests/.../PayoutServiceTests.cs` | Tests | Payout service tests |
| `tests/.../TreasuryServiceTests.cs` | Tests | Treasury service tests |
| `tests/.../ProjectGateServiceTests.cs` | Tests | Gate transition tests |

### Modified Files
| File | Changes |
|------|---------|
| `ardanova-client/prisma/database-architecture.dbml` | Add 7 tables + 6 enums; modify ProjectTokenConfig, TokenAllocation, TokenBalance, PayoutRequest, PlatformTreasury |
| `ArdaNova.Application/DependencyInjection.cs` | Register 6 new services |
| `ArdaNova.Application/Mappings/MappingProfile.cs` | Add token DTO mappings including gate and treasury DTOs |
| `ArdaNova.Application/Services/Interfaces/IAlgorandService.cs` | Add fungible ASA methods |
| `ArdaNova.Infrastructure/Algorand/AlgorandService.cs` | Implement fungible ASA methods |
| `ardanova-client/src/lib/api/ardanova/index.ts` | Register 6 new endpoint modules |
| `ardanova-client/src/server/api/root.ts` | Register new tRPC routers |
