import { type BaseApiClient, type ApiResponse, type PagedResult } from "../../base-client";

export interface Attachment {
  id: string;
  uploadedById: string;
  bucketPath?: string | null;
  type: string;
  createdAt: string;
  lastUsedAt?: string | null;
  url?: string | null;
  fileName?: string | null;
  fileSize?: number | null;
  [key: string]: unknown;
}

export interface CreateAttachmentDto {
  uploadedById: string;
  bucketPath: string;
  type: string;
  fileName?: string;
  fileSize?: number;
  [key: string]: unknown;
}

export interface UploadRequestDto {
  fileName: string;
  contentType: string;
  fileSize?: number;
  folder?: string;
  [key: string]: unknown;
}

export interface UploadResponseDto {
  uploadUrl: string;
  bucketPath: string;
  publicUrl: string;
  expiresAt: string;
  headers?: Record<string, string>;
  [key: string]: unknown;
}

export interface BulkUploadRequestDto {
  files: UploadRequestDto[];
  [key: string]: unknown;
}

export interface BulkUploadResponseDto {
  uploads: UploadResponseDto[];
  [key: string]: unknown;
}

export interface DownloadResponseDto {
  downloadUrl: string;
  fileName: string;
  expiresAt: string;
  [key: string]: unknown;
}

function userHeader(userId: string): Record<string, string> {
  return { "X-User-Id": userId };
}

export class AttachmentsEndpoint {
  constructor(private client: BaseApiClient) {}

  getPaged(page = 1, pageSize = 10): Promise<ApiResponse<PagedResult<Attachment>>> {
    const q = new URLSearchParams({ page: String(page), pageSize: String(pageSize) });
    return this.client.get<PagedResult<Attachment>>(`/api/attachments?${q.toString()}`);
  }

  getById(id: string): Promise<ApiResponse<Attachment>> {
    return this.client.get<Attachment>(`/api/attachments/${encodeURIComponent(id)}`);
  }

  getByUserId(userId: string): Promise<ApiResponse<Attachment[]>> {
    return this.client.get<Attachment[]>(`/api/attachments/user/${encodeURIComponent(userId)}`);
  }

  getUploadUrl(userId: string, request: UploadRequestDto): Promise<ApiResponse<UploadResponseDto>> {
    return this.client.post<UploadResponseDto>(
      "/api/attachments/upload-url",
      request,
      userHeader(userId)
    );
  }

  getUploadUrls(userId: string, request: BulkUploadRequestDto): Promise<ApiResponse<BulkUploadResponseDto>> {
    return this.client.post<BulkUploadResponseDto>(
      "/api/attachments/upload-urls",
      request,
      userHeader(userId)
    );
  }

  getDownloadUrl(id: string, expirationMinutes = 60): Promise<ApiResponse<DownloadResponseDto>> {
    const q = new URLSearchParams({ expirationMinutes: String(expirationMinutes) });
    return this.client.get<DownloadResponseDto>(
      `/api/attachments/${encodeURIComponent(id)}/download-url?${q.toString()}`
    );
  }

  getPublicUrl(id: string): Promise<ApiResponse<{ url: string }>> {
    return this.client.get<{ url: string }>(`/api/attachments/${encodeURIComponent(id)}/public-url`);
  }

  create(data: CreateAttachmentDto): Promise<ApiResponse<Attachment>> {
    return this.client.post<Attachment>("/api/attachments", data);
  }

  upload(
    userId: string,
    file: Blob,
    fileName: string,
    options?: { folder?: string }
  ): Promise<ApiResponse<Attachment>> {
    const form = new FormData();
    form.append("file", file, fileName);
    const q = options?.folder ? `?folder=${encodeURIComponent(options.folder)}` : "";
    return this.client.postFormData<Attachment>(`/api/attachments/upload${q}`, form, userHeader(userId));
  }

  delete(id: string): Promise<ApiResponse<void>> {
    return this.client.delete(`/api/attachments/${encodeURIComponent(id)}`);
  }
}
