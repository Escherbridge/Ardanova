import { type BaseApiClient, type ApiResponse } from "../../base-client";
import type { TokenHolderClass } from "./project-tokens";

export type PayoutStatus = "PENDING" | "PROCESSING" | "COMPLETED" | "FAILED" | "CANCELLED";

export interface PayoutRequestDto {
  id: string;
  userId: string;
  status: PayoutStatus;
  [key: string]: unknown;
}

export interface CreatePayoutRequestDto {
  sourceProjectTokenConfigId?: string;
  sourceTokenAmount: number;
  holderClass: TokenHolderClass | string;
}

export class PayoutsEndpoint {
  constructor(private client: BaseApiClient) {}

  requestPayout(userId: string, dto: CreatePayoutRequestDto): Promise<ApiResponse<PayoutRequestDto>> {
    const q = `?userId=${encodeURIComponent(userId)}`;
    return this.client.post<PayoutRequestDto>(`/api/Payouts${q}`, dto);
  }

  processPayout(payoutRequestId: string): Promise<ApiResponse<PayoutRequestDto>> {
    return this.client.post<PayoutRequestDto>(
      `/api/Payouts/${encodeURIComponent(payoutRequestId)}/process`,
      {},
    );
  }

  cancelPayout(payoutRequestId: string): Promise<ApiResponse<PayoutRequestDto>> {
    return this.client.post<PayoutRequestDto>(
      `/api/Payouts/${encodeURIComponent(payoutRequestId)}/cancel`,
      {},
    );
  }

  getPayoutsByUser(userId: string): Promise<ApiResponse<PayoutRequestDto[]>> {
    return this.client.get<PayoutRequestDto[]>(`/api/Payouts/by-user/${encodeURIComponent(userId)}`);
  }

  getPendingPayouts(): Promise<ApiResponse<PayoutRequestDto[]>> {
    return this.client.get<PayoutRequestDto[]>("/api/Payouts/pending");
  }
}
