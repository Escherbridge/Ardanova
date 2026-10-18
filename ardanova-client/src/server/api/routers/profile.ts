import { z } from "zod";
import { TRPCError } from "@trpc/server";
import {
  createTRPCRouter,
  publicProcedure,
  protectedProcedure,
} from "~/server/api/trpc";
import { apiClient } from "~/lib/api";
import type { ApiResponse } from "~/lib/api/base-client";
import { recordIdSchema, requireRequestedUser, toPublicUser } from "./user";

/** True for an absolute http(s) URL; mirrors the .NET profile link rule (UserProfileRules). */
export function isHttpUrl(value: string): boolean {
  try {
    const url = new URL(value.trim());
    return (
      (url.protocol === "http:" || url.protocol === "https:") && url.host !== ""
    );
  } catch {
    return false;
  }
}

/** A handle without a scheme, or an http(s) URL; anything naming another scheme is refused. */
function isHandleOrHttpUrl(value: string): boolean {
  const trimmed = value.trim();
  if (!trimmed) return true;
  if (!/^[a-z][a-z0-9+.-]*:/i.test(trimmed) && !trimmed.startsWith("//"))
    return true;
  return isHttpUrl(trimmed);
}

const linkMessage = "Use an http:// or https:// link.";
const optionalHttpUrl = z
  .string()
  .max(500)
  .refine((value) => value === "" || isHttpUrl(value), linkMessage);

/** Maps a failed .NET response onto the matching tRPC error, keeping the backend's 4xx reason. */
export function backendFailure(
  response: ApiResponse<unknown>,
  fallback: string,
): TRPCError {
  const status = response.status;
  const code =
    status === 400 || status === 409 || status === 422
      ? "BAD_REQUEST"
      : status === 401
        ? "UNAUTHORIZED"
        : status === 403
          ? "FORBIDDEN"
          : status === 404
            ? "NOT_FOUND"
            : "INTERNAL_SERVER_ERROR";
  const message =
    code !== "INTERNAL_SERVER_ERROR" && response.error
      ? response.error
      : fallback;
  return new TRPCError({ code, message });
}

// ---------------------------------------------------------------------------
// Router
// ---------------------------------------------------------------------------

export const profileRouter = createTRPCRouter({
  // ---- Queries ----

  /**
   * Get the current authenticated user's profile with skills and experience.
   */
  getMyProfile: protectedProcedure.query(async ({ ctx }) => {
    const response = await apiClient.users.getById(ctx.session.user.id);
    if (!response.data) {
      throw new TRPCError({ code: "NOT_FOUND", message: "Profile not found" });
    }
    const [skillsRes, experienceRes] = await Promise.all([
      apiClient.users.getSkills(ctx.session.user.id),
      apiClient.users.getExperience(ctx.session.user.id),
    ]);
    return {
      ...response.data,
      skills: skillsRes.data ?? [],
      experience: experienceRes.data ?? [],
    };
  }),

  /**
   * Get any user's profile by ID; contact and authority fields only for the user themself.
   */
  getProfileById: publicProcedure
    .input(z.object({ userId: recordIdSchema }))
    .query(async ({ input, ctx }) => {
      const response = await apiClient.users.getById(input.userId);
      const user = requireRequestedUser(response.data, input.userId);
      const [skillsRes, experienceRes] = await Promise.all([
        apiClient.users.getSkills(input.userId),
        apiClient.users.getExperience(input.userId),
      ]);
      return {
        ...(ctx.session?.user.id === user.id ? user : toPublicUser(user)),
        skills: skillsRes.data ?? [],
        experience: experienceRes.data ?? [],
      };
    }),

  /**
   * Get the current user's trust score and reputation data.
   */
  getMyTrustScore: protectedProcedure.query(async ({ ctx }) => {
    const response = await apiClient.users.getById(ctx.session.user.id);
    if (!response.data) {
      throw new TRPCError({ code: "NOT_FOUND", message: "User not found" });
    }
    return {
      trustScore: response.data.trustScore,
      totalXP: response.data.totalXP,
      level: response.data.level,
      tier: response.data.tier,
    };
  }),

  // ---- Mutations ----

  /**
   * Update the current user's profile fields.
   */
  updateProfile: protectedProcedure
    .input(
      z.object({
        name: z.string().min(1).max(100).optional(),
        bio: z.string().max(2000).optional(),
        location: z.string().max(200).optional(),
        phone: z.string().max(30).optional(),
        website: optionalHttpUrl.optional(),
        linkedIn: z
          .string()
          .max(500)
          .refine(isHandleOrHttpUrl, linkMessage)
          .optional(),
        twitter: z
          .string()
          .max(100)
          .refine(isHandleOrHttpUrl, linkMessage)
          .optional(),
      }),
    )
    .mutation(async ({ input, ctx }) => {
      const response = await apiClient.users.update(ctx.session.user.id, input);
      if (!response.data)
        throw backendFailure(response, "Failed to update profile");
      return response.data;
    }),

  /**
   * Update the current user's profile image.
   */
  updateProfileImage: protectedProcedure
    .input(
      z.object({
        imageUrl: z.string().max(2048).refine(isHttpUrl, linkMessage),
      }),
    )
    .mutation(async ({ input, ctx }) => {
      const response = await apiClient.users.update(ctx.session.user.id, {
        image: input.imageUrl,
      });
      if (!response.data)
        throw backendFailure(response, "Failed to update image");
      return response.data;
    }),

  // ---- Skills ----

  /**
   * Add a skill to the current user's profile.
   */
  addSkill: protectedProcedure
    .input(
      z.object({
        skill: z.string().min(1).max(100),
        level: z.number().int().min(1).max(10).optional(),
      }),
    )
    .mutation(async ({ input, ctx }) => {
      const response = await apiClient.users.addSkill(ctx.session.user.id, {
        skill: input.skill,
        level: input.level,
      });
      if (!response.data) throw backendFailure(response, "Failed to add skill");
      return response.data;
    }),

  /**
   * Remove a skill from the current user's profile.
   */
  removeSkill: protectedProcedure
    .input(z.object({ skillId: recordIdSchema }))
    .mutation(async ({ input, ctx }) => {
      const response = await apiClient.users.deleteSkill(
        ctx.session.user.id,
        input.skillId,
      );
      if (response.error) throw backendFailure(response, "Failed to remove");
      return { success: true };
    }),

  /**
   * Update a skill on the current user's profile.
   */
  updateSkill: protectedProcedure
    .input(
      z.object({
        skillId: recordIdSchema,
        level: z.number().int().min(1).max(10).optional(),
      }),
    )
    .mutation(async ({ input, ctx }) => {
      const { skillId, ...data } = input;
      const response = await apiClient.users.updateSkill(
        ctx.session.user.id,
        skillId,
        data,
      );
      if (!response.data)
        throw backendFailure(response, "Failed to update skill");
      return response.data;
    }),

  // ---- Experience ----

  /**
   * Add work experience to the current user's profile.
   */
  addExperience: protectedProcedure
    .input(
      z
        .object({
          title: z.string().min(1).max(200),
          company: z.string().min(1).max(200),
          description: z.string().max(2000).optional(),
          startDate: z.coerce.date(),
          endDate: z.coerce.date().optional(),
          isCurrent: z.boolean().optional(),
        })
        .refine((entry) => !entry.endDate || entry.endDate >= entry.startDate, {
          message: "The end date is before the start date.",
          path: ["endDate"],
        }),
    )
    .mutation(async ({ input, ctx }) => {
      const response = await apiClient.users.addExperience(
        ctx.session.user.id,
        {
          title: input.title,
          company: input.company,
          description: input.description,
          startDate: input.startDate.toISOString(),
          endDate: input.endDate?.toISOString(),
          isCurrent: input.isCurrent,
        },
      );
      if (!response.data)
        throw backendFailure(response, "Failed to add experience");
      return response.data;
    }),

  /**
   * Remove an experience entry from the current user's profile.
   */
  removeExperience: protectedProcedure
    .input(z.object({ experienceId: recordIdSchema }))
    .mutation(async ({ input, ctx }) => {
      const response = await apiClient.users.deleteExperience(
        ctx.session.user.id,
        input.experienceId,
      );
      if (response.error) throw backendFailure(response, "Failed to remove");
      return { success: true };
    }),

  /**
   * Update an experience entry on the current user's profile.
   */
  updateExperience: protectedProcedure
    .input(
      z.object({
        experienceId: recordIdSchema,
        title: z.string().min(1).max(200).optional(),
        company: z.string().min(1).max(200).optional(),
        description: z.string().max(2000).optional().nullable(),
        startDate: z.coerce.date().optional(),
        endDate: z.coerce.date().optional().nullable(),
        isCurrent: z.boolean().optional(),
      }),
    )
    .mutation(async ({ input, ctx }) => {
      const { experienceId } = input;
      const response = await apiClient.users.updateExperience(
        ctx.session.user.id,
        experienceId,
        {
          title: input.title,
          company: input.company,
          description: input.description ?? undefined,
          startDate: input.startDate?.toISOString(),
          endDate: input.endDate?.toISOString() ?? undefined,
          isCurrent: input.isCurrent,
        },
      );
      if (!response.data)
        throw backendFailure(response, "Failed to update experience");
      return response.data;
    }),
});
