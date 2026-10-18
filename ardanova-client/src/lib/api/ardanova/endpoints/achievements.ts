import { apiPath } from "../../path";
import { type BaseApiClient, type ApiResponse } from "../../base-client";

// ============ Type Definitions ============

export interface AchievementDto {
  id: string;
  name: string;
  description: string;
  category: string;
  criteria: string;
  xpReward: number;
  equityReward?: number;
  rarity: string;
  icon?: string;
  isActive: boolean;
  createdAt: string;
}

export interface CreateAchievementDto {
  name: string;
  description: string;
  category: string;
  criteria: string;
  xpReward: number;
  equityReward?: number;
  rarity?: string;
  icon?: string;
}

export interface UpdateAchievementDto {
  name?: string;
  description?: string;
  criteria?: string;
  xpReward?: number;
  equityReward?: number;
  rarity?: string;
  icon?: string;
  isActive?: boolean;
}

export interface UserAchievementDto {
  id: string;
  userId: string;
  achievementId: string;
  progress: number;
  earnedAt?: string;
}

export interface UpdateProgressDto {
  progress: number;
}

// ============ Achievements Endpoint ============

export class AchievementsEndpoint {
  constructor(private client: BaseApiClient) {}

  getAll(): Promise<ApiResponse<AchievementDto[]>> {
    return this.client.get<AchievementDto[]>("/api/achievements");
  }

  getById(id: string): Promise<ApiResponse<AchievementDto>> {
    return this.client.get<AchievementDto>(apiPath`/api/achievements/${id}`);
  }

  getByCategory(category: string): Promise<ApiResponse<AchievementDto[]>> {
    return this.client.get<AchievementDto[]>(
      apiPath`/api/achievements/category/${category}`,
    );
  }

  create(data: CreateAchievementDto): Promise<ApiResponse<AchievementDto>> {
    return this.client.post<AchievementDto>("/api/achievements", data);
  }

  update(
    id: string,
    data: UpdateAchievementDto,
  ): Promise<ApiResponse<AchievementDto>> {
    return this.client.put<AchievementDto>(
      apiPath`/api/achievements/${id}`,
      data,
    );
  }

  delete(id: string): Promise<ApiResponse<boolean>> {
    return this.client.delete<boolean>(apiPath`/api/achievements/${id}`);
  }

  getUserAchievements(
    userId: string,
  ): Promise<ApiResponse<UserAchievementDto[]>> {
    return this.client.get<UserAchievementDto[]>(
      apiPath`/api/achievements/user/${userId}`,
    );
  }

  updateProgress(
    userId: string,
    achievementId: string,
    data: UpdateProgressDto,
  ): Promise<ApiResponse<UserAchievementDto>> {
    return this.client.put<UserAchievementDto>(
      apiPath`/api/achievements/user/${userId}/${achievementId}/progress`,
      data,
    );
  }

  awardAchievement(
    userId: string,
    achievementId: string,
  ): Promise<ApiResponse<UserAchievementDto>> {
    return this.client.post<UserAchievementDto>(
      apiPath`/api/achievements/user/${userId}/${achievementId}/award`,
    );
  }
}
