import { type BaseApiClient, type ApiResponse, type PagedResult } from "../../base-client";

// ============ Type Definitions ============

export interface Business {
  id: string;
  name: string;
  description?: string;
  industry?: string;
  plan: string;
  isActive: boolean;
  ownerId: string;
  createdAt: string;
  updatedAt: string;
}

export interface CreateBusinessDto {
  name: string;
  description?: string;
  industry?: string;
  ownerId: string;
}

export interface UpdateBusinessDto {
  name?: string;
  description?: string;
  industry?: string;
}

// ============ Businesses Endpoint ============

export class BusinessesEndpoint {
  constructor(private client: BaseApiClient) {}

  getById(id: string): Promise<ApiResponse<Business>> {
    return this.client.get<Business>(`/api/businesses/${id}`);
  }

  getByOwner(ownerId: string): Promise<ApiResponse<Business[]>> {
    return this.client.get<Business[]>(`/api/businesses/owner/${ownerId}`);
  }

  getAll(page = 1, pageSize = 10): Promise<ApiResponse<PagedResult<Business>>> {
    return this.client.get<PagedResult<Business>>(`/api/businesses?page=${page}&pageSize=${pageSize}`);
  }

  create(data: CreateBusinessDto): Promise<ApiResponse<Business>> {
    return this.client.post<Business>("/api/businesses", data);
  }

  update(id: string, data: UpdateBusinessDto): Promise<ApiResponse<Business>> {
    return this.client.put<Business>(`/api/businesses/${id}`, data);
  }

  delete(id: string): Promise<ApiResponse<void>> {
    return this.client.delete(`/api/businesses/${id}`);
  }

  upgradePlan(id: string, plan: string): Promise<ApiResponse<Business>> {
    return this.client.post<Business>(`/api/businesses/${id}/upgrade`, { plan });
  }
}
