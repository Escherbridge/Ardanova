import { type BaseApiClient, type ApiResponse } from "../../base-client";

export type SprintStatus = "PLANNED" | "ACTIVE" | "COMPLETED" | "CANCELLED";

export interface Sprint {
  id: string;
  epicId: string;
  name?: string | null;
  goal?: string | null;
  startDate?: string | null;
  endDate?: string | null;
  status: SprintStatus;
  order?: number | null;
  createdAt: string;
  updatedAt: string;
  [key: string]: unknown;
}

export interface CreateSprint {
  epicId: string;
  name?: string;
  goal?: string;
  startDate?: string;
  endDate?: string;
  [key: string]: unknown;
}

export interface UpdateSprint {
  name?: string;
  goal?: string;
  startDate?: string;
  endDate?: string;
  status?: SprintStatus;
  [key: string]: unknown;
}

export class SprintsEndpoint {
  constructor(private client: BaseApiClient) {}

  getByEpicId(epicId: string): Promise<ApiResponse<Sprint[]>> {
    return this.client.get<Sprint[]>(`/api/epics/${epicId}/sprints`);
  }

  getById(id: string): Promise<ApiResponse<Sprint>> {
    return this.client.get<Sprint>(`/api/sprints/${id}`);
  }

  create(data: CreateSprint): Promise<ApiResponse<Sprint>> {
    return this.client.post<Sprint>("/api/sprints", data);
  }

  update(id: string, data: UpdateSprint): Promise<ApiResponse<Sprint>> {
    return this.client.put<Sprint>(`/api/sprints/${id}`, data);
  }

  delete(id: string): Promise<ApiResponse<void>> {
    return this.client.delete(`/api/sprints/${id}`);
  }

  start(id: string): Promise<ApiResponse<Sprint>> {
    return this.client.post<Sprint>(`/api/sprints/${id}/start`, {});
  }

  complete(id: string): Promise<ApiResponse<Sprint>> {
    return this.client.post<Sprint>(`/api/sprints/${id}/complete`, {});
  }

  cancel(id: string): Promise<ApiResponse<Sprint>> {
    return this.client.post<Sprint>(`/api/sprints/${id}/cancel`, {});
  }
}
