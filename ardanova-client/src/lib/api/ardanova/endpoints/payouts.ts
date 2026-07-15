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

  requestPayout(dto: CreatePayoutRequestDto): Promise<ApiResponse<PayoutRequestDto>> {
    return this.client.post<PayoutRequestDto>("/api/Payouts", dto);
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

  getMine(): Promise<ApiResponse<PayoutRequestDto[]>> {
    return this.client.get<PayoutRequestDto[]>("/api/Payouts/me");
  }

  getPendingPayouts(): Promise<ApiResponse<PayoutRequestDto[]>> {
    return this.client.get<PayoutRequestDto[]>("/api/Payouts/pending");
  }
}
