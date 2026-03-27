import type { PrismaClient, XPEvent, XPEventType, Prisma } from "@prisma/client";
import { updateUserLevel } from "./leveling.service";

// ---------------------------------------------------------------------------
// Types
// ---------------------------------------------------------------------------

type DB = Pick<PrismaClient, "xPEvent" | "user" | "$transaction">;

interface AwardXPInput {
  db: DB;
  userId: string;
  eventType: XPEventType;
  amount: number;
  source: string;
  sourceId?: string;
  metadata?: Prisma.InputJsonValue;
}

interface GetXPHistoryInput {
  db: DB;
  userId: string;
  eventType?: XPEventType;
  limit?: number;
  offset?: number;
}

interface UserIdInput {
  db: DB;
  userId: string;
}

interface XPByEventTypeInput extends UserIdInput {
  eventType: XPEventType;
}

// ---------------------------------------------------------------------------
// Constants
// ---------------------------------------------------------------------------

/**
 * Default XP reward amounts per event type.
 * Used as the canonical source for how much XP each action is worth.
 */
export const XP_REWARDS: Record<XPEventType, number> = {
  TASK_COMPLETED: 50,
  PROPOSAL_CREATED: 25,
  PROPOSAL_PASSED: 50,
  VOTE_CAST: 10,
  PROJECT_FUNDED: 75,
  MEMBER_REFERRED: 100,
  ACHIEVEMENT_EARNED: 30,
  STREAK_MAINTAINED: 15,
  LEVEL_UP: 20,
  REVIEW_GIVEN: 15,
  CONTRIBUTION_MADE: 20,
} as const;

// ---------------------------------------------------------------------------
// Mutations
// ---------------------------------------------------------------------------

/**
 * Award XP to a user. Creates an XPEvent record and atomically increments
 * the user's totalXP using a Prisma transaction. After the transaction,
 * recalculates the user's level and awards bonus LEVEL_UP XP if applicable.
 *
 * @throws {Error} If amount is not a positive integer.
 */
export async function awardXP(input: AwardXPInput): Promise<XPEvent> {
  const { db, userId, eventType, amount, source, sourceId, metadata } = input;

  if (amount <= 0) {
    throw new Error("XP amount must be a positive integer");
  }

  const xpEvent = await db.$transaction(async (tx) => {
    const event = await tx.xPEvent.create({
      data: {
        userId,
        eventType,
        amount,
        source,
        sourceId,
        metadata,
      },
    });

    await tx.user.update({
      where: { id: userId },
      data: {
        totalXP: { increment: amount },
      },
    });

    return event;
  });

  // Recalculate user level after XP is updated.
  // Skip level-up check for LEVEL_UP events to prevent infinite recursion.
  if (eventType !== "LEVEL_UP") {
    const levelResult = await updateUserLevel({ db: db as Parameters<typeof updateUserLevel>[0]["db"], userId });

    if (levelResult.leveledUp) {
      // Award bonus XP for leveling up (non-recursive since eventType is LEVEL_UP)
      await awardXP({
        db,
        userId,
        eventType: "LEVEL_UP",
        amount: XP_REWARDS.LEVEL_UP,
        source: "leveling",
        metadata: {
          previousLevel: levelResult.previousLevel,
          newLevel: levelResult.newLevel,
        },
      });
    }
  }

  return xpEvent;
}

// ---------------------------------------------------------------------------
// Queries
// ---------------------------------------------------------------------------

/**
 * Get paginated XP events for a user, ordered by most recent first.
 * Optionally filter by event type.
 */
export async function getXPHistory(input: GetXPHistoryInput): Promise<XPEvent[]> {
  const { db, userId, eventType, limit = 20, offset = 0 } = input;

  return db.xPEvent.findMany({
    where: {
      userId,
      ...(eventType ? { eventType } : {}),
    },
    orderBy: { createdAt: "desc" },
    take: limit,
    skip: offset,
  });
}

/**
 * Get the total XP for a user by summing all XPEvent amounts.
 * Returns 0 if the user has no XP events.
 */
export async function getTotalXP(input: UserIdInput): Promise<number> {
  const { db, userId } = input;

  const result = await db.xPEvent.aggregate({
    where: { userId },
    _sum: { amount: true },
  });

  return result._sum.amount ?? 0;
}

/**
 * Get the sum of XP for a specific event type for a user.
 * Returns 0 if no events of that type exist.
 */
export async function getXPByEventType(input: XPByEventTypeInput): Promise<number> {
  const { db, userId, eventType } = input;

  const result = await db.xPEvent.aggregate({
    where: { userId, eventType },
    _sum: { amount: true },
  });

  return result._sum.amount ?? 0;
}
