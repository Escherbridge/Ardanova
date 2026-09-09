import { type ApiResponse, type BaseApiClient } from "../../base-client";

/** How much to add, in whole cents. Dollars never cross the wire as a decimal. */
export interface BeginAddFundsRequest {
  amountMinorUnits: number;
}

/**
 * One add-funds payment. `status` and `settlementState` are the server's own words and are never
 * rendered: `~/lib/commerce/on-ramp-status` is the only thing allowed to turn them into copy.
 */
export interface AddFundsPayment {
  /** The same value the return address carries back as `?intent=`. */
  paymentId: string;
  status: string;
  settlementState: string;
  /** The hosted payment page, while this payment is still open. */
  paymentUrl?: string | null;
  /** True when the server resumed an open payment rather than starting a second one. */
  resumed: boolean;
  /** The amount that actually cleared. Null until it has. */
  amountPaidMinorUnits?: number | null;
  currencyCode?: string | null;
  expiresAt?: string | null;
}

/**
 * Actor-signed add-funds routes. The account paying is derived from the signed actor on the
 * server, so nothing here carries an account id; a browser redirect never reports success on its
 * own, so the return page reads `getPayment` instead of trusting the URL it came back on.
 */
export class AddFundsEndpoint {
  constructor(private readonly client: BaseApiClient) {}

  /**
   * Starts a payment. `requestKey` must be STABLE across retries of the same attempt: the server
   * resumes the open payment for a repeated key, so a fresh key per tap opens a second payment.
   */
  begin(
    request: BeginAddFundsRequest,
    requestKey: string,
  ): Promise<ApiResponse<AddFundsPayment>> {
    return this.client.post<AddFundsPayment>("/api/add-funds", request, {
      "X-Idempotency-Key": requestKey,
    });
  }

  /** Reads one payment belonging to the signed-in person. Another person's id reads nothing. */
  getPayment(paymentId: string): Promise<ApiResponse<AddFundsPayment>> {
    return this.client.get<AddFundsPayment>(
      `/api/add-funds/${encodeURIComponent(paymentId)}`,
    );
  }
}
