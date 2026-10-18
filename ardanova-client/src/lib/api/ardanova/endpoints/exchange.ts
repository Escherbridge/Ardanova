import { apiPath, query } from "../../path";
import { type BaseApiClient, type ApiResponse } from "../../base-client";
import type { ConversionPreviewDto } from "./token-balances";
import type { TreasuryStatusDto } from "./treasury";

/** Exchange rate & conversion helpers; maps to `TokenBalanceController` + `TreasuryController` exchange routes. */
export class ExchangeEndpoint {
  constructor(private client: BaseApiClient) {}

  getProjectTokenValue(configId: string): Promise<ApiResponse<number>> {
    return this.client.get<number>(
      apiPath`/api/TokenBalance/exchange/project-token-value/${configId}`,
    );
  }

  getArdaValue(): Promise<ApiResponse<number>> {
    return this.client.get<number>("/api/TokenBalance/exchange/arda-value");
  }

  getConversionPreview(
    projectTokenConfigId: string,
    tokenAmount: number,
  ): Promise<ApiResponse<ConversionPreviewDto>> {
    const q = query({ projectTokenConfigId, tokenAmount });
    return this.client.get<ConversionPreviewDto>(
      apiPath`/api/TokenBalance/exchange/conversion-preview${q}`,
    );
  }

  getTreasuryStatus(): Promise<ApiResponse<TreasuryStatusDto>> {
    return this.client.get<TreasuryStatusDto>(
      "/api/Treasury/exchange/treasury-status",
    );
  }
}
