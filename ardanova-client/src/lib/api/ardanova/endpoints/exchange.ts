import { type BaseApiClient, type ApiResponse } from "../../base-client";
import type { ConversionPreviewDto } from "./token-balances";
import type { TreasuryStatusDto } from "./treasury";

/** Exchange rate & conversion helpers; maps to `TokenBalanceController` + `TreasuryController` exchange routes. */
export class ExchangeEndpoint {
  constructor(private client: BaseApiClient) {}

  getProjectTokenValue(configId: string): Promise<ApiResponse<unknown>> {
    return this.client.get<unknown>(`/api/TokenBalance/exchange/project-token-value/${encodeURIComponent(configId)}`);
  }

  getArdaValue(): Promise<ApiResponse<unknown>> {
    return this.client.get<unknown>("/api/TokenBalance/exchange/arda-value");
  }

  getConversionPreview(projectTokenConfigId: string, tokenAmount: number): Promise<ApiResponse<ConversionPreviewDto>> {
    const q = `?projectTokenConfigId=${encodeURIComponent(projectTokenConfigId)}&tokenAmount=${tokenAmount}`;
    return this.client.get<ConversionPreviewDto>(`/api/TokenBalance/exchange/conversion-preview${q}`);
  }

  getTreasuryStatus(): Promise<ApiResponse<TreasuryStatusDto>> {
    return this.client.get<TreasuryStatusDto>("/api/Treasury/exchange/treasury-status");
  }
}
