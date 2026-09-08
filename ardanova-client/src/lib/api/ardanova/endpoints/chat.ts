import { type BaseApiClient, type ApiResponse } from "../../base-client";

// ============ Type Definitions ============

export type ConversationType = "DIRECT" | "GROUP";
export type ConversationRole = "OWNER" | "ADMIN" | "MEMBER";
export type MessageStatus = "SENT" | "DELIVERED" | "SEEN";

export interface ChatParticipant {
  id: string;
  userId: string;
  userName: string;
  userImage?: string | null;
  role: ConversationRole;
  lastReadAt?: string | null;
  lastActiveAt?: string | null;
  joinedAt: string;
  isOnline: boolean;
}

export interface ChatMessage {
  id: string;
  conversationId: string;
  userFromId: string;
  userFromName: string;
  userFromImage?: string | null;
  message?: string | null;
  status: MessageStatus;
  replyToId?: string | null;
  replyTo?: ChatMessage | null;
  sentAt: string;
  deliveredAt?: string | null;
  seenAt?: string | null;
  editedAt?: string | null;
  isDeleted: boolean;
}

export interface ChatConversation {
  id: string;
  type: ConversationType;
  name?: string | null;
  avatarUrl?: string | null;
  createdAt: string;
  lastMessageAt?: string | null;
  members: ChatParticipant[];
  lastMessage?: ChatMessage | null;
  unreadCount: number;
}

export interface GetConversationsParams {
  page?: number;
  pageSize?: number;
  type?: ConversationType;
}

export interface CursorPaginatedResult<T> {
  items: T[];
  totalCount?: number;
  hasMore?: boolean;
  nextCursor?: string | null;
}

export interface GetOrCreateDirectDto {
  participantUserId: string;
}

export interface CreateGroupDto {
  name: string;
  avatarUrl?: string;
  memberUserIds: string[];
}

export interface UpdateGroupDto {
  name?: string;
  avatarUrl?: string;
}

export interface AddMemberDto {
  userId: string;
  role?: ConversationRole;
}

export interface SendMessageDto {
  conversationId: string;
  message: string;
  replyToId?: string;
}

export interface UpdateMessageDto {
  message: string;
}

export interface MarkAsReadDto {
  conversationId: string;
  readUpTo: string;
}

export interface TypingIndicatorDto {
  conversationId: string;
  isTyping: boolean;
}

// ============ Chat Endpoint ============

export class ChatEndpoint {
  constructor(private client: BaseApiClient) {}

  // Conversations

  getConversations(
    params: GetConversationsParams,
  ): Promise<ApiResponse<CursorPaginatedResult<ChatConversation>>> {
    const searchParams = new URLSearchParams();
    if (params.type) searchParams.set("type", params.type);
    if (params.page) searchParams.set("page", String(params.page));
    if (params.pageSize) searchParams.set("pageSize", String(params.pageSize));

    return this.client.get<CursorPaginatedResult<ChatConversation>>(
      `/api/chat/conversations?${searchParams.toString()}`,
    );
  }

  getConversation(conversationId: string): Promise<ApiResponse<ChatConversation>> {
    return this.client.get<ChatConversation>(`/api/chat/conversations/${conversationId}`);
  }

  getOrCreateDirect(dto: GetOrCreateDirectDto): Promise<ApiResponse<ChatConversation>> {
    return this.client.post<ChatConversation>(
      "/api/chat/conversations/direct",
      dto,
    );
  }

  createGroup(dto: CreateGroupDto): Promise<ApiResponse<ChatConversation>> {
    return this.client.post<ChatConversation>(
      "/api/chat/conversations/group",
      dto,
    );
  }

  updateGroup(
    conversationId: string,
    dto: UpdateGroupDto,
  ): Promise<ApiResponse<ChatConversation>> {
    return this.client.put<ChatConversation>(
      `/api/chat/conversations/${conversationId}`,
      dto,
    );
  }

  addMember(
    conversationId: string,
    dto: AddMemberDto,
  ): Promise<ApiResponse<ChatConversation>> {
    return this.client.post<ChatConversation>(
      `/api/chat/conversations/${conversationId}/members`,
      dto,
    );
  }

  removeMember(
    conversationId: string,
    memberUserId: string,
  ): Promise<ApiResponse<void>> {
    return this.client.delete(
      `/api/chat/conversations/${conversationId}/members/${memberUserId}`,
    );
  }

  leaveConversation(conversationId: string): Promise<ApiResponse<{ success: boolean }>> {
    return this.client.post<{ success: boolean }>(
      `/api/chat/conversations/${conversationId}/leave`,
      {},
    );
  }

  // Messages

  getMessages(
    conversationId: string,
    limit: number,
    cursor?: string,
  ): Promise<ApiResponse<CursorPaginatedResult<ChatMessage>>> {
    const searchParams = new URLSearchParams();
    searchParams.set("limit", String(limit));
    if (cursor) searchParams.set("cursor", cursor);

    return this.client.get<CursorPaginatedResult<ChatMessage>>(
      `/api/chat/conversations/${conversationId}/messages?${searchParams.toString()}`,
    );
  }

  sendMessage(dto: SendMessageDto): Promise<ApiResponse<ChatMessage>> {
    return this.client.post<ChatMessage>(
      "/api/chat/messages",
      dto,
    );
  }

  updateMessage(
    messageId: string,
    dto: UpdateMessageDto,
  ): Promise<ApiResponse<ChatMessage>> {
    return this.client.put<ChatMessage>(
      `/api/chat/messages/${messageId}`,
      dto,
    );
  }

  deleteMessage(messageId: string): Promise<ApiResponse<void>> {
    return this.client.delete(`/api/chat/messages/${messageId}`);
  }

  markAsRead(dto: MarkAsReadDto): Promise<ApiResponse<{ success: boolean }>> {
    return this.client.post<{ success: boolean }>(
      "/api/chat/messages/read",
      dto,
    );
  }

  sendTypingIndicator(dto: TypingIndicatorDto): Promise<ApiResponse<{ success: boolean }>> {
    return this.client.post<{ success: boolean }>(
      "/api/chat/typing",
      dto,
    );
  }
}
