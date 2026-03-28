import { type BaseApiClient, type ApiResponse, type PagedResult } from "../../base-client";

/** Mirrors `ArdaNova.Domain.Models.Enums.PostType` JSON values. */
export type PostType = "POST" | "ANNOUNCEMENT" | "MILESTONE" | "UPDATE" | string;

/** Mirrors `ArdaNova.Domain.Models.Enums.PostVisibility` JSON values. */
export type PostVisibility = "PUBLIC" | "FOLLOWERS" | "PROJECT_MEMBERS" | "PRIVATE" | string;

export interface Post {
  id: string;
  authorId: string;
  projectId?: string | null;
  guildId?: string | null;
  type: string;
  visibility: string;
  title?: string | null;
  content: string;
  metadata?: string | null;
  likesCount: number;
  commentsCount: number;
  sharesCount: number;
  viewsCount: number;
  isPinned: boolean;
  createdAt: string;
  updatedAt: string;
  [key: string]: unknown;
}

export interface CreatePostDto {
  authorId: string;
  projectId?: string;
  guildId?: string;
  type?: PostType;
  visibility?: PostVisibility;
  title?: string;
  content: string;
  metadata?: string;
  [key: string]: unknown;
}

export interface UpdatePostDto {
  title?: string;
  content?: string;
  visibility?: PostVisibility;
  metadata?: string;
  isPinned?: boolean;
  [key: string]: unknown;
}

export interface PostComment {
  id: string;
  postId: string;
  authorId: string;
  parentId?: string | null;
  content: string;
  likesCount: number;
  createdAt: string;
  updatedAt: string;
  [key: string]: unknown;
}

export interface CreatePostCommentDto {
  authorId: string;
  parentId?: string;
  content: string;
  [key: string]: unknown;
}

export interface CreatePostShareDto {
  sharedToProjectId?: string;
  sharedToGuildId?: string;
  comment?: string;
  [key: string]: unknown;
}

export class PostsEndpoint {
  constructor(private client: BaseApiClient) {}

  getFeed(page = 1, pageSize = 20): Promise<ApiResponse<PagedResult<Post>>> {
    const q = new URLSearchParams({
      page: String(page),
      pageSize: String(pageSize),
    });
    return this.client.get<PagedResult<Post>>(`/api/posts?${q.toString()}`);
  }

  getById(id: string): Promise<ApiResponse<Post>> {
    return this.client.get<Post>(`/api/posts/${encodeURIComponent(id)}`);
  }

  getByUserId(userId: string): Promise<ApiResponse<Post[]>> {
    return this.client.get<Post[]>(`/api/posts/user/${encodeURIComponent(userId)}`);
  }

  create(data: CreatePostDto): Promise<ApiResponse<Post>> {
    return this.client.post<Post>("/api/posts", data);
  }

  update(id: string, authorId: string, data: UpdatePostDto): Promise<ApiResponse<Post>> {
    const q = new URLSearchParams({ authorId });
    return this.client.put<Post>(`/api/posts/${encodeURIComponent(id)}?${q.toString()}`, data);
  }

  delete(id: string, authorId: string): Promise<ApiResponse<void>> {
    const q = new URLSearchParams({ authorId });
    return this.client.delete(`/api/posts/${encodeURIComponent(id)}?${q.toString()}`);
  }

  like(id: string, userId: string): Promise<ApiResponse<Post>> {
    const q = new URLSearchParams({ userId });
    return this.client.post<Post>(`/api/posts/${encodeURIComponent(id)}/like?${q.toString()}`, {});
  }

  unlike(id: string, userId: string): Promise<ApiResponse<Post>> {
    const q = new URLSearchParams({ userId });
    return this.client.delete(`/api/posts/${encodeURIComponent(id)}/like?${q.toString()}`);
  }

  share(id: string, userId: string, data?: CreatePostShareDto): Promise<ApiResponse<Post>> {
    const q = new URLSearchParams({ userId });
    return this.client.post<Post>(
      `/api/posts/${encodeURIComponent(id)}/share?${q.toString()}`,
      data ?? {}
    );
  }

  bookmark(id: string, userId: string): Promise<ApiResponse<boolean>> {
    const q = new URLSearchParams({ userId });
    return this.client.post<boolean>(`/api/posts/${encodeURIComponent(id)}/bookmark?${q.toString()}`, {});
  }

  unbookmark(id: string, userId: string): Promise<ApiResponse<boolean>> {
    const q = new URLSearchParams({ userId });
    return this.client.delete(`/api/posts/${encodeURIComponent(id)}/bookmark?${q.toString()}`);
  }

  getComments(id: string): Promise<ApiResponse<PostComment[]>> {
    return this.client.get<PostComment[]>(`/api/posts/${encodeURIComponent(id)}/comments`);
  }

  addComment(id: string, data: CreatePostCommentDto): Promise<ApiResponse<PostComment>> {
    return this.client.post<PostComment>(`/api/posts/${encodeURIComponent(id)}/comments`, data);
  }

  deleteComment(id: string, commentId: string, authorId: string): Promise<ApiResponse<void>> {
    const q = new URLSearchParams({ authorId });
    return this.client.delete(
      `/api/posts/${encodeURIComponent(id)}/comments/${encodeURIComponent(commentId)}?${q.toString()}`
    );
  }
}
