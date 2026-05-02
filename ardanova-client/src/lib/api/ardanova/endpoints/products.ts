import { type BaseApiClient, type ApiResponse } from "../../base-client";

export interface Product {
  id: string;
  projectId: string;
  name: string;
  description?: string | null;
  sku?: string | null;
  price?: number | null;
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
  [key: string]: unknown;
}

export interface CreateProduct {
  projectId: string;
  name: string;
  description?: string;
  sku?: string;
  price?: number;
  [key: string]: unknown;
}

export interface UpdateProduct {
  name?: string;
  description?: string;
  sku?: string;
  price?: number;
  isActive?: boolean;
  [key: string]: unknown;
}

export class ProductsEndpoint {
  constructor(private client: BaseApiClient) {}

  getById(id: string): Promise<ApiResponse<Product>> {
    return this.client.get<Product>(`/api/products/${id}`);
  }

  getByProjectId(projectId: string): Promise<ApiResponse<Product[]>> {
    return this.client.get<Product[]>(`/api/products/project/${projectId}`);
  }

  create(data: CreateProduct): Promise<ApiResponse<Product>> {
    return this.client.post<Product>("/api/products", data);
  }

  update(id: string, data: UpdateProduct): Promise<ApiResponse<Product>> {
    return this.client.put<Product>(`/api/products/${id}`, data);
  }

  delete(id: string): Promise<ApiResponse<void>> {
    return this.client.delete(`/api/products/${id}`);
  }

  toggleActive(id: string): Promise<ApiResponse<Product>> {
    return this.client.post<Product>(`/api/products/${id}/toggle-active`, {});
  }
}
