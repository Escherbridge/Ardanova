import { apiPath } from "../../path";
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
    return this.client.get<Activity>(apiPath`/api/activities/${id}`);
  }

  getByUserId(userId: string): Promise<ApiResponse<Activity[]>> {
    return this.client.get<Activity[]>(apiPath`/api/activities/user/${userId}`);
  }

  getByUserIdPaged(userId: string, page = 1, pageSize = 10): Promise<ApiResponse<PagedResult<Activity>>> {
    const q = new URLSearchParams({ page: String(page), pageSize: String(pageSize) });
    return this.client.get<PagedResult<Activity>>(
      apiPath`/api/activities/user/${userId}/paged?${q}`
    );
  }

  getByProjectId(projectId: string): Promise<ApiResponse<Activity[]>> {
    return this.client.get<Activity[]>(apiPath`/api/activities/project/${projectId}`);
  }

  getByProjectIdPaged(projectId: string, page = 1, pageSize = 10): Promise<ApiResponse<PagedResult<Activity>>> {
    const q = new URLSearchParams({ page: String(page), pageSize: String(pageSize) });
    return this.client.get<PagedResult<Activity>>(
      apiPath`/api/activities/project/${projectId}/paged?${q}`
    );
  }

  create(data: CreateActivityDto): Promise<ApiResponse<Activity>> {
    return this.client.post<Activity>("/api/activities", data);
  }

  delete(id: string): Promise<ApiResponse<void>> {
    return this.client.delete(apiPath`/api/activities/${id}`);
  }
}
