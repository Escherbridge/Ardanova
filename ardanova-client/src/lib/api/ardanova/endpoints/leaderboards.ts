import { type BaseApiClient, type ApiResponse } from "../../base-client";

// ============ Type Definitions ============

export interface LeaderboardDto {
  id: string;
  period: string;
  category: string;
  startDate: string;
  endDate: string;
  createdAt: string;
}

export interface CreateLeaderboardDto {
  period: string;
  category: string;
  startDate: string;
  endDate: string;
}

export interface LeaderboardEntryDto {
  id: string;
  leaderboardId: string;
  userId: string;
  rank: number;
  score: number;
  metadata?: string;
}

export interface CreateLeaderboardEntryDto {
  leaderboardId: string;
  userId: string;
  score: number;
  metadata?: string;
}

export interface UpdateLeaderboardEntryDto {
  score?: number;
  rank?: number;
  metadata?: string;
}

// ============ Leaderboards Endpoint ============

export class LeaderboardsEndpoint {
  constructor(private client: BaseApiClient) {}

  getById(id: string): Promise<ApiResponse<LeaderboardDto>> {
    return this.client.get<LeaderboardDto>(`/api/leaderboards/${id}`);
  }

  getByPeriod(period: string): Promise<ApiResponse<LeaderboardDto[]>> {
    return this.client.get<LeaderboardDto[]>(
      `/api/leaderboards/period/${encodeURIComponent(period)}`,
    );
  }

  getByCategory(category: string): Promise<ApiResponse<LeaderboardDto[]>> {
    return this.client.get<LeaderboardDto[]>(
      `/api/leaderboards/category/${encodeURIComponent(category)}`,
    );
  }

  create(data: CreateLeaderboardDto): Promise<ApiResponse<LeaderboardDto>> {
    return this.client.post<LeaderboardDto>("/api/leaderboards", data);
  }

  delete(id: string): Promise<ApiResponse<void>> {
    return this.client.delete(`/api/leaderboards/${id}`);
  }

  getEntries(
    leaderboardId: string,
  ): Promise<ApiResponse<LeaderboardEntryDto[]>> {
    return this.client.get<LeaderboardEntryDto[]>(
      `/api/leaderboards/${leaderboardId}/entries`,
    );
  }

  addEntry(
    data: CreateLeaderboardEntryDto,
  ): Promise<ApiResponse<LeaderboardEntryDto>> {
    return this.client.post<LeaderboardEntryDto>(
      "/api/leaderboards/entries",
      data,
    );
  }

  updateEntry(
    entryId: string,
    data: UpdateLeaderboardEntryDto,
  ): Promise<ApiResponse<LeaderboardEntryDto>> {
    return this.client.put<LeaderboardEntryDto>(
      `/api/leaderboards/entries/${entryId}`,
      data,
    );
  }

  deleteEntry(entryId: string): Promise<ApiResponse<void>> {
    return this.client.delete(`/api/leaderboards/entries/${entryId}`);
  }

  getUserRankings(
    userId: string,
  ): Promise<ApiResponse<LeaderboardEntryDto[]>> {
    return this.client.get<LeaderboardEntryDto[]>(
      `/api/leaderboards/user/${userId}/rankings`,
    );
  }
}
