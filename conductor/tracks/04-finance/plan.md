## 1. Core Financial Infrastructure
- [ ] **[P0] Asset Standards**: `ProjectShare` (ERC-20 style) and `ProjectEquity`.
- [ ] **[P0] Precision**: Use `Decimal(18, 8)` for all values; enforce non-negative balances.
- [ ] **[P0] Wallets**: Multi-provider support (`PERA`, `DEFLY`, `WALLETCONNECT`), Primary wallet logic.

## 2. Treasury & Tokenomics
- [ ] **[P0] Treasury Management**: 
    - [ ] Single Treasury per Project/Guild.
    - [ ] Transaction Types: `DEPOSIT`, `WITHDRAWAL`, `TASK_PAYMENT`, `DIVIDEND`, `FEE`.
- [ ] **[P0] Share Management**:
    - [ ] `ShareHolder` balance tracking (`available`, `staked`, `locked`).
    - [ ] `ShareVesting`: Schedules with Cliff, Frequency (`DAILY`...`QUARTERLY`), and Release logic.

## 3. DeFi Primitives
- [ ] **[P1] Escrow**: `TaskEscrow` lifecycle (`FUNDED` -> `RELEASED`/`DISPUTED`).
- [ ] **[P1] Swaps**: `ShareSwap` atomic exchange between assets.
- [ ] **[P2] Liquidity Pools**: `LiquidityPool` (AMM logic), `LiquidityProvider` tracking.

## 4. Fundraising
- [ ] **[P0] Campaigns**: `Fundraising` entity with `fundingGoal`, `min`/`maxContribution`, `startDate`/`endDate`.
- [ ] **[P0] Contribution Flow**: `PENDING` -> `CONFIRMED` / `FAILED`.
- [ ] **[P1] Outcomes**:
    - [ ] Success: Funds released to Treasury.
    - [ ] Failure: Auto-refund trigger.
