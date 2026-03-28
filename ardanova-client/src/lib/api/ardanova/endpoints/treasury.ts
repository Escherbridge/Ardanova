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
  type: TreasuryTransactionType | string;
  amount?: number;
  createdAt?: string;
  [key: string]: unknown;
}

export class TreasuryEndpoint {
  constructor(private client: BaseApiClient) {}

  getStatus(): Promise<ApiResponse<TreasuryStatusDto>> {
    return this.client.get<TreasuryStatusDto>("/api/Treasury/status");
  }

  getTransactions(limit: number): Promise<ApiResponse<TreasuryTransactionDto[]>> {
    return this.client.get<TreasuryTransactionDto[]>(`/api/Treasury/transactions?limit=${limit}`);
  }

  getExchangeTreasuryStatus(): Promise<ApiResponse<TreasuryStatusDto>> {
    return this.client.get<TreasuryStatusDto>("/api/Treasury/exchange/treasury-status");
  }

  processFundingInflow(usdAmount: number, projectId?: string): Promise<ApiResponse<unknown>> {
    let q = `?usdAmount=${encodeURIComponent(String(usdAmount))}`;
    if (projectId) q += `&projectId=${encodeURIComponent(projectId)}`;
    return this.client.post<unknown>(`/api/Treasury/funding-inflow${q}`, {});
  }

  applyIndexReturn(): Promise<ApiResponse<unknown>> {
    return this.client.post<unknown>("/api/Treasury/apply-index-return", {});
  }

  rebalance(requiredLiquid: number): Promise<ApiResponse<unknown>> {
    return this.client.post<unknown>(`/api/Treasury/rebalance?requiredLiquid=${encodeURIComponent(String(requiredLiquid))}`, {});
  }

  reconcile(): Promise<ApiResponse<unknown>> {
    return this.client.post<unknown>("/api/Treasury/reconcile", {});
  }
}
