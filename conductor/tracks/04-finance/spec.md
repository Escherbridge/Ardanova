# Financial & Tokenomics Specification

## Overview
This track handles the dual-asset economy (Shares vs Credentials), treasury management, and escrow services. It connects the off-chain application state with on-chain (Algorand) assets.

## Data Models

### ProjectShare
Economic token representing ownership/equity.
- `Id`: UUID (PK)
- `ProjectId`: UUID (Unique FK)
- `AssetId`: String? (Algorand ASA ID)
- `Name`: String
- `Symbol`: String
- `TotalSupply`: Decimal
- `Decimals`: Integer (Default 6)
- `Allocation`: Json (Pool distribution)
- `IsDeployed`: Boolean

### Wallet
- `UserId`: UUID (FK)
- `Address`: String (Algorand Address)
- `Provider`: Enum (PERA, DEFLY, ALGOSIGNER, WALLETCONNECT)
- `IsVerified`: Boolean
- `IsPrimary`: Boolean

### Treasury
- `ProjectId`: UUID (FK)
- `Balance`: Decimal (Stablecoin/Native)
- `ShareAssetId`: String?
- **TreasuryTransaction**: `Amount`, `Type` (DEPOSIT, WITHDRAWAL, TASK_PAYMENT), `TxHash`, `ProposalId`.

### TaskEscrow
Holds funds for a task until completion.
- `TaskId`: UUID (FK)
- `FunderId`: UUID (FK)
- `ShareId`: UUID (FK)
- `Amount`: Decimal
- `Status`: Enum (FUNDED, RELEASED, DISPUTED, REFUNDED)
- `TxHashFund`: String?
- `TxHashRelease`: String?

## API Endpoints (`FinanceController` / `GovernanceController`)

| Method | Endpoint | Description | DTO |
| :--- | :--- | :--- | :--- |
| `POST` | `/api/wallets` | Connect wallet | `CreateWalletDto` |
| `GET` | `/api/wallets` | List user wallets | `WalletDto[]` |
| `POST` | `/api/governance/proposals` | Create proposal | `CreateProposalDto` |
| `POST` | `/api/governance/votes` | Cast vote | `CastVoteDto` |
| `POST` | `/api/finance/escrow/fund` | Fund task | `CreateTaskEscrowDto` |
| `POST` | `/api/finance/escrow/release` | Release funds | `ReleaseEscrowDto` |

## Business Logic & Validation

### 1. Dual-Asset Model
- **Credentials** (Auth Track): 1 person = 1 vote. Used for Governance.
- **Shares** (Finance Track): 1 share = 1 vote. Used for Economic decisions (Treasury).
- **VoteWeight**: API must distinguish between Proposal Types to calculate weight correctly.

### 2. Proposal Lifecycle
- **Quorum**: Minimum % of voting power required (Default 50%).
- **Threshold**: Minimum % of YES votes required (Default 51%).
- **ExecutionDelay**: Time buffer after passing before execution.

### 3. Escrow Workflow
1.  **Fund**: `CreateTaskEscrowDto` -> Validates balance -> Records `TxHash` -> Status `FUNDED`.
2.  **Release**: Task Owner/Admin approves -> Triggers smart contract release -> Records `TxHashRelease` -> Status `RELEASED`.

## Integration Points
- **Projects**: Linking Shares/Treasury to Project entities.
- **Tasks**: Linking Escrow to specific Tasks.
- **Algorand**: All `TxHash` fields verify on-chain finality.
