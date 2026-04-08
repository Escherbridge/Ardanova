import { type BaseApiClient, type ApiResponse, type PagedResult } from "../../base-client";

export interface Task {
  id: string;
  projectId: string;
  title: string;
  description?: string | null;
  status: string;
  priority?: string | null;
  type?: string | null;
  assigneeId?: string | null;
  createdById: string;
  createdAt: string;
  updatedAt: string;
  [key: string]: unknown;
}

export interface TaskUser {
  id: string;
  name?: string | null;
  email: string;
  image?: string | null;
}

export interface TaskProject {
  id: string;
  title: string;
  slug: string;
}

export interface CreateTaskDto {
  projectId: string;
  title: string;
  description?: string;
  priority?: string;
  /** Task type (backend TaskType enum, e.g. FEATURE, BUG) */
  taskType?: string;
  type?: string;
  assigneeId?: string | null;
  assignedToId?: string | null;
  pbiId?: string;
  estimatedHours?: number;
  dueDate?: string;
  equityReward?: number;
  [key: string]: unknown;
}

export interface UpdateTaskDto {
  title?: string;
  description?: string;
  priority?: string;
  type?: string;
  assigneeId?: string | null;
  [key: string]: unknown;
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

  delete(id: string): Promise<ApiResponse<void>> {
    return this.client.delete(`/api/tasks/${id}`);
  }
}
