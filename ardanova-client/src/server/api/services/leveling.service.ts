import type { PrismaClient } from "@prisma/client";

// ---------------------------------------------------------------------------
// Types
// ---------------------------------------------------------------------------

type DB = Pick<PrismaClient, "user">;

interface UpdateUserLevelInput {
  db: DB;
  userId: string;
}

interface UpdateUserLevelResult {
  previousLevel: number;
  newLevel: number;
  leveledUp: boolean;
}

interface LevelProgressInfo {
  currentLevel: number;
  nextLevelXP: number;
  xpProgress: number;
  xpNeeded: number;
}

// ---------------------------------------------------------------------------
// Constants
// ---------------------------------------------------------------------------

/**
 * Base XP constant used in the leveling formula.
 * The XP threshold for level N is: BASE_XP * (N - 1)^2
 *
 * Level 1:    0 XP
 * Level 2:  100 XP
 * Level 3:  400 XP
 * Level 4:  900 XP
 * Level 5: 1600 XP
 * ...
 */
export const BASE_XP = 100;

/**
 * Pre-computed XP thresholds for the first 50 levels.
 * LEVEL_THRESHOLDS[0] = 0 (level 1), LEVEL_THRESHOLDS[1] = 100 (level 2), etc.
 */
export const LEVEL_THRESHOLDS: readonly number[] = Array.from(
  { length: 50 },
  (_, i) => BASE_XP * i ** 2,
);

// ---------------------------------------------------------------------------
// Pure Functions
// ---------------------------------------------------------------------------

/**
 * Calculate the user's level from their totalXP using a quadratic curve.
 *
 * Formula: level = floor(1 + sqrt(totalXP / BASE_XP))
 *
 * This is the inverse of the threshold formula:
 *   threshold(level) = BASE_XP * (level - 1)^2
 *   => level - 1 = sqrt(threshold / BASE_XP)
 *   => level = 1 + sqrt(totalXP / BASE_XP)  (floored)
 *
 * @param totalXP - The user's total accumulated XP. Must be >= 0.
 * @returns The calculated level (minimum 1).
 */
export function calculateLevel(totalXP: number): number {
  if (totalXP <= 0) return 1;
  return Math.floor(1 + Math.sqrt(totalXP / BASE_XP));
}

/**
 * Get the XP threshold required to reach a given level.
 *
 * @param level - The target level (must be >= 1).
 * @returns The minimum XP needed to be at the given level.
 * @throws {Error} If level is less than 1.
 */
export function getXPForLevel(level: number): number {
  if (level < 1) {
    throw new Error("Level must be >= 1");
  }
  return BASE_XP * (level - 1) ** 2;
}

/**
 * Get progress information towards the next level.
 *
 * @param currentXP - The user's current total XP.
 * @returns An object with current level, next level XP threshold,
 *          XP progress into the current level, and XP remaining to next level.
 */
export function getXPForNextLevel(currentXP: number): LevelProgressInfo {
  const currentLevel = calculateLevel(currentXP);
  const currentLevelThreshold = getXPForLevel(currentLevel);
  const nextLevelThreshold = getXPForLevel(currentLevel + 1);

  return {
    currentLevel,
    nextLevelXP: nextLevelThreshold,
    xpProgress: currentXP - currentLevelThreshold,
    xpNeeded: nextLevelThreshold - currentXP,
  };
}

// ---------------------------------------------------------------------------
// Database Operations
// ---------------------------------------------------------------------------

/**
 * Recalculate and update a user's level based on their totalXP.
 *
 * Fetches the user's current totalXP and level from the database,
 * calculates the correct level, and updates if it changed.
 *
 * @returns Whether a level-up occurred and the old/new level values.
 */
export async function updateUserLevel(
  input: UpdateUserLevelInput,
): Promise<UpdateUserLevelResult> {
  const { db, userId } = input;

  const user = await db.user.findUniqueOrThrow({
    where: { id: userId },
    select: { totalXP: true, level: true },
  });

  const newLevel = calculateLevel(user.totalXP);
  const previousLevel = user.level;
  const leveledUp = newLevel !== previousLevel;

  if (leveledUp) {
    await db.user.update({
      where: { id: userId },
      data: { level: newLevel },
    });
  }

  return { previousLevel, newLevel, leveledUp };
}
