import { type BaseApiClient, type ApiResponse, type PagedResult } from "../../base-client";

export interface Activity {
  id: string;
  userId: string;
  projectId?: string | null;
  type: string;
  entityType: string;
  entityId: string;
  action: string;
  metadata?: string | null;
  createdAt: string;
  [key: string]: unknown;
}

export interface CreateActivityDto {
  userId: string;
  projectId?: string;
  type: string;
  entityType: string;
  entityId: string;
  action: string;
  metadata?: string;
  [key: string]: unknown;
}

export class ActivitiesEndpoint {
  constructor(private client: BaseApiClient) {}

  getById(id: string): Promise<ApiResponse<Activity>> {
    return this.client.get<Activity>(`/api/activities/${encodeURIComponent(id)}`);
  }

  getByUserId(userId: string): Promise<ApiResponse<Activity[]>> {
    return this.client.get<Activity[]>(`/api/activities/user/${encodeURIComponent(userId)}`);
  }

  getByUserIdPaged(userId: string, page = 1, pageSize = 10): Promise<ApiResponse<PagedResult<Activity>>> {
    const q = new URLSearchParams({ page: String(page), pageSize: String(pageSize) });
    return this.client.get<PagedResult<Activity>>(
      `/api/activities/user/${encodeURIComponent(userId)}/paged?${q.toString()}`
    );
  }

  getByProjectId(projectId: string): Promise<ApiResponse<Activity[]>> {
    return this.client.get<Activity[]>(`/api/activities/project/${encodeURIComponent(projectId)}`);
  }

  getByProjectIdPaged(projectId: string, page = 1, pageSize = 10): Promise<ApiResponse<PagedResult<Activity>>> {
    const q = new URLSearchParams({ page: String(page), pageSize: String(pageSize) });
    return this.client.get<PagedResult<Activity>>(
      `/api/activities/project/${encodeURIComponent(projectId)}/paged?${q.toString()}`
    );
  }

  create(data: CreateActivityDto): Promise<ApiResponse<Activity>> {
    return this.client.post<Activity>("/api/activities", data);
  }

  delete(id: string): Promise<ApiResponse<void>> {
    return this.client.delete(`/api/activities/${encodeURIComponent(id)}`);
  }
}
