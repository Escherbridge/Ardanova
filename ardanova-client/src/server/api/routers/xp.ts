import { z } from "zod";
import type { Prisma } from "@prisma/client";
import { TRPCError } from "@trpc/server";
import {
  createTRPCRouter,
  publicProcedure,
  protectedProcedure,
  adminProcedure,
} from "~/server/api/trpc";
import {
  awardXP,
  getXPHistory,
  getTotalXP,
  getXPByEventType,
  XP_REWARDS,
} from "~/server/api/services/xp.service";
import {
  getXPForNextLevel,
  getXPForLevel,
  LEVEL_THRESHOLDS,
} from "~/server/api/services/leveling.service";

// ---------------------------------------------------------------------------
// Zod schemas
// ---------------------------------------------------------------------------

/** Zod schema that validates to Prisma.InputJsonValue */
const jsonValue: z.ZodType<Prisma.InputJsonValue> = z.lazy(() =>
  z.union([
    z.string(),
    z.number(),
    z.boolean(),
    z.null(),
    z.array(jsonValue),
    z.record(jsonValue),
  ])
);

const xpEventTypeEnum = z.enum([
  "TASK_COMPLETED",
  "PROPOSAL_CREATED",
  "PROPOSAL_PASSED",
  "VOTE_CAST",
  "PROJECT_FUNDED",
  "MEMBER_REFERRED",
  "ACHIEVEMENT_EARNED",
  "STREAK_MAINTAINED",
  "LEVEL_UP",
  "REVIEW_GIVEN",
  "CONTRIBUTION_MADE",
]);

const getXPHistorySchema = z.object({
  eventType: xpEventTypeEnum.optional(),
  limit: z.number().int().min(1).max(100).optional(),
  offset: z.number().int().min(0).optional(),
});

const awardXPSchema = z.object({
  userId: z.string().min(1),
  eventType: xpEventTypeEnum,
  amount: z.number().int().min(1),
  source: z.string().min(1),
  sourceId: z.string().optional(),
  metadata: z.record(jsonValue).optional(),
});

// ---------------------------------------------------------------------------
// Router
// ---------------------------------------------------------------------------

export const xpRouter = createTRPCRouter({
  // ---- Queries ----

  /**
   * Get the current authenticated user's total XP.
   */
  getMyXP: protectedProcedure.query(async ({ ctx }) => {
    const totalXP = await getTotalXP({
      db: ctx.db,
      userId: ctx.session.user.id,
    });
    return { totalXP };
  }),

  /**
   * Get the current authenticated user's XP event history with pagination.
   */
  getMyXPHistory: protectedProcedure
    .input(getXPHistorySchema.optional())
    .query(async ({ input, ctx }) => {
      return getXPHistory({
        db: ctx.db,
        userId: ctx.session.user.id,
        eventType: input?.eventType,
        limit: input?.limit,
        offset: input?.offset,
      });
    }),

  /**
   * Get any user's total XP by ID (public, for leaderboards).
   */
  getXPByUser: publicProcedure
    .input(z.object({ userId: z.string().min(1) }))
    .query(async ({ input, ctx }) => {
      const totalXP = await getTotalXP({
        db: ctx.db,
        userId: input.userId,
      });
      return { totalXP };
    }),

  /**
   * Get any user's XP breakdown by event type (public, for leaderboards).
   */
  getXPByUserEventType: publicProcedure
    .input(
      z.object({
        userId: z.string().min(1),
        eventType: xpEventTypeEnum,
      })
    )
    .query(async ({ input, ctx }) => {
      const xp = await getXPByEventType({
        db: ctx.db,
        userId: input.userId,
        eventType: input.eventType,
      });
      return { xp };
    }),

  /**
   * Get the XP rewards configuration (public).
   */
  getRewards: publicProcedure.query(() => {
    return XP_REWARDS;
  }),

  // ---- Leveling Queries ----

  /**
   * Get the current authenticated user's level info including progress to
   * the next level.
   */
  getMyLevel: protectedProcedure.query(async ({ ctx }) => {
    const user = await ctx.db.user.findUniqueOrThrow({
      where: { id: ctx.session.user.id },
      select: { totalXP: true, level: true },
    });
    const progress = getXPForNextLevel(user.totalXP);
    return {
      level: user.level,
      totalXP: user.totalXP,
      ...progress,
    };
  }),

  /**
   * Get XP thresholds for any level (public, for UI display).
   * Returns the threshold for the requested level and the full thresholds
   * array for the first 50 levels.
   */
  getLevelInfo: publicProcedure
    .input(z.object({ level: z.number().int().min(1) }))
    .query(({ input }) => {
      return {
        level: input.level,
        xpRequired: getXPForLevel(input.level),
        thresholds: LEVEL_THRESHOLDS,
      };
    }),

  // ---- Mutations ----

  /**
   * Admin-only procedure to manually award XP to a user.
   * Most XP awarding happens internally via service calls from other services.
   */
  awardXP: adminProcedure
    .input(awardXPSchema)
    .mutation(async ({ input, ctx }) => {
      try {
        return await awardXP({
          db: ctx.db,
          userId: input.userId,
          eventType: input.eventType,
          amount: input.amount,
          source: input.source,
          sourceId: input.sourceId,
          metadata: input.metadata,
        });
      } catch (error) {
        const message =
          error instanceof Error ? error.message : "Failed to award XP";
        if (message === "XP amount must be a positive integer") {
          throw new TRPCError({ code: "BAD_REQUEST", message });
        }
        throw new TRPCError({ code: "INTERNAL_SERVER_ERROR", message });
      }
    }),
});
