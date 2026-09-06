/**
 * Single import surface for the AZOA "oasis-object" types the app consumes.
 *
 * App code should import AZOA object types from `~/lib/azoa-sdk/types` rather
 * than reaching into `azoa-sdk` directly, so the SDK boundary stays in one
 * place and future SDK moves only touch this file.
 *
 * READ-ONLY: these are the shapes of objects we *read* into client state.
 * Write/param types (mint/transfer/swap params, etc.) are intentionally NOT
 * re-exported here — value-bearing writes go through the .NET backend, never
 * the browser. See `client.ts` for the architecture rationale.
 */

// Wallet + portfolio object shapes ("./api" export).
export type {
  WalletResult,
  PortfolioResult,
  PortfolioAsset,
  PortfolioAssetKind,
  NftHolding,
} from "azoa-sdk/api";

// Holon object shapes (root export — holons are not first-class methods on the
// API client, so we read them via the generic request path; see queries.ts).
export type { HolonResult, HolonQueryParams } from "azoa-sdk";

// Action run-state object shapes (root export, also available from
// "azoa-sdk/workflow"). NOTE: a action *run* uses `WorkflowRunStatus` /
// `WorkflowExecutionState` — distinct from a action *definition*'s
// `ActionStatus` / `ActionNodeState` below.
export type {
  WorkflowExecutionState,
  WorkflowNodeExecution,
  WorkflowRunResult,
  WorkflowRunStatus,
} from "azoa-sdk";

// Action *definition* object shapes ("./api" export) — useful when rendering the
// static DAG alongside a live run.
export type {
  ActionResult,
  ActionNodeResult,
  ActionStatus,
  ActionNodeState,
} from "azoa-sdk/api";

// Error type surfaced by every SDK call (Result<T, SdkError>).
export type { SdkError } from "azoa-sdk";
