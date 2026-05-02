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
  balances: ProjectTokenBalanceDto[];
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
    userId: string,
    projectTokenConfigId: string,
    holderClass: TokenHolderClass | string,
  ): Promise<ApiResponse<TokenBalanceDto>> {
    const q = `?projectTokenConfigId=${encodeURIComponent(projectTokenConfigId)}&holderClass=${encodeURIComponent(holderClass)}`;
    return this.client.get<TokenBalanceDto>(`/api/TokenBalance/${encodeURIComponent(userId)}/balance${q}`);
  }

  getArdaBalance(userId: string): Promise<ApiResponse<unknown>> {
    return this.client.get<unknown>(`/api/TokenBalance/${encodeURIComponent(userId)}/arda`);
  }

  getPortfolio(userId: string): Promise<ApiResponse<UserPortfolioDto>> {
    return this.client.get<UserPortfolioDto>(`/api/TokenBalance/${encodeURIComponent(userId)}/portfolio`);
  }

  checkLiquidity(
    userId: string,
    projectTokenConfigId: string,
    holderClass: TokenHolderClass | string,
  ): Promise<ApiResponse<boolean>> {
    const q = `?projectTokenConfigId=${encodeURIComponent(projectTokenConfigId)}&holderClass=${encodeURIComponent(holderClass)}`;
    return this.client.get<boolean>(`/api/TokenBalance/${encodeURIComponent(userId)}/liquidity${q}`);
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
