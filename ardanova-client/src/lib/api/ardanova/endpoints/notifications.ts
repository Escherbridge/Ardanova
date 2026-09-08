import { type BaseApiClient, type ApiResponse, type PagedResult } from "../../base-client";

export interface Notification {
  id: string;
  userId: string;
  type: string;
  title: string;
  message: string;
  data?: string | null;
  isRead: boolean;
  readAt?: string | null;
  actionUrl?: string | null;
  createdAt: string;
  [key: string]: unknown;
}

export interface NotificationSummary {
  totalCount: number;
  unreadCount: number;
  [key: string]: unknown;
}

export class NotificationsEndpoint {
  constructor(private client: BaseApiClient) {}

  getById(id: string): Promise<ApiResponse<Notification>> {
    return this.client.get<Notification>(`/api/notifications/${encodeURIComponent(id)}`);
  }

  getMine(): Promise<ApiResponse<Notification[]>> {
    return this.client.get<Notification[]>("/api/notifications/me");
  }

  getPaged(page = 1, pageSize = 10): Promise<ApiResponse<PagedResult<Notification>>> {
    const q = new URLSearchParams({ page: String(page), pageSize: String(pageSize) });
    return this.client.get<PagedResult<Notification>>(
      `/api/notifications/me/paged?${q.toString()}`
    );
  }

  getUnread(): Promise<ApiResponse<Notification[]>> {
    return this.client.get<Notification[]>("/api/notifications/me/unread");
  }

  getSummary(): Promise<ApiResponse<NotificationSummary>> {
    return this.client.get<NotificationSummary>("/api/notifications/me/summary");
  }

  markAsRead(id: string): Promise<ApiResponse<Notification>> {
    return this.client.post<Notification>(`/api/notifications/${encodeURIComponent(id)}/read`, {});
  }

  markAllAsRead(): Promise<ApiResponse<{ success: boolean }>> {
    return this.client.post<{ success: boolean }>(
      "/api/notifications/me/read-all",
      {}
    );
  }

  delete(id: string): Promise<ApiResponse<void>> {
    return this.client.delete(`/api/notifications/${encodeURIComponent(id)}`);
  }

  deleteAll(): Promise<ApiResponse<void>> {
    return this.client.delete("/api/notifications/me");
  }
}
