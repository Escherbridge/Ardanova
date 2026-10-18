import { apiPath } from "../../path";
import { type BaseApiClient, type ApiResponse } from "../../base-client";

// ============ Type Definitions ============

export interface Referral {
  id: string;
  referrerId: string;
  referredId: string;
  referralCode?: string;
  status: string;
  rewardClaimed: boolean;
  xpRewarded?: number;
  equityRewarded?: number;
  createdAt: string;
  completedAt?: string;
}

export interface CreateReferralDto {
  referrerId: string;
  referredId: string;
  referralCode?: string;
}

export interface ClaimReferralRewardDto {
  xpAmount: number;
  equityAmount?: number;
  tokenAmount?: number;
}

// ============ Referrals Endpoint ============

export class ReferralsEndpoint {
  constructor(private client: BaseApiClient) {}

  getById(id: string): Promise<ApiResponse<Referral>> {
    return this.client.get<Referral>(apiPath`/api/referrals/${id}`);
  }

  getByReferrerId(referrerId: string): Promise<ApiResponse<Referral[]>> {
    return this.client.get<Referral[]>(
      apiPath`/api/referrals/referrer/${referrerId}`,
    );
  }

  getByReferredId(referredId: string): Promise<ApiResponse<Referral>> {
    return this.client.get<Referral>(
      apiPath`/api/referrals/referred/${referredId}`,
    );
  }

  getByCode(code: string): Promise<ApiResponse<Referral>> {
    return this.client.get<Referral>(apiPath`/api/referrals/code/${code}`);
  }

  create(data: CreateReferralDto): Promise<ApiResponse<Referral>> {
    return this.client.post<Referral>("/api/referrals", data);
  }

  complete(id: string): Promise<ApiResponse<Referral>> {
    return this.client.post<Referral>(
      apiPath`/api/referrals/${id}/complete`,
      {},
    );
  }

  claimReward(
    id: string,
    data: ClaimReferralRewardDto,
  ): Promise<ApiResponse<Referral>> {
    return this.client.post<Referral>(
      apiPath`/api/referrals/${id}/claim`,
      data,
    );
  }

  expire(id: string): Promise<ApiResponse<Referral>> {
    return this.client.post<Referral>(apiPath`/api/referrals/${id}/expire`, {});
  }

  cancel(id: string): Promise<ApiResponse<Referral>> {
    return this.client.post<Referral>(apiPath`/api/referrals/${id}/cancel`, {});
  }
}
