import { apiPath, query } from "../../path";
import { type BaseApiClient, type ApiResponse } from "../../base-client";

// ============ Type Definitions ============

export interface XPEventDto {
  id: string;
  userId: string;
  eventType: string;
  amount: number;
  source: string;
  sourceId?: string;
  metadata?: string;
  createdAt: string;
}

export interface AwardXPDto {
  userId: string;
  eventType: string;
  amount: number;
  source: string;
  sourceId?: string;
  metadata?: string;
}

export interface XPSummaryDto {
  userId: string;
  totalXP: number;
  level: number;
  tier: string;
  xpForCurrentLevel: number;
  xpForNextLevel: number;
  progressPercent: number;
}

export interface LevelInfoDto {
  level: number;
  xpRequired: number;
}

export interface XPRewardsConfigDto {
  rewards: Record<string, number>;
}

// ============ XP Events Endpoint ============

export class XPEventsEndpoint {
  constructor(private client: BaseApiClient) {}

  getTotalXP(userId: string): Promise<ApiResponse<number>> {
    return this.client.get<number>(apiPath`/api/xpevents/user/${userId}/total`);
  }

  getHistory(
    userId: string,
    eventType?: string,
    limit?: number,
    offset?: number,
  ): Promise<ApiResponse<XPEventDto[]>> {
    const queryString = query({ eventType: eventType || undefined, limit, offset });
    return this.client.get<XPEventDto[]>(apiPath`/api/xpevents/user/${userId}/history${queryString}`);
  }

  getXPByEventType(userId: string, eventType: string): Promise<ApiResponse<number>> {
    return this.client.get<number>(apiPath`/api/xpevents/user/${userId}/by-type/${eventType}`);
  }

  getSummary(userId: string): Promise<ApiResponse<XPSummaryDto>> {
    return this.client.get<XPSummaryDto>(apiPath`/api/xpevents/user/${userId}/summary`);
  }

  getRewardsConfig(): Promise<ApiResponse<XPRewardsConfigDto>> {
    return this.client.get<XPRewardsConfigDto>("/api/xpevents/rewards-config");
  }

  getLevelInfo(level: number): Promise<ApiResponse<LevelInfoDto>> {
    return this.client.get<LevelInfoDto>(apiPath`/api/xpevents/level-info/${level}`);
  }

  award(data: AwardXPDto): Promise<ApiResponse<XPEventDto>> {
    return this.client.post<XPEventDto>("/api/xpevents/award", data);
  }
}
