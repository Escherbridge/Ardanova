import { type BaseApiClient, type ApiResponse } from "../../base-client";

export type EpicStatus = "PLANNED" | "IN_PROGRESS" | "COMPLETED" | "CANCELLED";
export type EpicPriority = "CRITICAL" | "HIGH" | "MEDIUM" | "LOW";

export interface Epic {
  id: string;
  projectId: string;
  milestoneId?: string | null;
  guildId?: string | null;
  title: string;
  description?: string | null;
  priority: EpicPriority;
  status: EpicStatus;
  startDate?: string | null;
  endDate?: string | null;
  assigneeId?: string | null;
  order?: number | null;
  createdAt: string;
  updatedAt: string;
  [key: string]: unknown;
}

export interface CreateEpic {
  milestoneId: string;
  title: string;
  description?: string;
  priority?: EpicPriority;
  startDate?: string;
  endDate?: string;
  [key: string]: unknown;
}

export interface UpdateEpic {
  title?: string;
  description?: string;
  priority?: EpicPriority;
  status?: EpicStatus;
  startDate?: string;
  endDate?: string;
  assigneeId?: string | null;
  [key: string]: unknown;
}

export class EpicsEndpoint {
  constructor(private client: BaseApiClient) {}

  getByMilestoneId(milestoneId: string): Promise<ApiResponse<Epic[]>> {
    return this.client.get<Epic[]>(`/api/milestones/${milestoneId}/epics`);
  }

  getById(id: string): Promise<ApiResponse<Epic>> {
    return this.client.get<Epic>(`/api/epics/${id}`);
  }

  create(data: CreateEpic): Promise<ApiResponse<Epic>> {
    return this.client.post<Epic>("/api/epics", data);
  }

  update(id: string, data: UpdateEpic): Promise<ApiResponse<Epic>> {
    return this.client.put<Epic>(`/api/epics/${id}`, data);
  }

  assign(id: string, userId: string | null): Promise<ApiResponse<Epic>> {
    return this.client.put<Epic>(`/api/epics/${id}/assign`, { userId });
  }

  updateStatus(id: string, status: EpicStatus): Promise<ApiResponse<Epic>> {
    return this.client.put<Epic>(`/api/epics/${id}/status`, { status });
  }

  updatePriority(id: string, priority: EpicPriority): Promise<ApiResponse<Epic>> {
    return this.client.put<Epic>(`/api/epics/${id}/priority`, { priority });
  }

  delete(id: string): Promise<ApiResponse<void>> {
    return this.client.delete(`/api/epics/${id}`);
  }
}
