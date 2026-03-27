import { type BaseApiClient, type ApiResponse } from "../../base-client";

// ============ Type Definitions ============

export interface UserStreak {
  id: string;
  userId: string;
  currentStreak: number;
  longestStreak: number;
  lastActivityDate?: string;
  streakType: string;
  createdAt: string;
  updatedAt: string;
}

export interface CreateUserStreakDto {
  userId: string;
  streakType?: string;
}

// ============ Streaks Endpoint ============

export class StreaksEndpoint {
  constructor(private client: BaseApiClient) {}

  getById(id: string): Promise<ApiResponse<UserStreak>> {
    return this.client.get<UserStreak>(`/api/userstreaks/${id}`);
  }

  getByUserId(userId: string): Promise<ApiResponse<UserStreak>> {
    return this.client.get<UserStreak>(`/api/userstreaks/user/${userId}`);
  }

  create(data: CreateUserStreakDto): Promise<ApiResponse<UserStreak>> {
    return this.client.post<UserStreak>("/api/userstreaks", data);
  }

  recordActivity(userId: string): Promise<ApiResponse<UserStreak>> {
    return this.client.post<UserStreak>(
      `/api/userstreaks/user/${userId}/record`,
      {},
    );
  }

  resetStreak(userId: string): Promise<ApiResponse<UserStreak>> {
    return this.client.post<UserStreak>(
      `/api/userstreaks/user/${userId}/reset`,
      {},
    );
  }

  delete(id: string): Promise<ApiResponse<void>> {
    return this.client.delete(`/api/userstreaks/${id}`);
  }
}
