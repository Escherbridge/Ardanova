import { z } from "zod";
import { type BaseApiClient, type ApiResponse, type PagedResult } from "../../base-client";

export interface Guild {
  id: string;
  name: string;
  slug?: string | null;
  description?: string | null;
  logoUrl?: string | null;
  website?: string | null;
  isVerified: boolean;
  ownerId: string;
  createdAt: string;
  updatedAt: string;
  [key: string]: unknown;
}

export interface GuildMember {
  id: string;
  guildId: string;
  userId: string;
  role?: string | null;
  joinedAt: string;
  user?: { id: string; name?: string; email: string; image?: string };
  [key: string]: unknown;
}

export interface GuildReview {
  id: string;
  guildId: string;
  reviewerId: string;
  rating: number;
  title?: string | null;
  content?: string | null;
  createdAt: string;
  [key: string]: unknown;
}

export interface CreateGuildDto {
  name: string;
  slug?: string;
  description?: string;
  logoUrl?: string;
  website?: string;
  /** Required by `POST /api/guilds` (see `CreateGuildDto` in backend). */
  email: string;
  phone?: string;
  ownerId: string;
  [key: string]: unknown;
}

export interface UpdateGuildDto {
  name?: string;
  slug?: string;
  description?: string;
  logoUrl?: string;
  website?: string;
  [key: string]: unknown;
}

export interface CreateGuildMemberDto {
  guildId?: string;
  userId: string;
  role?: string;
  [key: string]: unknown;
}

export interface UpdateGuildMemberDto {
  role?: string;
  [key: string]: unknown;
}

export interface CreateGuildReviewDto {
  guildId?: string;
  reviewerId: string;
  rating: number;
  title?: string;
  content?: string;
  [key: string]: unknown;
}

export interface UpdateGuildReviewDto {
  rating?: number;
  title?: string;
  content?: string;
  [key: string]: unknown;
}

// Zod schemas for validation (re-exported from index)
export const GuildApiSchema = z.object({
  id: z.string(),
  name: z.string(),
  slug: z.string().nullable().optional(),
  description: z.string().nullable().optional(),
  logoUrl: z.string().nullable().optional(),
  website: z.string().nullable().optional(),
  isVerified: z.boolean(),
  ownerId: z.string(),
  createdAt: z.string(),
  updatedAt: z.string(),
});

export const CreateGuildSchema = z.object({
  name: z.string().min(1),
  slug: z.string().optional(),
  description: z.string().min(1),
  email: z.string().min(1).email(),
  phone: z.string().optional(),
  logoUrl: z.string().url().optional(),
  // Backend accepts optional URL; form may omit or send a full URL (not `z.string().url()` on empty)
  website: z
    .string()
    .optional()
    .transform((s) =>
      s === undefined || s.trim() === "" ? undefined : s.trim(),
    )
    .refine((s) => s === undefined || /^https?:\/\/.+/.test(s), {
      message: "Website must start with http:// or https://",
    }),
  ownerId: z.string().min(1),
});

export const UpdateGuildSchema = z.object({
  name: z.string().min(1).optional(),
  slug: z.string().optional(),
  description: z.string().optional(),
  logoUrl: z.string().url().optional(),
  website: z.string().url().optional(),
});

export const GuildMemberApiSchema = z.object({
  id: z.string(),
  guildId: z.string(),
  userId: z.string(),
  role: z.string().nullable().optional(),
  joinedAt: z.string(),
});

export const CreateGuildMemberSchema = z.object({
  userId: z.string().min(1),
  role: z.string().optional(),
});

export const UpdateGuildMemberSchema = z.object({
  role: z.string().optional(),
});

export const GuildReviewApiSchema = z.object({
  id: z.string(),
  guildId: z.string(),
  reviewerId: z.string(),
  rating: z.number(),
  title: z.string().nullable().optional(),
  content: z.string().nullable().optional(),
  createdAt: z.string(),
});

export const CreateGuildReviewSchema = z.object({
  reviewerId: z.string().min(1),
  rating: z.number().min(1).max(5),
  title: z.string().optional(),
  content: z.string().optional(),
});

export const UpdateGuildReviewSchema = z.object({
  rating: z.number().min(1).max(5).optional(),
  title: z.string().optional(),
  content: z.string().optional(),
});

export class GuildsEndpoint {
  constructor(private client: BaseApiClient) {}

  getById(id: string): Promise<ApiResponse<Guild>> {
    return this.client.get<Guild>(`/api/guilds/${id}`);
  }

  getBySlug(slug: string): Promise<ApiResponse<Guild>> {
    return this.client.get<Guild>(`/api/guilds/slug/${encodeURIComponent(slug)}`);
  }

  getByOwnerId(ownerId: string): Promise<ApiResponse<Guild | Guild[]>> {
    return this.client.get<Guild | Guild[]>(`/api/guilds/owner/${ownerId}`);
  }

  getPaged(page = 1, limit = 10): Promise<ApiResponse<PagedResult<Guild>>> {
    return this.client.get<PagedResult<Guild>>(`/api/guilds/paged?page=${page}&pageSize=${limit}`);
  }

  getVerified(): Promise<ApiResponse<Guild[]>> {
    return this.client.get<Guild[]>("/api/guilds/verified");
  }

  create(data: CreateGuildDto): Promise<ApiResponse<Guild>> {
    return this.client.post<Guild>("/api/guilds", data);
  }

  update(id: string, data: UpdateGuildDto): Promise<ApiResponse<Guild>> {
    return this.client.put<Guild>(`/api/guilds/${id}`, data);
  }

  delete(id: string): Promise<ApiResponse<void>> {
    return this.client.delete(`/api/guilds/${id}`);
  }

  getMembers(guildId: string): Promise<ApiResponse<GuildMember[]>> {
    return this.client.get<GuildMember[]>(`/api/guilds/${guildId}/members`);
  }

  addMember(input: { guildId: string; userId: string; role?: string }): Promise<ApiResponse<GuildMember>> {
    const { guildId, ...dto } = input;
    return this.client.post<GuildMember>(`/api/guilds/${guildId}/members`, dto);
  }

  updateMember(guildId: string, memberId: string, data: UpdateGuildMemberDto): Promise<ApiResponse<GuildMember>> {
    return this.client.put<GuildMember>(`/api/guilds/${guildId}/members/${memberId}`, data);
  }

  removeMember(guildId: string, memberId: string): Promise<ApiResponse<void>> {
    return this.client.delete(`/api/guilds/${guildId}/members/${memberId}`);
  }

  getReviews(guildId: string): Promise<ApiResponse<GuildReview[]>> {
    return this.client.get<GuildReview[]>(`/api/guilds/${guildId}/reviews`);
  }

  createReview(input: { guildId: string; reviewerId: string; rating: number; title?: string; content?: string }): Promise<ApiResponse<GuildReview>> {
    const { guildId, ...dto } = input;
    return this.client.post<GuildReview>(`/api/guilds/${guildId}/reviews`, dto);
  }

  updateReview(guildId: string, reviewId: string, data: UpdateGuildReviewDto): Promise<ApiResponse<GuildReview>> {
    return this.client.put<GuildReview>(`/api/guilds/${guildId}/reviews/${reviewId}`, data);
  }

  deleteReview(guildId: string, reviewId: string): Promise<ApiResponse<void>> {
    return this.client.delete(`/api/guilds/${guildId}/reviews/${reviewId}`);
  }

  getUpdates(guildId: string): Promise<ApiResponse<unknown[]>> {
    return this.client.get<unknown[]>(`/api/guilds/${guildId}/updates`);
  }

  createUpdate(guildId: string, data: { title: string; content: string }): Promise<ApiResponse<unknown>> {
    return this.client.post(`/api/guilds/${guildId}/updates`, data);
  }

  deleteUpdate(guildId: string, updateId: string): Promise<ApiResponse<void>> {
    return this.client.delete(`/api/guilds/${guildId}/updates/${updateId}`);
  }

  getApplications(guildId: string): Promise<ApiResponse<unknown[]>> {
    return this.client.get<unknown[]>(`/api/guilds/${guildId}/applications`);
  }

  /** Matches `CreateGuildApplicationDto` — `guildId` is set on the server from the route; include it for compatibility. */
  submitApplication(
    guildId: string,
    data: {
      userId: string;
      requestedRole: string;
      message: string;
      skills?: string;
      experience?: string;
      portfolio?: string;
      availability?: string;
    }
  ): Promise<ApiResponse<unknown>> {
    return this.client.post(`/api/guilds/${guildId}/applications`, { guildId, ...data });
  }

  getApplicationById(guildId: string, applicationId: string): Promise<ApiResponse<{ id: string; guildId: string; userId: string; [key: string]: unknown }>> {
    return this.client.get(`/api/guilds/${guildId}/applications/${applicationId}`);
  }

  acceptApplication(guildId: string, applicationId: string, data?: { reviewMessage?: string }): Promise<ApiResponse<unknown>> {
    return this.client.post(`/api/guilds/${guildId}/applications/${applicationId}/accept`, data ?? {});
  }

  rejectApplication(guildId: string, applicationId: string, data?: { reviewMessage?: string }): Promise<ApiResponse<unknown>> {
    return this.client.post(`/api/guilds/${guildId}/applications/${applicationId}/reject`, data ?? {});
  }

  getInvitations(guildId: string): Promise<ApiResponse<unknown[]>> {
    return this.client.get<unknown[]>(`/api/guilds/${guildId}/invitations`);
  }

  getInvitationById(guildId: string, invitationId: string): Promise<ApiResponse<{ id: string; invitedUserId: string; [key: string]: unknown }>> {
    return this.client.get(`/api/guilds/${guildId}/invitations/${invitationId}`);
  }

  /** Matches `CreateGuildInvitationDto` */
  createInvitation(
    guildId: string,
    data: {
      invitedById: string;
      invitedUserId?: string;
      invitedEmail?: string;
      role: string;
      message?: string;
    }
  ): Promise<ApiResponse<unknown>> {
    return this.client.post(`/api/guilds/${guildId}/invitations`, { guildId, ...data });
  }

  respondToInvitation(guildId: string, invitationId: string, data: { accept: boolean }): Promise<ApiResponse<unknown>> {
    return data.accept
      ? this.client.post(`/api/guilds/${guildId}/invitations/${invitationId}/accept`, {})
      : this.client.post(`/api/guilds/${guildId}/invitations/${invitationId}/reject`, {});
  }

  getFollowers(guildId: string): Promise<ApiResponse<unknown[]>> {
    return this.client.get<unknown[]>(`/api/guilds/${guildId}/followers`);
  }

  /** `POST api/guilds/{guildId}/follow` — body matches `CreateGuildFollowDto`. */
  followGuild(guildId: string, userId: string): Promise<ApiResponse<unknown>> {
    return this.client.post(`/api/guilds/${guildId}/follow`, {
      guildId,
      userId,
      notifyUpdates: true,
      notifyEvents: true,
      notifyProjects: true,
    });
  }

  /** `DELETE api/guilds/{guildId}/follow?userId=` */
  unfollowGuild(guildId: string, userId: string): Promise<ApiResponse<void>> {
    return this.client.delete(
      `/api/guilds/${guildId}/follow?userId=${encodeURIComponent(userId)}`
    );
  }

  /** `GET api/guilds/{guildId}/follow/check?userId=` */
  isFollowing(guildId: string, userId: string): Promise<ApiResponse<boolean>> {
    return this.client.get<boolean>(
      `/api/guilds/${guildId}/follow/check?userId=${encodeURIComponent(userId)}`
    );
  }
}
