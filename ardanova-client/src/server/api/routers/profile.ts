import { z } from "zod";
import { TRPCError } from "@trpc/server";
import {
  createTRPCRouter,
  publicProcedure,
  protectedProcedure,
} from "~/server/api/trpc";
import {
  getProfile,
  updateProfile,
  updateProfileImage,
  getTrustScore,
  addSkill,
  removeSkill,
  updateSkill,
  addExperience,
  removeExperience,
  updateExperience,
} from "~/server/api/services/profile.service";

// ---------------------------------------------------------------------------
// Zod schemas
// ---------------------------------------------------------------------------

const updateProfileSchema = z.object({
  name: z.string().min(1).max(100).optional(),
  bio: z.string().max(2000).optional(),
  location: z.string().max(200).optional(),
  phone: z.string().max(30).optional(),
  website: z.string().url().max(500).optional().or(z.literal("")),
  linkedIn: z.string().max(500).optional(),
  twitter: z.string().max(100).optional(),
});

const addSkillSchema = z.object({
  skill: z.string().min(1).max(100),
  level: z.number().int().min(1).max(10).optional(),
});

const updateSkillSchema = z.object({
  skillId: z.string().min(1),
  skill: z.string().min(1).max(100).optional(),
  level: z.number().int().min(1).max(10).optional(),
});

const addExperienceSchema = z.object({
  title: z.string().min(1).max(200),
  company: z.string().min(1).max(200),
  description: z.string().max(2000).optional(),
  startDate: z.coerce.date(),
  endDate: z.coerce.date().optional(),
  isCurrent: z.boolean().optional(),
});

const updateExperienceSchema = z.object({
  experienceId: z.string().min(1),
  title: z.string().min(1).max(200).optional(),
  company: z.string().min(1).max(200).optional(),
  description: z.string().max(2000).optional().nullable(),
  startDate: z.coerce.date().optional(),
  endDate: z.coerce.date().optional().nullable(),
  isCurrent: z.boolean().optional(),
});

// ---------------------------------------------------------------------------
// Router
// ---------------------------------------------------------------------------

export const profileRouter = createTRPCRouter({
  // ---- Queries ----

  /**
   * Get the current authenticated user's profile.
   */
  getMyProfile: protectedProcedure.query(async ({ ctx }) => {
    return getProfile({ db: ctx.db, userId: ctx.session.user.id });
  }),

  /**
   * Get any user's public profile by ID.
   */
  getProfileById: publicProcedure
    .input(z.object({ userId: z.string().min(1) }))
    .query(async ({ input, ctx }) => {
      return getProfile({ db: ctx.db, userId: input.userId });
    }),

  /**
   * Get the current user's trust score and reputation data.
   */
  getMyTrustScore: protectedProcedure.query(async ({ ctx }) => {
    return getTrustScore({ db: ctx.db, userId: ctx.session.user.id });
  }),

  /**
   * Get any user's trust score by ID.
   */
  getTrustScoreById: publicProcedure
    .input(z.object({ userId: z.string().min(1) }))
    .query(async ({ input, ctx }) => {
      return getTrustScore({ db: ctx.db, userId: input.userId });
    }),

  // ---- Mutations ----

  /**
   * Update the current user's profile fields.
   */
  updateProfile: protectedProcedure
    .input(updateProfileSchema)
    .mutation(async ({ input, ctx }) => {
      return updateProfile({
        db: ctx.db,
        userId: ctx.session.user.id,
        data: input,
      });
    }),

  /**
   * Update the current user's profile image.
   */
  updateProfileImage: protectedProcedure
    .input(z.object({ imageUrl: z.string().url() }))
    .mutation(async ({ input, ctx }) => {
      return updateProfileImage({
        db: ctx.db,
        userId: ctx.session.user.id,
        imageUrl: input.imageUrl,
      });
    }),

  // ---- Skills ----

  /**
   * Add a skill to the current user's profile.
   */
  addSkill: protectedProcedure
    .input(addSkillSchema)
    .mutation(async ({ input, ctx }) => {
      return addSkill({
        db: ctx.db,
        userId: ctx.session.user.id,
        skill: input.skill,
        level: input.level,
      });
    }),

  /**
   * Remove a skill from the current user's profile.
   */
  removeSkill: protectedProcedure
    .input(z.object({ skillId: z.string().min(1) }))
    .mutation(async ({ input, ctx }) => {
      try {
        return await removeSkill({
          db: ctx.db,
          skillId: input.skillId,
          userId: ctx.session.user.id,
        });
      } catch (error) {
        const message =
          error instanceof Error ? error.message : "Failed to remove skill";
        if (message === "Skill not found") {
          throw new TRPCError({ code: "NOT_FOUND", message });
        }
        if (message === "You can only remove your own skills") {
          throw new TRPCError({ code: "FORBIDDEN", message });
        }
        throw new TRPCError({ code: "INTERNAL_SERVER_ERROR", message });
      }
    }),

  /**
   * Update a skill on the current user's profile.
   */
  updateSkill: protectedProcedure
    .input(updateSkillSchema)
    .mutation(async ({ input, ctx }) => {
      const { skillId, ...data } = input;
      try {
        return await updateSkill({
          db: ctx.db,
          skillId,
          userId: ctx.session.user.id,
          data,
        });
      } catch (error) {
        const message =
          error instanceof Error ? error.message : "Failed to update skill";
        if (message === "Skill not found") {
          throw new TRPCError({ code: "NOT_FOUND", message });
        }
        if (message === "You can only update your own skills") {
          throw new TRPCError({ code: "FORBIDDEN", message });
        }
        throw new TRPCError({ code: "INTERNAL_SERVER_ERROR", message });
      }
    }),

  // ---- Experience ----

  /**
   * Add work experience to the current user's profile.
   */
  addExperience: protectedProcedure
    .input(addExperienceSchema)
    .mutation(async ({ input, ctx }) => {
      return addExperience({
        db: ctx.db,
        userId: ctx.session.user.id,
        data: {
          title: input.title,
          company: input.company,
          description: input.description,
          startDate: input.startDate,
          endDate: input.endDate,
          isCurrent: input.isCurrent,
        },
      });
    }),

  /**
   * Remove an experience entry from the current user's profile.
   */
  removeExperience: protectedProcedure
    .input(z.object({ experienceId: z.string().min(1) }))
    .mutation(async ({ input, ctx }) => {
      try {
        return await removeExperience({
          db: ctx.db,
          experienceId: input.experienceId,
          userId: ctx.session.user.id,
        });
      } catch (error) {
        const message =
          error instanceof Error
            ? error.message
            : "Failed to remove experience";
        if (message === "Experience not found") {
          throw new TRPCError({ code: "NOT_FOUND", message });
        }
        if (message === "You can only remove your own experience") {
          throw new TRPCError({ code: "FORBIDDEN", message });
        }
        throw new TRPCError({ code: "INTERNAL_SERVER_ERROR", message });
      }
    }),

  /**
   * Update an experience entry on the current user's profile.
   */
  updateExperience: protectedProcedure
    .input(updateExperienceSchema)
    .mutation(async ({ input, ctx }) => {
      const { experienceId, ...data } = input;
      try {
        return await updateExperience({
          db: ctx.db,
          experienceId,
          userId: ctx.session.user.id,
          data,
        });
      } catch (error) {
        const message =
          error instanceof Error
            ? error.message
            : "Failed to update experience";
        if (message === "Experience not found") {
          throw new TRPCError({ code: "NOT_FOUND", message });
        }
        if (message === "You can only update your own experience") {
          throw new TRPCError({ code: "FORBIDDEN", message });
        }
        throw new TRPCError({ code: "INTERNAL_SERVER_ERROR", message });
      }
    }),
});
