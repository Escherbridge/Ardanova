import { type BaseApiClient, type ApiResponse, type PagedResult } from "../../base-client";

export interface Task {
  id: string;
  projectId: string;
  pbiId?: string | null;
  featureId?: string | null;
  sprintId?: string | null;
  epicId?: string | null;
  milestoneId?: string | null;
  guildId?: string | null;
  title: string;
  description?: string | null;
  status: string;
  priority: string;
  taskType: string;
  effortEstimate?: string | null;
  estimatedHours?: number | null;
  actualHours?: number | null;
  equityReward?: number | null;
  escrowStatus: string;
  dueDate?: string | null;
  completedAt?: string | null;
  createdAt: string;
  updatedAt: string;
  assignedToId?: string | null;
  opportunityId?: string | null;
}

export interface CreateTaskDto {
  projectId: string;
  title: string;
  description?: string;
  priority?: string;
  taskType?: string;
  effortEstimate?: string;
  assignedToId?: string | null;
  pbiId?: string;
  featureId?: string;
  sprintId?: string;
  epicId?: string;
  milestoneId?: string;
  guildId?: string;
  estimatedHours?: number;
  dueDate?: string;
  equityReward?: number;
}

export interface UpdateTaskDto {
  title?: string;
  description?: string;
  priority?: string;
  taskType?: string;
  status?: string;
  effortEstimate?: string;
  assignedToId?: string | null;
  pbiId?: string;
  estimatedHours?: number;
  dueDate?: string;
}

export interface SearchTasksParams {
  searchTerm?: string;
  status?: string;
  priority?: string;
  taskType?: string;
  projectId?: string;
  page?: number;
  pageSize?: number;
}

export class TasksEndpoint {
  constructor(private client: BaseApiClient) {}

  getById(id: string): Promise<ApiResponse<Task>> {
    return this.client.get<Task>(`/api/tasks/${id}`);
  }

  getByUserId(userId: string): Promise<ApiResponse<Task[]>> {
    return this.client.get<Task[]>(`/api/tasks/user/${userId}`);
  }

  search(params: SearchTasksParams = {}): Promise<ApiResponse<PagedResult<Task>>> {
    const sp = new URLSearchParams();
    if (params.searchTerm) sp.set("searchTerm", params.searchTerm);
    if (params.status) sp.set("status", params.status);
    if (params.priority) sp.set("priority", params.priority);
    if (params.taskType) sp.set("taskType", params.taskType);
    if (params.projectId) sp.set("projectId", params.projectId);
    sp.set("page", String(params.page ?? 1));
    sp.set("pageSize", String(params.pageSize ?? 10));
    return this.client.get<PagedResult<Task>>(`/api/tasks/search?${sp.toString()}`);
  }

  create(data: CreateTaskDto): Promise<ApiResponse<Task>> {
    return this.client.post<Task>("/api/tasks", data);
  }

  update(id: string, data: UpdateTaskDto): Promise<ApiResponse<Task>> {
    return this.client.put<Task>(`/api/tasks/${id}`, data);
  }

  updateStatus(id: string, status: string): Promise<ApiResponse<Task>> {
    return this.client.patch<Task>(`/api/tasks/${id}/status`, { status });
  }

  getByPbiId(pbiId: string): Promise<ApiResponse<Task[]>> {
    return this.client.get<Task[]>(`/api/tasks/pbi/${pbiId}`);
  }

  delete(id: string): Promise<ApiResponse<void>> {
    return this.client.delete(`/api/tasks/${id}`);
  }
}
