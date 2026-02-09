# Track 09 — Tokenomics & Project Equity

## 1. Schema — Token & Equity Models ✅ COMPLETE
- [x] **[P0] DBML: ProjectTokenConfig table**
- [x] **[P0] DBML: TokenAllocation table**
- [x] **[P0] DBML: TokenBalance table**
- [x] **[P0] DBML: PayoutRequest table**
- [x] **[P0] DBML: PlatformTreasury table (three-bucket model)**
- [x] **[P0] DBML: TreasuryTransaction table (audit log)**
- [x] **[P0] DBML: ProjectInvestment table**
- [x] **[P0] DBML: New enums** (ProjectTokenStatus, ProjectGateStatus, TokenHolderClass, AllocationStatus, PayoutStatus, TreasuryTransactionType)
- [x] **[P0] Run generators**: `npm run generate:prisma` + `npm run generate:csharp`

## 2. Backend — Project Token Service ✅ COMPLETE
- [x] **[P0] DTOs: Token management data objects** (ProjectTokenConfigDto, TokenAllocationDto, TokenBalanceDto, UserPortfolioDto, PayoutRequestDto, GateTransitionResult, ProjectGateStatusDto, TreasuryStatusDto, TreasuryTransactionDto, ProjectInvestmentDto + create DTOs)
- [x] **[P0] IProjectTokenService interface** (14 methods)
- [x] **[P0] ProjectTokenService implementation** (supply invariant, holder class enforcement, burn/trust protection)
- [x] **[P1] ProjectTokensController** (18 endpoints including config CRUD, allocations, gate management, failure handling)

## 3. Backend — Project Gate Service ✅ COMPLETE
- [x] **[P0] IProjectGateService interface** (EvaluateGate1, ClearGate2, FailProject, GetGateStatus)
- [x] **[P0] ProjectGateService implementation** (Gate 1/2 logic, failure with burn + trust protection, state transition validation)
- [x] **[P1] Gate endpoints on ProjectTokensController** (evaluate, clear, fail, get gate status)

## 4. Backend — Token Balance & Portfolio Service ✅ COMPLETE
- [x] **[P0] ITokenBalanceService interface** (GetBalance, GetArda, GetPortfolio, Credit, Debit, Lock, Unlock, IsBalanceLiquid)
- [x] **[P0] TokenBalanceService implementation** (atomic updates, lock/unlock, portfolio aggregation, liquidity check)
- [x] **[P1] TokenBalanceController** (balance, arda, portfolio, liquidity, exchange endpoints)

## 5. Backend — Treasury Service ✅ COMPLETE
- [x] **[P0] ITreasuryService interface** (ProcessFundingInflow, ApplyIndexFundReturn, RebalanceIfNeeded, Reconcile, GetStatus, GetTransactionHistory)
- [x] **[P0] TreasuryService implementation** (three-bucket split, index returns, auto-rebalance, reconciliation, tx logging)
- [x] **[P1] TreasuryController** (status, transactions, funding-inflow, apply-index-return, rebalance, reconcile)

## 6. Backend — Exchange & Payout Service ✅ COMPLETE
- [x] **[P0] IExchangeService interface** (GetProjectTokenValue, GetArdaValue, CalculateConversion, GetTreasuryStatus)
- [x] **[P0] ExchangeService implementation** (deterministic rates, gate-aware adjustments)
- [x] **[P0] IPayoutService interface** (RequestPayout, ProcessPayout, GetPayoutsByUser, GetPendingPayouts, CancelPayout)
- [x] **[P0] PayoutService implementation** (gate enforcement, lock/debit/convert, gate-specific rejection messages)
- [x] **[P1] PayoutsController** (request, process, cancel, by-user, pending)

## 7. Backend — DI, Mappings, Build ✅ COMPLETE
- [x] **[P0] DependencyInjection.cs** — Register 6 tokenomics services
- [x] **[P0] MappingProfile.cs** — AutoMapper profiles for all token/gate/treasury DTOs
- [x] **[P0] Build** — Clean dotnet build with all services, controllers, DTOs

## 8. Unit Tests ✅ COMPLETE (533/533 passing)
- [x] **[P0] ProjectTokenServiceTests.cs** — Config creation, allocations (task/investor/founder), distribution, revocation, burn, trust protection
- [x] **[P0] ProjectGateServiceTests.cs** — Gate 1/2 transitions, failure, invalid transitions, contributor unaffected
- [x] **[P0] TokenBalanceServiceTests.cs** — Credit/debit, lock/unlock, portfolio, overdraft prevention, liquidity rules
- [x] **[P0] TreasuryServiceTests.cs** — 55/30/15 split, index return, profit share, auto-rebalance, reconciliation
- [x] **[P0] ExchangeServiceTests.cs** — Token value, ARDA value, conversion, gate-aware values, edge cases
- [x] **[P0] PayoutServiceTests.cs** — Request/process/cancel, gate enforcement for all holder classes, auto-rebalance

---

## 9. Stripe SDK Integration [P1] — NEW
- [ ] **Install `Stripe.net` NuGet package** in `ArdaNova.Application.csproj`
- [ ] **Create `IStripeService` interface** in `Services/Interfaces/IStripeService.cs`
    - `CreateCheckoutSessionAsync(projectTokenConfigId, userId, usdAmount, ct)` → StripeCheckoutSessionDto
    - `HandlePaymentSucceededAsync(paymentIntentId, ct)` → ProjectInvestmentDto
    - `HandlePaymentFailedAsync(paymentIntentId, failureReason, ct)` → bool
    - `CreateConnectedAccountAsync(userId, email, ct)` → StripeConnectedAccountDto
    - `CreatePayoutTransferAsync(payoutRequestId, connectedAccountId, usdAmount, ct)` → StripeTransferDto
    - `HandlePayoutSucceededAsync(transferId, ct)` → PayoutRequestDto
    - `HandlePayoutFailedAsync(transferId, failureReason, ct)` → PayoutRequestDto
- [ ] **Create Stripe DTOs** in `DTOs/StripeDtos.cs`
    - `StripeCheckoutSessionDto` (SessionId, SessionUrl, ProjectTokenConfigId, UsdAmount)
    - `StripeConnectedAccountDto` (AccountId, UserId, OnboardingUrl, Status)
    - `StripeTransferDto` (TransferId, PayoutRequestId, UsdAmount, Status)
- [ ] **Create `StripeService` implementation** in `Services/Implementations/StripeService.cs`
    - Uses `Stripe.PaymentIntentService`, `Stripe.Checkout.SessionService`, `Stripe.TransferService`
    - Inject `IConfiguration` for API keys
    - Inject `IProjectTokenService`, `ITokenBalanceService`, `ITreasuryService`, `IProjectGateService` for payment orchestration
    - Payment succeeded handler: create ProjectInvestment → AllocateToInvestor → CreditAsync (locked) → ProcessFundingInflow → EvaluateGate1
    - Payout transfer: call Stripe API to create transfer to connected account
- [ ] **Create `StripeWebhookController`** in `ArdaNova.API/Controllers/StripeWebhookController.cs`
    - `POST /api/webhooks/stripe` — validate signature, parse event, dispatch to IStripeService
    - Event routing: `payment_intent.succeeded`, `payment_intent.payment_failed`, `transfer.paid`, `transfer.failed`
- [ ] **Register IStripeService** in `DependencyInjection.cs`
- [ ] **Wire PayoutService** to call `IStripeService.CreatePayoutTransferAsync` during ProcessPayoutAsync
- [ ] **Write `StripeServiceTests.cs`** unit tests (mock Stripe SDK classes + dependent services)
    - Checkout session creation
    - Payment succeeded → full investment flow
    - Payment failed → no side effects
    - Connected account creation
    - Payout transfer success/failure
    - Webhook event routing

## 10. API Client + tRPC Routers [P1] — NEW
- [ ] **Create `project-tokens.ts` API endpoint** in `ardanova-client/src/lib/api/ardanova/endpoints/`
    - Config: createConfig, getConfig, getConfigByProject, getSupply
    - Allocations: allocateToTask, allocateToInvestor, allocateToFounder, distribute, revoke, getAllocations, getAllocationsByTask, getInvestors
    - Gate: getGateStatus, evaluateGate, clearGate, failProject
    - Failure: burnFounder, trustProtection
- [ ] **Create `token-balances.ts` API endpoint**
    - getBalance, getArdaBalance, getPortfolio, checkLiquidity
    - Exchange: getProjectTokenValue, getArdaValue, getConversionPreview
- [ ] **Create `payouts.ts` API endpoint**
    - requestPayout, processPayout, cancelPayout, getPayoutsByUser, getPendingPayouts
- [ ] **Create `treasury.ts` API endpoint**
    - getStatus, getTransactions, processFundingInflow, applyIndexReturn, rebalance, reconcile
- [ ] **Register all new endpoints** in `ArdaNovaApiClient` class (`index.ts`)
    - Add imports, properties, constructor initialization
    - Add type re-exports
- [ ] **Create `project-tokens.ts` tRPC router** in `ardanova-client/src/server/api/routers/`
    - Thin proxy: protectedProcedure → apiClient.projectTokens.method() → TRPCError on failure
    - Zod input schemas for each procedure
- [ ] **Create `token-balances.ts` tRPC router**
- [ ] **Create `payouts.ts` tRPC router**
- [ ] **Create `treasury.ts` tRPC router**
- [ ] **Register all tRPC routers** in `appRouter` (`root.ts`)

## 11. Service-Level Flow Tests [P0] — NEW
- [ ] **Create test infrastructure** in `tests/ArdaNova.Application.Tests/Flows/`
    - Shared in-memory repository backing stores (`Dictionary<string, T>`)
    - Repository mock factory (generic helper for FindAsync/FindOneAsync/GetByIdAsync/AddAsync/UpdateAsync)
    - Real AutoMapper instance with MappingProfile
    - Mocked IAlgorandService and IStripeService
- [ ] **Flow 1: Project Creation → Funding → Gate 1**
    - CreateConfigAsync → AllocateToFounderAsync → simulate investment → ProcessFundingInflow → EvaluateGate1
    - Verify: FUNDING→ACTIVE, contributor tokens liquid, investor/founder locked, treasury 55/30/15 split
    - Invariant: supply breakdown sums correctly
- [ ] **Flow 2: Task Completion → Contributor Payout**
    - AllocateToTaskAsync → DistributeAsync → CalculateConversion → RequestPayout → ProcessPayout
    - Verify: tokens locked then debited, payout COMPLETED, balance updated
- [ ] **Flow 3: Project Success (Gate 2)**
    - ClearGate2Async → verify SUCCEEDED
    - Verify: all INVESTOR + FOUNDER balances isLiquid = true, CONTRIBUTOR unchanged
- [ ] **Flow 4: Project Failure — Founder Burn + Investor Trust Protection**
    - FailProjectAsync → verify FAILED
    - Verify: FOUNDER allocations BURNED, balances zeroed, burnedSupply incremented
    - Verify: investor trust protection paid, index fund debited
    - Verify: CONTRIBUTOR tokens unaffected
    - Invariant: `contributorSupply + investorSupply + founderSupply + burnedSupply <= totalSupply`

## 12. Frontend — Token & Equity UI [P2] — DEFERRED
- [ ] **[P2] Project funding page** (funding progress, gate badge, Stripe checkout)
- [ ] **[P2] Project equity dashboard** (supply breakdown, allocation table, gate timeline)
- [ ] **[P2] User portfolio page** (holdings by liquidity, ARDA balance, payout history)
- [ ] **[P2] Payout request flow** (conversion preview, submit, status tracking)
- [ ] **[P2] Task equity display** (equity % on task cards)
- [ ] **[P2] Treasury dashboard (admin)** (three-bucket visualization, audit log)
