import { type BaseApiClient, type ApiResponse, type PagedResult } from "../../base-client";

// ============ Type Definitions ============

export interface User {
  id: string;
  email: string;
  emailVerified?: string;
  name?: string;
  image?: string;
  bio?: string;
  location?: string;
  phone?: string;
  website?: string;
  linkedIn?: string;
  twitter?: string;
  role: string;
  userType: string;
  isVerified: boolean;
  verificationLevel: string;
  createdAt: string;
  updatedAt: string;
}

export interface CreateUserDto {
  email: string;
  name?: string;
  image?: string;
  role?: string;
  userType?: string;
}

export interface UpdateUserDto {
  name?: string;
  bio?: string;
  location?: string;
  phone?: string;
  website?: string;
  linkedIn?: string;
  twitter?: string;
  image?: string;
}

// ============ Users Endpoint ============

export class UsersEndpoint {
  constructor(private client: BaseApiClient) {}

  getById(id: string): Promise<ApiResponse<User>> {
    return this.client.get<User>(`/api/users/${id}`);
  }

  getByEmail(email: string): Promise<ApiResponse<User>> {
    return this.client.get<User>(`/api/users/email/${encodeURIComponent(email)}`);
  }

  getAll(page = 1, pageSize = 10): Promise<ApiResponse<PagedResult<User>>> {
    return this.client.get<PagedResult<User>>(`/api/users?page=${page}&pageSize=${pageSize}`);
  }

  search(query: string, page = 1, pageSize = 20): Promise<ApiResponse<PagedResult<User>>> {
    return this.client.get<PagedResult<User>>(
      `/api/users/search?query=${encodeURIComponent(query)}&page=${page}&pageSize=${pageSize}`
    );
  }

  create(data: CreateUserDto): Promise<ApiResponse<User>> {
    return this.client.post<User>("/api/users", data);
  }

  update(id: string, data: UpdateUserDto): Promise<ApiResponse<User>> {
    return this.client.put<User>(`/api/users/${id}`, data);
  }

  delete(id: string): Promise<ApiResponse<void>> {
    return this.client.delete(`/api/users/${id}`);
  }
}
