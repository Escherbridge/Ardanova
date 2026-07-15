import { type BaseApiClient, type ApiResponse } from "../../base-client";
import type { TokenHolderClass } from "./project-tokens";

export interface TokenBalanceDto {
  userId: string;
  projectTokenConfigId: string;
  holderClass: string;
  balance: number;
  [key: string]: unknown;
}

export interface ProjectTokenBalanceDto {
  projectTokenConfigId: string;
  balance: number;
  [key: string]: unknown;
}

export interface UserPortfolioDto {
  userId: string;
  holdings: ProjectTokenBalanceDto[];
  [key: string]: unknown;
}

export interface ConversionPreviewDto {
  projectTokenAmount?: number;
  ardaEquivalent?: number;
  [key: string]: unknown;
}

export class TokenBalancesEndpoint {
  constructor(private client: BaseApiClient) {}

  getBalance(
    projectTokenConfigId: string,
    holderClass: TokenHolderClass | string,
  ): Promise<ApiResponse<TokenBalanceDto>> {
    const q = `?projectTokenConfigId=${encodeURIComponent(projectTokenConfigId)}&holderClass=${encodeURIComponent(holderClass)}`;
    return this.client.get<TokenBalanceDto>(`/api/TokenBalance/me/balance${q}`);
  }

  getArdaBalance(): Promise<ApiResponse<unknown>> {
    return this.client.get<unknown>("/api/TokenBalance/me/arda");
  }

  getPortfolio(): Promise<ApiResponse<UserPortfolioDto>> {
    return this.client.get<UserPortfolioDto>("/api/TokenBalance/me/portfolio");
  }

  checkLiquidity(
    projectTokenConfigId: string,
    holderClass: TokenHolderClass | string,
  ): Promise<ApiResponse<boolean>> {
    const q = `?projectTokenConfigId=${encodeURIComponent(projectTokenConfigId)}&holderClass=${encodeURIComponent(holderClass)}`;
    return this.client.get<boolean>(`/api/TokenBalance/me/liquidity${q}`);
  }

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
}
