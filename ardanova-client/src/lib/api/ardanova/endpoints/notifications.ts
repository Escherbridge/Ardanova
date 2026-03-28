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

export interface CreateNotificationDto {
  userId: string;
  type: string;
  title: string;
  message: string;
  data?: string;
  actionUrl?: string;
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

  getByUserId(userId: string): Promise<ApiResponse<Notification[]>> {
    return this.client.get<Notification[]>(`/api/notifications/user/${encodeURIComponent(userId)}`);
  }

  getPaged(userId: string, page = 1, pageSize = 10): Promise<ApiResponse<PagedResult<Notification>>> {
    const q = new URLSearchParams({ page: String(page), pageSize: String(pageSize) });
    return this.client.get<PagedResult<Notification>>(
      `/api/notifications/user/${encodeURIComponent(userId)}/paged?${q.toString()}`
    );
  }

  getUnread(userId: string): Promise<ApiResponse<Notification[]>> {
    return this.client.get<Notification[]>(`/api/notifications/user/${encodeURIComponent(userId)}/unread`);
  }

  getSummary(userId: string): Promise<ApiResponse<NotificationSummary>> {
    return this.client.get<NotificationSummary>(`/api/notifications/user/${encodeURIComponent(userId)}/summary`);
  }

  create(data: CreateNotificationDto): Promise<ApiResponse<Notification>> {
    return this.client.post<Notification>("/api/notifications", data);
  }

  markAsRead(id: string): Promise<ApiResponse<Notification>> {
    return this.client.post<Notification>(`/api/notifications/${encodeURIComponent(id)}/read`, {});
  }

  markAllAsRead(userId: string): Promise<ApiResponse<{ success: boolean }>> {
    return this.client.post<{ success: boolean }>(
      `/api/notifications/user/${encodeURIComponent(userId)}/read-all`,
      {}
    );
  }

  delete(id: string): Promise<ApiResponse<void>> {
    return this.client.delete(`/api/notifications/${encodeURIComponent(id)}`);
  }

  deleteAll(userId: string): Promise<ApiResponse<void>> {
    return this.client.delete(`/api/notifications/user/${encodeURIComponent(userId)}`);
  }
}
