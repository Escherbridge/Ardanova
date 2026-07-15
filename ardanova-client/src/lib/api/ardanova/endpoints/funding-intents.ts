import { type ApiResponse, type BaseApiClient } from "../../base-client";

export interface CreateFundingIntentRequest {
  projectTokenConfigId: string;
  amount: string;
  disclosureVersion: string;
}

export interface FundingCheckout {
  intentId: string;
  checkoutUrl: string;
}

export interface FundingIntentStatus {
  intentId: string;
  projectTokenConfigId: string;
  currencyCode: string;
  amount: string;
  status: string;
  paymentVerifiedAt?: string | null;
}

/** Actor-signed funding intent routes; browser redirects never report payment success. */
export class FundingIntentsEndpoint {
  constructor(private readonly client: BaseApiClient) {}

  createCheckout(
    request: CreateFundingIntentRequest,
    idempotencyKey: string,
  ): Promise<ApiResponse<FundingCheckout>> {
    return this.client.post<FundingCheckout>(
      "/api/funding-intents/checkout",
      request,
      { "X-Idempotency-Key": idempotencyKey },
    );
  }

  getStatus(intentId: string): Promise<ApiResponse<FundingIntentStatus>> {
    return this.client.get<FundingIntentStatus>(`/api/funding-intents/${encodeURIComponent(intentId)}`);
  }
}
