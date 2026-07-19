/**
 * AZOA oasis-object READ hooks (React Query state layer).
 *
 * Each hook wraps a READ-ONLY AZOA SDK call and lands the result in the React
 * Query cache — the cache is the client-held "oasis-object state". No hook here
 * performs a value-bearing write; writes go through the .NET backend (see
 * `client.ts`).
 *
 * SDK calls return `Result<T, SdkError>` (they never throw). React Query models
 * failure by a thrown error, so `unwrapResult` re-throws the `SdkError` on
 * failure and returns the value on success — giving each hook idiomatic
 * `data` / `error` / `isLoading` semantics.
 */

import { useQuery } from "@tanstack/react-query";
import type { UseQueryOptions } from "@tanstack/react-query";

import { isAwaiting, isOk } from "azoa-sdk";
import type { Result, SdkError } from "azoa-sdk";
import { API_PATHS } from "azoa-sdk/api";

import { getAzoaSdkClient } from "./client";
import type {
  WalletResult,
  PortfolioResult,
  HolonResult,
  WorkflowExecutionState,
  WorkflowRunStatus,
} from "./types";

/* -------------------------------------------------------------------------- */
/* Query keys                                                                 */
/* -------------------------------------------------------------------------- */

/**
 * Stable, hierarchical query keys for every AZOA object kind. Use these (rather
 * than inline arrays) so invalidation/prefetch stays consistent across the app.
 */
export const azoaKeys = {
  all: ["azoa"] as const,
  wallet: (walletId: string) => ["azoa", "wallet", walletId] as const,
  wallets: () => ["azoa", "wallets"] as const,
  portfolio: (walletId: string) => ["azoa", "portfolio", walletId] as const,
  holon: (holonId: string) => ["azoa", "holon", holonId] as const,
  holonChildren: (holonId: string) =>
    ["azoa", "holon", holonId, "children"] as const,
  questRunState: (runId: string) =>
    ["azoa", "quest", "run", runId, "execution-state"] as const,
} as const;

/* -------------------------------------------------------------------------- */
/* Helpers                                                                    */
/* -------------------------------------------------------------------------- */

/** Re-throw the SdkError on failure so React Query populates `error`. */
function unwrapResult<T>(result: Result<T, SdkError>): T {
  if (isOk(result)) return result.value;
  throw result.error;
}

/** True while a quest run is still progressing/parked (keep polling). */
export function isAzoaRunPending(status: WorkflowRunStatus): boolean {
  return status === "Pending" || status === "Running" || isAwaiting(status);
}

/**
 * Common shape for the optional per-hook overrides we forward to React Query.
 * Excludes `queryKey`/`queryFn` (owned by the hook) so callers only tune
 * behaviour (enabled, staleTime, refetchInterval, etc.).
 */
type ReadHookOptions<T> = Omit<
  UseQueryOptions<T, SdkError, T, readonly unknown[]>,
  "queryKey" | "queryFn"
>;

/* -------------------------------------------------------------------------- */
/* Wallets                                                                    */
/* -------------------------------------------------------------------------- */

/** Read a single wallet object into state. */
export function useAzoaWallet(
  walletId: string | undefined,
  options?: ReadHookOptions<WalletResult>,
) {
  return useQuery<WalletResult, SdkError, WalletResult, readonly unknown[]>({
    queryKey: azoaKeys.wallet(walletId ?? ""),
    queryFn: async () => {
      const client = getAzoaSdkClient();
      return unwrapResult(await client.getWallet(walletId!));
    },
    enabled: Boolean(walletId),
    // Wallet metadata changes rarely; keep it fresh for a minute.
    staleTime: 60 * 1000,
    ...options,
  });
}

/** Read the list of wallets into state. */
export function useAzoaWallets(options?: ReadHookOptions<WalletResult[]>) {
  return useQuery<WalletResult[], SdkError, WalletResult[], readonly unknown[]>({
    queryKey: azoaKeys.wallets(),
    queryFn: async () => {
      const client = getAzoaSdkClient();
      return unwrapResult(await client.listWallets());
    },
    staleTime: 60 * 1000,
    ...options,
  });
}

/* -------------------------------------------------------------------------- */
/* Portfolio                                                                  */
/* -------------------------------------------------------------------------- */

/**
 * Read a wallet's render-ready portfolio into state. Prefer `data.assets`
 * (precomputed raw + display amounts) over the legacy scalar `balance`/`nfts`.
 * Chain remains source of truth — AZOA stores no balance.
 */
export function useAzoaPortfolio(
  walletId: string | undefined,
  options?: ReadHookOptions<PortfolioResult>,
) {
  return useQuery<PortfolioResult, SdkError, PortfolioResult, readonly unknown[]>(
    {
      queryKey: azoaKeys.portfolio(walletId ?? ""),
      queryFn: async () => {
        const client = getAzoaSdkClient();
        return unwrapResult(
          await client.getWalletPortfolioRenderModel(walletId!),
        );
      },
      enabled: Boolean(walletId),
      // Balances move with the chain; keep this short.
      staleTime: 15 * 1000,
      ...options,
    },
  );
}

/* -------------------------------------------------------------------------- */
/* Holons                                                                     */
/* -------------------------------------------------------------------------- */
/*
 * Holon reads are not first-class methods on AzoaApiClient, so we go through the
 * public generic `request<T>(method, path)` using API_PATHS. These are
 * AZOAResult<T>-wrapped endpoints, matching `request` (not `requestBare`).
 */

/** Read a single holon object into state. */
export function useAzoaHolon(
  holonId: string | undefined,
  options?: ReadHookOptions<HolonResult>,
) {
  return useQuery<HolonResult, SdkError, HolonResult, readonly unknown[]>({
    queryKey: azoaKeys.holon(holonId ?? ""),
    queryFn: async () => {
      const client = getAzoaSdkClient();
      return unwrapResult(
        await client.request<HolonResult>("GET", API_PATHS.HOLON_GET(holonId!)),
      );
    },
    enabled: Boolean(holonId),
    staleTime: 60 * 1000,
    ...options,
  });
}

/** Read a holon's direct children into state. */
export function useAzoaHolonChildren(
  holonId: string | undefined,
  options?: ReadHookOptions<HolonResult[]>,
) {
  return useQuery<HolonResult[], SdkError, HolonResult[], readonly unknown[]>({
    queryKey: azoaKeys.holonChildren(holonId ?? ""),
    queryFn: async () => {
      const client = getAzoaSdkClient();
      return unwrapResult(
        await client.request<HolonResult[]>(
          "GET",
          API_PATHS.HOLON_CHILDREN(holonId!),
        ),
      );
    },
    enabled: Boolean(holonId),
    staleTime: 60 * 1000,
    ...options,
  });
}

/* -------------------------------------------------------------------------- */
/* Quest run-state                                                            */
/* -------------------------------------------------------------------------- */

/**
 * Read a quest run's live execution-state into state. Poll-friendly: pass
 * `refetchInterval` to drive a status display. The hook does NOT error on
 * awaiting/in-progress statuses (Pending / Running / Suspended / AwaitingSignal
 * / AwaitingTimer) — those are normal pending states; use `isAzoaRunPending`
 * (or the SDK's `isTerminal`) to decide whether to keep polling.
 *
 * @example
 *   const { data } = useAzoaQuestRunState(runId, {
 *     refetchInterval: (q) =>
 *       q.state.data && !isAzoaRunPending(q.state.data.status) ? false : 2000,
 *   });
 */
export function useAzoaQuestRunState(
  runId: string | undefined,
  options?: ReadHookOptions<WorkflowExecutionState>,
) {
  return useQuery<
    WorkflowExecutionState,
    SdkError,
    WorkflowExecutionState,
    readonly unknown[]
  >({
    queryKey: azoaKeys.questRunState(runId ?? ""),
    queryFn: async () => {
      const client = getAzoaSdkClient();
      return unwrapResult(
        await client.request<WorkflowExecutionState>(
          "GET",
          API_PATHS.QUEST_RUN_EXECUTION_STATE(runId!),
        ),
      );
    },
    enabled: Boolean(runId),
    // Run-state is live; default to fresh-on-read so polling actually refetches.
    staleTime: 0,
    ...options,
  });
}
