import { describe, it, expect, vi, beforeEach } from "vitest";
import {
  awardXP,
  getXPHistory,
  getTotalXP,
  getXPByEventType,
  XP_REWARDS,
} from "../xp.service";

// ---------------------------------------------------------------------------
// Prisma mock
// ---------------------------------------------------------------------------

const mockXPEventCreate = vi.fn();
const mockXPEventFindMany = vi.fn();
const mockXPEventAggregate = vi.fn();
const mockUserUpdate = vi.fn();
const mockUserFindUniqueOrThrow = vi.fn();
const mockTransaction = vi.fn();

const db = {
  xPEvent: {
    create: mockXPEventCreate,
    findMany: mockXPEventFindMany,
    aggregate: mockXPEventAggregate,
  },
  user: {
    update: mockUserUpdate,
    findUniqueOrThrow: mockUserFindUniqueOrThrow,
  },
  $transaction: mockTransaction,
} as unknown as Parameters<typeof awardXP>[0]["db"];

beforeEach(() => {
  vi.resetAllMocks();
});

// ===========================================================================
// XP_REWARDS constant
// ===========================================================================
describe("XP_REWARDS", () => {
  it("should define reward amounts for all expected event types", () => {
    expect(XP_REWARDS.TASK_COMPLETED).toBe(50);
    expect(XP_REWARDS.PROPOSAL_CREATED).toBe(25);
    expect(XP_REWARDS.VOTE_CAST).toBe(10);
    expect(XP_REWARDS.MEMBER_REFERRED).toBe(100);
    expect(XP_REWARDS.STREAK_MAINTAINED).toBe(15);
  });

  it("should define reward amounts for additional event types", () => {
    expect(XP_REWARDS.PROPOSAL_PASSED).toBe(50);
    expect(XP_REWARDS.PROJECT_FUNDED).toBe(75);
    expect(XP_REWARDS.ACHIEVEMENT_EARNED).toBe(30);
    expect(XP_REWARDS.LEVEL_UP).toBe(20);
    expect(XP_REWARDS.REVIEW_GIVEN).toBe(15);
    expect(XP_REWARDS.CONTRIBUTION_MADE).toBe(20);
  });
});

// ===========================================================================
// awardXP
// ===========================================================================
describe("awardXP", () => {
  it("should create XPEvent and increment user totalXP atomically via transaction", async () => {
    const fakeXPEvent = {
      id: "xp-1",
      userId: "user-1",
      eventType: "TASK_COMPLETED",
      amount: 50,
      source: "task",
      sourceId: "task-123",
      metadata: { taskName: "Build feature" },
      createdAt: new Date(),
    };

    // $transaction receives a callback; we simulate it by calling the callback with db
    mockTransaction.mockImplementation(async (cb: (tx: typeof db) => Promise<unknown>) => {
      return cb(db);
    });
    mockXPEventCreate.mockResolvedValue(fakeXPEvent);
    mockUserUpdate.mockResolvedValue({ id: "user-1", totalXP: 150 });
    // Mock for updateUserLevel call after transaction (no level change)
    mockUserFindUniqueOrThrow.mockResolvedValue({ totalXP: 150, level: 2 });

    const result = await awardXP({
      db,
      userId: "user-1",
      eventType: "TASK_COMPLETED",
      amount: 50,
      source: "task",
      sourceId: "task-123",
      metadata: { taskName: "Build feature" },
    });

    expect(result).toEqual(fakeXPEvent);
    expect(mockTransaction).toHaveBeenCalledTimes(1);
    expect(mockXPEventCreate).toHaveBeenCalledWith({
      data: {
        userId: "user-1",
        eventType: "TASK_COMPLETED",
        amount: 50,
        source: "task",
        sourceId: "task-123",
        metadata: { taskName: "Build feature" },
      },
    });
    expect(mockUserUpdate).toHaveBeenCalledWith({
      where: { id: "user-1" },
      data: {
        totalXP: { increment: 50 },
      },
    });
  });

  it("should work without optional sourceId and metadata", async () => {
    const fakeXPEvent = {
      id: "xp-2",
      userId: "user-1",
      eventType: "VOTE_CAST",
      amount: 10,
      source: "governance",
      sourceId: null,
      metadata: null,
      createdAt: new Date(),
    };

    mockTransaction.mockImplementation(async (cb: (tx: typeof db) => Promise<unknown>) => {
      return cb(db);
    });
    mockXPEventCreate.mockResolvedValue(fakeXPEvent);
    mockUserUpdate.mockResolvedValue({ id: "user-1", totalXP: 10 });
    // Mock for updateUserLevel call after transaction (no level change)
    mockUserFindUniqueOrThrow.mockResolvedValue({ totalXP: 10, level: 1 });

    const result = await awardXP({
      db,
      userId: "user-1",
      eventType: "VOTE_CAST",
      amount: 10,
      source: "governance",
    });

    expect(result).toEqual(fakeXPEvent);
    expect(mockXPEventCreate).toHaveBeenCalledWith({
      data: {
        userId: "user-1",
        eventType: "VOTE_CAST",
        amount: 10,
        source: "governance",
        sourceId: undefined,
        metadata: undefined,
      },
    });
  });

  it("should throw if amount is not positive", async () => {
    await expect(
      awardXP({
        db,
        userId: "user-1",
        eventType: "TASK_COMPLETED",
        amount: 0,
        source: "task",
      })
    ).rejects.toThrow("XP amount must be a positive integer");

    await expect(
      awardXP({
        db,
        userId: "user-1",
        eventType: "TASK_COMPLETED",
        amount: -5,
        source: "task",
      })
    ).rejects.toThrow("XP amount must be a positive integer");
  });
});

// ===========================================================================
// getXPHistory
// ===========================================================================
describe("getXPHistory", () => {
  it("should return paginated XP events for a user", async () => {
    const fakeEvents = [
      {
        id: "xp-1",
        userId: "user-1",
        eventType: "TASK_COMPLETED",
        amount: 50,
        source: "task",
        sourceId: "task-1",
        metadata: null,
        createdAt: new Date(),
      },
      {
        id: "xp-2",
        userId: "user-1",
        eventType: "VOTE_CAST",
        amount: 10,
        source: "governance",
        sourceId: null,
        metadata: null,
        createdAt: new Date(),
      },
    ];
    mockXPEventFindMany.mockResolvedValue(fakeEvents);

    const result = await getXPHistory({ db, userId: "user-1" });

    expect(result).toEqual(fakeEvents);
    expect(mockXPEventFindMany).toHaveBeenCalledWith({
      where: { userId: "user-1" },
      orderBy: { createdAt: "desc" },
      take: 20,
      skip: 0,
    });
  });

  it("should support pagination with limit and offset", async () => {
    mockXPEventFindMany.mockResolvedValue([]);

    await getXPHistory({ db, userId: "user-1", limit: 10, offset: 20 });

    expect(mockXPEventFindMany).toHaveBeenCalledWith({
      where: { userId: "user-1" },
      orderBy: { createdAt: "desc" },
      take: 10,
      skip: 20,
    });
  });

  it("should support filtering by eventType", async () => {
    mockXPEventFindMany.mockResolvedValue([]);

    await getXPHistory({
      db,
      userId: "user-1",
      eventType: "TASK_COMPLETED",
    });

    expect(mockXPEventFindMany).toHaveBeenCalledWith({
      where: { userId: "user-1", eventType: "TASK_COMPLETED" },
      orderBy: { createdAt: "desc" },
      take: 20,
      skip: 0,
    });
  });
});

// ===========================================================================
// getTotalXP
// ===========================================================================
describe("getTotalXP", () => {
  it("should return the user totalXP from the aggregate", async () => {
    mockXPEventAggregate.mockResolvedValue({
      _sum: { amount: 350 },
    });

    const result = await getTotalXP({ db, userId: "user-1" });

    expect(result).toBe(350);
    expect(mockXPEventAggregate).toHaveBeenCalledWith({
      where: { userId: "user-1" },
      _sum: { amount: true },
    });
  });

  it("should return 0 if user has no XP events", async () => {
    mockXPEventAggregate.mockResolvedValue({
      _sum: { amount: null },
    });

    const result = await getTotalXP({ db, userId: "user-1" });

    expect(result).toBe(0);
  });
});

// ===========================================================================
// getXPByEventType
// ===========================================================================
describe("getXPByEventType", () => {
  it("should return sum of XP for a specific event type", async () => {
    mockXPEventAggregate.mockResolvedValue({
      _sum: { amount: 200 },
    });

    const result = await getXPByEventType({
      db,
      userId: "user-1",
      eventType: "TASK_COMPLETED",
    });

    expect(result).toBe(200);
    expect(mockXPEventAggregate).toHaveBeenCalledWith({
      where: { userId: "user-1", eventType: "TASK_COMPLETED" },
      _sum: { amount: true },
    });
  });

  it("should return 0 if no events of that type exist", async () => {
    mockXPEventAggregate.mockResolvedValue({
      _sum: { amount: null },
    });

    const result = await getXPByEventType({
      db,
      userId: "user-1",
      eventType: "PROPOSAL_CREATED",
    });

    expect(result).toBe(0);
  });
});
