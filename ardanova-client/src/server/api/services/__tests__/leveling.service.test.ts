import { describe, it, expect, vi, beforeEach } from "vitest";
import {
  calculateLevel,
  getXPForLevel,
  getXPForNextLevel,
  updateUserLevel,
  LEVEL_THRESHOLDS,
  BASE_XP,
} from "../leveling.service";

// ---------------------------------------------------------------------------
// Pure function tests (no DB mocking needed)
// ---------------------------------------------------------------------------

// ===========================================================================
// LEVEL_THRESHOLDS constant
// ===========================================================================
describe("LEVEL_THRESHOLDS", () => {
  it("should export an array of at least 50 level thresholds", () => {
    expect(Array.isArray(LEVEL_THRESHOLDS)).toBe(true);
    expect(LEVEL_THRESHOLDS.length).toBeGreaterThanOrEqual(50);
  });

  it("should have 0 as the first threshold (level 1)", () => {
    expect(LEVEL_THRESHOLDS[0]).toBe(0);
  });

  it("should be monotonically increasing", () => {
    for (let i = 1; i < LEVEL_THRESHOLDS.length; i++) {
      expect(LEVEL_THRESHOLDS[i]).toBeGreaterThan(LEVEL_THRESHOLDS[i - 1]);
    }
  });

  it("should have increasing gaps between levels (quadratic growth)", () => {
    // The gap between level N and level N+1 should be larger than between N-1 and N
    for (let i = 2; i < Math.min(LEVEL_THRESHOLDS.length, 20); i++) {
      const gap1 = LEVEL_THRESHOLDS[i] - LEVEL_THRESHOLDS[i - 1];
      const gap2 = LEVEL_THRESHOLDS[i - 1] - LEVEL_THRESHOLDS[i - 2];
      expect(gap1).toBeGreaterThan(gap2);
    }
  });
});

// ===========================================================================
// BASE_XP constant
// ===========================================================================
describe("BASE_XP", () => {
  it("should be a positive number", () => {
    expect(BASE_XP).toBeGreaterThan(0);
  });

  it("should equal 100 (the base XP per level)", () => {
    expect(BASE_XP).toBe(100);
  });
});

// ===========================================================================
// calculateLevel
// ===========================================================================
describe("calculateLevel", () => {
  it("should return level 1 for 0 XP", () => {
    expect(calculateLevel(0)).toBe(1);
  });

  it("should return level 1 for XP below level 2 threshold", () => {
    expect(calculateLevel(50)).toBe(1);
    expect(calculateLevel(99)).toBe(1);
  });

  it("should return level 2 at exactly 100 XP", () => {
    expect(calculateLevel(100)).toBe(2);
  });

  it("should return level 2 for XP between level 2 and 3 thresholds", () => {
    expect(calculateLevel(150)).toBe(2);
    expect(calculateLevel(399)).toBe(2);
  });

  it("should return level 3 at exactly 400 XP", () => {
    expect(calculateLevel(400)).toBe(3);
  });

  it("should return level 4 at exactly 900 XP", () => {
    expect(calculateLevel(900)).toBe(4);
  });

  it("should return level 5 at exactly 1600 XP", () => {
    expect(calculateLevel(1600)).toBe(5);
  });

  it("should handle very high XP values", () => {
    // Level 10 threshold = 100 * (10-1)^2 = 8100
    expect(calculateLevel(8100)).toBe(10);
    expect(calculateLevel(10000)).toBe(11);
  });

  it("should return level 1 for negative XP (edge case)", () => {
    expect(calculateLevel(-10)).toBe(1);
  });

  it("should follow the quadratic formula: threshold = BASE_XP * (level - 1)^2", () => {
    // Verify the formula at several points
    for (let level = 1; level <= 20; level++) {
      const threshold = 100 * (level - 1) ** 2;
      expect(calculateLevel(threshold)).toBe(level);

      // One XP below should be the previous level (for levels > 1)
      if (level > 1) {
        expect(calculateLevel(threshold - 1)).toBe(level - 1);
      }
    }
  });
});

// ===========================================================================
// getXPForLevel
// ===========================================================================
describe("getXPForLevel", () => {
  it("should return 0 for level 1", () => {
    expect(getXPForLevel(1)).toBe(0);
  });

  it("should return 100 for level 2", () => {
    expect(getXPForLevel(2)).toBe(100);
  });

  it("should return 400 for level 3", () => {
    expect(getXPForLevel(3)).toBe(400);
  });

  it("should return 900 for level 4", () => {
    expect(getXPForLevel(4)).toBe(900);
  });

  it("should return 1600 for level 5", () => {
    expect(getXPForLevel(5)).toBe(1600);
  });

  it("should follow the quadratic formula: BASE_XP * (level - 1)^2", () => {
    for (let level = 1; level <= 30; level++) {
      expect(getXPForLevel(level)).toBe(100 * (level - 1) ** 2);
    }
  });

  it("should throw for level less than 1", () => {
    expect(() => getXPForLevel(0)).toThrow();
    expect(() => getXPForLevel(-1)).toThrow();
  });
});

// ===========================================================================
// getXPForNextLevel
// ===========================================================================
describe("getXPForNextLevel", () => {
  it("should return correct progress info at 0 XP", () => {
    const result = getXPForNextLevel(0);
    expect(result.currentLevel).toBe(1);
    expect(result.nextLevelXP).toBe(100); // XP needed to reach level 2
    expect(result.xpProgress).toBe(0);
    expect(result.xpNeeded).toBe(100);
  });

  it("should return correct progress info at 50 XP (mid level 1)", () => {
    const result = getXPForNextLevel(50);
    expect(result.currentLevel).toBe(1);
    expect(result.nextLevelXP).toBe(100);
    expect(result.xpProgress).toBe(50);
    expect(result.xpNeeded).toBe(50);
  });

  it("should return correct progress info at exactly 100 XP (level 2)", () => {
    const result = getXPForNextLevel(100);
    expect(result.currentLevel).toBe(2);
    expect(result.nextLevelXP).toBe(400); // XP needed to reach level 3
    expect(result.xpProgress).toBe(0); // 100 - 100 = 0 progress into level 2
    expect(result.xpNeeded).toBe(300); // 400 - 100 = 300 more needed
  });

  it("should return correct progress info at 250 XP (mid level 2)", () => {
    const result = getXPForNextLevel(250);
    expect(result.currentLevel).toBe(2);
    expect(result.nextLevelXP).toBe(400);
    expect(result.xpProgress).toBe(150); // 250 - 100 = 150 progress into level 2
    expect(result.xpNeeded).toBe(150); // 400 - 250 = 150 more needed
  });

  it("should return correct progress info at high XP values", () => {
    // Level 10: threshold = 100 * 9^2 = 8100
    // Level 11: threshold = 100 * 10^2 = 10000
    const result = getXPForNextLevel(9000);
    expect(result.currentLevel).toBe(10);
    expect(result.nextLevelXP).toBe(10000);
    expect(result.xpProgress).toBe(900); // 9000 - 8100 = 900
    expect(result.xpNeeded).toBe(1000); // 10000 - 9000 = 1000
  });
});

// ===========================================================================
// updateUserLevel (requires DB mock)
// ===========================================================================
describe("updateUserLevel", () => {
  const mockUserFindUniqueOrThrow = vi.fn();
  const mockUserUpdate = vi.fn();

  const db = {
    user: {
      findUniqueOrThrow: mockUserFindUniqueOrThrow,
      update: mockUserUpdate,
    },
  } as unknown as Parameters<typeof updateUserLevel>[0]["db"];

  beforeEach(() => {
    vi.resetAllMocks();
  });

  it("should detect a level-up and update the user", async () => {
    // User has 400 XP but is still level 1 => should be level 3
    mockUserFindUniqueOrThrow.mockResolvedValue({
      id: "user-1",
      totalXP: 400,
      level: 1,
    });
    mockUserUpdate.mockResolvedValue({
      id: "user-1",
      totalXP: 400,
      level: 3,
    });

    const result = await updateUserLevel({ db, userId: "user-1" });

    expect(result.previousLevel).toBe(1);
    expect(result.newLevel).toBe(3);
    expect(result.leveledUp).toBe(true);
    expect(mockUserUpdate).toHaveBeenCalledWith({
      where: { id: "user-1" },
      data: { level: 3 },
    });
  });

  it("should not update if level is already correct", async () => {
    // User is at 400 XP and already level 3 => no change
    mockUserFindUniqueOrThrow.mockResolvedValue({
      id: "user-1",
      totalXP: 400,
      level: 3,
    });

    const result = await updateUserLevel({ db, userId: "user-1" });

    expect(result.previousLevel).toBe(3);
    expect(result.newLevel).toBe(3);
    expect(result.leveledUp).toBe(false);
    expect(mockUserUpdate).not.toHaveBeenCalled();
  });

  it("should handle user at level 1 with 0 XP (no change)", async () => {
    mockUserFindUniqueOrThrow.mockResolvedValue({
      id: "user-1",
      totalXP: 0,
      level: 1,
    });

    const result = await updateUserLevel({ db, userId: "user-1" });

    expect(result.previousLevel).toBe(1);
    expect(result.newLevel).toBe(1);
    expect(result.leveledUp).toBe(false);
    expect(mockUserUpdate).not.toHaveBeenCalled();
  });

  it("should handle level-up from accumulating XP (e.g., 99 -> 150 XP)", async () => {
    // User had 99 XP (level 1), gained XP and now has 150 (should be level 2)
    mockUserFindUniqueOrThrow.mockResolvedValue({
      id: "user-1",
      totalXP: 150,
      level: 1,
    });
    mockUserUpdate.mockResolvedValue({
      id: "user-1",
      totalXP: 150,
      level: 2,
    });

    const result = await updateUserLevel({ db, userId: "user-1" });

    expect(result.previousLevel).toBe(1);
    expect(result.newLevel).toBe(2);
    expect(result.leveledUp).toBe(true);
    expect(mockUserUpdate).toHaveBeenCalledWith({
      where: { id: "user-1" },
      data: { level: 2 },
    });
  });

  it("should fetch user by ID using findUniqueOrThrow", async () => {
    mockUserFindUniqueOrThrow.mockResolvedValue({
      id: "user-42",
      totalXP: 0,
      level: 1,
    });

    await updateUserLevel({ db, userId: "user-42" });

    expect(mockUserFindUniqueOrThrow).toHaveBeenCalledWith({
      where: { id: "user-42" },
      select: { totalXP: true, level: true },
    });
  });
});
