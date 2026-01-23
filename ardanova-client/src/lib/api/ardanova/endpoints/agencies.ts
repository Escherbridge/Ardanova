import { type BaseApiClient, type ApiResponse, type PagedResult } from "../../base-client";

// ============ Type Definitions ============

export interface Agency {
  id: string;
  name: string;
  slug: string;
  description: string;
  email: string;
  website?: string;
  phone?: string;
  isVerified: boolean;
  ownerId: string;
  createdAt: string;
  updatedAt: string;
}

export interface CreateAgencyDto {
  name: string;
  description: string;
  email: string;
  website?: string;
  phone?: string;
  ownerId: string;
}

export interface UpdateAgencyDto {
  name?: string;
  description?: string;
  email?: string;
  website?: string;
  phone?: string;
}

// ============ Agencies Endpoint ============

export class AgenciesEndpoint {
  constructor(private client: BaseApiClient) {}

  getById(id: string): Promise<ApiResponse<Agency>> {
    return this.client.get<Agency>(`/api/agencies/${id}`);
  }

  getBySlug(slug: string): Promise<ApiResponse<Agency>> {
    return this.client.get<Agency>(`/api/agencies/slug/${slug}`);
  }

  getAll(page = 1, pageSize = 10): Promise<ApiResponse<PagedResult<Agency>>> {
    return this.client.get<PagedResult<Agency>>(`/api/agencies?page=${page}&pageSize=${pageSize}`);
  }

  create(data: CreateAgencyDto): Promise<ApiResponse<Agency>> {
    return this.client.post<Agency>("/api/agencies", data);
  }

  update(id: string, data: UpdateAgencyDto): Promise<ApiResponse<Agency>> {
    return this.client.put<Agency>(`/api/agencies/${id}`, data);
  }

  delete(id: string): Promise<ApiResponse<void>> {
    return this.client.delete(`/api/agencies/${id}`);
  }

  verify(id: string): Promise<ApiResponse<Agency>> {
    return this.client.post<Agency>(`/api/agencies/${id}/verify`);
  }
}
