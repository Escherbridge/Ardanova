import "server-only";

/**
 * Generic Base API Client
 * Reusable foundation for any REST API integration
 */

export interface ApiResponse<T> {
  data?: T;
  error?: string;
  status: number;
}

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
}

export interface BaseApiClientConfig {
  baseUrl: string;
  headers?: Record<string, string>;
  timeout?: number;
}

export class BaseApiClient {
  protected readonly baseUrl: string;
  protected readonly defaultHeaders: Record<string, string>;
  protected readonly timeout: number;

  constructor(config: BaseApiClientConfig) {
    this.baseUrl = config.baseUrl;
    this.defaultHeaders = {
      "Content-Type": "application/json",
      ...config.headers,
    };
    this.timeout = config.timeout ?? 30000;
  }

  protected async request<T>(
    endpoint: string,
    options: RequestInit = {}
  ): Promise<ApiResponse<T>> {
    const url = `${this.baseUrl}${endpoint}`;

    const headers: HeadersInit = {
      ...this.defaultHeaders,
      ...options.headers,
    };

    try {
      const controller = new AbortController();
      const timeoutId = setTimeout(() => controller.abort(), this.timeout);

      const response = await fetch(url, {
        ...options,
        headers,
        signal: controller.signal,
      });

      clearTimeout(timeoutId);

      if (!response.ok) {
        const errorText = await response.text();
        return {
          error: errorText || `HTTP ${response.status}`,
          status: response.status,
        };
      }

      // Handle empty responses
      const text = await response.text();
      if (!text) {
        return { status: response.status };
      }

      const data = JSON.parse(text) as T;
      return { data, status: response.status };
    } catch (error) {
      if (error instanceof Error && error.name === "AbortError") {
        return {
          error: "Request timeout",
          status: 408,
        };
      }
      return {
        error: error instanceof Error ? error.message : "Unknown error",
        status: 500,
      };
    }
  }

  get<T>(endpoint: string): Promise<ApiResponse<T>> {
    return this.request<T>(endpoint, { method: "GET" });
  }

  post<T>(endpoint: string, body?: unknown, extraHeaders?: Record<string, string>): Promise<ApiResponse<T>> {
    return this.request<T>(endpoint, {
      method: "POST",
      body: body ? JSON.stringify(body) : undefined,
      headers: extraHeaders,
    });
  }

  /** Multipart POST — omits `Content-Type` so the boundary is set automatically. */
  async postFormData<T>(endpoint: string, formData: FormData, extraHeaders?: Record<string, string>): Promise<ApiResponse<T>> {
    const url = `${this.baseUrl}${endpoint}`;
    const headers: Record<string, string> = { ...this.defaultHeaders, ...extraHeaders };
    delete headers["Content-Type"];

    try {
      const controller = new AbortController();
      const timeoutId = setTimeout(() => controller.abort(), this.timeout);

      const response = await fetch(url, {
        method: "POST",
        body: formData,
        headers,
        signal: controller.signal,
      });

      clearTimeout(timeoutId);

      if (!response.ok) {
        const errorText = await response.text();
        return {
          error: errorText || `HTTP ${response.status}`,
          status: response.status,
        };
      }

      const text = await response.text();
      if (!text) {
        return { status: response.status };
      }

      const data = JSON.parse(text) as T;
      return { data, status: response.status };
    } catch (error) {
      if (error instanceof Error && error.name === "AbortError") {
        return {
          error: "Request timeout",
          status: 408,
        };
      }
      return {
        error: error instanceof Error ? error.message : "Unknown error",
        status: 500,
      };
    }
  }

  put<T>(endpoint: string, body?: unknown): Promise<ApiResponse<T>> {
    return this.request<T>(endpoint, {
      method: "PUT",
      body: body ? JSON.stringify(body) : undefined,
    });
  }

  patch<T>(endpoint: string, body?: unknown): Promise<ApiResponse<T>> {
    return this.request<T>(endpoint, {
      method: "PATCH",
      body: body ? JSON.stringify(body) : undefined,
    });
  }

  delete<T>(endpoint: string): Promise<ApiResponse<T>> {
    return this.request<T>(endpoint, { method: "DELETE" });
  }
}

/**
 * Create a client with API Key authentication
 */
export function createApiKeyClient(baseUrl: string, apiKey: string, headerName = "X-Api-Key") {
  return new BaseApiClient({
    baseUrl,
    headers: { [headerName]: apiKey },
  });
}

/**
 * Create a client with Bearer token authentication
 */
export function createBearerClient(baseUrl: string, token: string) {
  return new BaseApiClient({
    baseUrl,
    headers: { Authorization: `Bearer ${token}` },
  });
}
