import { type BaseApiClient, type ApiResponse } from "../../base-client";

export interface TaskEscrow {
  id: string;
  taskId: string;
  funderId: string;
  shareId: string;
  amount: number;
  status: string;
  txHashFund?: string | null;
  txHashRelease?: string | null;
  txHashRefund?: string | null;
  createdAt: string;
  fundedAt?: string | null;
  releasedAt?: string | null;
  refundedAt?: string | null;
  [key: string]: unknown;
}

export interface CreateTaskEscrowDto {
  taskId: string;
  funderId: string;
  shareId: string;
  amount: number;
  txHashFund?: string;
  [key: string]: unknown;
}

export interface ReleaseEscrowDto {
  txHash?: string;
  [key: string]: unknown;
}

export interface RefundEscrowDto {
  txHash?: string;
  [key: string]: unknown;
}

export class TaskEscrowsEndpoint {
  constructor(private client: BaseApiClient) {}

  getById(id: string): Promise<ApiResponse<TaskEscrow>> {
    return this.client.get<TaskEscrow>(`/api/taskescrows/${encodeURIComponent(id)}`);
  }

  getByTaskId(taskId: string): Promise<ApiResponse<TaskEscrow>> {
    return this.client.get<TaskEscrow>(`/api/taskescrows/task/${encodeURIComponent(taskId)}`);
  }

  getByFunderId(funderId: string): Promise<ApiResponse<TaskEscrow[]>> {
    return this.client.get<TaskEscrow[]>(`/api/taskescrows/funder/${encodeURIComponent(funderId)}`);
  }

  create(data: CreateTaskEscrowDto): Promise<ApiResponse<TaskEscrow>> {
    return this.client.post<TaskEscrow>("/api/taskescrows", data);
  }

  release(id: string, data: ReleaseEscrowDto): Promise<ApiResponse<TaskEscrow>> {
    return this.client.post<TaskEscrow>(`/api/taskescrows/${encodeURIComponent(id)}/release`, data);
  }

  dispute(id: string): Promise<ApiResponse<TaskEscrow>> {
    return this.client.post<TaskEscrow>(`/api/taskescrows/${encodeURIComponent(id)}/dispute`, {});
  }

  refund(id: string, data: RefundEscrowDto): Promise<ApiResponse<TaskEscrow>> {
    return this.client.post<TaskEscrow>(`/api/taskescrows/${encodeURIComponent(id)}/refund`, data);
  }
}
