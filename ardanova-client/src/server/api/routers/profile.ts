import { z } from "zod";
import { TRPCError } from "@trpc/server";
import {
  createTRPCRouter,
  publicProcedure,
  protectedProcedure,
} from "~/server/api/trpc";
import { apiClient } from "~/lib/api";

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
   * Get any user's public profile by ID.
   */
  getProfileById: publicProcedure
    .input(z.object({ userId: z.string().min(1) }))
    .query(async ({ input }) => {
      const response = await apiClient.users.getById(input.userId);
      if (!response.data) {
        throw new TRPCError({ code: "NOT_FOUND", message: "Profile not found" });
      }
      const [skillsRes, experienceRes] = await Promise.all([
        apiClient.users.getSkills(input.userId),
        apiClient.users.getExperience(input.userId),
      ]);
      return {
        ...response.data,
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
        website: z.string().url().max(500).optional().or(z.literal("")),
        linkedIn: z.string().max(500).optional(),
        twitter: z.string().max(100).optional(),
      })
    )
    .mutation(async ({ input, ctx }) => {
      const response = await apiClient.users.update(ctx.session.user.id, input);
      if (!response.data) {
        throw new TRPCError({
          code: "INTERNAL_SERVER_ERROR",
          message: "Failed to update profile",
        });
      }
      return response.data;
    }),

  /**
   * Update the current user's profile image.
   */
  updateProfileImage: protectedProcedure
    .input(z.object({ imageUrl: z.string().url() }))
    .mutation(async ({ input, ctx }) => {
      const response = await apiClient.users.update(ctx.session.user.id, {
        image: input.imageUrl,
      });
      if (!response.data) {
        throw new TRPCError({
          code: "INTERNAL_SERVER_ERROR",
          message: "Failed to update image",
        });
      }
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
      })
    )
    .mutation(async ({ input, ctx }) => {
      const response = await apiClient.users.addSkill(ctx.session.user.id, {
        skill: input.skill,
        level: input.level,
      });
      if (!response.data) {
        throw new TRPCError({
          code: "INTERNAL_SERVER_ERROR",
          message: "Failed to add skill",
        });
      }
      return response.data;
    }),

  /**
   * Remove a skill from the current user's profile.
   */
  removeSkill: protectedProcedure
    .input(z.object({ skillId: z.string().min(1) }))
    .mutation(async ({ input, ctx }) => {
      const response = await apiClient.users.deleteSkill(
        ctx.session.user.id,
        input.skillId
      );
      if (response.error) {
        throw new TRPCError({ code: "NOT_FOUND", message: response.error });
      }
      return { success: true };
    }),

  /**
   * Update a skill on the current user's profile.
   */
  updateSkill: protectedProcedure
    .input(
      z.object({
        skillId: z.string().min(1),
        level: z.number().int().min(1).max(10).optional(),
      })
    )
    .mutation(async ({ input, ctx }) => {
      const { skillId, ...data } = input;
      const response = await apiClient.users.updateSkill(
        ctx.session.user.id,
        skillId,
        data
      );
      if (!response.data) {
        throw new TRPCError({ code: "NOT_FOUND", message: "Skill not found" });
      }
      return response.data;
    }),

  // ---- Experience ----

  /**
   * Add work experience to the current user's profile.
   */
  addExperience: protectedProcedure
    .input(
      z.object({
        title: z.string().min(1).max(200),
        company: z.string().min(1).max(200),
        description: z.string().max(2000).optional(),
        startDate: z.coerce.date(),
        endDate: z.coerce.date().optional(),
        isCurrent: z.boolean().optional(),
      })
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
        }
      );
      if (!response.data) {
        throw new TRPCError({
          code: "INTERNAL_SERVER_ERROR",
          message: "Failed to add experience",
        });
      }
      return response.data;
    }),

  /**
   * Remove an experience entry from the current user's profile.
   */
  removeExperience: protectedProcedure
    .input(z.object({ experienceId: z.string().min(1) }))
    .mutation(async ({ input, ctx }) => {
      const response = await apiClient.users.deleteExperience(
        ctx.session.user.id,
        input.experienceId
      );
      if (response.error) {
        throw new TRPCError({ code: "NOT_FOUND", message: response.error });
      }
      return { success: true };
    }),

  /**
   * Update an experience entry on the current user's profile.
   */
  updateExperience: protectedProcedure
    .input(
      z.object({
        experienceId: z.string().min(1),
        title: z.string().min(1).max(200).optional(),
        company: z.string().min(1).max(200).optional(),
        description: z.string().max(2000).optional().nullable(),
        startDate: z.coerce.date().optional(),
        endDate: z.coerce.date().optional().nullable(),
        isCurrent: z.boolean().optional(),
      })
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
        }
      );
      if (!response.data) {
        throw new TRPCError({
          code: "NOT_FOUND",
          message: "Experience not found",
        });
      }
      return response.data;
    }),
});
