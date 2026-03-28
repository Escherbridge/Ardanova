import { type BaseApiClient, type ApiResponse } from "../../base-client";

export class EnumsEndpoint {
  constructor(private client: BaseApiClient) {}

  getAll(): Promise<ApiResponse<string[]>> {
    return this.client.get<string[]>("/api/enums");
  }

  getByName(name: string): Promise<ApiResponse<string[]>> {
    return this.client.get<string[]>(`/api/enums/${encodeURIComponent(name)}`);
  }
}
