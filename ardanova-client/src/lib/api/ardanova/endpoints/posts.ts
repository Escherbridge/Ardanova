import { apiPath } from "../../path";
import {
  type BaseApiClient,
  type ApiResponse,
  type PagedResult,
} from "../../base-client";

/** Mirrors `ArdaNova.Domain.Models.Enums.PostType` JSON values. */
export type PostType =
  | "POST"
  | "PROJECT_UPDATE"
  | "GUILD_ACTIVITY"
  | "TASK_COMPLETED"
  | "MILESTONE"
  | "PROPOSAL"
  | "SHOP_ITEM";

/** Mirrors `ArdaNova.Domain.Models.Enums.PostVisibility` JSON values. */
export type PostVisibility =
  | "PUBLIC"
  | "PROJECT_MEMBERS"
  | "GUILD_MEMBERS"
  | "PRIVATE";

export interface Post {
  id: string;
  authorId: string;
  projectId?: string | null;
  guildId?: string | null;
  type: PostType;
  visibility: PostVisibility;
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

/**
 * Create payload. There is no `authorId`: the API takes the author from the
 * signed actor assertion the BFF attaches, so a caller cannot post as anyone else.
 */
export interface CreatePostDto {
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

/** Reply payload; the author is the signed actor, never a request value. */
export interface CreatePostCommentDto {
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

/**
 * Posts are project-scoped field notes. There is no global feed endpoint: every read
 * is filtered server-side to what the signed actor may see, and every write is bound
 * to that same actor.
 */
export class PostsEndpoint {
  constructor(private client: BaseApiClient) {}

  getProjectFieldNotes(
    projectId: string,
    page = 1,
    pageSize = 20,
  ): Promise<ApiResponse<PagedResult<Post>>> {
    const q = new URLSearchParams({
      page: String(page),
      pageSize: String(pageSize),
    });
    return this.client.get<PagedResult<Post>>(
      apiPath`/api/projects/${projectId}/field-notes?${q}`,
    );
  }

  getById(id: string): Promise<ApiResponse<Post>> {
    return this.client.get<Post>(apiPath`/api/posts/${id}`);
  }

  getByUserId(userId: string): Promise<ApiResponse<Post[]>> {
    return this.client.get<Post[]>(apiPath`/api/posts/user/${userId}`);
  }

  create(data: CreatePostDto): Promise<ApiResponse<Post>> {
    return this.client.post<Post>("/api/posts", data);
  }

  update(id: string, data: UpdatePostDto): Promise<ApiResponse<Post>> {
    return this.client.put<Post>(apiPath`/api/posts/${id}`, data);
  }

  delete(id: string): Promise<ApiResponse<void>> {
    return this.client.delete(apiPath`/api/posts/${id}`);
  }

  like(id: string): Promise<ApiResponse<Post>> {
    return this.client.post<Post>(apiPath`/api/posts/${id}/like`, {});
  }

  unlike(id: string): Promise<ApiResponse<Post>> {
    return this.client.delete(apiPath`/api/posts/${id}/like`);
  }

  share(id: string, data?: CreatePostShareDto): Promise<ApiResponse<Post>> {
    return this.client.post<Post>(apiPath`/api/posts/${id}/share`, data ?? {});
  }

  bookmark(id: string): Promise<ApiResponse<boolean>> {
    return this.client.post<boolean>(apiPath`/api/posts/${id}/bookmark`, {});
  }

  unbookmark(id: string): Promise<ApiResponse<boolean>> {
    return this.client.delete(apiPath`/api/posts/${id}/bookmark`);
  }

  getComments(id: string): Promise<ApiResponse<PostComment[]>> {
    return this.client.get<PostComment[]>(apiPath`/api/posts/${id}/comments`);
  }

  addComment(
    id: string,
    data: CreatePostCommentDto,
  ): Promise<ApiResponse<PostComment>> {
    return this.client.post<PostComment>(
      apiPath`/api/posts/${id}/comments`,
      data,
    );
  }

  deleteComment(id: string, commentId: string): Promise<ApiResponse<void>> {
    return this.client.delete(apiPath`/api/posts/${id}/comments/${commentId}`);
  }
}
