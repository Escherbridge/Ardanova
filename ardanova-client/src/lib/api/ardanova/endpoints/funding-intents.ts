import { type ApiResponse, type BaseApiClient } from "../../base-client";

export interface CreateFundingIntentRequest {
  projectTokenConfigId: string;
  amount: string;
  disclosureVersion: string;
  /** Whether the sponsor agreed to be named to the project. False means unnamed. */
  displayNameConsent: boolean;
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
  /** ISO date the held stake becomes the sponsor's to move. Server-written; never guessed. */
  releaseNotBefore?: string | null;
  /** Consumer-safe reason text the server already wrote for a held sponsorship. */
  holdingReasonDetail?: string | null;
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
