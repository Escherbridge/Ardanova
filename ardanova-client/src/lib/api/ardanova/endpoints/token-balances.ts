import { type BaseApiClient, type ApiResponse } from "../../base-client";
import type { TokenHolderClass } from "./project-tokens";

export interface TokenBalanceDto {
  id: string;
  userId: string;
  projectTokenConfigId: string | null;
  isPlatformToken: boolean;
  holderClass: TokenHolderClass | null;
  isLiquid: boolean;
  balance: number;
  lockedBalance: number;
  updatedAt: string;
  availableBalance: number;
}

/** Compatibility alias; the API uses TokenBalanceDto for every holding. */
export type ProjectTokenBalanceDto = TokenBalanceDto;

export interface UserPortfolioDto {
  userId: string;
  holdings: TokenBalanceDto[];
  ardaBalance: TokenBalanceDto | null;
  totalLiquidValueUsd: number;
  totalLockedValueUsd: number;
  totalPortfolioValueUsd: number;
}

export interface ConversionPreviewDto {
  projectTokenValueUsd: number;
  ardaValueUsd: number;
  sourceTokenAmount: number;
  usdValue: number;
  ardaAmount: number;
}

export class TokenBalancesEndpoint {
  constructor(private client: BaseApiClient) {}

  getBalance(
    projectTokenConfigId: string,
    holderClass: TokenHolderClass,
  ): Promise<ApiResponse<TokenBalanceDto>> {
    const q = `?projectTokenConfigId=${encodeURIComponent(projectTokenConfigId)}&holderClass=${encodeURIComponent(holderClass)}`;
    return this.client.get<TokenBalanceDto>(`/api/TokenBalance/me/balance${q}`);
  }

  getArdaBalance(): Promise<ApiResponse<TokenBalanceDto>> {
    return this.client.get<TokenBalanceDto>("/api/TokenBalance/me/arda");
  }

  getPortfolio(): Promise<ApiResponse<UserPortfolioDto>> {
    return this.client.get<UserPortfolioDto>("/api/TokenBalance/me/portfolio");
  }

  checkLiquidity(
    projectTokenConfigId: string,
    holderClass: TokenHolderClass,
  ): Promise<ApiResponse<boolean>> {
    const q = `?projectTokenConfigId=${encodeURIComponent(projectTokenConfigId)}&holderClass=${encodeURIComponent(holderClass)}`;
    return this.client.get<boolean>(`/api/TokenBalance/me/liquidity${q}`);
  }

  getProjectTokenValue(configId: string): Promise<ApiResponse<number>> {
    return this.client.get<number>(
      `/api/TokenBalance/exchange/project-token-value/${encodeURIComponent(configId)}`,
    );
  }

  getArdaValue(): Promise<ApiResponse<number>> {
    return this.client.get<number>("/api/TokenBalance/exchange/arda-value");
  }

  getConversionPreview(
    projectTokenConfigId: string,
    tokenAmount: number,
  ): Promise<ApiResponse<ConversionPreviewDto>> {
    const q = `?projectTokenConfigId=${encodeURIComponent(projectTokenConfigId)}&tokenAmount=${tokenAmount}`;
    return this.client.get<ConversionPreviewDto>(
      `/api/TokenBalance/exchange/conversion-preview${q}`,
    );
  }
}
