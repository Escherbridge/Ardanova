import { type BaseApiClient, type ApiResponse } from "../../base-client";

export type EpicStatus = "PLANNED" | "IN_PROGRESS" | "COMPLETED" | "CANCELLED";
export type EpicPriority = "CRITICAL" | "HIGH" | "MEDIUM" | "LOW";

export interface Epic {
  id: string;
  projectId: string;
  milestoneId: string;
  title: string;
  description?: string | null;
  priority: EpicPriority;
  status: EpicStatus;
  equityBudget?: number | null;
  progress: number;
  startDate?: string | null;
  targetDate?: string | null;
  assigneeId?: string | null;
  createdAt: string;
  updatedAt: string;
}

export interface CreateEpic {
  milestoneId: string;
  title: string;
  description?: string;
  priority?: EpicPriority;
  equityBudget?: number;
  startDate?: string;
  targetDate?: string;
  assigneeId?: string;
}

export interface UpdateEpic {
  title?: string;
  description?: string;
  priority?: EpicPriority;
  status?: EpicStatus;
  equityBudget?: number;
  progress?: number;
  startDate?: string;
  targetDate?: string;
}

export class EpicsEndpoint {
  constructor(private client: BaseApiClient) {}

  getByMilestoneId(milestoneId: string): Promise<ApiResponse<Epic[]>> {
    return this.client.get<Epic[]>(`/api/milestones/${milestoneId}/epics`);
  }

  getByProjectId(projectId: string): Promise<ApiResponse<Epic[]>> {
    return this.client.get<Epic[]>(`/api/projects/${projectId}/epics`);
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

  updatePriority(
    id: string,
    priority: EpicPriority,
  ): Promise<ApiResponse<Epic>> {
    return this.client.put<Epic>(`/api/epics/${id}/priority`, { priority });
  }

  delete(id: string): Promise<ApiResponse<void>> {
    return this.client.delete(`/api/epics/${id}`);
  }
}
