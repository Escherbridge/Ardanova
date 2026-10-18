import { apiPath, query } from "../../path";
import { type BaseApiClient, type ApiResponse } from "../../base-client";

export type TreasuryTransactionType =
  | "FUNDING_INFLOW"
  | "ALLOCATION_INDEX"
  | "ALLOCATION_LIQUID"
  | "ALLOCATION_OPS"
  | "PAYOUT_DEBIT"
  | "INDEX_RETURN"
  | "PROFIT_SHARE"
  | "REBALANCE"
  | "TRUST_PROTECTION"
  | "FOUNDER_BURN";

export interface TreasuryStatusDto {
  totalUsd?: number;
  liquidUsd?: number;
  [key: string]: unknown;
}

export interface TreasuryTransactionDto {
  id: string;
  type: TreasuryTransactionType;
  amount?: number;
  createdAt?: string;
  [key: string]: unknown;
}

export class TreasuryEndpoint {
  constructor(private client: BaseApiClient) {}

  getStatus(): Promise<ApiResponse<TreasuryStatusDto>> {
    return this.client.get<TreasuryStatusDto>("/api/Treasury/status");
  }

  getTransactions(
    limit: number,
  ): Promise<ApiResponse<TreasuryTransactionDto[]>> {
    return this.client.get<TreasuryTransactionDto[]>(
      apiPath`/api/Treasury/transactions?limit=${limit}`,
    );
  }

  getExchangeTreasuryStatus(): Promise<ApiResponse<TreasuryStatusDto>> {
    return this.client.get<TreasuryStatusDto>(
      "/api/Treasury/exchange/treasury-status",
    );
  }

  processFundingInflow(
    usdAmount: number,
    projectId?: string,
  ): Promise<ApiResponse<unknown>> {
    const q = query({ usdAmount, projectId: projectId || undefined });
    return this.client.post<unknown>(
      apiPath`/api/Treasury/funding-inflow${q}`,
      {},
    );
  }

  applyIndexReturn(): Promise<ApiResponse<unknown>> {
    return this.client.post<unknown>("/api/Treasury/apply-index-return", {});
  }

  rebalance(requiredLiquid: number): Promise<ApiResponse<unknown>> {
    return this.client.post<unknown>(
      apiPath`/api/Treasury/rebalance?requiredLiquid=${String(requiredLiquid)}`,
      {},
    );
  }

  reconcile(): Promise<ApiResponse<unknown>> {
    return this.client.post<unknown>("/api/Treasury/reconcile", {});
  }
}
