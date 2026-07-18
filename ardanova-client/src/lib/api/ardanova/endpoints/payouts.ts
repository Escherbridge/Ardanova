import { type BaseApiClient, type ApiResponse } from "../../base-client";
import type { ProjectGateStatus, TokenHolderClass } from "./project-tokens";

export type PayoutStatus =
  | "PENDING"
  | "PROCESSING"
  | "COMPLETED"
  | "FAILED"
  | "CANCELLED";

export interface PayoutRequestDto {
  id: string;
  userId: string;
  sourceProjectTokenConfigId: string | null;
  sourceTokenAmount: number;
  ardaTokenAmount: number | null;
  usdAmount: number | null;
  status: PayoutStatus;
  holderClass: TokenHolderClass;
  gateStatusAtRequest: ProjectGateStatus;
  conversionTxHash: string | null;
  payoutTxHash: string | null;
  stripePayoutId: string | null;
  failureReason: string | null;
  requestedAt: string;
  processedAt: string | null;
  completedAt: string | null;
}

export interface CreatePayoutRequestDto {
  sourceProjectTokenConfigId: string;
  sourceTokenAmount: number;
  holderClass: TokenHolderClass;
}

export class PayoutsEndpoint {
  constructor(private client: BaseApiClient) {}

  requestPayout(
    dto: CreatePayoutRequestDto,
  ): Promise<ApiResponse<PayoutRequestDto>> {
    return this.client.post<PayoutRequestDto>("/api/Payouts", dto);
  }

  processPayout(
    payoutRequestId: string,
  ): Promise<ApiResponse<PayoutRequestDto>> {
    return this.client.post<PayoutRequestDto>(
      `/api/Payouts/${encodeURIComponent(payoutRequestId)}/process`,
      {},
    );
  }

  cancelPayout(
    payoutRequestId: string,
  ): Promise<ApiResponse<PayoutRequestDto>> {
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
